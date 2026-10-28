using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingFootprint
{
    private readonly HashSet<BuildingCell> cells = new();
    private readonly List<BuildingEdge> exteriorEdges = new();

    public IReadOnlyCollection<BuildingCell> Cells => cells;
    public IReadOnlyList<BuildingEdge> ExteriorEdges => exteriorEdges;
    public int CellCount => cells.Count;

    public void Clear()
    {
        cells.Clear();
        exteriorEdges.Clear();
    }

    public void BuildFrom(IReadOnlyList<BuildingSection> sections)
    {
        Clear();

        if (sections == null)
            return;

        for (int i = 0; i < sections.Count; i++)
        {
            BuildingSection section = sections[i];

            if (section == null || !section.isActiveAndEnabled)
                continue;

            AddSection(section);
        }

        BuildExteriorEdges();
    }

    public bool Contains(BuildingCell cell)
    {
        return cells.Contains(cell);
    }

    public bool Contains(int level, int x, int y)
    {
        return Contains(new BuildingCell(level, x, y));
    }

    private void AddSection(BuildingSection section)
    {
        Vector2Int origin = section.GridPosition;
        Vector2Int size = section.EffectiveSizeCells;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                cells.Add(
                    new BuildingCell(
                        section.StructureLevel,
                        origin.x + x,
                        origin.y + y
                    )
                );
            }
        }
    }

    private void BuildExteriorEdges()
    {
        exteriorEdges.Clear();

        foreach (BuildingCell cell in cells)
        {
            AddEdgeIfExterior(
                cell,
                BuildingEdgeDirection.North,
                0,
                1
            );

            AddEdgeIfExterior(
                cell,
                BuildingEdgeDirection.East,
                1,
                0
            );

            AddEdgeIfExterior(
                cell,
                BuildingEdgeDirection.South,
                0,
                -1
            );

            AddEdgeIfExterior(
                cell,
                BuildingEdgeDirection.West,
                -1,
                0
            );
        }
    }

    private void AddEdgeIfExterior(
        BuildingCell cell,
        BuildingEdgeDirection direction,
        int neighbourX,
        int neighbourY)
    {
        BuildingCell neighbour =
            cell.Offset(neighbourX, neighbourY);

        if (cells.Contains(neighbour))
            return;

        exteriorEdges.Add(
            new BuildingEdge(
                cell,
                direction
            )
        );
    }
}
