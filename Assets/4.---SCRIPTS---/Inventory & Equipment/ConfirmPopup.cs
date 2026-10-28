using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmPopup : MonoBehaviour
{
    public static ConfirmPopup Instance;

    [Header("UI")]
    [SerializeField] private GameObject root;          // Hela fullscreen-objektet
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirm;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    public void Show(string message, Action confirmAction)
    {
        root.SetActive(true);
        messageText.text = message;
        onConfirm = confirmAction;
    }

    public void OnYesClicked()
    {
        onConfirm?.Invoke();
        Close();
    }

    public void OnNoClicked()
    {
        Close();
    }

    private void Close()
    {
        root.SetActive(false);
        onConfirm = null;
    }
}

