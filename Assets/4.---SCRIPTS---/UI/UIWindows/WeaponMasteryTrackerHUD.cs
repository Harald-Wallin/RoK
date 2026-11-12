using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WeaponMasteryTrackerHUD : MonoBehaviour
{
    [SerializeField]
    private GameObject trackerVisuals;

    [SerializeField]
    private Image experienceFill;

    [SerializeField]
    private TMP_Text levelText;

    private PlayerWeaponMastery mastery;
    private bool configured;

    private void Awake()
    {
        configured =
            trackerVisuals != null &&
            experienceFill != null &&
            trackerVisuals != gameObject &&
            trackerVisuals.transform.IsChildOf(transform) &&
            experienceFill.transform.IsChildOf(trackerVisuals.transform);

        if (!configured)
        {
            Debug.LogError(
                "Assign the Visuals child to Tracker Visuals " +
                "and its XPFill Image to Experience Fill.",
                this);

            enabled = false;
            return;
        }

        trackerVisuals.SetActive(false);
    }

    private void Update()
    {
        PlayerStats player = PlayerReference.Player;

        PlayerWeaponMastery next = player != null
            ? player.GetComponent<PlayerWeaponMastery>()
            : null;

        if (ReferenceEquals(next, mastery))
            return;

        Unbind();

        mastery = next;

        if (mastery != null)
            mastery.Changed += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        Unbind();

        if (configured)
            trackerVisuals.SetActive(false);
    }

    private void Unbind()
    {
        if (mastery != null)
            mastery.Changed -= Refresh;

        mastery = null;
    }

    private void Refresh()
    {
        WeaponFamilyDefinition family = mastery != null
            ? mastery.TrackedFamily
            : null;

        if (family == null)
        {
            trackerVisuals.SetActive(false);
            return;
        }

        PlayerWeaponMastery.Progress progress = mastery.GetProgress(family);

        experienceFill.fillAmount = Mathf.Clamp01(progress.Fraction);

        if (levelText != null)
            levelText.text = $"{progress.Level}";

        trackerVisuals.SetActive(true);
    }
}