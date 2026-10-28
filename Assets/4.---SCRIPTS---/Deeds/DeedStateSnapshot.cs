using System;
using System.Collections.Generic;

/// <summary>
/// Serializable data for a future save system.
/// V1 provides Capture/Restore APIs but DOES NOT persist between launches.
/// </summary>
[Serializable]
public sealed class DeedStateSnapshot
{
    public string deedId;
    public int progress;
    public bool discovered;
    public bool tracked;
    public List<string> completedRankIds = new();
}
