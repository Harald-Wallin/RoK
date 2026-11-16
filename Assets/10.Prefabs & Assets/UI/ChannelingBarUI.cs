using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChannelingBarUI : MonoBehaviour
{
    private const float CancelFadeSeconds = 0.25f;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Content")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;

    [Header("References")]
    [SerializeField] private CharacterChannelController channelController;

    private CharacterActionController actionController;
    private Graphic fillGraphic;
    private Color normalFillColor;
    private float cancelFadeUntil;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
        }

        if (progressFillImage != null)
        {
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = 0;
            fillGraphic = progressFillImage;
        }
        else if (progressSlider != null && progressSlider.fillRect != null)
        {
            fillGraphic = progressSlider.fillRect.GetComponent<Graphic>();
        }

        if (fillGraphic != null)
            normalFillColor = fillGraphic.color;

        HideBar();
    }

    private void OnEnable()
    {
        ResolveControllers();
        RefreshBar();
    }

    private void OnDisable()
    {
        if (actionController != null)
            actionController.OnCastCancelledByMovement -= HandleMovementCancel;

        actionController = null;
        ClearCancelFeedback();
        HideBar();
    }

    private void LateUpdate()
    {
        if (channelController == null || actionController == null)
            ResolveControllers();

        RefreshBar();
    }

    private void ResolveControllers()
    {
        if (channelController == null)
        {
            PlayerStats player = PlayerReference.Player;

            if (player == null)
                player = FindFirstObjectByType<PlayerStats>();

            if (player != null)
                channelController = player.GetComponent<CharacterChannelController>();
        }

        if (actionController != null)
            return;

        if (channelController != null)
        {
            actionController =
                channelController.GetComponent<CharacterActionController>();
        }
        else if (PlayerReference.Player != null)
        {
            actionController =
                PlayerReference.Player.GetComponent<CharacterActionController>();
        }

        if (actionController != null)
            actionController.OnCastCancelledByMovement += HandleMovementCancel;
    }

    private void RefreshBar()
    {
        ChannelRuntime channel =
            channelController != null ? channelController.ActiveChannel : null;

        if (channel != null)
        {
            ClearCancelFeedback();

            float progress = Mathf.Clamp01(channel.NormalizedProgress);

            if (channel.IsReversed)
                progress = 1f - progress;

            DrawTimingContent(
                channel.DisplayName, channel.Icon, progress,
                $"{channel.RemainingTime:0.0}");

            SetAlpha(1f);
            return;
        }

        ActionContext action =
            actionController != null ? actionController.CurrentContext : null;

        if (action != null &&
            action.Phase == ActionPhase.Casting &&
            action.Ability != null)
        {
            ClearCancelFeedback();

            float progress = Mathf.Clamp01(action.NormalizedProgress);
            float remaining =
                action.Ability.TimingSettings.CastDuration * (1f - progress);

            DrawTimingContent(
                action.Ability.abilityName, action.Ability.icon,
                progress, $"{remaining:0.0}");

            SetAlpha(1f);
            return;
        }

        if (cancelFadeUntil > Time.unscaledTime)
        {
            SetAlpha(Mathf.Clamp01(
                (cancelFadeUntil - Time.unscaledTime) / CancelFadeSeconds));
            return;
        }

        ClearCancelFeedback();
        HideBar();
    }

    private void HandleMovementCancel(ActionContext action)
    {
        if (action?.Ability == null)
            return;

        float progress = Mathf.Clamp(action.NormalizedProgress, 0.06f, 1f);

        DrawTimingContent(
            action.Ability.abilityName, action.Ability.icon,
            progress, string.Empty);

        if (fillGraphic != null)
        {
            fillGraphic.color = new Color(
                0.9f, 0.18f, 0.18f, normalFillColor.a);
        }

        cancelFadeUntil = Time.unscaledTime + CancelFadeSeconds;
        SetAlpha(1f);
    }

    private void ClearCancelFeedback()
    {
        if (cancelFadeUntil == 0f)
            return;

        cancelFadeUntil = 0f;

        if (fillGraphic != null)
            fillGraphic.color = normalFillColor;
    }

    private void DrawTimingContent(
        string displayName,
        Sprite icon,
        float progress,
        string remainingTime)
    {
        if (nameText != null)
            nameText.text = displayName;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        SetProgress(progress);

        if (remainingTimeText != null)
            remainingTimeText.text = remainingTime;
    }

    private void SetProgress(float progress)
    {
        if (progressSlider != null)
            progressSlider.SetValueWithoutNotify(progress);

        if (progressFillImage != null)
            progressFillImage.fillAmount = progress;
    }

    private void HideBar()
    {
        SetAlpha(0f);
        SetProgress(0f);

        if (remainingTimeText != null)
            remainingTimeText.text = string.Empty;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}