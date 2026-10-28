using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor/runtime-representation av ett större land eller en zon.
///
/// NavigationZone delar automatiskt upp sin yta i
/// NavigationRegions.
///
/// Exempel:
///
/// Zone Size:
/// 320 x 256
///
/// Region Size:
/// 32
///
/// =>
///
/// 10 x 8 NavigationRegions.
///
/// Transform-scale används INTE för zonstorleken.
/// Width/Height beskriver istället world-storleken explicit.
/// </summary>
[ExecuteAlways]
public sealed class NavigationZone :
    MonoBehaviour
{
    // =========================================================
    // ZONE SIZE
    // =========================================================

    [Header("Zone")]

    [SerializeField]
    [Min(1f)]
    private float width =
        320f;

    [SerializeField]
    [Min(1f)]
    private float height =
        320f;

    [SerializeField]
    [Min(1f)]
    [Tooltip(
        "Storleken på varje NavigationRegion. " +
        "Ska normalt matcha NavigationWorld.RegionSize."
    )]
    private float regionSize =
        32f;

    // =========================================================
    // REGION PREFAB
    // =========================================================

    [Header("Region Template")]

    [SerializeField]
    [Tooltip(
        "NavigationRegion-prefab som används som mall. " +
        "Cell Size, Agent Radius, World-layer osv tas från prefabben."
    )]
    private NavigationRegion regionPrefab;

    // =========================================================
    // EDITOR AUTOMATION
    // =========================================================

    [Header("Editor Automation")]

    [SerializeField]
    [Tooltip(
        "Synka automatiskt regionerna när zonens storlek ändras."
    )]
    private bool autoSyncInEditor =
        true;

    [SerializeField]
    [Tooltip(
        "Ta bort gamla regions som hamnar utanför zonen " +
        "när zonen krymps."
    )]
    private bool removeUnusedRegions =
        true;

    [Header("Debug")]

    [SerializeField]
    private bool drawZoneBounds =
        true;

    [SerializeField]
    private bool drawRegionLayout =
        true;

#if UNITY_EDITOR

    private bool syncQueued;

#endif

    // =========================================================
    // PUBLIC
    // =========================================================

    public float Width =>
        width;

    public float Height =>
        height;

    public float RegionSize =>
        regionSize;

    public int Columns =>
        Mathf.Max(
            1,
            Mathf.CeilToInt(
                width /
                regionSize
            )
        );

    public int Rows =>
        Mathf.Max(
            1,
            Mathf.CeilToInt(
                height /
                regionSize
            )
        );

    public Vector2 BottomLeft =>
        (Vector2)transform.position -
        new Vector2(
            width * 0.5f,
            height * 0.5f
        );

    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        width =
            Mathf.Max(
                1f,
                width
            );

        height =
            Mathf.Max(
                1f,
                height
            );

        regionSize =
            Mathf.Max(
                1f,
                regionSize
            );

#if UNITY_EDITOR

        if (!Application.isPlaying &&
            autoSyncInEditor)
        {
            QueueEditorSync();
        }

#endif
    }

    // =========================================================
    // REGION GENERATION
    // =========================================================

    [ContextMenu("Sync Navigation Regions")]
    public void SyncRegions()
    {
        if (regionPrefab == null)
        {
            Debug.LogWarning(
                $"NavigationZone '{name}' saknar Region Prefab.",
                this
            );

            return;
        }

        int columns =
            Columns;

        int rows =
            Rows;

        Dictionary<Vector2Int, NavigationRegion>
            existingRegions =
                CollectExistingRegions();

        HashSet<NavigationRegion>
            usedRegions =
                new();

        for (int y = 0;
             y < rows;
             y++)
        {
            for (int x = 0;
                 x < columns;
                 x++)
            {
                Vector2Int coordinate =
                    new Vector2Int(
                        x,
                        y
                    );

                NavigationRegion region;

                if (!existingRegions
                        .TryGetValue(
                            coordinate,
                            out region) ||
                    region == null)
                {
                    region =
                        CreateRegion(
                            coordinate
                        );
                }

                if (region == null)
                    continue;

                ConfigureRegion(
                    region,
                    coordinate
                );

                usedRegions.Add(
                    region
                );
            }
        }

        if (removeUnusedRegions)
        {
            RemoveUnusedRegions(
                usedRegions
            );
        }
    }

    private Dictionary<
        Vector2Int,
        NavigationRegion>
        CollectExistingRegions()
    {
        Dictionary<
            Vector2Int,
            NavigationRegion>
            result =
                new();

        NavigationRegion[] children =
            GetComponentsInChildren<
                NavigationRegion>(
                true
            );

        for (int i = 0;
             i < children.Length;
             i++)
        {
            NavigationRegion region =
                children[i];

            if (region == null ||
                region.transform.parent !=
                transform)
            {
                continue;
            }

            Vector2Int coordinate =
                LocalPositionToCoordinate(
                    region.transform.localPosition
                );

            if (!result.ContainsKey(
                    coordinate))
            {
                result.Add(
                    coordinate,
                    region
                );
            }
        }

        return result;
    }

    private NavigationRegion CreateRegion(
        Vector2Int coordinate)
    {
        NavigationRegion region;

#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    regionPrefab.gameObject,
                    transform
                ) as GameObject;

            if (instance == null)
                return null;

            region =
                instance.GetComponent<
                    NavigationRegion>();

            Undo.RegisterCreatedObjectUndo(
                instance,
                "Create Navigation Region"
            );
        }
        else
