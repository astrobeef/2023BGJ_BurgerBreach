using Godot;
using System;

namespace AxialCS
{

/// <summary>
/// Data for polygon drawing of a 2D hexagon. Decoupled from its Axial position. Look to <see cref="HexagonRenderer._hexAxialDrawDictionary"/> for coupling axials and hexagons.
/// </summary>
    public struct HexagonDraw
    {
        public readonly Vector2 origin;
        public readonly Vector2[] Vertices;
        public readonly Color[] Colors;

        public readonly float side_length;

        private static readonly HexagonDraw _zero = new HexagonDraw(Vector2.Zero, 0.0f, new Color(0,0,0));
        private static readonly HexagonDraw _empty = new HexagonDraw(Vector2.Inf, 0.0f, new Color(0,0,0));

        public static HexagonDraw Zero => _zero;
        public static HexagonDraw Empty => _empty;

        public HexagonDraw(Vector2 origin, float side_length, Color color)
        {
            this.origin = origin;
            this.side_length = side_length;
            Vertices = BuildHexVertices(origin, side_length);
            Colors = new Color[] { color };
        }

        public HexagonDraw(Vector2 origin, float side_length, Color[] colors)
        {
            this.origin = origin;
            this.side_length = side_length;
            Vertices = BuildHexVertices(origin, side_length);
            Colors = colors;
        }

        private static Vector2[] BuildHexVertices(Vector2 origin, float side_length)
        {
            Vector2[] vertices = new Vector2[6];

            float angle = Mathf.DegToRad(30);
            
            for (int i = 0; i < 6; i++)
            {
                vertices[i] = new Vector2(Mathf.Cos(angle) * side_length, Mathf.Sin(angle) * side_length) + origin;
                angle += Mathf.DegToRad(60);
            }

            return vertices;
        }

        public static bool operator ==(HexagonDraw a, HexagonDraw b){
            return a.origin == b.origin && a.Vertices == b.Vertices;
        }

        public static bool operator !=(HexagonDraw a, HexagonDraw b){
            return !(a==b);
        }

        public override string ToString()
        {
            return $"[HexOrigin:{origin}]";
        }
    }

}