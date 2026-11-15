#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeAssetFieldNavigation
{
    internal static void HandleMouseDown(
        RoKContentStudioWindow window,
        MouseDownEvent evt)
    {
        if (evt.button != 0 || evt.clickCount != 2)
            return;

        if (evt.target is not VisualElement target)
            return;

        bool clickedReference = false;

        for (VisualElement element = target;
             element != null;
             element = element.parent)
        {
            if (element.ClassListContains(ObjectField.objectUssClassName))
                clickedReference = true;

            if (element is not ObjectField field)
                continue;

            if (clickedReference && TryQueueOpen(window, field.value))
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }

            return;
        }
    }

    [OnOpenAsset]
    private static bool HandleIMGUIAssetOpen(int instanceId, int line)
    {
        Event current = Event.current;

        if (current == null ||
            current.rawType != EventType.MouseDown ||
            current.button != 0 ||
            current.clickCount != 2 ||
            EditorWindow.focusedWindow is not RoKContentStudioWindow window)
        {
            return false;
        }

        return TryQueueOpen(
            window, EditorUtility.InstanceIDToObject(instanceId));
    }

    private static bool TryQueueOpen(
        RoKContentStudioWindow window,
        UnityEngine.Object asset)
    {
        if (asset == null || asset is MonoScript || !AssetDatabase.Contains(asset))
            return false;

        EditorApplication.delayCall += () =>
        {
            if (window == null || asset == null)
                return;

            if (asset is ScriptableObject contentAsset)
            {
                window.ShowAsset(contentAsset);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        };

        return true;
    }
}

#endif