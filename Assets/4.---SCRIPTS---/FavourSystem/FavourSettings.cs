using System;
using UnityEngine;

[Serializable]
public sealed class FavourFailureSettings
{
    [SerializeField]
    private FavourFailurePolicy policy =
        FavourFailurePolicy.None;

    [SerializeField]
    [Min(0f)]
    private float retryCooldownSeconds;

    public FavourFailurePolicy Policy =>
        policy;

    public float RetryCooldownSeconds =>
        Mathf.Max(
            0f,
            retryCooldownSeconds
        );
}

[Serializable]
public sealed class FavourRepeatSettings
{
    [SerializeField]
    private bool repeatable;

    [SerializeField]
    [Min(0f)]
    private float repeatCooldownSeconds;

    public bool Repeatable =>
        repeatable;

    public float RepeatCooldownSeconds =>
        Mathf.Max(
            0f,
            repeatCooldownSeconds
        );
}
