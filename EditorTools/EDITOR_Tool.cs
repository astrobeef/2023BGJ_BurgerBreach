using Godot;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using EditorTools;
using GodotPlugins.Game;
using AxialCS;
using Deck;
using Model;
using System.Data;

namespace EditorTools
{
	public partial class EDITOR_Tool : Control
	{

		public delegate void ProcessEvent();
		public event ProcessEvent OnProcess_EditorOnly;
		public event ProcessEvent OnProcess_PlayOnly;
		public event ProcessEvent OnProcess;

		public EDITOR_MouseMoveObserver MouseMoveObserver;
		public Model_DEMO Model;
		public View.View_DEMO View;

		[Export]
		bool _disableScript = true, _disableInput = true, _disableDraw = true;

		[Export]
		bool _disableBoardgame = true;

		[Export]
		float sideLength
		{
			get { return _sideLength; }
			set
			{
				if (OnSideLengthChanged(_sideLength, value))
					_sideLength = value;
			}
		}
		float _sideLength = 50.0f;

		[Export]
		int gridSize
		{
			get {return _gridSize; }
			set{
				_gridSize = value;
				TestAxialGrid(this, _gridSize);
			}
		}
		int _gridSize = 2;

		[Export]
		Vector2 _offset = new Vector2(1280 / 2, 720 / 2);
		static float _HEX_IMG_SCALE = 256.0f;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			// Execute in EDITOR only
			if (Engine.IsEditorHint() && !_disableScript)
			{
				GD.Print("Executing _Ready in editor");
			}
			else{
				
				GD.Print("Executing _Ready in play");
			}

			MouseMoveObserver = new EDITOR_MouseMoveObserver(this, OnMouseMovement_UpdateSelectedAxial);
			if(!_disableBoardgame)
			{
				EnableMVC();
			}
		}

		public void TestAxialGridProgress(Axial[] GridProgress){
			HexAxialGrid = AxialGrid_DEMO.CalcHexAxialGrid(GridProgress, _offset, _sideLength);
			QueueRedraw();
		}

		private async void TestAxialGrid(EDITOR_Tool This, int gridSize)
		{
			AxialGrid_DEMO axialGrid = new AxialGrid_DEMO(This, gridSize);

			await Task.Run(() =>
			{
				GD.Print("Executing before wait");

				int failsafe = 0;
				while (!axialGrid.isBuilt && failsafe < 100)
				{
					Thread.Sleep(50);
					failsafe++;
				}

				Thread.Sleep(1000);
			});
			
			GD.Print("Executing after wait");
			HexAxialGrid = AxialGrid_DEMO.CalcHexAxialGrid(axialGrid.Axials, _offset, _sideLength);
			// GD.Print($"TOTAL HEXES: {HexAxialGrid.Count}");
		}

