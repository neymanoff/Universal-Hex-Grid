using System;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Immutable axial coordinate (Q, R) on a pointy-top hexagonal grid, with derived cubic coordinate S = -Q - R.
    /// Pure C# domain struct with zero engine dependencies.
    /// </summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        /// <summary>
        /// The axial Q coordinate.
        /// </summary>
        public readonly int Q;

        /// <summary>
        /// The axial R coordinate.
        /// </summary>
        public readonly int R;

        /// <summary>
        /// Derived cubic coordinate S, always satisfying the cubic invariant Q + R + S = 0.
        /// </summary>
        public int S => -Q - R;

        /// <summary>
        /// Coordinate representing the origin (0, 0).
        /// </summary>
        public static readonly HexCoord Zero = new(0, 0);

        private static readonly HexCoord[] DirectionVectors =
        {
            new(1, 0),   // East (0)
            new(1, -1),  // SouthEast (1)
            new(0, -1),  // SouthWest (2)
            new(-1, 0),  // West (3)
            new(-1, 1),  // NorthWest (4)
            new(0, 1)    // NorthEast (5)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="HexCoord"/> struct.
        /// </summary>
        /// <param name="q">Axial Q coordinate.</param>
        /// <param name="r">Axial R coordinate.</param>
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>
        /// Returns the direction offset vector for the specified <see cref="HexDirection"/>.
        /// </summary>
        public static HexCoord GetDirectionVector(HexDirection direction)
        {
            return DirectionVectors[(int)direction];
        }

        /// <summary>
        /// Returns the direction offset vector for the specified direction index [0..5] (wrapped modulo 6).
        /// </summary>
        public static HexCoord GetDirectionVector(int directionIndex)
        {
            int idx = (directionIndex % 6 + 6) % 6;
            return DirectionVectors[idx];
        }

        /// <summary>
        /// Returns the immediate neighbor in the specified <see cref="HexDirection"/>.
        /// </summary>
        public HexCoord Neighbor(HexDirection direction)
        {
            var offset = DirectionVectors[(int)direction];
            return new HexCoord(Q + offset.Q, R + offset.R);
        }

        /// <summary>
        /// Returns the immediate neighbor in the direction index [0..5] (wrapped modulo 6).
        /// </summary>
        public HexCoord Neighbor(int directionIndex)
        {
            int idx = (directionIndex % 6 + 6) % 6;
            var offset = DirectionVectors[idx];
            return new HexCoord(Q + offset.Q, R + offset.R);
        }

        /// <summary>
        /// Copies all 6 immediate neighbors into a provided destination array (length must be >= 6).
        /// Allocation-free.
        /// </summary>
        public void GetNeighbors(HexCoord[] destination)
        {
            if (destination == null || destination.Length < 6)
                throw new ArgumentException("Destination array must be non-null and have length at least 6.", nameof(destination));

            for (int i = 0; i < 6; i++)
            {
                var offset = DirectionVectors[i];
                destination[i] = new HexCoord(Q + offset.Q, R + offset.R);
            }
        }

        /// <summary>
        /// Returns an array containing all 6 immediate neighbors.
        /// </summary>
        public HexCoord[] GetNeighbors()
        {
            var neighbors = new HexCoord[6];
            GetNeighbors(neighbors);
            return neighbors;
        }

        /// <summary>
        /// Calculates the exact hex grid distance to another coordinate in O(1) time.
        /// </summary>
        public int DistanceTo(HexCoord other)
        {
            int dq = Math.Abs(Q - other.Q);
            int dr = Math.Abs(R - other.R);
            int ds = Math.Abs(S - other.S);
            return (dq + dr + ds) / 2;
        }

        /// <summary>
        /// Calculates the distance between two coordinates in O(1) time.
        /// </summary>
        public static int Distance(HexCoord a, HexCoord b) => a.DistanceTo(b);

        /// <summary>
        /// Converts Unity Hexagon Point Top offset coordinates (Odd-R: odd rows shifted right) to axial HexCoord.
        /// Matches Unity Tilemap cell positions Vector3Int(col, row, 0).
        /// </summary>
        public static HexCoord FromOddR(int col, int row)
        {
            int q = col - ((row - (row & 1)) / 2);
            int r = row;
            return new HexCoord(q, r);
        }

        /// <summary>
        /// Converts this axial HexCoord into Unity Hexagon Point Top offset coordinates (Odd-R).
        /// </summary>
        public (int col, int row) ToOddR()
        {
            int col = Q + ((R - (R & 1)) / 2);
            int row = R;
            return (col, row);
        }

        /// <summary>
        /// Rotates this axial coordinate around the origin (0, 0) clockwise by the specified number of 60-degree steps.
        /// Pure integer cubic arithmetic (steps modulo 6).
        /// </summary>
        public HexCoord RotateCw(int steps = 1)
        {
            int mod = (steps % 6 + 6) % 6;
            int x = Q;
            int z = R;
            int y = -x - z;

            switch (mod)
            {
                case 0: return this;
                case 1: return new HexCoord(-y, -x); // 60° CW:  (x', y', z') = (-y, -z, -x) => Q' = -y, R' = -x
                case 2: return new HexCoord(z, y);   // 120° CW: (x', y', z') = (z, x, y)   => Q' = z,  R' = y
                case 3: return new HexCoord(-x, -z); // 180°:    (x', y', z') = (-x, -y, -z) => Q' = -x, R' = -z
                case 4: return new HexCoord(y, x);   // 240° CW: (x', y', z') = (y, z, x)   => Q' = y,  R' = x
                case 5: return new HexCoord(-z, -y); // 300° CW: (x', y', z') = (-z, -x, -y) => Q' = -z, R' = -y
                default: return this;
            }
        }

        /// <summary>
        /// Rotates this axial coordinate around the origin (0, 0) counter-clockwise by the specified number of 60-degree steps.
        /// </summary>
        public HexCoord RotateCcw(int steps = 1) => RotateCw(-steps);

        // --- Arithmetic Operators ---

        public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
        public static HexCoord operator *(HexCoord a, int scalar) => new(a.Q * scalar, a.R * scalar);
        public static HexCoord operator *(int scalar, HexCoord a) => new(a.Q * scalar, a.R * scalar);
        public static bool operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        // --- Equality & String Representation ---

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Q, R);

        public override string ToString() => $"Hex({Q}, {R})";
    }
}
