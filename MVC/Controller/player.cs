using Godot;
using System;

public partial class player : Node3D
{
	[Export(PropertyHint.Layers3DPhysics)] public uint CameraColliderLayers;
	[Export] private Camera3D camera;

	[Export] private Vector3 top_down_position = new Vector3(0, 1.8f, 0);
	[Export] private Vector3 top_down_rotation = new Vector3(-90, 0, 0);
	[Export] private Vector3 perspective_position = new Vector3(0f, 0.9f, 0.8f);
	[Export] private Vector3 perspective_rotation = new Vector3(-20, 0, 0);
	[Export] private float transition_time = 0.8f;

	private bool top_down = false;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SwitchToTopDown();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void RayCast() {
		PhysicsRayQueryParameters3D ray = new() {
			From = camera.GlobalPosition,
			To = camera.ProjectPosition(GetViewport().GetMousePosition(), 1000),
			CollideWithAreas = false,
			CollideWithBodies = true,
			CollisionMask = CameraColliderLayers
		};

		var hitDictionary = GetWorld3D().DirectSpaceState.IntersectRay(ray);
		
		if (hitDictionary.Count > 0) {
			GD.Print("I see it");
		}
	}

	public void SwitchToTopDown() {
		if (!top_down) {
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "position", top_down_position, transition_time);
			tween.TweenProperty(this, "rotation_degrees", top_down_rotation, transition_time);
			top_down = true;
		}
	}

	public void SwitchToPerspective() {
		if (top_down) {
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "position", perspective_position, transition_time);
			tween.TweenProperty(this, "rotation_degrees", perspective_rotation, transition_time);
			top_down = false;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("CamControl"))
		{
			if (top_down) {
				SwitchToPerspective();
			} else {
				SwitchToTopDown();
			}
		}
	}
}
