using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Announcement")]
public class AnnouncementData : ScriptableObject
{
    public string message;

    public Color color = Color.white;

    public int fontSize = 32;

    public float duration = 3.5f;

    public AudioClip sound;
}
