using AxialCS;
using Godot;
using Model;
using System;
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


	private Unit3D[] _ActiveUnit3Ds;
	

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
			PackedScene scene = GD.Load<PackedScene>(COIN_BASE);
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

	private void CreateUnit3D(PackedScene scene, Hex3D parentHex, Unit unitModel)
	{
		Unit3D unit3D = (Unit3D)scene.Instantiate();
		unit3D.unit = unitModel;
		parentHex.GetChild(0).AddChild(unit3D);
	}

	private void OnUnitAttack(Unit attacker, Unit target)
	{
		// Play any VFX/SFX
	}

	private void OnUnitMove(Axial destination, Unit unit)
	{
		// Move 3D unit towards destination then change parent placeholder Node3D. If that's too difficult, destroy and create.
	}

	private void OnUnitDeath(Unit unit)
	{
		// Remove unit from board
	}


	private Hex3D[] _Board3D;

	private void GenerateBoard3D(Axial[] axials)
	{
		_Board3D = new Hex3D[axials.Length];

		for (int i = 0; i < axials.Length; i++)
		{
			Axial ax = axials[i];
			Vector3 pos = Axial.AxToV3(_offset, _side_length, ax);

			PackedScene scene = GD.Load<PackedScene>("res://MVC/View/board_tile.tscn");
			Hex3D boardTile3D = (Hex3D)scene.Instantiate();
			boardTile3D.AxialPos = ax;
			boardTile3D.Position = pos;

			_Board3D[i] = boardTile3D;
			AddChild(boardTile3D);
		}
	}
}
