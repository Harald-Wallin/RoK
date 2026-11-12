using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Weapon Mastery Tree",
    menuName = "RPG/Weapons/Weapon Mastery Tree")]
public sealed class WeaponMasteryTreeDefinition : ScriptableObject
{
    public enum NodeState
    {
        Locked,
        Available,
        Unlocked,
        OtherPathSelected
    }

    [Serializable]
    public sealed class Node
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int requiredLevel = 1;
        [SerializeField] private string choiceGroup;
        [SerializeField] private string[] requiredNodes = Array.Empty<string>();
        [SerializeField] private string[] requiredChoiceGroups = Array.Empty<string>();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? id
            : displayName;

        public int RequiredLevel => Mathf.Max(1, requiredLevel);
        public string ChoiceGroup => choiceGroup;
        public bool IsChoice => !string.IsNullOrWhiteSpace(choiceGroup);

        internal bool MeetsRequirements(
            int level,
            HashSet<string> unlockedNodes,
            HashSet<string> completedChoiceGroups)
        {
            if (level < RequiredLevel)
                return false;

            foreach (string requiredNode in requiredNodes ?? Array.Empty<string>())
            {
                if (!unlockedNodes.Contains(requiredNode))
                    return false;
            }

            foreach (string requiredGroup in requiredChoiceGroups ?? Array.Empty<string>())
            {
                if (!completedChoiceGroups.Contains(requiredGroup))
                    return false;
            }

            return true;
        }
    }

    [SerializeField] private Node[] nodes = Array.Empty<Node>();

    public IReadOnlyList<Node> Nodes => nodes ?? Array.Empty<Node>();

    public Dictionary<string, NodeState> Evaluate(
        int level,
        HashSet<string> selectedNodes)
    {
        Dictionary<string, Node> nodesById = new(StringComparer.Ordinal);
        HashSet<string> unlockedNodes = new(StringComparer.Ordinal);
        HashSet<string> completedChoiceGroups = new(StringComparer.Ordinal);

        foreach (Node node in Nodes)
        {
            if (node != null && !string.IsNullOrWhiteSpace(node.Id))
                nodesById.TryAdd(node.Id, node);
        }

        // Resolve prerequisites even if the asset lists them after their dependants.
        bool unlockedAnotherNode;

        do
        {
            unlockedAnotherNode = false;

            foreach (Node node in nodesById.Values)
            {
                if (unlockedNodes.Contains(node.Id) ||
                    !node.MeetsRequirements(level, unlockedNodes, completedChoiceGroups))
                {
                    continue;
                }

                if (node.IsChoice &&
                    (selectedNodes == null ||
                     !selectedNodes.Contains(node.Id) ||
                     completedChoiceGroups.Contains(node.ChoiceGroup)))
                {
                    continue;
                }

                unlockedNodes.Add(node.Id);
                unlockedAnotherNode = true;

                if (node.IsChoice)
                    completedChoiceGroups.Add(node.ChoiceGroup);
            }
        }
        while (unlockedAnotherNode);

        Dictionary<string, NodeState> states = new(StringComparer.Ordinal);

        foreach (Node node in nodesById.Values)
        {
            NodeState state = NodeState.Locked;

            if (unlockedNodes.Contains(node.Id))
                state = NodeState.Unlocked;
            else if (node.IsChoice && completedChoiceGroups.Contains(node.ChoiceGroup))
                state = NodeState.OtherPathSelected;
            else if (node.IsChoice &&
                     node.MeetsRequirements(level, unlockedNodes, completedChoiceGroups))
                state = NodeState.Available;

            states.Add(node.Id, state);
        }

        return states;
    }
}
