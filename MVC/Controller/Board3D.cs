using AxialCS;
using Godot;
using Model;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using Utility;

public partial class Board3D : Node3D
{
	private Model_Game gameModel;

	[Export] private float _side_length = 0.985f;

	private Vector3 _offset;
	private float Y_Offset = 0.5f;
	private Vector2 _offset_2D => new Vector2(_offset.X, _offset.Z);

	private static string COIN_BURGER = "res://MVC/View/3D Assets/Prototype/Coin/coin_burger.tscn";
	private static string COIN_BASE = "res://MVC/View/3D Assets/Prototype/Coin/coin_base.tscn";
	private static string COIN_MOE = "res://MVC/View/3D Assets/Prototype/Coin/coin_moe.tscn";


	private Hex3D[] _Board3D;
	private List<Unit3D> _ActiveUnit3Ds = new List<Unit3D>();


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_offset = this.Position + new Vector3(0, Y_Offset, 0);

		AwaitReady();
	}

	private async void AwaitReady()
	{
		while (main.Instance?.gameModel?.Board.Axials == null || main.Instance?.gameModel?.Board.Axials?.Length == 0)
		{
			await System.Threading.Tasks.Task.Delay(5);
		}

		gameModel = main.Instance.gameModel;

		gameModel.OnUnitAddedToBoard += OnUnitAddedToBoard;
		gameModel.OnUnitAttack += OnUnitAttack;
		gameModel.OnUnitMove += OnUnitMove;
		gameModel.OnUnitDeath += OnUnitDeath;

		GenerateBoard3D(main.Instance?.gameModel?.Board.Axials);
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
			PackedScene scene = null;
			switch (newUnit.card.NAME)
			{
				case (Card.BASE_TEST_NAME):
					scene = GD.Load<PackedScene>(COIN_BASE);
					break;
				case (Card.RESOURCE_TEST_NAME):
					scene = GD.Load<PackedScene>(COIN_BURGER);
					break;
				case (Card.OFFENSE_TEST_NAME):
					scene = GD.Load<PackedScene>(COIN_MOE);
					break;
			}
			if (scene != null)
			{
				CreateUnit3D(scene, hex3DtoAddTo, newUnit);
			}
			else GD.PrintErr($"scene is null");
		}
		else
		{
			GD.PrintErr($"Could not find tile for unit position {newUnit.pos}");
		}
	}

	[Export]
	private Texture2D coinInside_Friendly, coinInside_Enemy;
	private const int coinInsideSurfaceIndex = 0;

	private void CreateUnit3D(PackedScene scene, Hex3D parentHex, Unit unitModel)
	{
		Unit3D unit3D = (Unit3D)scene.Instantiate().Duplicate(); //Try duplicating?
		unit3D.unit = unitModel;

		MeshInstance3D meshInstance = unit3D.GetChild<MeshInstance3D>(coinInsideSurfaceIndex);
		var material = meshInstance.GetActiveMaterial(coinInsideSurfaceIndex) as StandardMaterial3D;
		if (material != null)
		{
			material = material.Duplicate() as StandardMaterial3D;

			if (unitModel.ownerIndex == 0)
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

		parentHex.GetChild(0).AddChild(unit3D);
		parentHex.SetStatsText(false);
		_ActiveUnit3Ds.Add(unit3D);
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
				_ActiveUnit3Ds.Remove(unit3D);
				parentHex.SetStatsText(true);
			}
			else GD.PrintErr($"${parentHex.Name} missing coin slot");
		}
		else GD.PrintErr($"This was called on a hex, {parentHex.Name}, with no active unit3D");
	}

	private void MoveUnit3D(Unit3D unit3D, Hex3D originHex3D, Hex3D destinationHex3D)
	{
		if (originHex3D.GetChild(0) != null)
		{
			originHex3D.GetChild(0).RemoveChild(unit3D);
			originHex3D.SetStatsText(true);

			if (destinationHex3D.GetChild(0) != null)
			{

				PackedScene scene = null;
				switch (unit3D.unit.card.NAME)
				{
					case (Card.BASE_TEST_NAME):
						scene = GD.Load<PackedScene>(COIN_BASE);
						break;
					case (Card.RESOURCE_TEST_NAME):
						scene = GD.Load<PackedScene>(COIN_BURGER);
						break;
					case (Card.OFFENSE_TEST_NAME):
						scene = GD.Load<PackedScene>(COIN_MOE);
						break;
				}
				if (scene != null)
				{
					CreateUnit3D(scene, destinationHex3D, unit3D.unit);
				}
				else GD.PrintErr($"scene is null");

				DestroyUnit3D(originHex3D);
			}
			else GD.PrintErr($"${destinationHex3D.Name} missing coin slot");
		}
		else GD.PrintErr($"${originHex3D.Name} missing coin slot");
	}

	private void OnUnitAttack(Unit attacker, Unit target)
	{
		// Play any VFX/SFX
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
					MoveUnit3D(oldHex3D.activeUnit3D, oldHex3D, destHex3D);
				}
				else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that tile is already occupied by {destHex3D.activeUnit3D.unit.name}. This must be an View error since this case should have been checked in the Model");
			}
			else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that tile does not exist on the board. This must be a View error since this case should have been checked in the Model");
		}
		else GD.PrintErr($"{movedUnit} attempting to move to axial {newPosition}, but that unit does not exist on Board3D. This must be a View error since this case should have been checked in the Model");
	}

	private void OnUnitDeath(Unit unit)
	{
		if (IsUnitModelOnBoard3D(unit, out Hex3D hex3D))
		{
			DestroyUnit3D(hex3D);
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


	private void GenerateBoard3D(Axial[] axials)
	{
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

			_Board3D[i] = boardTile3D;
			AddChild(boardTile3D);
		}
	}
}
