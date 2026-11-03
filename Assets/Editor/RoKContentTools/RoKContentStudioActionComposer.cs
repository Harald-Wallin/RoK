
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoKContentStudioActionComposer
{
    // =====================================================
    // STATE
    // =====================================================

    private bool showAbilitySettings = true;
    private bool showLegacyRules;

    private string search = string.Empty;

    private List<AbilityEffect> cachedActions;
    private string cachedSearch;

    private Vector2 searchScroll;

    private int selectedTemplateIndex;

    private BuffDefinition selectedBuff;

    private AbilityEffect expandedAction;

    private string selectedHelp =
        "Klicka på ? vid ett fält för " +
        "en utförligare förklaring.";

    private Action<ScriptableObject> openAsset;
    private Action refreshLibrary;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void Draw(
        AbilityData ability,
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        if (ability == null)
            return;

        this.openAsset = openAsset;
        this.refreshLibrary = refreshLibrary;

        bool editingDisabled =
            EditorApplication.isPlayingOrWillChangePlaymode;

        if (editingDisabled)
        {
            EditorGUILayout.HelpBox(
                "Content Studio authoring är låst " +
                "under Play Mode.\n\n" +
                "Återgå till Edit Mode för att " +
                "ändra eller skapa assets.",
                MessageType.Warning
            );
        }

        using (new EditorGUI.DisabledScope(editingDisabled))
        {
            DrawAbilitySettings(ability);

            EditorGUILayout.Space(12);

            DrawActionList(ability);

            EditorGUILayout.Space(12);

            DrawActionPicker(ability);
        }

        EditorGUILayout.Space(12);

        DrawHelp();
    }

    public void InvalidateCache()
    {
        cachedActions = null;
        cachedSearch = null;
    }

    // =====================================================
    // ABILITY SETTINGS
    // =====================================================

    private void DrawAbilitySettings(
        AbilityData ability)
    {
        showAbilitySettings =
            EditorGUILayout.Foldout(
                showAbilitySettings,
                "Ability Configuration",
                true
            );

        if (!showAbilitySettings)
            return;

        SerializedObject serialized =
            new SerializedObject(ability);

        serialized.Update();

        Section("Identity");

        DrawProperty(
            serialized,
            "abilityName",
            "Ability Name",
            "Namnet som visas i spelets UI."
        );

        DrawProperty(
            serialized,
            "description",
            "Description",
            "Spelarens beskrivning av abilityn."
        );

        DrawProperty(
            serialized,
            "icon",
            "Icon",
            "Ikonen i actionbaren och Spellbook."
        );

        Section("Classification");

        DrawProperty(
            serialized,
            "types",
            "Ability Type",
            "Melee, Spell, Buff, Curse eller Ranged."
        );

        DrawProperty(
            serialized,
            "usageType",
            "Usage Type",
            "Ability eller BaseAttack.\n\n" +
            "BaseAttacks använder särskilda slots."
        );

        DrawProperty(
            serialized,
            "tags",
            "Tags",
            "Taggar för framtida interaktioner " +
            "och gameplay-regler."
        );

        Section("Targeting");

        DrawProperty(
            serialized,
            "previewMode",
            "Target Preview",
            "Bestämmer hur targeting visas, " +
            "inte vilka mål som faktiskt påverkas."
        );

        DrawProperty(
            serialized,
            "targetingSettings",
            "Targeting Settings",
            "Bestämmer vilka mål som är giltiga.\n\n" +
            "Exempel: Self, SingleTarget, Cone, Line."
        );

        Section("Timing & Execution");

        DrawProperty(
            serialized,
            "timingSettings",
            "Timing Settings",
            "Cast, Charge, Channel och Recovery."
        );

        DrawProperty(
            serialized,
            "executionSettings",
            "Execution Settings",
            "Cooldown, GCD, activation mode " +
            "och andra exekveringsregler."
        );

        DrawProperty(
            serialized,
            "chargeSettings",
            "Charge Settings",
            "Inställningar för charge-baserade abilities."
        );

        DrawProperty(
            serialized,
            "chargeCompletionPreviewEffect",
            "Full Charge Preview",
            "Visuell indikator vid full charge."
        );

        Section("Gameplay Rules");

        DrawProperty(
            serialized,
            "wardCost",
            "Ward Cost",
            "Hur många Wards abilityn kostar."
        );

        DrawProperty(
            serialized,
            "alwaysHits",
            "Always Hits",
            "Om abilityn alltid ska träffa."
        );

        DrawProperty(
            serialized,
            "canMiss",
            "Can Miss",
            "Om miss-resolution tillåts."
        );

        DrawProperty(
            serialized,
            "canCrit",
            "Can Crit",
            "Om abilityns skada får bli critical."
        );

        DrawProperty(
            serialized,
            "requiresHitCheck",
            "Requires Hit Check",
            "Legacy combat-regel. " +
            "Kontrollera actionens targeting och " +
            "execution-inställningar i nya systemet."
        );

        DrawProperty(
            serialized,
            "entersCombatState",
            "Enters Combat",
            "Om användningen ska registrera combat activity."
        );

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(ability);
        }
    }

    private static void DrawProperty(
        SerializedObject serialized,
        string path,
        string label,
        string help)
    {
        SerializedProperty property =
            serialized.FindProperty(path);

        if (property == null)
            return;

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, help),
            true
        );
    }

    // =====================================================
    // ACTION LIST
    // =====================================================

    private void DrawActionList(
        AbilityData ability)
    {
        SerializedObject serialized =
            new SerializedObject(ability);

        serialized.Update();

        SerializedProperty effects =
            serialized.FindProperty("effects");

        if (effects == null)
        {
            EditorGUILayout.HelpBox(
                "AbilityData saknar effects-array.",
                MessageType.Error
            );

            return;
        }

        Section($"Ability Actions ({effects.arraySize})");

        if (effects.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Abilityn saknar actions.\n\n" +
                "Lägg till en befintlig action eller " +
                "skapa en ny nedan.",
                MessageType.Info
            );
        }

        for (int i = 0; i < effects.arraySize; i++)
        {
            DrawActionRow(
                ability,
                effects,
                i
            );
        }

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(ability);

            QueueRefresh(ability);
        }
    }

    // =====================================================
    // ACTION ROW
    // =====================================================

    private void DrawActionRow(
        AbilityData ability,
        SerializedProperty effects,
        int index)
    {
        SerializedProperty element =
            effects.GetArrayElementAtIndex(index);

        AbilityEffect action =
            element.objectReferenceValue
                as AbilityEffect;

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label(
            $"Action {index + 1}",
            EditorStyles.boldLabel
        );

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(index == 0))
        {
            if (GUILayout.Button(
                    "↑",
                    GUILayout.Width(28)))
            {
                int captured = index;

                QueueArrayEdit(
                    ability,
                    "Move Action Up",
                    array =>
                        array.MoveArrayElement(
                            captured,
                            captured - 1
                        )
                );
            }
        }

        using (new EditorGUI.DisabledScope(
            index >= effects.arraySize - 1))
        {
            if (GUILayout.Button(
                    "↓",
                    GUILayout.Width(28)))
            {
                int captured = index;

                QueueArrayEdit(
                    ability,
                    "Move Action Down",
                    array =>
                        array.MoveArrayElement(
                            captured,
                            captured + 1
                        )
                );
            }
        }

        if (GUILayout.Button(
                "Remove",
                GUILayout.Width(65)))
        {
            int captured = index;

            QueueArrayEdit(
                ability,
                "Remove Ability Action",
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
                "Action Asset",
                "Referens till ett AbilityEffect-asset.\n\n" +
                "Detta är en handling som körs av " +
                "AbilityEffectPipeline."
            )
        );

        if (action != null)
        {
            EditorGUILayout.LabelField(
                action.GetType().Name,
                EditorStyles.miniLabel
            );

            EditorGUILayout.LabelField(
                $"Target: {action.TargetMode}  |  " +
                $"Timing: {action.ExecutionTiming}",
                EditorStyles.miniLabel
            );

            if (action is ApplyBuffDefinitionEffect)
            {
                BuffDefinition linkedBuff =
                    RoKContentStudioCatalog
                        .GetLinkedBuff(action);

                if (linkedBuff == null)
                {
                    EditorGUILayout.HelpBox(
                        "Apply Buff saknar BuffDefinition.",
                        MessageType.Warning
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Buff: {linkedBuff.DisplayName}",
                        EditorStyles.miniLabel
                    );
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    expandedAction == action
                        ? "Hide Settings"
                        : "Edit Settings"))
            {
                expandedAction =
                    expandedAction == action
                        ? null
                        : action;
            }

            if (GUILayout.Button(
                    "Open Asset",
                    GUILayout.Width(95)))
            {
                AbilityEffect captured = action;

                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            EditorGUILayout.EndHorizontal();

            if (expandedAction == action)
            {
                EditorGUILayout.Space(6);

                DrawInlineActionEditor(action);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Tom action-referens.",
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();
    }

    // =====================================================
    // INLINE ACTION EDITOR
    // =====================================================

    private void DrawInlineActionEditor(
        AbilityEffect action)
    {
        SerializedObject serialized =
            new SerializedObject(action);

        serialized.Update();

        Section("Action Settings");

        SerializedProperty iterator =
            serialized.GetIterator();

        bool hasNext =
            iterator.NextVisible(true);

        while (hasNext)
        {
            SerializedProperty property =
                iterator.Copy();

            if (property.name != "m_Script" &&
                !IsLegacyBuffRule(property.name))
            {
                DrawActionProperty(
                    action,
                    property
                );
            }

            hasNext =
                iterator.NextVisible(false);
        }

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(action);
        }

        // =================================================
        // LEGACY RULES
        // =================================================

        showLegacyRules =
            EditorGUILayout.Foldout(
                showLegacyRules,
                "Advanced: Legacy Buff Rules",
                true
            );

        if (showLegacyRules)
        {
            EditorGUILayout.HelpBox(
                "Dessa fält tillhör äldre AbilityEffect-" +
                "baserade buffs.\n\n" +
                "För våra nya BuffDefinitions konfigureras " +
                "duration, stacks och death rules på " +
                "BuffDefinition i stället.\n\n" +
                "Ändra inte legacy-fälten om du inte " +
                "vet att den aktuella actionen använder dem.",
                MessageType.Info
            );

            SerializedObject legacySerialized =
                new SerializedObject(action);

            foreach (string name in new[]
            {
                "stackable",
                "maxStacks",
                "refreshDurationOnStack",
                "removeOnDeath",
                "removeOnEncounterReset"
            })
            {
                SerializedProperty property =
                    legacySerialized.FindProperty(name);

                if (property != null)
                {
                    EditorGUILayout.PropertyField(
                        property,
                        true
                    );
                }
            }

            if (legacySerialized.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(action);
            }
        }
    }


    // =====================================================
    // LEGACY BUFF RULE FILTER
    // =====================================================

    private static bool IsLegacyBuffRule(
        string propertyName)
    {
        return propertyName switch
        {
            "stackable" => true,
            "maxStacks" => true,
            "refreshDurationOnStack" => true,
            "removeOnDeath" => true,
            "removeOnEncounterReset" => true,

            _ => false
        };
    }


    private void DrawActionProperty(
        AbilityEffect action,
        SerializedProperty property)
    {
        string help =
            GetPropertyHelp(
                action,
                property.name
            );

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(
                property.displayName,
                help
            ),
            true
        );

        if (GUILayout.Button(
                "?",
                GUILayout.Width(26)))
        {
            selectedHelp = help;
        }

        EditorGUILayout.EndHorizontal();
    }

    // =====================================================
    // ACTION PICKER
    // =====================================================

    private void DrawActionPicker(
        AbilityData ability)
    {
        Section("Add Ability Action");

        selectedPickerMode =
            GUILayout.Toolbar(
                selectedPickerMode,
                new[]
                {
                    "Reuse Existing",
                    "Create New"
                }
            );

        EditorGUILayout.Space(6);

        if (selectedPickerMode == 0)
        {
            DrawExistingPicker(ability);
        }
        else
        {
            DrawNewActionCreator(ability);
        }
    }

    private int selectedPickerMode;

    // =====================================================
    // REUSE EXISTING ACTION
    // =====================================================

    private void DrawExistingPicker(
        AbilityData ability)
    {
        string nextSearch =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Search",
                    "Söker efter befintliga " +
                    "AbilityEffect-assets i projektet."
                ),
                search
            );

        if (nextSearch != search)
        {
            search = nextSearch;
            InvalidateCache();
        }

        if (cachedActions == null ||
            cachedSearch != search)
        {
            RebuildCache();
        }

        if (cachedActions.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Inga matchande actions hittades.",
                MessageType.Info
            );

            return;
        }

        searchScroll =
            EditorGUILayout.BeginScrollView(
                searchScroll,
                GUILayout.MaxHeight(260)
            );

        foreach (AbilityEffect action
                 in cachedActions)
        {
            if (action == null)
                continue;

            EditorGUILayout.BeginHorizontal(
                EditorStyles.helpBox
            );

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(
                action.name,
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                action.GetType().Name,
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndVertical();

            bool alreadyLinked =
                ContainsAction(ability, action);

            using (new EditorGUI.DisabledScope(
                alreadyLinked))
            {
                if (GUILayout.Button(
                    "Add",
                    GUILayout.Width(50)))
                {
                    AbilityEffect captured =
                        action;

                    QueueArrayEdit(
                        ability,
                        "Add Existing Action",
                        array =>
                            AddElement(
                                array,
                                captured
                            )
                    );
                }
            }

            if (GUILayout.Button(
                "Open",
                GUILayout.Width(55)))
            {
                AbilityEffect captured =
                    action;

                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "Återanvändning länkar ett befintligt " +
            "AbilityEffect-asset.\n\n" +
            "Det skapas ingen kopia. Om asseten " +
            "redigeras påverkas alla abilities " +
            "som använder samma action.",
            MessageType.Info
        );
    }

    // =====================================================
    // ACTION CACHE
    // =====================================================

    private void RebuildCache()
    {
        cachedActions = new();

        List<ScriptableObject> assets =
            RoKContentStudioCatalog.Find(
                RoKContentCategory.AbilityActions,
                search
            );

        foreach (ScriptableObject asset in assets)
        {
            if (asset is AbilityEffect action)
            {
                cachedActions.Add(action);
            }
        }

        cachedSearch = search;
    }

    // =====================================================
    // CREATE NEW ACTION
    // =====================================================

    private void DrawNewActionCreator(
        AbilityData ability)
    {
        IReadOnlyList<RoKActionTemplate> templates =
            RoKContentStudioActionFactory.Templates;

        string[] names =
            new string[templates.Count];

        for (int i = 0; i < templates.Count; i++)
        {
            names[i] = templates[i].Name;
        }

        selectedTemplateIndex =
            Mathf.Clamp(
                selectedTemplateIndex,
                0,
                templates.Count - 1
            );

        selectedTemplateIndex =
            EditorGUILayout.Popup(
                new GUIContent(
                    "Action Type",
                    "Välj vilken action som ska skapas."
                ),
                selectedTemplateIndex,
                names
            );

        RoKActionTemplate template =
            templates[selectedTemplateIndex];

        EditorGUILayout.HelpBox(
            template.Description,
            MessageType.Info
        );

        if (template.ActionType ==
            typeof(ApplyBuffDefinitionEffect))
        {
            selectedBuff =
                (BuffDefinition)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Buff Definition",
                        "Buffen som actionen ska applicera.\n\n" +
                        "Lämna tomt för att länka senare."
                    ),
                    selectedBuff,
                    typeof(BuffDefinition),
                    false
                );
        }

        EditorGUILayout.Space(6);

        if (GUILayout.Button(
                "Create & Attach Action",
                GUILayout.Height(32)))
        {
            RoKActionTemplate capturedTemplate =
                template;

            BuffDefinition capturedBuff =
                selectedBuff;

            EditorApplication.delayCall += () =>
            {
                if (ability == null)
                    return;

                try
                {
                    AbilityEffect created =
                        RoKContentStudioActionFactory
                            .CreateAndAttach(
                                ability,
                                capturedTemplate,
                                capturedBuff
                            );

                    expandedAction = created;

                    InvalidateCache();

                    refreshLibrary?.Invoke();

                    openAsset?.Invoke(ability);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    EditorUtility.DisplayDialog(
                        "Action creation failed",
                        exception.Message,
                        "OK"
                    );
                }
            };
        }

        EditorGUILayout.HelpBox(
            "Den nya actionen sparas som ett separat " +
            "ScriptableObject och läggs automatiskt " +
            "till i AbilityData.effects.\n\n" +
            "Abilityns targeting och cooldown ändras inte. " +
            "Kontrollera dessa inställningar efteråt.",
            MessageType.Info
        );
    }

    // =====================================================
    // DEFERRED ARRAY EDITING
    // =====================================================

    private void QueueArrayEdit(
        AbilityData ability,
        string operation,
        Action<SerializedProperty> edit)
    {
        EditorApplication.delayCall += () =>
        {
            if (ability == null)
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SerializedObject serialized =
                new SerializedObject(ability);

            SerializedProperty array =
                serialized.FindProperty("effects");

            if (array == null)
                return;

            Undo.RecordObject(
                ability,
                operation
            );

            edit(array);

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(ability);

            AssetDatabase.SaveAssets();

            openAsset?.Invoke(ability);
        };
    }

    private static void AddElement(
        SerializedProperty array,
        AbilityEffect action)
    {
        int index = array.arraySize;

        array.InsertArrayElementAtIndex(index);

        array.GetArrayElementAtIndex(index)
            .objectReferenceValue = action;
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

        array.DeleteArrayElementAtIndex(index);

        if (array.arraySize == previousSize)
        {
            array.DeleteArrayElementAtIndex(index);
        }
    }

    private static bool ContainsAction(
        AbilityData ability,
        AbilityEffect action)
    {
        if (ability?.effects == null ||
            action == null)
        {
            return false;
        }

        foreach (AbilityEffect existing
                 in ability.effects)
        {
            if (existing == action)
                return true;
        }

        return false;
    }

    private void QueueRefresh(
        AbilityData ability)
    {
        EditorApplication.delayCall += () =>
        {
            if (ability != null)
            {
                openAsset?.Invoke(ability);
            }
        };
    }

    // =====================================================
    // HELP SYSTEM
    // =====================================================

    private static string GetPropertyHelp(
        AbilityEffect action,
        string propertyName)
    {
        switch (propertyName)
        {
            case "icon":
                return
                    "Valfri ikon kopplad till actionen.\n\n" +
                    "AbilityData har också en egen ikon " +
                    "som används av actionbaren.";

            case "executionTiming":
                return
                    "Immediate: Körs när actionen exekveras.\n\n" +
                    "Deferred: Körs inte av den vanliga " +
                    "omedelbara pipelinen. Den måste " +
                    "exekveras senare av exempelvis " +
                    "en projektil eller trap.\n\n" +
                    "Fel Timing kan göra att en effekt " +
                    "aldrig körs.";

            case "targetMode":
                return
                    "EachAffectedTarget: Körs på varje " +
                    "giltigt target som abilityn påverkar.\n\n" +
                    "PrimaryTarget: Körs endast på " +
                    "det primära målet.\n\n" +
                    "Caster: Körs på användaren själv.\n\n" +
                    "OncePerAction: Körs en gång " +
                    "utan att kräva ett CharacterStats-target.\n\n" +
                    "Viktigt: Action Target Mode är skilt " +
                    "från AbilityData Targeting Mode.";

            case "requiresCharacterTarget":
                return
                    "När detta är aktivt måste effekten " +
                    "ha ett CharacterStats-target.\n\n" +
                    "Vanligtvis On för damage, healing " +
                    "och buffs.\n\n" +
                    "Vanligtvis Off för fristående " +
                    "projektil- och VFX-spawning.";

            case "flatDamage":
                return
                    "Grundskada före damage variation, " +
                    "critical hits, armor och resistances.\n\n" +
                    "0 innebär ingen flat damage.\n\n" +
                    "Skadan kan fortfarande komma från scaling.";

            case "scaling":
                return
                    "Modifierar grundskadan baserat på " +
                    "caster-stats.\n\n" +
                    "Exempel: Strength eller annan stat.\n\n" +
                    "Varje scaling-entry bidrar till " +
                    "damage före defensiv beräkning.";

            case "damageType":
                return
                    "Physical, Fire, Frost, Nature, Curse " +
                    "eller Arcane.\n\n" +
                    "DamageType avgör vilka resistances " +
                    "som påverkar attacken.\n\n" +
                    "Det är oberoende av CreatureType.";

            case "dealsRawDamage":
                return
                    "Raw Damage kringgår den vanliga " +
                    "damage-beräkningen.\n\n" +
                    "Använd sparsamt, eftersom armor, " +
                    "resistances och vissa effektbonusar " +
                    "inte påverkar denna väg.";

            case "strengthMultiplier":
                return
                    "Hur mycket av caster-Strength som " +
                    "bidrar till ScaledDamageEffect.\n\n" +
                    "1.5 innebär 150% av Strength.";

            case "weaponDamageMultiplier":
                return
                    "Multiplikator för WeaponDamage.\n\n" +
                    "1.0 innebär 100% WeaponDamage.";

            case "flatHealing":
                return
                    "Fast healing före eventuella " +
                    "scaling- och charge-modifierare.";

            case "targetMaximumHealthPercentage":
                return
                    "Healing baserat på målets MaxHP.\n\n" +
                    "0.10 innebär 10% av MaxHP.";

            case "buffDefinition":
                return
                    "BuffDefinition som appliceras.\n\n" +
                    "Buffen äger duration, stacks och " +
                    "sina återanvändbara EffectDefinitions.\n\n" +
                    "Kontrollera även actionens Target Mode.";

            case "prefab":
            case "projectilePrefab":
                return
                    "Prefab som ska instansieras.\n\n" +
                    "Om referensen saknas kan ingen " +
                    "VFX eller projektil skapas.";

            case "homing":
                return
                    "Om projektilen kontinuerligt " +
                    "uppdaterar sin destination.\n\n" +
                    "Fungerar endast med kompatibelt " +
                    "projectile hit mode.";

            case "speed":
                return
                    "Projektilens rörelsehastighet " +
                    "i world units per sekund.";

            case "lifetime":
                return
                    "Hur länge en instans får leva " +
                    "innan den tas bort.";

            default:
                return
                    $"{propertyName}\n\n" +
                    $"Fält på {action.GetType().Name}.\n\n" +
                    "Ändra det för att konfigurera " +
                    "denna action. Se även eventuell " +
                    "fält-tooltip i Unity Inspector.";
        }
    }

    private void DrawHelp()
    {
        Section("Action Composer Help");

        EditorGUILayout.HelpBox(
            selectedHelp,
            MessageType.Info
        );

        if (GUILayout.Button(
                "Ability Targeting vs Action Targeting"))
        {
            selectedHelp =
                "Ability Targeting avgör vilka mål " +
                "som väljs ut av abilityn.\n\n" +
                "Action Targeting avgör vem som får " +
                "en specifik effekt.\n\n" +
                "Exempel:\n" +
                "Heroic Strike har Hostile/Cone.\n" +
                "Damage körs på EachAffectedTarget.\n" +
                "Self Buff körs på Caster.\n\n" +
                "Båda kan finnas i samma ability.";
        }

        if (GUILayout.Button(
                "Immediate vs Deferred"))
        {
            selectedHelp =
                "Immediate körs vid action-execution.\n\n" +
                "Deferred används när något annat " +
                "ska exekvera effekten senare, " +
                "exempelvis en projektil vid impact.\n\n" +
                "En Deferred-effekt utan en sådan " +
                "exekveringsväg gör ingenting.";
        }

        if (GUILayout.Button(
                "Shared Action Assets"))
        {
            selectedHelp =
                "Samma AbilityEffect-asset kan användas " +
                "av flera abilities.\n\n" +
                "Om du redigerar det delade assetet " +
                "påverkas alla abilities som refererar " +
                "till det.\n\n" +
                "Create New skapar i stället ett " +
                "separat asset för den aktuella abilityn.";
        }
    }

    private static void Section(string title)
    {
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}

#endif

