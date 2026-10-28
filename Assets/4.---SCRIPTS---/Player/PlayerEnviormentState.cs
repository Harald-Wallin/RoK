using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auktoritativ runtime-state för spelarens omgivning.
///
/// Just nu ansvarar den endast för Indoor / Outdoor.
///
/// Andra system kan senare lyssna på denna för:
/// - musik
/// - ambience
/// - abilities
/// - UI
/// - weather suppression
/// - lighting
///
/// Zoner registreras som separata sources.
/// Därför fungerar överlappande indoor-zoner korrekt.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerEnvironmentState :
    MonoBehaviour
{
    private readonly HashSet<UnityEngine.Object>
        indoorSources =
            new();

    public bool IsIndoors =>
        indoorSources.Count > 0;

    public event Action<bool>
        OnIndoorStateChanged;

    /// <summary>
    /// Registrerar en källa som håller spelaren indoors.
    ///
    /// Exempel:
    /// IndoorZone A
    /// IndoorZone B
    ///
    /// Om spelaren lämnar A men fortfarande står i B
    /// förblir spelaren indoors.
    /// </summary>
    public void EnterIndoor(
        UnityEngine.Object source)
    {
        if (source == null)
            return;

        bool wasIndoors =
            IsIndoors;

        indoorSources.Add(
            source
        );

        if (wasIndoors ==
            IsIndoors)
        {
            return;
        }

        OnIndoorStateChanged?.Invoke(
            true
        );
    }

    /// <summary>
    /// Tar bort en indoor-källa.
    /// </summary>
    public void ExitIndoor(
        UnityEngine.Object source)
    {
        if (source == null)
            return;

        bool wasIndoors =
            IsIndoors;

        indoorSources.Remove(
            source
        );

        CleanupDestroyedSources();

        if (wasIndoors ==
            IsIndoors)
        {
            return;
        }

        OnIndoorStateChanged?.Invoke(
            false
        );
    }

    /// <summary>
    /// Kan användas vid respawn, scene reset eller liknande.
    /// </summary>
    public void ClearIndoorState()
    {
        if (indoorSources.Count == 0)
            return;

        indoorSources.Clear();

        OnIndoorStateChanged?.Invoke(
            false
        );
    }

    private void CleanupDestroyedSources()
    {
        if (indoorSources.Count == 0)
            return;

        List<UnityEngine.Object>
            destroyedSources =
                null;

        foreach (
            UnityEngine.Object source
            in indoorSources)
        {
            if (source != null)
                continue;

            destroyedSources ??=
                new List<UnityEngine.Object>();

            destroyedSources.Add(
                source
            );
        }

        if (destroyedSources == null)
            return;

        for (int i = 0;
             i < destroyedSources.Count;
             i++)
        {
            indoorSources.Remove(
                destroyedSources[i]
            );
        }
    }
}
