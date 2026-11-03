
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// =========================================================
// RECIPE TYPES
// =========================================================

public enum RoKAbilityRecipeKind
{
    DirectDamage,
    DirectDamageWithSelfBuff
}

// =========================================================
// RECIPE CONFIGURATION
// =========================================================

public sealed class RoKAbilityRecipeRequest
{
    public string Name = "New Ability";
    public string Description = "";

    public RoKAbilityRecipeKind Recipe =
        RoKAbilityRecipeKind.DirectDamage;

    public AbilityType AbilityType =
        AbilityType.Spell;

    public DamageType DamageType =
        DamageType.Fire;

    public int FlatDamage = 50;

    public float Cooldown = 5f;
    public float Range = 6f;

    public int TargetLayers;

    public bool CanMiss = true;
    public bool CanCrit = true;

    public BuffDefinition SelfBuff;
}

// =========================================================
// ABILITY RECIPE FACTORY
// =========================================================

public static class RoKContentStudioAbilityRecipeFactory
{
    private const string OutputFolder =
        "Assets/1.Abilities/PlayerAbilities/ContentStudio";

    // =====================================================
    // VALIDATION
    // =====================================================

    public static bool Validate(
        RoKAbilityRecipeRequest request,
        out string error)
    {
        if (request == null)
        {
            error = "Recipe request is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "Ange ett namn för abilityn.";
            return false;
        }

        if (request.FlatDamage <= 0)
        {
            error =
                "Flat Damage måste vara större än 0.";

            return false;
        }

        if (request.Range <= 0f)
        {
            error =
                "Range måste vara större än 0.";

            return false;
        }

        if (request.TargetLayers == 0)
        {
            error =
                "Välj minst ett Target Layer.\n\n" +
                "Detta måste inkludera lagret som " +
                "fiendernas targetbara objekt använder.";

            return false;
        }

        if (request.Recipe ==
            RoKAbilityRecipeKind.DirectDamageWithSelfBuff)
        {
            if (request.SelfBuff == null)
            {
                error =
                    "Välj en BuffDefinition som " +
                    "abilityn ska applicera.";

                return false;
            }

            if (request.SelfBuff.Effects == null ||
                request.SelfBuff.Effects.Count == 0)
            {
                error =
                    "Den valda buffen saknar " +
                    "EffectDefinitions.\n\n" +
                    "Lägg till effekter via Effect Composer.";

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
        RoKAbilityRecipeRequest request)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Ability recipes can only be " +
                "created in Edit Mode."
            );
        }

        if (!Validate(request, out string error))
        {
            throw new InvalidOperationException(error);
        }

        EnsureFolder(OutputFolder);

        List<string> createdPaths = new();

        AbilityData ability = null;

        try
        {
            // =============================================
            // CREATE ABILITY ASSET
            // =============================================

            string safeName = Regex.Replace(
                request.Name.Trim(),
                @"[^\w\- ]",
                "_"
            );

            string abilityPath =
                AssetDatabase.GenerateUniqueAssetPath(
                    $"{OutputFolder}/{safeName}.asset"
                );

            ability =
                ScriptableObject.CreateInstance<
                    AbilityData>();

            AssetDatabase.CreateAsset(
                ability,
                abilityPath
            );

            createdPaths.Add(abilityPath);

            Undo.RegisterCreatedObjectUndo(
                ability,
                "Create Ability Recipe"
            );

            ConfigureAbility(
                ability,
                request
            );

            // =============================================
            // CREATE DAMAGE ACTION
            // =============================================

            RoKActionTemplate damageTemplate =
                FindTemplate(typeof(DamageEffect));

            DamageEffect damageAction =
                RoKContentStudioActionFactory
                    .CreateAndAttach(
                        ability,
                        damageTemplate
                    ) as DamageEffect;

            if (damageAction == null)
            {
                throw new InvalidOperationException(
                    "Failed to create DamageEffect."
                );
            }

            createdPaths.Add(
                AssetDatabase.GetAssetPath(damageAction)
            );

            ConfigureDamage(
                damageAction,
                request
            );

            // =============================================
            // OPTIONAL SELF BUFF
            // =============================================

            if (request.Recipe ==
                RoKAbilityRecipeKind
                    .DirectDamageWithSelfBuff)
            {
                RoKActionTemplate buffTemplate =
                    FindTemplate(
                        typeof(ApplyBuffDefinitionEffect)
                    );

                AbilityEffect buffAction =
                    RoKContentStudioActionFactory
                        .CreateAndAttach(
                            ability,
                            buffTemplate,
                            request.SelfBuff
                        );

                if (buffAction == null)
                {
                    throw new InvalidOperationException(
                        "Failed to create Apply Buff action."
                    );
                }

                createdPaths.Add(
                    AssetDatabase.GetAssetPath(buffAction)
                );
            }

            // =============================================
            // SAVE
            // =============================================

            EditorUtility.SetDirty(ability);

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();

            return ability;
        }
        catch
        {
            // Remove only assets created by this
            // recipe operation.
            for (int i = createdPaths.Count - 1;
                 i >= 0;
                 i--)
            {
                string path = createdPaths[i];

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            AssetDatabase.Refresh();

            throw;
        }
    }

