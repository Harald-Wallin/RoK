using System;
using UnityEngine;

[Serializable]
public sealed class AbilityExecutionSettings
{
    [SerializeField]
    private ActionActivationMode activationMode =
        ActionActivationMode.Immediate;

    [SerializeField]
    [Min(0f)]
    private float cooldown;

    [SerializeField]
    [Tooltip(
        "Om actionen ska aktivera spelets global cooldown."
    )]
    private bool triggersGlobalCooldown = true;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Används som actionens GCD om värdet är större än 0. " +
        "Noll innebär att controllerns standardvärde används."
    )]
    private float globalCooldownOverride;

    [SerializeField]
    [Tooltip(
        "Om actionen får avbrytas under sin förberedelsefas."
    )]
    private bool canBeCancelled = true;

    [SerializeField]
    [Tooltip(
    "Om hit och dodge ska avgöras när actionen exekveras. " +
    "Stäng av detta för projektiler som avgör träffen vid impact."
)]
    private bool resolvesTargetHitImmediately = true;

    public bool ResolvesTargetHitImmediately =>
        resolvesTargetHitImmediately;

    [SerializeField]
    [Tooltip(
        "Om extern crowd control eller damage får avbryta actionen. " +
        "De exakta interrupt-reglerna implementeras senare."
    )]
    private bool canBeInterrupted = true;

    public ActionActivationMode ActivationMode =>
        activationMode;

    public float Cooldown =>
        Mathf.Max(0f, cooldown);

    public bool TriggersGlobalCooldown =>
        triggersGlobalCooldown;

    public float GlobalCooldownOverride =>
        Mathf.Max(0f, globalCooldownOverride);

    public bool UsesGlobalCooldownOverride =>
        GlobalCooldownOverride > 0f;

    public bool CanBeCancelled =>
        canBeCancelled;

    public bool CanBeInterrupted =>
        canBeInterrupted;
}
