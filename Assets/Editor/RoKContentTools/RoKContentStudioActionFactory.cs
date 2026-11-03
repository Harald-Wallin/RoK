
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// =========================================================
// ACTION TEMPLATE
// =========================================================

public sealed class RoKActionTemplate
{
    public readonly string Name;
    public readonly string Description;
    public readonly Type ActionType;

    public readonly AbilityEffectTargetMode DefaultTarget;
    public readonly bool RequiresCharacterTarget;

    public RoKActionTemplate(
        string name,
        string description,
        Type actionType,
        AbilityEffectTargetMode defaultTarget,
        bool requiresCharacterTarget)
    {
        Name = name;
        Description = description;
        ActionType = actionType;
        DefaultTarget = defaultTarget;
        RequiresCharacterTarget = requiresCharacterTarget;
    }
}

// =========================================================
// ACTION FACTORY
// =========================================================

public static class RoKContentStudioActionFactory
{
    private const string OutputFolder =
        "Assets/3.Effects/AbilityEffects/ContentStudio";

    // =====================================================
    // SUPPORTED TEMPLATES
    // =====================================================

    public static readonly IReadOnlyList<RoKActionTemplate>
        Templates = new[]
        {
            new RoKActionTemplate(
                "Direct Damage",
                "Deals damage using flat values, scaling " +
                "and a selectable DamageType.\n\n" +
                "Default target: EachAffectedTarget.\n\n" +
                "Configure Flat Damage, Damage Type and " +
                "optional scaling after creation.",
                typeof(DamageEffect),
                AbilityEffectTargetMode.EachAffectedTarget,
                true
            ),

            new RoKActionTemplate(
                "Scaled Damage",
                "Physical damage based on Strength, " +
                "Weapon Damage and a flat bonus.\n\n" +
                "Uses the existing ScaledDamageEffect.\n\n" +
                "DamageType selection is not yet exposed " +
                "by this effect class.",
                typeof(ScaledDamageEffect),
                AbilityEffectTargetMode.EachAffectedTarget,
                true
            ),

            new RoKActionTemplate(
                "Heal",
                "Restores health to a character.\n\n" +
                "Defaults to Caster for safety.\n\n" +
                "Change Target Mode to EachAffectedTarget " +
                "when designing heals for friendly targets.",
                typeof(HealEffect),
                AbilityEffectTargetMode.Caster,
                true
            ),

            new RoKActionTemplate(
                "Apply Buff",
                "Applies an existing BuffDefinition.\n\n" +
                "The BuffDefinition owns duration, " +
                "stacks and passive EffectDefinitions.\n\n" +
                "Defaults to Caster, but can target " +
                "other characters.",
                typeof(ApplyBuffDefinitionEffect),
                AbilityEffectTargetMode.Caster,
                true
            ),

            new RoKActionTemplate(
                "Spawn VFX",
                "Spawns a visual effect prefab.\n\n" +
                "Defaults to OncePerAction.\n\n" +
                "Requires a prefab reference. " +
                "Does not automatically deal damage.",
                typeof(SpawnVFXEffect),
                AbilityEffectTargetMode.OncePerAction,
                false
            ),

            new RoKActionTemplate(
                "Spawn Projectile",
                "Creates a projectile through " +
                "the existing projectile system.\n\n" +
                "Defaults to OncePerAction.\n\n" +
                "Requires a projectile prefab. " +
                "Effects intended for impact must " +
                "use the appropriate deferred execution.",
                typeof(SpawnProjectileEffect),
                AbilityEffectTargetMode.OncePerAction,
                false
            )
        };

    // =====================================================
    // CREATE AND ATTACH
    // =====================================================

    public static AbilityEffect CreateAndAttach(
        AbilityData ability,
        RoKActionTemplate template,
        BuffDefinition selectedBuff = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Actions can only be created in Edit Mode."
            );
        }

        if (ability == null || template == null)
        {
            throw new ArgumentNullException(
                "Ability and template are required."
            );
        }

        if (template.ActionType == null ||
            !typeof(AbilityEffect).IsAssignableFrom(
                template.ActionType))
        {
            throw new InvalidOperationException(
                "Template has an invalid AbilityEffect type."
            );
        }

        EnsureFolder(OutputFolder);

        string safeName = Regex.Replace(
            $"{ability.name}_{template.Name}",
            @"[^\w\- ]",
            "_"
        );

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{OutputFolder}/{safeName}.asset"
            );

        AbilityEffect action = null;
        bool assetCreated = false;

        try
        {
            action =
                ScriptableObject.CreateInstance(
                    template.ActionType
                ) as AbilityEffect;

            if (action == null)
            {
                throw new InvalidOperationException(
                    "Failed to create AbilityEffect."
                );
            }

            AssetDatabase.CreateAsset(action, path);

            assetCreated = true;

            Undo.RegisterCreatedObjectUndo(
                action,
                "Create Ability Action"
            );

            // =============================================
            // CONFIGURE ACTION
            // =============================================

            SerializedObject serialized =
                new SerializedObject(action);

            SetEnum(
                serialized,
                "executionTiming",
                (int)AbilityEffectExecutionTiming.Immediate
            );

            SetEnum(
                serialized,
                "targetMode",
                (int)template.DefaultTarget
            );

            SetBool(
                serialized,
                "requiresCharacterTarget",
                template.RequiresCharacterTarget
            );

            if (action is ApplyBuffDefinitionEffect &&
                selectedBuff != null)
            {
                Require(
                    serialized,
                    "buffDefinition"
                ).objectReferenceValue = selectedBuff;
            }

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(action);

            // =============================================
            // ATTACH TO ABILITY
            // =============================================

            SerializedObject abilitySerialized =
                new SerializedObject(ability);

            SerializedProperty effects =
                Require(
                    abilitySerialized,
                    "effects"
                );

            int index = effects.arraySize;

            effects.InsertArrayElementAtIndex(index);

            effects.GetArrayElementAtIndex(index)
                .objectReferenceValue = action;

            abilitySerialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(ability);

            AssetDatabase.SaveAssets();

            return action;
        }
        catch
        {
            if (assetCreated)
            {
                AssetDatabase.DeleteAsset(path);
            }
            else if (action != null)
            {
                UnityEngine.Object.DestroyImmediate(action);
            }

            throw;
        }
    }

    // =====================================================
    // SERIALIZED HELPERS
    // =====================================================

    private static SerializedProperty Require(
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Missing serialized property " +
                $"'{propertyName}' on " +
                $"{serialized.targetObject.GetType().Name}"
            );
        }

        return property;
    }

    private static void SetEnum(
        SerializedObject serialized,
        string name,
        int value)
    {
        Require(serialized, name)
            .enumValueIndex = value;
    }

    private static void SetBool(
        SerializedObject serialized,
        string name,
        bool value)
    {
        Require(serialized, name)
            .boolValue = value;
    }

    // =====================================================
    // FOLDER CREATION
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

