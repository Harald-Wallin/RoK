using System;

public static class ExplorationEvents
{
    /// <summary>
    /// Den lokala spelaren har gått in i en exploration-zon.
    /// </summary>
    public static event Action<string>
        ZoneEntered;

    public static void RaiseZoneEntered(
        string zoneId)
    {
        if (string.IsNullOrWhiteSpace(
                zoneId))
        {
            return;
        }

        ZoneEntered?.Invoke(
            zoneId
        );
    }
}
