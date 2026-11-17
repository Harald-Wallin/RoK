using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterChannelController))]
[RequireComponent(typeof(CharacterActionController))]
public sealed class PlayerTimedInteraction : MonoBehaviour
{
    private const float MovementThreshold = 0.02f;

    private static readonly Dictionary<GameObject, PlayerTimedInteraction>
        reservations = new();

    private CharacterChannelController channels;
    private CharacterActionController actions;
    private Rigidbody2D movementBody;

    private InteractionContext context;
    private IInteractionOption option;
    private Action complete;
    private GameObject reservedTarget;
    private Vector2 startPosition;
    private bool pending;
    private bool completing;

    private void Awake()
    {
        channels = GetComponent<CharacterChannelController>();
        actions = GetComponent<CharacterActionController>();
        movementBody = GetComponent<Rigidbody2D>();
    }

    public bool TryExecute(
        InteractionContext request,
        IInteractionOption requestedOption,
        InteractionTimingQuery timing,
        Action onComplete)
    {
        if (completing)
            return false;

        Cancel();

        context = request;
        option = requestedOption;

        if (!IsValid() || channels.IsChanneling)
            return false;

        GameObject owner = request.Target.InteractionOwner;

        if (reservations.TryGetValue(owner, out PlayerTimedInteraction other) &&
            other != null && other.pending)
        {
            return false;
        }

        reservedTarget = owner;
        reservations[owner] = this;
        complete = onComplete;
        pending = true;
        startPosition = GetPosition();

        if (timing.Duration <= 0f)
        {
            Finish();
            return true;
        }

        channels.ChannelCompleted += HandleCompleted;
        channels.ChannelCancelled += HandleCancelled;
        actions.OnActionStarted += HandleActionStarted;

        SpriteRenderer renderer = owner.GetComponentInChildren<SpriteRenderer>();
        Sprite icon = renderer != null ? renderer.sprite : null;

        ChannelRequest channel = new(
            this,
            timing.Label,
            icon,
            timing.Duration,
            isReversed: timing.IsReversed);

        if (channels.TryStartChannel(channel))
            return true;

        ClearPending();
        return false;
    }

    private void Update()
    {
        if (pending && !completing && (!IsValid() || HasMoved()))
            Cancel();
    }

    private bool IsValid()
    {
        if (!context.IsValid ||
            !context.Player.isActiveAndEnabled ||
            !context.Player.IsAlive ||
            !context.Target.isActiveAndEnabled ||
            !context.Target.InteractionOwner.activeInHierarchy ||
            !context.Target.IsWithinInteractionDistance(context.Player.transform) ||
            actions.HasActiveAction)
        {
            return false;
        }

        if (option is Behaviour behaviour && !behaviour.isActiveAndEnabled)
            return false;

        return option != null && option.CanInteract(context);
    }

    private bool HasMoved()
    {
        return (GetPosition() - startPosition).sqrMagnitude >
               MovementThreshold * MovementThreshold;
    }

    private Vector2 GetPosition()
    {
        return movementBody != null
            ? movementBody.position
            : (Vector2)transform.position;
    }

    private void HandleCompleted(ChannelRuntime channel)
    {
        if (!ReferenceEquals(channel.Owner, this) || !pending)
            return;

        if (!IsValid() || HasMoved())
        {
            ClearPending();
            return;
        }

        Finish();
    }

    private void HandleCancelled(ChannelRuntime channel)
    {
        if (ReferenceEquals(channel.Owner, this))
            ClearPending();
    }

    private void HandleActionStarted(ActionContext action)
    {
        Cancel();
    }

    private void Finish()
    {
        completing = true;
        Unsubscribe();

        try
        {
            complete?.Invoke();
        }
        finally
        {
            ClearPending();
            completing = false;
        }
    }

    private void Cancel()
    {
        if (!pending)
            return;

        channels.CancelChannel(this);
        ClearPending();
    }

    private void ClearPending()
    {
        Unsubscribe();

        if (!ReferenceEquals(reservedTarget, null) &&
            reservations.TryGetValue(reservedTarget, out PlayerTimedInteraction owner) &&
            owner == this)
        {
            reservations.Remove(reservedTarget);
        }

        reservedTarget = null;
        complete = null;
        option = null;
        context = default;
        pending = false;
    }

    private void Unsubscribe()
    {
        if (channels != null)
        {
            channels.ChannelCompleted -= HandleCompleted;
            channels.ChannelCancelled -= HandleCancelled;
        }

        if (actions != null)
            actions.OnActionStarted -= HandleActionStarted;
    }

    private void OnDisable()
    {
        Cancel();
    }
}
