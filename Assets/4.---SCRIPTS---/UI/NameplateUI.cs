using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Presentation controller för en karaktärs nameplate.
///
/// Layout:
///
/// Rad 1:
/// - Namn
///
/// Rad 2:
/// - Level
/// - Healthbar
/// - StatusText
///     Default = NPC-titel
///     Hover/Combat = aktuell HP
/// - FactionIcon
///
/// Rad 3:
/// - Buff/debuff-display
///
/// NameplateUI äger inte gameplaydata.
/// </summary>
public sealed class NameplateUI :
    MonoBehaviour
{
    private static readonly Dictionary<
        CharacterStats,
        NameplateUI> Registry =
            new();


    // =========================================================
    // TARGET
    // =========================================================

    [Header("Target")]

    [SerializeField]
    private CharacterStats target;


    // =========================================================
    // IDENTITY
    // =========================================================

    [Header("Identity")]

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text levelText;


    // =========================================================
    // HEALTH / STATUS
    // =========================================================

    [Header("Health / Status")]

    [SerializeField]
    private HealthBarVisual healthBarVisual;

    [SerializeField]
    [FormerlySerializedAs("hpText")]
    private TMP_Text statusText;


    // =========================================================
    // FACTION
    // =========================================================

    [Header("Faction")]

    [SerializeField]
    private FactionIconUI factionIcon;


    // =========================================================
    // BUFFS
    // =========================================================

    [Header("Buffs")]

    [SerializeField]
    private NameplateBuffDisplay buffDisplay;


    // =========================================================
    // FUTURE STYLING
    // =========================================================

    [Header("Future Styling")]

    [SerializeField]
    [Tooltip(
        "Valfri rot för framtida ramar, bossmarkörer " +
        "och andra dekorationer."
    )]
    private GameObject decorationRoot;


    // =========================================================
    // REFRESH
    // =========================================================

    [Header("Refresh")]

    [SerializeField]
    [Min(0.05f)]
    private float passiveRefreshInterval =
        0.25f;


    // =========================================================
    // RUNTIME
    // =========================================================

    private PlayerStats player;

    private PlayerReputationManager
        reputationManager;

    private CharacterStateController
        stateController;

    private BuffSystem buffSystem;

    private NameplatePresentationState
        currentState;

    private bool isHovered;
    private bool isCorpse;
    private bool subscribed;

    private float passiveRefreshTimer;


    // =========================================================
    // PUBLIC
    // =========================================================

    public CharacterStats Target =>
        target;


    public NameplatePresentationState
        CurrentState =>
            currentState;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ResolveReferences();
    }


    private void Start()
    {
        player =
            PlayerReference.Player;

        reputationManager =
            FindFirstObjectByType<
                PlayerReputationManager
            >();

        BindBuffDisplay();

        RefreshAll();
    }


    private void OnEnable()
    {
        ResolveReferences();

        player =
            PlayerReference.Player;

        Register();
        Subscribe();
    }


    private void OnDisable()
    {
        Unsubscribe();
        Unregister();
    }


    private void Update()
    {
        passiveRefreshTimer -=
            Time.deltaTime;

        if (passiveRefreshTimer > 0f)
            return;

        passiveRefreshTimer =
            passiveRefreshInterval;

        /*
         * Temporary hostility och reputation
         * saknar ännu full event-driven presentation.
         */
        RefreshState();
        RefreshHealthColor();
    }


    // =========================================================
    // REGISTRY
    // =========================================================

    public static bool TryGet(
        CharacterStats character,
        out NameplateUI nameplate)
    {
        if (character == null)
        {
            nameplate = null;
            return false;
        }

        return Registry.TryGetValue(
            character,
            out nameplate
        );
    }


    private void Register()
    {
        if (target == null)
            return;

        Registry[target] =
            this;
    }


    private void Unregister()
    {
        if (target == null)
            return;

        if (Registry.TryGetValue(
                target,
                out NameplateUI registered) &&
            registered == this)
        {
            Registry.Remove(
                target
            );
        }
    }


    // =========================================================
    // EXTERNAL STATE
    // =========================================================

    public void SetHovered(
        bool hovered)
    {
        if (isHovered == hovered)
            return;

        isHovered =
            hovered;

        RefreshState();
    }


    public void SetCorpseMode()
    {
        if (isCorpse)
            return;

        isCorpse =
            true;

        healthBarVisual?.SetHealth(
            0f,
            1f,
            true
        );

        ApplyState(
            NameplatePresentationState
                .Corpse
        );
    }


    // =========================================================
    // REFERENCES
    // =========================================================

    private void ResolveReferences()
    {
        if (target == null)
        {
            target =
                GetComponentInParent<
                    CharacterStats
                >();
        }

        if (target == null)
            return;

        stateController =
            target.GetComponent<
                CharacterStateController
            >();

        buffSystem =
            target.GetComponent<
                BuffSystem
            >();
    }


    private void BindBuffDisplay()
    {
        if (buffDisplay == null)
            return;

        buffDisplay.Bind(
            buffSystem
        );
    }


    // =========================================================
    // SUBSCRIPTIONS
    // =========================================================

    private void Subscribe()
    {
        if (subscribed ||
            target == null)
        {
            return;
        }

        target.OnHealthChanged +=
            HandleHealthChanged;

        target.OnStatsChanged +=
            HandleStatsChanged;

        target.OnDied +=
            HandleDied;

        if (stateController != null)
        {
            stateController
                .OnCombatStateChanged +=
                HandleCombatStateChanged;
        }

        if (player != null)
        {
            player.OnLevelChanged +=
                HandlePlayerLevelChanged;
        }

        subscribed =
            true;
    }


    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (target != null)
        {
            target.OnHealthChanged -=
                HandleHealthChanged;

            target.OnStatsChanged -=
                HandleStatsChanged;

            target.OnDied -=
                HandleDied;
        }

        if (stateController != null)
        {
            stateController
                .OnCombatStateChanged -=
                HandleCombatStateChanged;
        }

        if (player != null)
        {
            player.OnLevelChanged -=
                HandlePlayerLevelChanged;
        }

        subscribed =
            false;
    }


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    private void HandlePlayerLevelChanged()
    {
        RefreshIdentity();
    }


    private void HandleHealthChanged()
    {
        RefreshHealth();
        RefreshState();
    }


    private void HandleStatsChanged()
    {
        RefreshIdentity();
        RefreshHealth();
    }


    private void HandleDied(
        CharacterStats deadCharacter)
    {
        SetCorpseMode();
    }


    private void HandleCombatStateChanged(
        bool inCombat)
    {
        RefreshState();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshAll()
    {
        if (target == null)
            return;

        RefreshIdentity();

        RefreshHealth(
            true
        );

        RefreshState();
    }


    private void RefreshIdentity()
    {
        if (target == null)
            return;

        if (nameText != null)
        {
            nameText.text =
                target.DisplayName;
        }

        if (levelText != null)
        {
            levelText.text =
                target.level.ToString();

            if (player != null)
            {
                int levelDifference =
                    target.level -
                    player.level;

                levelText.color =
                    GetDifficultyColor(
                        levelDifference
                    );
            }
        }

        if (factionIcon != null)
        {
            factionIcon.Bind(
                target.faction
            );
        }

        RefreshStatusText();
    }


    private void RefreshHealth(
        bool immediate = false)
    {
        if (target == null)
            return;

        int maximumHealth =
            Mathf.Max(
                1,
                target.GetMaxHP()
            );

        healthBarVisual?.SetHealth(
            target.currentHP,
            maximumHealth,
            immediate
        );

        RefreshStatusText();
        RefreshHealthColor();
    }


    private void RefreshStatusText()
    {
        if (statusText == null ||
            target == null)
        {
            return;
        }

        if (isCorpse ||
            !target.IsAlive)
        {
            statusText.text =
                "Corpse";

            return;
        }

        bool showHealth =
            currentState ==
                NameplatePresentationState.Hover ||
            currentState ==
                NameplatePresentationState.Combat ||
            !target.HasRole;

        if (showHealth)
        {
            int maximumHealth =
                Mathf.Max(
                    1,
                    target.GetMaxHP()
                );

            statusText.text =
                $"{target.currentHP} / " +
                $"{maximumHealth}";

            return;
        }

        statusText.text =
            target.RoleName;
    }


    private void RefreshHealthColor()
    {
        if (target == null ||
            healthBarVisual == null)
        {
            return;
        }

        Color fillColor =
            Color.white;

        NPCReactionController reaction =
            target.GetComponent<
                NPCReactionController
            >();

        bool hostile =
            player != null &&
            target.IsHostileToPlayer(
                player
            );

        bool temporarilyHostile =
            reaction != null &&
            reaction
                .IsTemporarilyHostile;

        if (hostile ||
            temporarilyHostile)
        {
            fillColor =
                ReputationColorUtility
                    .GetColor(
                        1
                    );
        }
        else if (
            reputationManager != null &&
            target.faction != null)
        {
            var reputation =
                reputationManager
                    .GetReputation(
                        target.faction
                    );

            int reputationLevel =
                reputation != null
                    ? reputation.level
                    : 3;

            fillColor =
                ReputationColorUtility
                    .GetColor(
                        reputationLevel
                    );
        }

        fillColor.a =
            1f;

        healthBarVisual.SetHealthColor(
            fillColor
        );
    }


    // =========================================================
    // PRESENTATION STATE
    // =========================================================

    private void RefreshState()
    {
        NameplatePresentationState
            resolvedState =
                ResolveState();

        ApplyState(
            resolvedState
        );
    }


    private NameplatePresentationState
        ResolveState()
    {
        if (isCorpse ||
            target == null ||
            !target.IsAlive)
        {
            return
                NameplatePresentationState
                    .Corpse;
        }

        if (stateController != null &&
            stateController.InCombat)
        {
            return
                NameplatePresentationState
                    .Combat;
        }

        if (isHovered)
        {
            return
                NameplatePresentationState
                    .Hover;
        }

        return
            NameplatePresentationState
                .Default;
    }


    private void ApplyState(
        NameplatePresentationState state)
    {
        currentState =
            state;

        /*
         * Den nya nameplaten har en stabil layout.
         *
         * Vi slår därför inte längre av/på:
         * - healthbar
         * - title
         * - faction
         * - buff row
         *
         * State bestämmer endast vilket innehåll
         * StatusText visar.
         */
        RefreshStatusText();
    }


    // =========================================================
    // COLOURS
    // =========================================================

    private static Color
        GetDifficultyColor(
            int difference)
    {
        if (difference >= 5)
        {
            return
                Hex(
                    "#D51512"
                );
        }

        if (difference >= 3)
        {
            return
                Hex(
                    "#D55912"
                );
        }

        if (difference >= -2)
        {
            return
                Hex(
                    "#F5F207"
                );
        }

        if (difference >= -5)
        {
            return
                Hex(
                    "#4D9927"
                );
        }

        return
            Hex(
                "#9E9E9E"
            );
    }


    private static Color Hex(
        string hex)
    {
        return
            ColorUtility
                .TryParseHtmlString(
                    hex,
                    out Color color
                )
                ? color
                : Color.white;
    }
}