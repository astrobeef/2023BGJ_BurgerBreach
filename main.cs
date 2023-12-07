using AxialCS;
using Godot;
using Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class main : Node
{
	private Godot.Color[] colors = new Color[] {
	new Color(0.1f, 0.1f, 0.1f),
	new Color(0.5f, 0.5f, 0.5f),
	new Color(0.9f, 0.9f, 0.9f)};

	private static main _instance;
	public static main Instance => _instance;

	public Utility.MouseMoveObserver MouseMoveObserver => _mouseMoveObserver;
	private Utility.MouseMoveObserver _mouseMoveObserver;

	public Model_Game gameModel;

	public Action OnProcess;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_instance = this;
		_mouseMoveObserver = new Utility.MouseMoveObserver(_instance);

		GD.Print($"Main ready. Is instance set ? {_instance != null}");
		gameModel = new Model_Game();
	}

	public override void _ExitTree()
	{
		_instance = null;
	}

    public override void _Process(double delta)
    {
		OnProcess?.Invoke();
    }

    public static Node GetActiveScene(Node anyNode, string GM_Name){
		Node[] sceneTreeNodes = anyNode.GetTree().Root.GetChildren(false).ToArray<Node>();

		if(sceneTreeNodes.Length > 2){
			GD.PrintErr("Scene tree node length exceeds 2. Currently, this method assumes there are two nodes: one the active scene and one the Game Manager." +
			" It may not function properly if there are more than two nodes.");
		}
		
		foreach (Node node in sceneTreeNodes)
		{
			GD.Print($"Iterating over node: {node.Name}");
			if(node.Name != GM_Name)
				return node;
		}

		GD.PrintErr("Failed to find active scene node. Returning null. Check that this script is an Autoload and not attached to the Active Scene");
		return null;
	}
}
