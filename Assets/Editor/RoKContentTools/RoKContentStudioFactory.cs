
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public enum RoKContentRecipe
{
    SelfBuffAbility,
    BuffDefinition,
    PassiveEffect,
    EmptyAbility
}

public enum RoKPassiveEffectKind
{
    StatModifier,
    DamageResistance,
    CreatureDamageBonus,
    DamageTypeBonus
}

public static class RoKContentStudioFactory
{
    // =====================================================
    // OUTPUT PATHS
    // =====================================================

    private const string AbilityFolder =
        "Assets/1.Abilities/PlayerAbilities/ContentStudio";

    private const string ActionFolder =
        "Assets/3.Effects/AbilityEffects/ContentStudio";

    private const string BuffFolder =
        "Assets/3.Effects/BuffDefinitions/ContentStudio";

    private const string EffectFolder =
        "Assets/3.Effects/SharedDefinitions/ContentStudio";

    // =====================================================
    // CREATE CONTENT
    // =====================================================

    public static ScriptableObject Create(
        RoKContentRecipe recipe,
        string contentName,
        EffectDefinition existingEffect,
        RoKPassiveEffectKind effectKind,
        StatType stat,
        ModifierType modifierType,
        DamageType damageType,
        CreatureType creatureTypes,
        float effectValue,
        float duration,
        float cooldown)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Content kan endast skapas i Edit Mode."
            );
        }

        if (string.IsNullOrWhiteSpace(contentName))
        {
            throw new ArgumentException(
                "Ange ett namn för innehållet."
            );
        }

        string cleanName = contentName.Trim();

        List<string> createdPaths = new();

        int undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Create RöK Content"
        );

        try
        {
            // =============================================
            // EMPTY ABILITY
            // =============================================

            if (recipe == RoKContentRecipe.EmptyAbility)
            {
                AbilityData empty =
                    CreateAsset<AbilityData>(
                        AbilityFolder,
                        cleanName,
                        createdPaths
                    );

                Undo.RecordObject(
                    empty,
                    "Configure Ability"
                );

                empty.abilityName = cleanName;
                empty.effects =
                    Array.Empty<AbilityEffect>();

                EditorUtility.SetDirty(empty);

                Finish();

                return empty;
            }

            // =============================================
            // PASSIVE EFFECT
            // =============================================

            if (recipe == RoKContentRecipe.PassiveEffect)
            {
                EffectDefinition standalone =
                    CreateEffect(
                        cleanName,
                        effectKind,
                        stat,
                        modifierType,
                        damageType,
                        creatureTypes,
                        effectValue,
                        createdPaths
                    );

                Finish();

                return standalone;
            }

            // =============================================
            // REUSE OR CREATE EFFECT
            // =============================================

            EffectDefinition effect =
                existingEffect != null
                    ? existingEffect
                    : CreateEffect(
                        cleanName + "_Effect",
                        effectKind,
                        stat,
                        modifierType,
                        damageType,
                        creatureTypes,
                        effectValue,
                        createdPaths
                    );

            // =============================================
            // CREATE BUFF
            // =============================================

            BuffDefinition buff =
                CreateAsset<BuffDefinition>(
                    BuffFolder,
                    cleanName + "_Buff",
                    createdPaths
                );

            Configure(
                buff,
                so =>
                {
                    SetString(
                        so,
                        "displayName",
                        cleanName
                    );

                    SetFloat(
                        so,
                        "duration",
                        Mathf.Max(0.01f, duration)
                    );

                    SerializedProperty effects =
                        Require(
                            so,
                            "effects"
                        );

                    effects.arraySize = 1;

                    effects.GetArrayElementAtIndex(0)
                        .objectReferenceValue = effect;
                }
            );

            if (recipe == RoKContentRecipe.BuffDefinition)
            {
                Finish();
                return buff;
            }

            // =============================================
            // APPLY BUFF ACTION
            // =============================================

            ApplyBuffDefinitionEffect action =
                CreateAsset<ApplyBuffDefinitionEffect>(
                    ActionFolder,
                    cleanName + "_ApplyBuff",
                    createdPaths
                );

            Configure(
                action,
                so =>
                {
                    SetObject(
                        so,
                        "buffDefinition",
                        buff
                    );

                    SetEnum(
                        so,
                        "targetMode",
                        (int)AbilityEffectTargetMode.Caster
                    );

                    SetEnum(
                        so,
                        "executionTiming",
                        (int)AbilityEffectExecutionTiming
                            .Immediate
                    );

                    SetBool(
                        so,
                        "requiresCharacterTarget",
                        true
                    );
                }
            );

            // =============================================
            // SELF BUFF ABILITY
            // =============================================

            AbilityData ability =
                CreateAsset<AbilityData>(
                    AbilityFolder,
                    cleanName,
                    createdPaths
                );

            Undo.RecordObject(
                ability,
                "Configure Self Buff Ability"
            );

            ability.abilityName = cleanName;
            ability.types = AbilityType.Buff;

            ability.alwaysHits = true;
            ability.canMiss = false;
            ability.canCrit = false;
            ability.requiresHitCheck = false;
            ability.entersCombatState = false;

            ability.effects = new AbilityEffect[]
            {
                action
            };

            Configure(
                ability,
                so =>
                {
                    SetEnum(
                        so,
                        "targetingSettings.targetingMode",
                        (int)TargetingMode.Self
                    );

                    SetInt(
                        so,
                        "targetingSettings.allowedRelations",
                        (int)TargetRelation.Self
                    );

                    SetBool(
                        so,
                        "targetingSettings.requiresAffectedTarget",
                        true
                    );

                    SetInt(
                        so,
                        "targetingSettings.maximumTargets",
                        1
                    );

                    SetEnum(
                        so,
                        "previewMode",
                        (int)ActionPreviewMode.None
                    );

                    SetEnum(
                        so,
                        "executionSettings.activationMode",
                        (int)ActionActivationMode.Immediate
                    );

                    SetFloat(
                        so,
                        "executionSettings.cooldown",
                        Mathf.Max(0f, cooldown)
                    );
                }
            );

            Finish();

            return ability;
        }
        catch
        {
            // Remove only assets created by this operation.
            for (int i = createdPaths.Count - 1;
                 i >= 0;
                 i--)
            {
                AssetDatabase.DeleteAsset(
                    createdPaths[i]
                );
            }

            AssetDatabase.Refresh();

            throw;
        }
        finally
        {
            Undo.CollapseUndoOperations(
                undoGroup
            );
        }
    }

    // =====================================================
    // CREATE REUSABLE EFFECT
    // =====================================================

    private static EffectDefinition CreateEffect(
        string name,
        RoKPassiveEffectKind kind,
        StatType stat,
        ModifierType modifierType,
        DamageType damageType,
        CreatureType creatureTypes,
        float value,
        List<string> createdPaths)
    {
        EffectDefinition effect;

        switch (kind)
        {
            case RoKPassiveEffectKind.StatModifier:
                {
                    var definition =
                        CreateAsset<
                            StatModifierEffectDefinition>(
                            EffectFolder,
                            name,
                            createdPaths
                        );

                    Configure(
                        definition,
                        so =>
                        {
                            SetEnum(
                                so,
                                "stat",
                                (int)stat
                            );

                            SetEnum(
                                so,
                                "modifierType",
                                (int)modifierType
                            );

                            SetFloat(
                                so,
                                "value",
                                value
                            );
                        }
                    );

                    effect = definition;
                    break;
                }

            case RoKPassiveEffectKind.DamageResistance:
                {
                    var definition =
                        CreateAsset<
                            DamageResistanceEffectDefinition>(
                            EffectFolder,
                            name,
                            createdPaths
                        );

                    Configure(
                        definition,
                        so =>
                        {
                            SetEnum(
                                so,
                                "damageType",
                                (int)damageType
                            );

                            SetFloat(
                                so,
                                "resistance",
                                value
                            );
                        }
                    );

                    effect = definition;
                    break;
                }

            case RoKPassiveEffectKind.CreatureDamageBonus:
                {
                    var definition =
                        CreateAsset<
                            CreatureDamageBonusEffectDefinition>(
                            EffectFolder,
                            name,
                            createdPaths
                        );

                    Configure(
                        definition,
                        so =>
                        {
                            SetInt(
                                so,
                                "targetTypes",
                                (int)creatureTypes
                            );

                            SetFloat(
                                so,
                                "bonus",
                                value
                            );
                        }
                    );

                    effect = definition;
                    break;
                }

            case RoKPassiveEffectKind.DamageTypeBonus:
                {
                    var definition =
                        CreateAsset<
                            DamageTypeBonusEffectDefinition>(
                            EffectFolder,
                            name,
                            createdPaths
                        );

                    Configure(
                        definition,
                        so =>
                        {
                            SetEnum(
                                so,
                                "damageType",
                                (int)damageType
                            );

                            SetFloat(
                                so,
                                "bonus",
                                value
                            );
                        }
                    );

                    effect = definition;
                    break;
                }

            default:
                throw new NotSupportedException(
                    $"Unsupported effect kind: {kind}"
                );
        }

        return effect;
    }

    // =====================================================
    // CREATE ASSET SAFELY
    // =====================================================

    private static T CreateAsset<T>(
        string folder,
        string name,
        List<string> createdPaths)
        where T : ScriptableObject
    {
        EnsureFolder(folder);

        string safeName =
            Regex.Replace(
                name,
                @"[^\w\- ]",
                "_"
            );

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{safeName}.asset"
            );

        T asset =
            ScriptableObject.CreateInstance<T>();

        AssetDatabase.CreateAsset(
            asset,
            path
        );

        createdPaths.Add(path);

        Undo.RegisterCreatedObjectUndo(
            asset,
            "Create RöK Asset"
        );

        return asset;
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";

        string[] segments =
            path.Split('/');

        for (int i = 1;
             i < segments.Length;
             i++)
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

    // =====================================================
    // SERIALIZED CONFIGURATION
    // =====================================================

    private static void Configure(
        ScriptableObject asset,
        Action<SerializedObject> configure)
    {
        Undo.RecordObject(
            asset,
            "Configure RöK Asset"
        );

        SerializedObject serialized =
            new SerializedObject(asset);

        configure(serialized);

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(asset);
    }

    private static SerializedProperty Require(
        SerializedObject serialized,
        string path)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Missing serialized property " +
                $"'{path}' on " +
                $"{serialized.targetObject.GetType().Name}"
            );
        }

        return property;
    }

    private static void SetEnum(
        SerializedObject so,
        string path,
        int value)
    {
        Require(so, path).enumValueIndex = value;
    }

    private static void SetInt(
        SerializedObject so,
        string path,
        int value)
    {
        Require(so, path).intValue = value;
    }

    private static void SetFloat(
        SerializedObject so,
        string path,
        float value)
    {
        Require(so, path).floatValue = value;
    }

    private static void SetBool(
        SerializedObject so,
        string path,
        bool value)
    {
        Require(so, path).boolValue = value;
    }

    private static void SetString(
        SerializedObject so,
        string path,
        string value)
    {
        Require(so, path).stringValue = value;
    }

    private static void SetObject(
        SerializedObject so,
        string path,
        UnityEngine.Object value)
    {
        Require(so, path).objectReferenceValue =
            value;
    }

    private static void Finish()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

#endif

