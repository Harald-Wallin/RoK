using System.Collections.Generic;
using UnityEngine;

public static class BuildingColliderBuilder
{
    public const string CollisionRootName =
        "Collision";

    public static void Rebuild(
        BuildingRoot root,
        BuildingFootprint footprint)
    {
        if (root == null ||
            footprint == null)
        {
            return;
        }

        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();

        Transform collisionRoot =
            FindOrCreateChild(
                generatedRoot,
                CollisionRootName
            );

        ClearGeneratedChildren(
            collisionRoot
        );

        List<BuildingEdgeRun> runs =
            BuildingEdgeRunBuilder.BuildRuns(
                footprint.ExteriorEdges,
                root.CollisionStructureLevel
            );

        float thickness =
            root.WallCollisionThickness;

        for (int i = 0; i < runs.Count; i++)
        {
            BuildingEdgeRun run = runs[i];

            if (run.Length <= 0.0001f)
                continue;

            GameObject colliderObject =
                new GameObject(
                    run.Horizontal
                        ? $"Wall_H_{i:00}"
                        : $"Wall_V_{i:00}"
                );

            colliderObject.transform.SetParent(
                collisionRoot,
                false
            );

            int layer =
                LayerMask.NameToLayer(
                    root.CollisionLayerName
                );

            if (layer >= 0)
                colliderObject.layer = layer;

            BoxCollider2D box =
                colliderObject.AddComponent<
                    BoxCollider2D>();

            float centerAlong =
                (
                    run.StartCoordinate +
                    run.EndCoordinate
                ) *
                0.5f;

            if (run.Horizontal)
            {
                colliderObject.transform.localPosition =
                    new Vector3(
                        centerAlong,
                        run.FixedCoordinate,
                        0f
                    );

                box.size =
                    new Vector2(
                        run.Length,
                        thickness
                    );
            }
            else
            {
                colliderObject.transform.localPosition =
                    new Vector3(
                        run.FixedCoordinate,
                        centerAlong,
                        0f
                    );

                box.size =
                    new Vector2(
                        thickness,
                        run.Length
                    );
            }
        }
    }

    private static Transform FindOrCreateChild(
        Transform parent,
        string childName)
    {
        Transform existing =
            parent.Find(childName);

        if (existing != null)
            return existing;

        GameObject child =
            new GameObject(childName);

        child.transform.SetParent(
            parent,
            false
        );

        return child.transform;
    }

    private static void ClearGeneratedChildren(
        Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                parent.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(
                    child.gameObject
                );

                continue;
            }
#endif

            Object.Destroy(
                child.gameObject
            );
        }
    }
}
