using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class WeaponComboHUD : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new(0f, 8f);

    private PlayerStats boundPlayer;
    private PlayerWeaponCombo combo;

    private RectTransform display;
    private RectTransform bar;
    private TextMeshProUGUI hitCount;
    private int displayedHits;

    private void Awake()
    {
        CreateDisplay();
    }

    private void Update()
    {
        PlayerStats player = PlayerReference.Player;

        if (boundPlayer != player || (player != null && combo == null))
        {
            boundPlayer = player;
            combo = player != null
                ? player.GetComponent<PlayerWeaponCombo>()
                : null;
        }

        if (combo == null ||
            !combo.isActiveAndEnabled ||
            !combo.TryGetComboTimer(out float fraction, out int landedHits))
        {
            display.gameObject.SetActive(false);
            return;
        }

        bar.anchorMax = new Vector2(fraction, 1f);

        if (displayedHits != landedHits)
        {
            displayedHits = landedHits;
            hitCount.text = landedHits.ToString();
        }

        display.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (display != null)
            display.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (display != null)
            Destroy(display.gameObject);
    }

    private void CreateDisplay()
    {
        display = new GameObject(
            "Weapon Combo Display",
            typeof(RectTransform),
            typeof(LayoutElement)).GetComponent<RectTransform>();

        display.SetParent(transform, false);
        display.GetComponent<LayoutElement>().ignoreLayout = true;
        display.anchorMin = new Vector2(0.5f, 1f);
        display.anchorMax = new Vector2(0.5f, 1f);
        display.pivot = new Vector2(0.5f, 0f);
        display.anchoredPosition = offset;
        display.sizeDelta = new Vector2(100f, 22f);

        Image barImage = new GameObject(
            "Timer Bar",
            typeof(RectTransform),
            typeof(Image)).GetComponent<Image>();

        barImage.color = Color.white;
        barImage.raycastTarget = false;

        bar = barImage.rectTransform;
        bar.SetParent(display, false);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = Vector2.one;
        bar.offsetMin = new Vector2(0f, -3f);
        bar.offsetMax = Vector2.zero;

        hitCount = new GameObject(
            "Hit Count",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();

        RectTransform countRect = hitCount.rectTransform;
        countRect.SetParent(display, false);
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = new Vector2(0f, -4f);

        hitCount.fontSize = 12f;
        hitCount.alignment = TextAlignmentOptions.Center;
        hitCount.color = Color.white;
        hitCount.raycastTarget = false;

        display.gameObject.SetActive(false);
    }
}
