using UnityEngine;

/// <summary>
/// Markerar UI som får ta emot pointer-hover och andra
/// EventSystem-events, men som inte ska blockera world input.
///
/// Exempel:
/// - nameplates
/// - health bars över karaktärer
/// - hover-baserade world labels
///
/// Lägg komponenten på UI-objektets gemensamma rot.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldInputPassthroughUI :
    MonoBehaviour
{
}
