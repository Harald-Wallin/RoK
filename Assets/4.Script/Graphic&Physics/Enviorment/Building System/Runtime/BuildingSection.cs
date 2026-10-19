using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// En rektangulär authored byggnadssektion.
///
/// BuildingRoot får placeras fritt i världen.
/// BuildingSection använder däremot BuildingRoots lokala
/// 32px structural grid.
///
/// Unitys vanliga Transform/Move Tool är authoring-verktyget:
/// om sectionens Transform flyttas i editorn översätts positionen
/// automatiskt tillbaka till GridPosition och snappar till närmaste
/// structural cell.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BuildingSection :
    MonoBehaviour
{
    // =========================================================
    // GRID
    // =========================================================

    [Header("Grid")]

    [SerializeField]
    private Vector2Int gridPosition;


    [SerializeField]
    [Min(1)]
    private int widthCells =
        6;


    [SerializeField]
    [Min(1)]
    private int depthCells =
        6;


    [SerializeField]
    [Tooltip(
        "Framtidssäkring för flera byggnadshöjder/våningar. " +
        "Använd level 0 tills vidare."
    )]
    private int structureLevel;


    [SerializeField]
    private bool rotate90Degrees;


    // =========================================================
    // STRUCTURE
    // =========================================================

    [Header("Structure")]

    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Strukturell vägghöjd i pixlar. Takets baseline genereras " +
        "automatiskt på denna höjd. Standard för RöK: 96 px."
    )]
    private int wallHeightPixels =
        96;


    // =========================================================
    // ROOF
    // =========================================================

    [Header("Roof")]

    [SerializeField]
    private BuildingSectionRoofSettings roofSettings =
        new();


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawSectionCells =
        true;


    [SerializeField]
    private bool drawSectionBounds =
        true;


    // =========================================================
    // PUBLIC
    // =========================================================

    public Vector2Int GridPosition =>
        gridPosition;

    public int WidthCells =>
        widthCells;

    public int DepthCells =>
        depthCells;

    public int StructureLevel =>
        structureLevel;

    public bool Rotate90Degrees =>
        rotate90Degrees;

    public int WallHeightPixels =>
        wallHeightPixels;

    public float WallHeightWorld =>
        wallHeightPixels /
        (float)BuildingGrid.PixelsPerUnit;

    public BuildingSectionRoofSettings RoofSettings =>
        roofSettings;


    public Vector2Int EffectiveSizeCells =>
        rotate90Degrees
            ? new Vector2Int(
                depthCells,
                widthCells
            )
            : new Vector2Int(
                widthCells,
                depthCells
            );


    public BuildingRoot Root =>
        GetComponentInParent<
            BuildingRoot>();


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        Sanitize();
        SyncTransformToGrid();
        NotifyRoot();
    }


    private void OnValidate()
    {
        Sanitize();
        SyncTransformToGrid();
        NotifyRoot();
    }


    private void OnTransformParentChanged()
    {
        SyncTransformToGrid();
        NotifyRoot();
    }


#if UNITY_EDITOR

    private void Update()
    {
        if (Application.isPlaying)
            return;

        SyncGridFromEditorTransform();
    }

#endif


    // =========================================================
    // AUTHORING API
    // =========================================================

    public void SetGridPosition(
        Vector2Int position)
    {
        if (gridPosition ==
            position)
        {
            SyncTransformToGrid();
            return;
        }


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            Undo.RecordObject(
                this,
                "Move Building Section"
            );
        }

#endif


        gridPosition =
            position;


        SyncTransformToGrid();
        NotifyRoot();
    }


    public void SetSizeCells(
        int width,
        int depth)
    {
        widthCells =
            Mathf.Max(
                1,
                width
            );

        depthCells =
            Mathf.Max(
                1,
                depth
            );

        NotifyRoot();
    }


    public void SetStructureLevel(
        int level)
    {
        structureLevel =
            Mathf.Max(
                0,
                level
            );

        NotifyRoot();
    }


    public void SetRotated90(
        bool rotated)
    {
        if (rotate90Degrees ==
            rotated)
        {
            return;
        }

        rotate90Degrees =
            rotated;

        NotifyRoot();
    }


    public void SyncTransformToGrid()
    {
        BuildingRoot root =
            Root;

        if (root == null)
            return;


        Vector2 snappedLocal =
            BuildingGrid.GridToLocal(
                gridPosition
            );


        Vector3 rootLocalPosition =
            root.transform
                .InverseTransformPoint(
                    transform.position
                );


        rootLocalPosition.x =
            snappedLocal.x;

        rootLocalPosition.y =
            snappedLocal.y;


        transform.position =
            root.transform.TransformPoint(
                rootLocalPosition
            );
    }


