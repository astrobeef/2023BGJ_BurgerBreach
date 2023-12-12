using Godot;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

// Axial Coordinate System
namespace AxialCS
{
	/// <summary>
	/// 2D Axial coordinate struct. This is purposefully decoupled from any game-world representation of the Axial.
	/// An axial can be visualized through a <see cref="HexagonDraw"/>, and these two things can be coupled with a dictionary.
	/// </summary>
	public struct Axial
	{
		public static readonly double _SQ3 = Math.Sqrt(3);
		public static readonly double _COS30 = _SQ3 * 0.5;

		//---FIELD DEFINITION---
		private readonly int[] _axial;

		//---CONSTRUCTORS---
		public Axial(int q, int r)
		{
			_axial = new int[3];
			_axial[0] = q;
			_axial[1] = r;
			_axial[2] = -q - r;
		}

		public Axial(int q, int r, int s)
		{
			_axial = new int[3];
			_axial[0] = q;
			_axial[1] = r;
			_axial[2] = s;
		}

		public Axial(int[] axial)
		{
			if (axial.Length == 3)
				this._axial = axial;
			else if (axial.Length == 2)
			{
				this._axial = new int[3];
				this._axial[0] = axial[0];
				this._axial[1] = axial[1];
				this._axial[2] = -axial[0] - axial[1];
			}
			else
			{
				GD.PrintErr("parameter is not of valid length");
				_axial = new int[3] { 0, 0, 0 };
			}
		}

		//---PROPERTIES---
		public int Q => _axial[0];
		public int R => _axial[1];
		public int S => _axial[2];
		/// <summary>
		/// True if the Axial (as a direction) can be scaled down to one of the 6 relevant <see cref="Cardinal"/> <see cref="_axialDirections"/>.
		/// </summary>
		public bool IsCardinalScalar => Array.Exists(_axial, element => element == 0);
		public int LengthSquared => Q * Q + R * R + S * S;

		//---GENERICS---

		private static readonly Axial _zero = new Axial(0, 0);
		public static Axial Zero => _zero;
		private static readonly Axial _empty = new Axial(0, 0, 100);        //This is not a possible Axial
		public static Axial Empty => _empty;

		//---OPERATOR OVERLOADS---
		public static bool operator ==(Axial a, Axial b)
		{
			return a.Q == b.Q && a.R == b.R && a.S == b.S;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is Axial obj_axial)
			{
				return this == obj_axial;
			}

			return false;
		}

		public override int GetHashCode()
		{
			// Use a prime number to combine hash codes in a way that reduces the chance of collisions
			const int prime = 31;
			int hash = 17;  // Another prime number
			hash = hash * prime + Q.GetHashCode();
			hash = hash * prime + R.GetHashCode();
			hash = hash * prime + S.GetHashCode();
			return hash;
		}

		public static bool operator !=(Axial a, Axial b)
		{
			return !(a == b);
		}

		public static Axial operator +(Axial a, Axial b)
		{
			return new Axial(
				a.Q + b.Q,
				a.R + b.R,
				a.S + b.S);
		}

		public static Axial operator -(Axial a, Axial b)
		{
			return new Axial(
				a.Q - b.Q,
				a.R - b.R,
				a.S - b.S);
		}

		public static Axial operator *(Axial a, Axial b)
		{
			return new Axial(
				a.Q * b.Q,
				a.R * b.R,
				a.S * b.S);
		}

		public static Axial operator *(Axial a, int scalar)
		{
			return new Axial(
				a.Q * scalar,
				a.R * scalar,
				a.S * scalar
			);
		}

		public static Axial operator *(int scalar, Axial a)
		{
			return new Axial(
				a.Q * scalar,
				a.R * scalar,
				a.S * scalar
			);
		}

		public static Axial operator /(Axial a, Axial b)
		{
			return new Axial(
				a.Q / b.Q,
				a.R / b.R,
				a.S / b.S);
		}

		public static Axial operator /(Axial a, double scalar)
		{
			return Justify(a.Q / scalar, a.R / scalar, a.S / scalar);
		}

		public static Axial operator /(double scalar, Axial a)
		{
			return Justify(a.Q / scalar, a.R / scalar, a.S / scalar);
		}

