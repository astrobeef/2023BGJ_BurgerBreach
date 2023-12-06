using AxialCS;
using Godot;
using System;
using System.Collections.Generic;

public partial class HexagonRenderer : Node2D
{
	//---VARIABLES---
	private Dictionary<Axial, HexagonDraw> _hexAxialDrawDictionary = new Dictionary<Axial, HexagonDraw>();
	public Dictionary<Axial, HexagonDraw> HexAxialDrawDictionary => _hexAxialDrawDictionary;
	private bool _triggerRedraw = false;

	//---LIFECYCLE METHODS---
	public override void _Ready()
	{
		GD.Print("DrawHexagon2D script running");
	}
	
	public override void _Draw()
	{
			GD.Print("Draw call fired");

		foreach(KeyValuePair<Axial, HexagonDraw> hexAxial in _hexAxialDrawDictionary){
			HexagonDraw hex = hexAxial.Value;

			DrawPolygon(hex.Vertices, hex.Colors);
		}
	}

	public override void _Process(double delta)
	{
		if(_triggerRedraw){
			_triggerRedraw = false;
			QueueRedraw();
		}
	}

	//---DICTIONARY METHODS---

	private readonly object _hexAxialLock = new object();

	public bool AddHex(Axial ax, HexagonDraw hex)
	{
		lock (_hexAxialLock)
		{
			try
			{
				_hexAxialDrawDictionary.Add(ax, hex);
				GD.Print($"Successfully added {ax} with hex {hex}. Triggering redraw");

				_triggerRedraw = true;

				return true;
			}
			catch
			{
				GD.PrintErr($"Failed to add hex {hex}. It is likely that a hexagon already exists, in the dictionary, at the axial coordinate: {ax}. It's possible that another thread executed this method just a moment ago.");
				return false;
			}
		}
	}

	public bool RemoveHex(Axial ax)
	{
		lock (_hexAxialLock)
		{
			try
			{
				_hexAxialDrawDictionary.Remove(ax);

				_triggerRedraw = true;

				return true;
			}
			catch
			{
				GD.PrintErr($"Failed to remove hex at axial {ax}. It is likely that no hex exists at this key value in the dictionary. It's possible that another thread executed this method just a moment ago.");
				return false;
			}
		}
	}

	public bool IsHexRendered(Axial ax)
	{
		lock (_hexAxialLock)
		{
			try
			{
				return _hexAxialDrawDictionary.ContainsKey(ax);
			}
			catch
			{
				GD.PrintErr($"Failed to check if {ax} exists in the dictionary");
				return false;
			}
		}
	}
}
