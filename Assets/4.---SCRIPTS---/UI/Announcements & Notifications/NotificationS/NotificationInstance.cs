using TMPro;
using UnityEngine;

public sealed class NotificationInstance :
    MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private NotificationData data;

    private float timer;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();
    }

    public void Initialize(
        NotificationData notification)
    {
        if (notification == null)
            return;

        Initialize(
            notification,
            notification.message
        );
    }

    public void Initialize(
        NotificationData notification,
        string message)
    {
        if (notification == null)
            return;

        data =
            notification;

        if (text != null)
        {
            text.text =
                string.IsNullOrWhiteSpace(message)
                    ? data.message
                    : message;

            text.color =
                data.textColor;

            text.fontSize =
                data.fontSize;
        }

        timer =
            Mathf.Max(
                0.01f,
                data.duration
            );

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;
        }
    }

    private void Update()
    {
        if (data == null)
            return;

        timer -=
            Time.unscaledDeltaTime;

        HandleAnimation();

        float duration =
            Mathf.Max(
                0.01f,
                data.duration
            );

        float fadeStartTime =
            duration * 0.5f;

        if (canvasGroup != null &&
            timer <= fadeStartTime)
        {
            canvasGroup.alpha =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        fadeStartTime
                    )
                );
        }

        if (timer <= 0f)
        {
            Destroy(
                gameObject
            );
        }
    }

    private void HandleAnimation()
    {
        if (rectTransform == null)
            return;

        switch (data.animationType)
        {
            case NotificationAnimationType.FloatUp:

                rectTransform.anchoredPosition +=
                    Vector2.up *
                    50f *
                    Time.unscaledDeltaTime;

                break;
        }
    }
}