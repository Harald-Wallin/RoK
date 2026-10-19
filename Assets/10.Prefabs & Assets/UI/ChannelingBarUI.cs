using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gemensam UI-bar för alla channels.
///
/// Scriptobjektet ska alltid vara aktivt.
/// Själva presentationen göms med CanvasGroup.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChannelingBarUI :
    MonoBehaviour
{
    [Header("Visibility")]

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Content")]

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text remainingTimeText;

    [SerializeField]
    private Slider progressSlider;

    [Tooltip(
        "Valfri direktreferens till sliderns Fill-image. " +
        "Använd Fill Amount om Slider-layouten inte fungerar.")]
    [SerializeField]
    private Image progressFillImage;

    [Header("References")]

    [SerializeField]
    private CharacterChannelController
        channelController;

    private bool isSubscribed;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.value = 0f;
        }

        if (progressFillImage != null)
        {
            progressFillImage.type =
                Image.Type.Filled;

            progressFillImage.fillMethod =
                Image.FillMethod.Horizontal;

            progressFillImage.fillOrigin = 0;
            progressFillImage.fillAmount = 0f;
        }

        HideImmediately();
    }

    private void OnEnable()
    {
        TryResolveAndSubscribe();
        RefreshFromController();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        /*
         * PlayerReference kan initieras efter UI:t.
         * Därför försöker vi igen tills controllern hittas.
         */
        if (channelController == null)
        {
            TryResolveAndSubscribe();
        }

        /*
         * Polling gör baren robust även om UI:t aktiverades
         * mitt under en redan pågående channel.
         */
        RefreshFromController();
    }

    private void TryResolveAndSubscribe()
    {
        if (channelController == null)
        {
            PlayerStats player =
                PlayerReference.Player;

            if (player == null)
            {
                player =
                    FindFirstObjectByType<
                        PlayerStats>();
            }

            if (player != null)
            {
                channelController =
                    player.GetComponent<
                        CharacterChannelController>();
            }
        }

        if (channelController == null ||
            isSubscribed)
        {
            return;
        }

        channelController.ChannelStarted +=
            HandleChannelStarted;

        channelController.ChannelCompleted +=
            HandleChannelEnded;

        channelController.ChannelCancelled +=
            HandleChannelEnded;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            channelController == null)
        {
            isSubscribed = false;
            return;
        }

        channelController.ChannelStarted -=
            HandleChannelStarted;

        channelController.ChannelCompleted -=
            HandleChannelEnded;

        channelController.ChannelCancelled -=
            HandleChannelEnded;

        isSubscribed = false;
    }

    private void RefreshFromController()
    {
        if (channelController == null ||
            !channelController.IsChanneling ||
            channelController.ActiveChannel == null)
        {
            SetVisible(false);
            ResetProgress();

            return;
        }

        ChannelRuntime channel =
            channelController.ActiveChannel;

        SetVisible(true);
        RefreshContent(channel);
    }

    private void HandleChannelStarted(
        ChannelRuntime channel)
    {
        if (channel == null)
            return;

        SetVisible(true);
        RefreshContent(channel);
    }

    private void HandleChannelEnded(
        ChannelRuntime channel)
    {
        SetVisible(false);
        ResetProgress();
    }

    private void RefreshContent(
        ChannelRuntime channel)
    {
        if (channel == null)
            return;

        if (nameText != null)
        {
            nameText.text =
                channel.DisplayName;
        }

        if (iconImage != null)
        {
            bool hasIcon =
                channel.Icon != null;

            iconImage.sprite =
                channel.Icon;

            iconImage.gameObject.SetActive(
                hasIcon);
        }

        float progress =
    channel.IsReversed
        ? 1f -
            Mathf.Clamp01(
                channel.NormalizedProgress)
        : Mathf.Clamp01(
                channel.NormalizedProgress);

        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(
                progress);
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount =
                progress;
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text =
                $"{channel.RemainingTime:0.0}";
        }
    }

    private void ResetProgress()
    {
        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(
                0f);
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount =
                0f;
        }

        if (remainingTimeText != null)
        {
            remainingTimeText.text =
                string.Empty;
        }
    }

    private void SetVisible(
        bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha =
            visible
                ? 1f
                : 0f;

        canvasGroup.interactable =
            false;

        canvasGroup.blocksRaycasts =
            false;
    }

    private void HideImmediately()
    {
        SetVisible(false);
        ResetProgress();
    }
}