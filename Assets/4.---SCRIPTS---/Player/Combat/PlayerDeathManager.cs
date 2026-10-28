
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;


[DisallowMultipleComponent]
[RequireComponent(typeof(BuffSystem))]
public class PlayerDeathManager : MonoBehaviour
{
    [Header("Main Death Curse")]
    [SerializeField]
    private List<InjuryEffect> mainDebuffs = new();

    [Header("Random Death Curses")]
    [SerializeField]
    private List<InjuryEffect> secondaryDebuffs = new();

    [Header("Death Curse Settings")]
    [SerializeField, Min(1)]
    private int permanentDeathThreshold = 4;

    [SerializeField, Min(1f)]
    private float durationPerDeath = 10800f;

    [Header("Debug")]
    [SerializeField]
    private int deathStacks;

    [SerializeField]
    private int totalDeathsLifetime;

    [SerializeField]
    private bool permanentStateReached;

    private BuffSystem buffs;

    public int DeathStacks => deathStacks;

    public int TotalDeathsLifetime =>
        totalDeathsLifetime;

    public bool IsDeathCursePermanent =>
        permanentStateReached;


    // =====================================================
    // DEATH SEQUENCE
    // =====================================================

    private const float DeathHoldSeconds = 2f;
    private const float FadeOutSeconds = 3f;
    private const float FadeInSeconds = 0.5f;

    private Coroutine deathRoutine;

    private CameraFollow cameraFollow;

    private GameObject fadeOverlayRoot;
    private Image fadeImage;

    public bool IsDeathSequenceActive
    {
        get;
        private set;
    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        buffs = GetComponent<BuffSystem>();
    }

    private void OnEnable()
    {
        if (buffs == null)
            buffs = GetComponent<BuffSystem>();

        if (buffs != null)
            buffs.OnBuffRemoved += OnBuffRemoved;
    }


    private void OnDisable()
    {
        if (buffs != null)
        {
            buffs.OnBuffRemoved -= OnBuffRemoved;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        IsDeathSequenceActive = false;

        if (cameraFollow != null)
            cameraFollow.Unfreeze();

        SetFadeAlpha(0f);

        if (fadeImage != null)
            fadeImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (fadeOverlayRoot != null)
            Destroy(fadeOverlayRoot);
    }


    private void OnValidate()
    {
        permanentDeathThreshold =
            Mathf.Max(1, permanentDeathThreshold);

        durationPerDeath =
            Mathf.Max(1f, durationPerDeath);
    }

    // =====================================================
    // DEATH
    // =====================================================

    public void HandleDeath(
        PlayerStats player,
        Vector3 respawnPosition)
    {
        if (player == null)
            return;

        totalDeathsLifetime++;

        if (buffs == null)
            buffs = player.GetComponent<BuffSystem>();

        if (buffs != null)
        {
            ResetCycleIfEmpty();

            deathStacks =
                Mathf.Min(
                    deathStacks + 1,
                    permanentDeathThreshold
                );

            // Permanent state is established BEFORE
            // applying the new stacks.
            permanentStateReached =
                deathStacks >=
                permanentDeathThreshold;

            float duration =
                GetCurrentDuration();

            ApplyMainDebuffs(player, duration);
            ApplyRandomDebuff(player, duration);

            // Refresh EVERY death curse,
            // not only the newly selected effect.
            SynchronizeCurseDurations(duration);
        }
        else
        {
            Debug.LogError(
                "PlayerDeathManager requires BuffSystem.",
                this
            );
        }

        RespawnPlayer(
            player,
            respawnPosition
        );
    }


    // =====================================================
    // DEATH SEQUENCE
    // =====================================================

    public void BeginDeathSequence(
        PlayerStats player,
        Vector3 respawnPosition)
    {
        if (player == null ||
            IsDeathSequenceActive)
        {
            return;
        }

        IsDeathSequenceActive = true;

        // Stop NPCs pursuing the dead player.
        ResetWorldAggro();

        ResolveCamera();
        CreateFadeOverlay();

        if (cameraFollow != null)
        {
            cameraFollow.Freeze();
        }
        else
        {
            Debug.LogWarning(
                "Death sequence could not find CameraFollow.",
                this
            );
        }

        SetFadeAlpha(0f);

        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        deathRoutine = StartCoroutine(
            RunDeathSequence(
                player,
                respawnPosition
            )
        );
    }

    private IEnumerator RunDeathSequence(
        PlayerStats player,
        Vector3 respawnPosition)
    {
        // =================================================
        // 0 - 2 SECONDS: DEATH HOLD
        // =================================================

        yield return new WaitForSeconds(
            DeathHoldSeconds
        );

        // =================================================
        // 2 - 5 SECONDS: FADE TO BLACK
        // =================================================

        yield return Fade(
            0f,
            1f,
            FadeOutSeconds
        );

        // =================================================
        // FULL BLACK: RESPAWN
        // =================================================

        // Apply curses, restore HP and teleport.
        HandleDeath(
            player,
            respawnPosition
        );

        // No visible camera travel.
        if (cameraFollow != null)
            cameraFollow.SnapToTarget();

        // =================================================
        // 5 - 5.5 SECONDS: FADE IN
        // =================================================

        yield return Fade(
            1f,
            0f,
            FadeInSeconds
        );

        // =================================================
        // RETURN CONTROL
        // =================================================

        if (cameraFollow != null)
            cameraFollow.Unfreeze();

        if (fadeImage != null)
            fadeImage.raycastTarget = false;

        IsDeathSequenceActive = false;
        deathRoutine = null;
    }

    // =====================================================
    // CAMERA
    // =====================================================

    private void ResolveCamera()
    {
        if (cameraFollow != null)
            return;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraFollow =
                mainCamera.GetComponent<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }
    }

