using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HealingFloatingText :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TMP_Text text;

    [Header("Movement")]

    [SerializeField]
    [Min(0f)]
    private float lifetime = 1.25f;

    [SerializeField]
    [Min(0f)]
    private float riseSpeed = 0.9f;

    [SerializeField]
    [Min(0f)]
    private float swayAmplitude = 0.18f;

    [SerializeField]
    [Min(0f)]
    private float swayFrequency = 5f;

    [Header("Fade")]

    [SerializeField]
    [Range(0f, 1f)]
    private float fadeStartNormalized = 0.55f;

    [Header("Critical")]

    [SerializeField]
    [Min(1f)]
    private float criticalScale = 1.25f;

    private Vector3 startPosition;
    private Vector3 initialScale;

    private float elapsed;
    private float randomPhase;

    private Color initialColor;

    public void Initialize(
        int amount,
        bool isCritical)
    {
        if (text == null)
        {
            text =
                GetComponentInChildren<
                    TMP_Text>(
                    true
                );
        }

        if (text == null)
        {
            text =
                GetComponentInChildren<
                    TMP_Text>(
                    true
                );
        }

        startPosition =
            transform.position;

        initialScale =
            transform.localScale;

        randomPhase =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f
            );

        if (text != null)
        {
            text.text =
                isCritical
                    ? $"+{amount}!"
                    : $"+{amount}";

            initialColor =
                text.color;
        }

        transform.localScale =
            isCritical
                ? initialScale *
                  criticalScale
                : initialScale;
    }

    private void Update()
    {
        elapsed +=
            Time.deltaTime;

        float normalized =
            lifetime > 0f
                ? Mathf.Clamp01(
                    elapsed /
                    lifetime
                )
                : 1f;

        float horizontalOffset =
            Mathf.Sin(
                elapsed *
                swayFrequency +
                randomPhase
            ) *
            swayAmplitude;

        transform.position =
            startPosition +
            Vector3.up *
            (
                elapsed *
                riseSpeed
            ) +
            Vector3.right *
            horizontalOffset;

        UpdateAlpha(
            normalized
        );

        if (elapsed >= lifetime)
        {
            Destroy(
                gameObject
            );
        }
    }

    private void UpdateAlpha(
        float normalized)
    {
        if (text == null)
            return;

        float alpha = 1f;

        if (normalized >
            fadeStartNormalized)
        {
            float fadeLength =
                Mathf.Max(
                    0.0001f,
                    1f -
                    fadeStartNormalized
                );

            float fadeProgress =
                (
                    normalized -
                    fadeStartNormalized
                ) /
                fadeLength;

            alpha =
                1f -
                Mathf.Clamp01(
                    fadeProgress
                );
        }

        Color color =
            initialColor;

        color.a =
            initialColor.a *
            alpha;

        text.color =
            color;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lifetime =
            Mathf.Max(
                0.01f,
                lifetime
            );

        riseSpeed =
            Mathf.Max(
                0f,
                riseSpeed
            );

        swayAmplitude =
            Mathf.Max(
                0f,
                swayAmplitude
            );

        swayFrequency =
            Mathf.Max(
                0f,
                swayFrequency
            );

        criticalScale =
            Mathf.Max(
                1f,
                criticalScale
            );
    }
#endif
}
