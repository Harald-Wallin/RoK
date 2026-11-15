
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shared linked-asset navigation helpers.
///
/// IMGUI serialized reference fields support
/// double-click navigation.
///
/// UI Toolkit InspectorElement reference fields
/// support the same navigation behavior.
///
/// Actual asset editing remains owned by the
/// existing Content Studio editors.
/// </summary>
public static class RoKForgeLinkedField
{
    // =====================================================
    // IMGUI SERIALIZED REFERENCE FIELD
    // =====================================================

    public static void Draw(
    SerializedProperty property,
    GUIContent label,
    Action<ScriptableObject> openAsset)
    {
        if (property == null)
            return;

        float height = EditorGUI.GetPropertyHeight(
            property, label, includeChildren: true);

        Rect position = EditorGUILayout.GetControlRect(true, height);
        Draw(position, property, label, openAsset);
    }

    public static void Draw(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        Action<ScriptableObject> openAsset)
    {
        if (property == null)
            return;

        Event current = Event.current;

        if (openAsset != null &&
            current.type == EventType.MouseDown &&
            current.button == 0 &&
            current.clickCount >= 2 &&
            position.Contains(current.mousePosition) &&
            property.objectReferenceValue is ScriptableObject linked &&
            linked != null &&
            AssetDatabase.Contains(linked))
        {
            current.Use();
            QueueOpen(linked, openAsset);
        }

        EditorGUI.PropertyField(position, property, label, includeChildren: true);
    }

    // =====================================================
    // UI TOOLKIT INSPECTOR REFERENCES
    // =====================================================

    public static void EnableNestedNavigation(
        VisualElement inspector,
        ScriptableObject owner,
        Action<ScriptableObject> openAsset)
    {
        if (inspector == null)
            return;

        inspector.RegisterCallback<MouseDownEvent>(
            evt =>
            {
                if (evt.clickCount < 2)
                    return;

                VisualElement target =
                    evt.target as VisualElement;

                if (target == null)
                    return;

                ObjectField field =
                    target as ObjectField ??
                    target.GetFirstAncestorOfType<
                        ObjectField>();

                if (field == null)
                    return;

                ScriptableObject linked =
                    field.value as ScriptableObject;

                if (linked == null ||
                    linked == owner ||
                    !AssetDatabase.Contains(linked))
                {
                    return;
                }

                // Do not let Unity's default
                // double-click action open a
                // separate inspector window.

                evt.StopImmediatePropagation();
                evt.PreventDefault();

                QueueOpen(
                    linked,
                    openAsset
                );
            },
            TrickleDown.TrickleDown
        );
    }

    // =====================================================
    // SAFE DEFERRED NAVIGATION
    // =====================================================

    public static void QueueOpen(
        ScriptableObject asset,
        Action<ScriptableObject> openAsset)
    {
        if (asset == null)
            return;

        EditorApplication.delayCall += () =>
        {
            if (asset != null)
            {
                openAsset?.Invoke(asset);
            }
        };
    }
}

#endif

