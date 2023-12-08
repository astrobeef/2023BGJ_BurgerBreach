using Godot;
using System;
using Utility;

public partial class player : Node3D
{
	[Export(PropertyHint.Layers3DPhysics)] public uint CameraHitLayers;
	[Export] private Camera3D camera;

	[Export] private Vector3 top_down_position = new Vector3(0, 1.3f, 0.00f);
	[Export] private Vector3 top_down_rotation = new Vector3(-90, 0, 0);
	[Export] private Vector3 perspective_position = new Vector3(0f, 0.9f, 0.7f);
	[Export] private Vector3 perspective_rotation = new Vector3(-35, 0, 0);
	[Export] private float transition_time = 0.4f;

	private bool top_down = false;

	private Hit3D _camHoverHit;
	public Hit3D CamHoverHit => _camHoverHit;

	private Hit3D _camClickHit;
	public Hit3D CamClickHit => _camClickHit;

	public Action<Hit3D> OnCamHoverNewHit;
	public Action<Hit3D> OnCamHoverOff;
	public Action<Hit3D> OnCamHoverUpdate;

	public Action<Hit3D> OnCamClickNewHit;
	public Action<Hit3D> OnCamClickOff;
	public Action<Hit3D> OnCamClickUpdate;

	public Action<Card3D> OnCardSelected;
	public Action OnCardDeselected;
	public Card3D selectedCard3D;

	public Action<Unit3D> OnUnitSelected;
	public Action OnUnitDeselected;
	public Unit3D selectedUnit3D;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		camera = GetChild(0) as Camera3D;

		main.Instance.gameModel.OnTurnEnd += OnTurnEnd;
		main.Instance.gameModel.OnTurnStart += OnTurnStart;
	}

	private void OnTurnStart(int turnCounter, int endTurnPlayerIndex)
	{
		if(endTurnPlayerIndex == 0)
		{
		OnCardSelected += FireOnCardSelected;
		OnCardDeselected += FireOnCardDeselected;

		OnUnitSelected += FireOnUnitSelected;
		OnUnitDeselected += FireOnUnitDeselected;
		}
	}

	private void OnTurnEnd(int turnCounter, int endTurnPlayerIndex)
	{
		if(endTurnPlayerIndex == 0)
		{

		OnCardSelected -= FireOnCardSelected;
		OnCardDeselected -= FireOnCardDeselected;

		OnUnitSelected -= FireOnUnitSelected;
		OnUnitDeselected -= FireOnUnitDeselected;
		}
	}

	private void FireOnCardSelected(Card3D card3D)
	{
		if (card3D != selectedCard3D)
		{
			SwitchToTopDown();
			selectedCard3D = card3D;
		}
	}

	private void FireOnCardDeselected()
	{
		var Async = async () =>
		{
			await System.Threading.Tasks.Task.Delay(5);
			// SwitchToPerspective();
			// selectedCard3D = null;
		};

		Async.Invoke();
	}

	private void FireOnUnitSelected(Unit3D unit)
	{
		selectedUnit3D = unit;
		SwitchToTopDown();
	}

	private void FireOnUnitDeselected()
	{
		// Delay in case any behavior needs to be done with the unit on deselect
		var Async = async () =>
		{
			await System.Threading.Tasks.Task.Delay(5);
			selectedUnit3D = null;
		};
	}



	bool _leftMouseClicked = false;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_leftMouseClicked)
			_leftMouseClicked = Input.IsMouseButtonPressed(MouseButton.Left);

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			if (!_leftMouseClicked)
			{
				_leftMouseClicked = true;

				if (Raycasting.RayCast(camera, CameraHitLayers, out Hit3D hit))
				{
						// If it is a new hit
						if (_camClickHit != hit)
						{
							Hit3D previousHit = _camClickHit;
							_camClickHit = hit;

							// If the colliders are NOT the same (meaning this hit has hit a new object)
							if (_camClickHit.collider != previousHit.collider)
							{
								OnCamClickOff?.Invoke(previousHit);

								OnCamClickNewHit?.Invoke(_camClickHit);
								OnCamClickUpdate?.Invoke(_camClickHit);
							}
							// Else the hit is at a different position, but on the same object
							else
							{
								OnCamClickUpdate?.Invoke(_camClickHit);
							}
						}
				}
					else
					{
						if (_camClickHit != Hit3D.EMPTY)
						{
							Hit3D previousHit = _camClickHit;
							_camClickHit = Hit3D.EMPTY;

							OnCamClickOff?.Invoke(previousHit);
						}
					}
			}
		}
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
								OnCamHoverOff?.Invoke(previousHit);

								OnCamHoverNewHit?.Invoke(_camHoverHit);
								OnCamHoverUpdate?.Invoke(_camHoverHit);
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
