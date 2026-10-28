using System.Collections.Generic;

/// <summary>
/// High-level route genom NavigationWorld.
///
/// Den beskriver vilka regions och portals en agent behöver
/// passera.
///
/// Den innehåller INTE den lokala cell-A*-pathen.
/// </summary>
public sealed class NavigationWorldPath
{
    private readonly List<NavigationRegion>
        regions =
            new();

    private readonly List<NavigationRegionPortal>
        portals =
            new();

    public IReadOnlyList<NavigationRegion>
        Regions =>
            regions;

    public IReadOnlyList<NavigationRegionPortal>
        Portals =>
            portals;

    public bool IsValid =>
        regions.Count > 0;

    public bool RequiresRegionTransition =>
        portals.Count > 0;

    public int RegionCount =>
        regions.Count;

    public int PortalCount =>
        portals.Count;

    public void SetRoute(
        IEnumerable<NavigationRegion> newRegions,
        IEnumerable<NavigationRegionPortal> newPortals)
    {
        regions.Clear();
        portals.Clear();

        if (newRegions != null)
        {
            regions.AddRange(
                newRegions
            );
        }

        if (newPortals != null)
        {
            portals.AddRange(
                newPortals
            );
        }
    }

    public NavigationRegion GetRegion(
        int index)
    {
        if (regions.Count == 0)
            return null;

        index =
            UnityEngine.Mathf.Clamp(
                index,
                0,
                regions.Count - 1
            );

        return regions[index];
    }

    public NavigationRegionPortal GetPortal(
        int index)
    {
        if (portals.Count == 0)
            return null;

        index =
            UnityEngine.Mathf.Clamp(
                index,
                0,
                portals.Count - 1
            );

        return portals[index];
    }

    public void Clear()
    {
        regions.Clear();
        portals.Clear();
    }
}
