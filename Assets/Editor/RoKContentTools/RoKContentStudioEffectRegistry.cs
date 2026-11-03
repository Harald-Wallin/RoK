
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// =========================================================
// EFFECT CATEGORY
// =========================================================

public enum RoKRegistryEffectKind
{
    AbilityAction,
    PassiveEffect
}

// =========================================================
// REGISTERED TYPE
// =========================================================

public sealed class RoKRegisteredEffectType
{
    public readonly Type Type;
    public readonly string DisplayName;
    public readonly string MenuPath;

    public RoKRegisteredEffectType(
        Type type,
        string displayName,
        string menuPath)
    {
        Type = type;
        DisplayName = displayName;
        MenuPath = menuPath;
    }
}

// =========================================================
// DYNAMIC EFFECT REGISTRY
// =========================================================

public static class RoKContentStudioEffectRegistry
{
    private const string ActionFolder =
        "Assets/3.Effects/AbilityEffects/ContentStudio";

    private const string PassiveFolder =
        "Assets/3.Effects/SharedDefinitions/ContentStudio";

    // =====================================================
    // DISCOVERY
    // =====================================================

    private static readonly
        List<RoKRegisteredEffectType> actionTypes =
            Discover<AbilityEffect>();

    private static readonly
        List<RoKRegisteredEffectType> passiveTypes =
            Discover<EffectDefinition>();

    public static IReadOnlyList<RoKRegisteredEffectType>
        GetTypes(RoKRegistryEffectKind kind)
    {
        return kind == RoKRegistryEffectKind.AbilityAction
            ? actionTypes
            : passiveTypes;
    }

    private static List<RoKRegisteredEffectType>
        Discover<T>() where T : ScriptableObject
    {
        List<RoKRegisteredEffectType> result =
            new();

        foreach (Type type in
                 TypeCache.GetTypesDerivedFrom<T>())
        {
            if (type == null ||
                type.IsAbstract ||
                type.IsGenericType ||
                type.ContainsGenericParameters)
            {
                continue;
            }

            // Only expose explicitly asset-creatable types.
            // Internal and abstract runtime helpers remain hidden.

            CreateAssetMenuAttribute attribute =
                Attribute.GetCustomAttribute(
                    type,
                    typeof(CreateAssetMenuAttribute),
                    false
                ) as CreateAssetMenuAttribute;

            if (attribute == null)
                continue;

            string menuPath =
                attribute.menuName;

            string displayName =
                GetDisplayName(
                    type,
                    menuPath
                );

            result.Add(
                new RoKRegisteredEffectType(
                    type,
                    displayName,
                    menuPath
                )
            );
        }

        result.Sort(
            (a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        return result;
    }

    private static string GetDisplayName(
        Type type,
        string menuPath)
    {
        if (!string.IsNullOrWhiteSpace(menuPath))
        {
            int lastSlash =
                menuPath.LastIndexOf('/');

            return menuPath.Substring(
                lastSlash + 1
            );
        }

        return ObjectNames.NicifyVariableName(
            type.Name
        );
    }

    // =====================================================
    // TYPE VALIDATION
    // =====================================================

    public static bool IsRegistered(Type type)
    {
        if (type == null)
            return false;

        foreach (RoKRegisteredEffectType entry
                 in actionTypes)
        {
            if (entry.Type == type)
                return true;
        }

        foreach (RoKRegisteredEffectType entry
                 in passiveTypes)
        {
            if (entry.Type == type)
                return true;
        }

        return false;
    }

    // =====================================================
    // CREATE & ATTACH
    // =====================================================

    public static ScriptableObject Create(
        Type effectType,
        string assetName,
        ScriptableObject attachTo = null,
        AbilityEffectTargetMode targetMode =
            AbilityEffectTargetMode.EachAffectedTarget,
        AbilityEffectExecutionTiming executionTiming =
            AbilityEffectExecutionTiming.Immediate,
        bool requiresCharacterTarget = true,
        BuffDefinition linkedBuff = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Effects can only be created in Edit Mode."
            );
        }

        if (!IsRegistered(effectType))
        {
            throw new InvalidOperationException(
                "The selected effect type is not " +
                "registered or is not asset-creatable."
            );
        }

        if (string.IsNullOrWhiteSpace(assetName))
        {
            throw new ArgumentException(
                "Enter a name for the new effect."
            );
        }

        bool isAction =
            typeof(AbilityEffect).IsAssignableFrom(
                effectType
            );

        ValidateAttachment(
            attachTo,
            isAction
        );

        string folder =
            isAction
                ? ActionFolder
                : PassiveFolder;

        EnsureFolder(folder);

        string safeName =
            Regex.Replace(
                assetName.Trim(),
                @"[^\p{L}\p{N}_\- ]",
                "_"
            );

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{safeName}.asset"
            );

        ScriptableObject created = null;

        bool assetSaved = false;

