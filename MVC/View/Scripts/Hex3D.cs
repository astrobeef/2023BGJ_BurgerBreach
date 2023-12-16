using AxialCS;
using Godot;
using Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Utility;

public partial class Hex3D : Node3D
{
	[Export] Material Unselected;
	[Export] Material Selected;
	[Export] Material Selectable;
	[Export] Material Movable;
	[Export] Material Attackable;

	[Export] AnimationPlayer animationPlayer;

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

	private TextMesh AtkText3D, HpText3D, Axial3D;
	private static string _ATK_TEXT_NAME = "AtkText3D",
	_HP_TEXT_NAME = "HpText3D",
	_AXIAL_TEXT_NAME = "Axial3D";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AtkText3D = FindTextMeshChild(_ATK_TEXT_NAME);
		HpText3D = FindTextMeshChild(_HP_TEXT_NAME);

		SetStatsText(false);
	}

	private Vector3 onHoverDisplace = new Vector3(0, 0.1f, 0.02f);
	bool isHoverDisplaced = false;

	public void SetStatsText(bool reset)
	{
		if (!reset && activeUnitModel != null)
		{
			if (AtkText3D.Text == "" && HpText3D.Text == "") animationPlayer.Play("ShowValues");

			AtkText3D.Text = activeUnitModel.atk.ToString();
			HpText3D.Text = activeUnitModel.hp.ToString();
		}
		else
		{
			AtkText3D.Text = "";
			HpText3D.Text = "";
			
			animationPlayer.Play("HideValues");
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

	

	public bool OnObjectSelected()
	{
		// Do anything when this hex is selected
		return true;
	}

	public bool OnObjectDeselected()
	{
		// Do anything when this hex is deselected
		return true;
	}

	public enum IndicatorState {Selected, Selectable, Movable, Attackable, Disabled};
	
	public void SetIndicator(IndicatorState indi) {
		MeshInstance3D mesh = GetNode("Indicator/Mesh") as MeshInstance3D;

		switch(indi) {
			case IndicatorState.Selected:
				mesh.SetSurfaceOverrideMaterial(0, Selected);
				break;

			case IndicatorState.Selectable:
				mesh.SetSurfaceOverrideMaterial(0, Selectable);
				break;

			case IndicatorState.Movable:
				mesh.SetSurfaceOverrideMaterial(0, Movable);
				break;

			case IndicatorState.Attackable:
				mesh.SetSurfaceOverrideMaterial(0, Attackable);
				break;

			case IndicatorState.Disabled:
				mesh.SetSurfaceOverrideMaterial(0, Unselected);
				break;
		}
	}

}
