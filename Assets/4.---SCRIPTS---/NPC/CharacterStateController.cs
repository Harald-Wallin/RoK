using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterStats))]
public class CharacterStateController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float combatDuration = 5f;

    private CharacterStats stats;

    private float combatTimer;
    private bool isPlayer;
    private PlayerDeathManager playerDeathManager;
    private CharacterStateController combatState;


    public bool InCombat { get; private set; }

    public event Action OnEnteredCombat;
    public event Action OnLeftCombat;

    public event Action<bool> OnCombatStateChanged;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        isPlayer = GetComponent<PlayerStats>() != null;

        if (isPlayer)
        {
            playerDeathManager =
                GetComponent<PlayerDeathManager>();
        }
    }

    void Update()
    {
        UpdateCombatTimer();
    }

    void UpdateCombatTimer()
    {
        if (!InCombat)
            return;

        combatTimer -= Time.deltaTime;

        if (combatTimer <= 0f)
        {
            LeaveCombat();
        }
    }

    public void EnterCombat()
    {

        if (InCombat)
        {
            combatTimer = combatDuration;
            return;
        }

        InCombat = true;
        combatTimer = combatDuration;

        OnEnteredCombat?.Invoke();
        OnCombatStateChanged?.Invoke(true);

        if (isPlayer)
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.enteringCombat);
        }
    }

    public void RefreshCombat()
    {
        if (!InCombat)
        {
            EnterCombat();
            return;
        }

        combatTimer = combatDuration;
    }

    public void ForceLeaveCombat()
    {
        LeaveCombat();
    }

    private void LeaveCombat()
    {
        if (!InCombat)
            return;

        InCombat = false;

        OnLeftCombat?.Invoke();
        OnCombatStateChanged?.Invoke(false);

        if (isPlayer)
        {
            NotificationSpawner.Instance?.Show(
                NotificationSpawner.Instance.Database.leavingCombat);
        }
    }

    public void NotifyCombatActivity()
    {
        RefreshCombat();
    }


    private readonly HashSet<object> gameplayLocks =
        new();

    public bool HasGameplayLock =>
        gameplayLocks.Count > 0;

    /// <summary>
    /// Blocks movement, actions and interactions,
    /// but still allows rotation.
    ///
    /// Multiple systems may own independent locks.
    /// </summary>
    public void SetGameplayLocked(
        object owner,
        bool locked)
    {
        if (owner == null)
            return;

        if (locked)
        {
            gameplayLocks.Add(owner);
        }
        else
        {
            gameplayLocks.Remove(owner);
        }
    }



    //---------------------------------------------------
    // STATE WRAPPERS
    //---------------------------------------------------

    public bool IsDead =>
        stats.currentHP <= 0;

    public bool IsAlive =>
        !IsDead;

    public bool IsStunned =>
        stats.IsStunned;

    private bool IsDeathSequenceLocked =>
        isPlayer &&
        playerDeathManager != null &&
        playerDeathManager.IsDeathSequenceActive;

    //---------------------------------------------------
    // PERMISSIONS
    //---------------------------------------------------

    public bool CanMove =>
        IsAlive &&
        !IsStunned &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanRotate =>
        IsAlive &&
        !IsStunned &&
        !IsDeathSequenceLocked;

    public bool CanAttack =>
        IsAlive &&
        !IsStunned &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanUseAbilities =>
        IsAlive &&
        !IsStunned &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanInteract =>
        IsAlive &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanTalk =>
        IsAlive &&
        !InCombat &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanEat =>
        IsAlive &&
        !InCombat &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanLoot =>
        IsAlive &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanOpenVendor =>
        IsAlive &&
        !InCombat &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

    public bool CanOpenChest =>
        IsAlive &&
        !IsDeathSequenceLocked &&
        !HasGameplayLock;

}