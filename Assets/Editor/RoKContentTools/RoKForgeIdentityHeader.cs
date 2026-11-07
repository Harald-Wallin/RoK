using UnityEngine;

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>
/// Shared identity header for The Forge.
///
/// Displays and edits the content identity without
/// changing runtime gameplay or asset architecture.
/// </summary>
public static class RoKForgeIdentityHeader
{
    // =====================================================
    // PUBLIC API
    // =====================================================

    public static VisualElement Create(
        ScriptableObject asset,
        Action onIdentityChanged = null)
    {
        if (asset == null)
            return new VisualElement();

        VisualElement root =
            new VisualElement();

        root.style.flexDirection =
            FlexDirection.Row;

        root.style.flexWrap =
            Wrap.Wrap;

        root.style.alignItems =
            Align.Center;

        root.style.paddingLeft = 14;
        root.style.paddingRight = 14;
        root.style.paddingTop = 14;
        root.style.paddingBottom = 14;

        root.style.marginBottom = 12;

        root.style.backgroundColor =
            EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.20f, 0.22f)
                : new Color(0.90f, 0.91f, 0.92f);

        root.style.borderTopLeftRadius = 6;
        root.style.borderTopRightRadius = 6;
        root.style.borderBottomLeftRadius = 6;
        root.style.borderBottomRightRadius = 6;

        // =================================================
        // RESOLVE SERIALIZED IDENTITY
        // =================================================

        string nameProperty =
            GetNameProperty(asset);

        SerializedObject serialized =
            new SerializedObject(asset);

        SerializedProperty name =
            nameProperty != null
                ? serialized.FindProperty(nameProperty)
                : null;

        SerializedProperty icon =
            serialized.FindProperty("icon");

        Sprite currentIcon =
            icon?.objectReferenceValue as Sprite;

        // =================================================
        // ICON PREVIEW
        // =================================================

        VisualElement iconContainer =
            new VisualElement();

        iconContainer.style.width = 76;
        iconContainer.style.height = 76;
        iconContainer.style.flexShrink = 0;

        iconContainer.style.marginRight = 14;

