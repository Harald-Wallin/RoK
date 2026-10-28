using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuffSlotUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]

    [SerializeField]
    private Image icon;

    [SerializeField]
    private TMP_Text stackText;

    [SerializeField]
    private TMP_Text timerText;

    [Header("Timer")]

    [SerializeField]
    [Min(0f)]
    private float showTimerBelow = 10f;

    [Header("Expiration Pulse")]

    [SerializeField]
    [Min(0f)]
    private float pulseBelow = 3f;

    [SerializeField]
    [Min(0f)]
    private float pulseSpeed = 7f;

    [SerializeField]
    [Range(0f, 0.25f)]
    private float pulseStrength = 0.06f;

    private ActiveBuff buff;
    private BuffSystem ownerBuffSystem;

    public ActiveBuff Buff =>
        buff;

    public void Setup(
    ActiveBuff activeBuff,
    BuffSystem owner)
    {
        buff =
            activeBuff;

        ownerBuffSystem =
            owner;

        if (icon != null)
        {
            Sprite buffIcon =
                buff?.Icon;

            icon.sprite =
                buffIcon;

            icon.enabled =
                buffIcon != null;

            icon.gameObject.SetActive(
                buffIcon != null);
        }

        transform.localScale =
            Vector3.one;

        gameObject.SetActive(
            buff != null);

        RefreshUI();
    }

    private void Update()
    {
        if (buff == null)
        {
            Destroy(
                gameObject
            );

            return;
        }

        if (ownerBuffSystem != null &&
            !ownerBuffSystem.HasBuff(buff))
        {
            Destroy(
                gameObject
            );

            return;
        }

        if (buff.IsFinished)
        {
            Destroy(
                gameObject
            );

            return;
        }

        RefreshUI();
        HandlePulseEffect();
    }

    private void RefreshUI()
    {
        if (buff == null)
            return;

        RefreshTimer();
        RefreshStacks();
    }

    private void RefreshTimer()
    {
        if (timerText == null)
            return;

        bool permanent =
            float.IsInfinity(
                buff.RemainingTime
            );

        bool showTimer =
            !permanent &&
            buff.RemainingTime <=
            showTimerBelow;

        timerText.gameObject.SetActive(
            showTimer
        );

        if (showTimer)
        {
            timerText.text =
                FormatDuration(
                    buff.RemainingTime
                );
        }
    }

    private void RefreshStacks()
    {
        if (stackText == null)
            return;

        bool showStacks =
            buff.stacks > 1;

        stackText.gameObject.SetActive(
            showStacks
        );

        if (showStacks)
        {
            stackText.text =
                buff.stacks.ToString();
        }
    }

    private void HandlePulseEffect()
    {
        if (icon == null)
            return;

        if (float.IsInfinity(
                buff.duration) ||
            buff.RemainingTime >
                pulseBelow)
        {
            icon.transform.localScale =
                Vector3.one;

            return;
        }

        float normalizedPulse =
            Mathf.Sin(
                Time.unscaledTime *
                pulseSpeed
            );

        float scale =
            1f +
            normalizedPulse *
            pulseStrength;

        icon.transform.localScale =
            Vector3.one *
            scale;
    }

    private static string FormatDuration(
        float seconds)
    {
        return
            $"{Mathf.CeilToInt(seconds)}";
    }

    public float GetRemainingTime()
    {
        return buff != null
            ? buff.RemainingTime
            : 0f;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (buff == null ||
            ItemTooltip.Instance == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            new BuffTooltipProvider(
                buff
            ),
            icon.rectTransform,
            PlayerReference.Player,
            ItemTooltip
                .TooltipAnchorMode
                .FixedBottomLeft
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        if (icon != null)
        {
            icon.transform.localScale =
                Vector3.one;
        }
    }


}