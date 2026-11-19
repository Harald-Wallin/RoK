
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

// Common rendering mechanics only.
internal static class RoKForgeEditorFields
{
    public static void Draw(
     ScriptableObject asset,
     bool twoColumns,
     Action<SerializedObject> left,
     Action<SerializedObject> right)
    {
        SerializedObject so = new(asset);
        so.Update();

        RoKForgeFormLayout.DrawColumns(
            twoColumns,
            () => left(so),
            () => right(so));

        if (so.ApplyModifiedProperties() && AssetDatabase.Contains(asset))
            EditorUtility.SetDirty(asset);
    }

    public static void Group(
        SerializedObject so,
        string heading,
        params string[] fields)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            heading, EditorStyles.boldLabel);

        foreach (string field in fields)
            Field(so, field);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    public static void Field(
        SerializedObject so,
        string name)
    {
        SerializedProperty property = so.FindProperty(name);

        if (property != null)
            EditorGUILayout.PropertyField(property, true);
    }

    public static bool Enabled(
        SerializedObject so,
        string name)
    {
        return so.FindProperty(name)?.boolValue ?? false;
    }



    public static void LinkedAssetArray(
        SerializedObject so,
        string propertyName,
        string title,
        Action<ScriptableObject> openAsset)
    {
        RoKForgePassiveEffectList.Draw(
            so,
            propertyName,
            title,
            openAsset);
    }
}

public static class RoKForgeItemEditor
{
    public static void Draw(
        ItemData item,
        bool twoColumns)
    {
        RoKForgeEditorFields.Draw(
            item,
            twoColumns,
            DrawLeft,
            DrawRight);
    }

    private static void DrawLeft(SerializedObject so)
    {
        RoKForgeEditorFields.Group(
            so,
            "GENERAL",
            "description",
            "worldSprite");

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                "ITEM PROFILE",
                EditorStyles.boldLabel);

            SerializedProperty type =
                so.FindProperty("itemType");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(type);
            bool changed = EditorGUI.EndChangeCheck();

            ItemType selected =
                (ItemType)type.enumValueIndex;

            bool draft =
                !AssetDatabase.Contains(so.targetObject);

            if (changed &&
                (draft ||
                 selected is ItemType.Container or ItemType.Trophy))
            {
                ApplyTypeDefaults(so, selected);
            }

            if (selected == ItemType.Trophy)
            {
                so.FindProperty("rarity").enumValueIndex =
                    (int)ItemRarity.Extraordinary;
            }

            using (new EditorGUI.DisabledScope(
                       selected == ItemType.Trophy))
            {
                RoKForgeEditorFields.Field(so, "rarity");
            }

            if (!draft &&
                GUILayout.Button("Apply Type Defaults"))
            {
                ApplyTypeDefaults(so, selected);
            }

