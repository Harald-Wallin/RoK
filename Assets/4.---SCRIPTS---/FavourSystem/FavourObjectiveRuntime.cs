using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class FavourObjectiveRuntime
{
    private bool active;

    protected FavourObjectiveRuntime(
        FavourObjectiveData data,
        FavourRuntime favour)
    {
        Data = data;
        Favour = favour;
    }

    public FavourObjectiveData Data
    {
        get;
    }

    public FavourRuntime Favour
    {
        get;
    }

    public bool IsActive =>
        active;

    public string DisplayName =>
        Data != null
            ? Data.DisplayName
            : "Missing Objective";

    public string Description =>
        Data != null
            ? Data.Description
            : string.Empty;

    public abstract bool IsComplete
    {
        get;
    }

    public abstract int CurrentProgress
    {
        get;
    }

    public abstract int RequiredProgress
    {
        get;
    }

    public int ClampedCurrentProgress =>
        Mathf.Clamp(
            CurrentProgress,
            0,
            Mathf.Max(
                0,
                RequiredProgress
            )
        );

    public float ProgressNormalized
    {
        get
        {
            if (RequiredProgress <= 0)
            {
                return IsComplete
                    ? 1f
                    : 0f;
            }

            return Mathf.Clamp01(
                (float)ClampedCurrentProgress /
                RequiredProgress
            );
        }
    }

    public virtual string ProgressText =>
        $"{ClampedCurrentProgress}/{RequiredProgress}";

    public virtual string DisplayText
    {
        get
        {
            if (RequiredProgress <= 1)
            {
                return DisplayName;
            }

            return
                $"{DisplayName} " +
                $"({ProgressText})";
        }
    }

    public event Action<FavourObjectiveRuntime>
        ProgressChanged;

    internal void Activate()
    {
        if (active)
            return;

        active = true;

        OnActivated();
    }

    internal void Deactivate()
    {
        if (!active)
            return;

        active = false;

        OnDeactivated();
    }

    internal void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (!active ||
            IsComplete ||
            result == null)
        {
            return;
        }

        OnCharacterDefeated(
            result
        );
    }

    internal void CollectTurnInCosts(
        List<FavourItemCost> costs)
    {
        if (costs == null)
            return;

        OnCollectTurnInCosts(
            costs
        );
    }

    protected void RaiseProgressChanged()
    {
        ProgressChanged?.Invoke(
            this
        );
    }

    protected virtual void OnActivated()
    {
    }

    protected virtual void OnDeactivated()
    {
    }

    protected virtual void OnCharacterDefeated(
        CharacterDefeatedResult result)
    {
    }

    protected virtual void OnCollectTurnInCosts(
        List<FavourItemCost> costs)
    {
    }

    // =========================================================
    // PRESENTATION TARGETING
    // =========================================================

    /// <summary>
    /// Returnerar true om detta objective fortfarande är relevant
    /// för en world entity med angivet EntityIdentity-ID.
    ///
    /// Basklassen matchar ingenting. Objective-typer som arbetar
    /// mot world entities overridar detta.
    /// </summary>
    public virtual bool IsRelevantToEntity(
        string entityId)
    {
        return false;
    }

    /// <summary>
    /// Returnerar true om detta objective fortfarande är relevant
    /// för angivet item.
    ///
    /// Basklassen matchar ingenting. Objective-typer som arbetar
    /// mot items overridar detta.
    /// </summary>
    public virtual bool IsRelevantToItem(
        ItemData item)
    {
        return false;
    }

    public abstract void ResetProgress();
}