
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnnouncementInstance : MonoBehaviour
{
    [Header("Text")]
    [SerializeField]
    private TMP_Text text;

    [Header("Artwork")]
    [SerializeField]
    private Image artworkImage;

    [Header("Animation")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    private AnnouncementData data;
    private float timer;

    // =====================================================
    // UNITY
    // =====================================================

    private void Update()
    {
        if (data == null)
            return;

        timer -= Time.deltaTime;

        float duration = Mathf.Max(
            0.01f,
            data.duration
        );

        float progress =
            1f - (timer / duration);

        transform.localScale =
            Vector3.Lerp(
                Vector3.one * 0.6f,
                Vector3.one,
                progress
            );

        if (timer <= duration * 0.5f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    Mathf.Clamp01(
                        timer / (duration * 0.5f)
                    );
            }
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // =====================================================
    // INITIALIZATION
    // =====================================================

    public void Initialize(
        AnnouncementData announcement,
        string customMessage,
        Sprite artwork = null)
    {
        if (announcement == null)
            return;

        data = announcement;

        // =================================================
        // TEXT
        // =================================================

        if (text != null)
        {
            text.richText = true;

            text.text =
                string.IsNullOrEmpty(customMessage)
                    ? data.message
                    : customMessage;

            text.color = data.color;
            text.fontSize = data.fontSize;
        }

        // =================================================
        // OPTIONAL ARTWORK
        // =================================================

        if (artworkImage != null)
        {
            artworkImage.sprite = artwork;

            artworkImage.gameObject.SetActive(
                artwork != null
            );
        }

        // =================================================
        // ANIMATION
        // =================================================

        timer = Mathf.Max(
            0.01f,
            data.duration
        );

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        transform.localScale =
            Vector3.one * 0.6f;
    }
}
