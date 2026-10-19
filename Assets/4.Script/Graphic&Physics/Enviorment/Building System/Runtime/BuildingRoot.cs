using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BuildingRoot :
    MonoBehaviour
{
    public const string AuthoringRootName =
        "Authoring";

    public const string GeneratedRootName =
        "Generated";

    public const string ManualRootName =
        "Manual";


    // =========================================================
    // GENERATION
    // =========================================================

    [Header("Generation")]

    [SerializeField]
    private bool generateRoofs =
        true;


    // =========================================================
    // COLLISION
    // =========================================================

    [Header("Collision")]

    [SerializeField]
    [Min(1)]
    private int wallCollisionThicknessPixels =
        8;


    [SerializeField]
    private string collisionLayerName =
        "World";


    [SerializeField]
    private int collisionStructureLevel;


    [SerializeField]
    private bool generateCollision =
        true;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawFootprint =
        true;


    [SerializeField]
    private bool drawExteriorEdges =
        true;


    // =========================================================
    // RUNTIME / DERIVED
    // =========================================================

    private readonly List<BuildingSection>
        sections =
            new();


    private readonly BuildingFootprint
        footprint =
            new();


    private bool isRebuilding;


#if UNITY_EDITOR

    private bool rebuildQueued;

#endif


    // =========================================================
    // PUBLIC
    // =========================================================

    public BuildingFootprint Footprint =>
        footprint;


    public IReadOnlyList<BuildingSection>
        Sections =>
            sections;


    public int CollisionStructureLevel =>
        collisionStructureLevel;


    public string CollisionLayerName =>
        collisionLayerName;


    public float WallCollisionThickness =>
        wallCollisionThicknessPixels /
        (float)BuildingGrid.PixelsPerUnit;


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }

#endif


        RebuildModel();
    }


    private void OnValidate()
    {
        wallCollisionThicknessPixels =
            Mathf.Max(
                1,
                wallCollisionThicknessPixels
            );


        collisionStructureLevel =
            Mathf.Max(
                0,
                collisionStructureLevel
            );


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }

#endif


        RebuildModel();
    }


#if UNITY_EDITOR

    // =========================================================
    // EDITOR REBUILD QUEUE
    // =========================================================

    public void QueueEditorRebuild()
    {
        if (Application.isPlaying ||
            rebuildQueued)
        {
            return;
        }


        rebuildQueued =
            true;


        EditorApplication.delayCall +=
            ExecuteQueuedEditorRebuild;
    }


    private void ExecuteQueuedEditorRebuild()
    {
        rebuildQueued =
            false;


        if (this == null)
            return;


        RebuildModel();


        EditorUtility.SetDirty(
            this
        );


        SceneView.RepaintAll();
    }

#endif


    // =========================================================
    // MODEL
    // =========================================================

    public void RebuildModel()
    {
        if (isRebuilding)
            return;


        isRebuilding =
            true;


        CollectSections();


        footprint.BuildFrom(
            sections
        );


        if (generateCollision)
        {
            BuildingColliderBuilder.Rebuild(
                this,
                footprint
            );
        }


        if (generateRoofs)
        {
            BuildingRoofRenderer.Rebuild(
                this
            );
        }


        isRebuilding =
            false;
    }


    private void CollectSections()
    {
        sections.Clear();


        BuildingSection[] found =
            GetComponentsInChildren<
                BuildingSection
            >(true);


        for (int i = 0;
             i < found.Length;
             i++)
        {
            BuildingSection section =
                found[i];


            if (section == null)
                continue;


            if (section.Root !=
                this)
            {
                continue;
            }


            sections.Add(
                section
            );
        }
    }


    // =========================================================
    // HIERARCHY
    // =========================================================

    public Transform GetOrCreateAuthoringRoot()
    {
        return GetOrCreateNamedRoot(
            AuthoringRootName
        );
    }


    public Transform GetOrCreateGeneratedRoot()
    {
        return GetOrCreateNamedRoot(
            GeneratedRootName
        );
    }


    public Transform GetOrCreateManualRoot()
    {
        return GetOrCreateNamedRoot(
            ManualRootName
        );
    }


    private Transform GetOrCreateNamedRoot(
        string rootName)
    {
        Transform existing =
            transform.Find(
                rootName
            );


        if (existing != null)
            return existing;


        GameObject child =
            new GameObject(
                rootName
            );


        child.transform.SetParent(
            transform,
            false
        );


        return child.transform;
    }


#if UNITY_EDITOR

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (footprint.CellCount == 0)
        {
            RebuildModel();
        }


        if (drawFootprint)
        {
            DrawFootprintGizmos();
        }


        if (drawExteriorEdges)
        {
            DrawExteriorEdgeGizmos();
        }
    }


    private void DrawFootprintGizmos()
    {
        Matrix4x4 previousMatrix =
            Gizmos.matrix;


        Gizmos.matrix =
            transform.localToWorldMatrix;


        float step =
            BuildingGrid.StructuralStep;


        foreach (BuildingCell cell
                 in footprint.Cells)
        {
            if (cell.Level != 0)
                continue;


            Vector3 center =
                new Vector3(
                    (
                        cell.X +
                        0.5f
                    ) *
                    step,

                    (
                        cell.Y +
                        0.5f
                    ) *
                    step,

                    0f
                );


            Gizmos.DrawWireCube(
                center,
                new Vector3(
                    step,
                    step,
                    0f
                )
            );
        }


        Gizmos.matrix =
            previousMatrix;
    }


    private void DrawExteriorEdgeGizmos()
    {
        Matrix4x4 previousMatrix =
            Gizmos.matrix;


        Gizmos.matrix =
            transform.localToWorldMatrix;


        for (int i = 0;
             i <
             footprint.ExteriorEdges.Count;
             i++)
        {
            BuildingEdge edge =
                footprint.ExteriorEdges[i];


            if (edge.Cell.Level != 0)
                continue;


            Gizmos.DrawLine(
                edge.LocalStart,
                edge.LocalEnd
            );
        }


        Gizmos.matrix =
            previousMatrix;
    }

#endif
}
