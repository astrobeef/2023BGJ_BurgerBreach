using Godot;
using System;

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

	public Unit3D activeUnit => (GetChildCount() > 0) ? GetChild<Unit3D>(0) : null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
}
