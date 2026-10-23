using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One generated sorting controller per BuildingRoot.
/// Stores semantic local depth anchors assigned by the structure builder.
/// Updates renderers only when the BuildingRoot transform changes.
/// No per-piece MonoBehaviours or manual Inspector setup.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class BuildingWorldSorting : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public SpriteRenderer Renderer;
        public Vector2 AnchorLocal;
        public int RelativeOrder;

        public Entry(
            SpriteRenderer renderer,
            Vector2 anchorLocal,
            int relativeOrder)
        {
            Renderer = renderer;
            AnchorLocal = anchorLocal;
            RelativeOrder = relativeOrder;
        }
    }

    [SerializeField, HideInInspector]
    private Transform buildingRoot;

    [SerializeField, HideInInspector]
    private List<Entry> entries = new();

    private Matrix4x4 lastRootMatrix;
    private bool needsRefresh = true;
    private bool reportedOutOfRange;

    public void BeginRebuild(Transform root)
    {
        buildingRoot = root;
        entries.Clear();
        needsRefresh = true;
        reportedOutOfRange = false;
    }

    public void Register(
        SpriteRenderer renderer,
        Vector2 localDepthAnchor,
        int relativeOrder)
    {
        if (renderer == null)
            return;

        entries.Add(new Entry(
            renderer,
            localDepthAnchor,
            relativeOrder
        ));

        needsRefresh = true;
    }

    public void RefreshSorting()
    {
        if (buildingRoot == null)
            return;

        if (!TryFindWorldSortingLayer(out int layerId))
        {
#if UNITY_EDITOR
            Debug.LogError(
                "[BUILDING SORTING] Missing Sorting Layer: " +
                "'Characters' (current) or 'World' (planned).",
                this
            );
#endif
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];

            if (entry.Renderer == null)
                continue;

            float worldY = buildingRoot.TransformPoint(
                entry.AnchorLocal
            ).y;

            int order = WorldSorting.CalculateOrder(
                worldY,
                0f,
                entry.RelativeOrder
            );

#if UNITY_EDITOR
            if (!reportedOutOfRange &&
                !WorldSorting.IsWithinSortingRange(order))
            {
                reportedOutOfRange = true;
                Debug.LogWarning(
                    $"[BUILDING SORTING] {buildingRoot.name} exceeds Unity's " +
                    "sortingOrder range. Current precision 100 is not " +
                    "sufficient for this world position.",
                    buildingRoot
                );
            }
#endif

            entry.Renderer.sortingLayerID = layerId;
            entry.Renderer.sortingOrder = Mathf.Clamp(
                order,
                WorldSorting.MinSortingOrder,
                WorldSorting.MaxSortingOrder
            );
        }

        lastRootMatrix = buildingRoot.localToWorldMatrix;
        needsRefresh = false;
    }

    private void OnEnable()
    {
        needsRefresh = true;
    }

    private void LateUpdate()
    {
        if (buildingRoot == null)
            return;

        if (needsRefresh ||
            buildingRoot.localToWorldMatrix != lastRootMatrix)
        {
            RefreshSorting();
        }
    }

    private static bool TryFindWorldSortingLayer(out int id)
    {
        // During migration, Characters remains the shared world layer.
        // After renaming Characters -> World, this automatically adapts.
        SortingLayer[] layers = SortingLayer.layers;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == "World")
            {
                id = layers[i].id;
                return true;
            }
        }

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == "Characters")
            {
                id = layers[i].id;
                return true;
            }
        }

        id = 0;
        return false;
    }
}
