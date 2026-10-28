using System;
using UnityEngine;

public enum ChargeCompletionPreviewEffectType
{
    None,

    LineEndShake
}

[Serializable]
public sealed class ChargeCompletionPreviewEffect
{
    [SerializeField]
    private ChargeCompletionPreviewEffectType effectType =
        ChargeCompletionPreviewEffectType.LineEndShake;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur långt linjens yttersta ände får röra sig."
    )]
    private float shakeAmount = 0.035f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur snabbt full-charge-skakningen rör sig."
    )]
    private float shakeSpeed = 28f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur mycket av rörelsen som sker längs linjen. " +
        "Noll innebär endast rörelse i sidled."
    )]
    private float forwardShakeInfluence = 0.15f;

    public ChargeCompletionPreviewEffectType EffectType =>
        effectType;

    public bool IsEnabled =>
        effectType !=
        ChargeCompletionPreviewEffectType.None;

    public float ShakeAmount =>
        Mathf.Max(
            0f,
            shakeAmount
        );

    public float ShakeSpeed =>
        Mathf.Max(
            0f,
            shakeSpeed
        );

    public float ForwardShakeInfluence =>
        Mathf.Clamp01(
            forwardShakeInfluence
        );

    /// <summary>
    /// Beräknar en visuell offset för targetingformens yttersta
    /// ände.
    ///
    /// Offseten påverkar endast renderingen och aldrig den
    /// verkliga targetinggeometrin.
    /// </summary>
    public Vector2 GetTerminalOffset(
        Vector2 direction,
        float time)
    {
        if (!IsEnabled ||
            ShakeAmount <= 0f)
        {
            return Vector2.zero;
        }

        direction =
            GetSafeDirection(
                direction
            );

        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        float sidewaysWave =
            Mathf.Sin(
                time *
                ShakeSpeed
            );

        /*
         * En andra våg med annan frekvens gör rörelsen mindre
         * mekanisk utan att använda Random.
         */
        float forwardWave =
            Mathf.Sin(
                time *
                ShakeSpeed *
                1.73f +
                1.2f
            );

        Vector2 sidewaysOffset =
            perpendicular *
            sidewaysWave *
            ShakeAmount;

        Vector2 forwardOffset =
            direction *
            forwardWave *
            ShakeAmount *
            ForwardShakeInfluence;

        return
            sidewaysOffset +
            forwardOffset;
    }

    private static Vector2 GetSafeDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

#if UNITY_EDITOR
    public void Validate()
    {
        shakeAmount =
            Mathf.Max(
                0f,
                shakeAmount
            );

        shakeSpeed =
            Mathf.Max(
                0f,
                shakeSpeed
            );

        forwardShakeInfluence =
            Mathf.Clamp01(
                forwardShakeInfluence
            );
    }
#endif
}