        iconContainer.style.backgroundColor =
            EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.13f, 0.15f)
                : new Color(0.80f, 0.81f, 0.83f);

        iconContainer.style.alignItems =
            Align.Center;

        iconContainer.style.justifyContent =
            Justify.Center;

        Image iconPreview =
            new Image();

        iconPreview.style.width = 66;
        iconPreview.style.height = 66;

        iconPreview.scaleMode =
            ScaleMode.ScaleToFit;

        Label emptyIcon =
            new Label("No Icon");

        emptyIcon.style.fontSize = 11;

        emptyIcon.style.color =
            MutedColor;

        iconContainer.Add(iconPreview);
        iconContainer.Add(emptyIcon);

        void UpdatePreview(Sprite sprite)
        {
            iconPreview.sprite = sprite;

            iconPreview.style.display =
                sprite != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            emptyIcon.style.display =
                sprite == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        UpdatePreview(currentIcon);

        if (asset is ItemData item)
        {
            const float borderWidth = 2f;

            iconContainer.style.borderTopWidth = borderWidth;
            iconContainer.style.borderRightWidth = borderWidth;
            iconContainer.style.borderBottomWidth = borderWidth;
            iconContainer.style.borderLeftWidth = borderWidth;

            void UpdateRarityBorder()
            {
                Color color = ItemRarityColors.GetColor(item.rarity);

                iconContainer.style.borderTopColor = color;
                iconContainer.style.borderRightColor = color;
                iconContainer.style.borderBottomColor = color;
                iconContainer.style.borderLeftColor = color;
            }

            UpdateRarityBorder();

            iconContainer.TrackPropertyValue(
                serialized.FindProperty(nameof(ItemData.rarity)),
                _ => UpdateRarityBorder());
        }

        root.Add(iconContainer);

        // =================================================
        // DETAILS COLUMN
        // =================================================

        VisualElement details =
            new VisualElement();

        details.style.flexGrow = 1;
        details.style.minWidth = 200;

        Label category =
            new Label(asset.GetType().Name);

        category.style.fontSize = 11;

        category.style.color =
            MutedColor;

        category.style.marginBottom = 5;

        details.Add(category);

        // =================================================
        // EDITABLE DISPLAY NAME
        // =================================================

        if (name != null)
        {
            TextField nameField =
                new TextField("Name")
                {
                    value = name.stringValue,
                    isDelayed = true
                };

            nameField.tooltip =
                "Display name used by RöK.\n\n" +
                "This does not rename the asset file.";

            nameField.RegisterValueChangedCallback(
                evt =>
                {
                    SetProperty(
                        asset,
                        nameProperty,
                        property =>
                            property.stringValue =
                                evt.newValue
                    );

                    onIdentityChanged?.Invoke();
                }
            );

            details.Add(nameField);
        }
        else
        {
            Label nameLabel =
                new Label(asset.name);

            nameLabel.style.fontSize = 17;

            nameLabel.style.unityFontStyleAndWeight =
                FontStyle.Bold;

            details.Add(nameLabel);
        }

        // =================================================
        // ABILITY CLASSIFICATION
        // =================================================

        if (asset is AbilityData ability)
        {
            EnumField typeField =
                new EnumField(
                    "Ability Type",
                    ability.types
                );

            typeField.RegisterValueChangedCallback(
                evt =>
                {
                    AbilityType selected =
                        (AbilityType)evt.newValue;

                    SetProperty(
                        asset,
                        "types",
                        property =>
                            property.enumValueIndex =
                                (int)selected
                    );
                }
            );

            typeField.tooltip =
                "Ability classification.\n\n" +
                "Does not automatically change " +
                "targeting or damage type.";

            details.Add(typeField);
        }

        // =================================================
        // EDITABLE ICON
        // =================================================

        if (icon != null)
        {
            ObjectField iconField =
                new ObjectField("Icon")
                {
                    objectType = typeof(Sprite),
                    allowSceneObjects = false,
                    value = currentIcon
                };

            iconField.tooltip =
                "Content icon.\n\n" +
                "For buffs this overrides the " +
                "source ability's HUD icon.";

            iconField.RegisterValueChangedCallback(
                evt =>
                {
                    Sprite selected =
                        evt.newValue as Sprite;

                    SetProperty(
                        asset,
                        "icon",
                        property =>
                            property.objectReferenceValue =
                                selected
                    );

                    UpdatePreview(selected);
                }
            );

            details.Add(iconField);
        }

        // =================================================
        // ASSET LOCATION
        // =================================================

        Label path =
            new Label(
                AssetDatabase.GetAssetPath(asset)
            );

        path.style.fontSize = 10;
        path.style.color = MutedColor;

        path.style.whiteSpace =
            WhiteSpace.Normal;

        path.style.marginTop = 7;

        details.Add(path);

        root.Add(details);

        return root;
    }

    // =====================================================
    // SERIALIZED NAME MAPPING
    // =====================================================

    private static string GetNameProperty(
        ScriptableObject asset)
    {
        if (asset is AbilityData)
            return "abilityName";

        if (asset is BuffDefinition)
            return "displayName";

        if (asset is TalentData)
            return "talentName";

        // Other asset types currently use
        // their Unity asset filename.

        if (asset is ItemData)
            return "itemName";

        return null;
    }

    // =====================================================
    // SAFE SERIALIZED EDITING
    // =====================================================

    private static void SetProperty(
        ScriptableObject asset,
        string propertyPath,
        Action<SerializedProperty> edit)
    {
        if (asset == null ||
            string.IsNullOrEmpty(propertyPath))
        {
            return;
        }

        SerializedObject serialized =
            new SerializedObject(asset);

        serialized.Update();

        SerializedProperty property =
            serialized.FindProperty(propertyPath);

        if (property == null)
            return;

        edit(property);

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(asset);
        }
    }

    // =====================================================
    // COLORS
    // =====================================================

    private static Color MutedColor =>
        EditorGUIUtility.isProSkin
            ? new Color(0.68f, 0.70f, 0.73f)
            : new Color(0.36f, 0.38f, 0.42f);
}

#endif