    // =====================================================
    // ABILITY CONFIGURATION
    // =====================================================

    private static void ConfigureAbility(
        AbilityData ability,
        RoKAbilityRecipeRequest request)
    {
        SerializedObject serialized =
            new SerializedObject(ability);

        SetString(
            serialized,
            "abilityName",
            request.Name.Trim()
        );

        SetString(
            serialized,
            "description",
            request.Description
        );

        SetEnum(
            serialized,
            "types",
            (int)request.AbilityType
        );

        // =============================================
        // TARGETING
        // =============================================

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
            request.Range
        );

        SetInt(
            serialized,
            "targetingSettings.targetLayers",
            request.TargetLayers
        );

        SetInt(
            serialized,
            "targetingSettings.maximumTargets",
            1
        );

        SetBool(
            serialized,
            "targetingSettings.requiresAffectedTarget",
            true
        );

        SetEnum(
            serialized,
            "previewMode",
            (int)ActionPreviewMode.FullGeometry
        );

        // =============================================
        // TIMING
        // =============================================

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
            Mathf.Max(0f, request.Cooldown)
        );

        SetBool(
            serialized,
            "executionSettings.triggersGlobalCooldown",
            true
        );

        SetBool(
            serialized,
            "executionSettings.resolvesTargetHitImmediately",
            true
        );

        // =============================================
        // COMBAT RULES
        // =============================================

        SetBool(
            serialized,
            "alwaysHits",
            !request.CanMiss
        );

        SetBool(
            serialized,
            "canMiss",
            request.CanMiss
        );

        SetBool(
            serialized,
            "canCrit",
            request.CanCrit
        );

        SetBool(
            serialized,
            "entersCombatState",
            true
        );

        // Legacy rule - not used for hit resolution
        // by the current AbilityHitResolver.
        SetBool(
            serialized,
            "requiresHitCheck",
            false
        );

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(ability);
    }

    // =====================================================
    // DAMAGE ACTION CONFIGURATION
    // =====================================================

    private static void ConfigureDamage(
        DamageEffect action,
        RoKAbilityRecipeRequest request)
    {
        SerializedObject serialized =
            new SerializedObject(action);

        SetInt(
            serialized,
            "flatDamage",
            request.FlatDamage
        );

        SetEnum(
            serialized,
            "damageType",
            (int)request.DamageType
        );

        SetBool(
            serialized,
            "dealsRawDamage",
            false
        );

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(action);
    }

    // =====================================================
    // ACTION TEMPLATE LOOKUP
    // =====================================================

    private static RoKActionTemplate FindTemplate(
        Type actionType)
    {
        foreach (
            RoKActionTemplate template
            in RoKContentStudioActionFactory.Templates)
        {
            if (template.ActionType == actionType)
                return template;
        }

        throw new InvalidOperationException(
            $"No Action Template registered for " +
            $"{actionType.Name}."
        );
    }

    // =====================================================
    // SERIALIZED HELPERS
    // =====================================================

    private static SerializedProperty Require(
        SerializedObject serialized,
        string path)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Missing property '{path}' on " +
                $"{serialized.targetObject.GetType().Name}"
            );
        }

        return property;
    }

    private static void SetString(
        SerializedObject so,
        string path,
        string value)
    {
        Require(so, path).stringValue =
            value ?? "";
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

    private static void SetEnum(
        SerializedObject so,
        string path,
        int value)
    {
        Require(so, path).enumValueIndex =
            value;
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

