using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Avgör om UI under muspekaren ska blockera world input.
///
/// UI under WorldInputPassthroughUI får fortfarande ta emot
/// hover-events, men räknas inte som blockerande UI.
/// </summary>
public static class WorldUIInputUtility
{
    private static readonly List<RaycastResult>
        RaycastResults =
            new();

    private static EventSystem cachedEventSystem;

    private static PointerEventData
        pointerEventData;

    /// <summary>
    /// Returnerar true om muspekaren befinner sig över minst
    /// ett UI-element som ska blockera world input.
    ///
    /// UI under WorldInputPassthroughUI ignoreras.
    /// </summary>
    public static bool IsPointerOverBlockingUI()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
            return false;

        EnsurePointerEventData(
            eventSystem
        );

        pointerEventData.Reset();

        pointerEventData.position =
            Input.mousePosition;

        RaycastResults.Clear();

        eventSystem.RaycastAll(
            pointerEventData,
            RaycastResults
        );

        for (int i = 0;
             i < RaycastResults.Count;
             i++)
        {
            GameObject hitObject =
                RaycastResults[i]
                    .gameObject;

            if (hitObject == null)
                continue;

            WorldInputPassthroughUI
                passthrough =
                    hitObject
                        .GetComponentInParent<
                            WorldInputPassthroughUI
                        >();

            /*
             * Nameplates och annan passthrough-UI får ta emot
             * hover-events men blockerar inte world input.
             */
            if (passthrough != null)
                continue;

            /*
             * Minst ett vanligt UI-element träffades.
             *
             * Inventory, vendor, popup-fönster, buttons och
             * andra UI-element blockerar därför fortfarande.
             */
            return true;
        }

        return false;
    }

    private static void EnsurePointerEventData(
        EventSystem eventSystem)
    {
        if (pointerEventData != null &&
            cachedEventSystem ==
            eventSystem)
        {
            return;
        }

        cachedEventSystem =
            eventSystem;

        pointerEventData =
            new PointerEventData(
                eventSystem
            );
    }
}