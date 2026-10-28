using UnityEngine;
using System;

/// <summary>
/// Authoring data for one measurable deed objective.
/// A new objective type requires only a new definition/runtime pair.
/// </summary>
public abstract class DeedObjectiveDefinition : ScriptableObject
{
    public abstract DeedObjectiveRuntime CreateRuntime();
}

public abstract class DeedObjectiveRuntime
{
    public int Progress { get; private set; }

    public void RestoreProgress(int progress)
    {
        Progress = Mathf.Max(0, progress);
    }

    protected void AddProgress(int amount)
    {
        if (amount <= 0)
            return;

        Progress = (int)Math.Min(
            (long)int.MaxValue,
            (long)Progress + amount
        );
    }

    public abstract void Receive(DeedProgressEvent progressEvent);
}