		private void EnableMVC()
		{
			if (!_disableBoardgame && Model == null)
			{
				Node ViewNode = this.GetChild(1);
				if (!(ViewNode is View.View_DEMO View))
				{
					GD.PrintErr("Could not pull View script off ViewNode");
				}
				else
				{
					if(!View.isInit && !View.isInitializing)
						View._Ready();
						
					Model = new Model_DEMO(View);
				}
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (MouseMoveObserver == null)
			{
				GD.PrintErr("Setting detect mouse movement script because it did not get set in 'ready'");
				MouseMoveObserver = new EDITOR_MouseMoveObserver(this, OnMouseMovement_UpdateSelectedAxial);
			}

			EnableMVC();

			// Execute in EDITOR only
			if (Engine.IsEditorHint() && !_disableScript)
			{
				OnProcess_EditorOnly?.Invoke();
				// ...
				if (!_disableInput)
					ProcessInput();
			}
			else{
				OnProcess_PlayOnly?.Invoke();
			}

			OnProcess?.Invoke();
		}

		private static int _POINTS_LENGTH = 100;
		private Vector2[] _pxPoints = System.Linq.Enumerable.Repeat(Vector2.Zero, _POINTS_LENGTH).ToArray();
		private Axial[] _axPoints = System.Linq.Enumerable.Repeat(Axial.Empty, _POINTS_LENGTH).ToArray();
		private HexagonDraw[] _hexDraws = System.Linq.Enumerable.Repeat(HexagonDraw.Empty, _POINTS_LENGTH).ToArray();
		private HexagonDraw[] HexDrawsUsed => _hexDraws.Where(h => h != HexagonDraw.Empty).ToArray();
		private Axial _axMousePosition = Axial.Empty;

		private static int _GRID_STEPS = 100;

		private Dictionary<Axial, HexagonDraw> HexAxialGrid = new Dictionary<Axial, HexagonDraw>();

		public override void _Draw()
		{
			if(_disableDraw)
				return;

			for (int i = 0; i < _pxPoints.Length; i++)
			{
				Vector2 pxPoint = _pxPoints[i];
				Axial axPoint = _axPoints[i];
				HexagonDraw hexDraw = _hexDraws[i];
				Vector2 axPx = Axial.AxToPx(_offset, _sideLength, axPoint);

				if (pxPoint != Vector2.Zero)
				{
					DrawCircle(pxPoint, 2.0f, Colors.Red);
				}
				if (axPx != Vector2.Zero)
				{
					DrawCircle(axPx, 2.0f, Colors.Green);
				}
				if (hexDraw != HexagonDraw.Zero)
				{
					DrawPolygon(hexDraw.Vertices, hexDraw.Colors);
				}
			}

			foreach (KeyValuePair<Axial, HexagonDraw> pair in HexAxialGrid)
			{
				HexagonDraw hex = pair.Value;
				for (int j = 0; j < hex.Vertices.Length; j++)
				{
					Vector2 vert_a = hex.Vertices[j];
					Vector2 vert_b = (j + 1 < hex.Vertices.Length) ? hex.Vertices[j + 1] : hex.Vertices[0];
					DrawLine(vert_a, vert_b, hex.Colors[0], 2f, true);
				}
			}

			if (_axMousePosition != Axial.Empty)
			{
				Vector2 pxMouse = Axial.AxToPx(_offset, _sideLength, _axMousePosition);
				DrawCircle(pxMouse, 3.0f, Colors.GreenYellow);

				DrawLine(MouseMoveObserver._pos_cur, pxMouse, Colors.SeaGreen, 1.0f, true);
				DrawString(GetThemeFont("font"), MouseMoveObserver._pos_cur + new Vector2(5, -5), _axMousePosition.ToString(), HorizontalAlignment.Left, -1, 16, Colors.GreenYellow);

				foreach (int i in Enum.GetValues(typeof(Axial.Cardinal)))
				{
					Axial axNeighbor = Axial.Neighbor(_axMousePosition, (Axial.Cardinal)i);
					Vector2 pxNeighbor = Axial.AxToPx(_offset, _sideLength, axNeighbor);
					DrawCircle(pxNeighbor, 1.0f, Colors.GreenYellow);
				}
			}
		}


		private void ProcessInput()
		{
			OnMouseClick_DrawHexagon();
		}

		bool _leftMouseClicked = false;

		private void OnMouseClick_DrawHexagon()
		{
			if (_leftMouseClicked)
				_leftMouseClicked = Input.IsMouseButtonPressed(MouseButton.Left);

			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				if (!_leftMouseClicked)
				{
					_leftMouseClicked = true;
					MouseMoveObserver._pos_cur = GetViewport().GetMousePosition();
					GD.Print($"Registered mouse input. Mouse position: {MouseMoveObserver._pos_cur}");

					float maxX = GetViewportRect().End.X;
					float maxY = GetViewportRect().End.Y;

					GD.Print($"maxX:{maxX}, maxY:{maxY}");

					if (MouseMoveObserver._pos_cur.X < 0 || MouseMoveObserver._pos_cur.X > maxX
						|| MouseMoveObserver._pos_cur.Y < 0 || MouseMoveObserver._pos_cur.Y > maxY)
					{
						GD.Print($"Input is out of bounds. Ignoring input");
						return;
					}

					Axial axMouse = Axial.PxToAx(_offset, _sideLength, MouseMoveObserver._pos_cur);

					int foundAxialIndex = -1;

					for (int i = 0; i < _axPoints.Length; i++)
					{
						if (_axPoints[i] == axMouse)
						{
							foundAxialIndex = i;
							break;
						}
					}

					//If the selected axial is NOT already rendering a hexagon,
					if (foundAxialIndex < 0)
					{
						for (int i = _pxPoints.Length - 1; i > 0; i--)
						{
							_pxPoints[i] = _pxPoints[i - 1];
							_axPoints[i] = _axPoints[i - 1];
							_hexDraws[i] = _hexDraws[i - 1];
						}

						_pxPoints[0] = MouseMoveObserver._pos_cur;
						_axPoints[0] = axMouse;

						HexagonDraw hexagonDraw = new HexagonDraw(Axial.AxToPx(_offset, _sideLength, _axPoints[0]), _sideLength, Colors.Black);
						_hexDraws[0] = hexagonDraw;
					}
					// Else, it is already rendering a hexagon,
					else
					{
						bool isBlack = _hexDraws[foundAxialIndex].Colors[0] == Colors.Black;
						if (isBlack)
							_hexDraws[foundAxialIndex].Colors[0] = Colors.White;
						else
						{
							_hexDraws[foundAxialIndex] = HexagonDraw.Zero;
							_axPoints[foundAxialIndex] = Axial.Empty;
						}
					}

					QueueRedraw();
				}
			}
		}

