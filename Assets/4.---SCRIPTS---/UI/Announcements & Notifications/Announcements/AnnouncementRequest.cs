using UnityEngine;

public class AnnouncementRequest
{
    public AnnouncementData data;

    public string customMessage;

    public AudioClip overrideSound;

    public AnnouncementRequest(
        AnnouncementData data,
        string customMessage,
        AudioClip overrideSound = null
    )
    {
        this.data = data;
        this.customMessage = customMessage;
        this.overrideSound = overrideSound;
    }
}
