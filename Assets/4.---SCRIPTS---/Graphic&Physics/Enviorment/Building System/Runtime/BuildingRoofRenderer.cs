using UnityEngine;

/// <summary>
/// SpriteRenderer-backend för buildings roofs.
///
/// Roof presentation delas i tre policies:
///
/// Main:
/// - själva stora Surface-spritesen
/// - fade:ar kraftigt indoors
///
/// Details:
/// - ridge
/// - overhangs
/// - front gable
/// - front windboards
/// - fade:ar mindre indoors
///
/// Persistent:
/// - rear windboards
/// - fade:ar aldrig
///
/// Placement använder automatiska sprite-kanter/corners.
/// Unity-pivotens läge spelar ingen roll.
/// </summary>
public static class BuildingRoofRenderer
{
    public const string RoofsRootName =
        "Roofs";


    private const string FadeRootName =
        "Fade";

    private const string MainFadeRootName =
        "Main";

    private const string DetailsFadeRootName =
        "Details";

    private const string PersistentRootName =
        "Persistent";


    // =========================================================
    // FADE SETTINGS
    // =========================================================

    private const float MainRoofOccludedAlpha =
        0.005f;

    private const float RoofDetailsOccludedAlpha =
        0.03f;

    private const float RoofFadeDuration =
        0.50f;


    // =========================================================
    // ENTRY
    // =========================================================


    public static void Clear(
        BuildingRoot root)
    {
        if (root == null)
            return;

        // =====================================================
        // FIND EXISTING ROOF HIERARCHY
        // =====================================================

        Transform generatedRoot =
            root.transform.Find(
                BuildingRoot.GeneratedRootName
            );

        if (generatedRoot == null)
            return;

        Transform roofsRoot =
            generatedRoot.Find(
                RoofsRootName
            );

        if (roofsRoot == null)
            return;

        Transform fadeRoot =
            roofsRoot.Find(
                FadeRootName
            );

        Transform mainRoot =
            fadeRoot != null
                ? fadeRoot.Find(MainFadeRootName)
                : null;

        Transform detailsRoot =
            fadeRoot != null
                ? fadeRoot.Find(DetailsFadeRootName)
                : null;

        Transform persistentRoot =
            roofsRoot.Find(
                PersistentRootName
            );

        // =====================================================
        // REMOVE LEGACY GENERATED CONTENT
        // =====================================================

        ClearChildrenExcept(
            roofsRoot,
            fadeRoot,
            persistentRoot
        );

        if (fadeRoot != null)
        {
            ClearChildrenExcept(
                fadeRoot,
                mainRoot,
                detailsRoot
            );

            RemoveLocalOcclusionFader(
                fadeRoot
            );
        }

        // =====================================================
        // CLEAR MAIN ROOF
        // =====================================================

        if (mainRoot != null)
        {
            ClearChildren(
                mainRoot
            );

            OcclusionFader mainFader =
                mainRoot.GetComponent<OcclusionFader>();

            if (mainFader != null)
            {
                mainFader.ClearOcclusion();
                mainFader.RefreshRenderers();
            }
        }

        // =====================================================
        // CLEAR DETAILS
        // =====================================================

        if (detailsRoot != null)
        {
            ClearChildren(
                detailsRoot
            );

            OcclusionFader detailsFader =
                detailsRoot.GetComponent<OcclusionFader>();

            if (detailsFader != null)
            {
                detailsFader.ClearOcclusion();
                detailsFader.RefreshRenderers();
            }
        }

        // =====================================================
        // CLEAR PERSISTENT ROOF DETAILS
        // =====================================================

        ClearChildren(
            persistentRoot
        );
    }



