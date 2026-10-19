using UnityEngine;

/// <summary>
/// SpriteRenderer-backend för det förenklade roofsystmet.
///
/// Placement använder automatiska sprite-kanter/corners.
/// Unity-pivotens läge spelar ingen roll.
/// </summary>
public static class BuildingRoofRenderer
{
    public const string RoofsRootName =
        "Roofs";


    public static void Rebuild(
        BuildingRoot root)
    {
        if (root == null)
            return;


        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();


        Transform roofsRoot =
            FindOrCreateChild(
                generatedRoot,
                RoofsRootName
            );


        ClearChildren(
            roofsRoot
        );


        for (int i = 0;
             i < root.Sections.Count;
             i++)
        {
            BuildingSection section =
                root.Sections[i];


            if (section == null ||
                !section.isActiveAndEnabled)
            {
                continue;
            }


            BuildingSectionRoofSettings settings =
                section.RoofSettings;


            if (settings == null ||
                !settings.Enabled ||
                settings.Profile == null)
            {
                continue;
            }


            BuildingRoofBuildPlan plan =
                BuildingRoofPlanner.BuildPlan(
                    section
                );


            if (!plan.IsValid)
            {
#if UNITY_EDITOR

                Debug.LogWarning(
                    $"[BUILDING ROOF] {root.name} / {section.name}: " +
                    $"{plan.Error}",
                    section
                );

#endif

                continue;
            }


            RenderSectionPlan(
                roofsRoot,
                section,
                settings.Profile,
                plan
            );
        }
    }


    private static void RenderSectionPlan(
        Transform roofsRoot,
        BuildingSection section,
        BuildingRoofProfile profile,
        BuildingRoofBuildPlan plan)
    {
        GameObject sectionRootObject =
            new GameObject(
                $"Roof_{section.name}"
            );


        Transform sectionRoot =
            sectionRootObject.transform;


        sectionRoot.SetParent(
            roofsRoot,
            false
        );


        for (int i = 0;
             i < plan.Pieces.Count;
             i++)
        {
            BuildingRoofBuildPiece piece =
                plan.Pieces[i];


            if (piece.Sprite == null)
                continue;


            Transform roleRoot =
                FindOrCreateChild(
                    sectionRoot,
                    piece.Role.ToString()
                );


            GameObject pieceObject =
                new GameObject(
                    $"{piece.Role}_{i:000}"
                );


            Transform pieceTransform =
                pieceObject.transform;


            pieceTransform.SetParent(
                roleRoot,
                false
            );


            SpriteRenderer renderer =
                pieceObject.AddComponent<
                    SpriteRenderer>();


            renderer.sprite =
                piece.Sprite;


            renderer.flipX =
                piece.FlipX;


            renderer.flipY =
                piece.FlipY;


            if (!string.IsNullOrWhiteSpace(
                    profile.SortingLayerName))
            {
                renderer.sortingLayerName =
                    profile.SortingLayerName;
            }


            renderer.sortingOrder =
                piece.SortingOrder;


            pieceTransform.localPosition =
                GetTransformPositionForAnchor(
                    piece
                );
        }
    }


    // =========================================================
    // AUTOMATIC ANCHOR PLACEMENT
    // =========================================================

    private static Vector3 GetTransformPositionForAnchor(
        BuildingRoofBuildPiece piece)
    {
        Sprite sprite =
            piece.Sprite;


        Vector2 sourceAnchorPixel =
            GetSourceAnchorPixel(
                sprite,
                piece.Anchor,
                piece.AnchorPixelOffset,
                piece.FlipX,
                piece.FlipY
            );


        Vector2 sourceOffsetFromPivot =
            sourceAnchorPixel -
            sprite.pivot;


        /*
         * SpriteRenderer flippar geometrin runt sprite-pivoten.
         * Därför måste även offset-vektorn speglas innan vi räknar
         * baklänges till Transform-positionen.
         */
        if (piece.FlipX)
        {
            sourceOffsetFromPivot.x *=
                -1f;
        }


        if (piece.FlipY)
        {
            sourceOffsetFromPivot.y *=
                -1f;
        }


        float ppu =
            Mathf.Max(
                0.0001f,
                sprite.pixelsPerUnit
            );


        Vector2 renderedOffset =
            sourceOffsetFromPivot /
            ppu;


        Vector2 transformLocal =
            piece.TargetAnchorLocal -
            renderedOffset;


        return
            new Vector3(
                transformLocal.x,
                transformLocal.y,
                0f
            );
    }


    /// <summary>
    /// Anchor-namnet beskriver hörnet/kanten på den RENDERADE
    /// spriten efter flip.
    ///
    /// Vi översätter därför tillbaka till rätt source-pixel först.
    /// </summary>
    private static Vector2 GetSourceAnchorPixel(
        Sprite sprite,
        BuildingSpriteAnchor renderedAnchor,
        Vector2 renderedAnchorPixelOffset,
        bool flipX,
        bool flipY)
    {
        Vector2 renderedPixel =
            GetAnchorPixel(
                sprite,
                renderedAnchor
            ) +
            renderedAnchorPixelOffset;


        if (flipX)
        {
            renderedPixel.x =
                sprite.rect.width -
                renderedPixel.x;
        }


        if (flipY)
        {
            renderedPixel.y =
                sprite.rect.height -
                renderedPixel.y;
        }


        return renderedPixel;
    }


    private static Vector2 GetAnchorPixel(
        Sprite sprite,
        BuildingSpriteAnchor anchor)
    {
        float width =
            sprite.rect.width;


        float height =
            sprite.rect.height;


        switch (anchor)
        {
            case BuildingSpriteAnchor.TopLeft:
                return new Vector2(
                    0f,
                    height
                );

            case BuildingSpriteAnchor.TopCenter:
                return new Vector2(
                    width *
                    0.5f,
                    height
                );

            case BuildingSpriteAnchor.TopRight:
                return new Vector2(
                    width,
                    height
                );

            case BuildingSpriteAnchor.MiddleLeft:
                return new Vector2(
                    0f,
                    height *
                    0.5f
                );

            case BuildingSpriteAnchor.Center:
                return new Vector2(
                    width *
                    0.5f,
                    height *
                    0.5f
                );

            case BuildingSpriteAnchor.MiddleRight:
                return new Vector2(
                    width,
                    height *
                    0.5f
                );

            case BuildingSpriteAnchor.BottomLeft:
                return Vector2.zero;

            case BuildingSpriteAnchor.BottomCenter:
                return new Vector2(
                    width *
                    0.5f,
                    0f
                );

            case BuildingSpriteAnchor.BottomRight:
                return new Vector2(
                    width,
                    0f
                );
        }


        return Vector2.zero;
    }


    // =========================================================
    // HIERARCHY
    // =========================================================

    private static Transform FindOrCreateChild(
        Transform parent,
        string childName)
    {
        Transform existing =
            parent.Find(
                childName
            );


        if (existing != null)
            return existing;


        GameObject child =
            new GameObject(
                childName
            );


        child.transform.SetParent(
            parent,
            false
        );


        return child.transform;
    }


    private static void ClearChildren(
        Transform parent)
    {
        if (parent == null)
            return;


        for (int i =
                 parent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                parent.GetChild(
                    i
                );


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
