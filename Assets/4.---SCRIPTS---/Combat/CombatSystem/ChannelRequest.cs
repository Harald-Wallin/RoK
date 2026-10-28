using UnityEngine;

/// <summary>
/// Runtime-data som beskriver en generell channel.
///
/// Klassen känner inte till mat, abilities, crafting eller
/// andra specifika system.
/// </summary>
public sealed class ChannelRequest
{
    public ChannelRequest(
        object owner,
        string displayName,
        Sprite icon,
        float duration,
        float tickInterval = 0f,
        bool isReversed = true)
    {
        Owner = owner;

        DisplayName =
            string.IsNullOrWhiteSpace(displayName)
                ? "Channeling"
                : displayName;

        Icon = icon;

        Duration =
            Mathf.Max(
                0.01f,
                duration);

        TickInterval =
            Mathf.Max(
                0f,
                tickInterval);

        IsReversed =
            isReversed;
    }

    /// <summary>
    /// Systemet eller komponenten som äger channelingen.
    ///
    /// Används för att förhindra att ett annat system
    /// av misstag avbryter fel channel.
    /// </summary>
    public object Owner
    {
        get;
    }

    public string DisplayName
    {
        get;
    }

    public Sprite Icon
    {
        get;
    }

    public float Duration
    {
        get;
    }

    /// <summary>
    /// 0 innebär att channelingen saknar ticks.
    /// </summary>
    public float TickInterval
    {
        get;
    }

    public bool IsReversed
    {
        get;
    }

    public bool HasTicks =>
        TickInterval > 0f;
}
