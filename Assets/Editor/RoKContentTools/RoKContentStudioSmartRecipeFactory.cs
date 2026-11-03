
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =========================================================
// BUFF EFFECT SOURCE
// =========================================================

public enum RoKBuffEffectSource
{
    Existing,
    CreateNew
}

// =========================================================
// INDIVIDUAL EFFECT DRAFT
// =========================================================

public sealed class RoKBuffEffectDraft
{
    public RoKBuffEffectSource Source =
        RoKBuffEffectSource.CreateNew;

    public EffectDefinition ExistingEffect;

    public string Name = "New Effect";

    public RoKPassiveEffectKind Kind =
        RoKPassiveEffectKind.DamageResistance;

    public StatType Stat = StatType.MaxHP;

    public ModifierType ModifierType =
        ModifierType.Percent;

    public DamageType DamageType =
        DamageType.Fire;

    public CreatureType CreatureTypes =
        CreatureType.Beast;

    public float Value = 0.25f;

    public RoKBuffEffectDraft Clone()
    {
        return new RoKBuffEffectDraft
        {
            Source = Source,
            ExistingEffect = ExistingEffect,
            Name = Name,
            Kind = Kind,
            Stat = Stat,
            ModifierType = ModifierType,
            DamageType = DamageType,
            CreatureTypes = CreatureTypes,
            Value = Value
        };
    }
}

// =========================================================
// BUFF DRAFT
// =========================================================

public sealed class RoKBuffRecipeDraft
{
    public string Name = "New Buff";

    public float Duration = 30f;

    public int MaxStacks = 1;

    public readonly List<RoKBuffEffectDraft> Effects =
        new()
        {
            new RoKBuffEffectDraft()
        };

    public RoKBuffRecipeDraft Clone()
    {
        RoKBuffRecipeDraft copy =
            new RoKBuffRecipeDraft
            {
                Name = Name,
                Duration = Duration,
                MaxStacks = MaxStacks
            };

        copy.Effects.Clear();

        foreach (RoKBuffEffectDraft effect in Effects)
        {
            copy.Effects.Add(
                effect?.Clone()
            );
        }

        return copy;
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Ange ett namn för buffen.";
            return false;
        }

        if (float.IsNaN(Duration) ||
            float.IsInfinity(Duration) ||
            Duration <= 0f)
        {
            error =
                "Buff Duration måste vara större än 0.";

            return false;
        }

        if (MaxStacks < 1)
        {
            error =
                "Max Stacks måste vara minst 1.";

            return false;
        }

        if (Effects.Count == 0)
        {
            error =
                "Buffen behöver minst en effekt.";

            return false;
        }

        HashSet<EffectDefinition> existing =
            new();

        for (int i = 0; i < Effects.Count; i++)
        {
            RoKBuffEffectDraft effect = Effects[i];

            if (effect == null)
            {
                error =
                    $"Effect {i + 1} saknar konfiguration.";

                return false;
            }

            if (effect.Source ==
                RoKBuffEffectSource.Existing)
            {
                if (effect.ExistingEffect == null)
                {
                    error =
                        $"Effect {i + 1}: välj en " +
                        "befintlig EffectDefinition.";

                    return false;
                }

                if (string.IsNullOrEmpty(
                    AssetDatabase.GetAssetPath(
                        effect.ExistingEffect)))
                {
                    error =
                        $"Effect {i + 1} är inte " +
                        "ett sparat project asset.";

                    return false;
                }

                if (!existing.Add(effect.ExistingEffect))
                {
                    error =
                        "Samma befintliga effekt är vald " +
                        "flera gånger. Använd en separat " +
                        "definition om två olika " +
                        "effektinstanser behövs.";

                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(
                    effect.Name))
                {
                    error =
                        $"Effect {i + 1} behöver ett namn.";

                    return false;
                }

                if (float.IsNaN(effect.Value) ||
                    float.IsInfinity(effect.Value))
                {
                    error =
                        $"Effect {i + 1} har " +
                        "ett ogiltigt värde.";

                    return false;
                }

                if (effect.Kind ==
                        RoKPassiveEffectKind
                            .CreatureDamageBonus &&
                    effect.CreatureTypes ==
                        CreatureType.None)
                {
                    error =
                        $"Effect {i + 1}: välj minst " +
                        "en CreatureType.";

                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }
}

// =========================================================
// SMART RECIPE FACTORY
// =========================================================

public static class RoKContentStudioSmartRecipeFactory
{
    // =====================================================
    // VALIDATION
    // =====================================================

    public static bool Validate(
        RoKAbilityRecipeRequest request,
        bool createNewBuff,
        RoKBuffRecipeDraft buffDraft,
        out string error)
    {
        if (request == null)
        {
            error = "Ability request saknas.";
            return false;
        }

        bool requiresBuff =
            request.Recipe ==
                RoKAbilityRecipeKind
                    .DirectDamageWithSelfBuff;

        if (!requiresBuff || !createNewBuff)
        {
            return
                RoKContentStudioAbilityRecipeFactory
                    .Validate(
                        request,
                        out error
                    );
        }

        // Validate general ability settings without
        // requiring an existing BuffDefinition yet.

        RoKAbilityRecipeRequest preflight =
            CloneRequest(request);

        preflight.Recipe =
            RoKAbilityRecipeKind.DirectDamage;

        if (!RoKContentStudioAbilityRecipeFactory
                .Validate(
                    preflight,
                    out error))
        {
            return false;
        }

        if (buffDraft == null)
        {
            error = "Buff Draft saknas.";
            return false;
        }

        return buffDraft.Validate(out error);
    }

    // =====================================================
    // CREATE COMPLETE ABILITY
    // =====================================================

    public static AbilityData Create(
        RoKAbilityRecipeRequest request,
        bool createNewBuff,
        RoKBuffRecipeDraft buffDraft)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Content kan endast skapas i Edit Mode."
            );
        }

