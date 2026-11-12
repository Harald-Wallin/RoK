using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class WeaponTechniqueGridUI : MonoBehaviour
{
    private const int SlotCount = 15;

    [SerializeField] private WeaponTechniqueSlotUI slotPrefab;

    private readonly List<WeaponTechniqueSlotUI> slots = new();

    private WeaponFamilyDefinition family;
    private PlayerDeedManager deedManager;
    private PlayerAbilityCollection abilities;
    private DeedsWindowUI journal;

    private void Awake()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("Assign Slot Prefab on WeaponTechniqueGridUI.", this);
            return;
        }

        for (int i = 0; i < SlotCount; i++)
            slots.Add(Instantiate(slotPrefab, transform, false));
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(
        WeaponFamilyDefinition selectedFamily,
        PlayerStats player,
        DeedsWindowUI deedsWindow)
    {
        PlayerDeedManager nextDeeds =
            player != null ? player.GetComponent<PlayerDeedManager>() : null;

        PlayerAbilityCollection nextAbilities =
            player != null ? player.GetComponent<PlayerAbilityCollection>() : null;

        if (family == selectedFamily &&
            deedManager == nextDeeds &&
            abilities == nextAbilities &&
            journal == deedsWindow)
        {
            return;
        }

        Unsubscribe();

        family = selectedFamily;
        deedManager = nextDeeds;
        abilities = nextAbilities;
        journal = deedsWindow;

        if (isActiveAndEnabled)
            Subscribe();

        Refresh();
    }

    private void Subscribe()
    {
        if (deedManager != null)
        {
            deedManager.DeedProgressChanged += OnDeedChanged;
            deedManager.DeedDiscovered += OnDeedChanged;
        }

        if (abilities != null)
            abilities.OnAbilityLearned += OnAbilityLearned;
    }

    private void Unsubscribe()
    {
        if (deedManager != null)
        {
            deedManager.DeedProgressChanged -= OnDeedChanged;
            deedManager.DeedDiscovered -= OnDeedChanged;
        }

        if (abilities != null)
            abilities.OnAbilityLearned -= OnAbilityLearned;
    }

    private void OnDeedChanged(DeedRuntime deed) => Refresh();

    private void OnAbilityLearned(AbilityData ability) => Refresh();

    private void Refresh()
    {
        int nextSlot = 0;

        if (family != null && deedManager != null && slots.Count > 0)
        {
            HashSet<AbilityData> shown = new();

            foreach (DeedDefinition definition in family.TechniqueDeeds)
            {
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.PersistentId) ||
                    !deedManager.TryGetDeed(definition.PersistentId, out DeedRuntime deed) ||
                    !deed.ShouldShowCard ||
                    !deed.IsRevealed)
                {
                    continue;
                }

                foreach (DeedRankDefinition rank in deed.Definition.Ranks)
                {
                    if (rank == null || rank.Rewards == null)
                        continue;

                    foreach (DeedRewardPreview reward in rank.Rewards)
                    {
                        if (reward == null ||
                            reward.Kind != DeedRewardKind.TrainableAbility ||
                            reward.Ability == null ||
                            !shown.Add(reward.Ability))
                        {
                            continue;
                        }

                        slots[nextSlot].Bind(
                            reward.Ability,
                            deed,
                            rank,
                            abilities != null && abilities.HasLearned(reward.Ability),
                            OpenSourceDeed);

                        nextSlot++;

                        if (nextSlot == slots.Count)
                            return;
                    }
                }
            }
        }

        for (int i = nextSlot; i < slots.Count; i++)
            slots[i].Bind(null, null, null, false, null);
    }

    private void OpenSourceDeed(DeedRuntime deed, DeedRankDefinition rank)
    {
        if (journal != null)
            journal.OpenDeed(deed, rank);
    }
}