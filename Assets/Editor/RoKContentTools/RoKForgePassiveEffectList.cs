
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class RoKForgePassiveEffectList
{
    public static void Draw(
     SerializedObject serialized,
     string propertyName,
     string heading,
     Action<ScriptableObject> openAsset)
    {
        SerializedProperty array = serialized.FindProperty(propertyName);
        ScriptableObject owner = serialized.targetObject as ScriptableObject;

        if (array == null || !array.isArray || owner == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            $"{heading} ({array.arraySize})",
            EditorStyles.boldLabel);

        if (array.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "No effects linked.",
                MessageType.Info);
        }

        for (int i = 0; i < array.arraySize; i++)
        {
            int index = i;
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            EffectDefinition effect = element.objectReferenceValue as EffectDefinition;

            RoKForgeVisualStyle.BeginEffectCard();
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Effect {index + 1}",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(28)))
                {
                    QueueEdit(owner, propertyName, "Move Effect Up", references =>
                    {
                        if (index > 0 && index < references.arraySize)
                            references.MoveArrayElement(index, index - 1);
                    });
                }
            }

            using (new EditorGUI.DisabledScope(index >= array.arraySize - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(28)))
                {
                    QueueEdit(owner, propertyName, "Move Effect Down", references =>
                    {
                        if (index >= 0 && index < references.arraySize - 1)
                            references.MoveArrayElement(index, index + 1);
                    });
                }
            }

            GUIContent removeLabel = effect != null
                ? new GUIContent("Unlink", "Remove this reference from the list.")
                : new GUIContent("Remove", "Remove this empty slot.");

            if (GUILayout.Button(removeLabel, GUILayout.Width(65)))
            {
                QueueEdit(owner, propertyName, "Remove Effect", references =>
                {
                    if (index < 0 || index >= references.arraySize)
                        return;

                    references.GetArrayElementAtIndex(index).objectReferenceValue = null;
                    references.DeleteArrayElementAtIndex(index);
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
                    RoKForgeLinkedField.QueueOpen(effect, openAsset);

                bool canDuplicate =
                    AssetDatabase.Contains(owner) &&
                    AssetDatabase.Contains(effect) &&
                    !EditorApplication.isPlayingOrWillChangePlaymode;

                using (new EditorGUI.DisabledScope(!canDuplicate))
                {
                    if (GUILayout.Button("Duplicate & Customize"))
                        QueueDuplicate(owner, propertyName, index, effect, openAsset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        if (GUILayout.Button(new GUIContent(
                "+ Add Reference Slot",
                "Add an empty slot for an existing effect.")))
        {
            QueueEdit(owner, propertyName, "Add Effect Reference", references =>
            {
                int index = references.arraySize;
                references.arraySize++;
                references.GetArrayElementAtIndex(index).objectReferenceValue = null;
            });
        }

        EditorGUILayout.EndVertical();
    }

    // =====================================================
    // SAFE ARRAY EDITING
    // =====================================================
    internal sealed class ExistingEffectPicker
    {
        private static int catalogVersion;

        private string search = string.Empty;
        private Vector2 scroll;
        private List<ScriptableObject> results;
        private int loadedCatalogVersion;

        static ExistingEffectPicker()
        {
            EditorApplication.projectChanged += () => catalogVersion++;
        }

        public void InvalidateCache()
        {
            results = null;
        }

        public void Draw(
            SerializedObject serialized,
            string propertyName,
            Action<ScriptableObject> openAsset)
        {
            if (serialized.targetObject is not ScriptableObject owner)
                return;

            SerializedProperty references = serialized.FindProperty(propertyName);

            if (references == null || !references.isArray)
                return;

            string nextSearch = EditorGUILayout.TextField("Search", search);

            if (nextSearch != search)
            {
                search = nextSearch;
                scroll = Vector2.zero;
                InvalidateCache();
            }

            if (results == null || loadedCatalogVersion != catalogVersion)
            {
                results = RoKContentStudioCatalog.Find(
                    RoKContentCategory.PassiveEffects,
                    search);

                loadedCatalogVersion = catalogVersion;
            }

            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No matching effects found.",
                    MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(
                scroll,
                GUILayout.MaxHeight(240));

            foreach (ScriptableObject asset in results)
            {
                if (asset is not EffectDefinition effect || effect == null)
                    continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField(effect.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    effect.GetDescription(),
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();

                bool linked = IsLinked(references, effect);

                using (new EditorGUI.DisabledScope(
                    linked || EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button(linked ? "Linked" : "Add", GUILayout.Width(55)))
                    {
                        QueueEdit(owner, propertyName, "Add Existing Effect", array =>
                        {
                            if (effect == null || IsLinked(array, effect))
                                return;

                            int index = array.arraySize;
                            array.arraySize++;
                            array.GetArrayElementAtIndex(index).objectReferenceValue = effect;
                        });
                    }
                }

                using (new EditorGUI.DisabledScope(openAsset == null))
                {
                    if (GUILayout.Button("Open", GUILayout.Width(55)))
                        RoKForgeLinkedField.QueueOpen(effect, openAsset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static bool IsLinked(
            SerializedProperty references,
            EffectDefinition effect)
        {
            for (int i = 0; i < references.arraySize; i++)
            {
                if (references.GetArrayElementAtIndex(i).objectReferenceValue == effect)
                    return true;
            }

            return false;
        }
    }
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
            RoKForgeEffectVariantWindow.Open(
                owner,
                propertyName,
                index,
                original,
                openAsset);
    }
}

#endif

