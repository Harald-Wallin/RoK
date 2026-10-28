using System.Collections.Generic;
using UnityEngine;

public static class BuildingWallRunBuilder
{
    private readonly struct Segment
    {
        public readonly int Level;
        public readonly BuildingEdgeDirection Direction;
        public readonly float Fixed;
        public readonly float Start;
        public readonly float End;

        public Segment(
            int level,
            BuildingEdgeDirection direction,
            float fixedCoordinate,
            float start,
            float end)
        {
            Level = level;
            Direction = direction;
            Fixed = fixedCoordinate;
            Start = Mathf.Min(start, end);
            End = Mathf.Max(start, end);
        }
    }

    public static List<BuildingWallRun> BuildRuns(
        IReadOnlyList<BuildingEdge> edges,
        int level)
    {
        List<Segment> segments = new();

        if (edges == null)
            return new List<BuildingWallRun>();

        for (int i = 0; i < edges.Count; i++)
        {
            BuildingEdge edge = edges[i];

            if (edge.Cell.Level != level)
                continue;

            Vector2 start = edge.LocalStart;
            Vector2 end = edge.LocalEnd;

            bool horizontal =
                edge.Direction == BuildingEdgeDirection.North ||
                edge.Direction == BuildingEdgeDirection.South;

            segments.Add(
                horizontal
                    ? new Segment(
                        level,
                        edge.Direction,
                        start.y,
                        start.x,
                        end.x
                    )
                    : new Segment(
                        level,
                        edge.Direction,
                        start.x,
                        start.y,
                        end.y
                    )
            );
        }

        segments.Sort(CompareSegments);

        List<BuildingWallRun> runs = new();
        int index = 0;

        while (index < segments.Count)
        {
            Segment first = segments[index];
            float start = first.Start;
            float end = first.End;
            int nextIndex = index + 1;

            while (nextIndex < segments.Count)
            {
                Segment next = segments[nextIndex];

                if (next.Level != first.Level ||
                    next.Direction != first.Direction ||
                    Mathf.Abs(next.Fixed - first.Fixed) > 0.0001f)
                {
                    break;
                }

                if (next.Start > end + 0.0001f)
                    break;

                end = Mathf.Max(end, next.End);
                nextIndex++;
            }

            runs.Add(
                new BuildingWallRun(
                    first.Level,
                    first.Direction,
                    first.Fixed,
                    start,
                    end
                )
            );

            index = nextIndex;
        }

        return runs;
    }

    private static int CompareSegments(
        Segment a,
        Segment b)
    {
        int levelCompare =
            a.Level.CompareTo(b.Level);

        if (levelCompare != 0)
            return levelCompare;

        int directionCompare =
            a.Direction.CompareTo(b.Direction);

        if (directionCompare != 0)
            return directionCompare;

        int fixedCompare =
            a.Fixed.CompareTo(b.Fixed);

        if (fixedCompare != 0)
            return fixedCompare;

        return a.Start.CompareTo(b.Start);
    }
}
