using AxialCS;
using Godot;
using Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using Utility;

public partial class Board3D : Node3D
{
	private Model_Game gameModel;

	[Export] public static float _side_length = 0.985f;

	private static Vector3 _offset;
	public static Vector3 Offset => _offset;

	private float Y_Offset = 0.5f;
	private static Vector2 _offset_2D => new Vector2(_offset.X, _offset.Z);

	private static string COIN_BASE = "res://MVC/View/3D Assets/Prototype/Coin/coin_base.tscn";
	private static string COIN_MOE = "res://MVC/View/3D Assets/Final/coins/coin_moe";
	private static string COIN_BURGER = "res://MVC/View/3D Assets/Final/plates/plate_burger";
	private static string COIN_BUSSER_RACOON = "res://MVC/View/3D Assets/Final/coins/coin_racoon";
	private static string COIN_CLAM_CHOWDER = "res://MVC/View/3D Assets/Final/plates/plate_chowder";
	private static string COIN_EXPO_PIGEON = "res://MVC/View/3D Assets/Final/coins/coin_pigeon";
	private static string COIN_LINE_SQUIRREL = "res://MVC/View/3D Assets/Final/coins/coin_squirrel";
	private static string COIN_MOE_FAMILY_FRIES = "res://MVC/View/3D Assets/Final/plates/plate_fries";
	private static string COIN_THE_SCRAPS = "res://MVC/View/3D Assets/Final/plates/plate_scraps";

	private static string ALLY_SUFFIX = "_ally.tscn";
	private static string ENEMY_SUFFIX = "_enemy.tscn";

	
	private StaticBody3D _body, _body_offBoard;
	private static string STATIC_BODY_NAME = "StaticBody3D-Board",
	STATIC_BODY_NAME_OFFBOARD = "StaticBody3D-OffBoard";

