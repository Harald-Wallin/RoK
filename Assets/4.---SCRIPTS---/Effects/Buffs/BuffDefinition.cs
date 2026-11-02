
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(
    menuName = "RöK/Buffs/Buff Definition"
)]
public sealed class BuffDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string displayName;

    [SerializeField, TextArea(2, 4)]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Lifetime")]
    [SerializeField, Min(0.01f)]
    private float duration = 30f;

    [SerializeField]
    private bool removeOnDeath = true;

    [SerializeField]
    private bool removeOnEncounterReset = true;

    [Header("Stacking")]
    [SerializeField, Min(1)]
    private int maxStacks = 1;

    [SerializeField]
    private bool refreshDurationOnReapply = true;

    [Header("Effects")]
    [SerializeField]
    private EffectDefinition[] effects =
        new EffectDefinition[0];

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

    public Sprite Icon => icon;

    public float Duration =>
        Mathf.Max(0.01f, duration);

    public int MaxStacks =>
        Mathf.Max(1, maxStacks);

    public bool RefreshDurationOnReapply =>
        refreshDurationOnReapply;

    public bool RemoveOnDeath =>
        removeOnDeath;

    public bool RemoveOnEncounterReset =>
        removeOnEncounterReset;

    public IReadOnlyList<EffectDefinition> Effects =>
        effects;

    // =====================================================
    // PRESENTATION
    // =====================================================

    public string GetDescription()
    {
        StringBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.Append(description);
        }

        if (effects != null)
        {
            foreach (EffectDefinition effect in effects)
            {
                if (effect == null)
                    continue;

                string effectDescription =
                    effect.GetDescription();

                if (string.IsNullOrWhiteSpace(
                        effectDescription))
                {
                    continue;
                }

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(effectDescription);
            }
        }

        return builder.ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        duration = Mathf.Max(
            0.01f,
            duration
        );

        maxStacks = Mathf.Max(
            1,
            maxStacks
        );
    }
#endif
}
