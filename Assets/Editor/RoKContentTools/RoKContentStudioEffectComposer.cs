
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

    private string search = string.Empty;

    private Vector2 resultScroll;

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

    private List<EffectDefinition> cachedEffects;

    private string cachedSearch;

    private Action<ScriptableObject> openAsset;
    private Action refreshLibrary;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void Draw(
        BuffDefinition buff,
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        if (buff == null)
            return;

        this.openAsset = openAsset;
        this.refreshLibrary = refreshLibrary;

        DrawBuffProperties(buff);

        EditorGUILayout.Space(12);

        DrawEffectList(buff);

        EditorGUILayout.Space(12);

        DrawEffectPicker(buff);

        EditorGUILayout.Space(12);

        DrawHelp();
    }

    public void InvalidateCache()
    {
        cachedEffects = null;
        cachedSearch = null;
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
            "displayName",
            "Display Name",
            "Namnet som visas i buff-UI."
        );

        DrawProperty(
            serialized,
            "description",
            "Description",
            "Valfri beskrivning av buffen."
        );

        DrawProperty(
            serialized,
            "icon",
            "Icon",
            "Ikonen som används av buff-UI."
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

    private void DrawEffectList(
        BuffDefinition buff)
    {
        SerializedObject serialized =
            new SerializedObject(buff);

        serialized.Update();

        SerializedProperty effects =
            serialized.FindProperty("effects");

        if (effects == null)
        {
            EditorGUILayout.HelpBox(
                "BuffDefinition saknar effects-array.",
                MessageType.Error
            );

            return;
        }

        EditorGUILayout.LabelField(
            $"Active Effects ({effects.arraySize})",
            EditorStyles.boldLabel
        );

        if (effects.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Buffen saknar gameplay-effekter.\n\n" +
                "Lägg till en befintlig effekt eller " +
                "skapa en ny nedan.",
                MessageType.Info
            );
        }

        for (int i = 0;
             i < effects.arraySize;
             i++)
        {
            DrawEffectRow(
                buff,
                effects,
                i
            );
        }

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(buff);
        }
    }

    // =====================================================
    // EFFECT ROW
    // =====================================================

    private void DrawEffectRow(
        BuffDefinition buff,
        SerializedProperty effects,
        int index)
    {
        SerializedProperty element =
            effects.GetArrayElementAtIndex(index);

        EffectDefinition definition =
            element.objectReferenceValue
                as EffectDefinition;

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            $"Effect {index + 1}",
            EditorStyles.boldLabel,
            GUILayout.Width(75)
        );

        EditorGUI.BeginDisabledGroup(
            index == 0
        );

        if (GUILayout.Button(
                "↑",
                GUILayout.Width(28)))
        {
            int captured = index;

            QueueEdit(
                buff,
                "Move Effect Up",
                array =>
                    array.MoveArrayElement(
                        captured,
                        captured - 1
                    )
            );
        }

        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(
            index >= effects.arraySize - 1
        );

        if (GUILayout.Button(
                "↓",
                GUILayout.Width(28)))
        {
            int captured = index;

            QueueEdit(
                buff,
                "Move Effect Down",
                array =>
                    array.MoveArrayElement(
                        captured,
                        captured + 1
                    )
            );
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
                "Remove",
                GUILayout.Width(65)))
        {
            int captured = index;

            QueueEdit(
                buff,
                "Remove Effect",
                array =>
                    DeleteElement(
                        array,
                        captured
                    )
            );
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(
            element,
            new GUIContent(
                "Definition",
                "Referens till ett återanvändbart " +
                "EffectDefinition-asset."
            )
        );

        if (definition != null)
        {
            EditorGUILayout.LabelField(
                definition.GetType().Name,
                EditorStyles.miniLabel
            );

            string description =
                definition.GetDescription();

            if (!string.IsNullOrWhiteSpace(
                    description))
            {
                EditorGUILayout.HelpBox(
                    description,
                    MessageType.None
                );
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "Open in Content Studio"))
            {
                EffectDefinition captured =
                    definition;

                // Avoid changing the UI tree
                // during IMGUI layout.
                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            if (GUILayout.Button(
                    "Ping",
                    GUILayout.Width(55)))
            {
                EditorGUIUtility.PingObject(
                    definition
                );
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Tom effekt-referens.",
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();
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

    // =====================================================
    // SEARCH EXISTING EFFECTS
    // =====================================================

    private void DrawExistingPicker(
        BuffDefinition buff)
    {
        string nextSearch =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Search",
                    "Sök bland samtliga befintliga " +
                    "passiva EffectDefinition-assets."
                ),
                search
            );

        if (nextSearch != search)
        {
            search = nextSearch;
            InvalidateCache();
        }

        if (cachedEffects == null ||
            cachedSearch != search)
        {
            RebuildCache();
        }

        if (cachedEffects.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Inga matchande effekter hittades.",
                MessageType.Info
            );

            return;
        }

        resultScroll =
            EditorGUILayout.BeginScrollView(
                resultScroll,
                GUILayout.MaxHeight(240)
            );

        foreach (EffectDefinition effect
                 in cachedEffects)
        {
            if (effect == null)
                continue;

            EditorGUILayout.BeginHorizontal(
                EditorStyles.helpBox
            );

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(
                effect.name,
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                effect.GetDescription(),
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndVertical();

            EditorGUI.BeginDisabledGroup(
                ContainsEffect(buff, effect)
            );

            if (GUILayout.Button(
                    "Add",
                    GUILayout.Width(55)))
            {
                EffectDefinition captured =
                    effect;

                QueueEdit(
                    buff,
                    "Add Existing Effect",
                    array =>
                        AddElement(
                            array,
                            captured
                        )
                );
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(
                    "Open",
                    GUILayout.Width(55)))
            {
                EffectDefinition captured =
                    effect;

                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "Effekter som redan finns i buffen " +
            "kan inte läggas till en andra gång.\n\n" +
            "Använd buffens Max Stacks för " +
            "att förstärka effekten vid återapplicering.",
            MessageType.Info
        );
    }

    // =====================================================
    // EFFECT CACHE
    // =====================================================

    private void RebuildCache()
    {
        cachedEffects = new();

        List<ScriptableObject> assets =
            RoKContentStudioCatalog.Find(
                RoKContentCategory.PassiveEffects,
                search
            );

        foreach (ScriptableObject asset in assets)
        {
            if (asset is EffectDefinition effect)
            {
                cachedEffects.Add(effect);
            }
        }

        cachedSearch = search;
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

    private static void DeleteElement(
        SerializedProperty array,
        int index)
    {
        if (index < 0 ||
            index >= array.arraySize)
        {
            return;
        }

        int previousSize =
            array.arraySize;

        array.DeleteArrayElementAtIndex(
            index
        );

        // Unity may first clear an Object reference
        // instead of removing its array slot.
        if (array.arraySize == previousSize)
        {
            array.DeleteArrayElementAtIndex(
                index
            );
        }
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

