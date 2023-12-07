using Godot;
using System;
using Utility;

public partial class player : Node3D
{
	[Export(PropertyHint.Layers3DPhysics)] public uint CameraHitLayers;
	[Export] private Camera3D camera;

	[Export] private Vector3 top_down_position = new Vector3(0, 1.8f, 0);
	[Export] private Vector3 top_down_rotation = new Vector3(-90, 0, 0);
	[Export] private Vector3 perspective_position = new Vector3(0f, 0.9f, 0.8f);
	[Export] private Vector3 perspective_rotation = new Vector3(-20, 0, 0);
	[Export] private float transition_time = 0.8f;

	private bool top_down = false;

	private Hit3D _camHoverHit;
	public Hit3D CamHoverHit => _camHoverHit;

	public Action<Hit3D> OnCamHoverNewHit;
	public Action<Hit3D> OnCamHoverOff;
	public Action<Hit3D> OnCamHoverUpdate;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SwitchToTopDown();
		camera = GetChild(0) as Camera3D;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	int wait = 0, waitThresh = 5;

	public override void _PhysicsProcess(double delta)
	{
		// Only fire method if an event is listening
		if (OnCamHoverNewHit != null || OnCamHoverOff != null)
		{
			if (camera != null)
			{
				wait++;
				if (wait % waitThresh == 0)
				{
					if (Raycasting.RayCast(camera, CameraHitLayers, out Hit3D hit))
					{
						// If it is a new hit
						if (_camHoverHit != hit)
						{
							Hit3D previousHit = _camHoverHit;
							_camHoverHit = hit;

							// If the colliders are NOT the same (meaning this hit has hit a new object)
							if (_camHoverHit.collider != previousHit.collider)
							{
								OnCamHoverNewHit?.Invoke(_camHoverHit);
								OnCamHoverUpdate?.Invoke(_camHoverHit);
								OnCamHoverOff?.Invoke(_camHoverHit);
							}
							// Else the hit is at a different position, but on the same object
							else
							{
								OnCamHoverUpdate?.Invoke(_camHoverHit);
							}
						}
					}
					else
					{

						if (_camHoverHit != Hit3D.EMPTY)
						{
							Hit3D previousHit = _camHoverHit;
							_camHoverHit = Hit3D.EMPTY;

							OnCamHoverOff?.Invoke(previousHit);
						}
					}
				}
			}
			else
			{
				GD.PrintErr("camera null");
			}
		}
	}

	public void SwitchToTopDown()
	{
		if (!top_down)
		{
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "position", top_down_position, transition_time);
			tween.TweenProperty(this, "rotation_degrees", top_down_rotation, transition_time);
			top_down = true;
		}
	}

	public void SwitchToPerspective()
	{
		if (top_down)
		{
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
			if (top_down)
			{
				SwitchToPerspective();
			}
			else
			{
				SwitchToTopDown();
			}
		}
	}
}
