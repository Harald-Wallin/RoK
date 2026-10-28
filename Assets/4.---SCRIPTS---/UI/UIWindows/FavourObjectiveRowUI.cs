using TMPro;
using UnityEngine;

public sealed class FavourObjectiveRowUI :
    MonoBehaviour
{
    [SerializeField]
    private TMP_Text objectiveText;

    public void Bind(
        FavourObjectiveRuntime objective)
    {
        if (objectiveText == null)
            return;

        if (objective == null)
        {
            objectiveText.text =
                "- Missing objective";

            return;
        }

        objectiveText.text =
            $"- {objective.DisplayText}";
    }

    public void BindReturnToGiver(
        string giverName,
        bool readyToTurnIn)
    {
        if (objectiveText == null)
            return;

        string resolvedName =
            string.IsNullOrWhiteSpace(
                giverName)
                ? "the favour giver"
                : giverName;

        objectiveText.text =
            readyToTurnIn
                ? $"- Return to {resolvedName}"
                : $"- Return to {resolvedName}";
    }
}
