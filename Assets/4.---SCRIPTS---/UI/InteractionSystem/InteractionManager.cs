using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central ingångspunkt för spelarens interaktioner med världen.
///
/// Managern:
/// - läser högerklick,
/// - hittar ett InteractionTarget,
/// - kontrollerar avstånd,
/// - filtrerar tillgängliga interaktioner,
/// - kör ett ensamt alternativ direkt,
/// - förbereder flera alternativ för en valmeny.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionManager : MonoBehaviour
{
    private readonly List<IInteractionOption>
        availableOptions = new();

    [Header("Input")]

    [Tooltip(
        "Musknapp eller tangent som startar world interaction.")]
    [SerializeField]
    private KeyCode interactionInput =
        KeyCode.Mouse1;

    [Header("Raycast")]

    [Tooltip(
        "Layer som innehåller InteractionTarget-colliders.")]
    [SerializeField]
    private LayerMask interactionLayerMask;

    [Header("References")]

    [SerializeField]
    private Camera worldCamera;

    private PlayerStats player;

    public static InteractionManager Instance
    {
        get;
        private set;
    }

    public InteractionTarget CurrentTarget
    {
        get;
        private set;
    }

    public IReadOnlyList<IInteractionOption>
        CurrentOptions => availableOptions;

    public bool HasCurrentInteraction =>
        CurrentTarget != null &&
        availableOptions.Count > 0;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Flera InteractionManager finns i scenen. " +
                "Den nya komponenten tas bort.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveWorldCamera();
    }

    private void Start()
    {
        ResolvePlayer();

        if (player == null)
        {
            Debug.LogError(
                "InteractionManager kunde inte hitta spelaren " +
                "via PlayerReference.Player.",
                this);
        }

        if (worldCamera == null)
        {
            Debug.LogError(
                "InteractionManager saknar en world camera.",
                this);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(interactionInput))
            return;

        if (IsTypingInInputField())
            return;

        if (IsPointerOverUI())
            return;

        TryInteractAtPointer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Försöker hitta och interagera med ett target
    /// under muspekaren.
    /// </summary>
    public bool TryInteractAtPointer()
    {
        if (!EnsureReferences())
            return false;

        Vector3 mouseWorldPosition =
            worldCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector2 interactionPoint =
            new(
                mouseWorldPosition.x,
                mouseWorldPosition.y
            );

        Collider2D[] hits =
            Physics2D.OverlapPointAll(
                interactionPoint,
                interactionLayerMask
            );

        if (hits == null ||
            hits.Length == 0)
        {
            ClearCurrentInteraction();
            return false;
        }

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
                continue;

            InteractionTarget target =
                FindInteractionTarget(
                    hit
                );

            /*
             * Hitbox-layern får även innehålla andra typer
             * av hitboxes, exempelvis CombatHitbox.
             *
             * Endast hitboxes som faktiskt leder till ett
             * InteractionTarget behandlas som interaktioner.
             */
            if (target == null)
                continue;

            if (TryInteract(
                    target))
            {
                return true;
            }
        }

        ClearCurrentInteraction();

        return false;
    }

    /// <summary>
    /// Försöker interagera med ett specifikt target.
    /// Kan senare även användas av exempelvis tangentbordsinteraktion
    /// eller gamepad-fokusering.
    /// </summary>
    public bool TryInteract(
        InteractionTarget target)
    {
        if (target == null)
            return false;

        if (!EnsureReferences())
            return false;

        if (!target.IsWithinInteractionDistance(
                player.transform))
        {
            ClearCurrentInteraction();
            return false;
        }

        InteractionContext context =
    new(
        player,
        target);

        target.GetInteractionOptions(
            context,
            availableOptions);

        RemoveUnavailableOptions(
            context);

        if (availableOptions.Count == 0)
        {
            ClearCurrentInteraction();
            return false;
        }

        CurrentTarget = target;

        if (availableOptions.Count == 1)
        {
            return ExecuteOption(
                availableOptions[0],
                context);
        }

        OpenSelectionWindow(context);
        return true;
    }

    /// <summary>
    /// Exekverar ett specifikt interaktionsalternativ.
    ///
    /// Metoden används senare av InteractionSelectionWindow
    /// när spelaren har valt ett alternativ.
    /// </summary>
    public bool ExecuteOption(
        IInteractionOption option)
    {
        if (option == null ||
            CurrentTarget == null)
        {
            return false;
        }

        if (!EnsureReferences())
            return false;

        InteractionContext context =
            new(
                player,
                CurrentTarget);

        return ExecuteOption(
            option,
            context);
    }

    public void ClearCurrentInteraction()
    {
        CurrentTarget = null;
        availableOptions.Clear();
    }

    private bool ExecuteOption(
        IInteractionOption option,
        in InteractionContext context)
    {
        if (option == null ||
            !context.IsValid)
        {
            return false;
        }

        if (!context.Target
                .IsWithinInteractionDistance(
                    context.Player.transform))
        {
            ClearCurrentInteraction();
            return false;
        }

        if (!option.CanInteract(context))
        {
            ClearCurrentInteraction();
            return false;
        }

        InteractionEvents
    .RaiseInteractionCommitted(
        context
    );

        option.Interact(
            context
        );

        InteractionEvents
            .RaiseInteractionCompleted(
                context
            );

        return true;
    }

    private void RemoveUnavailableOptions(
        in InteractionContext context)
    {
        for (int i =
                 availableOptions.Count - 1;
             i >= 0;
             i--)
        {
            IInteractionOption option =
                availableOptions[i];

            if (option == null ||
                !option.CanInteract(context))
            {
                availableOptions.RemoveAt(i);
            }
        }
    }

    private void OpenSelectionWindow(
    in InteractionContext context)
    {
        if (!context.IsValid ||
            availableOptions.Count == 0)
        {
            return;
        }

        InteractionSelectionWindow window =
            InteractionSelectionWindow.Instance;

        if (window == null)
        {
            Debug.LogWarning(
                $"'{context.Target.InteractionOwner.name}' har " +
                $"{availableOptions.Count} tillgängliga interaktioner, " +
                "men inget InteractionSelectionWindow finns i scenen.",
                context.Target
            );

            return;
        }

        window.Open(
            context.Target,
            availableOptions
        );
    }

    private static InteractionTarget
        FindInteractionTarget(
            Collider2D hit)
    {
        if (hit == null)
            return null;

        InteractionTarget target =
            hit.GetComponent<InteractionTarget>();

        if (target != null)
            return target;

        return hit.GetComponentInParent<
            InteractionTarget>();
    }

    private bool EnsureReferences()
    {
        if (player == null)
        {
            ResolvePlayer();
        }

        if (worldCamera == null)
        {
            ResolveWorldCamera();
        }

        return player != null &&
               worldCamera != null;
    }

    private void ResolvePlayer()
    {
        player = PlayerReference.Player;
    }

    private void ResolveWorldCamera()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current
                   .IsPointerOverGameObject();
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
        if (interactionLayerMask.value == 0)
        {
            interactionLayerMask =
                LayerMask.GetMask(
                    "Transparent Interactable, hitbox"
                );
        }
    }
#endif
}