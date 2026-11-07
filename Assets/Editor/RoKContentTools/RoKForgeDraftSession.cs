
#if UNITY_EDITOR

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class RoKForgeDraftSession
{
    public ScriptableObject Draft { get; private set; }

    public void Begin(Type type)
    {
        if (Draft != null && Draft.GetType() == type)
            return;

        Cancel();

        Draft = CreateTemporary(type);
    }

    public ScriptableObject Commit(
        string displayName,
        string folder)
    {
        if (Draft == null)
            throw new InvalidOperationException(
                "No active draft.");

        ScriptableObject created =
            SaveCopy(Draft, displayName, folder);

        Cancel();
        return created;
    }

    public void Cancel()
    {
        DiscardTemporary(Draft);
        Draft = null;
    }

    // Shared by Ability, Item, Talent and future
    // content types. Never called in constructors.
    public static ScriptableObject CreateTemporary(
        Type type)
    {
        if (type == null ||
            type.IsAbstract ||
            !typeof(ScriptableObject).IsAssignableFrom(type))
            throw new ArgumentException(
                "Expected a concrete ScriptableObject type.");

        ScriptableObject draft =
            ScriptableObject.CreateInstance(type);

        draft.hideFlags = HideFlags.DontSave;
        return draft;
    }

    public static T CreateTemporary<T>()
        where T : ScriptableObject
    {
        return (T)CreateTemporary(typeof(T));
    }

    public static void DiscardTemporary(
        ScriptableObject draft)
    {
        if (draft != null && !AssetDatabase.Contains(draft))
            UnityEngine.Object.DestroyImmediate(draft);
    }

    // For simple, single-asset content.
    // Complex creators can reuse draft handling
    // but keep their own specialized save pipelines.
    public static ScriptableObject SaveCopy(
        ScriptableObject source,
        string displayName,
        string folder)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Cannot create content in Play Mode.");

        if (source == null ||
            string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException(
                "A content name is required.");

        if (string.IsNullOrWhiteSpace(folder) ||
            !folder.StartsWith("Assets/", StringComparison.Ordinal) ||
            folder.Contains("..") ||
            folder.Contains("\\"))
            throw new ArgumentException(
                "Invalid project folder.");

        string safeName = Regex.Replace(
            displayName.Trim(),
            "[\\\\/:*?\"<>|\\x00-\\x1F]",
            "_"
        ).TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidOperationException(
                "Invalid content name.");

        EnsureFolder(folder);

        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{folder}/{safeName}.asset");

        ScriptableObject created = null;

        try
        {
            created = ScriptableObject.CreateInstance(
                source.GetType());

            EditorUtility.CopySerialized(source, created);

            created.hideFlags = HideFlags.None;
            created.name = safeName;

            AssetDatabase.CreateAsset(created, path);

            Undo.RegisterCreatedObjectUndo(
                created, "Create Forge Content");

            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();

            return created;
        }
        catch
        {
            if (created != null &&
                AssetDatabase.GetAssetPath(created) == path)
            {
                AssetDatabase.DeleteAsset(path);
            }
            else if (created != null &&
                     !AssetDatabase.Contains(created))
            {
                UnityEngine.Object.DestroyImmediate(created);
            }

            throw;
        }
    }

    private static void EnsureFolder(string folder)
    {
        string current = "Assets";
        string[] parts = folder.Split('/');

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}

#endif

