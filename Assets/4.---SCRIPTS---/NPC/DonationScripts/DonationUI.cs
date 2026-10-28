using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DonationUI : MonoBehaviour, IUIWindow
{
    public static DonationUI Instance;
    public bool IsOpen =>
    canvasGroup != null &&
    canvasGroup.alpha > 0f;

    [Header("Window")]
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private TMP_Text npcNameText;

    [Header("Item")]
    [SerializeField] private Image itemIcon;

    [SerializeField] private TMP_Text ownedAmountText;

    [Header("Quantity")]
    [SerializeField] private Slider quantitySlider;

    [SerializeField] private TMP_InputField quantityInput;

    [Header("Rewards")]
    [SerializeField] private TMP_Text reputationRewardText;

    [SerializeField] private TMP_Text expRewardText;

    [Header("Buttons")]
    [SerializeField] private Button giveButton;

    [SerializeField] private Button giveAllButton;

    private ReputationDonationNPC activeNPC;

    private bool suppressEvents = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Close();

        quantitySlider.onValueChanged.AddListener(OnSliderChanged);

        quantityInput.onValueChanged.AddListener(OnInputChanged);

        giveButton.onClick.AddListener(Give);

        giveAllButton.onClick.AddListener(GiveAll);
    }

    public void Open(ReputationDonationNPC npc)
    {
        activeNPC = npc;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        CharacterStats stats =
            npc.GetComponent<CharacterStats>();

        if (stats != null)
        {
            npcNameText.text = stats.displayName;
        }

        ItemData item = npc.RequiredItem;

        if (item != null)
        {
            itemIcon.sprite = item.icon;
        }

        int owned =
            Inventory.Instance.GetItemCount(item);

        ownedAmountText.text =
            $"Owned: {owned}";

        quantitySlider.minValue = 0;
        quantitySlider.maxValue = owned;

        SetQuantity(Mathf.Min(1, owned));
    }

    public void Close()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        activeNPC = null;

        GlobalUIManager.Instance?
            .ClearInteractionWindow(this);
    }

    void OnSliderChanged(float value)
    {
        if (suppressEvents)
            return;

        SetQuantity(Mathf.RoundToInt(value));
    }

    void OnInputChanged(string value)
    {
        if (suppressEvents)
            return;

        int parsed = 0;

        int.TryParse(value, out parsed);

        int max =
            Mathf.RoundToInt(quantitySlider.maxValue);

        parsed = Mathf.Clamp(parsed, 0, max);

        SetQuantity(parsed);
    }

    void SetQuantity(int amount)
    {
        suppressEvents = true;

        quantitySlider.value = amount;

        quantityInput.text = amount.ToString();

        UpdateRewardTexts(amount);

        suppressEvents = false;
    }

    void UpdateRewardTexts(int amount)
    {
        if (activeNPC == null)
            return;

        int rep =
            activeNPC.ReputationPerItem * amount;

        int exp =
            activeNPC.ExperiencePerItem * amount;

        reputationRewardText.text =
            $"+{rep} Reputation";

        expRewardText.text =
            $"+{exp} EXP";
    }

    void Give()
    {
        if (activeNPC == null)
            return;

        int amount =
            Mathf.RoundToInt(quantitySlider.value);

        if (amount <= 0)
            return;

        activeNPC.Donate(amount);

        Refresh();
    }

    void GiveAll()
    {
        if (activeNPC == null)
            return;

        int amount =
            Inventory.Instance.GetItemCount(
                activeNPC.RequiredItem
            );

        if (amount <= 0)
            return;

        activeNPC.Donate(amount);

        Refresh();
    }

    void Refresh()
    {
        if (activeNPC == null)
            return;

        Open(activeNPC);
    }
}