#endif
        {
            region =
                Instantiate(
                    regionPrefab,
                    transform
                );
        }

        region.name =
            $"NavigationRegion [{coordinate.x},{coordinate.y}]";

        return region;
    }

    private void ConfigureRegion(
        NavigationRegion region,
        Vector2Int coordinate)
    {
        if (region == null)
            return;

        region.transform.SetParent(
            transform,
            false
        );

        region.transform.localScale =
            Vector3.one;

        region.transform.localRotation =
            Quaternion.identity;

        region.transform.position =
            GetRegionWorldCenter(
                coordinate
            );

        region.SetDimensions(
            regionSize,
            regionSize
        );

        region.name =
            $"NavigationRegion [{coordinate.x},{coordinate.y}]";

#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(
                region
            );

            EditorUtility.SetDirty(
                region.transform
            );
        }

#endif
    }

    private Vector2 GetRegionWorldCenter(
        Vector2Int coordinate)
    {
        /*
         * Zonen centreras kring StartZone-transformen.
         *
         * Om Width/Height inte är exakt delbart med RegionSize
         * kan den sista raden/kolumnen sticka något utanför den
         * visuella zon-bounden.
         *
         * Det är avsiktligt bättre än ett hål i navigationen.
         */

        float totalGridWidth =
            Columns *
            regionSize;

        float totalGridHeight =
            Rows *
            regionSize;

        Vector2 gridBottomLeft =
            (Vector2)transform.position -
            new Vector2(
                totalGridWidth * 0.5f,
                totalGridHeight * 0.5f
            );

        return
            gridBottomLeft +
            new Vector2(
                (
                    coordinate.x +
                    0.5f
                ) *
                regionSize,

                (
                    coordinate.y +
                    0.5f
                ) *
                regionSize
            );
    }

    private Vector2Int LocalPositionToCoordinate(
        Vector3 localPosition)
    {
        float totalGridWidth =
            Columns *
            regionSize;

        float totalGridHeight =
            Rows *
            regionSize;

        Vector2 gridBottomLeft =
            new Vector2(
                -totalGridWidth * 0.5f,
                -totalGridHeight * 0.5f
            );

        Vector2 relative =
            (Vector2)localPosition -
            gridBottomLeft;

        int x =
            Mathf.FloorToInt(
                relative.x /
                regionSize
            );

        int y =
            Mathf.FloorToInt(
                relative.y /
                regionSize
            );

        return new Vector2Int(
            x,
            y
        );
    }

    private void RemoveUnusedRegions(
        HashSet<NavigationRegion> usedRegions)
    {
        NavigationRegion[] children =
            GetComponentsInChildren<
                NavigationRegion>(
                true
            );

        for (int i =
                 children.Length - 1;
             i >= 0;
             i--)
        {
            NavigationRegion region =
                children[i];

            if (region == null ||
                region.transform.parent !=
                transform)
            {
                continue;
            }

            if (usedRegions.Contains(
                    region))
            {
                continue;
            }

#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(
                    region.gameObject
                );

                continue;
            }

#endif

            Destroy(
                region.gameObject
            );
        }
    }

    // =========================================================
    // EDITOR AUTO SYNC
    // =========================================================

#if UNITY_EDITOR

    private void QueueEditorSync()
    {
        if (syncQueued)
            return;

        syncQueued =
            true;

        EditorApplication.delayCall +=
            ExecuteQueuedSync;
    }

    private void ExecuteQueuedSync()
    {
        syncQueued =
            false;

        if (this == null ||
            Application.isPlaying ||
            !autoSyncInEditor)
        {
            return;
        }

        SyncRegions();
    }

#endif

    // =========================================================
    // DEBUG
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (drawZoneBounds)
        {
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(
                    width,
                    height,
                    0f
                )
            );
        }

        if (!drawRegionLayout)
            return;

        int columns =
            Columns;

        int rows =
            Rows;

        for (int y = 0;
             y < rows;
             y++)
        {
            for (int x = 0;
                 x < columns;
                 x++)
            {
                Vector2 center =
                    GetRegionWorldCenter(
                        new Vector2Int(
                            x,
                            y
                        )
                    );

                Gizmos.DrawWireCube(
                    center,
                    new Vector3(
                        regionSize,
                        regionSize,
                        0f
                    )
                );
            }
        }
    }

#endif
}
