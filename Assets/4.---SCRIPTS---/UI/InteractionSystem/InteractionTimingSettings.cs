using System;
using UnityEngine;

public enum InteractionTimingMode
{
    Instant,
    Channel,
    Charge
}

[Serializable]
public sealed class InteractionTimingSettings
{
    [SerializeField] private InteractionTimingMode mode;
    [SerializeField, Min(0.01f)] private float seconds = 3f;

    public float Duration =>
        mode == InteractionTimingMode.Instant ? 0f : Mathf.Max(0.01f, seconds);

    public bool IsReversed => mode == InteractionTimingMode.Channel;
}

public interface IInteractionTimingProvider
{
    InteractionTimingSettings InteractionTiming { get; }
}

public sealed class InteractionTimingQuery
{
    public static event Action<InteractionTimingQuery> Resolving;

    public InteractionContext Context { get; }
    public float Duration { get; private set; }
    public bool IsReversed { get; private set; }
    public string Label { get; private set; }

    private InteractionTimingQuery(
        InteractionContext context,
        IInteractionOption option)
    {
        Context = context;
        Label = option.GetPresentation().Text;

        if (option is IInteractionTimingProvider provider)
            Use(provider.InteractionTiming, Label);
    }

    public static InteractionTimingQuery Resolve(
        InteractionContext context,
        IInteractionOption option)
    {
        InteractionTimingQuery query = new(context, option);
        Resolving?.Invoke(query);
        return query;
    }

    public void Use(InteractionTimingSettings settings, string label)
    {
        if (settings == null || settings.Duration <= Duration)
            return;

        Duration = settings.Duration;
        IsReversed = settings.IsReversed;
        Label = label;
    }
}