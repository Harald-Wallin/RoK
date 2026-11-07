#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

internal sealed class RoKForgeEffectVariantWindow : EditorWindow
{
    private const string EffectFolder =
        "Assets/3.Effects/SharedDefinitions/ContentStudio";

    private ScriptableObject owner;
    private string propertyName;
    private int index;
    private EffectDefinition original;
    private EffectDefinition draft;
    private SerializedObject draftSerialized;
    private Action<ScriptableObject> openAsset;
    private string variantName;
    private Vector2 scroll;

    internal static void Open(
        ScriptableObject owner,
        string propertyName,
        int index,
        EffectDefinition original,
        Action<ScriptableObject> openAsset)
    {
        if (owner == null ||
            original == null ||
            !AssetDatabase.Contains(owner) ||
            !AssetDatabase.Contains(original) ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        RoKForgeEffectVariantWindow window =
            CreateInstance<RoKForgeEffectVariantWindow>();

        try
        {
            window.owner = owner;
            window.propertyName = propertyName;
            window.index = index;
            window.original = original;
            window.openAsset = openAsset;
            window.variantName = original.name + "_Variant";

            if (!window.TryGetOriginalReference(out _))
            {
                DestroyImmediate(window);
                return;
            }

            window.draft = (EffectDefinition)
                RoKForgeDraftSession.CreateTemporary(original.GetType());

            EditorUtility.CopySerialized(original, window.draft);
            window.draft.hideFlags = HideFlags.DontSave;

            window.draftSerialized = new SerializedObject(window.draft);
            window.titleContent = new GUIContent("Customize Effect");
            window.minSize = new Vector2(400f, 280f);
            window.ShowUtility();
        }
        catch (Exception exception)
        {
            DestroyImmediate(window);
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Could Not Open Effect Draft",
                exception.Message,
                "OK");
        }
    }

    private void OnGUI()
    {
        if (draft == null)
        {
            EditorGUILayout.HelpBox(
                "This draft is no longer available.",
                MessageType.Info);

            if (GUILayout.Button("Close"))
                Close();

            return;
        }

        bool canSave =
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            TryGetOriginalReference(out _);

        EditorGUILayout.LabelField(
            "Customize Effect",
            EditorStyles.boldLabel);

        if (original != null)
        {
            EditorGUILayout.LabelField(
                "Original",
                original.name);
        }

        variantName = EditorGUILayout.TextField("New Asset Name", variantName);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        draftSerialized.Update();
        SerializedProperty property = draftSerialized.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.propertyPath != "m_Script")
                EditorGUILayout.PropertyField(property, includeChildren: true);
        }

        draftSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorGUILayout.EndScrollView();

        if (!canSave)
        {
            EditorGUILayout.HelpBox(
                "Saving is unavailable in Play Mode or if the original " +
                "effect is no longer in the selected slot.",
                MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Cancel"))
        {
            EditorGUILayout.EndHorizontal();
            Close();
            return;
        }

        bool save;
        using (new EditorGUI.DisabledScope(
            !canSave || string.IsNullOrWhiteSpace(variantName)))
        {
            save = GUILayout.Button("Save As New");
        }

        EditorGUILayout.EndHorizontal();

        if (save)
            SaveVariant();
    }

    private bool TryGetOriginalReference(out SerializedProperty reference)
    {
        reference = null;

        if (owner == null ||
            original == null ||
            !AssetDatabase.Contains(owner) ||
            !AssetDatabase.Contains(original))
        {
            return false;
        }

        SerializedObject serialized = new(owner);
        SerializedProperty array = serialized.FindProperty(propertyName);

        if (array == null || !array.isArray ||
            index < 0 || index >= array.arraySize)
        {
            return false;
        }

        reference = array.GetArrayElementAtIndex(index);
        return reference.objectReferenceValue == original;
    }

    private void SaveVariant()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !TryGetOriginalReference(out _))
        {
            return;
        }

        EffectDefinition saved = null;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Save Effect Variant");

        try
        {
            saved = (EffectDefinition)RoKForgeDraftSession.SaveCopy(
                draft,
                variantName,
                EffectFolder);

            // Saving/importing the new asset can invoke editor callbacks.
            // Read the owner's reference again before replacing it.
            if (!TryGetOriginalReference(out SerializedProperty reference))
            {
                throw new InvalidOperationException(
                    "The selected effect slot changed while saving.");
            }

            SerializedObject serialized = reference.serializedObject;
            reference.objectReferenceValue = saved;

            if (!serialized.ApplyModifiedProperties())
            {
                throw new InvalidOperationException(
                    "The effect reference could not be replaced.");
            }

            EditorUtility.SetDirty(owner);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);

            if (saved != null)
            {
                string path = AssetDatabase.GetAssetPath(saved);

                if (!string.IsNullOrEmpty(path))
                    AssetDatabase.DeleteAsset(path);
            }

            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Could Not Save Effect Variant",
                exception.Message,
                "OK");

            return;
        }

        Action<ScriptableObject> navigate = openAsset;
        Close();
        RoKForgeLinkedField.QueueOpen(saved, navigate);
    }

    private void OnDisable()
    {
        RoKForgeDraftSession.DiscardTemporary(draft);
        draft = null;
        draftSerialized = null;
    }
}

#endif
