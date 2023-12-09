using AxialCS;
using Godot;
using Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Utility;

public partial class Hex3D : Node3D
{
	public AxialCS.Axial AxialPos = AxialCS.Axial.Empty;
	[Export]
	Vector3 _exportAxial
	{
		get
		{
			return new Vector3(AxialPos.Q, AxialPos.S, AxialPos.R);
		}
		set
		{
			AxialPos = new AxialCS.Axial((int)value.X, (int)value.Y);
		}
	}

	private static string _COIN_SLOT_NAME = "coinSlot";

	public Unit3D activeUnit3D
	{
		get
		{
			if (FindChild(_COIN_SLOT_NAME) != null)
			{
				if (FindChild(_COIN_SLOT_NAME).GetChildCount() > 0)
				{
					if (FindChild(_COIN_SLOT_NAME).GetChild<Unit3D>(0) != null)
					{
						return FindChild(_COIN_SLOT_NAME).GetChild<Unit3D>(0);
					}
					else throw new Exception($"{_COIN_SLOT_NAME} has a child but it is not of type Unit3D");
				}
				else return null;
			}
			else throw new Exception($"{Name} is missing child {_COIN_SLOT_NAME}");
		}
	}

	public Unit activeUnitModel => activeUnit3D?.unit;

	private StaticBody3D _body;
	private static string STATIC_BODY_NAME = "StaticBody3D";

	private TextMesh AtkText3D, HpText3D, Axial3D;
	private static string _ATK_TEXT_NAME = "AtkText3D",
	_HP_TEXT_NAME = "HpText3D",
	_AXIAL_TEXT_NAME = "Axial3D";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		main.Instance.Player.OnCamHoverNewHit += OnCamHoverNewHit;
		main.Instance.Player.OnCamHoverOff += OnCamHoverOff;

		main.Instance.Player.OnCamClickNewHit += OnCamClickNewHit;
		main.Instance.Player.OnCamClickUpdate += OnCamClickNewHit;
		main.Instance.Player.OnCamClickOff += OnCamClickOff;

		main.Instance.gameModel.OnUnitDamaged += OnUnitDamaged;

		_body = FindChild(STATIC_BODY_NAME) as StaticBody3D;
		if (_body == null)
			GD.PrintErr($"Could not find {STATIC_BODY_NAME} on {this.Name}");

