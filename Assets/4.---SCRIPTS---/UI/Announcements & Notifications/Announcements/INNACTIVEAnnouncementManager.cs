using UnityEngine;

public class AnnouncementManager : MonoBehaviour
{
    public static AnnouncementManager Instance;

    [SerializeField]
    private AnnouncementUI announcementUI;

    void Awake()
    {
        Instance = this;
    }

    public void Show(
        string message,
        Color color,
        int fontSize,
        float duration,
        AudioClip sound = null
    )
    {
        announcementUI.Show(
            message,
            color,
            fontSize,
            duration
        );

        UISoundManager.Instance?.Play(sound);
    }
}
