using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace EditorTools
{
    [Tool]
    public partial class SceneTreeObserver : Control
    {
        [Export]
        Godot.Collections.Array<Node> _sceneTree;

        Godot.Collections.Array<Node> _sceneTreeTracked;

        public event Action<Node> EventNodeAddedToScene;
        public event Action<Node> EventNodeRemovedFromScene;

        // Called when the node enters the scene tree for the first time.
        public override void _EnterTree()
        {
            // Execute in EDITOR only
            if (Engine.IsEditorHint())
            {
                _handlingSceneTreeChange = false;

                GD.Print("Executing _EnterTree on SceneTreeObserver");

                _sceneTree = this.GetTree().EditedSceneRoot.GetChildren(true);
                GD.Print($"Scene tree has {_sceneTree.Count} nodes");
                foreach (Node node in _sceneTree)
                    GD.Print($"Scene Tree Node:{node.Name}");

                _sceneTreeTracked = new Godot.Collections.Array<Node>(_sceneTree.Cast<Node>());

                EventNodeAddedToScene += TEST_OnNodeAdded;
                EventNodeRemovedFromScene += TEST_OnNodeRemoved;
            }
        }

        Vector2 _offset = new Vector2(640, 360);
		float _sideLength = 30.0f;

        private void TEST_OnNodeAdded(Node node){
            GD.Print($"Node {node.Name} added to the scene");

            if(node is Node2D){
                GD.Print($"Modifying Node2D. side length is {_sideLength}");
                Node2D node2D = node as Node2D;
                
                Vector2 spritePosition = node2D.Position;
                AxialCS.Axial spriteAxial = AxialCS.Axial.PxToAx(_offset, _sideLength, spritePosition);
                node2D.Position = AxialCS.Axial.AxToPx(_offset, _sideLength, spriteAxial);
            }
            else if (node is Sprite2D) {
                GD.Print($"Modifying Sprite2D. side length is {_sideLength}");
                Sprite2D sprite2D = node as Sprite2D;

                Vector2 spritePosition = sprite2D.Position;
                AxialCS.Axial spriteAxial = AxialCS.Axial.PxToAx(_offset, _sideLength, spritePosition);
                sprite2D.Position = AxialCS.Axial.AxToPx(_offset, _sideLength, spriteAxial);
            }
        }

        private void TEST_OnNodeRemoved(Node node){
            GD.Print($"Node {node.Name} removed from the scene");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            // Execute in EDITOR only
            if (Engine.IsEditorHint() && !_handlingSceneTreeChange)
            {
                ObserveSceneTree();
            }
        }

        bool _handlingSceneTreeChange = false;

        private void ObserveSceneTree()
        {
            _sceneTree = this.GetTree().EditedSceneRoot.GetChildren(true);

            if (_sceneTreeTracked == null)
            {
                GD.PrintErr("_sceneTreeTracked is null");
                return;
            }

            _handlingSceneTreeChange = true;

            int sceneTreeCount = _sceneTree.Count;
            int sceneTreeTrackedCount = _sceneTreeTracked.Count;

            // A node was added
            if (sceneTreeCount > sceneTreeTrackedCount)
            {
                foreach (Node untrackedNode in _sceneTree)
                {
                    if (!_sceneTreeTracked.Contains(untrackedNode))
                    {
                        EventNodeAddedToScene?.Invoke(untrackedNode);
                        _sceneTreeTracked.Add(untrackedNode);
                        // Do not break in case multiple nodes were added
                    }
                }
            }
            // A node was removed
            else if (sceneTreeCount < sceneTreeTrackedCount)
            {
                //Create instance to allow modifying the tracked list within the for loop
                Node[] trackedSceneInstance = _sceneTreeTracked.ToArray();

                for (int i = 0; i < trackedSceneInstance.Length; i++)
                {
                    Node trackedNode = trackedSceneInstance[i];
                    if (!_sceneTree.Contains(trackedNode))
                    {
                        EventNodeRemovedFromScene?.Invoke(trackedNode);
                        _sceneTreeTracked.Remove(trackedNode);
                        // Do not break in case multiple nodes were added
                    }
                }
            }

            _handlingSceneTreeChange = false;
        }
    }
}