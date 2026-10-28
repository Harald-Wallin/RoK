using TMPro;
using UnityEngine;

public class AnnouncementUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text announcementText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private float timer;

    private float duration;

    private bool playing;

    public void Show(
        string message,
        Color color,
        int fontSize,
        float displayDuration
    )
    {
        announcementText.text = message;

        announcementText.color = color;

        announcementText.fontSize = fontSize;

        duration = displayDuration;

        timer = duration;

        playing = true;

        canvasGroup.alpha = 1f;

        transform.localScale = Vector3.one * 0.5f;
    }

    void Update()
    {
        if (!playing)
            return;

        timer -= Time.deltaTime;

        float normalized =
            1f - (timer / duration);

        transform.localScale =
            Vector3.Lerp(
                Vector3.one * 0.5f,
                Vector3.one,
                normalized
            );

        if (timer <= duration * 0.5f)
        {
            canvasGroup.alpha =
                timer / (duration * 0.5f);
        }

        if (timer <= 0)
        {
            canvasGroup.alpha = 0f;

            playing = false;
        }
    }
}
