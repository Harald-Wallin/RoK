using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum RoadPainterMode
{
    Draw,
    Edit
}

/// <summary>
/// Huvudfönster för RöK Road Painter.
///
/// Draw:
/// - håll vänster musknapp i Scene View
/// - freehand-path samplas
/// - samplingen förenklas automatiskt
/// - en RoadPath skapas
///
/// Edit:
/// - vald RoadPath redigeras genom RoadPathEditor
/// </summary>
public sealed class RoadPainterWindow :
    EditorWindow
{
    private static RoadPainterWindow instance;


    // =========================================================
    // AUTHORING
    // =========================================================

    [SerializeField]
    private RoadProfile selectedProfile;

    [SerializeField]
    private Transform roadParent;

    [SerializeField]
    private Transform groundClutterRoot;

    [SerializeField]
    private RoadPainterMode mode =
        RoadPainterMode.Draw;


    // =========================================================
    // DRAW SETTINGS
    // =========================================================

    [SerializeField]
    [Min(0.02f)]
    private float pointSpacing =
        0.2f;

    [SerializeField]
    [Min(0f)]
    private float simplificationTolerance =
        0.12f;


    // =========================================================
    // DRAW RUNTIME
    // =========================================================

    private readonly List<Vector2>
        strokePoints =
            new();

    private bool isDrawing;


    // =========================================================
    // PUBLIC EDITOR STATE
    // =========================================================

    public static bool IsDrawModeActive =>
        instance != null &&
        instance.mode ==
            RoadPainterMode.Draw;


    public static RoadPainterMode ActiveMode =>
        instance != null
            ? instance.mode
            : RoadPainterMode.Edit;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "Tools/Road Painter"
    )]
    public static void OpenWindow()
    {
        RoadPainterWindow window =
            GetWindow<RoadPainterWindow>();

        window.titleContent =
            new GUIContent(
                "RöK Road Painter"
            );

        window.minSize =
            new Vector2(
                320f,
                260f
            );

        window.Show();
    }


    // =========================================================
    // UNITY EDITOR
    // =========================================================

    private void OnEnable()
    {
        instance =
            this;

        SceneView.duringSceneGui -=
            HandleSceneGUI;

        SceneView.duringSceneGui +=
            HandleSceneGUI;
    }


    private void OnDisable()
    {
        SceneView.duringSceneGui -=
            HandleSceneGUI;

        if (instance == this)
        {
            instance =
                null;
        }

        CancelStroke();
    }


    // =========================================================
    // WINDOW GUI
    // =========================================================

    private void OnGUI()
    {
        EditorGUILayout.Space(
            6f
        );

        EditorGUILayout.LabelField(
            "RÖK ROAD PAINTER",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(
            8f
        );


        selectedProfile =
            (RoadProfile)
            EditorGUILayout.ObjectField(
                "Road Profile",
                selectedProfile,
                typeof(RoadProfile),
                false
            );


        roadParent =
            (Transform)
            EditorGUILayout.ObjectField(
                new GUIContent(
                    "Road Parent",
                    "Valfri parent för skapade vägar. " +
                    "Om tom används/skapas ett GameObject som heter Roads."
                ),
                roadParent,
                typeof(Transform),
                true
            );

        groundClutterRoot =
            (Transform)
            EditorGUILayout.ObjectField(
            new GUIContent(
                "Ground Clutter Root",
                "Samma Target Root som används i World Scatter. " +
                "Road Painter rensar endast dess direkta children."
            ),
                groundClutterRoot,
                typeof(Transform),
                true
            );


        EditorGUILayout.Space(
            10f
        );


        EditorGUILayout.LabelField(
            "Mode",
            EditorStyles.boldLabel
        );

        int newMode =
            GUILayout.Toolbar(
                (int)mode,
                new[]
                {
                    "DRAW",
                    "EDIT"
                }
            );

        if (newMode !=
            (int)mode)
        {
            mode =
                (RoadPainterMode)
                newMode;

            CancelStroke();

            SceneView.RepaintAll();
        }


        EditorGUILayout.Space(
            10f
        );


        if (mode ==
            RoadPainterMode.Draw)
        {
            DrawDrawSettings();
        }
        else
        {
            DrawEditHelp();
        }


        EditorGUILayout.Space(
            8f
        );

        DrawProfileStatus();

        EditorGUILayout.Space(10f);
        DrawClutterControls();

        DrawRoadNetworkControls();
    }

    private void DrawRoadNetworkControls()
    {
        EditorGUILayout.LabelField(
            "Road Network",
            EditorStyles.boldLabel
        );


        if (roadParent == null)
        {
            EditorGUILayout.HelpBox(
                "Tilldela Road Parent för att använda Road Network.",
                MessageType.Info
            );

            return;
        }


        RoadNetwork network =
            roadParent.GetComponent<
                RoadNetwork
            >();


        if (network == null)
        {
            EditorGUILayout.HelpBox(
                "Road Parent saknar RoadNetwork. " +
                "Lägg till den för automatiska junctions " +
                "och framtida gameplay road queries.",
                MessageType.Warning
            );


            if (GUILayout.Button(
                    "Add Road Network"))
            {
                network =
                    Undo.AddComponent<
                        RoadNetwork
                    >(
                        roadParent.gameObject
                    );


                network.RebuildNetwork();


                EditorUtility.SetDirty(
                    roadParent.gameObject
                );


                SceneView.RepaintAll();
            }


            return;
        }


        EditorGUILayout.LabelField(
            "Roads",
            network
                .RoadCount
                .ToString()
        );


        EditorGUILayout.LabelField(
            "Junctions",
            network
                .JunctionCount
                .ToString()
        );


        if (GUILayout.Button(
                "Rebuild Road Network"))
        {
            network.RebuildNetwork();

            SceneView.RepaintAll();
        }
    }

    private void DrawClutterControls()
    {
        EditorGUILayout.LabelField(
            "Ground Clutter",
            EditorStyles.boldLabel
        );


        RoadPath selectedRoad =
            null;


        if (Selection.activeGameObject != null)
        {
            selectedRoad =
                Selection.activeGameObject
                    .GetComponent<RoadPath>();
        }


        using (new EditorGUI.DisabledScope(
                   selectedRoad == null ||
                   groundClutterRoot == null))
        {
            if (GUILayout.Button(
                    "Clear Clutter For Selected Road"))
            {
                Undo.IncrementCurrentGroup();

                int undoGroup =
                    Undo.GetCurrentGroup();


                Undo.SetCurrentGroupName(
                    "Clear Road Ground Clutter"
                );


                int removedCount =
                    RoadClutterUtility
                        .ClearGroundClutter(
                            selectedRoad,
                            groundClutterRoot
                        );


                Undo.CollapseUndoOperations(
                    undoGroup
                );


                Debug.Log(
                    $"Road Painter removed " +
                    $"{removedCount} ground clutter objects."
                );


                SceneView.RepaintAll();
            }
        }


        if (groundClutterRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Tilldela Ground Clutter Root för att kunna " +
                "rensa WorldScatter-objekt under vägar.",
                MessageType.Info
            );
        }
    }

    private void DrawDrawSettings()
    {
        EditorGUILayout.LabelField(
            "Freehand",
            EditorStyles.boldLabel
        );

        pointSpacing =
            Mathf.Max(
                0.02f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Point Spacing",
                        "Minsta avstånd mellan sampled mouse-points."
                    ),
                    pointSpacing
                )
            );

        simplificationTolerance =
            Mathf.Max(
                0f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Simplification",
                        "Hur aggressivt freehand-samplingen förenklas " +
                        "när musknappen släpps."
                    ),
                    simplificationTolerance
                )
            );

        EditorGUILayout.Space(
            6f
        );

        EditorGUILayout.HelpBox(
            "DRAW: Håll vänster musknapp i Scene View och rita vägen. " +
            "Släpp knappen för att skapa RoadPath.",
            MessageType.Info
        );
    }


    private void DrawEditHelp()
    {
        EditorGUILayout.HelpBox(
            "EDIT: Markera en RoadPath i Hierarchy.\n\n" +
            "• Dra de vita punkterna för att ändra vägen.\n" +
            "• Shift + vänsterklick på vägen lägger till en punkt.\n" +
            "• Shift + högerklick nära en punkt tar bort den.",
            MessageType.Info
        );
    }


    private void DrawProfileStatus()
    {
        if (selectedProfile != null)
        {
            EditorGUILayout.LabelField(
                "Active Road",
                selectedProfile.name
            );

            EditorGUILayout.LabelField(
                "Base Width",
                selectedProfile
                    .BaseWidth
                    .ToString("0.##")
            );

            EditorGUILayout.LabelField(
                "Horizontal Scale",
                selectedProfile
                    .HorizontalWidthScale
                    .ToString("0.##")
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Välj ett Road Profile innan du börjar rita.",
                MessageType.Warning
            );
        }
    }


    // =========================================================
    // SCENE DRAWING
    // =========================================================

    private void HandleSceneGUI(
        SceneView sceneView)
    {
        if (mode !=
                RoadPainterMode.Draw ||
            selectedProfile == null)
        {
            return;
        }

        Event currentEvent =
            Event.current;

        if (currentEvent == null)
            return;


        /*
         * När Draw mode är aktivt äger Road Painter
         * vänsterklicket i Scene View.
         */
        int controlId =
            GUIUtility.GetControlID(
                FocusType.Passive
            );

        HandleUtility.AddDefaultControl(
            controlId
        );


        DrawStrokePreview();


        if (currentEvent.alt)
        {
            return;
        }


        if (currentEvent.type ==
                EventType.KeyDown &&
            currentEvent.keyCode ==
                KeyCode.Escape)
        {
            CancelStroke();

            currentEvent.Use();

            SceneView.RepaintAll();

            return;
        }


        if (currentEvent.button != 0)
            return;


        if (currentEvent.type ==
            EventType.MouseDown)
        {
            BeginStroke(
                currentEvent
            );

            currentEvent.Use();

            return;
        }


        if (currentEvent.type ==
                EventType.MouseDrag &&
            isDrawing)
        {
            ContinueStroke(
                currentEvent
            );

            currentEvent.Use();

            return;
        }


        if (currentEvent.type ==
                EventType.MouseUp &&
            isDrawing)
        {
            FinishStroke(
                currentEvent
            );

            currentEvent.Use();
        }
    }


    private void BeginStroke(
        Event currentEvent)
    {
        if (!TryGetCurrentMouseWorldPoint(
                currentEvent,
                out Vector2 worldPoint))
        {
            return;
        }

        strokePoints.Clear();

        strokePoints.Add(
            worldPoint
        );

        isDrawing =
            true;

        SceneView.RepaintAll();
    }


    private void ContinueStroke(
        Event currentEvent)
    {
        if (!TryGetCurrentMouseWorldPoint(
                currentEvent,
                out Vector2 worldPoint))
        {
            return;
        }

        if (strokePoints.Count == 0)
        {
            strokePoints.Add(
                worldPoint
            );

            return;
        }

        Vector2 previous =
            strokePoints[
                strokePoints.Count - 1
            ];

        float requiredSpacing =
            Mathf.Max(
                0.02f,
                pointSpacing
            );

        if (Vector2.Distance(
                previous,
                worldPoint) <
            requiredSpacing)
        {
            return;
        }

        strokePoints.Add(
            worldPoint
        );

        SceneView.RepaintAll();
    }


    private void FinishStroke(
        Event currentEvent)
    {
        if (TryGetCurrentMouseWorldPoint(
                currentEvent,
                out Vector2 finalPoint))
        {
            if (strokePoints.Count == 0 ||
                Vector2.Distance(
                    strokePoints[
                        strokePoints.Count - 1
                    ],
                    finalPoint
                ) >
                0.01f)
            {
                strokePoints.Add(
                    finalPoint
                );
            }
        }


        if (strokePoints.Count < 2)
        {
            CancelStroke();
            return;
        }


        List<Vector2> simplified =
            RoadPainterUtility
                .SimplifyPoints(
                    strokePoints,
                    simplificationTolerance
                );


        if (simplified.Count >= 2)
        {
            Undo.IncrementCurrentGroup();

            int undoGroup =
                Undo.GetCurrentGroup();


            Undo.SetCurrentGroupName(
                "Draw Road"
            );


            RoadPath createdRoad =
                RoadPainterUtility
                    .CreateRoad(
                        simplified,
                        selectedProfile,
                        roadParent
                    );


            if (createdRoad != null)
            {
                if (selectedProfile != null &&
                    selectedProfile
                        .ClearGroundClutter &&
                    groundClutterRoot != null)
                {
                    int removedCount =
                        RoadClutterUtility
                            .ClearGroundClutter(
                                createdRoad,
                                groundClutterRoot
                            );


                    if (removedCount > 0)
                    {
                        Debug.Log(
                            $"Road Painter removed " +
                            $"{removedCount} ground clutter objects."
                        );
                    }
                }


                mode =
                    RoadPainterMode.Edit;


                Repaint();
            }


            Undo.CollapseUndoOperations(
                undoGroup
            );
        }


        CancelStroke();

        SceneView.RepaintAll();
    }


    private void CancelStroke()
    {
        isDrawing =
            false;

        strokePoints.Clear();
    }


    // =========================================================
    // PREVIEW
    // =========================================================

    private void DrawStrokePreview()
    {
        if (!isDrawing ||
            strokePoints.Count == 0)
        {
            return;
        }

        DrawPreviewCenterLine();
        DrawPreviewWidth();
    }


    private void DrawPreviewCenterLine()
    {
        if (strokePoints.Count < 2)
            return;

        Vector3[] points =
            new Vector3[
                strokePoints.Count
            ];

        for (int i = 0;
             i < strokePoints.Count;
             i++)
        {
            points[i] =
                new Vector3(
                    strokePoints[i].x,
                    strokePoints[i].y,
                    GetDrawingZ()
                );
        }

        Handles.color =
            new Color(
                1f,
                0.75f,
                0.15f,
                1f
            );

        Handles.DrawAAPolyLine(
            3f,
            points
        );
    }


    private void DrawPreviewWidth()
    {
        if (strokePoints.Count < 2)
            return;

        Vector3[] leftEdge =
            new Vector3[
                strokePoints.Count
            ];

        Vector3[] rightEdge =
            new Vector3[
                strokePoints.Count
            ];

        for (int i = 0;
             i < strokePoints.Count;
             i++)
        {
            Vector2 tangent =
                GetStrokeTangent(
                    i
                );

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                );

            float width =
                GetPreviewWidth(
                    tangent
                );

            Vector2 center =
                strokePoints[i];

            Vector2 left =
                center +
                normal *
                width *
                0.5f;

            Vector2 right =
                center -
                normal *
                width *
                0.5f;

            leftEdge[i] =
                new Vector3(
                    left.x,
                    left.y,
                    GetDrawingZ()
                );

            rightEdge[i] =
                new Vector3(
                    right.x,
                    right.y,
                    GetDrawingZ()
                );
        }

        Handles.color =
            new Color(
                1f,
                0.5f,
                0.1f,
                0.75f
            );

        Handles.DrawAAPolyLine(
            2f,
            leftEdge
        );

        Handles.DrawAAPolyLine(
            2f,
            rightEdge
        );
    }


    private Vector2 GetStrokeTangent(
        int index)
    {
        if (strokePoints.Count < 2)
        {
            return
                Vector2.right;
        }

        Vector2 previous;
        Vector2 next;

        if (index <= 0)
        {
            previous =
                strokePoints[0];

            next =
                strokePoints[1];
        }
        else if (
            index >=
            strokePoints.Count - 1)
        {
            previous =
                strokePoints[
                    strokePoints.Count - 2
                ];

            next =
                strokePoints[
                    strokePoints.Count - 1
                ];
        }
        else
        {
            previous =
                strokePoints[index - 1];

            next =
                strokePoints[index + 1];
        }

        Vector2 direction =
            next -
            previous;

        if (direction.sqrMagnitude <=
            0.000001f)
        {
            return
                Vector2.right;
        }

        return
            direction.normalized;
    }


    private float GetPreviewWidth(
        Vector2 direction)
    {
        if (selectedProfile == null)
            return 1f;

        if (direction.sqrMagnitude <=
            0.000001f)
        {
            return
                selectedProfile.BaseWidth;
        }

        direction.Normalize();

        float verticalness =
            Mathf.Abs(
                Vector2.Dot(
                    direction,
                    Vector2.up
                )
            );

        float widthScale =
            Mathf.Lerp(
                selectedProfile
                    .HorizontalWidthScale,
                1f,
                verticalness
            );

        return
            selectedProfile.BaseWidth *
            widthScale;
    }


    // =========================================================
    // WORLD PLANE
    // =========================================================

    private bool TryGetCurrentMouseWorldPoint(
        Event currentEvent,
        out Vector2 worldPoint)
    {
        return
            RoadPainterUtility
                .TryGetMouseWorldPoint(
                    currentEvent.mousePosition,
                    GetDrawingZ(),
                    out worldPoint
                );
    }


    private float GetDrawingZ()
    {
        return
            roadParent != null
                ? roadParent.position.z
                : 0f;
    }
}
