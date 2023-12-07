using Godot;
using System;

public partial class board : Node3D
{
	[Export] private float tile_spacing = 0.5f;
	[Export] private uint board_size = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GenerateBoard();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void GenerateBoard()
	{
		CreateTileAtAxial(1, -3);
		CreateTileAtAxial(2, -3);

		for (int q = -1; q <= 3; q++) {
			CreateTileAtAxial(q, -2);
		}

		for (int q = -2; q <= 3; q++) {
			CreateTileAtAxial(q, -1);
		}

		for (int q = -2; q <= 2; q++) {
			CreateTileAtAxial(q, 0);
		}

		for (int q = -3; q <= 2; q++) {
			CreateTileAtAxial(q, 1);
		}

		for (int q = -3; q <= 1; q++) {
			CreateTileAtAxial(q, 2);
		}


		CreateTileAtAxial(-2, 3);
		CreateTileAtAxial(-1, 3);
	}

	public (float, float) GetCoordinatesFromAxial(int q, int r) {
		float x = 0, y = 0;

		x = q * tile_spacing;
		y = r * tile_spacing * 0.9f; // should be * 1.5 but idk why that makes it look worse
		x += r * tile_spacing * 0.5f;

		return (x, y);
	}

	public void CreateTileAtAxial(int q, int r) {
		var coordinates = GetCoordinatesFromAxial(q, r);
		var scene = GD.Load<PackedScene>("res://MVC/View/board_tile.tscn");
		Node3D instance = (Node3D) scene.Instantiate();
		instance.Position = new Vector3(coordinates.Item1, 0, coordinates.Item2);
		AddChild(instance);
	}
}
