
using UnityEngine;

public class AnnouncementRequest
{
    public AnnouncementData data;

    public string customMessage;

    public AudioClip overrideSound;

    // Optional image for announcements.
    public Sprite artwork;

    public AnnouncementRequest(
        AnnouncementData data,
        string customMessage,
        AudioClip overrideSound = null,
        Sprite artwork = null)
    {
        this.data = data;
        this.customMessage = customMessage;
        this.overrideSound = overrideSound;
        this.artwork = artwork;
    }
}
