
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Categorized AbilityData settings presentation.
///
/// Reuses existing serialized gameplay settings
/// and PropertyDrawers.
///
/// Does not own gameplay execution logic.
/// </summary>
public sealed class RoKForgeAbilitySettingsEditor
{
    // =====================================================
    // SECTION STATE
    // =====================================================

    private bool showGeneral = true;
    private bool showTargeting = true;
    private bool showTiming = true;
    private bool showCombat = true;

    private bool showLegacy;

    // =====================================================
    // DRAW
    // =====================================================


    public void Draw( AbilityData ability,bool twoColumns = false)
    {
        if (ability == null)
            return;

        SerializedObject serialized =
            new SerializedObject(ability);

        serialized.Update();


        if (twoColumns)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(
                GUILayout.MinWidth(300),
                GUILayout.ExpandWidth(true)
            );
        }


        // =================================================
        // GENERAL
        // =================================================

        DrawGroup(
            ref showGeneral,
            "GENERAL",
            () =>
            {
                Field(
                    serialized,
                    "description",
                    "Description"
                );

                Field(
                    serialized,
                    "usageType",
                    "Usage Type"
                );

                Field(serialized, "advancesWeaponCombo", "Advances Weapon Combo");

                Field(
                    serialized,
                    "tags",
                    "Ability Tags"
                );
            }
        );

        // =================================================
        // TARGETING
        // =================================================

        DrawGroup(
            ref showTargeting,
            "TARGETING",
            () =>
            {
                Field(
                    serialized,
                    "previewMode",
                    "Preview Mode"
                );

                // Existing custom PropertyDrawer handles
                // Circle, Cone, Line, Self etc.
                Field(
                    serialized,
                    "targetingSettings",
                    "Targeting Settings"
                );
            }
        );


        if (twoColumns)
        {
            EditorGUILayout.EndVertical();

            GUILayout.Space(12);

            EditorGUILayout.BeginVertical(
                GUILayout.MinWidth(300),
                GUILayout.ExpandWidth(true)
            );
        }


        // =================================================
        // TIMING AND EXECUTION
        // =================================================

        DrawGroup(
            ref showTiming,
            "TIMING & EXECUTION",
            () =>
            {
                // Existing timing PropertyDrawer already
                // hides irrelevant Cast/Charge/Channel
                // settings.
                Field(
                    serialized,
                    "timingSettings",
                    "Timing Settings"
                );

                DrawExecutionSettings(serialized);

                DrawChargeSettings(serialized);
            }
        );

        // =================================================
        // COMBAT RULES
        // =================================================

        DrawGroup(
            ref showCombat,
            "COMBAT RULES",
            () =>
            {
                Field(
                    serialized,
                    "wardCost",
                    "Ward Cost"
                );

                Field(
                    serialized,
                    "alwaysHits",
                    "Always Hits"
                );

                Field(
                    serialized,
                    "canMiss",
                    "Can Miss"
                );

                Field(
                    serialized,
                    "canCrit",
                    "Can Crit"
                );

                Field(
                    serialized,
                    "entersCombatState",
                    "Enters Combat"
                );
            }
        );

        // =================================================
        // LEGACY / ADVANCED
        // =================================================

        DrawGroup(
            ref showLegacy,
            "ADVANCED / LEGACY",
            () =>
            {
                EditorGUILayout.HelpBox(
                    "Compatibility fields for older " +
                    "ability implementations.\n\n" +
                    "Modern abilities use Targeting, " +
                    "Timing and Execution Settings.\n\n" +
                    "Do not change these unless the " +
                    "ability relies on legacy behavior.",
                    MessageType.Info
                );

                Field(
                    serialized,
                    "cooldown",
                    "Legacy Cooldown"
                );

                Field(
                    serialized,
                    "globalCooldown",
                    "Legacy GCD"
                );

                Field(
                    serialized,
                    "castTime",
                    "Legacy Cast Time"
                );

                Field(
                    serialized,
                    "isSelfCast",
                    "Legacy Self Cast"
                );

                Field(
                    serialized,
                    "requiresHitCheck",
                    "Legacy Hit Check"
                );
            }
        );

        // =================================================
        // COMMIT CHANGES
        // =================================================


