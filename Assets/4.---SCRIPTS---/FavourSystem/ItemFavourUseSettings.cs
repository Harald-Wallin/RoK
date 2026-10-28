using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ItemFavourUseSettings
{
    [SerializeField]
    [Tooltip(
        "Om detta item kan användas som en källa till Favours.\n\n" +
        "Detta är helt separat från ItemType. " +
        "En Ring, Weapon, Food eller FavourItem kan alltså samtidigt " +
        "ha Favour-interaktion."
    )]
    private bool enabled;

    [SerializeField]
    [Tooltip(
        "Favours som detta item kan presentera.\n\n" +
        "Ordningen fungerar som authored progression-prioritet. " +
        "Requirements på respektive FavourData avgör fortfarande " +
        "när varje favour faktiskt blir tillgänglig."
    )]
    private List<FavourData> favours =
        new();

    public bool Enabled =>
        enabled;

    public IReadOnlyList<FavourData> Favours =>
        favours;

    public bool HasFavours =>
        enabled &&
        favours != null &&
        favours.Count > 0;
}


#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ItemFavourUseSettings))]
public sealed class ItemFavourUseSettingsDrawer :
    UnityEditor.PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(
        Rect position,
        UnityEditor.SerializedProperty property,
        GUIContent label)
    {
        UnityEditor.SerializedProperty enabled =
            property.FindPropertyRelative(
                "enabled"
            );

        UnityEditor.SerializedProperty favours =
            property.FindPropertyRelative(
                "favours"
            );

        float lineHeight =
            UnityEditor.EditorGUIUtility
                .singleLineHeight;

        Rect enabledRect =
            new Rect(
                position.x,
                position.y,
                position.width,
                lineHeight
            );

        UnityEditor.EditorGUI.PropertyField(
            enabledRect,
            enabled,
            new GUIContent(
                "Enable Favour Interaction"
            )
        );

        if (!enabled.boolValue)
            return;

        Rect favoursRect =
            new Rect(
                position.x,
                position.y +
                lineHeight +
                Spacing,
                position.width,
                UnityEditor.EditorGUI
                    .GetPropertyHeight(
                        favours,
                        true
                    )
            );

        UnityEditor.EditorGUI.PropertyField(
            favoursRect,
            favours,
            new GUIContent("Favours"),
            true
        );
    }

    public override float GetPropertyHeight(
        UnityEditor.SerializedProperty property,
        GUIContent label)
    {
        UnityEditor.SerializedProperty enabled =
            property.FindPropertyRelative(
                "enabled"
            );

        float lineHeight =
            UnityEditor.EditorGUIUtility
                .singleLineHeight;

        if (!enabled.boolValue)
            return lineHeight;

        UnityEditor.SerializedProperty favours =
            property.FindPropertyRelative(
                "favours"
            );

        return
            lineHeight +
            Spacing +
            UnityEditor.EditorGUI
                .GetPropertyHeight(
                    favours,
                    true
                );
    }
}
#endif