using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Presenterar en Factions ikon i UI.
///
/// Ikonen hämtas direkt från Faction-asseten.
///
/// Vid hover visas factionens namn.
/// När pointern lämnar ikonen ligger namnet kvar en stund
/// innan det mjukt tonas bort.
/// </summary>
[DisallowMultipleComponent]
public sealed class FactionIconUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private GameObject hoverRoot;

    [SerializeField]
    private TMP_Text hoverText;

    [SerializeField]
    private CanvasGroup hoverCanvasGroup;


    [Header("Hover Presentation")]

    [SerializeField]
    [Min(0f)]
    private float hideDelay =
        3f;

    [SerializeField]
    [Min(0.01f)]
    private float fadeDuration =
        0.5f;


    private Faction faction;

    private Coroutine hideRoutine;


    private void Awake()
    {
        if (iconImage != null)
        {
            /*
             * Själva faction-ikonen måste ta emot
             * GraphicRaycaster-events.
             */
            iconImage.raycastTarget =
                true;
        }

        if (hoverText != null)
        {
            /*
             * Hovertexten ska inte själv fånga pointern.
             * Annars kan enter/exit börja fladdra.
             */
            hoverText.raycastTarget =
                false;
        }

        if (hoverCanvasGroup != null)
        {
            hoverCanvasGroup.interactable =
                false;

            hoverCanvasGroup.blocksRaycasts =
                false;
        }

        HideImmediate();
    }


    public void Bind(
        Faction newFaction)
    {
        faction =
            newFaction;

        bool hasIcon =
            faction != null &&
            faction.Icon != null;

        if (iconImage != null)
        {
            iconImage.sprite =
                hasIcon
                    ? faction.Icon
                    : null;
        }

        if (hoverText != null)
        {
            hoverText.text =
                faction != null
                    ? faction.DisplayName
                    : string.Empty;
        }

        StopHideRoutine();
        HideImmediate();

        gameObject.SetActive(
            hasIcon
        );
    }


    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (faction == null)
            return;

        StopHideRoutine();

        ShowImmediate();
    }


    public void OnPointerExit(
        PointerEventData eventData)
    {
        if (faction == null)
            return;

        StopHideRoutine();

        hideRoutine =
            StartCoroutine(
                DelayedFadeOut()
            );
    }


    private IEnumerator DelayedFadeOut()
    {
        if (hideDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    hideDelay
                );
        }

        if (hoverCanvasGroup == null)
        {
            HideImmediate();

            hideRoutine =
                null;

            yield break;
        }

        float elapsed =
            0f;

        float startAlpha =
            hoverCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );

            /*
             * SmoothStep ger lite mjukare fade
             * än en helt linjär övergång.
             */
            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            hoverCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    t
                );

            yield return null;
        }

        hoverCanvasGroup.alpha =
            0f;

        if (hoverRoot != null)
        {
            hoverRoot.SetActive(
                false
            );
        }

        hideRoutine =
            null;
    }


    private void ShowImmediate()
    {
        if (hoverRoot != null &&
            !hoverRoot.activeSelf)
        {
            hoverRoot.SetActive(
                true
            );
        }

        if (hoverCanvasGroup != null)
        {
            hoverCanvasGroup.alpha =
                1f;
        }
    }


    private void HideImmediate()
    {
        if (hoverCanvasGroup != null)
        {
            hoverCanvasGroup.alpha =
                0f;
        }

        if (hoverRoot != null)
        {
            hoverRoot.SetActive(
                false
            );
        }
    }


    private void StopHideRoutine()
    {
        if (hideRoutine == null)
            return;

        StopCoroutine(
            hideRoutine
        );

        hideRoutine =
            null;
    }


    private void OnDisable()
    {
        StopHideRoutine();
        HideImmediate();
    }
}