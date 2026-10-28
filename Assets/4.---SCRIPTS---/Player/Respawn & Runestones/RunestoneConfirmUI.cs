using TMPro;
using UnityEngine;

public class RunestoneConfirmUI : MonoBehaviour
{
    public static RunestoneConfirmUI Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text questionText;

    private RunestoneInteract currentRunestone;
    string hexColor = "#c91b12";

    void Awake()
    {
        Instance = this;
        Close();
    }

    public void Open(RunestoneInteract runestone)
    {
        currentRunestone = runestone;

        panel.SetActive(true);

        questionText.text =
            $"Make <color={hexColor}>{runestone.GetRunestoneName()}</color> your spawnpoint?";
    }

    public void Confirm()
    {
        if (currentRunestone != null)
        {
            currentRunestone.ConfirmActivation();
        }

        Close();
    }

    public void Cancel()
    {
        Close();
    }

    public void Close()
    {
        panel.SetActive(false);
        currentRunestone = null;
    }
}