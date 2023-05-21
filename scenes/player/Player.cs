using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export, ExportGroup("Movement")] float MovementSpeed = 5.0f;
	
	[Export, ExportGroup("Jump")] float JumpVelocity = 4.5f;
	[Export, ExportGroup("Jump")] double JumpBuffering = 0.2f;
	[Export, ExportGroup("Jump")] double CoyoteTime = 0.2f;
	
	[Export, ExportGroup("Camera")] Vector2 lookSensitivity = new Vector2(0.1f, 0.1f);
	[Export, ExportGroup("Camera"), ExportSubgroup("Head Bob")] bool EnableHeadBob = true;
	[Export, ExportGroup("Camera"), ExportSubgroup("Head Bob")] float BobFrequency = 2.4f;
	[Export, ExportGroup("Camera"), ExportSubgroup("Head Bob")] float BobAmplitude = 0.08f;
	[Export, ExportGroup("Camera"), ExportSubgroup("FOV")] float BaseFOV = 75f;
	[Export, ExportGroup("Camera"), ExportSubgroup("FOV")] float FOVChange = 1.5f;
	[Export, ExportGroup("Camera"), ExportSubgroup("Head Tilt")] bool EnableTilt = true;
	[Export, ExportGroup("Camera"), ExportSubgroup("Head Tilt")] float TiltAmount = 5f;

	private Node3D head;
	private Camera3D camera;

	private Vector2 lookDirection = Vector2.Zero;
	private double jumpBuffer;
	private double coyoteTime;

	private float timeBob;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	public override void _Ready() 
	{
		head = GetNode<Node3D>("Head");
		camera = GetNode<Camera3D>("Head/Camera");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

    public override void _Input( InputEvent @event ) 
	{
        if (@event is InputEventMouseMotion) {
			lookDirection.X -= ( (InputEventMouseMotion) @event ).Relative.X * lookSensitivity.X;
			lookDirection.Y = Mathf.Clamp(lookDirection.Y - ( (InputEventMouseMotion) @event ).Relative.Y * lookSensitivity.Y, -75, 85);
        }
    }

    public override void _Process( double delta ) 
	{
		// Rotate the player horizontally.
        Vector3 rotationDegrees = RotationDegrees;
		rotationDegrees.Y = lookDirection.X;
		RotationDegrees = rotationDegrees;

		// Rotate the camera vertically.
		Vector3 headRotationDegrees = head.RotationDegrees;
		headRotationDegrees.X = lookDirection.Y;
		head.RotationDegrees = headRotationDegrees;

		// Handle head bobbing.
		if (EnableHeadBob) {
			timeBob += (float) delta * Velocity.Length() * Convert.ToSingle(IsOnFloor());

			Transform3D cameraTransform = camera.Transform;
			cameraTransform.Origin = _Headbob(timeBob);
			camera.Transform = cameraTransform;
		}

		// Change FOV based on speed.
		Vector3 floorVelocity = new Vector3(Velocity.X, 0f, Velocity.Z);
		float velocityClamped = Mathf.Clamp(floorVelocity.Length(), 0.5f, MovementSpeed * 2f);
		float targetFOV = BaseFOV + FOVChange * velocityClamped;
		camera.Fov = (float) Mathf.Lerp(camera.Fov, targetFOV, delta * 8.0f);

		// Head tilt sideways.
		if (EnableTilt) {
			float sideDirection = Convert.ToSingle(Input.IsActionPressed("right")) - Convert.ToSingle(Input.IsActionPressed("left"));
			float targetTilt = -TiltAmount * Mathf.Sign(sideDirection);
			camera.RotationDegrees = camera.RotationDegrees.Lerp(Vector3.Back * targetTilt, (float) ( delta * 8.0f ));
		}
	}

    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (IsOnFloor()) {
			coyoteTime = CoyoteTime;
        } else {
			velocity.Y -= gravity * (float) delta;
			coyoteTime -= delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump")) {
			jumpBuffer = JumpBuffering;
        } else {
			jumpBuffer -= delta;
		}

		if (jumpBuffer > 0d && coyoteTime > 0d) {
			velocity.Y = JumpVelocity;
			jumpBuffer = 0d;
		}

		if (Input.IsActionJustReleased("jump") && velocity.Y > 0) {
			coyoteTime = 0d;
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (IsOnFloor()) {
			if (direction != Vector3.Zero) {
				velocity.X = direction.X * MovementSpeed;
				velocity.Z = direction.Z * MovementSpeed;
			} else {
				velocity.X = (float) Mathf.Lerp(Velocity.X, direction.X * MovementSpeed, delta * 7.0);
				velocity.Z = (float) Mathf.Lerp(Velocity.Z, direction.Z * MovementSpeed, delta * 7.0);
			}
		} else {
			velocity.X = (float) Mathf.Lerp(Velocity.X, direction.X * MovementSpeed, delta * 3.0);
			velocity.Z = (float) Mathf.Lerp(Velocity.Z, direction.Z * MovementSpeed, delta * 3.0);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private Vector3 _Headbob(float time) {
		Vector3 position = Vector3.Zero;
		position.Y = Mathf.Sin(time * BobFrequency) * BobAmplitude;
		position.X = Mathf.Cos(time * BobFrequency / 2.0f) * BobAmplitude;
		return position;
	}
}
