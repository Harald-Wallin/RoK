using System;
using UnityEngine;

[Serializable]
public sealed class AbilityTimingSettings
{
    [SerializeField]
    private ActionTimingType timingType =
        ActionTimingType.Instant;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tiden innan en Cast-action exekveras."
    )]
    private float castDuration;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Total varaktighet för en Channel-action."
    )]
    private float channelDuration;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Intervall mellan channel-ticks. " +
        "Används först när channel-execution implementeras."
    )]
    private float channelTickInterval =
        1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tiden som krävs för att nå full charge."
    )]
    private float maximumChargeDuration =
        1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Tid efter execution innan karaktären kan börja " +
        "en ny action."
    )]
    private float recoveryDuration;

    [Header("Movement During Cast")]

    [SerializeField]
    private ActionMovementSettings castMovement =
        new();

    [Header("Movement During Charge")]

    [SerializeField]
    private ActionMovementSettings chargeMovement =
        new();

    [Header("Movement During Channel")]

    [SerializeField]
    private ActionMovementSettings channelMovement =
        new();

    [Header("Movement During Recovery")]

    [SerializeField]
    private ActionMovementSettings recoveryMovement =
        new();

    public ActionTimingType TimingType =>
        timingType;

    public float CastDuration =>
        Mathf.Max(
            0f,
            castDuration
        );

    public float ChannelDuration =>
        Mathf.Max(
            0f,
            channelDuration
        );

    public float ChannelTickInterval =>
        Mathf.Max(
            0.01f,
            channelTickInterval
        );

    public float MaximumChargeDuration =>
        Mathf.Max(
            0f,
            maximumChargeDuration
        );

    public float RecoveryDuration =>
        Mathf.Max(
            0f,
            recoveryDuration
        );

    public ActionMovementSettings CastMovement =>
        castMovement;

    public ActionMovementSettings ChargeMovement =>
        chargeMovement;

    public ActionMovementSettings ChannelMovement =>
        channelMovement;

    public ActionMovementSettings RecoveryMovement =>
        recoveryMovement;

    public bool HasPreparationPhase =>
        timingType ==
            ActionTimingType.Cast ||
        timingType ==
            ActionTimingType.Charge;

    public bool HasChannelPhase =>
        timingType ==
        ActionTimingType.Channel;

    public ActionMovementSettings GetMovementSettings(
        ActionPhase phase)
    {
        return phase switch
        {
            ActionPhase.Casting =>
                castMovement,

            ActionPhase.Charging =>
                chargeMovement,

            ActionPhase.Recovery =>
                recoveryMovement,

            /*
             * När Channel senare får en egen ActionPhase
             * ska den kopplas här.
             */

            _ =>
                null
        };
    }

#if UNITY_EDITOR
    public void Validate()
    {
        castDuration =
            Mathf.Max(
                0f,
                castDuration
            );

        channelDuration =
            Mathf.Max(
                0f,
                channelDuration
            );

        channelTickInterval =
            Mathf.Max(
                0.01f,
                channelTickInterval
            );

        maximumChargeDuration =
            Mathf.Max(
                0f,
                maximumChargeDuration
            );

        recoveryDuration =
            Mathf.Max(
                0f,
                recoveryDuration
            );

        castMovement ??=
            new ActionMovementSettings();

        chargeMovement ??=
            new ActionMovementSettings();

        channelMovement ??=
            new ActionMovementSettings();

        recoveryMovement ??=
            new ActionMovementSettings();

        castMovement.Validate();
        chargeMovement.Validate();
        channelMovement.Validate();
        recoveryMovement.Validate();
    }
#endif
}