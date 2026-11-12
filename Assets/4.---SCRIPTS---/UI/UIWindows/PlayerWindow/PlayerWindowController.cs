using UnityEngine;

public sealed class PlayerWindowController :
    MonoBehaviour
{
    [SerializeField]
    private GameObject playerWindow;

    private void Awake()
    {
        if (playerWindow == null)
        {
            playerWindow =
                gameObject;
        }
    }

    private void Start()
    {
        Close();
    }

    public void Open()
    {
        if (playerWindow != null)
        {
            playerWindow.SetActive(
                true);
        }
    }

    public void Close()
    {
        if (playerWindow != null)
        {
            playerWindow.SetActive(
                false);
        }
    }

    public void Toggle()
    {
        if (playerWindow == null)
            return;

        playerWindow.SetActive(
            !playerWindow.activeSelf);
    }

    public void ToggleFromButton()
    {
        Toggle();
    }

    public bool IsOpen()
    {
        return playerWindow != null &&
               playerWindow.activeSelf;
    }
}
