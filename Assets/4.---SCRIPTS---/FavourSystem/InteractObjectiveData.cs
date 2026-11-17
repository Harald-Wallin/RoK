using System.Collections.Generic;
using UnityEngine;

public enum InteractionCountMode
{
    UniqueEntityTypes,
    ObjectInstances
}

[CreateAssetMenu(menuName = "RPG/Favours/Objectives/Interact Objective")]
public sealed class InteractObjectiveData : FavourObjectiveData
{
    [SerializeField] private List<EntityReference> targets = new();
    [SerializeField] private InteractionCountMode countMode;
    [SerializeField, Min(1)] private int requiredCount = 1;
    [SerializeField] private InteractionTimingSettings timing = new();

    public IReadOnlyList<EntityReference> Targets => targets;
    public InteractionCountMode CountMode => countMode;
    public InteractionTimingSettings Timing => timing;

    public int RequiredInteractions
    {
        get
        {
            if (countMode == InteractionCountMode.ObjectInstances)
                return Mathf.Max(1, requiredCount);

            HashSet<string> ids = new(System.StringComparer.Ordinal);

            foreach (EntityReference target in targets)
            {
                if (target != null && target.IsValid)
                    ids.Add(target.Id);
            }

            return ids.Count;
        }
    }

    public bool ContainsTarget(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
            return false;

        foreach (EntityReference target in targets)
        {
            if (target != null &&
                target.IsValid &&
                EntityTargetUtility.Matches(entityId, target.Id))
            {
                return true;
            }
        }

        return false;
    }

    public override FavourObjectiveRuntime CreateRuntime(FavourRuntime favour)
    {
        return new InteractObjectiveRuntime(this, favour);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targets ??= new List<EntityReference>();
        requiredCount = Mathf.Max(1, requiredCount);

        HashSet<string> ids = new(System.StringComparer.Ordinal);

        foreach (EntityReference target in targets)
        {
            if (target == null || !target.IsValid)
            {
                Debug.LogWarning($"InteractObjective '{name}' has an empty target.", this);
                continue;
            }

            if (!ids.Add(target.Id))
                Debug.LogWarning($"InteractObjective '{name}' repeats '{target.Id}'.", this);
        }
    }
#endif
}