    public static OcclusionFader[] Rebuild(
        BuildingRoot root)
    {
        if (root == null)
        {
            return
                System.Array.Empty<
                    OcclusionFader>();
        }


        Transform generatedRoot =
            root.GetOrCreateGeneratedRoot();

        // Shared per-building world-depth controller. Structure rebuild
        // has already reset its registrations, when enabled.
        BuildingWorldSorting worldSorting =
            generatedRoot.GetComponent<BuildingWorldSorting>();

        if (worldSorting == null)
        {
            worldSorting =
                generatedRoot.gameObject.AddComponent<BuildingWorldSorting>();

            worldSorting.BeginRebuild(root.transform);
        }

        Transform roofsRoot =
            FindOrCreateChild(
                generatedRoot,
                RoofsRootName
            );


        // =====================================================
        // PRESENTATION ROOTS
        // =====================================================

        Transform fadeRoot =
            FindOrCreateChild(
                roofsRoot,
                FadeRootName
            );


        Transform mainRoot =
            FindOrCreateChild(
                fadeRoot,
                MainFadeRootName
            );


        Transform detailsRoot =
            FindOrCreateChild(
                fadeRoot,
                DetailsFadeRootName
            );


        Transform persistentRoot =
            FindOrCreateChild(
                roofsRoot,
                PersistentRootName
            );


        // =====================================================
        // MIGRATION / CLEANUP
        // =====================================================

        /*
         * Tar bort gamla roof-roots som låg direkt
         * under Roofs innan Fade/Persistent-arkitekturen.
         *
         * Fade och Persistent själva sparas.
         */
        ClearChildrenExcept(
            roofsRoot,
            fadeRoot,
            persistentRoot
        );


        /*
         * Tar bort gammalt genererat content som tidigare
         * låg direkt under Fade.
         *
         * Main och Details sparas.
         */
        ClearChildrenExcept(
            fadeRoot,
            mainRoot,
            detailsRoot
        );


        /*
         * Gamla implementationen hade en OcclusionFader
         * direkt på Fade.
         *
         * Den FÅR INTE finnas kvar nu eftersom den annars
         * skulle fada både Main och Details en extra gång.
         */
        RemoveLocalOcclusionFader(
            fadeRoot
        );


        /*
         * Själva policy-rootsen behålls mellan rebuilds,
         * men deras genererade innehåll byggs om.
         */
        ClearChildren(
            mainRoot
        );


        ClearChildren(
            detailsRoot
        );


        ClearChildren(
            persistentRoot
        );


        // =====================================================
        // FADERS
        // =====================================================

        OcclusionFader mainFader =
            GetOrCreateOcclusionFader(
                mainRoot,
                MainRoofOccludedAlpha
            );


        OcclusionFader detailsFader =
            GetOrCreateOcclusionFader(
                detailsRoot,
                RoofDetailsOccludedAlpha
            );


        // =====================================================
        // SECTIONS
        // =====================================================

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
                mainRoot,
                detailsRoot,
                persistentRoot,
                section,
                settings.Profile,
                plan,
                worldSorting
            );
        }


        // Reapply depth orders after all roof renderers are generated.
        worldSorting.RefreshSorting();

        // =====================================================
        // REFRESH FADERS
        // =====================================================

        /*
         * SpriteRenderers har skapats EFTER att faders
         * skapades.
         *
         * Läs därför om deras renderer-listor nu.
         */
        mainFader.RefreshRenderers();

        detailsFader.RefreshRenderers();


        return new[]
        {
            mainFader,
            detailsFader
        };
    }


    // =========================================================
    // SECTION RENDERING
    // =========================================================

    private static void RenderSectionPlan(
        Transform mainRoot,
        Transform detailsRoot,
        Transform persistentRoot,
        BuildingSection section,
        BuildingRoofProfile profile,
        BuildingRoofBuildPlan plan,
        BuildingWorldSorting worldSorting)
    {
        Transform mainSectionRoot =
            CreateSectionRoot(
                mainRoot,
                section
            );


        Transform detailsSectionRoot =
            CreateSectionRoot(
                detailsRoot,
                section
            );


        Transform persistentSectionRoot =
            CreateSectionRoot(
                persistentRoot,
                section
            );

        // The entire roof assembly shares the section's structural front edge
        // as global depth anchor; piece.SortingOrder remains LOCAL ordering.
        Vector2 roofDepthAnchorLocal = new Vector2(
            section.GridPosition.x * BuildingGrid.StructuralStep,
            section.GridPosition.y * BuildingGrid.StructuralStep
        );

        for (int i = 0;
             i < plan.Pieces.Count;
             i++)
        {
            BuildingRoofBuildPiece piece =
                plan.Pieces[i];


            if (piece.Sprite == null)
                continue;


            Transform sectionRoot;


            /*
             * Rear windboards ligger alltid kvar.
             */
            if (IsPersistentRoofPiece(
                    piece.Role))
            {
                sectionRoot =
                    persistentSectionRoot;
            }
            /*
             * Själva stora roof-surface-spritesen
             * får stark fade.
             */
            else if (IsMainRoofPiece(
                         piece.Role))
            {
                sectionRoot =
                    mainSectionRoot;
            }
            /*
             * All övrig roof presentation:
             *
             * ridge
             * overhang
             * front gable
             * front windboards
             *
             * får mildare fade.
             */
            else
            {
                sectionRoot =
                    detailsSectionRoot;
            }


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


            // Semantics and depth are registered centrally. The controller
            // selects the current shared world Sorting Layer automatically.
            worldSorting.Register(
                renderer,
                roofDepthAnchorLocal,
                piece.SortingOrder
            );


            pieceTransform.localPosition =
                GetTransformPositionForAnchor(
                    piece
                );
        }
    }


    private static Transform CreateSectionRoot(
        Transform parent,
        BuildingSection section)
    {
        GameObject sectionObject =
            new GameObject(
                $"Roof_{section.name}"
            );


        Transform sectionRoot =
            sectionObject.transform;


        sectionRoot.SetParent(
            parent,
            false
        );


        return sectionRoot;
    }


    // =========================================================
    // PRESENTATION POLICY
    // =========================================================

    private static bool IsMainRoofPiece(
        BuildingRoofPieceRole role)
    {
        /*
         * Endast själva stora roof-surface-spritesen
         * räknas som Main.
         */
        return
            role ==
            BuildingRoofPieceRole.Surface;
    }


    private static bool IsPersistentRoofPiece(
        BuildingRoofPieceRole role)
    {
        /*
         * Rear windboards ska ligga kvar ovanpå
         * rear structural gable även när resten
         * av roof presentation fade:ar.
         */
        return
            role ==
                BuildingRoofPieceRole.RearWindboardTop ||
            role ==
                BuildingRoofPieceRole.RearWindboardMiddle ||
            role ==
                BuildingRoofPieceRole.RearWindboardBottom;
    }


    // =========================================================
    // FADERS
    // =========================================================

    private static OcclusionFader GetOrCreateOcclusionFader(
    Transform target,
    float defaultOccludedAlpha)
    {
        OcclusionFader fader =
            target.GetComponent<
                OcclusionFader>();


        /*
         * Finns fadern redan ska vi INTE konfigurera om den.
         *
         * Då får användaren själv justera:
         * - Occluded Alpha Multiplier
         * - Fade Duration
         *
         * i Inspectorn utan att Building rebuild skriver
         * över värdena.
         */
        if (fader != null)
        {
            return fader;
        }


        /*
         * Endast en helt ny fader får våra generator-defaults.
         */
        fader =
            target.gameObject
                .AddComponent<
                    OcclusionFader>();


        fader.ConfigureFade(
            defaultOccludedAlpha,
            RoofFadeDuration
        );


        return fader;
    }


    private static void RemoveLocalOcclusionFader(
        Transform target)
    {
        if (target == null)
            return;


        OcclusionFader fader =
            target.GetComponent<
                OcclusionFader>();


        if (fader == null)
            return;


        /*
         * OnDisable återställer alpha på de renderers
         * den gamla fadern eventuellt ägde.
         */
        fader.enabled =
            false;


#if UNITY_EDITOR

        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(
                fader
            );

            return;
        }

