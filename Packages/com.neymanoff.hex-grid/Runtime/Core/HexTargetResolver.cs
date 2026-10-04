using System;
using System.Collections.Generic;

namespace Neymanoff.HexGrid.Core
{
    /// <summary>
    /// Pure C# geometric targeting and area-of-effect resolver for pointy-top hexagonal grids.
    /// Provides deterministic coordinate sets for lines, symmetrical cones, blast areas, and rings
    /// without physics raycasts or scene dependencies.
    /// </summary>
    public static class HexTargetResolver
    {
        /// <summary>
        /// Finds the closest <see cref="HexDirection"/> pointing from <paramref name="from"/> toward <paramref name="to"/>.
        /// In case of equidistant directions, deterministically selects the lowest direction index.
        /// If <paramref name="from"/> equals <paramref name="to"/>, defaults to <see cref="HexDirection.East"/>.
        /// </summary>
        public static HexDirection GetBestDirection(HexCoord from, HexCoord to)
        {
            if (from == to)
                return HexDirection.East;

            HexDirection bestDir = HexDirection.East;
            int bestDistance = int.MaxValue;

            for (int d = 0; d < 6; d++)
            {
                var dir = (HexDirection)d;
                var neighbor = from.Neighbor(dir);
                int dist = neighbor.DistanceTo(to);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestDir = dir;
                }
            }

            return bestDir;
        }

        /// <summary>
        /// Resolves a straight line of cells starting from <paramref name="origin"/> along <paramref name="direction"/>.
        /// </summary>
        /// <param name="origin">The starting coordinate of the line.</param>
        /// <param name="direction">The direction along which the line extends.</param>
        /// <param name="length">Length of the line in cells (excluding origin unless <paramref name="includeOrigin"/> is true).</param>
        /// <param name="includeOrigin">Whether to include <paramref name="origin"/> in the result.</param>
        /// <returns>A list of coordinates forming the line in order from origin outward.</returns>
        public static List<HexCoord> ResolveLine(
            HexCoord origin,
            HexDirection direction,
            int length,
            bool includeOrigin = false)
        {
            if (length <= 0 && !includeOrigin)
                return new List<HexCoord>();

            var result = new List<HexCoord>(Math.Max(0, length) + (includeOrigin ? 1 : 0));
            if (includeOrigin)
                result.Add(origin);

            var dirOffset = HexCoord.GetDirectionVector(direction);
            for (int step = 1; step <= length; step++)
            {
                result.Add(origin + dirOffset * step);
            }

            return result;
        }

