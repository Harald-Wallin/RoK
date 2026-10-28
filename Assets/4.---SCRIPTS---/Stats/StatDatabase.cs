using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stat Database")]
public class StatDatabase : ScriptableObject
{
    private const string PreferredResourcePath = "Stats/StatDatabase";
    private const string LegacyResourcePath = "StatDatabase";

    [SerializeField]
    private List<StatDefinition> stats = new();

    private static StatDatabase instance;

    public IReadOnlyList<StatDefinition> Stats => stats;

    public static StatDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance =
                    Resources.Load<StatDatabase>(PreferredResourcePath);

                if (instance == null)
                {
                    instance =
                        Resources.Load<StatDatabase>(LegacyResourcePath);
                }

                if (instance == null)
                {
                    Debug.LogError(
                        "StatDatabase kunde inte hittas. " +
                        "Lägg StatDatabase.asset i Resources/Stats " +
                        "eller direkt i Resources."
                    );
                }
            }

            return instance;
        }
    }

    private void OnEnable()
    {
        instance = this;
    }

    public StatDefinition GetDefinition(StatType type)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            StatDefinition definition = stats[i];

            if (definition != null &&
                definition.stat == type)
            {
                return definition;
            }
        }

        return null;
    }

    public bool TryGetDefinition(
        StatType type,
        out StatDefinition definition)
    {
        definition = GetDefinition(type);
        return definition != null;
    }

    public bool IsKind(
        StatType type,
        StatKind kind)
    {
        StatDefinition definition =
            GetDefinition(type);

        return definition != null &&
               definition.kind == kind;
    }

    public float GetDefaultValue(StatType type)
    {
        StatDefinition definition =
            GetDefinition(type);

        return definition != null
            ? definition.defaultValue
            : 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<StatType> encountered = new();

        for (int i = stats.Count - 1; i >= 0; i--)
        {
            StatDefinition definition = stats[i];

            if (definition == null)
                continue;

            if (!encountered.Add(definition.stat))
            {
                Debug.LogWarning(
                    $"StatDatabase innehåller flera definitioner för " +
                    $"{definition.stat}. Den första används.",
                    this
                );
            }
        }
    }
#endif
}
