
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

        if (twoColumns)
        {
            EditorGUILayout.BeginHorizontal();
            BeginColumn();
        }

        left(so);

        if (twoColumns)
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            BeginColumn();
        }

        right(so);

        if (twoColumns)
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        if (so.ApplyModifiedProperties() &&
            AssetDatabase.Contains(asset))
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



    private static void BeginColumn()
    {
        EditorGUILayout.BeginVertical(
            GUILayout.MinWidth(280),
            GUILayout.ExpandWidth(true));
    }
}

// Item-specific presentation and rules.
public static class RoKForgeItemEditor
{
    public static void Draw(ItemData item, bool twoColumns)
    {
        RoKForgeEditorFields.Draw(
            item, twoColumns, DrawLeft, DrawRight);
    }


    private static void DrawLeft(SerializedObject so)
    {
        RoKForgeEditorFields.Group(
            so, "GENERAL", "description", "worldSprite");

        // =============================================
        // ITEM PROFILE
        // =============================================

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ITEM PROFILE",
            EditorStyles.boldLabel
        );

        SerializedProperty type =
            so.FindProperty("itemType");

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(type);

        bool changed = EditorGUI.EndChangeCheck();

        ItemType selectedType =
            (ItemType)type.enumValueIndex;

        bool isDraft =
            !AssetDatabase.Contains(so.targetObject);

        if (changed && isDraft)
        {
            ApplyTypeDefaults(so, selectedType);
        }

        RoKForgeEditorFields.Field(so, "rarity");

        if (!isDraft)
        {
            if (GUILayout.Button(
                new GUIContent(
                    "Apply Type Defaults",
                    "Updates Equippable, Stackable and " +
                    "Max Stack. Other settings are preserved."
                )))
            {
                ApplyTypeDefaults(so, selectedType);
            }
        }

        if (selectedType == ItemType.None)
        {
            EditorGUILayout.HelpBox(
                "Select an Item Type before using this item.",
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();

        // =============================================
        // INVENTORY
        // =============================================

        RoKForgeEditorFields.Group(
            so, "INVENTORY",
            "stackable", "maximumOwned");

        if (RoKForgeEditorFields.Enabled(so, "stackable"))
        {
            RoKForgeEditorFields.Group(
                so, "STACKING", "maxStack");
        }

        // =============================================
        // EQUIPMENT
        // =============================================

        RoKForgeEditorFields.Group(
            so, "EQUIPMENT", "equippable");

        if (!RoKForgeEditorFields.Enabled(so, "equippable"))
            return;

        RoKForgeEditorFields.Group(
            so, "DIRECTIONAL SPRITES",
            "frontSprite", "backSprite",
            "leftSprite", "rightSprite");

        RoKForgeEditorFields.Group(
            so, "HIDE CHARACTER PARTS",
            "hideHair", "hideBeard", "hideHead",
            "hideArms", "hideTorso", "hideLegs",
            "hideFeet");
    }


    private static void ApplyTypeDefaults(
        SerializedObject so,
        ItemType type)
    {
        bool equippable = type switch
        {
            ItemType.Weapon or
            ItemType.Helmet or
            ItemType.Chest or
            ItemType.Back or
            ItemType.Shoulders or
            ItemType.Legs or
            ItemType.Feet or
            ItemType.Neck or
            ItemType.Ring or
            ItemType.Offhand or
            ItemType.Shield => true,

            _ => false
        };

        bool stackable = type switch
        {
            ItemType.Food or
            ItemType.Reagent or
            ItemType.Trash => true,

            _ => false
        };

        so.FindProperty("equippable").boolValue =
            equippable;

        so.FindProperty("stackable").boolValue =
            stackable;

        so.FindProperty("maxStack").intValue =
            stackable ? 20 : 1;
    }


    private static void DrawRight(SerializedObject so)
    {
        RoKForgeEditorFields.Group(
            so, "STAT MODIFIERS", "statModifiers");

        RoKForgeEditorFields.Group(
            so, "REQUIREMENTS", "useLevelRequirement");

        if (RoKForgeEditorFields.Enabled(
            so, "useLevelRequirement"))
        {
            RoKForgeEditorFields.Group(
                so, "LEVEL", "requiredLevel");
        }

        RoKForgeEditorFields.Field(
            so, "useReputationRequirement");

        if (RoKForgeEditorFields.Enabled(
            so, "useReputationRequirement"))
        {
            RoKForgeEditorFields.Group(
                so, "REPUTATION",
                "reputationFaction", "requiredReputation");
        }

        RoKForgeEditorFields.Group(
            so, "ECONOMY", "buyPrice", "sellMultiplier");

        SerializedProperty type = so.FindProperty("itemType");

        if (type != null &&
            type.enumValueIndex == (int)ItemType.Food)
        {
            RoKForgeEditorFields.Group(
                so, "FOOD",
                "foodHealingPerTick",
                "foodTickInterval",
                "foodChannelDuration",
                "allowFoodAtFullHealth");
        }


        if (type != null &&
            type.enumValueIndex == (int)ItemType.FavourItem)
        {
            RoKForgeEditorFields.Group(
                so, "FAVOUR LOOT", "requiredFavour");

            SerializedProperty required =
                so.FindProperty("requiredFavour");

            if (required != null &&
                required.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "A FavourItem requires a linked FavourData " +
                    "to qualify for Favour loot.",
                    MessageType.Warning
                );
            }
        }


        RoKForgeEditorFields.Group(
            so, "FAVOUR INTERACTION", "favourUse");
    }
}

// Talent-specific presentation and rules.

public static class RoKForgeTalentEditor
{

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
            "PASSIVE EFFECTS",
            "passiveEffects",
            openAsset
        );

        EditorGUILayout.HelpBox(
            "Use the + button above to link existing " +
            "EffectDefinition assets.\n\n" +
            "Shared effects are edited separately.",
            MessageType.None
        );

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

