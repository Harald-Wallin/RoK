
using UnityEngine;

/// <summary>
/// Healer NPC role.
///
/// Interaction:
/// - Right-click through InteractionManager
/// - Confirm via existing ConfirmPopup
/// - Channel through CharacterChannelController
/// - Cleanse all death curses on completion
///
/// No separate interaction or channel system.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class NPCHealer :
    MonoBehaviour,
    IInteractionOption
{
    private const float SecondsPerStack = 5f;
    private const float MaxPositionDrift = 0.05f;
    private const float SpiritRestDuration = 600f;

    [Header("Presentation (optional)")]
    [SerializeField]
    private Sprite spiritIcon;

    private CharacterStats healerStats;
    private NPCReactionController reaction;

    private PlayerStats ritualPlayer;
    private PlayerMovement ritualMovement;
    private CharacterStateController ritualState;
    private CharacterChannelController ritualChannel;
    private PlayerDeathManager ritualDeathManager;
    private InteractionTarget ritualTarget;

    private Vector2 ritualStartPosition;

    public bool IsPerformingRitual =>
        ritualChannel != null &&
        ritualChannel.IsOwnedBy(this);

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        healerStats =
            GetComponent<CharacterStats>();

        reaction =
            GetComponent<NPCReactionController>();
    }

    private void Update()
    {
        if (ritualPlayer == null)
            return;

        if (!IsPerformingRitual)
        {
            ReleaseRitual();
            return;
        }

        // Movement input cancels the ritual.
        if (ritualMovement == null ||
            ritualMovement.MovementInput.sqrMagnitude >
                0.0001f)
        {
            CancelRitual();
            return;
        }

        // Also detect knockback, teleportation,
        // physics displacement and similar movement.
        Vector2 currentPosition =
            ritualPlayer.transform.position;

        if ((currentPosition - ritualStartPosition)
                .sqrMagnitude >
            MaxPositionDrift * MaxPositionDrift)
        {
            CancelRitual();
            return;
        }

        // The healer must remain available
        // and within interaction distance.
        if (ritualTarget == null ||
            !ritualTarget.IsWithinInteractionDistance(
                ritualPlayer.transform) ||
            healerStats == null ||
            !healerStats.IsAlive ||
            (reaction != null &&
             reaction.IsTemporarilyHostile))
        {
            CancelRitual();
        }
    }

    private void OnDisable()
    {
        CancelRitual();
    }

    // =====================================================
    // INTERACTION OPTION
    // =====================================================

    public InteractionCategory Category =>
        InteractionCategory.Other;

    public InteractionPresentation GetPresentation()
    {
        return new InteractionPresentation(
            Category,
            "Seek cleansing"
        );
    }

    public string GetStatusText()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return string.Empty;

        PlayerDeathManager death =
            player.GetComponent<PlayerDeathManager>();

        if (death == null)
            return string.Empty;

        int stacks =
            death.GetTotalDeathCurseStacks();

        if (stacks <= 0)
            return string.Empty;

        return $"{stacks * SecondsPerStack:0}s";
    }

    public bool CanInteract(
        in InteractionContext context)
    {
        if (!context.IsValid ||
            context.Target.InteractionOwner != gameObject)
        {
            return false;
        }

        return CanBeginRitual(
            context.Player,
            context.Target
        );
    }

    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        ConfirmPopup popup =
            ConfirmPopup.Instance;

        if (popup == null)
        {
            Debug.LogWarning(
                "NPCHealer requires the existing ConfirmPopup.",
                this
            );
            return;
        }

        PlayerStats player = context.Player;
        InteractionTarget target = context.Target;

        PlayerDeathManager death =
            player.GetComponent<PlayerDeathManager>();

        int stacks =
            death.GetTotalDeathCurseStacks();

        float duration =
            stacks * SecondsPerStack;

        popup.Show(
            $"The spirits have left their mark upon you.\n\n" +
            $"Cleansing requires {duration:0} seconds " +
            $"of stillness.\n\n" +
            $"Begin the ritual?",
            () => TryBeginRitual(player, target)
        );
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private bool CanBeginRitual(
        PlayerStats player,
        InteractionTarget target)
    {
        if (!isActiveAndEnabled ||
            player == null ||
            target == null ||
            healerStats == null ||
            !healerStats.IsAlive ||
            ritualPlayer != null)
        {
            return false;
        }

        if (reaction != null &&
            reaction.IsTemporarilyHostile)
        {
            return false;
        }

        if (!player.IsAlive ||
            !target.IsWithinInteractionDistance(
                player.transform))
        {
            return false;
        }

        CharacterStateController state =
            player.GetComponent<CharacterStateController>();

        if (state == null ||
            !state.CanTalk)
        {
            return false;
        }

        CharacterActionController actions =
            player.GetComponent<CharacterActionController>();

        if (actions != null &&
            actions.HasActiveAction)
        {
            return false;
        }

        CharacterChannelController channel =
            player.GetComponent<CharacterChannelController>();

        if (channel == null ||
            channel.IsChanneling)
        {
            return false;
        }

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null ||
            movement.MovementInput.sqrMagnitude >
                0.0001f)
        {
            return false;
        }

        PlayerDeathManager death =
            player.GetComponent<PlayerDeathManager>();

        return death != null &&
               !death.IsDeathSequenceActive &&
               death.GetTotalDeathCurseStacks() > 0;
    }

    // =====================================================
    // START RITUAL
    // =====================================================

    private void TryBeginRitual(
        PlayerStats player,
        InteractionTarget target)
    {
        // Validate again after the confirmation popup.
        // The player or NPC may have moved meanwhile.
        if (!CanBeginRitual(player, target))
            return;

        PlayerDeathManager death =
            player.GetComponent<PlayerDeathManager>();

        CharacterChannelController channel =
            player.GetComponent<CharacterChannelController>();

        int stacks =
            death.GetTotalDeathCurseStacks();

        ChannelRequest request =
            new ChannelRequest(
                this,
                "Helarens vaka",
                spiritIcon,
                stacks * SecondsPerStack,
                tickInterval: 0f,
                isReversed: false
            );

        if (!channel.TryStartChannel(request))
            return;

        ritualPlayer = player;

        ritualMovement =
            player.GetComponent<PlayerMovement>();

        ritualState =
            player.GetComponent<CharacterStateController>();

        ritualChannel = channel;
        ritualDeathManager = death;
        ritualTarget = target;

        ritualStartPosition =
            player.transform.position;

        // Block movement and actions, not rotation.
        ritualState.SetGameplayLocked(this, true);

        Subscribe();
    }

    // =====================================================
    // CHANNEL EVENTS
    // =====================================================

    private void Subscribe()
    {
        ritualMovement.MovementInputStarted +=
            HandleMovementStarted;

        ritualPlayer.OnDamagedBy +=
            HandlePlayerDamaged;

        ritualPlayer.OnDied +=
            HandlePlayerDied;

        ritualChannel.ChannelCompleted +=
            HandleChannelCompleted;

        ritualChannel.ChannelCancelled +=
            HandleChannelCancelled;
    }

    private void HandleMovementStarted()
    {
        CancelRitual();
    }

    private void HandlePlayerDamaged(
        CharacterStats attacker)
    {
        CancelRitual();
    }

    private void HandlePlayerDied(
        CharacterStats deadCharacter)
    {
        CancelRitual();
    }

    private void HandleChannelCompleted(
        ChannelRuntime completed)
    {
        if (!IsOurChannel(completed))
            return;

        PlayerStats player = ritualPlayer;
        PlayerDeathManager death = ritualDeathManager;

        bool validCompletion =
            player != null &&
            player.IsAlive &&
            death != null &&
            ritualMovement != null &&
            ritualMovement.MovementInput.sqrMagnitude <
                0.0001f &&
            ((Vector2)player.transform.position -
                ritualStartPosition).sqrMagnitude <=
                MaxPositionDrift * MaxPositionDrift &&
            ritualTarget != null &&
            ritualTarget.IsWithinInteractionDistance(
                player.transform) &&
            healerStats != null &&
            healerStats.IsAlive &&
            (reaction == null ||
             !reaction.IsTemporarilyHostile);

        ReleaseRitual();

        if (!validCompletion)
            return;

        // All curses disappear together.
        int removedStacks =
            death.CleanseAllDeathCurses();

        if (removedStacks <= 0)
            return;

        ApplySpiritRest(player);
    }

    private void HandleChannelCancelled(
        ChannelRuntime cancelled)
    {
        if (!IsOurChannel(cancelled))
            return;

        ReleaseRitual();
    }

    private bool IsOurChannel(
        ChannelRuntime channel)
    {
        return channel != null &&
               ReferenceEquals(channel.Owner, this);
    }

    // =====================================================
    // CANCELLATION / CLEANUP
    // =====================================================

    private void CancelRitual()
    {
        if (ritualChannel != null &&
            ritualChannel.IsOwnedBy(this))
        {
            ritualChannel.CancelChannel(this);
        }

        ReleaseRitual();
    }

    private void ReleaseRitual()
    {
        if (ritualMovement != null)
        {
            ritualMovement.MovementInputStarted -=
                HandleMovementStarted;
        }

        if (ritualPlayer != null)
        {
            ritualPlayer.OnDamagedBy -=
                HandlePlayerDamaged;

            ritualPlayer.OnDied -=
                HandlePlayerDied;
        }

        if (ritualChannel != null)
        {
            ritualChannel.ChannelCompleted -=
                HandleChannelCompleted;

            ritualChannel.ChannelCancelled -=
                HandleChannelCancelled;
        }

        if (ritualState != null)
        {
            ritualState.SetGameplayLocked(
                this,
                false
            );
        }

        ritualPlayer = null;
        ritualMovement = null;
        ritualState = null;
        ritualChannel = null;
        ritualDeathManager = null;
        ritualTarget = null;
    }

    // =====================================================
    // POST-CLEANSING BUFF
    // =====================================================

    private void ApplySpiritRest(
        PlayerStats player)
    {
        BuffSystem buffs =
            player.GetComponent<BuffSystem>();

        if (buffs == null)
            return;

        // Refresh instead of stacking the bonus.
        foreach (ActiveBuff active
                 in buffs.GetActiveBuffs())
        {
            if (active is SpiritRestBuff)
            {
                buffs.RemoveBuff(active);
                break;
            }
        }

        buffs.AddRuntimeBuff(
            new SpiritRestBuff(
                spiritIcon,
                SpiritRestDuration
            )
        );
    }
}


// =========================================================
// SPIRIT REST BUFF
// =========================================================

/// <summary>
/// Temporary positive effect granted after cleansing.
///
/// Uses the existing BuffSystem lifecycle.
/// Not a MonoBehaviour.
/// </summary>
public sealed class SpiritRestBuff : ActiveBuff
{
    private readonly Sprite buffIcon;

    public SpiritRestBuff(
        Sprite icon,
        float lifetime)
    {
        buffIcon = icon;
        duration = Mathf.Max(1f, lifetime);
    }

    public override string Name => "Andens ro";

    public override Sprite Icon => buffIcon;

    public override bool RemoveOnDeath => true;

    public override bool RemoveOnEncounterReset => true;

    public override string GetDescription(
        CharacterStats viewer)
    {
        return
            "Health regeneration is 50% faster.";
    }

    public override void OnApplied(
        CharacterStats target)
    {
        if (target is PlayerStats player)
        {
            player.SetRegenerationSpeedModifier(
                this,
                1.5f
            );
        }
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += deltaTime;
    }

    public override void OnRemoved(
        CharacterStats target)
    {
        if (target is PlayerStats player)
        {
            player.SetRegenerationSpeedModifier(
                this,
                1f
            );
        }
    }
}

