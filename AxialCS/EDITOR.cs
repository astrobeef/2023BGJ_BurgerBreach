using Godot;
using System;

namespace AxialCS
{
	[Tool]
	public partial class EDITOR : Node2D
	{
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			// Execute in EDITOR only
			if(Engine.IsEditorHint()){
				// ...
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			// Execute in EDITOR only
			if(Engine.IsEditorHint()){
				// ...
			}
		}

        public override void _UnhandledInput(InputEvent @event)
        {
			GD.Print("Unhandled Input");
            base._UnhandledInput(@event);
        }
    }
}
