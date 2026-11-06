
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public enum RoKForgeAbilityTemplate
{
    Blank,
    DirectDamage,
    DirectDamageWithSelfBuff
}

/// <summary>
/// Commits an unsaved AbilityData draft.
///
/// All values configured through the normal
/// Ability Settings Editor are preserved.
///
/// New actions are saved using the existing
/// Content Studio Action Factory.
///
/// Existing action assets are only referenced,
/// never copied or modified.
/// </summary>
public static class RoKForgeAbilityDraftFactory
{
    private const string AbilityFolder =
        "Assets/1.Abilities/PlayerAbilities/ContentStudio";

    // =====================================================
    // VALIDATION
    // =====================================================

    public static bool Validate(
        AbilityData draft,
        IReadOnlyList<AbilityEffect> actions,
        out string error)
    {
        if (draft == null)
        {
            error = "Ability draft saknas.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(draft.abilityName))
        {
            error = "Ange ett Ability Name.";
            return false;
        }

        if (actions == null)
        {
            error = "Action-listan saknas.";
            return false;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            AbilityEffect action = actions[i];

            if (action == null)
            {
                error =
                    $"Action {i + 1} saknar referens.";
                return false;
            }

            // Existing assets are valid references.
            if (AssetDatabase.Contains(action))
                continue;

            // Unsaved actions must have a registered
            // creation template.
            if (FindTemplate(action.GetType()) == null)
            {
                error =
                    $"Actiontypen {action.GetType().Name} " +
                    "saknar skaparmall.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    // =====================================================
    // CREATE
    // =====================================================

    public static AbilityData Create(
        AbilityData draft,
        IReadOnlyList<AbilityEffect> actions)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Abilities kan endast skapas i Edit Mode."
            );
        }

        if (!Validate(draft, actions, out string error))
        {
            throw new InvalidOperationException(error);
        }

        // Snapshot references before writing assets.
        List<AbilityEffect> sourceActions =
            new List<AbilityEffect>(actions);

        EnsureFolder(AbilityFolder);

        string safeName = Regex.Replace(
            draft.abilityName.Trim(),
            @"[^\w\- ]",
            "_"
        );

        string abilityPath =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{AbilityFolder}/{safeName}.asset"
            );

        List<string> createdPaths =
            new List<string>();

        AbilityData createdAbility = null;

        try
        {
            // =============================================
            // SAVE ABILITY DATA
            // =============================================

            createdAbility =
                ScriptableObject.CreateInstance<
                    AbilityData>();

            // Preserve targeting, timing, cooldown,
            // identity, icon, charge and combat rules.
            EditorUtility.CopySerialized(
                draft,
                createdAbility
            );

            // Never persist the draft's temporary
            // HideFlags or temporary action references.
            createdAbility.hideFlags =
                HideFlags.None;

            createdAbility.name = safeName;

            createdAbility.effects =
                Array.Empty<AbilityEffect>();

            AssetDatabase.CreateAsset(
                createdAbility,
                abilityPath
            );

            createdPaths.Add(abilityPath);

            Undo.RegisterCreatedObjectUndo(
                createdAbility,
                "Create Forge Ability"
            );

            // =============================================
            // SAVE OR LINK ACTIONS IN ORIGINAL ORDER
            // =============================================

            foreach (AbilityEffect source in sourceActions)
            {
                if (AssetDatabase.Contains(source))
                {
                    AttachExisting(
                        createdAbility,
                        source
                    );

                    continue;
                }

                RoKActionTemplate template =
                    FindTemplate(source.GetType());

                if (template == null)
                {
                    throw new InvalidOperationException(
                        $"No template for {source.GetType().Name}"
                    );
                }

                // Factory creates a unique asset and
                // attaches it to the new ability.
                AbilityEffect createdAction =
                    RoKContentStudioActionFactory
                        .CreateAndAttach(
                            createdAbility,
                            template
                        );

                if (createdAction == null)
                {
                    throw new InvalidOperationException(
                        "Failed to create action asset."
                    );
                }

                string actionPath =
                    AssetDatabase.GetAssetPath(
                        createdAction
                    );

                createdPaths.Add(actionPath);

                string generatedName =
                    createdAction.name;

                // Copy all settings edited in the
                // draft, including damage, target mode,
                // buff links and execution timing.
                EditorUtility.CopySerialized(
                    source,
                    createdAction
                );

                createdAction.hideFlags =
                    HideFlags.None;

                createdAction.name =
                    generatedName;

                EditorUtility.SetDirty(
                    createdAction
                );
            }

            EditorUtility.SetDirty(
                createdAbility
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return createdAbility;
        }
        catch
        {
            // =============================================
            // ROLLBACK
            // =============================================

            // Delete only the assets created during
            // this operation, in reverse order.
            for (int i = createdPaths.Count - 1;
                 i >= 0;
                 i--)
            {
                string path = createdPaths[i];

                if (string.IsNullOrEmpty(path))
                    continue;

                AssetDatabase.DeleteAsset(path);
            }

            if (createdPaths.Count == 0 &&
                createdAbility != null &&
                !AssetDatabase.Contains(createdAbility))
            {
                UnityEngine.Object.DestroyImmediate(
                    createdAbility
                );
            }

            AssetDatabase.Refresh();

            throw;
        }
    }

    // =====================================================
    // EXISTING ACTION REFERENCE
    // =====================================================

    private static void AttachExisting(
        AbilityData ability,
        AbilityEffect action)
    {
        SerializedObject serialized =
            new SerializedObject(ability);

        SerializedProperty array =
            serialized.FindProperty("effects");

        if (array == null || !array.isArray)
        {
            throw new InvalidOperationException(
                "AbilityData.effects is unavailable."
            );
        }

        int index = array.arraySize;

        array.InsertArrayElementAtIndex(index);

        array.GetArrayElementAtIndex(index)
            .objectReferenceValue = action;

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(ability);
    }

    // =====================================================
    // TEMPLATE LOOKUP
    // =====================================================

    public static RoKActionTemplate FindTemplate(
        Type type)
    {
        foreach (RoKActionTemplate template
                 in RoKContentStudioActionFactory.Templates)
        {
            if (template.ActionType == type)
                return template;
        }

        return null;
    }

    // =====================================================
    // FOLDER HELPERS
    // =====================================================

    private static void EnsureFolder(string path)
    {
        string current = "Assets";

        string[] segments = path.Split('/');

        for (int i = 1; i < segments.Length; i++)
        {
            string next =
                current + "/" + segments[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    segments[i]
                );
            }

            current = next;
        }
    }
}

#endif

