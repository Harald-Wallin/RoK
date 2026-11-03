
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoKContentStudioWindow :
    EditorWindow
{
    // =====================================================
    // STATE
    // =====================================================

    private RoKContentCategory category =
        RoKContentCategory.All;

    private RoKContentRecipe recipe =
        RoKContentRecipe.SelfBuffAbility;

    private RoKPassiveEffectKind effectKind =
        RoKPassiveEffectKind.StatModifier;

    private ScriptableObject selectedAsset;

    private EffectDefinition existingEffect;

    private string search = "";
    private string contentName = "New Ability";

    private float effectValue = 0.10f;
    private float duration = 30f;
    private float cooldown = 5f;

    private StatType stat = StatType.MaxHP;

    private ModifierType modifierType =
        ModifierType.Percent;

    private DamageType damageType =
        DamageType.Fire;

    private CreatureType creatureTypes =
        CreatureType.Beast;

    // =====================================================
    // UI REFERENCES
    // =====================================================

    private ScrollView libraryView;
    private ScrollView workspace;
    private Label helpLabel;

    private readonly RoKContentStudioEffectComposer
        effectComposer = new();


    private readonly RoKContentStudioActionComposer
        actionComposer = new();


    private readonly RoKContentStudioRecipePanel
        recipePanel = new();


    private readonly RoKContentStudioDiagnosticsPanel
        diagnosticsPanel = new();


    private readonly RoKContentStudioEffectRegistryPanel
        effectRegistryPanel = new();




    // =====================================================
    // MENU
    // =====================================================

    [MenuItem("RöK/Content Studio")]
    public static void Open()
    {
        var window =
            GetWindow<RoKContentStudioWindow>();

        window.titleContent =
            new GUIContent("RöK Content Studio");

        window.minSize =
            new Vector2(850f, 560f);

        window.Show();
    }

    // =====================================================
    // UI TOOLKIT
    // =====================================================

    public void CreateGUI()
    {
        VisualElement root =
            rootVisualElement;

        root.Clear();

        root.style.flexDirection =
            FlexDirection.Column;

        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        // =================================================
        // HEADER
        // =================================================

        VisualElement header =
            new VisualElement();

        header.style.flexDirection =
            FlexDirection.Row;

        header.style.alignItems =
            Align.Center;

        header.style.marginBottom = 10;

        Label title =
            new Label("RöK Content Studio");

        title.style.fontSize = 18;
        title.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        title.style.flexGrow = 1;

        header.Add(title);

        Button newButton =
            new Button(DrawCreator)
            {
                text = "+ New Content"
            };

        header.Add(newButton);


        Button recipeButton =
            new Button(DrawAbilityRecipes)
            {
                text = "Ability Recipes"
            };

        recipeButton.tooltip =
            "Create a complete ability with " +
            "automatically connected actions.";

        header.Add(recipeButton);


        Button refreshButton =
            new Button(RefreshLibrary)
            {
                text = "Refresh Library"
            };

        header.Add(refreshButton);

        root.Add(header);


        Button auditButton =
            new Button(DrawContentAudit)
            {
                text = "Content Audit"
            };

        auditButton.tooltip =
            "Validate content and inspect asset dependencies.";

        header.Add(auditButton);


        Button registryButton =
            new Button(DrawEffectRegistry)
            {
                text = "Effect Registry"
            };

        registryButton.tooltip =
            "Discover and create any registered RöK effect.";

        header.Add(registryButton);



        // =================================================
        // MAIN LAYOUT
        // =================================================

        VisualElement main =
            new VisualElement();

        main.style.flexDirection =
            FlexDirection.Row;

        main.style.flexGrow = 1;

        main.style.minHeight = 0;

        root.Add(main);

        // =================================================
        // LIBRARY COLUMN
        // =================================================

        VisualElement sidebar =
            new VisualElement();

        sidebar.style.width = 260;
        sidebar.style.marginRight = 12;

        sidebar.style.flexShrink = 0;

        main.Add(sidebar);

        EnumField categoryField =
            new EnumField("Category", category);

        categoryField.tooltip =
            "Choose which content type appears " +
            "in the library.\n\n" +
            "This only filters the list. " +
            "It never modifies assets.";

        categoryField.RegisterValueChangedCallback(
            evt =>
            {
                category =
                    (RoKContentCategory)evt.newValue;

                RefreshLibrary();
            }
        );

        sidebar.Add(categoryField);

        TextField searchField =
            new TextField("Search");

        searchField.tooltip =
            "Search asset names across your project.\n" +
            "The search does not require assets " +
            "to be stored in a particular folder.";

        searchField.RegisterValueChangedCallback(
            evt =>
            {
                search = evt.newValue;
                RefreshLibrary();
            }
        );

        sidebar.Add(searchField);

        libraryView =
            new ScrollView();

        libraryView.style.flexGrow = 1;

        sidebar.Add(libraryView);

        // =================================================
        // WORKSPACE COLUMN
        // =================================================

        workspace =
            new ScrollView();

        workspace.style.flexGrow = 1;
        workspace.style.minWidth = 0;

        main.Add(workspace);

        // =================================================
        // HELP PANEL
        // =================================================

        helpLabel =
            new Label(
                "Välj ett asset eller klicka på " +
                "New Content för att börja."
            );

        helpLabel.style.whiteSpace =
            WhiteSpace.Normal;

        helpLabel.style.marginTop = 10;
        helpLabel.style.paddingTop = 8;
        helpLabel.style.paddingBottom = 8;

        root.Add(helpLabel);

        RefreshLibrary();

        DrawWelcome();
    }

    // =====================================================
    // PROJECT CHANGES
    // =====================================================

    private void OnProjectChange()
    {
        if (libraryView != null)
        {
            RefreshLibrary();
        }
    }

    // =====================================================
    // LIBRARY
    // =====================================================

    private void RefreshLibrary()
    {
        if (libraryView == null)
            return;

        libraryView.Clear();

        List<ScriptableObject> assets =
            RoKContentStudioCatalog.Find(
                category,
                search
            );

        foreach (ScriptableObject asset in assets)
        {
            ScriptableObject captured = asset;

            Button button =
                new Button(
                    () => ShowAsset(captured)
                )
                {
                    text =
                        $"{asset.name}  " +
                        $"[{asset.GetType().Name}]"
                };

            button.tooltip =
                AssetDatabase.GetAssetPath(asset);

            button.style.marginBottom = 3;

            libraryView.Add(button);
        }

        if (assets.Count == 0)
        {
            libraryView.Add(
                new Label("No matching content.")
            );
        }
    }

    // =====================================================
    // WELCOME
    // =====================================================

    private void DrawWelcome()
    {
        workspace.Clear();

        workspace.Add(
            Heading("Welcome to RöK Content Studio")
        );

        workspace.Add(
            Paragraph(
                "Select an existing asset from the " +
                "library to inspect or edit it.\n\n" +
                "Choose New Content to create a " +
                "complete ability, buff or reusable effect."
            )
        );
    }

    // =====================================================
    // SELECTED ASSET
    // =====================================================

    private void ShowAsset(
        ScriptableObject asset)
    {
        if (asset == null)
            return;

        selectedAsset = asset;

        workspace.Clear();

        workspace.Add(
            Heading(asset.name)
        );

        workspace.Add(
            Paragraph(
                asset.GetType().Name + "\n" +
                AssetDatabase.GetAssetPath(asset)
            )
        );

        Button ping =
            new Button(
                () =>
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            )
            {
                text = "Show in Project"
            };

        workspace.Add(ping);


        // =================================================
        // CONTENT-SPECIFIC EDITORS
        // =================================================

        if (asset is BuffDefinition buff)
        {
            IMGUIContainer composer =
                new IMGUIContainer(
                    () =>
                    {
                        if (buff == null)
                            return;

                        effectComposer.Draw(
                            buff,
                            ShowAsset,
                            RefreshLibrary
                        );
                    }
                );

            composer.style.marginTop = 10;

            workspace.Add(composer);
        }
        else if (asset is AbilityData ability)
        {
            IMGUIContainer composer =
                new IMGUIContainer(
                    () =>
                    {
                        if (ability == null)
                            return;

                        actionComposer.Draw(
                            ability,
                            ShowAsset,
                            RefreshLibrary
                        );
                    }
                );

            composer.style.marginTop = 10;

            workspace.Add(composer);
        }
        else
        {
            InspectorElement inspector =
                new InspectorElement(asset);

            inspector.style.marginTop = 10;

            workspace.Add(inspector);
        }


        // =================================================
        // DEPENDENCIES
        // =================================================

        workspace.Add(
            Heading("Linked Assets")
        );

        List<ScriptableObject> links =
            RoKContentStudioCatalog.GetLinks(
                asset
            );

        if (links.Count == 0)
        {
            workspace.Add(
                Paragraph("No direct links found.")
            );
        }

        foreach (ScriptableObject linked in links)
        {
            ScriptableObject captured = linked;

            workspace.Add(
                new Button(
                    () => ShowAsset(captured)
                )
                {
                    text =
                        $"{linked.name} " +
                        $"[{linked.GetType().Name}]"
                }
            );
        }

        // =================================================
        // VALIDATION
        // =================================================

        workspace.Add(
            Heading("Validation")
        );

        Button validate =
            new Button(
                () => ShowAsset(asset)
            )
            {
                text = "Refresh Validation"
            };

        workspace.Add(validate);

        List<string> issues =
            RoKContentStudioCatalog.Validate(
                asset
            );

        if (issues.Count == 0)
        {
            workspace.Add(
                Paragraph(
                    "No configuration issues detected."
                )
            );
        }
        else
        {
            foreach (string issue in issues)
            {
                workspace.Add(
                    Paragraph(issue)
                );
            }
        }

        // =================================================
        // HELP
        // =================================================

        workspace.Add(
            Heading("Understanding this Asset")
        );

        workspace.Add(
            Paragraph(GetAssetHelp(asset))
        );

        helpLabel.text =
            "Inspector fields can be edited here. " +
            "Use Validation after changing references.";
    }

    // =====================================================
    // GUIDED CREATOR
    // =====================================================

    // =====================================================
    // ABILITY RECIPE BUILDER
    // =====================================================

    private void DrawAbilityRecipes()
    {
        workspace.Clear();

        workspace.Add(
            Heading("Ability Recipe Builder")
        );

        workspace.Add(
            Paragraph(
                "Create a complete AbilityData with " +
                "automatically configured AbilityEffects.\n\n" +
                "Existing BuffDefinitions are reused " +
                "without duplication."
            )
        );

        IMGUIContainer panel =
            new IMGUIContainer(
                () =>
                {
                    recipePanel.Draw(
                        ShowAsset,
                        RefreshLibrary
                    );
                }
            );

        panel.style.marginTop = 10;

        workspace.Add(panel);

        helpLabel.text =
            "Use the ? buttons beside fields " +
            "for detailed documentation.";
    }


    // =====================================================
    // DYNAMIC EFFECT REGISTRY
    // =====================================================

    private void DrawEffectRegistry()
    {
        workspace.Clear();

        workspace.Add(
            Heading("Dynamic Effect Registry")
        );

        workspace.Add(
            Paragraph(
                "Discover, create and attach reusable " +
                "gameplay effects without manually " +
                "registering every new effect type."
            )
        );

        IMGUIContainer panel =
            new IMGUIContainer(
                () =>
                {
                    effectRegistryPanel.Draw(
                        ShowAsset,
                        RefreshLibrary
                    );
                }
            );

        panel.style.marginTop = 10;

        workspace.Add(panel);

        helpLabel.text =
            "Effect Registry discovers asset-creatable " +
            "effect types automatically.";
    }



    // =====================================================
    // CONTENT AUDIT
    // =====================================================

    private void DrawContentAudit()
    {
        workspace.Clear();

        workspace.Add(
            Heading("Content Validation & Dependencies")
        );

        workspace.Add(
            Paragraph(
                "Find missing references, suspicious " +
                "configurations and dependencies between " +
                "RöK gameplay assets."
            )
        );

        IMGUIContainer panel =
            new IMGUIContainer(
                () =>
                {
                    diagnosticsPanel.Draw(
                        ShowAsset
                    );
                }
            );

        panel.style.marginTop = 10;

        workspace.Add(panel);

        helpLabel.text =
            "Content Audit is read-only. " +
            "Run a new scan after editing or creating assets.";
    }



    private void DrawCreator()
    {
        workspace.Clear();

        workspace.Add(
            Heading("Guided Content Creation")
        );

        workspace.Add(
            Paragraph(
                "Create related ScriptableObjects " +
                "and connect their references automatically."
            )
        );

        EnumField recipeField =
            new EnumField("Recipe", recipe);

        recipeField.RegisterValueChangedCallback(
            evt =>
            {
                recipe =
                    (RoKContentRecipe)evt.newValue;

                DrawCreator();
            }
        );

        AddField(
            recipeField,
            "Choose what to build.\n\n" +
            "SelfBuffAbility creates:\n" +
            "AbilityData → ApplyBuffDefinitionEffect → " +
            "BuffDefinition → EffectDefinition.\n\n" +
            "BuffDefinition creates a timed buff.\n" +
            "PassiveEffect creates a reusable effect.\n" +
            "EmptyAbility creates an unconfigured ability."
        );

        TextField nameField =
            new TextField("Name")
            {
                value = contentName
            };

        nameField.RegisterValueChangedCallback(
            evt => contentName = evt.newValue
        );

        AddField(
            nameField,
            "Name of the created gameplay content.\n\n" +
            "The factory automatically generates " +
            "unique asset paths, so existing files " +
            "will not be overwritten."
        );

        if (recipe != RoKContentRecipe.EmptyAbility)
        {
            DrawEffectSettings();
        }

        if (recipe == RoKContentRecipe.SelfBuffAbility ||
            recipe == RoKContentRecipe.BuffDefinition)
        {
            FloatField durationField =
                new FloatField("Buff Duration")
                {
                    value = duration
                };

            durationField.RegisterValueChangedCallback(
                evt => duration = evt.newValue
            );

            AddField(
                durationField,
                "How long the applied buff lasts " +
                "in seconds.\n\n" +
                "The BuffDefinition owns this timer, " +
                "not the EffectDefinition.\n\n" +
                "Example: 30 = thirty seconds."
            );
        }

        if (recipe == RoKContentRecipe.SelfBuffAbility)
        {
            FloatField cooldownField =
                new FloatField("Cooldown")
                {
                    value = cooldown
                };

            cooldownField.RegisterValueChangedCallback(
                evt => cooldown = evt.newValue
            );

            AddField(
                cooldownField,
                "Ability cooldown in seconds.\n\n" +
                "This controls how often the ability " +
                "can be activated. It does not control " +
                "how long the resulting buff remains active."
            );
        }

        // =================================================
        // CREATE
        // =================================================

        Button create =
            new Button(CreateContent)
            {
                text = "Create Content"
            };

        create.style.height = 36;
        create.style.marginTop = 14;

        create.SetEnabled(
            !EditorApplication
                .isPlayingOrWillChangePlaymode
        );

        workspace.Add(create);

        helpLabel.text =
            "Click the ? beside a field for " +
            "detailed documentation and examples.";
    }

    // =====================================================
    // EFFECT SETTINGS
    // =====================================================

    private void DrawEffectSettings()
    {
        workspace.Add(
            Heading("Effect Configuration")
        );

        if (recipe != RoKContentRecipe.PassiveEffect)
        {
            ObjectField existing =
                new ObjectField("Existing Effect")
                {
                    objectType =
                        typeof(EffectDefinition),

                    allowSceneObjects = false,

                    value = existingEffect
                };

            existing.RegisterValueChangedCallback(
                evt =>
                {
                    existingEffect =
                        evt.newValue
                        as EffectDefinition;

                    DrawCreator();
                }
            );

            AddField(
                existing,
                "Optional: reuse an existing " +
                "EffectDefinition asset.\n\n" +
                "When an existing effect is selected, " +
                "Content Studio does not create a duplicate.\n\n" +
                "Leave empty to create a new effect."
            );

            if (existingEffect != null)
            {
                workspace.Add(
                    Paragraph(
                        "Using existing effect: " +
                        existingEffect.name
                    )
                );

                return;
            }
        }

        EnumField kindField =
            new EnumField(
                "Effect Type",
                effectKind
            );

        kindField.RegisterValueChangedCallback(
            evt =>
            {
                effectKind =
                    (RoKPassiveEffectKind)evt.newValue;

                DrawCreator();
            }
        );

        AddField(
            kindField,
            "Select the reusable effect behaviour.\n\n" +
            "Stat Modifier changes character stats.\n" +
            "Damage Resistance reduces incoming damage.\n" +
            "Creature Damage Bonus affects selected creatures.\n" +
            "Damage Type Bonus improves one damage type."
        );

        if (effectKind ==
            RoKPassiveEffectKind.StatModifier)
        {
            EnumField statField =
                new EnumField("Stat", stat);

            statField.RegisterValueChangedCallback(
                evt => stat = (StatType)evt.newValue
            );

            AddField(
                statField,
                "Character stat to modify.\n\n" +
                "Example: MaxHP, Strength or Armor."
            );

            EnumField modifierField =
                new EnumField(
                    "Modifier Type",
                    modifierType
                );

            modifierField.RegisterValueChangedCallback(
                evt =>
                    modifierType =
                        (ModifierType)evt.newValue
            );

            AddField(
                modifierField,
                "Flat adds a numeric amount.\n\n" +
                "Percent applies a percentage according " +
                "to RöK's stat calculation stages."
            );
        }

        if (effectKind ==
                RoKPassiveEffectKind.DamageResistance ||
            effectKind ==
                RoKPassiveEffectKind.DamageTypeBonus)
        {
            EnumField damageField =
                new EnumField(
                    "Damage Type",
                    damageType
                );

            damageField.RegisterValueChangedCallback(
                evt =>
                    damageType =
                        (DamageType)evt.newValue
            );

            AddField(
                damageField,
                "Damage type affected by the effect.\n\n" +
                "Examples: Physical, Fire, Frost, Curse.\n" +
                "This is separate from CreatureType."
            );
        }

        if (effectKind ==
            RoKPassiveEffectKind.CreatureDamageBonus)
        {
            EnumFlagsField creatures =
                new EnumFlagsField(
                    "Creature Types",
                    creatureTypes
                );

            creatures.RegisterValueChangedCallback(
                evt =>
                    creatureTypes =
                        (CreatureType)evt.newValue
            );

            AddField(
                creatures,
                "Creature classifications affected.\n\n" +
                "Multiple types can be selected.\n\n" +
                "Example: Beast | Wolf.\n" +
                "Any matching flag qualifies."
            );
        }

        FloatField amount =
            new FloatField("Effect Value")
            {
                value = effectValue
            };

        amount.RegisterValueChangedCallback(
            evt => effectValue = evt.newValue
        );

        AddField(
            amount,
            "Magnitude of the effect.\n\n" +
            "For percentage modifiers:\n" +
            "0.10 = +10%\n" +
            "0.25 = +25%\n" +
            "-0.20 = -20%\n\n" +
            "For Flat stat modifiers the value " +
            "represents a numeric amount.\n\n" +
            "Remember: the effect itself owns no timer."
        );
    }

    // =====================================================
    // CREATE ACTION
    // =====================================================

    private void CreateContent()
    {
        try
        {
            ScriptableObject result =
                RoKContentStudioFactory.Create(
                    recipe,
                    contentName,
                    existingEffect,
                    effectKind,
                    stat,
                    modifierType,
                    damageType,
                    creatureTypes,
                    effectValue,
                    duration,
                    cooldown
                );

            selectedAsset = result;

            RefreshLibrary();

            Selection.activeObject = result;

            ShowAsset(result);

            EditorGUIUtility.PingObject(result);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Content creation failed",
                exception.Message,
                "OK"
            );
        }
    }

    // =====================================================
    // FIELD HELP
    // =====================================================

    private void AddField(
        VisualElement field,
        string help)
    {
        VisualElement row =
            new VisualElement();

        row.style.flexDirection =
            FlexDirection.Row;

        row.style.alignItems =
            Align.Center;

        row.style.marginBottom = 6;

        field.style.flexGrow = 1;
        field.tooltip = help;

        row.Add(field);

        Button info =
            new Button(
                () => helpLabel.text = help
            )
            {
                text = "?"
            };

        info.style.width = 28;
        info.style.marginLeft = 6;

        info.tooltip =
            "Show detailed field documentation";

        row.Add(info);

        workspace.Add(row);
    }

    // =====================================================
    // PRESENTATION HELPERS
    // =====================================================

    private static Label Heading(string text)
    {
        Label label = new Label(text);

        label.style.fontSize = 15;

        label.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        label.style.marginTop = 12;
        label.style.marginBottom = 8;

        return label;
    }

    private static Label Paragraph(string text)
    {
        Label label = new Label(text);

        label.style.whiteSpace =
            WhiteSpace.Normal;

        label.style.marginBottom = 8;

        return label;
    }

    // =====================================================
    // CONTEXTUAL ASSET HELP
    // =====================================================

    private static string GetAssetHelp(
        ScriptableObject asset)
    {
        if (asset is AbilityData)
        {
            return
                "AbilityData controls targeting, timing, " +
                "cooldown and which actions are executed.\n\n" +
                "Its Effects array contains AbilityEffect " +
                "actions, not passive EffectDefinitions.\n\n" +
                "For a self buff, use ApplyBuffDefinitionEffect " +
                "with Target Mode = Caster.";
        }

        if (asset is BuffDefinition)
        {
            return
                "BuffDefinition owns duration, stacking " +
                "and removal rules.\n\n" +
                "Its Effects array contains reusable " +
                "EffectDefinitions.\n\n" +
                "The effects remain active while the " +
                "buff owns their runtime handles.";
        }

        if (asset is EffectDefinition)
        {
            return
                "EffectDefinition describes reusable " +
                "gameplay behaviour.\n\n" +
                "It can be shared between buffs, talents, " +
                "titles and future equipment.\n\n" +
                "Active state belongs to EffectRuntime, " +
                "not this ScriptableObject.";
        }

        if (asset is AbilityEffect)
        {
            return
                "AbilityEffect is an execution action.\n\n" +
                "The action pipeline controls targeting, " +
                "hit resolution and execution timing.\n\n" +
                "An ApplyBuffDefinitionEffect connects " +
                "an action to a BuffDefinition.";
        }

        if (asset is TalentData)
        {
            return
                "TalentData describes talent progression, " +
                "requirements and rewards.\n\n" +
                "Passive effects should use the shared " +
                "EffectDefinition model when migrated.\n\n" +
                "Invested points control effect stacks.";
        }

        return "No documentation available.";
    }
}

#endif
