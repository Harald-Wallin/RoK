
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

        float height =
            EditorGUI.GetPropertyHeight(
                property,
                label,
                true
            );

        Rect rect =
            EditorGUILayout.GetControlRect(
                true,
                height
            );

        ScriptableObject linked =
            property.objectReferenceValue
                as ScriptableObject;

        Event current =
            Event.current;

        bool doubleClick =
            current.type == EventType.MouseDown &&
            current.clickCount >= 2 &&
            rect.Contains(current.mousePosition);

        if (doubleClick &&
            linked != null &&
            AssetDatabase.Contains(linked))
        {
            current.Use();

            QueueOpen(
                linked,
                openAsset
            );
        }

        EditorGUI.PropertyField(
            rect,
            property,
            label,
            true
        );
    }

    // =====================================================
    // UI TOOLKIT INSPECTOR REFERENCES
    // =====================================================

    public static void EnableNestedNavigation(
        InspectorElement inspector,
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

