
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoKContentStudioRecipePanel
{
    // =====================================================
    // STATE
    // =====================================================

    private readonly RoKAbilityRecipeRequest request =
        new();


    private bool targetLayersInitialized;


    private RoKSelfBuffSource selfBuffSource =
        RoKSelfBuffSource.Existing;

    private readonly RoKContentStudioInlineBuffComposer
        inlineBuffComposer = new();


    private AbilityData targetingReference;

    private string selectedHelp =
        "Klicka på frågetecknet bredvid ett fält " +
        "för att läsa mer.";

    // =====================================================
    // PUBLIC GUI
    // =====================================================

    public void Draw(
        Action<ScriptableObject> openAsset,
        Action refreshLibrary,
        bool twoColumns = false)
    {

        // =====================================================
        // INITIALIZE DEFAULT TARGET LAYERS
        // =====================================================
        //
        // Unity APIs must not be called from field
        // initializers or constructors owned by
        // EditorWindow.
        //
        // Initialize once, when the Editor UI is active.
        // =====================================================

        if (!targetLayersInitialized)
        {
            targetLayersInitialized = true;

            request.TargetLayers =
                LayerMask.GetMask(
                    "Hitbox",
                    "hitbox"
                );
        }


        EditorGUILayout.HelpBox(
            "Build a complete AbilityData with " +
            "automatically connected AbilityEffects.\n\n" +
            "The recipe does not change existing " +
            "gameplay systems or shared assets.",
            MessageType.Info
        );

        RoKForgeFormLayout.DrawColumns(
        twoColumns,
        DrawRecipeIdentityAndDamage,
        DrawTargetingExecutionAndBuff);

            DrawRecipeResult(openAsset, refreshLibrary);
    }

    private void DrawRecipeIdentityAndDamage()
    {
        Section("Recipe");

        Field(
            () =>
            {
                request.Recipe =
                    (RoKAbilityRecipeKind)
                    EditorGUILayout.EnumPopup(
                        new GUIContent(
                            "Recipe",
                            "Välj vilka actions abilityn " +
                            "ska innehålla."
                        ),
                        request.Recipe
                    );
            },
            "Direct Damage:\n" +
            "Skapar en AbilityData och DamageEffect.\n\n" +
            "Direct Damage With Self Buff:\n" +
            "Skapar även en ApplyBuffDefinitionEffect " +
            "som applicerar en befintlig buff på castern."
        );

        Section("Identity");

        Field(
            () =>
            {
                request.Name =
                    EditorGUILayout.TextField(
                        "Ability Name",
                        request.Name
                    );
            },
            "Namnet på den nya abilityn.\n\n" +
            "Content Studio skapar automatiskt " +
            "en unik asset-sökväg och skriver aldrig " +
            "över en befintlig ability."
        );


        Field(
            () =>
            {
                request.Icon =
                    (Sprite)EditorGUILayout.ObjectField(
                        "Icon",
                        request.Icon,
                        typeof(Sprite),
                        false
                    );
            },
            "Ikonen som används av abilityn i " +
            "Spellbook, Actionbar och eventuella " +
            "buffs som ärver abilityns ikon."
        );


        Field(
            () =>
            {
                request.Description =
                    EditorGUILayout.TextField(
                        "Description",
                        request.Description
                    );
            },
            "Beskrivning som visas i abilityns tooltip.\n\n" +
            "Exempel:\n" +
            "Hurls a burning strike at an enemy " +
            "and grants temporary fire protection."
        );

        Field(
            () =>
            {
                request.AbilityType =
                    (AbilityType)
                    EditorGUILayout.EnumPopup(
                        "Ability Type",
                        request.AbilityType
                    );
            },
            "Klassificerar abilityn som exempelvis " +
            "Melee, Spell eller Ranged.\n\n" +
            "Detta ändrar inte automatiskt " +
            "dess targeting eller damage type."
        );

        Section("Damage");

        Field(
            () =>
            {
                request.DamageType =
                    (DamageType)
                    EditorGUILayout.EnumPopup(
                        "Damage Type",
                        request.DamageType
                    );
            },
            "Vilken sorts skada attacken orsakar.\n\n" +
            "Physical påverkas av fysisk armor.\n" +
            "Fire påverkas av Fire Resistance.\n" +
            "Curse påverkas av Curse Resistance.\n\n" +
            "Detta är oberoende av målets CreatureType."
        );

        Field(
            () =>
            {
                request.FlatDamage =
                    EditorGUILayout.IntField(
                        "Flat Damage",
                        request.FlatDamage
                    );
            },
            "Grundskada innan variation, kritiska " +
            "träffar och försvarsmekanismer.\n\n" +
            "Exempel: 50 innebär 50 grundskada, " +
            "inte garanterat 50 slutskada."
        );

        Field(
            () =>
            {
                request.CanMiss =
                    EditorGUILayout.Toggle(
                        "Can Miss",
                        request.CanMiss
                    );
            },
            "När aktivt använder abilityn det " +
            "befintliga hit/dodge-systemet.\n\n" +
            "När avstängt träffar den automatiskt " +
            "ett redan giltigt target."
        );

        Field(
            () =>
            {
                request.CanCrit =
                    EditorGUILayout.Toggle(
                        "Can Crit",
                        request.CanCrit
                    );
            },
            "Bestämmer om skadan kan bli critical.\n\n" +
            "Den faktiska crit-chansen och " +
            "multiplikatorn hämtas från casterns stats."
        );

    }

    private void DrawTargetingExecutionAndBuff()
    {
        Section("Targeting");

        Field(
            () => request.Range = EditorGUILayout.FloatField("Range", request.Range),
            "Maximalt avstånd till ett giltigt target.\n\n" +
            "Recipe Builder skapar SingleTarget / Hostile.\n\n" +
            "Exempel:\n" +
            "2 för en närstridsattack.\n" +
            "6 för en kortdistans-spell.");

        Field(
            () =>
            {
                targetingReference = (AbilityData)EditorGUILayout.ObjectField(
                    "Copy Layers From",
                    targetingReference,
                    typeof(AbilityData),
                    false);
            },
            "Valfri hjälp för target layers.\n\n" +
            "Välj en befintlig fungerande ability, " +
            "exempelvis Heroic Strike eller TestFireAbility.\n\n" +
            "Vi kopierar endast dess TargetLayers, " +
            "inte cooldown, targeting mode eller effects.");

        if (targetingReference != null && GUILayout.Button("Copy Target Layers"))
        {
            request.TargetLayers = targetingReference.TargetingSettings.TargetLayers.value;
        }

        Field(
            () => request.TargetLayers = DrawLayerMask(request.TargetLayers),
            "Vilka Unity-lager som får innehålla targetbara fiender.\n\n" +
            "Detta måste matcha lagren i din scen.\n\n" +
            "Välj rätt lager eller kopiera från en redan fungerande offensiv ability.\n\n" +
            "Om inga lager väljs kan abilityn inte skapas.");

        Section("Execution");

        Field(
            () => request.Cooldown = EditorGUILayout.FloatField("Cooldown", request.Cooldown),
            "Cooldown anges i sekunder.\n\n" +
            "Den här cooldownen styr hur ofta abilityn får användas.\n\n" +
            "Om abilityn även applicerar en buff har buffen sin egen Duration.");

        if (request.Recipe != RoKAbilityRecipeKind.DirectDamageWithSelfBuff)
            return;

        Section("Self Buff");

        selfBuffSource = (RoKSelfBuffSource)EditorGUILayout.EnumPopup(
            new GUIContent(
                "Buff Source",
                "Existing återanvänder en buff.\n\n" +
                "CreateNew bygger en ny buff direkt i denna guide."),
            selfBuffSource);

        EditorGUILayout.Space(6);

        if (selfBuffSource == RoKSelfBuffSource.Existing)
        {
            Field(
                () =>
                {
                    request.SelfBuff = (BuffDefinition)EditorGUILayout.ObjectField(
                        "Buff Definition",
                        request.SelfBuff,
                        typeof(BuffDefinition),
                        false);
                },
                "Välj en befintlig BuffDefinition.\n\n" +
                "Den återanvänds utan duplicering.");

            if (request.SelfBuff != null)
            {
                int effectCount = request.SelfBuff.Effects?.Count ?? 0;

                EditorGUILayout.HelpBox(
                    $"Buff: {request.SelfBuff.DisplayName}\n" +
                    $"Duration: {request.SelfBuff.Duration:0.#}s\n" +
                    $"Effects: {effectCount}\n\n" +
                    "Target: Caster",
                    MessageType.Info);
            }
        }
        else
        {
            inlineBuffComposer.Draw();
        }
    }

    private void DrawRecipeResult(
    Action<ScriptableObject> openAsset,
    Action refreshLibrary)
    {
        bool createNewBuff =
            request.Recipe == RoKAbilityRecipeKind.DirectDamageWithSelfBuff &&
            selfBuffSource == RoKSelfBuffSource.CreateNew;

        RoKBuffRecipeDraft buffDraft =
            createNewBuff ? inlineBuffComposer.Draft : null;

        Section("Recipe Preview");
        EditorGUILayout.HelpBox(
            BuildRecipePreview(createNewBuff, buffDraft),
            MessageType.None);

        bool valid = RoKContentStudioSmartRecipeFactory.Validate(
            request,
            createNewBuff,
            buffDraft,
            out string error);

        if (!valid)
            EditorGUILayout.HelpBox(error, MessageType.Warning);

        using (new EditorGUI.DisabledScope(
            !valid || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Create Complete Ability", GUILayout.Height(36)))
                QueueCreation(openAsset, refreshLibrary);
        }

        EditorGUILayout.Space(12);

        Section("Detailed Help");
        EditorGUILayout.HelpBox(selectedHelp, MessageType.Info);
    }

    private string BuildRecipePreview(
        bool createNewBuff,
        RoKBuffRecipeDraft buffDraft)
    {
        List<string> lines = new()
    {
        "Create AbilityData",
        "  Create DamageEffect"
    };

        if (request.Recipe != RoKAbilityRecipeKind.DirectDamageWithSelfBuff)
            return string.Join("\n", lines);

        lines.Add("  Create ApplyBuffDefinitionEffect");

        if (!createNewBuff)
        {
            lines.Add(request.SelfBuff != null
                ? $"    Link existing buff: {request.SelfBuff.DisplayName}"
                : "    Select an existing buff");

            return string.Join("\n", lines);
        }

        if (buffDraft == null)
        {
            lines.Add("    Configure the new buff");
            return string.Join("\n", lines);
        }

        string buffName = string.IsNullOrWhiteSpace(buffDraft.Name)
            ? "(unnamed)"
            : buffDraft.Name;

        lines.Add($"    Create buff: {buffName}");

        foreach (RoKBuffEffectDraft effect in buffDraft.Effects)
        {
            if (effect == null)
            {
                lines.Add("      Configure this effect");
                continue;
            }

            if (effect.Source == RoKBuffEffectSource.Existing)
            {
                lines.Add(effect.ExistingEffect != null
                    ? $"      Link existing effect: {effect.ExistingEffect.name}"
                    : "      Select an existing effect");
            }
            else
            {
                string effectName = string.IsNullOrWhiteSpace(effect.Name)
                    ? "(unnamed)"
                    : effect.Name;

                lines.Add($"      Create effect: {effectName}");
            }
        }

        return string.Join("\n", lines);
    }

    // =====================================================
    // CREATE DEFERRED
    // =====================================================


    private void QueueCreation(
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        // =================================================
        // SNAPSHOT ABILITY SETTINGS
        // =================================================

        RoKAbilityRecipeRequest captured =
            new RoKAbilityRecipeRequest
            {

                Icon = request.Icon,

                Name = request.Name,
                Description = request.Description,

                Recipe = request.Recipe,
                AbilityType = request.AbilityType,

                DamageType = request.DamageType,
                FlatDamage = request.FlatDamage,

                Cooldown = request.Cooldown,
                Range = request.Range,

                TargetLayers = request.TargetLayers,

                CanMiss = request.CanMiss,
                CanCrit = request.CanCrit,

                SelfBuff = request.SelfBuff
            };

        // =================================================
        // SNAPSHOT BUFF SETTINGS
        // =================================================

        bool createNewBuff =
            captured.Recipe ==
                RoKAbilityRecipeKind
                    .DirectDamageWithSelfBuff &&
            selfBuffSource ==
                RoKSelfBuffSource.CreateNew;

        RoKBuffRecipeDraft capturedBuff =
            createNewBuff
                ? inlineBuffComposer.Draft.Clone()
                : null;

        // =================================================
        // CREATE DEFERRED
        // =================================================

        EditorApplication.delayCall += () =>
        {
            try
            {
                AbilityData ability =
                    RoKContentStudioSmartRecipeFactory
                        .Create(
                            captured,
                            createNewBuff,
                            capturedBuff
                        );

                refreshLibrary?.Invoke();

                openAsset?.Invoke(ability);

                Selection.activeObject = ability;

                EditorGUIUtility.PingObject(ability);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Recipe Creation Failed",
                    exception.Message,
                    "OK"
                );
            }
        };
    }


    // =====================================================
    // LAYER MASK EDITOR
    // =====================================================

    private static int DrawLayerMask(int currentMask)
    {
        List<string> names = new();
        List<int> layerIndices = new();

        for (int layer = 0; layer < 32; layer++)
        {
            string name =
                LayerMask.LayerToName(layer);

            if (string.IsNullOrEmpty(name))
                continue;

            names.Add(name);
            layerIndices.Add(layer);
        }

        int compressedMask = 0;

        for (int i = 0; i < layerIndices.Count; i++)
        {
            int bit = 1 << layerIndices[i];

            if ((currentMask & bit) != 0)
            {
                compressedMask |= 1 << i;
            }
        }

        int selected =
            EditorGUILayout.MaskField(
                "Target Layers",
                compressedMask,
                names.ToArray()
            );

        int result = 0;

        for (int i = 0; i < layerIndices.Count; i++)
        {
            if ((selected & (1 << i)) != 0)
            {
                result |= 1 << layerIndices[i];
            }
        }

        return result;
    }

    // =====================================================
    // FIELD HELP
    // =====================================================

    private void Field(
        Action draw,
        string help)
    {
        EditorGUILayout.BeginHorizontal();

        draw?.Invoke();

        if (GUILayout.Button(
                new GUIContent(
                    "?",
                    "Visa utförlig hjälp för detta fält."
                ),
                GUILayout.Width(26)))
        {
            selectedHelp = help;
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void Section(string title)
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}

#endif