		public override string ToString()
		{
			return $"({Q}, {R}, {S})";
		}

		public int[] ToArray()
		{
			return _axial;
		}

		//---OPERATIONS---
		public static int Length(Axial ax)
		{
			return (Math.Abs(ax.Q) + Math.Abs(ax.R) + Math.Abs(ax.S))
					/ 2;
		}

		public static int Distance(Axial a, Axial b)
		{
			return Length(a - b);
		}

		//---DIRECTIONS---
		public enum Cardinal
		{
			E,
			SE,
			SW,
			W,
			NW,
			NE
		}

		public static int CARDINAL_LENGTH = Enum.GetValues(typeof(Axial.Cardinal)).Length;

		private static readonly Axial[] _axialDirections = {
			new Axial(1,0,-1),		//East
			new Axial(0,1,-1),		//South-East
			new Axial(-1,1,0),		//South-West
			new Axial(-1,0,1),		//West
			new Axial(0,-1,1),		//North-West
			new Axial(1,-1,0)		//North-East
		};

		public static Axial Direction(Cardinal direction)
		{
			if ((int)direction >= 0 && (int)direction < _axialDirections.Length)
			{
				return _axialDirections[(int)direction];
			}
			else
			{
				GD.PrintErr($"Direction index {(int)direction} is not within range.");
				Debug.Assert(true, $"Direction index {(int)direction} is not within range.");
				throw new System.Exception($"Direction index {(int)direction} is not within range.");
				return _axialDirections[0];
			}
		}

		/// <summary>
		/// Given an input Axial direction, approximate the closest cardinal unit direction
		/// </summary>
		/// <returns></returns>
		public static Axial ApproximateCardinal(Axial dir)
		{
			/* I made a great revelation in how Axials work in terms of direction.
			* An axial which is perfectly aligned with one of the 6 relevant cardinal directions will:
			*	1. Have one component equal 0.
			* Programmatically, direction validity can be done by:
				1. Check if any one component is 0.
				2. Divide all remaining components by their own absolute value such that they equal +/- 1.
			*
			* Along with this, axials which do NOT align with one of the 6 relevant cardinal directions can be approximated by:
			*	1. Look to which value is closest to 0.
			*	2. Look to the sign of the Q/R/S values.
			* Programmatically, approximation can be done by:
			*	1. Reduce the component with the smallest absolute value to 0.
			*	2. Divide the remaining components by their own absolute value such that they equal +/- 1.
			*	3. Compare these new values to the 6 cardinal directions. One will match.
			*/

			Axial cardinalDirection;
			bool isCardinalScalar = dir.IsCardinalScalar;

			if (isCardinalScalar)
			{

				//Scale components to +/- 1
				int q = (dir.Q == 0) ? 0 : dir.Q / Mathf.Abs(dir.Q);
				int r = (dir.R == 0) ? 0 : dir.R / Mathf.Abs(dir.R);

				cardinalDirection = new Axial(q, r);
			}
			else
			{
				//Get array of the Axial
				int[] dirArray = dir.ToArray();
				//Create tuple for finding the component with the minimum absolute value
				(int index, int value) min = (0, Mathf.Abs(dirArray[0]));

				//Iterate through each component to attempt to find a smaller absolute value than the first index (which is used as the initial minimum)
				for (int i = 1; i < dirArray.Length; i++)
				{
					int absValue = Mathf.Abs(dirArray[i]);

					if (absValue < min.value)
					{
						min.index = i;
					}
				}

				//Set the component of minimum absolute value to 0
				dirArray[min.index] = 0;

				//Iterate through each component to scale to +/- 1 (except for the component just set to 0)
				for (int i = 0; i < dirArray.Length; i++)
				{
					int value = dirArray[i];
					int absValue = Mathf.Abs(dirArray[i]);
					if (value != 0)
						dirArray[i] = value / absValue;
				}

				cardinalDirection = new Axial(dirArray);
			}

			return cardinalDirection;
		}

