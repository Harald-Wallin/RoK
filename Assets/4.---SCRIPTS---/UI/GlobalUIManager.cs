using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central hantering av globala UI-hotkeys och öppna
/// interaktionsfönster.
///
/// Vanliga UI-fönster, såsom Inventory och Spellbook,
/// refereras direkt genom Inspectorn.
///
/// Interaktionsfönster, såsom Vendor, Donation och Favours,
/// registreras när de öppnas och stängs automatiskt när
/// spelaren går för långt från interaktionskällan.
/// </summary>
[DisallowMultipleComponent]
public sealed class GlobalUIManager : MonoBehaviour
{
    [Header("Global Windows")]

    [SerializeField]
    private SpellbookUI spellbookUI;

    [SerializeField]
    private InventoryUI inventoryUI;

    [SerializeField]
    private PlayerWindowController playerWindow;

    [SerializeField]
    private TalentWindowUI talentWindow;

    [SerializeField]
    private ReputationWindowUI reputationWindow;

    [SerializeField]
    private LootUI lootUI;

    [SerializeField]
    private DeedsWindowUI deedsWindow;

    [Header("Interaction Windows")]

    [SerializeField]
    private VendorUI vendorUI;

    [SerializeField]
    private DonationUI donationUI;

    [SerializeField]
    private FavourWindow favourWindow;

    [Header("References")]

    [Tooltip(
        "Spelarens transform. Om fältet lämnas tomt försöker " +
        "managern använda PlayerReference.Player.")]
    [SerializeField]
    private Transform playerTransform;

    private IUIWindow currentInteractionWindow;
    private Transform currentInteractionSource;
    private float currentInteractionCloseDistance;

    public static GlobalUIManager Instance
    {
        get;
        private set;
    }

    public IUIWindow CurrentInteractionWindow =>
        currentInteractionWindow;

    public Transform CurrentInteractionSource =>
        currentInteractionSource;

