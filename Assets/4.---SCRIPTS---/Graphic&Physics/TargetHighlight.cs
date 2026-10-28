using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generell visuell highlight för targets.
///
/// Komponenten är inte beroende av CharacterStats,
/// HumanoidVisualController eller någon specifik targettyp.
///
/// Om Visual Root lämnas tom används objektets egen hierarchy.
/// </summary>
public sealed class TargetHighlight : MonoBehaviour
{
    [Header("Visual Hierarchy")]

    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private bool includeInactiveRenderers = true;

    private readonly List<SpriteRenderer> renderers =
        new();

    private readonly Dictionary<SpriteRenderer, Color>
        originalColors =
            new();

    private bool isHighlighted;

    public bool IsHighlighted =>
        isHighlighted;

    private void Awake()
    {
        RefreshRenderers();
    }

    private void OnDisable()
    {
        ClearHighlight();
    }

    private void OnDestroy()
    {
        ClearHighlight();
    }

    /// <summary>
    /// Söker om komponentens SpriteRenderers.
    ///
    /// Kan anropas efter att utrustning eller nya visual-delar
    /// har lagts till dynamiskt.
    /// </summary>
    public void RefreshRenderers()
    {
        if (isHighlighted)
        {
            ClearHighlight();
        }

        renderers.Clear();

        Transform root =
            visualRoot != null
                ? visualRoot
                : transform;

        SpriteRenderer[] foundRenderers =
            root.GetComponentsInChildren<SpriteRenderer>(
                includeInactiveRenderers
            );

        for (int i = 0;
             i < foundRenderers.Length;
             i++)
        {
            SpriteRenderer spriteRenderer =
                foundRenderers[i];

            if (spriteRenderer == null)
                continue;

            renderers.Add(
                spriteRenderer
            );
        }
    }

    public void SetHighlight(
        Color highlightColor)
    {
        if (!isHighlighted)
        {
            CaptureOriginalColors();
            isHighlighted = true;
        }

        for (int i = 0;
             i < renderers.Count;
             i++)
        {
            SpriteRenderer spriteRenderer =
                renderers[i];

            if (spriteRenderer == null)
                continue;

            if (!originalColors.TryGetValue(
                    spriteRenderer,
                    out Color originalColor))
            {
                originalColor =
                    spriteRenderer.color;

                originalColors[
                    spriteRenderer
                ] = originalColor;
            }

            spriteRenderer.color =
                MultiplyColors(
                    originalColor,
                    highlightColor
                );
        }
    }

    public void ClearHighlight()
    {
        if (!isHighlighted &&
            originalColors.Count == 0)
        {
            return;
        }

        foreach (
            KeyValuePair<SpriteRenderer, Color>
                entry in originalColors)
        {
            if (entry.Key == null)
                continue;

            entry.Key.color =
                entry.Value;
        }

        originalColors.Clear();
        isHighlighted = false;
    }

    private void CaptureOriginalColors()
    {
        originalColors.Clear();

        for (int i = 0;
             i < renderers.Count;
             i++)
        {
            SpriteRenderer spriteRenderer =
                renderers[i];

            if (spriteRenderer == null)
                continue;

            originalColors[
                spriteRenderer
            ] = spriteRenderer.color;
        }
    }

    private static Color MultiplyColors(
        Color original,
        Color highlight)
    {
        return new Color(
            original.r * highlight.r,
            original.g * highlight.g,
            original.b * highlight.b,
            original.a
        );
    }
}
