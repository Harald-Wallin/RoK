#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeFormLayout
{
    private const float TwoColumnBreakpoint = 700f;
    private const float ColumnMinWidth = 300f;
    private const float FormMaxWidth = 720f;
    private const float ColumnGap = 12f;

    internal static bool UseTwoColumns(ScrollView workspace)
    {
        return workspace.contentViewport.resolvedStyle.width >= TwoColumnBreakpoint;
    }

    internal static void Constrain(VisualElement element)
    {
        element.style.width = Length.Percent(100);
        element.style.maxWidth = FormMaxWidth;
        element.style.alignSelf = Align.FlexStart;
    }

    internal static void DrawColumns(bool twoColumns, Action left, Action right)
    {
        if (!twoColumns)
        {
            left();
            right();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(
                GUILayout.MinWidth(ColumnMinWidth),
                GUILayout.ExpandWidth(true)))
            {
                left();
            }

            GUILayout.Space(ColumnGap);

            using (new EditorGUILayout.VerticalScope(
                GUILayout.MinWidth(ColumnMinWidth),
                GUILayout.ExpandWidth(true)))
            {
                right();
            }
        }
    }

    internal static VisualElement CreateColumns(
        ScrollView workspace,
        VisualElement left,
        VisualElement right)
    {
        VisualElement layout = new();
        Constrain(layout);

        layout.Add(left);
        layout.Add(right);

        void UpdateLayout(float width)
        {
            bool twoColumns = width >= TwoColumnBreakpoint;

            layout.style.flexDirection =
                twoColumns ? FlexDirection.Row : FlexDirection.Column;

            left.style.minWidth = twoColumns ? ColumnMinWidth : 0f;
            right.style.minWidth = twoColumns ? ColumnMinWidth : 0f;

            left.style.flexGrow = twoColumns ? 1f : 0f;
            right.style.flexGrow = twoColumns ? 1f : 0f;

            StyleLength basis = twoColumns
                ? new StyleLength(0f)
                : new StyleLength(StyleKeyword.Auto);

            left.style.flexBasis = basis;
            right.style.flexBasis = basis;

            right.style.marginLeft = twoColumns ? ColumnGap : 0f;
            right.style.marginTop = twoColumns ? 0f : ColumnGap;
        }

        VisualElement viewport = workspace.contentViewport;

        void OnViewportChanged(GeometryChangedEvent evt)
        {
            UpdateLayout(evt.newRect.width);
        }

        layout.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            viewport.RegisterCallback<GeometryChangedEvent>(OnViewportChanged);
            UpdateLayout(viewport.resolvedStyle.width);
        });

        layout.RegisterCallback<DetachFromPanelEvent>(_ =>
        {
            viewport.UnregisterCallback<GeometryChangedEvent>(OnViewportChanged);
        });

        return layout;
    }
}

#endif