        /// <summary>
        /// Resolves a mathematically symmetrical 120-degree cone spreading outward from <paramref name="origin"/>
        /// centered along <paramref name="facing"/>.
        /// At depth d (1..range), the arc contains 2*d + 1 cells, all at exact distance d from origin.
        /// </summary>
        /// <param name="origin">The apex of the cone.</param>
        /// <param name="facing">The forward center direction of the cone.</param>
        /// <param name="range">Maximum range/depth of the cone.</param>
        /// <param name="includeOrigin">Whether to include the apex (<paramref name="origin"/>) in the result.</param>
        /// <returns>A list of coordinates covered by the cone.</returns>
        public static List<HexCoord> ResolveCone(
            HexCoord origin,
            HexDirection facing,
            int range,
            bool includeOrigin = false)
        {
            if (range <= 0 && !includeOrigin)
                return new List<HexCoord>();

            int totalCapacity = (range > 0 ? range * (range + 2) : 0) + (includeOrigin ? 1 : 0);
            var result = new List<HexCoord>(totalCapacity);

            if (includeOrigin)
                result.Add(origin);

            if (range <= 0)
                return result;

            var dirLeft = facing.RotateCounterClockwise(1);
            var walkDir1 = facing.RotateClockwise(1);
            var walkDir2 = facing.RotateClockwise(2);

            var leftUnit = HexCoord.GetDirectionVector(dirLeft);

            for (int d = 1; d <= range; d++)
            {
                var current = origin + leftUnit * d;
                result.Add(current);

                for (int j = 0; j < d; j++)
                {
                    current = current.Neighbor(walkDir1);
                    result.Add(current);
                }

                for (int j = 0; j < d; j++)
                {
                    current = current.Neighbor(walkDir2);
                    result.Add(current);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves a hexagonal blast area centered at <paramref name="center"/> with the specified <paramref name="radius"/>.
        /// </summary>
        /// <param name="center">Center of the area.</param>
        /// <param name="radius">Radius of the blast area (>= 0).</param>
        /// <param name="includeCenter">Whether to include the center coordinate in the result.</param>
        /// <returns>A list of coordinates within the radius.</returns>
        public static List<HexCoord> ResolveArea(
            HexCoord center,
            int radius,
            bool includeCenter = true)
        {
            if (radius < 0)
                return new List<HexCoord>();

            if (radius == 0)
                return includeCenter ? new List<HexCoord> { center } : new List<HexCoord>();

            int capacity = 3 * radius * (radius + 1) + (includeCenter ? 1 : 0);
            var result = new List<HexCoord>(capacity);

            for (int dq = -radius; dq <= radius; dq++)
            {
                int rMin = Math.Max(-radius, -dq - radius);
                int rMax = Math.Min(radius, -dq + radius);
                for (int dr = rMin; dr <= rMax; dr++)
                {
                    if (!includeCenter && dq == 0 && dr == 0)
                        continue;

                    result.Add(new HexCoord(center.Q + dq, center.R + dr));
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves a 1-cell thick perimeter ring at exact distance <paramref name="radius"/> from <paramref name="center"/>.
        /// </summary>
        /// <param name="center">Center of the ring.</param>
        /// <param name="radius">Radius/distance of the ring.</param>
        /// <returns>A list of coordinates forming the perimeter ring.</returns>
        public static List<HexCoord> ResolveRing(HexCoord center, int radius)
        {
            if (radius < 0)
                return new List<HexCoord>();

            if (radius == 0)
                return new List<HexCoord> { center };

            var result = new List<HexCoord>(6 * radius);

            // Start at center + NorthWest * radius
            var startOffset = HexCoord.GetDirectionVector(HexDirection.NorthWest) * radius;
            var current = center + startOffset;

            for (int side = 0; side < 6; side++)
            {
                var dir = (HexDirection)side;
                for (int step = 0; step < radius; step++)
                {
                    result.Add(current);
                    current = current.Neighbor(dir);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves target cells for the specified <see cref="TargetShape"/>.
        /// </summary>
        /// <param name="origin">The casting origin / center.</param>
        /// <param name="targetOrFacing">The primary target coordinate or facing direction coordinate.</param>
        /// <param name="shape">The geometric targeting shape.</param>
        /// <param name="range">
        /// For Line/Cone: length/depth.
        /// For SingleCell/Area: maximum casting range from origin (if > 0, validates target distance).
        /// </param>
        /// <param name="radius">
        /// For Area: blast radius around target (if 0, defaults to range when targeting self, or 0).
        /// For Ring: ring radius around target.
        /// </param>
        /// <returns>A list of coordinates affected by the target shape.</returns>
        public static List<HexCoord> Resolve(
            HexCoord origin,
            HexCoord targetOrFacing,
            TargetShape shape,
            int range,
            int radius = 0)
        {
            switch (shape)
            {
                case TargetShape.SingleCell:
                {
                    if (range > 0 && origin.DistanceTo(targetOrFacing) > range)
                        return new List<HexCoord>();

                    return new List<HexCoord> { targetOrFacing };
                }

                case TargetShape.Line:
                {
                    var dir = GetBestDirection(origin, targetOrFacing);
                    return ResolveLine(origin, dir, range, includeOrigin: false);
                }

                case TargetShape.Cone:
                {
                    var facing = GetBestDirection(origin, targetOrFacing);
                    return ResolveCone(origin, facing, range, includeOrigin: false);
                }

                case TargetShape.Area:
                {
                    if (range > 0 && origin.DistanceTo(targetOrFacing) > range)
                        return new List<HexCoord>();

                    int effectiveRadius = radius > 0 ? radius : (targetOrFacing == origin ? range : 0);
                    return ResolveArea(targetOrFacing, effectiveRadius, includeCenter: true);
                }

                case TargetShape.Ring:
                {
                    int ringRadius = radius > 0 ? radius : range;
                    if (range > 0 && radius > 0 && origin.DistanceTo(targetOrFacing) > range)
                        return new List<HexCoord>();

                    return ResolveRing(targetOrFacing, ringRadius);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported target shape.");
            }
        }
    }
}