            if (selected == ItemType.None)
            {
                EditorGUILayout.HelpBox(
                    "Select an Item Type.",
                    MessageType.Warning);
            }
        }

        ItemType selectedType =
            (ItemType)so.FindProperty("itemType").enumValueIndex;

        bool container =
            selectedType == ItemType.Container;

        bool trophy =
            selectedType == ItemType.Trophy;

        if (selectedType == ItemType.Weapon)
            DrawWeaponProfile(so);

        using (new EditorGUI.DisabledScope(container || trophy))
        {
            RoKForgeEditorFields.Group(
                so,
                "INVENTORY",
                "stackable");
        }

        RoKForgeEditorFields.Field(
            so,
            "maximumOwned");

        if (RoKForgeEditorFields.Enabled(so, "stackable"))
        {
            RoKForgeEditorFields.Group(
                so,
                "STACKING",
                "maxStack");
        }

        using (new EditorGUI.DisabledScope(container || trophy))
        {
            RoKForgeEditorFields.Group(
                so,
                "EQUIPMENT",
                "equippable");
        }

        if (!RoKForgeEditorFields.Enabled(so, "equippable"))
            return;

        RoKForgeEditorFields.Group(
            so,
            "DIRECTIONAL SPRITES",
            "frontSprite",
            "backSprite",
            "leftSprite",
            "rightSprite");

        RoKForgeEditorFields.Group(
            so,
            "HIDE CHARACTER PARTS",
            "hideHair",
            "hideBeard",
            "hideHead",
            "hideArms",
            "hideTorso",
            "hideFeet");
    }

    private static void DrawWeaponProfile(
        SerializedObject so)
    {
        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                "WEAPON",
                EditorStyles.boldLabel);

            RoKForgeEditorFields.Field(
                so,
                "weaponFamily");

            SerializedProperty property =
                so.FindProperty("weaponDamageRange");

            Vector2Int range =
                property.vector2IntValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(
                    new GUIContent(
                        "Weapon Damage",
                        "Minimum and maximum damage. " +
                        "0–0 uses the existing stat modifiers."));

                EditorGUI.BeginChangeCheck();

                int minimum =
                    EditorGUILayout.IntField(range.x);

                GUILayout.Label(
                    "–",
                    GUILayout.Width(12));

                int maximum =
                    EditorGUILayout.IntField(range.y);

                if (EditorGUI.EndChangeCheck())
                {
                    minimum = Mathf.Max(0, minimum);

                    property.vector2IntValue =
                        new Vector2Int(
                            minimum,
                            Mathf.Max(minimum, maximum));
                }
            }
        }
    }

    private static void ApplyTypeDefaults(
        SerializedObject so,
        ItemType type)
    {
        bool equippable =
            type is ItemType.Weapon or
                ItemType.Helmet or
                ItemType.Chest or
                ItemType.Back or
                ItemType.Shoulders or
                ItemType.Legs or
                ItemType.Feet or
                ItemType.Neck or
                ItemType.Ring or
                ItemType.Offhand or
                ItemType.Shield;

        bool stackable =
            type is ItemType.Food or
                ItemType.Reagent or
                ItemType.Trash or
                ItemType.Trophy;

        so.FindProperty("equippable").boolValue =
            equippable;

        so.FindProperty("stackable").boolValue =
            stackable;

        so.FindProperty("maxStack").intValue =
            type == ItemType.Trophy
                ? 999
                : stackable ? 20 : 1;

        if (type == ItemType.Trophy)
        {
            so.FindProperty("rarity").enumValueIndex =
                (int)ItemRarity.Extraordinary;
        }
    }

    private static void DrawRight(
        SerializedObject so)
    {
        RoKForgeEditorFields.Group(
            so,
            "STAT MODIFIERS",
            "statModifiers");

        RoKForgeEditorFields.Group(
            so,
            "REQUIREMENTS",
            "useLevelRequirement");

        if (RoKForgeEditorFields.Enabled(
                so,
                "useLevelRequirement"))
        {
            RoKForgeEditorFields.Group(
                so,
                "LEVEL",
                "requiredLevel");
        }

        RoKForgeEditorFields.Field(
            so,
            "useReputationRequirement");

        if (RoKForgeEditorFields.Enabled(
                so,
                "useReputationRequirement"))
        {
            RoKForgeEditorFields.Group(
                so,
                "REPUTATION",
                "reputationFaction",
                "requiredReputation");
        }

        RoKForgeEditorFields.Group(
            so,
            "ECONOMY",
            "buyPrice",
            "sellMultiplier");

        ItemType type =
            (ItemType)so.FindProperty("itemType").enumValueIndex;

        if (type == ItemType.Container)
        {
            RoKForgeEditorFields.Group(
                so,
                "CONTAINER LOOT",
                "containerLootTables",
                "containerMinRolls",
                "containerMaxRolls");
        }

        if (type == ItemType.Food)
        {
            RoKForgeEditorFields.Group(
                so,
                "FOOD",
                "foodHealingPerTick",
                "foodTickInterval",
                "foodChannelDuration",
                "allowFoodAtFullHealth");
        }

        if (type == ItemType.FavourItem)
        {
            RoKForgeEditorFields.Group(
                so,
                "FAVOUR LOOT",
                "requiredFavour");

            if (so.FindProperty(
                    "requiredFavour").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "A FavourItem requires a linked FavourData " +
                    "to qualify for Favour loot.",
                    MessageType.Warning);
            }
        }

        RoKForgeEditorFields.Group(
            so,
            "FAVOUR INTERACTION",
            "favourUse");
    }
}

// Talent-specific presentation and rules.

public static class RoKForgeTalentEditor
{
    private static readonly RoKForgePassiveEffectList.ExistingEffectPicker existingEffectPicker =
    new();


    public static void Draw(
        TalentData talent,
        bool twoColumns,
        Action<TalentData> createNewEffect = null,
        Action<ScriptableObject> openAsset = null)

    {
        RoKForgeEditorFields.Draw(
            talent,
            twoColumns,
            DrawLeft,

            so => DrawRight(
                so,
                talent,
                createNewEffect,
                openAsset
            )

        );
    }

    private static void DrawLeft(SerializedObject so)
    {
        RoKForgeEditorFields.Group(
            so, "GENERAL", "description");

        RoKForgeEditorFields.Group(
            so, "PROGRESSION", "maxPoints", "tier");

        RoKForgeEditorFields.Group(
            so, "PREREQUISITES", "requirements");
    }

    private static void DrawRight(
        SerializedObject so,
        TalentData talent,
        Action<TalentData> createNewEffect,
        Action<ScriptableObject> openAsset)
    {
        // Existing shared effects.
        // Only references are modified here.
        RoKForgeEditorFields.LinkedAssetArray(
            so,
            "passiveEffects",
            "PASSIVE EFFECTS",
            openAsset
        );

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Add Existing Effect", EditorStyles.boldLabel);
        existingEffectPicker.Draw(so, "passiveEffects", openAsset);
        EditorGUILayout.Space(6);

        bool saved = AssetDatabase.Contains(talent);

        using (new EditorGUI.DisabledScope(
            !saved ||
            createNewEffect == null ||
            EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(
                "Create & Attach New Effect",
                GUILayout.Height(30)))
            {
                TalentData captured = talent;

                EditorApplication.delayCall += () =>
                {
                    if (captured != null)
                        createNewEffect?.Invoke(captured);
                };
            }
        }

        if (!saved)
        {
            EditorGUILayout.HelpBox(
                "Save the Talent before creating new " +
                "EffectDefinition assets.\n\n" +
                "Existing effects can be linked now.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(8);

        RoKForgeEditorFields.Group(
            so, "ABILITY UNLOCK", "unlockedAbility");

        RoKForgeEditorFields.Group(
            so, "WARD", "unlocksWardSystem");
    }
}


#endif

