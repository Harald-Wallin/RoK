using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Authoring marker for a generated building opening.
///
/// V1 behaviour:
/// - Drag the GameObject with Unity's normal Move Tool.
/// - The marker resolves to the nearest valid 64px exterior wall slot.
/// - North/South doors use a configurable passable width (default 34px).
/// - East/West doors use a configurable passable height (default 64px).
/// - The wall artwork itself remains intact; only collision is opened.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BuildingOpening : MonoBehaviour
{
    [Header("Passage")]

    [SerializeField]
    [Range(1, 64)]
    [Tooltip(
        "Passable width in pixels when the opening resolves to a North/South wall. " +
        "The door artwork may still be 64px wide; this only controls the collider gap."
    )]
    private int horizontalPassageWidthPixels = 34;

    [SerializeField]
    [Range(1, 64)]
    [Tooltip(
        "Passable height in pixels when the opening resolves to an East/West wall. " +
        "Default 64px = two 32px side-wall cells."
    )]
    private int sidePassageHeightPixels = 64;

    [Header("Debug")]

    [SerializeField]
    private bool drawOpeningGizmo = true;


    public BuildingRoot Root =>
        GetComponentInParent<BuildingRoot>();

    public int HorizontalPassageWidthPixels =>
        Mathf.Clamp(horizontalPassageWidthPixels, 1, 64);

    public int SidePassageHeightPixels =>
        Mathf.Clamp(sidePassageHeightPixels, 1, 64);


    private void OnEnable()
    {
        Sanitize();
        transform.hasChanged = false;
        NotifyRoot();
    }


    private void OnValidate()
    {
        Sanitize();
        NotifyRoot();
    }


    private void OnTransformParentChanged()
    {
        NotifyRoot();
    }


#if UNITY_EDITOR

    private void Update()
    {
        if (Application.isPlaying ||
            !transform.hasChanged)
        {
            return;
        }

        transform.hasChanged = false;
        NotifyRoot();
    }

#endif


    public float GetPassageSizeWorld(
        BuildingEdgeDirection direction)
    {
        int pixels =
            direction == BuildingEdgeDirection.North ||
            direction == BuildingEdgeDirection.South
                ? HorizontalPassageWidthPixels
                : SidePassageHeightPixels;

        return
            pixels /
            (float)BuildingGrid.PixelsPerUnit;
    }


    /// <summary>
    /// Called by BuildingRoot after the footprint is rebuilt.
    /// Keeps the authoring marker itself on the resolved 64px wall slot,
    /// so moving a door remains a normal scene-handle workflow.
    /// </summary>

    public void SnapToFootprint(
        BuildingFootprint footprint)
    {
#if UNITY_EDITOR

        // Snapping belongs to authoring, not gameplay.
        //
        // Prevent door markers from moving when Unity
        // enters or runs Play Mode.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        BuildingRoot root =
            Root;

        if (root == null ||
            footprint == null ||
            !BuildingOpeningResolver.TryResolve(
                this,
                root,
                footprint,
                out BuildingResolvedOpening resolved))
        {
            return;
        }

        Vector3 local =
            root.transform.InverseTransformPoint(
                transform.position
            );

        Vector3 targetLocal =
            new Vector3(
                resolved.LocalCenter.x,
                resolved.LocalCenter.y,
                local.z
            );

        Vector3 targetWorld =
            root.transform.TransformPoint(
                targetLocal
            );

        if ((transform.position - targetWorld)
                .sqrMagnitude <= 0.0000001f)
        {
            transform.hasChanged = false;
            return;
        }

        Undo.RecordObject(
            transform,
            "Snap Building Opening"
        );

        transform.position =
            targetWorld;

        transform.hasChanged = false;

#endif
    }



    private void Sanitize()
    {
        horizontalPassageWidthPixels =
            Mathf.Clamp(
                horizontalPassageWidthPixels,
                1,
                64
            );

        sidePassageHeightPixels =
            Mathf.Clamp(
                sidePassageHeightPixels,
                1,
                64
            );
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

    private void OnDrawGizmosSelected()
    {
        if (!drawOpeningGizmo)
            return;

        BuildingRoot root =
            Root;

        if (root == null ||
            root.Footprint == null ||
            root.Footprint.CellCount == 0)
        {
            return;
        }

        if (!BuildingOpeningResolver.TryResolve(
                this,
                root,
                root.Footprint,
                out BuildingResolvedOpening resolved))
        {
            return;
        }

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Color previousColor =
            Gizmos.color;

        Gizmos.matrix =
            root.transform.localToWorldMatrix;

        Gizmos.color =
            new Color(
                1f,
                0.65f,
                0.1f,
                1f
            );

        float passage =
            GetPassageSizeWorld(
                resolved.Direction
            );

        Vector3 size =
            resolved.Horizontal
                ? new Vector3(
                    passage,
                    0.05f,
                    0f
                )
                : new Vector3(
                    0.05f,
                    passage,
                    0f
                );

        Gizmos.DrawWireCube(
            resolved.LocalCenter,
            size
        );

        Gizmos.DrawWireSphere(
            resolved.LocalCenter,
            BuildingGrid.FineStep *
            0.35f
        );

        Gizmos.color =
            previousColor;

        Gizmos.matrix =
            previousMatrix;
    }

#endif
}


/// <summary>
/// Shared resolved representation consumed by both visual generation
/// and collision generation. Keeping this central avoids duplicated
/// door-placement math.
/// </summary>
public readonly struct BuildingResolvedOpening
{
    public readonly BuildingEdgeDirection Direction;
    public readonly float FixedCoordinate;
    public readonly float CenterCoordinate;
    public readonly float RunStartCoordinate;
    public readonly float RunEndCoordinate;
    public readonly Vector2 LocalCenter;

    public BuildingResolvedOpening(
        BuildingEdgeDirection direction,
        float fixedCoordinate,
        float centerCoordinate,
        float runStartCoordinate,
        float runEndCoordinate,
        Vector2 localCenter)
    {
        Direction = direction;
        FixedCoordinate = fixedCoordinate;
        CenterCoordinate = centerCoordinate;
        RunStartCoordinate = runStartCoordinate;
        RunEndCoordinate = runEndCoordinate;
        LocalCenter = localCenter;
    }

    public bool Horizontal =>
        Direction == BuildingEdgeDirection.North ||
        Direction == BuildingEdgeDirection.South;
}


/// <summary>
/// Resolves an authored opening marker to the nearest valid 64px exterior
/// wall slot. The same resolver is used by Structure and Collision.
/// </summary>
public static class BuildingOpeningResolver
{
    private const int DoorSlotPixels = 64;
    private const float Epsilon = 0.0001f;


    public static bool TryResolve(
        BuildingOpening opening,
        BuildingRoot root,
        BuildingFootprint footprint,
        out BuildingResolvedOpening resolved)
    {
        resolved = default;

        if (opening == null ||
            root == null ||
            footprint == null)
        {
            return false;
        }

        List<BuildingWallRun> runs =
            BuildingWallRunBuilder.BuildRuns(
                footprint.ExteriorEdges,
                root.StructureLevel
            );

        if (runs == null ||
            runs.Count == 0)
        {
            return false;
        }

        Vector3 local3 =
            root.transform.InverseTransformPoint(
                opening.transform.position
            );

        Vector2 desiredLocal =
            new Vector2(
                local3.x,
                local3.y
            );

        float slotSpan =
            DoorSlotPixels /
            (float)BuildingGrid.PixelsPerUnit;

        bool found = false;
        float bestDistanceSquared =
            float.PositiveInfinity;

        for (int i = 0;
             i < runs.Count;
             i++)
        {
            BuildingWallRun run =
                runs[i];

            if (run.Length + Epsilon <
                slotSpan)
            {
                continue;
            }

            int slotCount =
                Mathf.FloorToInt(
                    (run.Length + Epsilon) /
                    slotSpan
                );

            if (slotCount <= 0)
                continue;

            float desiredAlong =
                run.Horizontal
                    ? desiredLocal.x
                    : desiredLocal.y;

            float firstCenter =
                run.StartCoordinate +
                slotSpan *
                0.5f;

            int nearestSlot =
                Mathf.RoundToInt(
                    (desiredAlong - firstCenter) /
                    slotSpan
                );

            nearestSlot =
                Mathf.Clamp(
                    nearestSlot,
                    0,
                    slotCount - 1
                );

            float centerAlong =
                firstCenter +
                nearestSlot *
                slotSpan;

            Vector2 candidate =
                run.Horizontal
                    ? new Vector2(
                        centerAlong,
                        run.FixedCoordinate
                    )
                    : new Vector2(
                        run.FixedCoordinate,
                        centerAlong
                    );

            float distanceSquared =
                (candidate - desiredLocal)
                    .sqrMagnitude;

            if (distanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared =
                distanceSquared;

            found = true;

            resolved =
                new BuildingResolvedOpening(
                    run.Direction,
                    run.FixedCoordinate,
                    centerAlong,
                    run.StartCoordinate,
                    run.EndCoordinate,
                    candidate
                );
        }

        return found;
    }
}