    public bool HasInteractionWindow =>
        currentInteractionWindow != null &&
        currentInteractionWindow.IsOpen;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera GlobalUIManager hittades. " +
                "Den nya komponenten tas bort.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveMissingReferences();
        ResolvePlayerTransform();
    }

    private void Update()
    {
        UpdateInteractionWindow();

        if (IsTypingInInputField())
            return;

        HandleHotkeys();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleSpellbook();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePlayerWindow();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTalentWindow();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            ToggleReputationWindow();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleDeedsWindow();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAll();
        }
    }

    // =========================================================
    // INTERACTION WINDOWS
    // =========================================================

    /// <summary>
    /// Registrerar ett öppet UI-fönster som tillhör ett objekt
    /// i världen.
    ///
    /// Endast ett interaction-fönster kan vara registrerat åt
    /// gången. Ett tidigare öppet interaction-fönster stängs
    /// innan det nya registreras.
    /// </summary>
    public void RegisterInteractionWindow(
        IUIWindow window,
        Transform source,
        float closeDistance)
    {
        if (window == null)
        {
            Debug.LogWarning(
                "Ett null-fönster försökte registreras som " +
                "interaction window.",
                this);

            return;
        }

        if (source == null)
        {
            Debug.LogWarning(
                "Ett interaction-fönster försökte registreras " +
                "utan en giltig source-transform.",
                this);

            return;
        }

        if (currentInteractionWindow != null &&
            !ReferenceEquals(
                currentInteractionWindow,
                window))
        {
            currentInteractionWindow.Close();
        }

        currentInteractionWindow = window;
        currentInteractionSource = source;
        currentInteractionCloseDistance =
            Mathf.Max(0f, closeDistance);
    }

    /// <summary>
    /// Tar bort den aktuella registreringen utan att stänga
    /// fönstret.
    ///
    /// Används främst av UI-fönstrets egen Close-metod.
    /// </summary>
    public void ClearInteractionWindow(
        IUIWindow window)
    {
        if (window == null)
            return;

        if (!ReferenceEquals(
                currentInteractionWindow,
                window))
        {
            return;
        }

        ClearInteractionRegistration();
    }

    /// <summary>
    /// Tar bort den aktuella interaction-registreringen.
    ///
    /// Den parameterlösa varianten kan användas av äldre kod,
    /// men varianten som tar emot ett fönster är säkrare.
    /// </summary>
    public void ClearInteractionWindow()
    {
        ClearInteractionRegistration();
    }

    public void CloseCurrentInteractionWindow()
    {
        IUIWindow window =
            currentInteractionWindow;

        ClearInteractionRegistration();

        if (window != null &&
            window.IsOpen)
        {
            window.Close();
        }
    }

    private void UpdateInteractionWindow()
    {
        if (currentInteractionWindow == null)
            return;

        if (!currentInteractionWindow.IsOpen)
        {
            ClearInteractionRegistration();
            return;
        }

        if (currentInteractionSource == null)
        {
            CloseCurrentInteractionWindow();
            return;
        }

        ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        float allowedDistance =
            Mathf.Max(
                0f,
                currentInteractionCloseDistance);

        Vector2 playerPosition =
            playerTransform.position;

        Vector2 sourcePosition =
            currentInteractionSource.position;

        float sqrDistance =
            (
                playerPosition -
                sourcePosition
            ).sqrMagnitude;

        float sqrAllowedDistance =
            allowedDistance *
            allowedDistance;

        if (sqrDistance >
            sqrAllowedDistance)
        {
            CloseCurrentInteractionWindow();
        }
    }

    private void ClearInteractionRegistration()
    {
        currentInteractionWindow = null;
        currentInteractionSource = null;
        currentInteractionCloseDistance = 0f;
    }

    // =========================================================
    // TOGGLES
    // =========================================================

    public void ToggleSpellbook()
    {
        if (spellbookUI != null)
        {
            spellbookUI.Toggle();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.Toggle();
        }
    }

    public void TogglePlayerWindow()
    {
        if (playerWindow != null)
        {
            playerWindow.Toggle();
        }
    }

    public void ToggleTalentWindow()
    {
        if (talentWindow != null)
        {
            talentWindow.Toggle();
        }
    }

    public void ToggleReputationWindow()
    {
        if (reputationWindow == null)
            return;

        reputationWindow.gameObject.SetActive(
            !reputationWindow.gameObject.activeSelf);
    }

    public void ToggleDeedsWindow()
    {
        deedsWindow?.Toggle();
    }

    // =========================================================
    // CLOSE
    // =========================================================

    /// <summary>
    /// Stänger samtliga kända UI-fönster.
    ///
    /// Escape stänger alltså allt på samma sätt som tidigare,
    /// inklusive det registrerade interaction-fönstret.
    /// </summary>
    public void CloseAll()
    {
        CloseCurrentInteractionWindow();

        if (talentWindow != null &&
            talentWindow.gameObject.activeSelf)
        {
            talentWindow.Close();
        }

        if (spellbookUI != null &&
            spellbookUI.gameObject.activeSelf)
        {
            spellbookUI.Close();
        }

        if (vendorUI != null &&
            vendorUI.IsOpen)
        {
            vendorUI.Close();
        }

        if (inventoryUI != null &&
            inventoryUI.IsOpen())
        {
            inventoryUI.Close();
        }

        if (lootUI != null &&
            lootUI.gameObject.activeSelf)
        {
            lootUI.Close();
        }

        if (playerWindow != null &&
            playerWindow.IsOpen())
        {
            playerWindow.Close();
        }

        if (reputationWindow != null &&
            reputationWindow.gameObject.activeSelf)
        {
            reputationWindow.gameObject.SetActive(
                false);
        }

        if (donationUI != null &&
            donationUI.IsOpen)
        {
            donationUI.Close();
        }

        if (favourWindow != null &&
            favourWindow.IsOpen)
        {
            favourWindow.Close();
        }

        if (deedsWindow != null &&
            deedsWindow.IsOpen)
        {
            deedsWindow.Close();
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void ResolvePlayerTransform()
    {
        if (playerTransform != null)
            return;

        PlayerStats player =
            PlayerReference.Player;

        if (player != null)
        {
            playerTransform =
                player.transform;
        }
    }

    /// <summary>
    /// Fallback som endast körs vid initialisering.
    ///
    /// Normal användning ska vara serialiserade Inspector-
    /// referenser. Fallbacken gör migreringen säkrare och
    /// förhindrar att hela UI-systemet slutar fungera om ett
    /// enskilt fält missas i Inspectorn.
    /// </summary>
    private void ResolveMissingReferences()
    {
        if (spellbookUI == null)
        {
            spellbookUI =
                FindFirstObjectByType<SpellbookUI>(
                    FindObjectsInactive.Include);
        }

        if (inventoryUI == null)
        {
            inventoryUI =
                FindFirstObjectByType<InventoryUI>(
                    FindObjectsInactive.Include);
        }

        if (playerWindow == null)
        {
            playerWindow =
                FindFirstObjectByType<PlayerWindowController>(
                    FindObjectsInactive.Include);
        }

        if (talentWindow == null)
        {
            talentWindow =
                FindFirstObjectByType<TalentWindowUI>(
                    FindObjectsInactive.Include);
        }

        if (reputationWindow == null)
        {
            reputationWindow =
                FindFirstObjectByType<ReputationWindowUI>(
                    FindObjectsInactive.Include);
        }

        if (lootUI == null)
        {
            lootUI =
                FindFirstObjectByType<LootUI>(
                    FindObjectsInactive.Include);
        }

        if (vendorUI == null)
        {
            vendorUI =
                FindFirstObjectByType<VendorUI>(
                    FindObjectsInactive.Include);
        }

        if (donationUI == null)
        {
            donationUI =
                FindFirstObjectByType<DonationUI>(
                    FindObjectsInactive.Include);
        }

        if (favourWindow == null)
        {
            favourWindow =
                FindFirstObjectByType<FavourWindow>(
                    FindObjectsInactive.Include);
        }

        if (deedsWindow == null)
        {
            deedsWindow =
                FindFirstObjectByType<DeedsWindowUI>(
                    FindObjectsInactive.Include
                );
        }
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current
                .currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<
                   TMP_InputField>() != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        /*
         * Vi gör inte runtime-sökningar här.
         *
         * Referenserna bör dras in manuellt i Inspectorn.
         * ResolveMissingReferences finns endast som en
         * migrationssäker fallback i Awake.
         */
    }
#endif
}