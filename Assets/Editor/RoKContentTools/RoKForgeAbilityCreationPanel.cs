
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unsaved Ability authoring workspace.
///
/// Shares Ability Settings and action property
/// editing with existing AbilityData assets.
///
/// No project assets are created until Create
/// Ability is explicitly confirmed.
/// </summary>
public sealed class RoKForgeAbilityCreationPanel
{
    private readonly RoKForgeAbilitySettingsEditor
        settingsEditor = new();

    private readonly RoKContentStudioActionComposer
        actionEditor = new();

    private readonly List<AbilityEffect> actions =
        new();

    private readonly List<AbilityEffect> temporaryActions =
        new();

    private AbilityData draft;

    private RoKForgeAbilityTemplate template =
        RoKForgeAbilityTemplate.Blank;

    private int selectedActionTemplate;

    private AbilityEffect existingActionToAdd;

    private AbilityEffect expandedAction;

    private bool showSettings = true;

    public AbilityData Draft => draft;

    public bool UseTwoColumns { get; set; }


    // =====================================================
    // BEGIN
    // =====================================================

    public void Begin()
    {
        Cancel();

        // Deliberately created from a user-triggered
        // Editor action, never a field initializer.

        draft = RoKForgeDraftSession
            .CreateTemporary<AbilityData>();


        draft.name = "New Ability";


        draft.hideFlags = HideFlags.DontSave;


        draft.abilityName = "New Ability";
        draft.types = AbilityType.Spell;

        template =
            RoKForgeAbilityTemplate.Blank;

        showSettings = true;

        InitializeDefaults();
    }

    // =====================================================
    // INITIAL SETTINGS
    // =====================================================

    private void InitializeDefaults()
    {
        if (draft == null)
            return;

        SerializedObject serialized =
            new SerializedObject(draft);

        SetEnum(
            serialized,
            "targetingSettings.targetingMode",
            (int)TargetingMode.SingleTarget
        );

        SetInt(
            serialized,
            "targetingSettings.allowedRelations",
            (int)TargetRelation.Hostile
        );

        SetFloat(
            serialized,
            "targetingSettings.range",
            6f
        );

        SetInt(
            serialized,
            "targetingSettings.maximumTargets",
            1
        );

        SetInt(
            serialized,
            "targetingSettings.targetLayers",
            LayerMask.GetMask("Hitbox", "hitbox")
        );

        SetEnum(
            serialized,
            "timingSettings.timingType",
            (int)ActionTimingType.Instant
        );

        SetEnum(
            serialized,
            "executionSettings.activationMode",
            (int)ActionActivationMode.Immediate
        );

        SetFloat(
            serialized,
            "executionSettings.cooldown",
            5f
        );

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================
    // DRAW
    // =====================================================

    public void Draw(
        Action<AbilityData> onCreated,
        Action onCancelled)
    {
        if (draft == null)
        {
            EditorGUILayout.HelpBox(
                "No active Ability draft.",
                MessageType.Warning
            );

            return;
        }

        bool locked =
            EditorApplication.isPlayingOrWillChangePlaymode;

        if (locked)
        {
            EditorGUILayout.HelpBox(
                "Creation is disabled during Play Mode.",
                MessageType.Warning
            );
        }

        using (new EditorGUI.DisabledScope(locked))
        {
            DrawTemplateSelector();

            EditorGUILayout.Space(12);

            showSettings =
                EditorGUILayout.Foldout(
                    showSettings,
                    "ABILITY CONFIGURATION",
                    true
                );

            if (showSettings)
            {
                settingsEditor.Draw(draft, UseTwoColumns);
            }   

            EditorGUILayout.Space(10);

            DrawActionList();

            EditorGUILayout.Space(8);

            DrawAddActions();
        }

        EditorGUILayout.Space(12);

        DrawFooter(
            onCreated,
            onCancelled,
            locked
        );
    }

    // =====================================================
    // STARTING TEMPLATE
    // =====================================================

    private void DrawTemplateSelector()
    {
        EditorGUILayout.LabelField(
            "STARTING TEMPLATE",
            EditorStyles.boldLabel
        );

        RoKForgeAbilityTemplate selected =
            (RoKForgeAbilityTemplate)
            EditorGUILayout.EnumPopup(
                "Template",
                template
            );

        if (selected != template)
        {
            ApplyTemplate(selected);
        }

        EditorGUILayout.HelpBox(
            template switch
            {
                RoKForgeAbilityTemplate.Blank =>
                    "Start with no actions. " +
                    "Configure the ability freely.",

                RoKForgeAbilityTemplate.DirectDamage =>
                    "Creates an editable Direct Damage " +
                    "action with 50 Fire Damage.",

                RoKForgeAbilityTemplate
                    .DirectDamageWithSelfBuff =>
                    "Creates Direct Damage and Apply Buff. " +
                    "Link an existing BuffDefinition below.",

                _ => string.Empty
            },
            MessageType.None
        );
    }

    private void ApplyTemplate(
        RoKForgeAbilityTemplate selected)
    {
        // Switching templates resets only the
        // draft action list, not ability settings,
        // name, icon or targeting configuration.

        actions.Clear();
        expandedAction = null;

        template = selected;

        switch (template)
        {
            case RoKForgeAbilityTemplate.DirectDamage:
                AddTemporaryAction(
                    typeof(DamageEffect)
                );
                break;

            case RoKForgeAbilityTemplate
                .DirectDamageWithSelfBuff:

                AddTemporaryAction(
                    typeof(DamageEffect)
                );

                AddTemporaryAction(
                    typeof(ApplyBuffDefinitionEffect)
                );
                break;
        }

        GUI.changed = true;
    }

    // =====================================================
    // ACTION LIST
    // =====================================================

    private void DrawActionList()
    {
        EditorGUILayout.LabelField(
            $"ABILITY ACTIONS ({actions.Count})",
            EditorStyles.boldLabel
        );

        if (actions.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No actions added.\n\n" +
                "You can create a blank ability " +
                "or add actions below.",
                MessageType.Info
            );
        }

        int removeIndex = -1;
        int moveFrom = -1;
        int moveTo = -1;

        for (int i = 0; i < actions.Count; i++)
        {
            AbilityEffect action = actions[i];

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Action {i + 1}",
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button(
                    "↑",
                    GUILayout.Width(28)))
                {
                    moveFrom = i;
                    moveTo = i - 1;
                }
            }

