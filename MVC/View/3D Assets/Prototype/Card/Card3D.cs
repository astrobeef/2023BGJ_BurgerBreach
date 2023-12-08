using Godot;
using Model;
using System;
using System.Diagnostics;
using Utility;

public partial class Card3D : Node3D
{
	public Card card;

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

	private Vector3 _onHoverDisplace = new Vector3(0, 0.1f, 0.02f);
	private bool _isOnHoverDisplaced = false;

	private void OnCamHoverNewHit(Hit3D hit)
	{
		if (hit.collider == _body)
		{
			if (!_isOnHoverDisplaced)
			{
				_isOnHoverDisplaced = true;
				Position += _onHoverDisplace;
			}
		}
	}

	private void OnCamHoverOff(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			if (_isOnHoverDisplaced)
			{
				_isOnHoverDisplaced = false;
				Position -= _onHoverDisplace;
			}
		}
	}

	private void OnCamClickNewHit(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			main.Instance.Player.OnCardSelected?.Invoke(this);
		}
	}

	private void OnCamClickOff(Hit3D hit)
	{
		if(hit.collider == _body)
		{
			main.Instance.Player.OnCardDeselected?.Invoke();
		}
	}
}