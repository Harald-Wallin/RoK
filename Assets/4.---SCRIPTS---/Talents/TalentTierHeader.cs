using TMPro;
using UnityEngine;

public class TalentTierHeader : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;

    public void Setup(int tier)
    {
        int requiredPoints = TalentManager.Instance.GetTierRequirement(tier);

        headerText.text = $"Tier {tier} - Unlock: {requiredPoints} points";
    }
}