		private void OnMouseMovement_UpdateSelectedAxial(){
				_axMousePosition = Axial.PxToAx(_offset, _sideLength, MouseMoveObserver._pos_cur);
				QueueRedraw();
		}

		private bool OnSideLengthChanged(float old_length, float new_length)
		{
			if (_disableScript || _disableDraw || !Engine.IsEditorHint())
				return false;

			GD.Print($"Side length was set to {new_length} from {old_length}. Modifying HexagonDraws and queuing redraw");

			if (!_awaitingRedraw)
			{
				Threaded_RedrawHexagons(old_length, new_length);
				return true;
			}
			else
			{
				GD.PrintErr("Cannot change side length because a preivous redraw process has not finished.");
				return false;
			}
		}

		private bool _awaitingRedraw = false;
		private async void Threaded_RedrawHexagons(float old_length, float new_length)
		{
			_awaitingRedraw = true;

			int length_hexAll = _hexDraws.Length;
			int length_hexUsed = HexDrawsUsed.Length;

			int maxTasks = 4;
			int tasksLength = Math.Min(maxTasks, length_hexUsed);

			if(tasksLength <= 0)
			{
				_awaitingRedraw = false;
				return;
			}

			int iterationsPerTask = length_hexUsed / tasksLength;
			int remainderIterations = length_hexUsed % tasksLength;

			Task[] tasks = new Task[tasksLength];

			HexagonDraw[] newHexDraws = _hexDraws;
			Axial[] newAxPoints = _axPoints;

			Vector2 firstOrigin = HexDrawsUsed[0].origin;
			Axial firstAxial = Axial.PxToAx(_offset, old_length, HexDrawsUsed[0].origin);

			for (int i = 0; i < tasksLength; i++)
			{
				int taskIndex = i;
				tasks[i] = Task.Run(() =>
				{
					int iterations = (taskIndex < tasksLength - 1) ? iterationsPerTask : iterationsPerTask + remainderIterations;

					for (int j = 0; j < iterations; j++)
					{
						int index = taskIndex * iterationsPerTask + j;

						if (index >= length_hexAll)
						{
							GD.PrintErr($"Index {index} is out of range {length_hexAll}");
							break;
						}

						HexagonDraw oldIterateHex = HexDrawsUsed[index];
						Axial oldIterateAxial = Axial.PxToAx(_offset, old_length, oldIterateHex.origin);

						Axial AxialDistance_FirstToIterate = oldIterateAxial - firstAxial;
						// The pixel distance between the first indexed Hexagon and the iterate Hexagon, using the new length
						Vector2 newPixelDistance_FirstToIterate = Axial.AxToPx(Vector2.Zero, new_length, AxialDistance_FirstToIterate);
						Vector2 newOrigin = firstOrigin + newPixelDistance_FirstToIterate;
						Axial newAxial = Axial.PxToAx(_offset, new_length, newOrigin);
						Vector2 newOrigin_adjusted = Axial.AxToPx(_offset, new_length, newAxial);

						newHexDraws[index] = new HexagonDraw(newOrigin_adjusted, new_length, oldIterateHex.Colors);
						newAxPoints[index] = newAxial;
					}
				});
			}

			await Task.WhenAll(tasks);

			_awaitingRedraw = false;
			_hexDraws = newHexDraws;
			_axPoints = newAxPoints;
			QueueRedraw();
		}
	}
}
