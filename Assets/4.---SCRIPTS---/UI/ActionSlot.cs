using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ActionSlot :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ICombatSlot
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField]
    private Image icon;

    [SerializeField]
    private Image cooldownOverlay;

    [SerializeField]
    private TMP_Text cooldownText;

    [SerializeField]
    private TMP_Text hotkeyText;

    [SerializeField]
    private RectTransform slotTransform;

    [SerializeField]
    private Image flashImage;

    // =========================================================
    // REFERENCES
    // =========================================================

    private CharacterActionController
        actionController;

    private PlayerActionInput
        playerActionInput;

    private PlayerAbilityCollection
        collection;

    // =========================================================
    // DATA
    // =========================================================

    public AbilityData ability;

    private int slotIndex;

    private KeyCode Hotkey =>
        KeyCode.Alpha1 +
        slotIndex;

    private bool UsesHoldAndRelease =>
        ability != null &&
        ability.UsesHoldAndRelease;

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
        PlayerAbilityCollection abilityCollection,
        AbilityData initializedAbility,
        int index)
    {
        collection =
            abilityCollection;

        if (collection != null)
        {
            actionController =
                collection.GetComponent<
                    CharacterActionController>();

            playerActionInput =
                collection.GetComponent<
                    PlayerActionInput>();
        }

        ability =
            initializedAbility;

        slotIndex =
            index;

        if (hotkeyText != null)
        {
            hotkeyText.text =
                (index + 1)
                .ToString();
        }

        RefreshVisual();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        HandleHotkeyInput();

        if (ability == null)
        {
            ClearCooldownUI();

            return;
        }

        UpdateCooldownUI();
    }

    // =========================================================
    // HOTKEY
    // =========================================================

    private void HandleHotkeyInput()
    {
        if (ability == null)
            return;

        if (Input.GetKeyDown(
                Hotkey))
        {
            PlayClickFeedback();

            if (UsesHoldAndRelease)
            {
                playerActionInput
                    ?.BeginAbilityHotkeyInput(
                        ability
                    );

                return;
            }

            TryActivateAbility();
        }

        if (UsesHoldAndRelease &&
            Input.GetKeyUp(
                Hotkey))
        {
            playerActionInput
                ?.ReleaseAbilityHotkeyInput(
                    ability
                );
        }
    }

    // =========================================================
    // POINTER
    // =========================================================

    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (eventData.button !=
                PointerEventData
                    .InputButton.Left ||
            !UsesHoldAndRelease)
        {
            return;
        }

        PlayClickFeedback();

        playerActionInput
            ?.BeginAbilityPointerInput(
                ability
            );
    }

    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (eventData.button !=
                PointerEventData
                    .InputButton.Left ||
            !UsesHoldAndRelease)
        {
            return;
        }

        playerActionInput
            ?.ReleaseAbilityPointerInput(
                ability
            );
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData
                .InputButton.Left)
        {
            return;
        }

        if (UsesHoldAndRelease)
            return;

        PlayClickFeedback();

        TryActivateAbility();
    }

    // =========================================================
    // ACTIVATION
    // =========================================================

    private bool TryActivateAbility()
    {
        if (ability == null ||
            ability.IsBaseAttack ||
            playerActionInput == null)
        {
            return false;
        }

        return playerActionInput
            .TryStartAbility(
                ability
            );
    }

    // =========================================================
    // COOLDOWN
    // =========================================================

    private void UpdateCooldownUI()
    {
        if (ability == null ||
            actionController == null)
        {
            ClearCooldownUI();

            return;
        }

        float remaining =
            actionController
                .GetCooldownRemaining(
                    ability
                );

        float maximum =
            actionController
                .GetMaxCooldown(
                    ability
                );

        if (remaining <= 0f ||
            maximum <= 0f)
        {
            ClearCooldownUI();

            return;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                Mathf.Clamp01(
                    remaining /
                    maximum
                );
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                Mathf.CeilToInt(
                    remaining
                ).ToString();
        }
    }

    private void ClearCooldownUI()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                0f;
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                string.Empty;
        }
    }

    // =========================================================
    // FEEDBACK
    // =========================================================

    private void PlayClickFeedback()
    {
        StopAllCoroutines();

        StartCoroutine(
            ClickFeedbackRoutine()
        );
    }

    private IEnumerator
        ClickFeedbackRoutine()
    {
        if (slotTransform == null ||
            flashImage == null)
        {
            yield break;
        }

        float elapsed =
            0f;

        const float duration =
            0.15f;

        Vector3 originalScale =
            slotTransform.localScale;

        Vector3 targetScale =
            originalScale *
            1.05f;

        flashImage.color =
            new Color(
                1f,
                1f,
                1f,
                0.6f
            );

        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            slotTransform.localScale =
                Vector3.Lerp(
                    targetScale,
                    originalScale,
                    progress
                );

            flashImage.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    Mathf.Lerp(
                        0.1f,
                        0f,
                        progress
                    )
                );

            yield return null;
        }

        slotTransform.localScale =
            originalScale;

        flashImage.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );
    }

    // =========================================================
    // DRAG & DROP
    // =========================================================

    public void OnDrop(
        PointerEventData eventData)
    {
        DraggableAbility dragged =
            eventData.pointerDrag
                ?.GetComponent<
                    DraggableAbility>();

        if (dragged == null ||
            dragged.ability == null)
        {
            return;
        }

        if (dragged.ability.IsBaseAttack)
            return;

        dragged.wasDroppedOnSlot =
            true;

        AbilityData previous =
            ability;

        SetAbility(
            dragged.ability
        );

        if (dragged.sourceSlot != null &&
            dragged.sourceSlot != this &&
            dragged.sourceSlot is
                ActionSlot oldSlot)
        {
            oldSlot.SetAbility(
                previous
            );
        }
    }

    public void SetAbility(
        AbilityData newAbility)
    {
        if (newAbility != null &&
            newAbility.IsBaseAttack)
        {
            return;
        }

        if (ability != null &&
            ability != newAbility)
        {
            playerActionInput
                ?.CancelHeldAbilityInput(
                    ability
                );
        }

        ability =
            newAbility;

        RefreshVisual();

        collection
            ?.SetEquippedAbility(
                slotIndex,
                ability
            );
    }

    private void RefreshVisual()
    {
        if (icon == null)
            return;

        DraggableAbility drag =
            icon.GetComponent<
                DraggableAbility>();

        if (ability != null)
        {
            icon.sprite =
                ability.icon;

            icon.enabled =
                true;

            icon.color =
                Color.white;

            if (drag != null)
            {
                drag.ability =
                    ability;
            }

            return;
        }

        icon.sprite =
            null;

        icon.enabled =
            false;

        if (drag != null)
        {
            drag.ability =
                null;
        }
    }

    // =========================================================
    // TOOLTIP
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (ability == null ||
            DraggableAbility.dragged ||
            ItemTooltip.Instance == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            ability,
            slotTransform,
            PlayerReference.Player,
            ItemTooltip
                .TooltipAnchorMode
                .TopRight
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance
            ?.Hide();
    }

    public void ClearSlot()
    {
        SetAbility(
            null
        );
    }

    private void OnDisable()
    {
        if (ability != null)
        {
            playerActionInput
                ?.CancelHeldAbilityInput(
                    ability
                );
        }

        ItemTooltip.Instance
            ?.Hide();
    }
}