    // =====================================================
    // SCREEN FADE
    // =====================================================

    private void CreateFadeOverlay()
    {
        if (fadeImage != null)
            return;

        fadeOverlayRoot = new GameObject(
            "Death Screen Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster)
        );

        Canvas canvas =
            fadeOverlayRoot.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        GameObject imageObject = new GameObject(
            "Black Fade",
            typeof(RectTransform),
            typeof(Image)
        );

        imageObject.transform.SetParent(
            fadeOverlayRoot.transform,
            false
        );

        RectTransform rect =
            imageObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage =
            imageObject.GetComponent<Image>();

        fadeImage.color =
            new Color(0f, 0f, 0f, 0f);

        fadeImage.raycastTarget = false;
    }

    private IEnumerator Fade(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        float elapsed = 0f;

        SetFadeAlpha(startAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            SetFadeAlpha(
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                )
            );

            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        fadeImage.color = new Color(
            0f,
            0f,
            0f,
            Mathf.Clamp01(alpha)
        );
    }


    // =====================================================
    // DURATION
    // =====================================================

    private float GetCurrentDuration()
    {
        if (permanentStateReached)
            return float.PositiveInfinity;

        return deathStacks * durationPerDeath;
    }

    private void SynchronizeCurseDurations(
        float duration)
    {
        foreach (InjuryEffect curse
                 in GetConfiguredCurses())
        {
            buffs.RefreshEffectDuration(
                curse,
                duration
            );
        }
    }

    // =====================================================
    // APPLY MAIN CURSES
    // =====================================================

    private void ApplyMainDebuffs(
        PlayerStats player,
        float duration)
    {
        if (mainDebuffs == null)
            return;

        HashSet<InjuryEffect> applied = new();

        foreach (InjuryEffect curse in mainDebuffs)
        {
            if (curse == null ||
                !applied.Add(curse))
            {
                continue;
            }

            buffs.ApplyEffect(
                curse,
                player,
                duration
            );
        }
    }

    // =====================================================
    // APPLY RANDOM SECONDARY CURSE
    // =====================================================

    private void ApplyRandomDebuff(
        PlayerStats player,
        float duration)
    {
        if (secondaryDebuffs == null)
            return;

        List<InjuryEffect> candidates = new();

        foreach (InjuryEffect curse
                 in secondaryDebuffs)
        {
            if (curse != null &&
                !candidates.Contains(curse))
            {
                candidates.Add(curse);
            }
        }

        if (candidates.Count == 0)
            return;

        InjuryEffect selected =
            candidates[
                Random.Range(0, candidates.Count)
            ];

        buffs.ApplyEffect(
            selected,
            player,
            duration
        );
    }

    // =====================================================
    // DEATH CURSE REGISTRY
    // =====================================================

    private IEnumerable<InjuryEffect>
        GetConfiguredCurses()
    {
        HashSet<InjuryEffect> seen = new();

        if (mainDebuffs != null)
        {
            foreach (InjuryEffect curse in mainDebuffs)
            {
                if (curse != null && seen.Add(curse))
                    yield return curse;
            }
        }

        if (secondaryDebuffs != null)
        {
            foreach (InjuryEffect curse in secondaryDebuffs)
            {
                if (curse != null && seen.Add(curse))
                    yield return curse;
            }
        }
    }

    // =====================================================
    // CURSE STACK COUNT
    // =====================================================

    public int GetTotalDeathCurseStacks()
    {
        if (buffs == null)
            return 0;

        int total = 0;

        foreach (InjuryEffect curse
                 in GetConfiguredCurses())
        {
            ActiveBuff active =
                buffs.GetBuff(curse);

            if (active != null)
                total += active.stacks;
        }

        return total;
    }

    // =====================================================
    // CLEANSING API
    // =====================================================

    /// <summary>
    /// Removes every registered death curse.
    /// Returns the number of stacks removed.
    ///
    /// Intended for a completed healer ritual.
    /// </summary>
    public int CleanseAllDeathCurses()
    {
        if (buffs == null)
            return 0;

        int removedStacks =
            GetTotalDeathCurseStacks();

        foreach (InjuryEffect curse
                 in GetConfiguredCurses())
        {
            buffs.RemoveEffect(curse);
        }

        ResetDeathCycle();

        return removedStacks;
    }

    // =====================================================
    // NATURAL EXPIRATION
    // =====================================================

    private void OnBuffRemoved(
        ActiveBuff removed,
        BuffSystem owner)
    {
        if (owner != buffs ||
            removed == null)
        {
            return;
        }

        ResetCycleIfEmpty();
    }

    private void ResetCycleIfEmpty()
    {
        if (GetTotalDeathCurseStacks() > 0)
            return;

        ResetDeathCycle();
    }

    private void ResetDeathCycle()
    {
        deathStacks = 0;
        permanentStateReached = false;
    }

    // =====================================================
    // WORLD AGGRO RESET
    // =====================================================

    private void ResetWorldAggro()
    {
        NPCBehavior[] mobs =
            FindObjectsByType<NPCBehavior>(
                FindObjectsSortMode.None
            );

        foreach (NPCBehavior mob in mobs)
        {
            if (mob == null)
                continue;

            mob.ResetAggro();
            mob.ReturnToSpawn();
        }
    }

    // =====================================================
    // RESPAWN
    // =====================================================

    private void RespawnPlayer(
        PlayerStats player,
        Vector3 respawnPosition)
    {
        Rigidbody2D rb =
            player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = respawnPosition;
        }

        player.transform.position =
            respawnPosition;

        player.currentHP =
            player.GetMaxHP();

        player.RaiseHealthChangedExternally();
    }
}