		public static bool isCardinal(Axial ax, out Cardinal direction)
		{

			// Is it a cardinal direction with a square length of 2 ?
			if (ax.IsCardinalScalar && ax.LengthSquared == 2)
			{

				bool foundComparison = false;
				direction = Cardinal.E;     //Must initialize to some value to satisfy logic

				for (int i = 0; i < _axialDirections.Length; i++)
				{
					Axial dir = _axialDirections[i];
					if (ax == dir)
					{
						foundComparison = true;
						direction = (Cardinal)i;
						break;
					}
				}

				if (foundComparison)
				{
					return true;
				}
				else
				{
					GD.PrintErr($"Axial {ax} is scalar of cardinal and has length of a unit Axial direction, yet found no match.");
					return false;
				}
			}
			else
			{
				GD.PrintErr($"Is cardinal ? {ax.IsCardinalScalar}");
				GD.PrintErr($"Length squared : {ax.LengthSquared}");
				direction = Cardinal.E;
				return false;
			}
		}

		public static Axial Neighbor(Axial ax, Cardinal direction)
		{
			return ax + Axial.Direction(direction);
		}

		private static Vector2[] _displacements = new Vector2[] {
			new Vector2((float)_SQ3, 0),			//East
			new Vector2((float)_COS30, -1.5f),		//South-East
			new Vector2(-(float)_COS30, -1.5f),		//South-West
			new Vector2(-(float)_SQ3, 0),			//West
			new Vector2(-(float)_COS30, 1.5f),		//North-West
			new Vector2((float)_COS30, 1.5f)		//North-East
		};

		/// <summary>
		/// Find the pixel displacement vector of a direction.
		/// </summary>
		/// <param name="side_length"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static Vector2 pxDisplacement(float side_length, Cardinal direction)
		{
			Vector2 displacement = _displacements[(int)direction];

			return side_length * displacement;
		}

		public static Vector2 AxToPx(Vector2 offset, float side_length, Axial ax)
		{
			float x = side_length * (ax.Q * (float)_SQ3 + ax.R * (float)_SQ3 * 0.5f);
			float y = side_length * (ax.Q * 0.0f + ax.R * 1.5f);

			return offset + new Vector2(x, y);
		}

		public static Axial PxToAx(Vector2 offset, float side_length, Vector2 position)
		{
			Vector2 pt = new Vector2((position.X - offset.X) / side_length, (position.Y - offset.Y) / side_length);

			double b0 = _SQ3 / 3.0;
			double b1 = -1.0 / 3.0;
			double b2 = 0;
			double b3 = 2.0 / 3.0;

			double q_dub = b0 * pt.X + b1 * pt.Y;
			double r_dub = b2 * pt.X + b3 * pt.Y;
			double s_dub = -q_dub - r_dub;

			return Justify(q_dub, r_dub, s_dub);
		}

		public static Vector3 AxToV3(Vector3 offset, float side_length, Axial ax)
		{
			Vector2 offset_2D = new Vector2(offset.X, offset.Z);
            Vector2 AxToVetor2 = AxToPx(offset_2D, side_length, ax);
			Vector3 axPosToV3 = new Vector3(AxToVetor2.X, 0, AxToVetor2.Y);
			return offset + axPosToV3;
		}
		
		public static Axial V3ToAx(Vector3 offset, float side_length, Vector3 position)
		{
			Vector2 offset_2D = new Vector2(offset.X, offset.Z);
			Vector2 position_2D = new Vector2(position.X, position.Z);
			Axial axial = PxToAx(offset_2D, side_length, position_2D);
			return axial;
		}


		public static Axial Justify(double q_dub, double r_dub, double s_dub)
		{

			int q = Mathf.RoundToInt(q_dub);
			int r = Mathf.RoundToInt(r_dub);
			int s = Mathf.RoundToInt(s_dub);

			double q_diff = Math.Abs(q - q_dub);
			double r_diff = Math.Abs(r - r_dub);
			double s_diff = Math.Abs(s - s_dub);

			// Series of conditionals to ensure that Q+R+S=0 and that the value with the largest difference is modified.
			if (q_diff > r_diff && q_diff > s_diff)
				q = -r - s;
			else if (r_diff > s_diff)
				r = -q - s;
			else
				s = -q - r;

			return new Axial(q, r, s);
		}


	}
}
