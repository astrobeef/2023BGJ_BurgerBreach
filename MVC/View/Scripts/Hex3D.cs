using Godot;
using Model;
using System;
using Utility;

public partial class Hex3D : Node3D
{
	public AxialCS.Axial AxialPos;
	[Export]
	Vector3 _exportAxial{
		get{
			return new Vector3(AxialPos.Q, AxialPos.S, AxialPos.R);
		}
		set
		{
			AxialPos = new AxialCS.Axial((int)value.X, (int)value.Y);
		}
	}

	public Unit3D activeUnit3D => (GetChildCount() > 0) ? GetChild<Unit3D>(0) : null;
	public Unit activeUnitModel => (GetChildCount() > 0) ? GetChild<Unit3D>(0)?.unit : null;

	private StaticBody3D _body;
	private static string STATIC_BODY_NAME = "StaticBody3D";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		main.Instance.Player.OnCamHoverNewHit += OnCamHoverNewHit;
		main.Instance.Player.OnCamHoverOff += OnCamHoverOff;

		main.Instance.Player.OnCamClickNewHit += OnCamClickNewHit;
		main.Instance.Player.OnCamClickOff += OnCamClickOff;

		_body = FindChild(STATIC_BODY_NAME) as StaticBody3D;
		if (_body == null)
			GD.PrintErr($"Could not find {STATIC_BODY_NAME} on {this.Name}");
	}
	
	private Vector3 onHoverDisplace = new Vector3(0, 0.1f, 0.02f);
	
	private void OnCamHoverNewHit(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			if(activeUnit3D != null)
			{
				activeUnit3D.Position += onHoverDisplace;
			}
		}
	}

	private void OnCamHoverOff(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			if(activeUnit3D != null)
			{
				activeUnit3D.Position -= onHoverDisplace;
			}
		}
	}

	private void OnCamClickNewHit(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			if(main.Instance.Player.selectedCard3D != null)
			{
				main.Instance.gameModel
				.TryPlaceCard_FromHand(
					0,
					main.Instance.Player.selectedCard3D.card.id,
					AxialPos);
			}
		}
	}

	private void OnCamClickOff(Hit3D hit)
	{
		if(hit.collider == _body)
		{
		}
	}
}