            using (new EditorGUI.DisabledScope(
                i == actions.Count - 1))
            {
                if (GUILayout.Button(
                    "↓",
                    GUILayout.Width(28)))
                {
                    moveFrom = i;
                    moveTo = i + 1;
                }
            }

            if (GUILayout.Button(
                "Remove",
                GUILayout.Width(65)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            if (action != null)
            {
                EditorGUILayout.LabelField(
                    action.name,
                    EditorStyles.boldLabel
                );

                EditorGUILayout.LabelField(
                    action.GetType().Name +
                    (
                        AssetDatabase.Contains(action)
                            ? " · Existing Asset"
                            : " · New Draft"
                    ),
                    EditorStyles.miniLabel
                );

                if (GUILayout.Button(
                    expandedAction == action
                        ? "Hide Settings"
                        : "Edit Settings"))
                {
                    expandedAction =
                        expandedAction == action
                            ? null
                            : action;
                }


                if (expandedAction == action)
                {
                    EditorGUILayout.Space(5);

                    if (AssetDatabase.Contains(action))
                    {
                        EditorGUILayout.HelpBox(
                            "This is a shared Action asset.\n\n" +
                            "Its settings are read-only during " +
                            "Ability creation.\n\n" +
                            "Open the existing asset in The Forge " +
                            "to edit its shared configuration.",
                            MessageType.Info
                        );
                    }
                    else
                    {
                        actionEditor.DrawInlineActionEditor(
                            action
                        );
                    }
                }

            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
        }

        // Apply operations after drawing the list.
        if (removeIndex >= 0)
        {
            if (expandedAction == actions[removeIndex])
                expandedAction = null;

            actions.RemoveAt(removeIndex);

            GUI.changed = true;
        }
        else if (moveFrom >= 0)
        {
            AbilityEffect moving =
                actions[moveFrom];

            actions.RemoveAt(moveFrom);

            actions.Insert(
                moveTo,
                moving
            );

            GUI.changed = true;
        }
    }

    // =====================================================
    // ADD ACTIONS
    // =====================================================

    private void DrawAddActions()
    {
        EditorGUILayout.LabelField(
            "ADD ABILITY ACTION",
            EditorStyles.boldLabel
        );

        IReadOnlyList<RoKActionTemplate> templates =
            RoKContentStudioActionFactory.Templates;

        string[] names =
            new string[templates.Count];

        for (int i = 0; i < templates.Count; i++)
        {
            names[i] = templates[i].Name;
        }

        selectedActionTemplate =
            EditorGUILayout.Popup(
                "New Action Type",
                selectedActionTemplate,
                names
            );

        if (GUILayout.Button(
            "+ Create Draft Action",
            GUILayout.Height(28)))
        {
            AddTemporaryAction(
                templates[selectedActionTemplate].ActionType
            );
        }

        EditorGUILayout.Space(6);

        existingActionToAdd =
            (AbilityEffect)
            EditorGUILayout.ObjectField(
                "Existing Action",
                existingActionToAdd,
                typeof(AbilityEffect),
                false
            );

        using (new EditorGUI.DisabledScope(
            existingActionToAdd == null))
        {
            if (GUILayout.Button(
                "+ Link Existing Action",
                GUILayout.Height(28)))
            {
                actions.Add(existingActionToAdd);

                existingActionToAdd = null;

                GUI.changed = true;
            }
        }

        EditorGUILayout.HelpBox(
            "New Draft Action skapar inget asset ännu.\n\n" +
            "Existing Action länkar ett delat asset. " +
            "Ändringar i ett delat asset påverkar " +
            "även andra abilities som använder det.",
            MessageType.None
        );
    }

    // =====================================================
    // TEMPORARY ACTION CREATION
    // =====================================================

    private void AddTemporaryAction(Type actionType)
    {
        RoKActionTemplate actionTemplate =
            RoKForgeAbilityDraftFactory.FindTemplate(
                actionType
            );

        if (actionTemplate == null)
            return;


        AbilityEffect action =
            RoKForgeDraftSession.CreateTemporary(
                actionType
            ) as AbilityEffect;


        if (action == null)
            return;


        action.hideFlags = HideFlags.DontSave;


        action.name =
            actionTemplate.Name;

        SerializedObject serialized =
            new SerializedObject(action);

        SetEnum(
            serialized,
            "targetMode",
            (int)actionTemplate.DefaultTarget
        );

        SetEnum(
            serialized,
            "executionTiming",
            (int)AbilityEffectExecutionTiming.Immediate
        );

        SetBool(
            serialized,
            "requiresCharacterTarget",
            actionTemplate.RequiresCharacterTarget
        );

        if (action is DamageEffect)
        {
            SetInt(
                serialized,
                "flatDamage",
                50
            );

            SetEnum(
                serialized,
                "damageType",
                (int)DamageType.Fire
            );
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        temporaryActions.Add(action);
        actions.Add(action);

        expandedAction = action;

        GUI.changed = true;
    }

    // =====================================================
    // FOOTER / VALIDATION
    // =====================================================

    private void DrawFooter(
        Action<AbilityData> onCreated,
        Action onCancelled,
        bool locked)
    {
        bool valid =
            RoKForgeAbilityDraftFactory.Validate(
                draft,
                actions,
                out string error
            );

        // ApplyBuff draft must reference a buff.
        foreach (AbilityEffect action in actions)
        {
            if (action == null ||
                AssetDatabase.Contains(action) ||
                action is not ApplyBuffDefinitionEffect)
            {
                continue;
            }

            SerializedObject serialized =
                new SerializedObject(action);

            SerializedProperty buff =
                serialized.FindProperty(
                    "buffDefinition"
                );

            if (buff == null ||
                buff.objectReferenceValue == null)
            {
                valid = false;

                error =
                    "Apply Buff behöver en BuffDefinition.";

                break;
            }
        }

        if (!valid)
        {
            EditorGUILayout.HelpBox(
                error,
                MessageType.Warning
            );
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "Cancel",
            GUILayout.Height(34)))
        {
            EditorApplication.delayCall +=
                () => onCancelled?.Invoke();
        }

        using (new EditorGUI.DisabledScope(
            !valid || locked))
        {
            if (GUILayout.Button(
                "Create Ability",
                GUILayout.Height(34)))
            {
                QueueCreate(onCreated);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // =====================================================
    // COMMIT
    // =====================================================

    private void QueueCreate(
        Action<AbilityData> onCreated)
    {
        AbilityData capturedDraft = draft;

        List<AbilityEffect> capturedActions =
            new List<AbilityEffect>(actions);

        EditorApplication.delayCall += () =>
        {
            if (capturedDraft == null ||
                capturedDraft != draft)
            {
                return;
            }

            try
            {
                AbilityData created =
                    RoKForgeAbilityDraftFactory.Create(
                        capturedDraft,
                        capturedActions
                    );

                Cancel();

                onCreated?.Invoke(created);

                Selection.activeObject = created;

                EditorGUIUtility.PingObject(created);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Ability Creation Failed",
                    exception.Message,
                    "OK"
                );
            }
        };
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    public void Cancel()
    {
        actions.Clear();

        expandedAction = null;

        foreach (AbilityEffect action in temporaryActions)
        {
            if (action != null &&
                !AssetDatabase.Contains(action))
            {
                UnityEngine.Object.DestroyImmediate(
                    action
                );
            }
        }

        temporaryActions.Clear();

        if (draft != null)
        {
            UnityEngine.Object.DestroyImmediate(
                draft
            );

            draft = null;
        }
    }

    public void Dispose()
    {
        Cancel();
    }

    // =====================================================
    // SERIALIZATION HELPERS
    // =====================================================

    private static void SetInt(
        SerializedObject serialized,
        string path,
        int value)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property != null)
            property.intValue = value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string path,
        float value)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property != null)
            property.floatValue = value;
    }

    private static void SetBool(
        SerializedObject serialized,
        string path,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property != null)
            property.boolValue = value;
    }

    private static void SetEnum(
        SerializedObject serialized,
        string path,
        int value)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property != null)
            property.enumValueIndex = value;
    }
}

#endif

