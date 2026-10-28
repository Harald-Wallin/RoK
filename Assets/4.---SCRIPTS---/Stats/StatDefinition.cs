using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Stats/Stat Definition"
)]
public class StatDefinition :
    ScriptableObject
{
    [Header("Identity")]

    public StatType stat;

    public string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    [Tooltip(
        "Kort beskrivning som visas när spelaren hovrar " +
        "över stat-raden."
    )]
    private string description;

    [Header("Display")]

    public StatDisplayFormat displayFormat =
        StatDisplayFormat.Number;

    [SerializeField]
    private List<StatCategory> categories =
        new();

    [SerializeField]
    [Tooltip(
        "Om denna stat ska kunna visas i PlayerWindow."
    )]
    private bool showInPlayerWindow =
        true;

    [SerializeField]
    [Tooltip(
        "Lägre värde visas tidigare i listan."
    )]
    private int displayOrder;

    [SerializeField]
    [Min(0.0001f)]
    [Tooltip(
        "Värdet som räknas som 100% när statens display-format " +
        "är Percentage. Lämna på 1 för vanliga procentvärden. " +
        "MovementSpeed kan exempelvis använda 2.5."
    )]
    private float displayReferenceValue =
        1f;

    [Header("Behavior")]

    public StatKind kind;

    [Tooltip(
        "Default value used when the stat is first added. " +
        "For derived stats, this is also the value before " +
        "scaling is applied."
    )]
    public float defaultValue;

    [Header("Inspector")]

    public bool editable =
        true;

    public bool visible =
        true;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName)
            ? stat.ToString()
            : displayName;

    public string Description =>
        description;

    public IReadOnlyList<StatCategory>
        Categories =>
            categories;

    public bool ShowInPlayerWindow =>
        showInPlayerWindow;

    public int DisplayOrder =>
        displayOrder;

    public float DisplayReferenceValue =>
        Mathf.Max(
            0.0001f,
            displayReferenceValue
        );

    public bool HasCategory(
        StatCategory category)
    {
        return categories != null &&
               categories.Contains(
                   category);
    }

    /// <summary>
    /// Konverterar statens interna runtime-värde till det värde
    /// som ska skickas vidare till UI-formateringen.
    ///
    /// Exempel:
    /// MovementSpeed runtime = 2.5
    /// DisplayReferenceValue = 2.5
    /// Resultat = 1.0, vilket Percentage-format visar som 100%.
    /// </summary>
    public float GetDisplayValue(
        float runtimeValue)
    {
        return runtimeValue /
               DisplayReferenceValue;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        categories ??=
            new List<StatCategory>();

        displayReferenceValue =
            Mathf.Max(
                0.0001f,
                displayReferenceValue
            );
    }
#endif
}

public enum StatKind
{
    Primary,
    Derived
}