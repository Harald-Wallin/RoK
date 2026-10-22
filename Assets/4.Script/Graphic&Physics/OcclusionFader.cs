using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generell fade-controller för SpriteRenderers.
///
/// Komponenten vet inte VARFÖR objektet ska fade:a.
/// Andra system registrerar endast att objektet är occluded.
///
/// Exempel:
/// - IndoorZone
/// - framtida TreeOcclusionDetector
/// - scripted events
///
/// Flera sources kan begära fade samtidigt.
/// Objektet blir synligt igen först när samtliga sources
/// har släppt sin begäran.
/// </summary>
[DisallowMultipleComponent]
public sealed class OcclusionFader :
    MonoBehaviour
{
    [Header("Renderers")]

    [SerializeField]
    [Tooltip(
        "Om alla SpriteRenderers under detta objekt automatiskt " +
        "ska inkluderas."
    )]
    private bool includeChildren =
        true;

    [SerializeField]
    private SpriteRenderer[] renderers;

    [Header("Fade")]

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Alpha-multiplikator när objektet är transparent. " +
        "0 = helt osynligt, 0.15 = 15% av original alpha."
    )]
    private float occludedAlphaMultiplier =
        0.12f;

    [SerializeField]
    [Min(0f)]
    private float fadeDuration =
        0.2f;

    private readonly HashSet<UnityEngine.Object>
        occlusionSources =
            new();

    private float[] originalAlpha;

    private float currentVisibility =
        1f;

    private float targetVisibility =
        1f;

    public bool IsOccluded =>
        occlusionSources.Count > 0;

    public float CurrentVisibility =>
        currentVisibility;

    private void Awake()
    {
        ResolveRenderers();
        CacheOriginalAlpha();

        ApplyVisibility(
            1f
        );
    }

    private void OnValidate()
    {
        occludedAlphaMultiplier =
            Mathf.Clamp01(
                occludedAlphaMultiplier
            );

        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );
    }

    private void Update()
    {
        float desiredVisibility =
            IsOccluded
                ? occludedAlphaMultiplier
                : 1f;

        targetVisibility =
            desiredVisibility;

        if (Mathf.Approximately(
                currentVisibility,
                targetVisibility))
        {
            return;
        }

        if (fadeDuration <= 0f)
        {
            currentVisibility =
                targetVisibility;

            ApplyVisibility(
                currentVisibility
            );

            return;
        }

        float speed =
            1f /
            fadeDuration;

        currentVisibility =
            Mathf.MoveTowards(
                currentVisibility,
                targetVisibility,
                speed *
                Time.deltaTime
            );

        ApplyVisibility(
            currentVisibility
        );
    }

    /// <summary>
    /// Registrerar eller tar bort en fade-request.
    /// </summary>
    public void SetOccluded(
        UnityEngine.Object source,
        bool occluded)
    {
        if (source == null)
            return;

        if (occluded)
        {
            occlusionSources.Add(
                source
            );
        }
        else
        {
            occlusionSources.Remove(
                source
            );
        }

        targetVisibility =
            IsOccluded
                ? occludedAlphaMultiplier
                : 1f;
    }

    public void AddOcclusion(
        UnityEngine.Object source)
    {
        SetOccluded(
            source,
            true
        );
    }

    public void RemoveOcclusion(
        UnityEngine.Object source)
    {
        SetOccluded(
            source,
            false
        );
    }

    /// <summary>
    /// Omedelbar reset till helt synlig.
    /// </summary>
    public void ClearOcclusion()
    {
        occlusionSources.Clear();

        targetVisibility =
            1f;

        currentVisibility =
            1f;

        ApplyVisibility(
            1f
        );
    }

    public void ConfigureFade(
    float occludedAlpha,
    float duration)
    {
        occludedAlphaMultiplier =
            Mathf.Clamp01(
                occludedAlpha
            );


        fadeDuration =
            Mathf.Max(
                0f,
                duration
            );


        targetVisibility =
            IsOccluded
                ? occludedAlphaMultiplier
                : 1f;
    }

    public void RefreshRenderers()
    {
        ResolveRenderers();

        CacheOriginalAlpha();

        ApplyVisibility(
            currentVisibility
        );
    }

    private void ResolveRenderers()
    {
        if (!includeChildren &&
            renderers != null &&
            renderers.Length > 0)
        {
            return;
        }

        if (!includeChildren)
            return;

        renderers =
            GetComponentsInChildren<
                SpriteRenderer>(
                true
            );
    }

    private void CacheOriginalAlpha()
    {
        if (renderers == null)
        {
            originalAlpha =
                System.Array.Empty<float>();

            return;
        }

        originalAlpha =
            new float[
                renderers.Length
            ];

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            originalAlpha[i] =
                renderer != null
                    ? renderer.color.a
                    : 1f;
        }
    }

    private void ApplyVisibility(
        float visibility)
    {
        if (renderers == null ||
            originalAlpha == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                renderers.Length,
                originalAlpha.Length
            );

        for (int i = 0;
             i < count;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            if (renderer == null)
                continue;

            Color color =
                renderer.color;

            /*
             * RGB lämnas orört.
             *
             * Vi modifierar endast original-spritens alpha.
             */
            color.a =
                originalAlpha[i] *
                visibility;

            renderer.color =
                color;
        }
    }

    private void OnDisable()
    {
        /*
         * Komponenten ska aldrig lämna sprites halvtransparenta
         * bara för att GameObject/component stängdes av.
         */
        currentVisibility =
            1f;

        targetVisibility =
            1f;

        occlusionSources.Clear();

        ApplyVisibility(
            1f
        );
    }
}
