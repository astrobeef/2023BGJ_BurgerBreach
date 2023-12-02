using Godot;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using EditorTools;


namespace AxialCS
{
    /// <summary>
    /// Currently handles HEXAGON shaped grids only. Generates an axial grid.
    /// </summary>
    public struct AxialGrid
    {
        private readonly int _width;
        public Axial[] Axials => _axials.ToArray();
        private readonly List<Axial> _axials;
        private readonly GridType _type = GridType.Hexagon;

        public bool isBuilt => _axials[0] != Axial.Empty;

        public enum GridType { Hexagon };

        public AxialGrid(int width)
        {
            this._width = width;

            _axials = new List<Axial>{Axial.Empty};
            Build();
        }

        private void Build()
        {
            GD.Print("Beginning build");

            switch (_type)
            {
                case GridType.Hexagon:
                    {
                        Axial[] grid = Build_Hexagon(_width);
                        _axials.Clear();
                        _axials.AddRange(grid);
                        
                        GD.Print($"Set _axials. New count is {_axials.Count} @ {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                    }
                    break;
                default:
                    break;
            }
        }

        private static Axial[] Build_Hexagon(int width)
        {

            Axial[] grid;
            Axial taskInitial = Axial.Zero;

            Task<Axial[]>[] tasks = new Task<Axial[]>[Axial.CARDINAL_LENGTH];

            int failsafe = 0;

            for (int i = 0; i < Axial.CARDINAL_LENGTH; i++)
            {
                int taskIndex = i;

                tasks[i] = Task.Run(() =>
                {
                    List<Axial> taskGrid = new List<Axial>();

                    Axial.Cardinal taskCardinal = (Axial.Cardinal)taskIndex;
                    Axial taskDirection = Axial.Direction(taskCardinal);

                    taskGrid.Add(taskDirection);

                    RecursiveAxialBuild_Hexagon(ref failsafe, ref taskGrid, taskDirection, taskDirection, width);

                    return taskGrid.ToArray();
                });
            }
            
                int count = 1 + tasks.Sum(innerArray => innerArray.Result.Length);
                grid = new Axial[count];
                grid[0] = Axial.Zero;

                int index = 1;
                for (int iterateTask = 0; iterateTask < tasks.Length; iterateTask++)
                {
                    Task<Axial[]> t = tasks[iterateTask];
                    Axial[] taskGrid = t.Result;

                    for (int i = 0; i < taskGrid.Length; i++)
                    {
                        GD.Print($"Adding {taskGrid[i]}@[{i}] from task grid {iterateTask} to grid @[{index}]");

                        grid[index] = taskGrid[i];

                        index++;
                    }
                }

                for (int i = 0; i < grid.Length; i++)
                {
                    Axial ax = grid[i];
                    GD.Print($"grid to return[{i}]: {ax}");
                }

                return grid;
        }

        private static void RecursiveAxialBuild_Hexagon(ref int iteration, ref List<Axial> taskGrid, Axial origin, Axial inputDirection, float width)
        {
            iteration++;
            Axial[] newNeighborDirections = ParsedDirections(inputDirection);
            float radiusSquared = 2 * ((width+1) * (width+1));

            foreach (Axial direction in newNeighborDirections)
            {
                Axial newAxial = origin + direction;

                Thread.Sleep(100);

                if (!taskGrid.Contains(newAxial))
                {
                    if (newAxial.LengthSquared < radiusSquared)
                    {
                        GD.Print($"Adding new ax {newAxial} to grid");
                        taskGrid.Add(newAxial);
                    }
                    else
                    {
                        GD.Print($"Not adding new ax {newAxial} to grid because it exceeds the max length");
                    }
                }
                else
                {
                    GD.PrintErr($"Redundant addition of {newAxial} from {origin} with parameter direction {inputDirection}");

                    foreach(Axial ax in taskGrid){
                        GD.PrintErr($"ax:{ax}");
                    }
                }
                if (newAxial.LengthSquared < radiusSquared && iteration < 100000)
                {
                    Axial newDirection = newAxial - origin;
                    RecursiveAxialBuild_Hexagon(ref iteration, ref taskGrid, newAxial, newDirection, width);
                }
            }
        }

        private static Axial[] ParsedDirections(Axial input)
        {
            Axial[] parsedDirections;

            Axial axCardinalApproximation = Axial.ApproximateCardinal(input);

            int length = Axial.CARDINAL_LENGTH;

            if (length % 2 != 0)
                GD.PrintErr($"Length of cardinal directions {length} is not even and so this method will not work as intended.");

            Axial.Cardinal cardinalDirection;

            if (Axial.isCardinal(axCardinalApproximation, out cardinalDirection))
            {

                int cardDir_int = (int)cardinalDirection;

                switch (cardinalDirection)
                {
                    case Axial.Cardinal.E:
                        {
                            parsedDirections = new Axial[3];

                            parsedDirections[0] = Axial.Direction(cardinalDirection);

                            int neighbor_clockwise = (cardDir_int + 1) % length;
                            parsedDirections[1] = Axial.Direction((Axial.Cardinal)neighbor_clockwise);

                            int Neighbor_CounterClockwise = ((cardDir_int - 1) + length) % length;     //Add length to ensure the dividend is positive.
                            parsedDirections[2] = Axial.Direction((Axial.Cardinal)Neighbor_CounterClockwise);
                        }
                        break;
                    case Axial.Cardinal.SE:
                        {
                            parsedDirections = new Axial[1];
                            parsedDirections[0] = Axial.Direction(cardinalDirection);
                        }
                        break;
                    case Axial.Cardinal.SW:
                        {
                            parsedDirections = new Axial[3];
                            // Set parsed to inverse direction and its adjacent neighbors
                            parsedDirections[0] = Axial.Direction(cardinalDirection);

                            int neighbor_clockwise = (cardDir_int + 1) % length;
                            parsedDirections[1] = Axial.Direction((Axial.Cardinal)neighbor_clockwise);

                            int neighbor_counterClockwise = ((cardDir_int - 1) + length) % length;     //Add length to ensure the dividend is positive.
                            parsedDirections[2] = Axial.Direction((Axial.Cardinal)neighbor_counterClockwise);
                        }
                        break;
                    case Axial.Cardinal.W:
                        {
                            parsedDirections = new Axial[1];
                            parsedDirections[0] = Axial.Direction(cardinalDirection);
                        }
                        break;
                    case Axial.Cardinal.NW:
                        {
                            parsedDirections = new Axial[3];
                            // Set parsed to inverse direction and its adjacent neighbors
                            parsedDirections[0] = Axial.Direction(cardinalDirection);

                            int neighbor_clockwise = (cardDir_int + 1) % length;
                            parsedDirections[1] = Axial.Direction((Axial.Cardinal)neighbor_clockwise);

                            int neighbor_counterClockwise = ((cardDir_int - 1) + length) % length;     //Add length to ensure the dividend is positive.
                            parsedDirections[2] = Axial.Direction((Axial.Cardinal)neighbor_counterClockwise);
                        }
                        break;
                    case Axial.Cardinal.NE:
                        {
                            parsedDirections = new Axial[1];
                            parsedDirections[0] = Axial.Direction(cardinalDirection);
                        }
                        break;
                    default:
                        parsedDirections = new Axial[1] { Axial.Zero };
                        GD.PrintErr($"Uncaught parse direction {cardinalDirection.ToString()}");
                        break;
                }
            }
            else
            {
                GD.PrintErr("Failed to convert input direction to cardinal direction");
                return new Axial[1] { Axial.Zero };
            }

            return parsedDirections;
        }

        public static Dictionary<Axial, HexagonDraw> CalcHexAxialGrid(Axial[] GridAxials, Vector2 offset, float side_length){
            Dictionary<Axial, HexagonDraw> dictionary = new Dictionary<Axial, HexagonDraw>();

            foreach(Axial ax in GridAxials){
                HexagonDraw hexDraw = CalcHexDraw(ax, offset, side_length);
                if(!dictionary.ContainsKey(ax) && ax != Axial.Empty)
                {
                    GD.Print($"Adding {ax} @ {hexDraw.origin}px to dictionary");
                    dictionary.Add(ax, hexDraw);
                }
                else{
                    GD.PrintErr($"Redundant addition {ax} to dictionary OR ax is empty");
                    if (ax == Axial.Empty)
                        break;
                }
            }

            return dictionary;
        }
        
		private static HexagonDraw CalcHexDraw(Axial axial, Vector2 offset, float side_length)
		{
			Vector2 pxOrigin = Axial.AxToPx(offset, side_length, axial);
			HexagonDraw hexDraw = new HexagonDraw(pxOrigin, side_length, Colors.Black);

            return hexDraw;
		}

        /// <summary>
        /// Returns true if the parameter exists in the grid
        /// </summary>
        public bool IsAxialOnGrid(Axial axial){
            return _axials.Contains(axial);
        }

    }
}