#endif


        Object.Destroy(
            fader
        );
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
         * Därför måste även offset-vektorn speglas innan vi
         * räknar baklänges till Transform-positionen.
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


    /// <summary>
    /// Tar bort alla barn förutom de uttryckligen angivna.
    /// Används vid migration mellan generated-hierarkier.
    /// </summary>
    private static void ClearChildrenExcept(
        Transform parent,
        params Transform[] keep)
    {
        if (parent == null)
            return;


        for (int i =
                 parent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                parent.GetChild(i);


            bool shouldKeep =
                false;


            if (keep != null)
            {
                for (int k = 0;
                     k < keep.Length;
                     k++)
                {
                    if (child != keep[k])
                        continue;


                    shouldKeep =
                        true;

                    break;
                }
            }


            if (shouldKeep)
                continue;


#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(
                    child.gameObject
                );

                continue;
            }

#endif


            child.gameObject.SetActive(
                false
            );


            child.SetParent(
                null,
                false
            );


            Object.Destroy(
                child.gameObject
            );
        }
    }


    /// <summary>
    /// Runtime-safe clear.
    ///
    /// Vi lossar children före Destroy så att en rebuild
    /// under samma frame aldrig råkar återanvända ett objekt
    /// som redan väntar på destruction.
    /// </summary>
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


            child.gameObject.SetActive(
                false
            );


            child.SetParent(
                null,
                false
            );


            Object.Destroy(
                child.gameObject
            );
        }
    }
}