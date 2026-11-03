
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// =========================================================
// SELF BUFF SOURCE
// =========================================================

public enum RoKSelfBuffSource
{
    Existing,
    CreateNew
}

// =========================================================
// INLINE BUFF COMPOSER
// =========================================================

public sealed class RoKContentStudioInlineBuffComposer
{
    public RoKBuffRecipeDraft Draft { get; } =
        new();

    // =====================================================
    // DRAW
    // =====================================================

    public void Draw()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.LabelField(
            "New Buff",
            EditorStyles.boldLabel
        );

        Draft.Name =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Buff Name",
                    "Namnet på den nya BuffDefinition."
                ),
                Draft.Name
            );

        Draft.Duration =
            EditorGUILayout.FloatField(
                new GUIContent(
                    "Duration",
                    "Buffens varaktighet i sekunder.\n\n" +
                    "Exempel: 30 = 30 sekunder."
                ),
                Draft.Duration
            );

        Draft.MaxStacks =
            EditorGUILayout.IntField(
                new GUIContent(
                    "Max Stacks",
                    "Maximalt antal stacks.\n\n" +
                    "Alla buffens effekter påverkas " +
                    "av stack-antalet."
                ),
                Draft.MaxStacks
            );

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            $"Passive Effects ({Draft.Effects.Count})",
            EditorStyles.boldLabel
        );

        int removeIndex = -1;
        int moveFrom = -1;
        int moveTo = -1;

        // =============================================
        // EFFECT ENTRIES
        // =============================================

        for (int i = 0; i < Draft.Effects.Count; i++)
        {
            RoKBuffEffectDraft effect =
                Draft.Effects[i];

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Effect {i + 1}",
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button(
                    "↑",
                    GUILayout.Width(28)))
                {
                    moveFrom = i;
                    moveTo = i - 1;
                }
            }

            using (new EditorGUI.DisabledScope(
                i >= Draft.Effects.Count - 1))
            {
                if (GUILayout.Button(
                    "↓",
                    GUILayout.Width(28)))
                {
                    moveFrom = i;
                    moveTo = i + 1;
                }
            }

            if (GUILayout.Button(
                "Remove",
                GUILayout.Width(65)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            // =========================================
            // SOURCE
            // =========================================

            effect.Source =
                (RoKBuffEffectSource)
                EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Source",
                        "Existing återanvänder ett asset.\n\n" +
                        "CreateNew skapar en ny " +
                        "EffectDefinition."
                    ),
                    effect.Source
                );

            if (effect.Source ==
                RoKBuffEffectSource.Existing)
            {
                DrawExistingEffect(effect);
            }
            else
            {
                DrawNewEffect(effect);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
        }

        // =============================================
        // APPLY LIST OPERATIONS
        // =============================================

        if (removeIndex >= 0)
        {
            Draft.Effects.RemoveAt(
                removeIndex
            );

            GUI.changed = true;
        }
        else if (moveFrom >= 0 &&
                 moveTo >= 0)
        {
            RoKBuffEffectDraft temporary =
                Draft.Effects[moveFrom];

            Draft.Effects[moveFrom] =
                Draft.Effects[moveTo];

            Draft.Effects[moveTo] =
                temporary;

            GUI.changed = true;
        }

        if (GUILayout.Button(
            "+ Add Passive Effect",
            GUILayout.Height(28)))
        {
            Draft.Effects.Add(
                new RoKBuffEffectDraft()
            );

            GUI.changed = true;
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Alla effekter i denna lista aktiveras " +
            "när buffen appliceras.\n\n" +
            "BuffDefinition äger duration och stacks. " +
            "Varje EffectDefinition beskriver en " +
            "enskild gameplay-modifiering.",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    // =====================================================
    // EXISTING EFFECT
    // =====================================================

    private static void DrawExistingEffect(
        RoKBuffEffectDraft effect)
    {
        effect.ExistingEffect =
            (EffectDefinition)
            EditorGUILayout.ObjectField(
                new GUIContent(
                    "Effect Definition",
                    "Befintligt EffectDefinition-asset.\n\n" +
                    "Detta återanvänds utan att kopieras."
                ),
                effect.ExistingEffect,
                typeof(EffectDefinition),
                false
            );

        if (effect.ExistingEffect != null)
        {
            EditorGUILayout.HelpBox(
                effect.ExistingEffect.GetDescription(),
                MessageType.None
            );
        }
    }

    // =====================================================
    // NEW EFFECT
    // =====================================================

    private static void DrawNewEffect(
        RoKBuffEffectDraft effect)
    {
        effect.Name =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Effect Name",
                    "Namnet på den nya " +
                    "EffectDefinition-filen."
                ),
                effect.Name
            );

        effect.Kind =
            (RoKPassiveEffectKind)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Effect Type",
                    "Vilken gameplay-mekanism " +
                    "effekten ska modifiera."
                ),
                effect.Kind
            );

        switch (effect.Kind)
        {
            case RoKPassiveEffectKind.StatModifier:

                DrawStatModifier(effect);
                break;

            case RoKPassiveEffectKind.DamageResistance:

                DrawDamageType(effect);
                break;

            case RoKPassiveEffectKind.CreatureDamageBonus:

                effect.CreatureTypes =
                    (CreatureType)
                    EditorGUILayout.EnumFlagsField(
                        new GUIContent(
                            "Creature Types",
                            "Vilka creature-kategorier " +
                            "som påverkas."
                        ),
                        effect.CreatureTypes
                    );

                break;

            case RoKPassiveEffectKind.DamageTypeBonus:

                DrawDamageType(effect);
                break;
        }

        effect.Value =
            EditorGUILayout.FloatField(
                new GUIContent(
                    "Value",
                    "0.10 = +10%\n" +
                    "0.25 = +25%\n" +
                    "-0.20 = -20%\n\n" +
                    "Flat stat modifiers använder " +
                    "numeriska värden."
                ),
                effect.Value
            );

        EditorGUILayout.HelpBox(
            GetEffectHelp(effect.Kind),
            MessageType.None
        );
    }

    private static void DrawStatModifier(
        RoKBuffEffectDraft effect)
    {
        effect.Stat =
            (StatType)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Stat",
                    "Karaktärsstatistiken som " +
                    "ska modifieras."
                ),
                effect.Stat
            );

        effect.ModifierType =
            (ModifierType)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Modifier Type",
                    "Flat eller Percent."
                ),
                effect.ModifierType
            );
    }

    private static void DrawDamageType(
        RoKBuffEffectDraft effect)
    {
        effect.DamageType =
            (DamageType)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Damage Type",
                    "Vilken sorts skada som påverkas."
                ),
                effect.DamageType
            );
    }

    // =====================================================
    // CONTEXTUAL HELP
    // =====================================================

    private static string GetEffectHelp(
        RoKPassiveEffectKind kind)
    {
        return kind switch
        {
            RoKPassiveEffectKind.StatModifier =>
                "Stat Modifier ändrar en karaktärsstat.\n" +
                "Exempel: +10% Maximum Health.",

            RoKPassiveEffectKind.DamageResistance =>
                "Damage Resistance reducerar inkommande " +
                "skada av vald DamageType.\n" +
                "Exempel: 0.25 Fire = +25% Fire Resistance.",

            RoKPassiveEffectKind.CreatureDamageBonus =>
                "Creature Damage Bonus ökar skadan mot " +
                "valda creature-kategorier.\n" +
                "Exempel: 0.10 Beast = +10% mot Beasts.",

            RoKPassiveEffectKind.DamageTypeBonus =>
                "Damage Type Bonus ökar utgående " +
                "skada av vald DamageType.\n" +
                "Exempel: 0.10 Frost = +10% Frost Damage.",

            _ => string.Empty
        };
    }
}

#endif