        if (!Validate(
                request,
                createNewBuff,
                buffDraft,
                out string error))
        {
            throw new InvalidOperationException(error);
        }

        RoKAbilityRecipeRequest abilityRequest =
            CloneRequest(request);

        bool needsNewBuff =
            createNewBuff &&
            abilityRequest.Recipe ==
                RoKAbilityRecipeKind
                    .DirectDamageWithSelfBuff;

        if (!needsNewBuff)
        {
            return
                RoKContentStudioAbilityRecipeFactory
                    .Create(abilityRequest);
        }

        RoKBuffRecipeDraft draft =
            buffDraft.Clone();

        // Tracks only assets created by this
        // Smart Recipe operation.
        List<string> createdPaths = new();

        try
        {
            // =============================================
            // RESOLVE PASSIVE EFFECTS
            // =============================================

            List<EffectDefinition> effects =
                new();

            foreach (RoKBuffEffectDraft entry
                     in draft.Effects)
            {
                EffectDefinition effect;

                if (entry.Source ==
                    RoKBuffEffectSource.Existing)
                {
                    effect = entry.ExistingEffect;
                }
                else
                {
                    effect =
                        RoKContentStudioFactory.Create(
                            RoKContentRecipe.PassiveEffect,
                            entry.Name,
                            null,
                            entry.Kind,
                            entry.Stat,
                            entry.ModifierType,
                            entry.DamageType,
                            entry.CreatureTypes,
                            entry.Value,
                            draft.Duration,
                            0f
                        ) as EffectDefinition;

                    if (effect == null)
                    {
                        throw new InvalidOperationException(
                            "Failed to create passive effect."
                        );
                    }

                    TrackAsset(
                        effect,
                        createdPaths
                    );
                }

                effects.Add(effect);
            }

            // =============================================
            // CREATE BUFF THROUGH EXISTING FACTORY
            // =============================================

            RoKBuffEffectDraft first =
                draft.Effects[0];

            BuffDefinition buff =
                RoKContentStudioFactory.Create(
                    RoKContentRecipe.BuffDefinition,
                    draft.Name,
                    effects[0],
                    first.Kind,
                    first.Stat,
                    first.ModifierType,
                    first.DamageType,
                    first.CreatureTypes,
                    first.Value,
                    draft.Duration,
                    0f
                ) as BuffDefinition;

            if (buff == null)
            {
                throw new InvalidOperationException(
                    "Failed to create BuffDefinition."
                );
            }

            TrackAsset(
                buff,
                createdPaths
            );

            ConfigureBuff(
                buff,
                draft,
                effects
            );

            // =============================================
            // CREATE ABILITY THROUGH EXISTING FACTORY
            // =============================================

            abilityRequest.SelfBuff = buff;

            return
                RoKContentStudioAbilityRecipeFactory
                    .Create(abilityRequest);
        }
        catch
        {
            // The existing ability factory rolls back
            // its own newly created ability/actions.
            //
            // Here we only remove the buff and effect
            // assets created by this operation.
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            throw;
        }
    }

    // =====================================================
    // BUFF CONFIGURATION
    // =====================================================

    private static void ConfigureBuff(
        BuffDefinition buff,
        RoKBuffRecipeDraft draft,
        IReadOnlyList<EffectDefinition> effects)
    {
        SerializedObject serialized =
            new SerializedObject(buff);

        SerializedProperty duration =
            Require(serialized, "duration");

        SerializedProperty stacks =
            Require(serialized, "maxStacks");

        SerializedProperty array =
            Require(serialized, "effects");

        duration.floatValue =
            draft.Duration;

        stacks.intValue =
            draft.MaxStacks;

        array.arraySize =
            effects.Count;

        for (int i = 0; i < effects.Count; i++)
        {
            array.GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    effects[i];
        }

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(buff);

        AssetDatabase.SaveAssets();
    }

    // =====================================================
    // REQUEST SNAPSHOT
    // =====================================================

    private static RoKAbilityRecipeRequest CloneRequest(
        RoKAbilityRecipeRequest source)
    {
        return new RoKAbilityRecipeRequest
        {
            Name = source.Name,
            Description = source.Description,

            Recipe = source.Recipe,
            AbilityType = source.AbilityType,

            DamageType = source.DamageType,
            FlatDamage = source.FlatDamage,

            Cooldown = source.Cooldown,
            Range = source.Range,

            TargetLayers = source.TargetLayers,

            CanMiss = source.CanMiss,
            CanCrit = source.CanCrit,

            SelfBuff = source.SelfBuff
        };
    }

    // =====================================================
    // INTERNAL HELPERS
    // =====================================================

    private static void TrackAsset(
        ScriptableObject asset,
        List<string> paths)
    {
        string path =
            AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException(
                "Created asset has no project path."
            );
        }

        paths.Add(path);
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
                $"Missing property '{path}' on " +
                $"{serialized.targetObject.GetType().Name}"
            );
        }

        return property;
    }
}

#endif

