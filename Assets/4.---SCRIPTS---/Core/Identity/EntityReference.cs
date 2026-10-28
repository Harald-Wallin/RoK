using System;
using UnityEngine;

[Serializable]
public sealed class EntityReference
{
    [SerializeField]
    [HideInInspector]
    private string entityId;

    [SerializeField]
    [HideInInspector]
    private string displayName;

    public string Id =>
        entityId;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName)
            ? entityId
            : displayName;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(
            entityId);
}
