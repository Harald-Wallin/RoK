
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =========================================================
// SEVERITY
// =========================================================

public enum RoKDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

// =========================================================
// ISSUE
// =========================================================

public sealed class RoKDiagnosticIssue
{
    public readonly ScriptableObject Asset;
    public readonly RoKDiagnosticSeverity Severity;
    public readonly string Message;

    public RoKDiagnosticIssue(
        ScriptableObject asset,
        RoKDiagnosticSeverity severity,
        string message)
    {
        Asset = asset;
        Severity = severity;
        Message = message;
    }
}

// =========================================================
// REPORT
// =========================================================

public sealed class RoKDiagnosticReport
{
    public readonly List<ScriptableObject> Assets =
        new();

    public readonly List<RoKDiagnosticIssue> Issues =
        new();

    public readonly Dictionary<
        ScriptableObject,
        List<ScriptableObject>> References =
            new();

    public readonly Dictionary<
        ScriptableObject,
        List<ScriptableObject>> UsedBy =
            new();

    public int Count(RoKDiagnosticSeverity severity)
    {
        int count = 0;

        foreach (RoKDiagnosticIssue issue in Issues)
        {
            if (issue.Severity == severity)
                count++;
        }

        return count;
    }

    public IReadOnlyList<ScriptableObject>
        GetReferences(ScriptableObject asset)
    {
        if (asset != null &&
            References.TryGetValue(
                asset,
                out List<ScriptableObject> result))
        {
            return result;
        }

        return Array.Empty<ScriptableObject>();
    }

    public IReadOnlyList<ScriptableObject>
        GetUsers(ScriptableObject asset)
    {
        if (asset != null &&
            UsedBy.TryGetValue(
                asset,
                out List<ScriptableObject> result))
        {
            return result;
        }

        return Array.Empty<ScriptableObject>();
    }
}

// =========================================================
// DIAGNOSTIC SERVICE
// =========================================================

public static class RoKContentStudioDiagnostics
{
    // =====================================================
    // SCAN
    // =====================================================

    public static RoKDiagnosticReport Scan()
    {
        RoKDiagnosticReport report =
            new();

        List<ScriptableObject> assets =
            RoKContentStudioCatalog.Find(
                RoKContentCategory.All
            );

        foreach (ScriptableObject asset in assets)
        {
            if (asset == null)
                continue;

            report.Assets.Add(asset);

            report.References[asset] =
                FindDirectReferences(asset);

            ValidateAsset(
                asset,
                report
            );
        }

        BuildReverseReferences(report);

        report.Issues.Sort(
            (a, b) =>
            {
                int severity =
                    b.Severity.CompareTo(a.Severity);

                if (severity != 0)
                    return severity;

                return string.Compare(
                    a.Asset != null
                        ? a.Asset.name
                        : "",
                    b.Asset != null
                        ? b.Asset.name
                        : "",
                    StringComparison.OrdinalIgnoreCase
                );
            }
        );

        return report;
    }

    // =====================================================
    // DIRECT REFERENCES
    // =====================================================

