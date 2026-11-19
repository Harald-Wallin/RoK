using System;
using UnityEngine;

public readonly struct BuffApplicationEvent
{
    public readonly DamageSourceContext Source;
    public readonly CharacterStats Target;
    public readonly BuffDefinition Definition;
    public readonly float AddedDurationFraction;
    public readonly bool WasDispelled;

    public BuffApplicationEvent(
        DamageSourceContext source,
        CharacterStats target,
        BuffDefinition definition,
        float addedDurationFraction,
        bool wasDispelled)
    {
        Source = source;
        Target = target;
        Definition = definition;

        AddedDurationFraction =
            Mathf.Clamp01(addedDurationFraction);

        WasDispelled = wasDispelled;
    }
}

public static class BuffApplicationEvents
{
    public static event Action<BuffApplicationEvent> Applied;

    public static void Raise(BuffApplicationEvent result)
    {
        if (result.Target != null &&
            result.Definition != null)
        {
            Applied?.Invoke(result);
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        Applied = null;
    }
}
