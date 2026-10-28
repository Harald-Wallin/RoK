using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TalentWindowUI : MonoBehaviour
{
    [SerializeField] private Transform tierContainer;
    [SerializeField] private GameObject tierPrefab;
    [SerializeField] private GameObject talentSlotPrefab;
    [SerializeField] private TMP_Text availablePointsText;
    //[SerializeField] private List<TalentData> allTalents;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        BuildUI();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void BuildUI()
    {
        //Debug.Log($"All talents count: {allTalents.Count}");

        foreach (Transform child in tierContainer)
        {
            Destroy(child.gameObject);
        }

        Dictionary<int, List<TalentData>> tierMap =
            new Dictionary<int, List<TalentData>>();

        foreach (var talent in TalentManager.Instance.AllTalents)
        {
            //Debug.Log($"Talent found: {talent.name}");

            if (!tierMap.ContainsKey(talent.tier))
            {
                tierMap.Add(
                    talent.tier,
                    new List<TalentData>()
                );
            }

            tierMap[talent.tier].Add(talent);
        }

        foreach (var tier in tierMap.OrderBy(t => t.Key))
        {
            GameObject tierGO = Instantiate(tierPrefab,tierContainer);
            TalentTierHeader header = tierGO.GetComponent<TalentTierHeader>();
            Transform talentContainer = tierGO.transform.Find("TalentContainer");

            if (header != null)
            {
                header.Setup(tier.Key);
            }

            foreach (var talentData in tier.Value)
            {
                TalentRuntime runtime =
                    TalentManager.Instance.talents
                    .Find(t => t.data == talentData);

                if (runtime == null)
                    continue;

                GameObject slotGO = Instantiate(talentSlotPrefab,talentContainer);

                slotGO
                    .GetComponent<TalentSlotUI>()
                    .Setup(runtime);
            }
        }
    }

    void Update()
    {

        if (availablePointsText != null && TalentManager.Instance != null)
        {
            availablePointsText.text =
                $"Talent points: {TalentManager.Instance.availablePoints}";
        }
    }

    public void Toggle()
    {
        if (canvasGroup != null)
        {
            bool isOpen = canvasGroup.alpha > 0f;

            if (!isOpen)
            {
                TalentNotificationManager.Instance
                    ?.MarkAsViewed();
            }

            canvasGroup.alpha = isOpen ? 0f : 1f;
            canvasGroup.interactable = !isOpen;
            canvasGroup.blocksRaycasts = !isOpen;
        }
        else
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    public void Open()
    {
        TalentNotificationManager.Instance ?.MarkAsViewed();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
            gameObject.SetActive(true);
    }

    public void RefreshAllSlots()
    {
        TalentSlotUI[] slots =
            GetComponentsInChildren<TalentSlotUI>();

        foreach (var slot in slots)
        {
            slot.ForceRefresh();
        }
    }

    public void Close()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
            gameObject.SetActive(false);
    }
}