    private static List<ScriptableObject>
        FindDirectReferences(ScriptableObject asset)
    {
        List<ScriptableObject> result =
            new();

        HashSet<ScriptableObject> unique =
            new();

        SerializedObject serialized =
            new SerializedObject(asset);

        SerializedProperty property =
            serialized.GetIterator();

        // Next(true) also traverses serialized
        // children and arrays.
        while (property.Next(true))
        {
            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            ScriptableObject referenced =
                property.objectReferenceValue
                    as ScriptableObject;

            if (referenced == null ||
                referenced == asset ||
                !IsContentAsset(referenced) ||
                !unique.Add(referenced))
            {
                continue;
            }

            result.Add(referenced);
        }

        result.Sort(
            (a, b) =>
                string.Compare(
                    a.name,
                    b.name,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        return result;
    }

    private static bool IsContentAsset(
        ScriptableObject asset)
    {
        return asset is AbilityData ||
               asset is AbilityEffect ||
               asset is BuffDefinition ||
               asset is EffectDefinition ||
               asset is TalentData;
    }

    // =====================================================
    // REVERSE REFERENCES
    // =====================================================

    private static void BuildReverseReferences(
        RoKDiagnosticReport report)
    {
        foreach (var pair in report.References)
        {
            ScriptableObject owner =
                pair.Key;

            foreach (ScriptableObject dependency
                     in pair.Value)
            {
                if (!report.UsedBy.TryGetValue(
                        dependency,
                        out List<ScriptableObject> users))
                {
                    users = new();
                    report.UsedBy.Add(
                        dependency,
                        users
                    );
                }

                if (!users.Contains(owner))
                {
                    users.Add(owner);
                }
            }
        }

        foreach (var pair in report.UsedBy)
        {
            pair.Value.Sort(
                (a, b) =>
                    string.Compare(
                        a.name,
                        b.name,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private static void ValidateAsset(
        ScriptableObject asset,
        RoKDiagnosticReport report)
    {
        SerializedObject serialized =
            new SerializedObject(asset);

        if (asset is AbilityData ability)
        {
            ValidateAbility(
                ability,
                serialized,
                report
            );
        }

        if (asset is BuffDefinition)
        {
            ValidateArray(
                asset,
                serialized,
                "effects",
                "Buff Effects",
                true,
                report
            );
        }

        if (asset is TalentData)
        {
            ValidateArray(
                asset,
                serialized,
                "passiveEffects",
                "Talent Passive Effects",
                false,
                report
            );
        }

        if (asset is DamageEffect)
        {
            ValidateDamage(
                asset,
                serialized,
                report
            );
        }

        if (asset is ApplyBuffDefinitionEffect)
        {
            ValidateRequiredReference(
                asset,
                serialized,
                "buffDefinition",
                "Apply Buff saknar BuffDefinition.",
                report
            );
        }

        if (asset is SpawnVFXEffect)
        {
            ValidateRequiredReference(
                asset,
                serialized,
                "prefab",
                "Spawn VFX saknar prefab.",
                report
            );
        }

        if (asset is SpawnProjectileEffect)
        {
            ValidateRequiredReference(
                asset,
                serialized,
                "projectilePrefab",
                "Spawn Projectile saknar projectile prefab.",
                report
            );
        }
    }

    // =====================================================
    // ABILITY VALIDATION
    // =====================================================

    private static void ValidateAbility(
        AbilityData ability,
        SerializedObject serialized,
        RoKDiagnosticReport report)
    {
        ValidateArray(
            ability,
            serialized,
            "effects",
            "Ability Actions",
            true,
            report
        );

        AbilityTargetingSettings settings =
            ability.TargetingSettings;

        if (settings == null)
        {
            Add(
                report,
                ability,
                RoKDiagnosticSeverity.Error,
                "Ability saknar TargetingSettings."
            );

            return;
        }

        TargetingMode mode =
            settings.TargetingMode;

        if (mode != TargetingMode.Self &&
            settings.TargetLayers.value == 0)
        {
            Add(
                report,
                ability,
                RoKDiagnosticSeverity.Warning,
                "Target Layers är tomt. " +
                "Kontrollera att abilityn kan hitta sina mål."
            );
        }

        if (mode == TargetingMode.Circle &&
            settings.MaximumTargets == 1)
        {
            Add(
                report,
                ability,
                RoKDiagnosticSeverity.Info,
                "Circle använder Maximum Targets = 1. " +
                "Endast ett target påverkas. " +
                "Använd 0 för Unlimited."
            );
        }

        if (mode == TargetingMode.Circle &&
            settings.Radius <= 0f)
        {
            Add(
                report,
                ability,
                RoKDiagnosticSeverity.Warning,
                "Circle har Radius = 0. " +
                "Ingen användbar AoE-yta har konfigurerats."
            );
        }

        bool hasProjectile = false;
        bool hasDeferred = false;

        if (ability.effects != null)
        {
            foreach (AbilityEffect effect
                     in ability.effects)
            {
                if (effect == null)
                    continue;

                if (effect is SpawnProjectileEffect)
                {
                    hasProjectile = true;
                }

                if (effect.ExecutionTiming ==
                    AbilityEffectExecutionTiming.Deferred)
                {
                    hasDeferred = true;
                }
            }
        }

        if (hasDeferred && !hasProjectile)
        {
            Add(
                report,
                ability,
                RoKDiagnosticSeverity.Info,
                "Abilityn innehåller Deferred-actions " +
                "men ingen SpawnProjectileEffect. " +
                "Kontrollera att en trap, delayed action " +
                "eller annan executor faktiskt kör dem."
            );
        }
    }

    // =====================================================
    // DAMAGE VALIDATION
    // =====================================================

    private static void ValidateDamage(
        ScriptableObject asset,
        SerializedObject serialized,
        RoKDiagnosticReport report)
    {
        SerializedProperty flat =
            serialized.FindProperty(
                "flatDamage"
            );

        SerializedProperty scaling =
            serialized.FindProperty(
                "scaling"
            );

        bool noFlat =
            flat == null ||
            flat.intValue <= 0;

        bool noScaling =
            scaling == null ||
            scaling.arraySize == 0;

        if (noFlat && noScaling)
        {
            Add(
                report,
                asset,
                RoKDiagnosticSeverity.Warning,
                "DamageEffect har varken Flat Damage " +
                "eller Scaling. Den beräknar 0 grundskada."
            );
        }
    }

    // =====================================================
    // ARRAY VALIDATION
    // =====================================================

    private static void ValidateArray(
        ScriptableObject asset,
        SerializedObject serialized,
        string propertyName,
        string displayName,
        bool required,
        RoKDiagnosticReport report)
    {
        SerializedProperty array =
            serialized.FindProperty(
                propertyName
            );

        if (array == null || !array.isArray)
        {
            Add(
                report,
                asset,
                RoKDiagnosticSeverity.Error,
                $"Serialized array '{propertyName}' saknas."
            );

            return;
        }

        if (array.arraySize == 0)
        {
            if (required)
            {
                Add(
                    report,
                    asset,
                    RoKDiagnosticSeverity.Warning,
                    $"{displayName} är tom."
                );
            }

            return;
        }

        for (int i = 0;
             i < array.arraySize;
             i++)
        {
            SerializedProperty item =
                array.GetArrayElementAtIndex(i);

            if (item.propertyType ==
                    SerializedPropertyType.ObjectReference &&
                item.objectReferenceValue == null)
            {
                Add(
                    report,
                    asset,
                    RoKDiagnosticSeverity.Error,
                    $"{displayName}[{i}] saknar referens."
                );
            }
        }
    }

    // =====================================================
    // REQUIRED REFERENCES
    // =====================================================

    private static void ValidateRequiredReference(
        ScriptableObject asset,
        SerializedObject serialized,
        string propertyName,
        string message,
        RoKDiagnosticReport report)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property == null)
        {
            Add(
                report,
                asset,
                RoKDiagnosticSeverity.Error,
                $"Property '{propertyName}' saknas."
            );

            return;
        }

        if (property.objectReferenceValue == null)
        {
            Add(
                report,
                asset,
                RoKDiagnosticSeverity.Warning,
                message
            );
        }
    }

    // =====================================================
    // ISSUE HELPER
    // =====================================================

    private static void Add(
        RoKDiagnosticReport report,
        ScriptableObject asset,
        RoKDiagnosticSeverity severity,
        string message)
    {
        report.Issues.Add(
            new RoKDiagnosticIssue(
                asset,
                severity,
                message
            )
        );
    }
}

#endif

