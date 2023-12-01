using AxialCS;
using Godot;
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		HexagonRenderer hexRenderer = new HexagonRenderer();
		GetActiveScene(this, this.Name).AddChild(hexRenderer);

		Vector2 initPos = new Vector2(500,500);
		float side_length = 80.0f;

		Axial ax_origin = new Axial(0, 0);
		GD.Print($"These axials do not have a screen space position. The first axial is located in Axial position: {ax_origin}");

		IProgress<(Axial, HexagonDraw)> progress = new Progress<(Axial, HexagonDraw)>(tuple => {
			Axial ax = tuple.Item1;
			HexagonDraw hex = tuple.Item2;
			hexRenderer.AddHex(ax, hex);
		});

		Task.Run(() =>
		{
			for(int i = 1; i < 5; i++){
			foreach (Axial.Cardinal card in Enum.GetValues(typeof(Axial.Cardinal)))
			{
				Thread.Sleep(250);

				GD.Print("\n");
				Axial direction = i * Axial.Direction(card);
				GD.Print($"The iterate direction is: {card}");
				GD.Print($"The axial direction is: {direction}");
				Axial ax_inDir = ax_origin + direction;
				GD.Print($"The axial coordinate to the {card} of the origin axial is : {ax_inDir}");
				Vector2 displacement = i * Axial.pxDisplacement(side_length, card);
				float mag = displacement.Length();
				GD.Print($"The axial coordinate to the {card} of the origin axial, given a side length of {side_length}px, will be displaced by {displacement}, for a distance of {mag}");

				Vector2 posPx = Axial.AxToPx(initPos, side_length, ax_inDir);
				GD.Print($"The position, according to our AxToPx method, is {posPx}");

				Axial ax = Axial.PxToAx(initPos, side_length, posPx + new Vector2(25, 25));
				GD.Print($"The axial, according to our PxToAx method, is {ax}");

				HexagonDraw hex = new HexagonDraw(posPx, side_length, colors[(int)card % 3]);

				progress.Report((ax_inDir, hex));
			}
			}
		});

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