#if UNITY_EDITOR

    private void SyncGridFromEditorTransform()
    {
        BuildingRoot root =
            Root;

        if (root == null)
            return;


        Vector3 rootLocal3 =
            root.transform
                .InverseTransformPoint(
                    transform.position
                );


        Vector2 rootLocal =
            new Vector2(
                rootLocal3.x,
                rootLocal3.y
            );


        Vector2 expectedLocal =
            BuildingGrid.GridToLocal(
                gridPosition
            );


        if ((rootLocal -
             expectedLocal)
                .sqrMagnitude <=
            0.000001f)
        {
            return;
        }


        Vector2Int snappedGrid =
            BuildingGrid.LocalToGrid(
                rootLocal
            );


        if (snappedGrid !=
            gridPosition)
        {
            Undo.RecordObject(
                this,
                "Move Building Section"
            );


            gridPosition =
                snappedGrid;


            EditorUtility.SetDirty(
                this
            );


            NotifyRoot();
        }


        SyncTransformToGrid();
    }

#endif


    // =========================================================
    // INTERNAL
    // =========================================================

    private void Sanitize()
    {
        widthCells =
            Mathf.Max(
                1,
                widthCells
            );

        depthCells =
            Mathf.Max(
                1,
                depthCells
            );

        structureLevel =
            Mathf.Max(
                0,
                structureLevel
            );

        wallHeightPixels =
            Mathf.Max(
                1,
                wallHeightPixels
            );

        if (roofSettings == null)
        {
            roofSettings =
                new BuildingSectionRoofSettings();
        }
    }


    private void NotifyRoot()
    {
        BuildingRoot root =
            Root;

        if (root == null)
            return;


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            root.QueueEditorRebuild();
            return;
        }

#endif


        root.RebuildModel();
    }


#if UNITY_EDITOR

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        BuildingRoot root =
            Root;

        if (root == null)
            return;


        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Color previousColor =
            Gizmos.color;


        Gizmos.matrix =
            root.transform.localToWorldMatrix;


        Vector2Int size =
            EffectiveSizeCells;

        float step =
            BuildingGrid.StructuralStep;


        if (drawSectionCells)
        {
            Gizmos.color =
                new Color(
                    0.25f,
                    0.85f,
                    1f,
                    0.85f
                );


            for (int y = 0;
                 y < size.y;
                 y++)
            {
                for (int x = 0;
                     x < size.x;
                     x++)
                {
                    Vector3 center =
                        new Vector3(
                            (
                                gridPosition.x +
                                x +
                                0.5f
                            ) *
                            step,

                            (
                                gridPosition.y +
                                y +
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
            }
        }


        if (drawSectionBounds)
        {
            Gizmos.color =
                new Color(
                    0f,
                    1f,
                    0.35f,
                    1f
                );


            Vector3 center =
                new Vector3(
                    (
                        gridPosition.x +
                        size.x *
                        0.5f
                    ) *
                    step,

                    (
                        gridPosition.y +
                        size.y *
                        0.5f
                    ) *
                    step,

                    0f
                );


            Vector3 sectionSize =
                new Vector3(
                    size.x *
                    step,

                    size.y *
                    step,

                    0f
                );


            Gizmos.DrawWireCube(
                center,
                sectionSize
            );


            Vector3 origin =
                new Vector3(
                    gridPosition.x *
                    step,

                    gridPosition.y *
                    step,

                    0f
                );


            Gizmos.DrawWireSphere(
                origin,
                BuildingGrid.FineStep *
                0.35f
            );
        }


        Gizmos.color =
            previousColor;

        Gizmos.matrix =
            previousMatrix;
    }

#endif
}
