using UnityEngine;

[CreateAssetMenu(menuName = "RöK/Deeds/Category")]
public sealed class DeedCategoryDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private int sortOrder;
    [SerializeField] private DeedCategoryDefinition parentCategory;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public int SortOrder => sortOrder;
    public DeedCategoryDefinition ParentCategory => parentCategory;
}
