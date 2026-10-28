using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [SerializeField]
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
    }

    public void Play(AudioClip clip)
    {
        //Debug.Log("UISoundManager.Play called");

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
