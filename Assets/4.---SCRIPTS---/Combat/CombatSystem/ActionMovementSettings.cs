using System;
using UnityEngine;

[Serializable]
public sealed class ActionMovementSettings
{
    [SerializeField]
    [Tooltip(
        "Bestämmer hur karaktären får röra sig under fasen."
    )]
    private ActionMovementMode mode =
        ActionMovementMode.Unrestricted;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Multiplicerar karaktärens MovementSpeed under fasen. " +
        "0.5 innebär 50 procent av normal rörelsehastighet."
    )]
    private float speedMultiplier =
        0.5f;

    public ActionMovementMode Mode =>
        mode;

    public float SpeedMultiplier
    {
        get
        {
            switch (mode)
            {
                case ActionMovementMode.Locked:
                    return 0f;

                case ActionMovementMode.SpeedMultiplier:
                    return Mathf.Clamp01(
                        speedMultiplier
                    );

                case ActionMovementMode.Unrestricted:
                default:
                    return 1f;
            }
        }
    }

    public bool BlocksMovement =>
        mode ==
        ActionMovementMode.Locked;

#if UNITY_EDITOR
    public void Validate()
    {
        speedMultiplier =
            Mathf.Clamp01(
                speedMultiplier
            );
    }
#endif
}
