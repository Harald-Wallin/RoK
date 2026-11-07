
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public static class RoKForgePassiveEffectList
{
    private const string EffectFolder =
        "Assets/3.Effects/SharedDefinitions/ContentStudio";

    public static void Draw(
        SerializedObject serialized,
        string propertyName,
        string heading,
        Action<ScriptableObject> openAsset)
    {
        SerializedProperty array =
            serialized.FindProperty(propertyName);

        ScriptableObject owner =
            serialized.targetObject as ScriptableObject;

        if (array == null || !array.isArray || owner == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            $"{heading} ({array.arraySize})",
            EditorStyles.boldLabel);

        if (array.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "No passive effects linked.",
                MessageType.Info);
        }

        for (int i = 0; i < array.arraySize; i++)
        {
            int index = i;

            SerializedProperty element =
                array.GetArrayElementAtIndex(index);

            EffectDefinition effect =
                element.objectReferenceValue
                    as EffectDefinition;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Effect {index + 1}",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(28)))
                {
                    QueueEdit(owner, propertyName,
                        "Move Effect Up", a =>
                        {
                            if (index > 0 && index < a.arraySize)
                                a.MoveArrayElement(index, index - 1);
                        });
                }
            }

            using (new EditorGUI.DisabledScope(
                index >= array.arraySize - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(28)))
                {
                    QueueEdit(owner, propertyName,
                        "Move Effect Down", a =>
                        {
                            if (index >= 0 &&
                                index < a.arraySize - 1)
                                a.MoveArrayElement(index, index + 1);
                        });
                }
            }

            if (GUILayout.Button("Remove", GUILayout.Width(65)))
            {
                QueueEdit(owner, propertyName,
                    "Remove Effect", a =>
                    {
                        if (index < 0 || index >= a.arraySize)
                            return;

                        a.GetArrayElementAtIndex(index)
                            .objectReferenceValue = null;

                        a.DeleteArrayElementAtIndex(index);
                    });
            }

            EditorGUILayout.EndHorizontal();

            RoKForgeLinkedField.Draw(
                element,
                new GUIContent(
                    "Definition",
                    "Double-click to open in The Forge."),
                openAsset);

            if (effect != null)
            {
                EditorGUILayout.LabelField(
                    effect.GetType().Name,
                    EditorStyles.miniLabel);

                string description = effect.GetDescription();

                if (!string.IsNullOrWhiteSpace(description))
                {
                    EditorGUILayout.LabelField(
                        description,
                        EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Open"))
                {
                    RoKForgeLinkedField.QueueOpen(
                        effect, openAsset);
                }

                bool canDuplicate =
                    AssetDatabase.Contains(owner) &&
                    AssetDatabase.Contains(effect) &&
                    !EditorApplication.isPlayingOrWillChangePlaymode;

                using (new EditorGUI.DisabledScope(!canDuplicate))
                {
                    if (GUILayout.Button("Duplicate & Customize"))
                    {
                        QueueDuplicate(
                            owner,
                            propertyName,
                            index,
                            effect,
                            openAsset);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        if (GUILayout.Button("+ Add Effect Reference"))
        {
            QueueEdit(owner, propertyName,
                "Add Effect Reference", a =>
                {
                    int index = a.arraySize;
                    a.arraySize++;

                    a.GetArrayElementAtIndex(index)
                        .objectReferenceValue = null;
                });
        }

        EditorGUILayout.EndVertical();
    }

    // =====================================================
    // SAFE ARRAY EDITING
    // =====================================================

    private static void QueueEdit(
        ScriptableObject owner,
        string propertyName,
        string operation,
        Action<SerializedProperty> edit)
    {
        EditorApplication.delayCall += () =>
        {
            if (owner == null ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SerializedObject serialized = new(owner);
            serialized.Update();

            SerializedProperty array =
                serialized.FindProperty(propertyName);

            if (array == null || !array.isArray)
                return;

            bool saved = AssetDatabase.Contains(owner);

            if (saved)
                Undo.RecordObject(owner, operation);

            edit(array);

            serialized.ApplyModifiedProperties();

            if (saved)
            {
                EditorUtility.SetDirty(owner);
                AssetDatabase.SaveAssets();
            }
        };
    }

    // =====================================================
    // DUPLICATE WITHOUT MODIFYING ORIGINAL EFFECT
    // =====================================================

    private static void QueueDuplicate(
        ScriptableObject owner,
        string propertyName,
        int index,
        EffectDefinition original,
        Action<ScriptableObject> openAsset)
    {
        EditorApplication.delayCall += () =>
        {
            if (owner == null ||
                original == null ||
                !AssetDatabase.Contains(owner) ||
                !AssetDatabase.Contains(original) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SerializedObject serialized = new(owner);
            serialized.Update();

            SerializedProperty array =
                serialized.FindProperty(propertyName);

            if (array == null ||
                !array.isArray ||
                index < 0 ||
                index >= array.arraySize ||
                array.GetArrayElementAtIndex(index)
                    .objectReferenceValue != original)
                return;

            EffectDefinition copy = null;

            try
            {
                copy = (EffectDefinition)
                    RoKForgeDraftSession.SaveCopy(
                        original,
                        original.name + "_Variant",
                        EffectFolder);

                Undo.RecordObject(
                    owner,
                    "Replace With Duplicated Effect");

                array.GetArrayElementAtIndex(index)
                    .objectReferenceValue = copy;

                serialized.ApplyModifiedProperties();

                EditorUtility.SetDirty(owner);
                AssetDatabase.SaveAssets();

                openAsset?.Invoke(copy);
            }

            catch (Exception exception)
            {
                if (copy != null)
                {
                    SerializedObject check = new(owner);
                    SerializedProperty references =
                        check.FindProperty(propertyName);

                    bool attached =
                        references != null &&
                        references.isArray &&
                        index >= 0 &&
                        index < references.arraySize &&
                        references.GetArrayElementAtIndex(index)
                            .objectReferenceValue == copy;

                    // Never delete an asset that the owner
                    // is already referencing.
                    if (!attached)
                    {
                        string path = AssetDatabase.GetAssetPath(copy);

                        if (!string.IsNullOrEmpty(path))
                            AssetDatabase.DeleteAsset(path);
                    }
                }

                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Effect Duplication Failed",
                    exception.Message,
                    "OK");
            }

        };
    }
}

#endif

