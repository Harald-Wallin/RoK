
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoKContentStudioEffectComposer
{
    // =====================================================
    // STATE
    // =====================================================

    private enum AddMode
    {
        Existing,
        CreateNew
    }

    private AddMode addMode = AddMode.Existing;

    private readonly RoKForgePassiveEffectList.ExistingEffectPicker existingEffectPicker =
    new();

    private RoKPassiveEffectKind effectKind =
        RoKPassiveEffectKind.StatModifier;

    private string newEffectName =
        "New Effect";

    private StatType stat = StatType.MaxHP;

    private ModifierType modifierType =
        ModifierType.Percent;

    private DamageType damageType =
        DamageType.Fire;

    private CreatureType creatureTypes =
        CreatureType.Beast;

    private float effectValue = 0.10f;

    private string selectedHelp =
        "Välj ett fält för mer information.";

    private Action<ScriptableObject> openAsset;
    private Action refreshLibrary;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void Draw(
        BuffDefinition buff,
        Action<ScriptableObject> openAsset,
        Action refreshLibrary,
        bool twoColumns=false)
    {
        if (buff == null)
            return;

        this.openAsset = openAsset;
        this.refreshLibrary = refreshLibrary;

            RoKForgeFormLayout.DrawColumns(
        twoColumns,
        () =>
        {
            DrawBuffProperties(buff);
            EditorGUILayout.Space(12);
            DrawEffectList(buff);
        },
        () => DrawEffectPicker(buff));

        EditorGUILayout.Space(12);

        DrawHelp();
    }


    // =====================================================
    // BUFF PROPERTIES
    // =====================================================

    private static void DrawBuffProperties(
        BuffDefinition buff)
    {
        SerializedObject serialized =
            new SerializedObject(buff);

        serialized.Update();

        EditorGUILayout.LabelField(
            "Buff Configuration",
            EditorStyles.boldLabel
        );

        DrawProperty(
            serialized,
            "description",
            "Description",
            "Valfri beskrivning av buffen."
        );

        EditorGUILayout.Space();

        DrawProperty(
            serialized,
            "duration",
            "Duration",
            "Buffens varaktighet i sekunder. " +
            "Effektdefinitionerna äger ingen timer."
        );

        DrawProperty(
            serialized,
            "maxStacks",
            "Max Stacks",
            "Maximalt antal stacks för samma buff " +
            "från samma källa."
        );

        DrawProperty(
            serialized,
            "refreshDurationOnReapply",
            "Refresh On Reapply",
            "Återställer buffens timer när den " +
            "appliceras igen."
        );

        DrawProperty(
            serialized,
            "removeOnDeath",
            "Remove On Death",
            "Tar bort buffen när karaktären dör."
        );

        DrawProperty(
            serialized,
            "removeOnEncounterReset",
            "Remove On Encounter Reset",
            "Tar bort buffen när en NPC gör " +
            "full encounter-reset."
        );

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(buff);
        }
    }

    private static void DrawProperty(
        SerializedObject serialized,
        string path,
        string label,
        string tooltip)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property == null)
            return;

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, tooltip),
            true
        );
    }

    // =====================================================
    // EFFECT LIST
    // =====================================================


    private void DrawEffectList(BuffDefinition buff)
    {
        SerializedObject serialized = new(buff);
        serialized.Update();

        RoKForgePassiveEffectList.Draw(
            serialized,
            "effects",
            "ACTIVE EFFECTS",
            openAsset);

        if (serialized.ApplyModifiedProperties())
            EditorUtility.SetDirty(buff);
    }

    // =====================================================
    // EFFECT PICKER
    // =====================================================

    private void DrawEffectPicker(
        BuffDefinition buff)
    {
        EditorGUILayout.LabelField(
            "Add Effect",
            EditorStyles.boldLabel
        );

        addMode = (AddMode)GUILayout.Toolbar(
            (int)addMode,
            new[]
            {
                "Reuse Existing",
                "Create New"
            }
        );

        EditorGUILayout.Space(6);

        if (addMode == AddMode.Existing)
        {
            DrawExistingPicker(buff);
        }
        else
        {
            DrawNewEffectCreator(buff);
        }
    }

    private void DrawExistingPicker(BuffDefinition buff)
    {
        SerializedObject serialized = new(buff);

        existingEffectPicker.Draw(serialized, "effects", openAsset);

        EditorGUILayout.HelpBox(
            "Effekter som redan finns i buffen kan inte läggas till igen. " +
            "Använd buffens Max Stacks för att förstärka effekten vid återapplicering.",
            MessageType.Info);
    }

    public void InvalidateCache()
    {
        existingEffectPicker.InvalidateCache();
    }

    // =====================================================
    // CREATE NEW EFFECT
    // =====================================================

    private void DrawNewEffectCreator(
        BuffDefinition buff)
    {
        newEffectName =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Effect Name",
                    "Namnet på det nya " +
                    "EffectDefinition-assetet."
                ),
                newEffectName
            );

        effectKind =
            (RoKPassiveEffectKind)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Effect Type",
                    "Vilken gameplay-mekanism " +
                    "effekten ska modifiera."
                ),
                effectKind
            );

        EditorGUILayout.Space(4);

        switch (effectKind)
        {
            case RoKPassiveEffectKind.StatModifier:

                stat =
                    (StatType)
                    EditorGUILayout.EnumPopup(
                        "Stat",
                        stat
                    );

                modifierType =
                    (ModifierType)
                    EditorGUILayout.EnumPopup(
                        "Modifier Type",
                        modifierType
                    );

                break;

            case RoKPassiveEffectKind.DamageResistance:
            case RoKPassiveEffectKind.DamageTypeBonus:

                damageType =
                    (DamageType)
                    EditorGUILayout.EnumPopup(
                        "Damage Type",
                        damageType
                    );

                break;

            case RoKPassiveEffectKind.CreatureDamageBonus:

                creatureTypes =
                    (CreatureType)
                    EditorGUILayout.EnumFlagsField(
                        "Creature Types",
                        creatureTypes
                    );

                break;
        }

        effectValue =
            EditorGUILayout.FloatField(
                new GUIContent(
                    "Value",
                    "0.10 = +10%\n" +
                    "0.25 = +25%\n" +
                    "-0.20 = -20%\n\n" +
                    "Flat modifiers använder " +
                    "ett numeriskt värde."
                ),
                effectValue
            );

        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(
            EditorApplication.isPlayingOrWillChangePlaymode ||
            string.IsNullOrWhiteSpace(newEffectName)
        );

        if (GUILayout.Button(
                "Create Effect & Add To Buff",
                GUILayout.Height(30)))
        {
            QueueCreateEffect(buff);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.HelpBox(
            "Den nya effekten sparas som ett eget " +
            "ScriptableObject och länkas automatiskt " +
            "till denna BuffDefinition.\n\n" +
            "Den får ingen egen timer. " +
            "BuffDefinition äger fortfarande duration.",
            MessageType.Info
        );
    }

    // =====================================================
    // CREATE EFFECT DEFERRED
    // =====================================================

    private void QueueCreateEffect(
        BuffDefinition buff)
    {
        string capturedName =
            newEffectName.Trim();

        RoKPassiveEffectKind capturedKind =
            effectKind;

        StatType capturedStat = stat;

        ModifierType capturedModifier =
            modifierType;

        DamageType capturedDamage =
            damageType;

        CreatureType capturedCreatures =
            creatureTypes;

        float capturedValue =
            effectValue;

        EditorApplication.delayCall += () =>
        {
            if (buff == null)
                return;

            try
            {
                EffectDefinition created =
                    RoKContentStudioFactory.Create(
                        RoKContentRecipe.PassiveEffect,
                        capturedName,
                        null,
                        capturedKind,
                        capturedStat,
                        capturedModifier,
                        capturedDamage,
                        capturedCreatures,
                        capturedValue,
                        30f,
                        0f
                    ) as EffectDefinition;

                if (created == null)
                {
                    Debug.LogError(
                        "Effect factory returned null."
                    );

                    return;
                }

                AddEffectToBuff(
                    buff,
                    created
                );

                InvalidateCache();

                refreshLibrary?.Invoke();

                EditorGUIUtility.PingObject(
                    created
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Effect creation failed",
                    exception.Message,
                    "OK"
                );
            }
        };
    }

    // =====================================================
    // ADD EFFECT TO BUFF
    // =====================================================

    private static void AddEffectToBuff(
        BuffDefinition buff,
        EffectDefinition effect)
    {
        if (buff == null ||
            effect == null ||
            ContainsEffect(buff, effect))
        {
            return;
        }

        SerializedObject serialized =
            new SerializedObject(buff);

        SerializedProperty array =
            serialized.FindProperty("effects");

        if (array == null)
            return;

        Undo.RecordObject(
            buff,
            "Add Effect To Buff"
        );

        AddElement(array, effect);

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(buff);

        AssetDatabase.SaveAssets();
    }

    // =====================================================
    // EDIT ARRAY DEFERRED
    // =====================================================

    private static void QueueEdit(
        BuffDefinition buff,
        string operation,
        Action<SerializedProperty> edit)
    {
        EditorApplication.delayCall += () =>
        {
            if (buff == null)
                return;

            SerializedObject serialized =
                new SerializedObject(buff);

            SerializedProperty array =
                serialized.FindProperty("effects");

            if (array == null)
                return;

            Undo.RecordObject(
                buff,
                operation
            );

            edit(array);

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(buff);

            AssetDatabase.SaveAssets();
        };
    }

    // =====================================================
    // ARRAY HELPERS
    // =====================================================

    private static void AddElement(
        SerializedProperty array,
        EffectDefinition definition)
    {
        int index = array.arraySize;

        array.InsertArrayElementAtIndex(
            index
        );

        array.GetArrayElementAtIndex(index)
            .objectReferenceValue =
                definition;
    }

    private static bool ContainsEffect(
        BuffDefinition buff,
        EffectDefinition effect)
    {
        if (buff?.Effects == null ||
            effect == null)
        {
            return false;
        }

        foreach (EffectDefinition existing
                 in buff.Effects)
        {
            if (existing == effect)
                return true;
        }

        return false;
    }

    // =====================================================
    // HELP
    // =====================================================

    private void DrawHelp()
    {
        EditorGUILayout.LabelField(
            "Effect Composer Help",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            selectedHelp,
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "What is an EffectDefinition?"))
        {
            selectedHelp =
                "EffectDefinition är ett återanvändbart " +
                "ScriptableObject som beskriver " +
                "en gameplay-effekt.\n\n" +
                "Det innehåller ingen aktiv timer " +
                "och ska inte lagra karaktärens " +
                "föränderliga runtime-state.\n\n" +
                "Samma definition kan användas " +
                "av flera buffs, talents, " +
                "titles och equipment.";
        }

        if (GUILayout.Button(
                "How do Stacks work?"))
        {
            selectedHelp =
                "BuffDefinition bestämmer hur många " +
                "stacks en buff får ha.\n\n" +
                "När stacks ändras uppdaterar " +
                "CharacterEffectController alla " +
                "effektinstanser som tillhör buffen.\n\n" +
                "Exempel:\n" +
                "+10% MaxHP med 2 stacks = +20%.";
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(
                "Ability Action vs Passive Effect"))
        {
            selectedHelp =
                "AbilityEffect är en handling som " +
                "körs av AbilityEffectPipeline.\n\n" +
                "EffectDefinition är en återanvändbar " +
                "gameplay-effekt som kan vara aktiv " +
                "under längre tid.\n\n" +
                "ApplyBuffDefinitionEffect är bryggan " +
                "mellan en ability och en buff.\n\n" +
                "AbilityData → AbilityEffect → " +
                "BuffDefinition → EffectDefinition.";
        }
    }
}

#endif

