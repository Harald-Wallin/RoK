using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldScatterWindow :
    EditorWindow
{
    private enum BrushShape
    {
        Circle,
        Square
    }

    private const float MinimumBrushSize =
        0.1f;

    private const float MinimumDensity =
        0.1f;

    private WorldScatterPalette palette;

    private Transform targetRoot;

    private BrushShape brushShape =
        BrushShape.Circle;

    [SerializeField]
    private float brushSize =
        2f;

    [SerializeField]
    private float brushDensity =
        5f;

    [SerializeField]
    private float minimumSpacing =
        0.05f;

    private readonly List<Vector2>
        currentStrokePositions =
            new();

    private bool isPainting;

    private Vector2 lastPaintPosition;

    private bool hasLastPaintPosition;

    [MenuItem(
        "Tools/World Scatter"
    )]
    public static void OpenWindow()
    {
        WorldScatterWindow window =
            GetWindow<
                WorldScatterWindow
            >();

        window.titleContent =
            new GUIContent(
                "World Scatter"
            );

        window.minSize =
            new Vector2(
                320f,
                400f
            );

        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui +=
            OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -=
            OnSceneGUI;

        EndStroke();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(
            8f
        );

        EditorGUILayout.LabelField(
            "WORLD SCATTER",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(
            8f
        );

        DrawPaletteSection();

        EditorGUILayout.Space(
            12f
        );

        DrawBrushSection();

        EditorGUILayout.Space(
            12f
        );

        DrawTargetSection();

        EditorGUILayout.Space(
            12f
        );

        DrawControls();

        EditorGUILayout.Space(
            12f
        );

        DrawStatus();
    }

    private void DrawPaletteSection()
    {
        EditorGUILayout.LabelField(
            "Palette",
            EditorStyles.boldLabel
        );

        palette =
            (WorldScatterPalette)
            EditorGUILayout.ObjectField(
                "Active Palette",
                palette,
                typeof(
                    WorldScatterPalette
                ),
                false
            );

        if (GUILayout.Button(
                "Create New Palette"
            ))
        {
            CreateNewPalette();
        }

        if (palette != null)
        {
            EditorGUILayout.Space(
                4f
            );

            if (GUILayout.Button(
                    "Select Palette Asset"
                ))
            {
                Selection.activeObject =
                    palette;

                EditorGUIUtility
                    .PingObject(
                        palette
                    );
            }
        }
    }

    private void DrawBrushSection()
    {
        EditorGUILayout.LabelField(
            "Brush",
            EditorStyles.boldLabel
        );

        brushShape =
            (BrushShape)
            EditorGUILayout.EnumPopup(
                "Shape",
                brushShape
            );

        brushSize =
            Mathf.Max(
                MinimumBrushSize,
                EditorGUILayout.FloatField(
                    "Size",
                    brushSize
                )
            );

        brushDensity =
            Mathf.Max(
                MinimumDensity,
                EditorGUILayout.Slider(
                    "Main Density",
                    brushDensity,
                    0.1f,
                    25f
                )
            );

        minimumSpacing =
            Mathf.Max(
                0f,
                EditorGUILayout.FloatField(
                    "Minimum Spacing",
                    minimumSpacing
                )
            );

        EditorGUILayout.HelpBox(
            "Main Density styr hur många spawn-försök " +
            "som görs per world-area. Varje grupp i paletten " +
            "har sedan sin egen Density-procent.",
            MessageType.Info
        );
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.LabelField(
            "Placement",
            EditorStyles.boldLabel
        );

        targetRoot =
            (Transform)
            EditorGUILayout.ObjectField(
                "Target Root",
                targetRoot,
                typeof(Transform),
                true
            );

        if (targetRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Välj ett Target Root i scenen. " +
                "Alla målade prefab-instanser läggs under denna.",
                MessageType.Warning
            );
        }
    }

    private void DrawControls()
    {
        EditorGUILayout.LabelField(
            "Controls",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "LMB",
            "Paint"
        );

        EditorGUILayout.LabelField(
            "LMB + Drag",
            "Paint continuously"
        );

        EditorGUILayout.LabelField(
            "Shift + LMB",
            "Erase scatter objects"
        );

        EditorGUILayout.LabelField(
            "Ctrl/Cmd + Z",
            "Undo"
        );
    }

    private void DrawStatus()
    {
        if (palette == null)
        {
            EditorGUILayout.HelpBox(
                "Ingen palette vald.",
                MessageType.Warning
            );

            return;
        }

        if (targetRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Ingen Target Root vald.",
                MessageType.Warning
            );

            return;
        }

        EditorGUILayout.HelpBox(
            "World Scatter är redo. Måla direkt i Scene View.",
            MessageType.Info
        );
    }

    private void CreateNewPalette()
    {
        string path =
            EditorUtility.SaveFilePanelInProject(
                "Create Scatter Palette",
                "New World Scatter Palette",
                "asset",
                "Välj var paletten ska sparas."
            );

        if (string.IsNullOrWhiteSpace(
                path))
        {
            return;
        }

        WorldScatterPalette newPalette =
            CreateInstance<
                WorldScatterPalette
            >();

        AssetDatabase.CreateAsset(
            newPalette,
            path
        );

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();

        palette =
            newPalette;

        Selection.activeObject =
            newPalette;

        EditorGUIUtility.PingObject(
            newPalette
        );
    }

    private void OnSceneGUI(
        SceneView sceneView)
    {
        if (palette == null ||
            targetRoot == null)
        {
            return;
        }

        Event currentEvent =
            Event.current;

        if (currentEvent == null)
            return;

        Vector3 worldPosition =
            GetMouseWorldPosition(
                currentEvent.mousePosition
            );

        DrawBrushPreview(
            worldPosition,
            currentEvent.shift
        );

        HandlePaintingInput(
            currentEvent,
            worldPosition
        );

        sceneView.Repaint();
    }

    private Vector3 GetMouseWorldPosition(
        Vector2 mousePosition)
    {
        Ray ray =
            HandleUtility.GUIPointToWorldRay(
                mousePosition
            );

        Plane plane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );

        if (plane.Raycast(
                ray,
                out float distance))
        {
            Vector3 position =
                ray.GetPoint(
                    distance
                );

            position.z = 0f;

            return position;
        }

        return Vector3.zero;
    }

    private void DrawBrushPreview(
        Vector3 center,
        bool eraseMode)
    {
        Color previousColor =
            Handles.color;

        Handles.color =
            eraseMode
                ? new Color(
                    1f,
                    0.25f,
                    0.25f,
                    0.9f
                )
                : new Color(
                    0.35f,
                    1f,
                    0.45f,
                    0.9f
                );

        float radius =
            brushSize * 0.5f;

        if (brushShape ==
            BrushShape.Circle)
        {
            Handles.DrawWireDisc(
                center,
                Vector3.forward,
                radius
            );
        }
        else
        {
            Vector3[] corners =
            {
                center +
                new Vector3(
                    -radius,
                    -radius,
                    0f
                ),

                center +
                new Vector3(
                    -radius,
                    radius,
                    0f
                ),

                center +
                new Vector3(
                    radius,
                    radius,
                    0f
                ),

                center +
                new Vector3(
                    radius,
                    -radius,
                    0f
                )
            };

            Handles.DrawAAPolyLine(
                2f,
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                corners[0]
            );
        }

        Handles.color =
            previousColor;
    }

    private void HandlePaintingInput(
        Event currentEvent,
        Vector3 worldPosition)
    {
        if (currentEvent.alt)
        {
            return;
        }

        bool leftMouse =
            currentEvent.button == 0;

        if (currentEvent.type ==
                EventType.MouseDown &&
            leftMouse)
        {
            BeginStroke();

            ApplyBrush(
                worldPosition,
                currentEvent.shift
            );

            lastPaintPosition =
                worldPosition;

            hasLastPaintPosition =
                true;

            currentEvent.Use();

            return;
        }

        if (currentEvent.type ==
                EventType.MouseDrag &&
            leftMouse &&
            isPainting)
        {
            if (ShouldApplyDragStamp(
                    worldPosition))
            {
                ApplyBrush(
                    worldPosition,
                    currentEvent.shift
                );

                lastPaintPosition =
                    worldPosition;

                hasLastPaintPosition =
                    true;
            }

            currentEvent.Use();

            return;
        }

        if ((currentEvent.type ==
                EventType.MouseUp ||
             currentEvent.rawType ==
                EventType.MouseUp) &&
            leftMouse)
        {
            EndStroke();

            currentEvent.Use();
        }
    }

    private void BeginStroke()
    {
        isPainting = true;

        hasLastPaintPosition =
            false;

        currentStrokePositions.Clear();

        Undo.IncrementCurrentGroup();

        Undo.SetCurrentGroupName(
            "World Scatter Stroke"
        );
    }

    private void EndStroke()
    {
        isPainting = false;

        hasLastPaintPosition =
            false;

        currentStrokePositions.Clear();
    }

    private bool ShouldApplyDragStamp(
        Vector2 currentPosition)
    {
        if (!hasLastPaintPosition)
            return true;

        float stampDistance =
            Mathf.Max(
                0.05f,
                brushSize * 0.25f
            );

        return Vector2.Distance(
                   lastPaintPosition,
                   currentPosition
               ) >=
               stampDistance;
    }

    private void ApplyBrush(
        Vector3 center,
        bool eraseMode)
    {
        if (eraseMode)
        {
            EraseInsideBrush(
                center
            );

            return;
        }

        PaintInsideBrush(
            center
        );
    }

    private void PaintInsideBrush(
        Vector3 center)
    {
        if (palette == null ||
            targetRoot == null)
        {
            return;
        }

        float area =
            GetBrushArea();

        int attempts =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    area *
                    brushDensity
                )
            );

        foreach (ScatterGroup group
                 in palette.Groups)
        {
            if (group == null ||
                !group.Enabled ||
                group.Density <= 0f)
            {
                continue;
            }

            for (int i = 0;
                 i < attempts;
                 i++)
            {
                if (UnityEngine.Random.value >
                    group.Density)
                {
                    continue;
                }

                Vector2 position =
                    GetRandomPositionInBrush(
                        center
                    );

                if (!PassesMinimumSpacing(
                        position))
                {
                    continue;
                }

                GameObject prefab =
                    group.GetRandomPrefab();

                if (prefab == null)
                    continue;

                CreatePrefabInstance(
                    prefab,
                    group,
                    position
                );
            }
        }
    }

    private float GetBrushArea()
    {
        float radius =
            brushSize * 0.5f;

        if (brushShape ==
            BrushShape.Circle)
        {
            return Mathf.PI *
                   radius *
                   radius;
        }

        return brushSize *
               brushSize;
    }

    private Vector2 GetRandomPositionInBrush(
        Vector3 center)
    {
        float radius =
            brushSize * 0.5f;

        Vector2 offset;

        if (brushShape ==
            BrushShape.Circle)
        {
            offset =
                UnityEngine.Random
                    .insideUnitCircle *
                radius;
        }
        else
        {
            offset =
                new Vector2(
                    UnityEngine.Random.Range(
                        -radius,
                        radius
                    ),
                    UnityEngine.Random.Range(
                        -radius,
                        radius
                    )
                );
        }

        return new Vector2(
            center.x + offset.x,
            center.y + offset.y
        );
    }

    private bool PassesMinimumSpacing(
        Vector2 position)
    {
        if (minimumSpacing <= 0f)
            return true;

        float squaredMinimumDistance =
            minimumSpacing *
            minimumSpacing;

        for (int i = 0;
             i <
             currentStrokePositions.Count;
             i++)
        {
            Vector2 existing =
                currentStrokePositions[i];

            if ((existing - position)
                    .sqrMagnitude <
                squaredMinimumDistance)
            {
                return false;
            }
        }

        return true;
    }

    private void CreatePrefabInstance(
        GameObject prefab,
        ScatterGroup group,
        Vector2 position)
    {
        if (prefab == null ||
            group == null ||
            targetRoot == null)
        {
            return;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                targetRoot
            ) as GameObject;

        if (instance == null)
            return;

        instance.transform.position =
            new Vector3(
                position.x,
                position.y,
                0f
            );

        ApplyVariation(
            instance,
            group
        );

        ApplyStaticYSorting(
            instance
        );

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Paint World Scatter"
        );

        currentStrokePositions.Add(
            position
        );

        EditorUtility.SetDirty(
            instance
        );

        EditorUtility.SetDirty(
            targetRoot.gameObject
        );
    }

    // =========================================================
    // VARIATION
    // =========================================================

    private static void ApplyVariation(
        GameObject instance,
        ScatterGroup group)
    {
        if (instance == null ||
            group == null)
        {
            return;
        }

        ApplyRandomScale(
            instance,
            group
        );

        ApplyRandomFlipX(
            instance,
            group
        );
    }

    private static void ApplyRandomScale(
        GameObject instance,
        ScatterGroup group)
    {
        if (instance == null ||
            group == null)
        {
            return;
        }

        float randomScale =
            group.GetRandomScale();

        Vector3 originalScale =
            instance.transform.localScale;

        instance.transform.localScale =
            new Vector3(
                originalScale.x *
                randomScale,

                originalScale.y *
                randomScale,

                originalScale.z
            );
    }

    private static void ApplyRandomFlipX(
        GameObject instance,
        ScatterGroup group)
    {
        if (instance == null ||
            group == null ||
            !group.GetRandomFlipX())
        {
            return;
        }

        SpriteRenderer[] renderers =
            instance.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        foreach (SpriteRenderer renderer
                 in renderers)
        {
            if (renderer == null)
                continue;

            /*
             * Vi inverterar prefabens befintliga flipX
             * i stället för att anta att den alltid är false.
             */
            renderer.flipX =
                !renderer.flipX;
        }
    }

    // =========================================================
    // STATIC Y SORTING
    // =========================================================


    private void ApplyStaticYSorting(
        GameObject instance)
    {
        if (instance == null)
            return;

        // =====================================================
        // EXISTING YSORTER
        // =====================================================

        YSorter[] sorters =
            instance.GetComponentsInChildren<YSorter>(
                true
            );

        if (sorters.Length > 0)
        {
            foreach (YSorter sorter in sorters)
            {
                if (sorter == null)
                    continue;


                sorter.ConfigureStaticSorting();

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        sorter
                    );


                SortingGroup group =
                    sorter.GetComponent<SortingGroup>();

                if (group == null)
                    continue;

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        group
                    );
            }

            // Do NOT add global sorting to child sprites.
            return;
        }

        // =====================================================
        // STATIC PREFAB WITHOUT YSORTER
        // =====================================================

        int baseOrder =
            WorldSorting.CalculateOrder(
                instance.transform.position.y
            );

        SortingGroup[] groups =
            instance.GetComponentsInChildren<SortingGroup>(
                true
            );

        if (groups.Length > 0)
        {
            foreach (SortingGroup group in groups)
            {
                if (group == null)
                    continue;

                // Nested groups retain their local ordering.
                bool isNested = false;

                Transform parent =
                    group.transform.parent;

                while (parent != null &&
                       parent.IsChildOf(instance.transform))
                {
                    if (parent.GetComponent<SortingGroup>() != null)
                    {
                        isNested = true;
                        break;
                    }

                    parent = parent.parent;
                }

                if (isNested)
                    continue;

                // Applied once to the newly created instance.
                group.sortingOrder += baseOrder;

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        group
                    );
            }

            return;
        }

        // =====================================================
        // LEGACY SPRITE-ONLY PREFABS
        // =====================================================

        // Preserve the previous behaviour for simple
        // static prefabs without SortingGroup/YSorter.

        SpriteRenderer[] renderers =
            instance.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingOrder += baseOrder;

            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    renderer
                );
        }
    }


    // =========================================================
    // ERASE
    // =========================================================

    private void EraseInsideBrush(
        Vector3 center)
    {
        if (targetRoot == null)
            return;

        List<GameObject> objectsToDelete =
            new();

        CollectObjectsInsideBrush(
            targetRoot,
            center,
            objectsToDelete
        );

        foreach (GameObject objectToDelete
                 in objectsToDelete)
        {
            if (objectToDelete == null)
                continue;

            Undo.DestroyObjectImmediate(
                objectToDelete
            );
        }
    }

    private void CollectObjectsInsideBrush(
        Transform root,
        Vector3 center,
        List<GameObject> results)
    {
        for (int i =
                 root.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                root.GetChild(i);

            if (child == null)
                continue;

            /*
             * Bara direkta scatter-instanser under Target Root
             * ska raderas.
             *
             * Vi går alltså INTE ned och börjar radera child-
             * objekt inuti ett prefab.
             */
            if (IsInsideBrush(
                    child.position,
                    center))
            {
                results.Add(
                    child.gameObject
                );
            }
        }
    }

    private bool IsInsideBrush(
        Vector3 position,
        Vector3 center)
    {
        float radius =
            brushSize * 0.5f;

        Vector2 delta =
            new Vector2(
                position.x -
                center.x,
                position.y -
                center.y
            );

        if (brushShape ==
            BrushShape.Circle)
        {
            return delta.sqrMagnitude <=
                   radius * radius;
        }

        return Mathf.Abs(
                   delta.x
               ) <= radius &&
               Mathf.Abs(
                   delta.y
               ) <= radius;
    }
}