	private Hex3D[] _Board3D;
	private List<Unit3D> _ActiveUnit3Ds = new List<Unit3D>();


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AwaitReady();
	}

	private async void AwaitReady()
	{
		while (main.Instance?.gameModel?.Board.Axials == null || main.Instance?.gameModel?.Board.Axials?.Length == 0)
		{
			await System.Threading.Tasks.Task.Delay(5);
		}

		gameModel = main.Instance.gameModel;
		
		_body = FindChild(STATIC_BODY_NAME) as StaticBody3D;
		if (_body == null)
			GD.PrintErr($"Could not find {STATIC_BODY_NAME} on {this.Name}");
		
		_body_offBoard = FindChild(STATIC_BODY_NAME_OFFBOARD) as StaticBody3D;
		if (_body_offBoard == null)
			GD.PrintErr($"Could not find {STATIC_BODY_NAME_OFFBOARD} on {this.Name}");


		main.Instance.Player.OnCamHoverNewHit += OnCamHoverNewHit;
		main.Instance.Player.OnCamHoverOff += OnCamHoverOff;

		main.Instance.Player.OnCamClickNewHit += OnCamClickNewHit;
		main.Instance.Player.OnCamClickUpdate += OnCamClickUpdate;
		main.Instance.Player.OnCamClickOff += OnCamClickOff;

		gameModel.OnUnitAddedToBoard += OnUnitAddedToBoard;
		gameModel.OnUnitAttack += OnUnitAttack;
		gameModel.OnUnitDamaged += OnUnitDamaged;
		gameModel.OnUnitBuffed += OnUnitBuffed;
		gameModel.OnUnitMove += OnUnitMove;
		gameModel.OnUnitDeath += OnUnitDeath;
		gameModel.OnUnitOwnerChanged += OnUnitOwnerChanged;

		main.Instance.Player.OnObjectSelected += OnObjectSelected;
		main.Instance.Player.OnObjectDeselected += OnObjectDeselected;
		main.Instance.Player.OnPlayerAttackMode += OnPlayerAttackMode;

		GenerateBoard3D(main.Instance?.gameModel?.Board.Axials);
	}

	private void OnObjectSelected(Node3D node3D)
	{
		HandleIndication(node3D);
	}

	private void OnObjectDeselected(Node3D node3D) {
		IndicateAllHexes(Hex3D.IndicatorState.Disabled);
	}

	private void OnPlayerAttackMode(bool isEntering)
	{
		Unit3D selectedUnit3D = main.Instance.Player.selectedObject as Unit3D;

		if (selectedUnit3D != null && selectedUnit3D.unit.ownerIndex == 0)
		{
			IndicateUnitActions(selectedUnit3D, isEntering);
		}
	}

	private void OnCamHoverNewHit(Hit3D hit)
	{
		if (IsHitOnBoard(hit, out Hex3D hitHex3D))
		{
			hitHex3D.activeUnit3D?.OnHover();
		}
	}

	private void OnCamHoverOff(Hit3D hitOff)
	{
		if(IsHitOnBoard(hitOff, out Hex3D hitHex3D))
		{
			hitHex3D.activeUnit3D?.OnHoverOff();
		}
	}

	private void OnCamClickNewHit(Hit3D hit)
	{
		if (IsHitOnBoard(hit, out Hex3D hitHex3D))
		{
			if (hitHex3D.activeUnit3D != null)
			{
				if (main.Instance.Player.HandleObjectClicked(hitHex3D.activeUnit3D))
					hitHex3D.activeUnit3D.OnClicked();

			}
			else
			{
				main.Instance.Player.HandleObjectClicked(hitHex3D);
			}
		}
		else if(hit.collider == _body_offBoard)
		{
			GD.Print($"Board3D is firing HandleObjectClicked for 'null' because a click was fired off board");
			main.Instance.Player.HandleObjectClicked(null);
		}
	}

	private void OnCamClickUpdate(Hit3D hit)
	{
		OnCamClickNewHit(hit);
	}

	private void OnCamClickOff(Hit3D hitOff)
	{
		if(IsHitOnBoard(hitOff, out Hex3D hitHex3D))
		{
			if(hitHex3D.activeUnit3D != null)
			{
				if(main.Instance.Player.HandleObjectClickOff(hitHex3D.activeUnit3D))
					hitHex3D.activeUnit3D.OnClickOff();
			}
			else
			{
				main.Instance.Player.HandleObjectClicked(hitHex3D);
			}
		}
	}



	private void OnUnitAddedToBoard(Unit newUnit)
	{
		// Add unit instance to relevant board placeholder Node3D
		Hex3D hex3DtoAddTo = null;
		foreach (Hex3D tile in _Board3D)
		{
			if (tile.AxialPos == newUnit.pos)
			{
				hex3DtoAddTo = tile;
				break;
			}
		}

		if (hex3DtoAddTo != null)
		{
				CreateUnit3D(hex3DtoAddTo, newUnit);
		}
		else
		{
			GD.PrintErr($"Could not find tile for unit position {newUnit.pos}");
		}
	}

	[Export]
	private Texture2D coinInside_Friendly, coinInside_Enemy;
	private const int coinInsideSurfaceIndex = 0;

	private Unit3D CreateUnit3D(Hex3D parentHex, Unit unitModel)
	{
			PackedScene scene = null;
		switch (unitModel.card.NAME)
		{
			case Card.BASE_NAME:
				scene = GD.Load<PackedScene>(COIN_BASE);
				break;
			case Card.BIG_MOE_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_MOE + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_MOE + ENEMY_SUFFIX);
				break;
			case Card.BURGER_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_BURGER + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_BURGER + ENEMY_SUFFIX);
				break;
			case Card.BUSSER_RACOON_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_BUSSER_RACOON + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_BUSSER_RACOON + ENEMY_SUFFIX);
				break;
			case Card.CLAM_CHOWDER_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_CLAM_CHOWDER + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_CLAM_CHOWDER + ENEMY_SUFFIX);
				break;
			case Card.EXPO_PIGEON_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_EXPO_PIGEON + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_EXPO_PIGEON + ENEMY_SUFFIX);
				break;
			case Card.LINE_SQUIRREL_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_LINE_SQUIRREL + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_LINE_SQUIRREL + ENEMY_SUFFIX);
				break;
			case Card.MOE_FAMILY_FRIES_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_MOE_FAMILY_FRIES + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_MOE_FAMILY_FRIES + ENEMY_SUFFIX);
				break;
			case Card.THE_SCRAPS_NAME:
				if (unitModel.ownerIndex == 0)
					scene = GD.Load<PackedScene>(COIN_THE_SCRAPS + ALLY_SUFFIX);
				else
					scene = GD.Load<PackedScene>(COIN_THE_SCRAPS + ENEMY_SUFFIX);
				break;
			default:
				GD.PrintErr($"Uncaught case for card name \"{unitModel.card.NAME}\"");
				return null;
		}

		Unit3D unit3D = (Unit3D)scene.Instantiate().Duplicate();
		unit3D.unit = unitModel;

		SetUnitMeshMaterialBasedOnOwnerIndex(unit3D);

		parentHex.GetChild(0).AddChild(unit3D);
		parentHex.SetStatsText(false);
		_ActiveUnit3Ds.Add(unit3D);

		unit3D.OnUnitCreated();
		return unit3D;
	}

	private void OnUnitOwnerChanged(Unit unit)
	{
		if(IsUnitModelOnBoard3D(unit, out Hex3D hex3D))
		{
			Unit3D unit3D = hex3D.activeUnit3D;
			// SetUnitMeshMaterialBasedOnOwnerIndex(unit3D);
		}
	}

	private void SetUnitMeshMaterialBasedOnOwnerIndex(Unit3D unit3D)
	{
		MeshInstance3D meshInstance = unit3D.GetChild<MeshInstance3D>(coinInsideSurfaceIndex);
		var material = meshInstance.GetActiveMaterial(coinInsideSurfaceIndex) as StandardMaterial3D;
		if (material != null)
		{
			material = material.Duplicate() as StandardMaterial3D;

			if (unit3D.unit.ownerIndex == 0)
			{
				material.AlbedoTexture = coinInside_Friendly;
			}
			else
			{
				material.AlbedoTexture = coinInside_Enemy;
			}

			meshInstance.SetSurfaceOverrideMaterial(coinInsideSurfaceIndex, material);
		}
		else GD.PrintErr("material is null");
	}

	private async void DestroyUnit3D(Hex3D parentHex)
	{
		await System.Threading.Tasks.Task.Delay(250);

		if (parentHex.activeUnit3D != null)
		{
			if (parentHex.GetChild(0) != null)
			{
				Unit3D unit3D = parentHex.activeUnit3D;
				parentHex.GetChild(0).RemoveChild(unit3D);
				unit3D.QueueFree();
				_ActiveUnit3Ds.Remove(unit3D);
				parentHex.SetStatsText(true);
			}
			else GD.PrintErr($"${parentHex.Name} missing coin slot");
		}
		else GD.PrintErr($"Attempted to destroy hex, {parentHex.Name}, but it has no active unit3D");
	}

	private void OnUnitAttack(Unit attacker, Unit target)
	{
		if(IsUnitModelOnBoard3D(attacker, out Hex3D attacker3D))
		{
			if(IsUnitModelOnBoard3D(target, out Hex3D target3D))
			{
				attacker3D.activeUnit3D.OnUnitAttack(target3D.activeUnit3D);
			}
		}
	}

	private void OnUnitDamaged(Unit attacker, Unit target)
	{
		if (IsUnitModelOnBoard3D(target, out Hex3D target3D))
		{
			target3D.activeUnit3D.OnUnitDamaged();
			target3D.SetStatsText(false);
		}
	}

	private void OnUnitBuffed(Unit unit)
	{
		if (IsUnitModelOnBoard3D(unit, out Hex3D unit3D))
		{
			unit3D.activeUnit3D.OnUnitBuffed();
			unit3D.SetStatsText(false);
		}
	}

	private void OnUnitMove(Axial oldPosition, Unit movedUnit)
	{
		Axial newPosition = movedUnit.pos;
		// Move 3D unit towards destination then change parent placeholder Node3D. If that's too difficult, destroy and create.
		if (IsUnitModelOnBoard3D(movedUnit, out Hex3D oldHex3D))
		{
			if (IsAxialOnBoard3D(newPosition, out Hex3D destHex3D))
			{
				if (destHex3D.activeUnit3D == null)
				{
					MoveHexParents(oldHex3D.activeUnit3D, oldHex3D, destHex3D);

					if(movedUnit.ownerIndex == 0) HandleIndication(destHex3D.activeUnit3D);
				}
				else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that tile is already occupied by {destHex3D.activeUnit3D.unit.name}. This must be an View error since this case should have been checked in the Model");
			}
			else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that tile does not exist on the board. This must be a View error since this case should have been checked in the Model");
		}
		else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that unit does not exist on Board3D. This must be a View error since this case should have been checked in the Model");
	}

	private void MoveHexParents(Unit3D unit3D, Hex3D originHex3D, Hex3D destinationHex3D)
	{
		if (destinationHex3D.GetChild(0) != null)
		{
			Node3D destinationSlot = destinationHex3D.GetChild(0) as Node3D;

			Transform3D globalTransform = unit3D.GlobalTransform;

			unit3D.GetParent().RemoveChild(unit3D);
			destinationSlot.AddChild(unit3D);

			unit3D.GlobalTransform = globalTransform;

			unit3D.OnUnitMove(destinationSlot.Position);

			originHex3D.SetStatsText(true);
			destinationHex3D.SetStatsText(false);
		}
		else GD.PrintErr($"${destinationHex3D.Name} missing coin slot");
	}

	private void OnUnitDeath(Unit unit)
	{
		if (IsUnitModelOnBoard3D(unit, out Hex3D hex3D))
		{
			// Do any death animations here
			DestroyUnit3D(hex3D);
			IndicateAllHexes(Hex3D.IndicatorState.Disabled);
		}
	}

	private bool IsUnitModelOnBoard3D(Unit unit, out Hex3D hex3D)
	{
		foreach (Hex3D iHex in _Board3D)
		{
			if (iHex.activeUnit3D != null)
			{
				if (iHex.activeUnit3D.unit == unit)
				{
					hex3D = iHex;
					return true;
				}
			}
		}

		hex3D = null;
		return false;
	}

	private bool IsAxialOnBoard3D(Axial ax, out Hex3D hex3D)
	{
		foreach (Hex3D iHex in _Board3D)
		{
			if (iHex.AxialPos == ax)
			{
				hex3D = iHex;
				return true;
			}
		}

		hex3D = null;
		return false;
	}

	private bool IsHitOnBoard(Hit3D hit, out Hex3D hitHex3D)
	{
		Axial axPos = Axial.V3ToAx(Board3D.Offset, Board3D._side_length, hit.position);

		if(IsAxialOnBoard3D(axPos, out hitHex3D))
		{
			return true;
		}
		return false;
	}


	private void GenerateBoard3D(Axial[] axials)
	{
		_offset = this.Position + new Vector3(0, Y_Offset, 0);
		_Board3D = new Hex3D[axials.Length];

		for (int i = 0; i < axials.Length; i++)
		{
			Axial ax = axials[i];
			Vector3 pos = Axial.AxToV3(_offset, _side_length, ax);

			PackedScene scene = GD.Load<PackedScene>("res://MVC/View/board_tile.tscn");
			Hex3D boardTile3D = (Hex3D)scene.Instantiate();
			boardTile3D.Name = $"Hex@{ax.ToString()}";
			boardTile3D.AxialPos = ax;
			boardTile3D.Position = pos;
			boardTile3D.SetIndicator(Hex3D.IndicatorState.Disabled);

			_Board3D[i] = boardTile3D;
			AddChild(boardTile3D);
		}

		if(IsAxialOnBoard3D(Axial.Zero, out Hex3D hexZero))
		{
			if(IsAxialOnBoard3D(Axial.Direction(Axial.Cardinal.E), out Hex3D hexEast))
			{
				_side_length = (hexZero.GlobalPosition - hexEast.GlobalPosition).Length() / (float)Axial._SQ3;
				_offset = this.GlobalPosition;
			}
		}
	}

	#region  Indication

	private void HandleIndication(Node3D node3D)
	{
		switch (node3D)
		{
			case Card3D card3D:
				{
					switch (card3D.card.TYPE)
					{
						case Card.CardType.Resource:
							{
								IndicateResourcePlacements();
								break;
							}

						case Card.CardType.Offense:
							{
								IndicateOffensePlacements(card3D);
								break;
							}
					}

					break;
				}

			case Unit3D unit3D:
				{
					IndicateUnitActions(unit3D, false);
					break;
				}

			case Hex3D hex3D:
				{
					break;
				}
				
			case null:
				{
					IndicateAllHexes(Hex3D.IndicatorState.Disabled);
					break;
				}
		}
	}

	private void IndicateAllHexes(Hex3D.IndicatorState indi) {
		foreach (Hex3D hex in _Board3D) {
			hex.SetIndicator(indi);
		}
	}

	private void IndicateHexes(Axial[] axials, Hex3D.IndicatorState indi) {
		for (int i = 0; i < axials.Length; i++) {
			try {
				Hex3D hex = new Hex3D();

				if (IsAxialOnBoard3D(axials[i], out hex)) {
					hex.SetIndicator(indi);
				}

			} catch (Exception e) {
				return;
			}
		}
	}

	private void IndicateResourcePlacements()
	{
		IndicateAllHexes(Hex3D.IndicatorState.Disabled);

		Axial[] validPlacements = new Axial[_Board3D.Length];
		main.Instance.gameModel.GetAllOpenResourcePlacements(0, out validPlacements);
		IndicateHexes(validPlacements, Hex3D.IndicatorState.Selectable);
	}

	private void IndicateOffensePlacements(Card3D card3D)
	{
		IndicateAllHexes(Hex3D.IndicatorState.Disabled);

		Dictionary<Axial, Unit> validPlacementDictionary = new Dictionary<Axial, Unit>();
		main.Instance.gameModel.GetAllValidOffensePlacements(0, card3D.card, out validPlacementDictionary);

		Axial[] placementArray = new Axial[_Board3D.Length];
		int i = 0;

		foreach (KeyValuePair<Axial, Unit> placement in validPlacementDictionary)
		{
			placementArray[i] = placement.Key;
			i++;
		}

		IndicateHexes(placementArray, Hex3D.IndicatorState.Selectable);
	}

	private void IndicateUnitActions(Unit3D unit3D, bool isAttacking)
	{
		IndicateAllHexes(Hex3D.IndicatorState.Disabled);

		var pos = new Axial[1];
		pos[0] = unit3D.unit.pos;
		IndicateHexes(pos, Hex3D.IndicatorState.Selected);

		if (isAttacking)
		{
			var attackUnits = new Unit[32];
			unit3D.unit.GetAllAttackTargets(out attackUnits);
			var attackPositions = new Axial[32];
			int i = 0;
			foreach (Unit unit in attackUnits)
			{
				attackPositions[i] = unit.pos;
				i++;
			}

			IndicateHexes(attackPositions, Hex3D.IndicatorState.Attackable);
		}
		else
		{
			var movePositions = new Axial[32];
			unit3D.unit.GetAllMovePositions(out movePositions);
			IndicateHexes(movePositions, Hex3D.IndicatorState.Movable);
		}
	}

	#endregion
}
