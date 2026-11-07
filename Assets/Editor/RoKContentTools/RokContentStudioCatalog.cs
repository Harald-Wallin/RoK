
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public enum RoKContentCategory
{
    All,
    Abilities,
    Buffs,
    AbilityActions,
    PassiveEffects,
    Talents,
    Items
}


public static class RoKContentStudioCatalog
{
    // =====================================================
    // CONTENT DISCOVERY
    // =====================================================

    public static List<ScriptableObject> Find(
        RoKContentCategory category,
        string search = "")
    {
        List<ScriptableObject> result = new();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { "Assets" }
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            ScriptableObject asset =
                AssetDatabase.LoadAssetAtPath<
                    ScriptableObject>(path);

            if (asset == null ||
                !MatchesCategory(asset, category))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                bool nameMatches =
                    asset.name.ToLowerInvariant()
                        .Contains(
                            search.ToLowerInvariant()
                        );

                if (!nameMatches)
                    continue;
            }

            result.Add(asset);
        }

        result.Sort((a, b) =>
            string.Compare(
                a.name,
                b.name,
                System.StringComparison.OrdinalIgnoreCase
            )
        );

        return result;
    }

    private static bool MatchesCategory(
        ScriptableObject asset,
        RoKContentCategory category)
    {
        return category switch
        {
            RoKContentCategory.All =>
                asset is AbilityData ||
                asset is BuffDefinition ||
                asset is AbilityEffect ||
                asset is EffectDefinition ||
                asset is TalentData ||
                asset is ItemData,


            RoKContentCategory.Abilities =>
                asset is AbilityData,

            RoKContentCategory.Buffs =>
                asset is BuffDefinition,

            RoKContentCategory.AbilityActions =>
                asset is AbilityEffect,

            RoKContentCategory.PassiveEffects =>
                asset is EffectDefinition,

            RoKContentCategory.Talents =>
                asset is TalentData,


            RoKContentCategory.Items =>
                asset is ItemData,


            _ => false
        };
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    public static List<string> Validate(
        ScriptableObject asset)
    {
        List<string> issues = new();

        if (asset == null)
        {
            issues.Add("Inget asset valt.");
            return issues;
        }

        if (asset is AbilityData ability)
        {
            if (ability.effects == null ||
                ability.effects.Length == 0)
            {
                issues.Add(
                    "VARNING: Abilityn saknar actions. " +
                    "Den utför inget gameplay-beteende."
                );
            }
            else
            {
                foreach (AbilityEffect action
                         in ability.effects)
                {
                    if (action == null)
                    {
                        issues.Add(
                            "FEL: En action-referens är tom."
                        );

                        continue;
                    }

                    if (action is ApplyBuffDefinitionEffect)
                    {
                        if (GetLinkedBuff(action) == null)
                        {
                            issues.Add(
                                "FEL: Apply Buff saknar " +
                                "BuffDefinition."
                            );
                        }

                        if (!ability.EffectiveIsSelfCast &&
                            action.TargetMode ==
                                AbilityEffectTargetMode
                                    .EachAffectedTarget)
                        {
                            issues.Add(
                                "INFO: Denna buff appliceras " +
                                "på varje påverkat mål, " +
                                "inte automatiskt på castern. " +
                                "Kontrollera att detta är avsiktligt."
                            );
                        }
                    }
                }
            }
        }

        if (asset is BuffDefinition buff)
        {
            if (buff.Effects == null ||
                buff.Effects.Count == 0)
            {
                issues.Add(
                    "VARNING: Buffen har inga " +
                    "gameplay-effekter."
                );
            }
            else
            {
                foreach (EffectDefinition effect
                         in buff.Effects)
                {
                    if (effect == null)
                    {
                        issues.Add(
                            "FEL: Buffen innehåller " +
                            "en tom effekt-referens."
                        );
                    }
                }
            }
        }

        if (asset is ApplyBuffDefinitionEffect apply)
        {
            if (GetLinkedBuff(apply) == null)
            {
                issues.Add(
                    "FEL: Buff Definition saknas."
                );
            }
        }

        if (asset is TalentData talent)
        {
            SerializedObject serialized =
                new SerializedObject(talent);

            SerializedProperty passive =
                serialized.FindProperty(
                    "passiveEffects"
                );

            if (passive != null)
            {
                for (int i = 0;
                     i < passive.arraySize;
                     i++)
                {
                    if (passive
                            .GetArrayElementAtIndex(i)
                            .objectReferenceValue == null)
                    {
                        issues.Add(
                            "FEL: Talent har en tom " +
                            "passive effect-referens."
                        );
                    }
                }
            }
        }

        return issues;
    }

    // =====================================================
    // LINKED CONTENT
    // =====================================================

    public static BuffDefinition GetLinkedBuff(
        AbilityEffect action)
    {
        if (action == null ||
            action is not ApplyBuffDefinitionEffect)
        {
            return null;
        }

        SerializedObject serialized =
            new SerializedObject(action);

        SerializedProperty property =
            serialized.FindProperty(
                "buffDefinition"
            );

        return property?.objectReferenceValue
            as BuffDefinition;
    }

    public static List<ScriptableObject> GetLinks(
        ScriptableObject asset)
    {
        List<ScriptableObject> links = new();

        if (asset is AbilityData ability &&
            ability.effects != null)
        {
            foreach (AbilityEffect action
                     in ability.effects)
            {
                if (action != null)
                    links.Add(action);
            }
        }

        if (asset is ApplyBuffDefinitionEffect actionAsset)
        {
            BuffDefinition buff =
                GetLinkedBuff(actionAsset);

            if (buff != null)
                links.Add(buff);
        }

        if (asset is BuffDefinition definition &&
            definition.Effects != null)
        {
            foreach (EffectDefinition effect
                     in definition.Effects)
            {
                if (effect != null)
                    links.Add(effect);
            }
        }


        if (asset is TalentData talent)
        {
            foreach (EffectDefinition effect in talent.PassiveEffects)
            {
                if (effect != null && !links.Contains(effect))
                    links.Add(effect);
            }

            if (talent.unlockedAbility != null)
                links.Add(talent.unlockedAbility);

            if (talent.requirements != null)
            {
                foreach (TalentRequirement requirement
                         in talent.requirements)
                {
                    if (requirement?.talent != null &&
                        !links.Contains(requirement.talent))
                    {
                        links.Add(requirement.talent);
                    }
                }
            }
        }

        if (asset is ItemData item)
        {
            if (item.RequiredFavour != null)
                links.Add(item.RequiredFavour);

            if (item.reputationFaction != null &&
                !links.Contains(item.reputationFaction))
            {
                links.Add(item.reputationFaction);
            }

            foreach (FavourData favour in item.LinkedFavours)
            {
                if (favour != null && !links.Contains(favour))
                    links.Add(favour);
            }
        }


        return links;
    }
}

#endif
