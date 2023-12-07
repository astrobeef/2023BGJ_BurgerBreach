using Godot;
using System;

public partial class board_tile : Node3D
{
	[Export] Material standard;
	[Export] Material highlighted;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<MeshInstance3D>("TileMesh").MaterialOverlay = standard;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	
	private void _on_static_body_3d_mouse_entered()
	{
		GetNode<MeshInstance3D>("TileMesh").MaterialOverlay = highlighted;
	}


	private void _on_static_body_3d_mouse_exited()
	{
		GetNode<MeshInstance3D>("TileMesh").MaterialOverlay = standard;
	}

}
