using TMPro;
using UnityEngine;

public class AnnouncementInstance : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private AnnouncementData data;

    private float timer;

    void Update()
    {
        if (data == null)
            return;

        timer -= Time.deltaTime;

        float progress =
            1f - (timer / data.duration);

        transform.localScale =
            Vector3.Lerp(
                Vector3.one * 0.6f,
                Vector3.one,
                progress
            );

        if (timer <= data.duration * 0.5f)
        {
            canvasGroup.alpha =
                timer /
                (data.duration * 0.5f);
        }

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(AnnouncementData announcement,string customMessage)
    {
        data = announcement;

        text.richText = true;
        text.text =string.IsNullOrEmpty(customMessage)? data.message: customMessage;
        text.color = data.color;
        text.fontSize = data.fontSize;

        timer = data.duration;

        canvasGroup.alpha = 1f;

        transform.localScale = Vector3.one * 0.6f;
    }
}
