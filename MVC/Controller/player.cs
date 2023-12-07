using Godot;
using System;

public partial class player : Node3D
{
	[Export(PropertyHint.Layers3DPhysics)] public uint CameraColliderLayers;
	[Export] private Camera3D camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// RayCast();
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
}