        if (twoColumns)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(ability);
        }
    }

    // =====================================================
    // EXECUTION SETTINGS
    // =====================================================

    private static void DrawExecutionSettings(
        SerializedObject serialized)
    {
        SerializedProperty settings =
            serialized.FindProperty(
                "executionSettings"
            );

        if (settings == null)
            return;

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Execution",
            EditorStyles.boldLabel
        );

        Child(
            settings,
            "activationMode",
            "Activation Mode"
        );

        Child(
            settings,
            "cooldown",
            "Cooldown"
        );

        SerializedProperty triggersGcd =
            settings.FindPropertyRelative(
                "triggersGlobalCooldown"
            );

        if (triggersGcd != null)
        {
            EditorGUILayout.PropertyField(
                triggersGcd,
                new GUIContent(
                    "Triggers Global Cooldown"
                )
            );

            if (triggersGcd.boolValue)
            {
                Child(
                    settings,
                    "globalCooldownOverride",
                    "GCD Override"
                );
            }
        }

        Child(
            settings,
            "resolvesTargetHitImmediately",
            "Resolve Hit Immediately"
        );

        SerializedProperty timing =
            serialized.FindProperty(
                "timingSettings.timingType"
            );

        ActionTimingType timingType =
            timing != null
                ? (ActionTimingType)
                    timing.enumValueIndex
                : ActionTimingType.Instant;

        bool hasPreparation =
            timingType == ActionTimingType.Cast ||
            timingType == ActionTimingType.Charge ||
            timingType == ActionTimingType.Channel;

        if (hasPreparation)
        {
            Child(
                settings,
                "canBeCancelled",
                "Can Be Cancelled"
            );

            Child(
                settings,
                "canBeInterrupted",
                "Can Be Interrupted"
            );
        }
    }

    // =====================================================
    // CONDITIONAL CHARGE SETTINGS
    // =====================================================

    private static void DrawChargeSettings(
        SerializedObject serialized)
    {
        SerializedProperty timing =
            serialized.FindProperty(
                "timingSettings.timingType"
            );

        if (timing == null ||
            (ActionTimingType)timing.enumValueIndex !=
                ActionTimingType.Charge)
        {
            return;
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Charge Configuration",
            EditorStyles.boldLabel
        );

        SerializedProperty charge =
            serialized.FindProperty(
                "chargeSettings"
            );

        if (charge != null)
        {
            SerializedProperty scaling =
                charge.FindPropertyRelative(
                    "scalingMode"
                );

            if (scaling != null)
            {
                EditorGUILayout.PropertyField(
                    scaling,
                    new GUIContent(
                        "Scaling Mode"
                    )
                );

                ChargeScalingMode mode =
                    (ChargeScalingMode)
                        scaling.intValue;

                if ((mode & ChargeScalingMode.Damage) != 0)
                {
                    Child(
                        charge,
                        "minimumDamageMultiplier",
                        "Minimum Damage Multiplier"
                    );
                }

                if ((mode & ChargeScalingMode.Range) != 0)
                {
                    Child(
                        charge,
                        "minimumRangeMultiplier",
                        "Minimum Range Multiplier"
                    );
                }
            }
        }

        // =============================================
        // FULL CHARGE PREVIEW
        // =============================================

        SerializedProperty preview =
            serialized.FindProperty(
                "chargeCompletionPreviewEffect"
            );

        if (preview == null)
            return;

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            "Full Charge Preview",
            EditorStyles.boldLabel
        );

        SerializedProperty effectType =
            preview.FindPropertyRelative(
                "effectType"
            );

        if (effectType == null)
            return;

        EditorGUILayout.PropertyField(
            effectType,
            new GUIContent("Effect Type")
        );

        bool enabled =
            effectType.enumValueIndex !=
            (int)ChargeCompletionPreviewEffectType.None;

        if (!enabled)
            return;

        Child(
            preview,
            "shakeAmount",
            "Shake Amount"
        );

        Child(
            preview,
            "shakeSpeed",
            "Shake Speed"
        );

        Child(
            preview,
            "forwardShakeInfluence",
            "Forward Shake Influence"
        );
    }

    // =====================================================
    // REUSABLE SECTION
    // =====================================================

    private static void DrawGroup(
        ref bool expanded,
        string title,
        Action drawContent)
    {
        expanded =
            EditorGUILayout.Foldout(
                expanded,
                title,
                true
            );

        if (!expanded)
        {
            EditorGUILayout.Space(6);
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUI.indentLevel++;

        drawContent?.Invoke();

        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
    }

    // =====================================================
    // SERIALIZED PROPERTY HELPERS
    // =====================================================

    private static void Field(
        SerializedObject serialized,
        string path,
        string label)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property == null)
            return;

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label),
            true
        );
    }

    private static void Child(
        SerializedProperty parent,
        string path,
        string label)
    {
        if (parent == null)
            return;

        SerializedProperty property =
            parent.FindPropertyRelative(path);

        if (property == null)
            return;

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label),
            true
        );
    }
}

#endif