		AtkText3D = FindTextMeshChild(_ATK_TEXT_NAME);
		HpText3D = FindTextMeshChild(_HP_TEXT_NAME);
		Axial3D = FindTextMeshChild(_AXIAL_TEXT_NAME);
		Axial3D.Text = AxialPos.ToString();
		SetStatsText(false);
	}

	private Vector3 onHoverDisplace = new Vector3(0, 0.1f, 0.02f);
	bool isHoverDisplaced = false;

	private void OnCamHoverNewHit(Hit3D hit)
	{
		if (hit.collider == _body)
		{
			if (!isHoverDisplaced)
			{
				isHoverDisplaced = true;
				// activeUnit3D.Position += onHoverDisplace;
			}
		}
	}

	private void OnCamHoverOff(Hit3D hit)
	{
		if (hit.collider == _body)
		{
			if (activeUnit3D != null)
			{
				if (isHoverDisplaced)
				{
					isHoverDisplaced = false;
					// activeUnit3D.Position -= onHoverDisplace;
				}
			}
		}
	}

	private void OnCamClickNewHit(Hit3D hit)
	{
		if (hit.collider == _body)
		{
			if (main.Instance.Player.selectedCard3D == null && main.Instance.Player.selectedUnit3D == null)
				main.Instance.Player?.OnUnitSelected?.Invoke(activeUnit3D);

			// TRY TO PLACE A CARD ON THIS TILE
			// If this hex tile has been clicked on AND a card is selected
			if (main.Instance.Player.selectedCard3D != null)
			{
				if (!main.Instance.gameModel
				.TryPlaceCard_FromHand(
					0,
					main.Instance.Player.selectedCard3D.card.id,
					AxialPos))
				{
					main.Instance.SoundController.Play(sound_controller.SFX_CARD_PLACE_NAME);
					// GD.PrintErr("This should be done with an event like 'OnCardPlacmentFailed', but it works for now");
					// main.Instance.Player.SwitchToPerspective();
				}
				else
				{
					if (main.Instance.Player.selectedCard3D.card.TYPE != Card.CardType.Offense)
					{
						GD.PrintErr("This should be done with an event like 'OnUnitAdded', but it works for now");
						main.Instance.Player.SwitchToPerspective();
					}
				}

				return;
			}

			// TRY TO MOVE TO THIS TILE
			// Else a card is NOT selected, if a unit is selected AND this hex has no unit
			if (main.Instance.Player.selectedUnit3D != null		// A unit is selected
			 && activeUnit3D == null									// This tile has no unit
			 )
			{
				Unit unitToMove = main.Instance.Player.selectedUnit3D.unit;
				Axial unitToMove_Pos = unitToMove.pos;
				if(main.Instance.gameModel.Unit_TryMove(true, unitToMove.ownerIndex, unitToMove_Pos, AxialPos))
				{
					main.Instance.SoundController.Play(sound_controller.SFX_CARD_MOVE_NAME);
				}
				
				return;
			}
			else
			{
				GD.PrintErr($"Not trying to move unit because either a unit is NOT selected ({main.Instance.Player.selectedUnit3D == null}) OR this hex has a unit in it ({activeUnit3D != null})");
			}

			// TRY TO ATTACK THE UNIT ON THIS TILE
			// Else a card is NOT selected, if a unit is selected AND this hex has a unit AND they are not the same unit
			if (main.Instance.Player.selectedUnit3D != null			// A unit is selected
			 && activeUnit3D != null										// This tile has a unit
			 && main.Instance.Player.selectedUnit3D != activeUnit3D			// This tile is NOT the selected unit
			 )
			{
				Unit attackerUnit = main.Instance.Player.selectedUnit3D.unit;
				Axial attackDirection = (AxialPos - attackerUnit.pos);
				if(main.Instance.gameModel.Unit_TryAttack(true, attackerUnit, attackDirection, activeUnit3D.unit))
				{
					main.Instance.SoundController.Play(sound_controller.SFX_PLAYER_ATTACK_NAME);
				}
				return;
			}
			else
			{
				GD.PrintErr($"Not trying to attack with this unit because either a unit is NOT selected ({main.Instance.Player.selectedUnit3D == null}) OR this hex does NOT have a unit ({activeUnit3D == null}) OR this hex is the same as the attacker ({main.Instance.Player.selectedUnit3D == activeUnit3D})");
			}
			
			main.Instance.Player?.OnUnitSelected?.Invoke(activeUnit3D);
		}
	}

	private void OnCamClickOff(Hit3D hit)
	{
		if (hit.collider == _body)
		{
			GD.PrintErr($"{activeUnit3D.unit.name} is invoking OnCamClickOff because the hit is this");
			main.Instance.Player.OnUnitDeselected?.Invoke(activeUnit3D);
		}
	}

	private void OnUnitDamaged(Unit unitDamaged)
	{
		if (activeUnitModel != null)
		{
			if (unitDamaged.name == activeUnitModel?.name)
			{
				SetStatsText(false);
			}
		}
	}

	public void SetStatsText(bool reset)
	{
		if (!reset && activeUnitModel != null)
		{
			AtkText3D.Text = activeUnitModel.atk.ToString();
			HpText3D.Text = activeUnitModel.hp.ToString();
		}
		else
		{
			AtkText3D.Text = "";
			HpText3D.Text = "";
		}
	}

	private TextMesh FindTextMeshChild(string name)
	{
		MeshInstance3D mesh3D = FindChild(name) as MeshInstance3D;
		if (mesh3D != null && mesh3D.Mesh is TextMesh originalTextMesh)
		{
			TextMesh newTextMesh = originalTextMesh.Duplicate() as TextMesh;
			mesh3D.Mesh = newTextMesh;
			return newTextMesh;
		}
		else if (mesh3D == null)
		{
			GD.PrintErr($"Child not found: {name}");
		}
		else
		{
			GD.PrintErr($"Mesh3D {mesh3D.Name} is not of type TextMesh");
		}

		return null;
	}

}
