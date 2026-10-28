using UnityEngine;
using UnityEngine.UI;

public class TalentButtonBlinker : MonoBehaviour
{
    [SerializeField]
    private Graphic targetGraphic;

    [SerializeField]
    private float pulseSpeed = 4f;

    [SerializeField]
    private float maxAlpha = 0.8f;

    private bool blinking;

    void Start()
    {
        TalentNotificationManager.Instance
            .OnUnreadStateChanged +=
            SetBlinking;

        SetBlinking(
            TalentNotificationManager.Instance
            .HasUnreadPoints
        );

        Color c = targetGraphic.color;
        c.a = 0f;
        targetGraphic.color = c;
    }

    void OnDestroy()
    {
        if (TalentNotificationManager.Instance != null)
        {
            TalentNotificationManager.Instance
                .OnUnreadStateChanged -=
                SetBlinking;
        }
    }

    void Update()
    {
        if (!blinking)
            return;

        Color c = targetGraphic.color;

        c.a =
            Mathf.Lerp(
                maxAlpha,
                0f,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f
            );

        targetGraphic.color = c;
    }

    void SetBlinking(bool value)
    {
        blinking = value;

        if (!blinking)
        {
            Color c = targetGraphic.color;
            c.a = 0f;
            targetGraphic.color = c;
        }
    }
}