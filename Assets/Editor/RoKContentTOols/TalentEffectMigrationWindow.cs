
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time migration from legacy
/// TalentData.effects to reusable
/// TalentData.passiveEffects.
///
/// Original references remain intact.
/// </summary>
public static class TalentEffectMigrationWindow
{
    private const string OutputFolder =
        "Assets/3.Effects/SharedDefinitions/" +
        "MigratedTalents";

    private static readonly Dictionary<
        AbilityEffect,
        EffectDefinition> Converted = new();

    // =====================================================
    // 1. PREVIEW
    // =====================================================

    [MenuItem(
        "RöK/Content Tools/Talent Effects/" +
        "1. Preview Migration"
    )]
    private static void Preview()
    {
        int ready = 0;
        int unsupported = 0;
        int existing = 0;
        int empty = 0;

        StringBuilder report =
            new StringBuilder(
                "=== RöK Talent Effect Migration: PREVIEW ===\n"
            );

        foreach (TalentData talent
                 in FindTalents())
        {
            if (talent.HasSharedEffects)
            {
                existing++;

                report.AppendLine(
                    $"SKIP (already shared): " +
                    $"{talent.name}"
                );

                continue;
            }

            if (talent.effects == null ||
                talent.effects.Length == 0)
            {
                empty++;

                report.AppendLine(
                    $"SKIP (no legacy effects): " +
                    $"{talent.name}"
                );

                continue;
            }

            if (!CanMigrate(
                    talent,
                    out string reason))
            {
                unsupported++;

                report.AppendLine(
                    $"BLOCKED: {talent.name} " +
                    $"— {reason}"
                );

                continue;
            }

            ready++;

            report.AppendLine(
                $"READY: {talent.name} " +
                $"— {talent.effects.Length} " +
                $"reusable effect(s)"
            );
        }

        report.AppendLine(
            $"Summary: {ready} ready, " +
            $"{existing} already shared, " +
            $"{empty} no legacy effects, " +
            $"{unsupported} blocked."
        );

        Debug.Log(
            report.ToString()
        );

        EditorUtility.DisplayDialog(
            "Talent migration preview",
            $"{ready} talents ready\n" +
            $"{existing} already migrated\n" +
            $"{empty} no legacy effects\n" +
            $"{unsupported} need manual review\n\n" +
            "See Unity Console for details.",
            "OK"
        );
    }

    // =====================================================
    // 2. MIGRATE
    // =====================================================

