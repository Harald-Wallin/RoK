
using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : CharacterStats
{
    public int Level => level;

    public int currentExp = 0;
    public int expToNextLevel = 112;

    [Header("LevelUpConfetti")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiDuration = 5f;

    public Action OnExpChanged;
    public Action OnLevelChanged;

    [Header("Crime")]
    public bool murderMode = false;

    // =========================================================
    // HEALTH REGENERATION
    // =========================================================

    [Header("Regen")]

    [Tooltip("Percent of max HP restored each tick (0.01 = 1%)")]
    [SerializeField]
    private float regenPercentPerTick = 0.01f;

    [Tooltip("Seconds between each regen tick")]
    [SerializeField]
    private float regenInterval = 3f;

    private float regenTimer;

    private readonly Dictionary<object, float>
        regenerationSpeedModifiers = new();

    private float regenerationSpeedMultiplier = 1f;

    // =========================================================
    // REFERENCES
    // =========================================================

    private PlayerDeathManager deathManager;

    private CharacterStateController combatState;

    // =========================================================
    // LEVEL UP
    // =========================================================

    private void ApplyLevelUpStats()
    {
        // =====================================================
        // LEVEL-UP STATS
        // =====================================================

        SetBaseStat(
            StatType.Strength,
            GetBaseStatValue(
                StatType.Strength
            ) + 1f
        );

        SetBaseStat(
            StatType.Health,
            GetBaseStatValue(
                StatType.Health
            ) + 4f
        );

        // =====================================================
        // FULL HEAL
        // =====================================================

        /*
         * Level-up återställer spelaren till full HP efter att
         * alla level-up stats har applicerats.
         *
         * ResetHealth använder det NYA MaxHP-värdet.
         */
        ResetHealth();

        // =====================================================
        // TALENT POINT
        // =====================================================

        if (TalentManager.Instance != null)
        {
            TalentManager.Instance
                .availablePoints++;
        }

        TalentNotificationManager.Instance
            ?.NotifyNewTalentPoints();

        // =====================================================
        // LEVEL-UP VFX
        // =====================================================

        if (confettiPrefab != null)
        {
            Vector3 spawnPosition =
                effectPoint != null
                    ? effectPoint.position
                    : transform.position +
                      Vector3.up * 1.5f;

            GameObject effect =
                Instantiate(
                    confettiPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            Destroy(
                effect,
                confettiDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "Confetti prefab är inte tilldelad i PlayerStats.",
                this
            );
        }
    }

    // =========================================================
    // EXPERIENCE
    // =========================================================

    public bool GainExp(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning(
                $"GainExp kräver ett positivt värde. " +
                $"Mottaget värde: {amount}.",
                this
            );

            return false;
        }

        currentExp += amount;

        bool levelChanged = false;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;

            level++;

            base.level = level;

            ApplyLevelUpStats();

            AnnouncementSpawner.Instance
                ?.ShowLevelUp(level);

            expToNextLevel =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        expToNextLevel * 1.12f
                    )
                );

            levelChanged = true;

            OnLevelChanged?.Invoke();
        }

        OnExpChanged?.Invoke();

        return true;
    }

    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        base.level = level;

        regenTimer = regenInterval;

        deathManager =
            GetComponent<PlayerDeathManager>();

        combatState =
            GetComponent<CharacterStateController>();
    }

    private void Update()
    {
        // =====================================================
        // DEATH SEQUENCE
        // =====================================================

        if (deathManager != null &&
            deathManager.IsDeathSequenceActive)
        {
            return;
        }

        // =====================================================
        // DEBUG INPUT
        // =====================================================

        // H = Take damage
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(
                new DamageResult
                {
                    damage = 5
                },
                this
            );
        }

        // J = Heal
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(5);
        }

        // =====================================================
        // PASSIVE HEALTH REGENERATION
        // =====================================================

        /*
         * CharacterStateController är auktoritativ
         * för om spelaren befinner sig i combat.
         *
         * Passiv HP-regeneration är endast tillåten
         * utanför combat.
         *
         * Direkt healing från abilities påverkas inte.
         */

        bool canRegenerate =
            combatState == null ||
            !combatState.InCombat;

        int actualMaxHP =
            Mathf.RoundToInt(
                GetStat(StatType.MaxHP)
            );

        if (canRegenerate &&
            actualMaxHP > 0 &&
            currentHP > 0 &&
            currentHP < actualMaxHP)
        {
            /*
             * RegenerationSpeedMultiplier används av
             * exempelvis SpiritRestBuff (Andens ro).
             *
             * 1.0 = normal regeneration
             * 1.5 = 50% snabbare regeneration
             */

            regenTimer -=
                Time.deltaTime *
                regenerationSpeedMultiplier;

            while (regenTimer <= 0f)
            {
                int healAmount =
                    Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            actualMaxHP *
                            regenPercentPerTick
                        )
                    );

                Heal(healAmount);

                regenTimer += regenInterval;
            }
        }
        else
        {
            /*
             * När spelaren:
             *
             * - är i combat
             * - har full HP
             * - är död
             *
             * återställs regen-timern.
             *
             * Därmed kan spelaren inte spara nästan
             * färdiga regeneration-ticks under combat.
             */
            regenTimer = regenInterval;
        }
    }

    // =========================================================
    // HEALTH REGENERATION MODIFIERS
    // =========================================================

    /// <summary>
    /// Supports temporary regeneration-speed effects.
    /// Different sources can coexist independently.
    ///
    /// 1f = normal speed.
    /// 1.5f = 50% faster regeneration.
    /// </summary>
    public void SetRegenerationSpeedModifier(
        object source,
        float multiplier)
    {
        if (source == null)
            return;

        if (Mathf.Approximately(multiplier, 1f))
        {
            regenerationSpeedModifiers.Remove(source);
        }
        else
        {
            regenerationSpeedModifiers[source] =
                Mathf.Max(0.01f, multiplier);
        }

        regenerationSpeedMultiplier = 1f;

        foreach (float value
                 in regenerationSpeedModifiers.Values)
        {
            regenerationSpeedMultiplier *= value;
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    protected override void Die(
        CharacterStats killer)
    {
        // Prevent duplicate death processing.
        if (deathManager == null ||
            deathManager.IsDeathSequenceActive)
        {
            return;
        }

        PlayerRespawnManager respawn =
            GetComponent<PlayerRespawnManager>();

        if (respawn == null)
        {
            Debug.LogError(
                "Player is missing PlayerRespawnManager.",
                this
            );

            return;
        }

        // Existing death event and cleanup.
        RaiseDied(this);

        HandleDeathCleanup();

        // Interrupt active abilities without
        // clearing their cooldowns.
        CharacterActionController actions =
            GetComponent<CharacterActionController>();

        actions?.InterruptForDeath();

        // Cancel any active channel.
        CharacterChannelController channels =
            GetComponent<CharacterChannelController>();

        channels?.CancelActiveChannel();

        // Start the delayed death sequence.
        deathManager.BeginDeathSequence(
            this,
            respawn.GetRespawnPosition()
        );
    }

    // =========================================================
    // LEGACY RESPAWN
    // =========================================================

    private void Respawn()
    {
        // TODO: replace with runestone system later
        transform.position = Vector3.zero;

        currentHP = GetMaxHP();

        RaiseHealthChanged();

        ResetHealth();
    }

    public void RaiseHealthChangedExternally()
    {
        RaiseHealthChanged();
    }
}
