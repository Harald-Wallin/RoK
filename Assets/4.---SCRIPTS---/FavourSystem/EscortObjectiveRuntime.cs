using System;
using UnityEngine;

public enum EscortObjectiveState
{
    WaitingToStart,
    Escorting,
    ReachedDestination
}

public sealed class EscortObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly EscortObjectiveData
        escortData;

    private EscortObjectiveState state =
        EscortObjectiveState.WaitingToStart;


    public EscortObjectiveRuntime(
        EscortObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        escortData = data;
    }


    // =========================================================
    // STATE
    // =========================================================

    public EscortObjectiveState State =>
        state;

    public bool IsWaitingToStart =>
        IsActive &&
        state ==
        EscortObjectiveState.WaitingToStart;

    public bool IsEscorting =>
        IsActive &&
        state ==
        EscortObjectiveState.Escorting;

    public bool HasReachedDestination =>
        state ==
        EscortObjectiveState.ReachedDestination;


    // =========================================================
    // DATA
    // =========================================================

    public string EscortNpcId =>
        escortData != null
            ? escortData.EscortNpcId
            : string.Empty;

    public string EscortNpcDisplayName =>
        escortData != null
            ? escortData.EscortNpcDisplayName
            : string.Empty;

    public string DestinationId =>
        escortData != null
            ? escortData.DestinationId
            : string.Empty;

    public string DestinationDisplayName =>
        escortData != null
            ? escortData.DestinationDisplayName
            : string.Empty;

    public float MaximumPlayerDistance =>
        escortData != null
            ? escortData.MaximumPlayerDistance
            : 0f;

    public float MovementSpeedMultiplier =>
        escortData != null
            ? escortData.MovementSpeedMultiplier
            : 1f;

    public float StartDelay =>
        escortData != null
            ? escortData.StartDelay
            : 0f;

    public float PostSuccessStaySeconds =>
    escortData != null
        ? escortData.PostSuccessStaySeconds
        : 0f;


    // =========================================================
    // PROGRESS
    // =========================================================

    public override bool IsComplete =>
        state ==
        EscortObjectiveState.ReachedDestination;

    public override int CurrentProgress =>
        IsComplete
            ? 1
            : 0;

    public override int RequiredProgress =>
        1;

    public override string ProgressText
    {
        get
        {
            switch (state)
            {
                case EscortObjectiveState.WaitingToStart:
                    return "Waiting to start";

                case EscortObjectiveState.Escorting:
                    return "In progress";

                case EscortObjectiveState.ReachedDestination:
                    return "Complete";

                default:
                    return string.Empty;
            }
        }
    }


    // =========================================================
    // TARGETING
    // =========================================================

    public bool RequiresEscortNpc(
        string entityId)
    {
        if (string.IsNullOrWhiteSpace(
                entityId) ||
            string.IsNullOrWhiteSpace(
                EscortNpcId))
        {
            return false;
        }

        return string.Equals(
            entityId,
            EscortNpcId,
            StringComparison.Ordinal
        );
    }


    public bool CanStartAt(
        string entityId)
    {
        return
            IsWaitingToStart &&
            RequiresEscortNpc(
                entityId
            );
    }


    // =========================================================
    // START ESCORT
    // =========================================================

    public bool TryStartEscort(
        string entityId)
    {
        if (!CanStartAt(
                entityId))
        {
            return false;
        }

        /*
         * ---------------------------------------------------------
         * RESOLVE ESCORT NPC
         * ---------------------------------------------------------
         */

        EntityIdentity escortIdentity =
            FindActiveEntity(
                EscortNpcId
            );

        if (escortIdentity == null)
        {
            Debug.LogWarning(
                $"EscortObjective '{DisplayName}' kunde inte starta: " +
                $"Escort NPC '{EscortNpcDisplayName}' " +
                $"({EscortNpcId}) hittades inte i den aktiva världen."
            );

            return false;
        }

        NPCBehavior escortBehavior =
            escortIdentity.GetComponentInParent<
                NPCBehavior>();

        if (escortBehavior == null)
        {
            escortBehavior =
                escortIdentity.GetComponentInChildren<
                    NPCBehavior>();
        }

        if (escortBehavior == null)
        {
            Debug.LogWarning(
                $"EscortObjective '{DisplayName}' kunde inte starta: " +
                $"'{escortIdentity.name}' saknar NPCBehavior.",
                escortIdentity
            );

            return false;
        }

        /*
         * ---------------------------------------------------------
         * RESOLVE DESTINATION
         * ---------------------------------------------------------
         */

        EntityIdentity destinationIdentity =
            FindActiveEntity(
                DestinationId
            );

        if (destinationIdentity == null)
        {
            Debug.LogWarning(
                $"EscortObjective '{DisplayName}' kunde inte starta: " +
                $"Destination '{DestinationDisplayName}' " +
                $"({DestinationId}) hittades inte i den aktiva världen."
            );

            return false;
        }

        /*
         * NPCBehavior får först godkänna sessionen.
         *
         * Vi byter INTE runtime-state till Escorting förrän vi
         * vet att NPC:n faktiskt kan ta emot escort-kommandot.
         */
        bool npcAcceptedEscort =
            escortBehavior.TryBeginEscort(
                this,
                destinationIdentity.transform
            );

        if (!npcAcceptedEscort)
        {
            return false;
        }

        state =
            EscortObjectiveState.Escorting;

        RaiseProgressChanged();

        return true;
    }


    // =========================================================
    // COMPLETION
    // =========================================================

    public void MarkDestinationReached()
    {
        if (!IsActive ||
            state !=
            EscortObjectiveState.Escorting)
        {
            return;
        }

        state =
            EscortObjectiveState.ReachedDestination;

        RaiseProgressChanged();
    }


    // =========================================================
    // PRESENTATION TARGETING
    // =========================================================

    public override bool IsRelevantToEntity(
        string entityId)
    {
        if (string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        if (IsWaitingToStart)
        {
            return RequiresEscortNpc(
                entityId
            );
        }

        if (IsEscorting)
        {
            return string.Equals(
                entityId,
                DestinationId,
                StringComparison.Ordinal
            );
        }

        return false;
    }


    // =========================================================
    // RESET
    // =========================================================

    public override void ResetProgress()
    {
        state =
            EscortObjectiveState.WaitingToStart;

        RaiseProgressChanged();
    }


    // =========================================================
    // WORLD ENTITY RESOLUTION
    // =========================================================

    private static EntityIdentity FindActiveEntity(
        string entityId)
    {
        if (string.IsNullOrWhiteSpace(
                entityId))
        {
            return null;
        }

        EntityIdentity[] identities =
            UnityEngine.Object
                .FindObjectsByType<
                    EntityIdentity>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

        foreach (EntityIdentity identity
                 in identities)
        {
            if (identity == null)
                continue;

            if (EntityTargetUtility.Matches(
                    identity,
                    entityId))
            {
                return identity;
            }
        }

        return null;
    }
}