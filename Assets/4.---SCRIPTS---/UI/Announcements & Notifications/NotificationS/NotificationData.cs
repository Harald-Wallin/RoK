using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Notification")]
public class NotificationData : ScriptableObject
{
    [Header("Text")]
    public string message;

    [Header("Visual")]
    public Color textColor = Color.white;

    public int fontSize = 60;

    [Header("Timing")]
    public float duration = 2f;

    [Header("Animation")]
    public NotificationAnimationType animationType;

    [Header("Audio")]
    public AudioClip sound;
}