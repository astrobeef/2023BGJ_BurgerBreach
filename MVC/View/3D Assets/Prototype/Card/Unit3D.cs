using Godot;
using Model;
using System;

public partial class Unit3D : Node3D
{
	[Export]
	public Unit unit;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Wait to assign name incase _Ready fires before unit reference is assigned.
		var async = async () =>
		{
			while (unit == null)
				await System.Threading.Tasks.Task.Delay(10);

			Name = $"[{unit.name}@{unit.pos}]";
		};

		async.Invoke();
	}

	public void OnHover()
	{
		// Any VFX here
	}

	public void OnHoverOff()
	{
		// Any VFX here
	}

	public void OnClicked()
	{
		// Any VFX here
	}

	public void OnClickOff()
	{
		// Any VFX here
	}

	public void OnUnitCreated()
	{
		// Creation VFX here
	}

	public void OnUnitMove(Vector3 destination)
	{
		// Move the unit here
		// Position = destination;

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(this, "position", destination, 0.3f).SetTrans(Tween.TransitionType.Sine);

		//Update name to show position. Just for debugging
		Name = $"[{unit.name}@{unit.pos}]";
	}

	public void OnUnitAttack(Unit3D target3D)
	{
		// Attack VFX here
	}

	public void OnUnitDamaged()
	{
		// Tween tween = GetTree().CreateTween();
		// tween.TweenProperty(this, "scale", 0.8f, 0.15f).SetTrans(Tween.TransitionType.Sine);
		// tween.TweenProperty(this, "scale", 1.0f, 0.15f).SetTrans(Tween.TransitionType.Sine);
	}

	public void OnUnitBuffed()
	{
		// Buffed VFX here
	}

	public void OnUnitDeath()
	{
		// Any death VFX here
	}
	


	public bool OnObjectSelected()
	{
		// Do anything when this unit is selected
		return true;
	}

	public bool OnObjectDeselected()
	{
		// Do anything when this unit is deselected
		return true;
	}
}
