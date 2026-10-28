using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    private bool generateStructure =
        true;

    [SerializeField]
    private BuildingStructureStyle structureStyle;

    [SerializeField]
    private bool generateRoofs =
        true;


    // =========================================================
    // COLLISION
    // =========================================================

    [Header("Collision")]

    [SerializeField]
    [FormerlySerializedAs("wallCollisionThicknessPixels")]
    [Min(1)]
    [Tooltip(
    "Collision thickness for East / West perimeter edges."
)]
    private int eastWestCollisionThicknessPixels =
    8;


    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Collision thickness for North / South perimeter edges."
    )]
    private int northSouthCollisionThicknessPixels =
        1;


    [Header("Pillar Foot Collision")]

    [SerializeField]
    private bool generatePillarFootColliders =
        true;


    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Width of the small collider generated at each visible pillar foot."
    )]
    private int pillarFootColliderWidthPixels =
        20;


    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Height of the small collider generated at each visible pillar foot."
    )]
    private int pillarFootColliderHeightPixels =
        12;

    [SerializeField]
    private string collisionLayerName =
        "World";

    [SerializeField]
    [Tooltip(
        "Structure V1 och perimeter-collision använder samma level. " +
        "Behåll 0 tills multi-story-systemet byggs ut."
    )]
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

    private readonly List<BuildingOpening>
        openings =
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

    public IReadOnlyList<BuildingOpening>
        Openings =>
            openings;

    public BuildingStructureStyle StructureStyle =>
        structureStyle;

    /// <summary>
    /// Structure V1 använder samma level som perimeter-collisionen.
    /// Detta undviker dubbla Inspector-fält tills multi-story faktiskt
    /// behöver separata generation levels.
    /// </summary>
    public int StructureLevel =>
        collisionStructureLevel;

    public int CollisionStructureLevel =>
        collisionStructureLevel;

    public string CollisionLayerName =>
        collisionLayerName;

    public float EastWestWallCollisionThickness =>
    eastWestCollisionThicknessPixels /
    (float)BuildingGrid.PixelsPerUnit;


    public float NorthSouthWallCollisionThickness =>
        northSouthCollisionThicknessPixels /
        (float)BuildingGrid.PixelsPerUnit;


    public bool GeneratePillarFootColliders =>
        generatePillarFootColliders;


    public float PillarFootColliderWidth =>
        pillarFootColliderWidthPixels /
        (float)BuildingGrid.PixelsPerUnit;


    public float PillarFootColliderHeight =>
        pillarFootColliderHeightPixels /
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
        eastWestCollisionThicknessPixels =
    Mathf.Max(
        1,
        eastWestCollisionThicknessPixels
    );


        northSouthCollisionThicknessPixels =
            Mathf.Max(
                1,
                northSouthCollisionThicknessPixels
            );


        pillarFootColliderWidthPixels =
            Mathf.Max(
                1,
                pillarFootColliderWidthPixels
            );


        pillarFootColliderHeightPixels =
            Mathf.Max(
                1,
                pillarFootColliderHeightPixels
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


        try
        {
            CollectSections();
            CollectOpenings();


            footprint.BuildFrom(
                sections
            );


            SnapOpeningsToFootprint();


            // =========================================================
            // STRUCTURE
            // =========================================================

            if (generateStructure)
            {
                BuildingStructureRenderer.Rebuild(
                    this,
                    footprint
                );
            }
            else
            {
                BuildingStructureRenderer.Clear(
                    this
                );
            }



            // =========================================================
            // COLLISION
            // =========================================================

            if (generateCollision)
            {
                BuildingColliderBuilder.Rebuild(
                    this,
                    footprint
                );
            }
            else
            {
                BuildingColliderBuilder.Clear(
                    this
                );
            }




            // =========================================================
            // ROOF
            // =========================================================

            OcclusionFader[] roofFaders =
                System.Array.Empty<OcclusionFader>();

            if (generateRoofs)
            {
                roofFaders =
                    BuildingRoofRenderer.Rebuild(
                        this
                    );
            }
            else
            {
                BuildingRoofRenderer.Clear(
                    this
                );
            }



            // =========================================================
            // INDOOR ZONE
            // =========================================================

            BuildingIndoorZoneBuilder.Rebuild(
                this,
                footprint,
                roofFaders
            );
        }
        finally
        {
            isRebuilding =
                false;
        }
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


    private void CollectOpenings()
    {
        openings.Clear();

        BuildingOpening[] found =
            GetComponentsInChildren<
                BuildingOpening
            >(true);

        for (int i = 0;
             i < found.Length;
             i++)
        {
            BuildingOpening opening =
                found[i];

            if (opening == null ||
                !opening.isActiveAndEnabled)
            {
                continue;
            }

            if (opening.Root !=
                this)
            {
                continue;
            }

            openings.Add(
                opening
            );
        }
    }


    private void SnapOpeningsToFootprint()
    {
        for (int i = 0;
             i < openings.Count;
             i++)
        {
            BuildingOpening opening =
                openings[i];

            if (opening == null ||
                !opening.isActiveAndEnabled)
            {
                continue;
            }

            opening.SnapToFootprint(
                footprint
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
