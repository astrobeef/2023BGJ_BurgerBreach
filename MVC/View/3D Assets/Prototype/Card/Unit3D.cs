using Godot;
using Model;
using System;

public partial class Unit3D : Node3D
{
	[Export]
	public Unit unit;

	private StaticBody3D _body;
	private static string STATIC_BODY_NAME = "StaticBody3D";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
}
