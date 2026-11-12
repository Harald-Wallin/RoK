using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WeaponTrainerWindowUI : MonoBehaviour, IUIWindow
{
    public static WeaponTrainerWindowUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform rowContent;
    [SerializeField] private WeaponTrainingRowUI rowPrefab;

    private readonly List<WeaponTrainingRowUI> rows = new();

    private WeaponTrainer trainer;
    private InteractionContext context;
    private PlayerCurrency currency;
    private PlayerDeedManager deeds;
    private PlayerAbilityCollection abilities;

    public bool IsOpen => panelRoot != null && panelRoot.activeInHierarchy;

    private void Awake()
    {
        if (panelRoot == null || panelRoot == gameObject ||
            closeButton == null || titleText == null ||
            rowContent == null || rowPrefab == null)
        {
            Debug.LogError("Assign the training window references. Panel Root must be a child.", this);
            enabled = false;
            return;
        }

        Instance = this;
        closeButton.onClick.AddListener(Close);
        panelRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Close();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (Instance == this)
            Instance = null;
    }

    public void Open(WeaponTrainer source, in InteractionContext interaction)
    {
        if (!isActiveAndEnabled || source == null || !interaction.IsValid)
            return;

        Close();

        trainer = source;
        context = interaction;

        EntityIdentity identity =
            EntityTargetUtility.GetIdentity(interaction.Target.InteractionOwner);

        titleText.text = identity != null && !string.IsNullOrWhiteSpace(identity.DisplayName)
            ? $"{identity.DisplayName}"
            : $"{source.name}";

        panelRoot.SetActive(true);

        HashSet<AbilityData> shown = new();

        foreach (WeaponTrainer.TrainingOffer offer in trainer.GetOffers())
        {
            if (!shown.Add(offer.Ability))
                continue;

            WeaponTrainingRowUI row = Instantiate(rowPrefab, rowContent, false);
            row.Bind(offer, context.Player, Train);
            rows.Add(row);
        }

        Subscribe();
        RefreshRows();

        GlobalUIManager.Instance?.RegisterInteractionWindow(
            this,
            context.Target.InteractionTransform,
            context.Target.WindowCloseDistance);
    }

    public void Close()
    {
        Unsubscribe();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        foreach (WeaponTrainingRowUI row in rows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        rows.Clear();
        trainer = null;
        context = default;

        GlobalUIManager.Instance?.ClearInteractionWindow(this);
    }

    private void Subscribe()
    {
        currency = PlayerCurrency.Instance;
        deeds = context.Player.GetComponent<PlayerDeedManager>();
        abilities = context.Player.GetComponent<PlayerAbilityCollection>();

        if (currency != null)
            currency.OnCoinsChanged += RefreshRows;

        if (deeds != null)
            deeds.DeedRankCompleted += OnRankCompleted;

        if (abilities != null)
            abilities.OnAbilityLearned += OnAbilityLearned;
    }

    private void Unsubscribe()
    {
        if (currency != null)
            currency.OnCoinsChanged -= RefreshRows;

        if (deeds != null)
            deeds.DeedRankCompleted -= OnRankCompleted;

        if (abilities != null)
            abilities.OnAbilityLearned -= OnAbilityLearned;

        currency = null;
        deeds = null;
        abilities = null;
    }

    private void OnRankCompleted(DeedRuntime deed, DeedRankDefinition rank) =>
        RefreshRows();

    private void OnAbilityLearned(AbilityData ability) =>
        RefreshRows();

    private void Train(AbilityData ability)
    {
        if (IsOpen && trainer != null)
            trainer.TryTrain(context, ability);

        RefreshRows();
    }

    private void RefreshRows()
    {
        if (trainer == null || context.Player == null)
            return;

        foreach (WeaponTrainingRowUI row in rows)
            row.SetState(trainer.GetTrainingState(context.Player, row.Offer));
    }
}