        int undoGroup =
            Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Create RöK Effect"
        );

        try
        {
            // =============================================
            // CREATE SCRIPTABLEOBJECT
            // =============================================

            created =
                ScriptableObject.CreateInstance(
                    effectType
                );

            if (created == null)
            {
                throw new InvalidOperationException(
                    "ScriptableObject creation failed."
                );
            }

            AssetDatabase.CreateAsset(
                created,
                path
            );

            assetSaved = true;

            Undo.RegisterCreatedObjectUndo(
                created,
                "Create Effect Asset"
            );

            // =============================================
            // ACTION CONFIGURATION
            // =============================================

            if (created is AbilityEffect action)
            {
                ConfigureAction(
                    action,
                    targetMode,
                    executionTiming,
                    requiresCharacterTarget,
                    linkedBuff
                );
            }

            // =============================================
            // OPTIONAL ATTACHMENT
            // =============================================

            if (attachTo != null)
            {
                Attach(
                    attachTo,
                    created
                );
            }

            AssetDatabase.SaveAssets();

            return created;
        }
        catch
        {
            // Restore the owner before removing
            // a partially created asset.

            if (attachTo != null && created != null)
            {
                RemoveReferenceIfPresent(
                    attachTo,
                    created
                );
            }

            if (assetSaved)
            {
                AssetDatabase.DeleteAsset(path);
            }
            else if (created != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    created
                );
            }

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
    // VALIDATE ATTACHMENT
    // =====================================================

    private static void ValidateAttachment(
        ScriptableObject owner,
        bool isAction)
    {
        if (owner == null)
            return;

        if (!AssetDatabase.Contains(owner))
        {
            throw new InvalidOperationException(
                "Attachment target must be " +
                "a saved project asset."
            );
        }

        if (isAction)
        {
            if (owner is not AbilityData)
            {
                throw new InvalidOperationException(
                    "AbilityEffect can only be attached " +
                    "to an AbilityData."
                );
            }
        }
        else
        {
            if (owner is not BuffDefinition &&
                owner is not TalentData)
            {
                throw new InvalidOperationException(
                    "EffectDefinition can only be attached " +
                    "to a BuffDefinition or TalentData."
                );
            }
        }
    }

    // =====================================================
    // CONFIGURE ACTION
    // =====================================================

    private static void ConfigureAction(
        AbilityEffect action,
        AbilityEffectTargetMode targetMode,
        AbilityEffectExecutionTiming executionTiming,
        bool requiresCharacterTarget,
        BuffDefinition linkedBuff)
    {
        SerializedObject serialized =
            new SerializedObject(action);

        Require(serialized, "targetMode")
            .enumValueIndex =
                (int)targetMode;

        Require(serialized, "executionTiming")
            .enumValueIndex =
                (int)executionTiming;

        Require(serialized, "requiresCharacterTarget")
            .boolValue =
                requiresCharacterTarget;

        if (action is ApplyBuffDefinitionEffect &&
            linkedBuff != null)
        {
            Require(serialized, "buffDefinition")
                .objectReferenceValue =
                    linkedBuff;
        }

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(action);
    }

    // =====================================================
    // ATTACH TO OWNER
    // =====================================================

    private static void Attach(
        ScriptableObject owner,
        ScriptableObject effect)
    {
        string propertyName =
            owner is TalentData
                ? "passiveEffects"
                : "effects";

        SerializedObject serialized =
            new SerializedObject(owner);

        SerializedProperty array =
            Require(
                serialized,
                propertyName
            );

        if (!array.isArray)
        {
            throw new InvalidOperationException(
                $"'{propertyName}' is not an array."
            );
        }

        Undo.RecordObject(
            owner,
            "Attach RöK Effect"
        );

        int index =
            array.arraySize;

        array.InsertArrayElementAtIndex(
            index
        );

        array.GetArrayElementAtIndex(index)
            .objectReferenceValue =
                effect;

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(owner);
    }

    // =====================================================
    // ROLLBACK HELPER
    // =====================================================

    private static void RemoveReferenceIfPresent(
        ScriptableObject owner,
        ScriptableObject effect)
    {
        string propertyName =
            owner is TalentData
                ? "passiveEffects"
                : "effects";

        SerializedObject serialized =
            new SerializedObject(owner);

        SerializedProperty array =
            serialized.FindProperty(propertyName);

        if (array == null ||
            !array.isArray)
        {
            return;
        }

        for (int i = array.arraySize - 1;
             i >= 0;
             i--)
        {
            SerializedProperty element =
                array.GetArrayElementAtIndex(i);

            if (element.objectReferenceValue != effect)
                continue;

            int previousSize =
                array.arraySize;

            array.DeleteArrayElementAtIndex(i);

            if (array.arraySize == previousSize)
            {
                array.DeleteArrayElementAtIndex(i);
            }

            break;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(owner);
    }

    // =====================================================
    // SERIALIZATION
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
                $"Missing serialized property '{path}' " +
                $"on {serialized.targetObject.GetType().Name}."
            );
        }

        return property;
    }

    // =====================================================
    // FOLDERS
    // =====================================================

    private static void EnsureFolder(string path)
    {
        string current = "Assets";

        string[] segments =
            path.Split('/');

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