    [MenuItem(
        "RöK/Content Tools/Talent Effects/" +
        "2. Migrate Supported Talents"
    )]
    private static void Migrate()
    {
        if (EditorApplication
            .isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Migration blocked",
                "Exit Play Mode before modifying " +
                "asset references.",
                "OK"
            );

            return;
        }

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Migrate talent effects?",
                "This creates NEW shared effect " +
                "assets and populates passiveEffects.\n\n" +
                "Original TalentData.effects references " +
                "are preserved.\n\n" +
                "Commit or back up your project " +
                "before proceeding.",
                "Migrate",
                "Cancel"
            );

        if (!confirmed)
            return;

        Converted.Clear();

        EnsureFolder(OutputFolder);

        int migrated = 0;
        int skipped = 0;

        StringBuilder report =
            new StringBuilder(
                "=== RöK Talent Effect Migration ===\n"
            );

        foreach (TalentData talent
                 in FindTalents())
        {
            if (talent.HasSharedEffects ||
                talent.effects == null ||
                talent.effects.Length == 0)
            {
                skipped++;
                continue;
            }

            if (!CanMigrate(
                    talent,
                    out string reason))
            {
                skipped++;

                report.AppendLine(
                    $"SKIPPED: {talent.name}: " +
                    $"{reason}"
                );

                continue;
            }

            List<EffectDefinition> shared =
                new();

            bool success = true;

            foreach (AbilityEffect legacy
                     in talent.effects)
            {
                EffectDefinition replacement =
                    GetOrCreate(legacy);

                if (replacement == null)
                {
                    success = false;

                    report.AppendLine(
                        $"SKIPPED: {talent.name}: " +
                        $"conversion failed for " +
                        $"{legacy.name}"
                    );

                    break;
                }

                shared.Add(replacement);
            }

            if (!success)
            {
                skipped++;
                continue;
            }

            // =================================================
            // APPLY REFERENCES
            // =================================================

            Undo.RecordObject(
                talent,
                "Migrate Talent Effects"
            );

            SerializedObject serialized =
                new SerializedObject(talent);

            SerializedProperty property =
                serialized.FindProperty(
                    "passiveEffects"
                );

            if (property == null)
            {
                skipped++;

                report.AppendLine(
                    $"SKIPPED: {talent.name}: " +
                    "missing passiveEffects property"
                );

                continue;
            }

            property.arraySize =
                shared.Count;

            for (int i = 0;
                 i < shared.Count;
                 i++)
            {
                property
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                        shared[i];
            }

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(talent);

            migrated++;

            report.AppendLine(
                $"MIGRATED: {talent.name} " +
                $"({shared.Count} effects)"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.AppendLine(
            $"Summary: {migrated} migrated; " +
            $"{skipped} unchanged."
        );

        Debug.Log(
            report.ToString()
        );

        EditorUtility.DisplayDialog(
            "Talent migration complete",
            $"Migrated: {migrated}\n" +
            $"Unchanged: {skipped}\n\n" +
            "Review Console and test " +
            "talents in Play Mode.",
            "OK"
        );
    }

    // =====================================================
    // 3. VALIDATE
    // =====================================================

    [MenuItem(
        "RöK/Content Tools/Talent Effects/" +
        "3. Validate Talent Effects"
    )]
    private static void Validate()
    {
        int shared = 0;
        int legacy = 0;
        int empty = 0;
        int broken = 0;

        foreach (TalentData talent
                 in FindTalents())
        {
            if (talent.HasSharedEffects)
            {
                shared++;

                foreach (
                    EffectDefinition effect
                    in talent.PassiveEffects)
                {
                    if (effect != null)
                        continue;

                    broken++;

                    Debug.LogError(
                        $"Talent {talent.name} " +
                        "has a missing passive effect.",
                        talent
                    );
                }
            }
            else if (talent.effects != null &&
                     talent.effects.Length > 0)
            {
                legacy++;

                Debug.LogWarning(
                    $"Talent {talent.name} " +
                    "still uses legacy AbilityEffects.",
                    talent
                );
            }
            else
            {
                empty++;
            }
        }

        Debug.Log(
            $"Talent validation: " +
            $"{shared} shared, " +
            $"{legacy} legacy, " +
            $"{empty} without effects, " +
            $"{broken} missing shared references."
        );
    }

    // =====================================================
    // FIND ALL TALENTS
    // =====================================================

    private static List<TalentData>
        FindTalents()
    {
        List<TalentData> result =
            new();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:TalentData"
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            TalentData talent =
                AssetDatabase.LoadAssetAtPath<
                    TalentData>(path);

            if (talent != null)
            {
                result.Add(talent);
            }
        }

        result.Sort(
            (a, b) =>
                string.CompareOrdinal(
                    a.name,
                    b.name
                )
        );

        return result;
    }

    // =====================================================
    // CHECK MIGRATION SUPPORT
    // =====================================================

    private static bool CanMigrate(
        TalentData talent,
        out string reason)
    {
        foreach (AbilityEffect effect
                 in talent.effects)
        {
            if (effect == null)
            {
                reason =
                    "Null legacy effect reference";

                return false;
            }

            if (effect is StatModifierEffect ||
                effect is WardCapacityEffect)
            {
                continue;
            }

            reason =
                $"Unsupported legacy effect type " +
                $"{effect.GetType().Name}";

            return false;
        }

        reason = string.Empty;

        return true;
    }

    // =====================================================
    // CREATE OR REUSE MIGRATED EFFECT
    // =====================================================

    private static EffectDefinition GetOrCreate(
        AbilityEffect legacy)
    {
        if (Converted.TryGetValue(
                legacy,
                out EffectDefinition cached))
        {
            return cached;
        }

        string legacyPath =
            AssetDatabase.GetAssetPath(
                legacy
            );

        string guid =
            AssetDatabase.AssetPathToGUID(
                legacyPath
            );

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError(
                $"Legacy effect {legacy.name} " +
                "must be a saved asset."
            );

            return null;
        }

        string safeName =
            Regex.Replace(
                legacy.name,
                @"[^A-Za-z0-9_-]",
                "_"
            );

        string path =
            $"{OutputFolder}/" +
            $"{safeName}_" +
            $"{guid.Substring(0, 12)}.asset";

        EffectDefinition definition =
            AssetDatabase.LoadAssetAtPath<
                EffectDefinition>(path);

        // Never overwrite an unrelated asset.
        if (definition == null &&
            AssetDatabase.LoadMainAssetAtPath(
                path) != null)
        {
            Debug.LogError(
                $"Cannot replace existing " +
                $"unrelated asset at {path}."
            );

            return null;
        }

        // =================================================
        // CREATE NEW DEFINITION
        // =================================================

        if (definition == null)
        {
            if (legacy is
                StatModifierEffect stat)
            {
                definition =
                    ScriptableObject.CreateInstance<
                        StatModifierEffectDefinition>();

                SerializedObject so =
                    new SerializedObject(
                        definition
                    );

                so.FindProperty("stat")
                    .enumValueIndex =
                        (int)stat.stat;

                so.FindProperty("modifierType")
                    .enumValueIndex =
                        (int)stat.type;

                so.FindProperty("value")
                    .floatValue =
                        stat.value;

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (legacy is
                WardCapacityEffect ward)
            {
                definition =
                    ScriptableObject.CreateInstance<
                        WardCapacityEffectDefinition>();

                SerializedObject so =
                    new SerializedObject(
                        definition
                    );

                so.FindProperty("wardsPerStack")
                    .intValue =
                        ward.wardsPerPoint;

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                return null;
            }

            AssetDatabase.CreateAsset(
                definition,
                path
            );

            Undo.RegisterCreatedObjectUndo(
                definition,
                "Create shared talent effect"
            );

            EditorUtility.SetDirty(
                definition
            );
        }

        // =================================================
        // EXISTING DEFINITION TYPE CHECK
        // =================================================

        else if (
            (legacy is StatModifierEffect &&
             definition is not
                 StatModifierEffectDefinition)
            ||
            (legacy is WardCapacityEffect &&
             definition is not
                 WardCapacityEffectDefinition))
        {
            Debug.LogError(
                $"Shared asset has incorrect type " +
                $"at {path}."
            );

            return null;
        }

        Converted[legacy] =
            definition;

        return definition;
    }

    // =====================================================
    // ENSURE OUTPUT FOLDER
    // =====================================================

    private static void EnsureFolder(
        string path)
    {
        string parent = "Assets";

        string[] parts =
            path.Split('/');

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string next =
                parent + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(
                    next))
            {
                AssetDatabase.CreateFolder(
                    parent,
                    parts[i]
                );
            }

            parent = next;
        }
    }
}

#endif

