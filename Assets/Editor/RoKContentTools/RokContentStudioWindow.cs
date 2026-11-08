
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


    private readonly RoKForgeShell forgeShell =
        new();


    private readonly RoKForgeNavigation navigation =
        new();


    private readonly RoKForgeNavigationInput
        navigationInput = new();




    private readonly RoKForgeAbilityCreationPanel
        abilityCreationPanel = new();


    private readonly RoKForgeDraftSession
        contentDraftSession = new();




    // =====================================================
    // THE FORGE MENU
    // =====================================================

    [MenuItem("The Forge/Open The Forge")]
    public static void Open()
    {
        RoKContentStudioWindow window =
            GetWindow<RoKContentStudioWindow>();

        window.titleContent =
            new GUIContent("The Forge");

        window.minSize =
            new Vector2(900f, 560f);

        window.Show();
    }



    // =====================================================
    // THE FORGE V2.0 - ROOT LAYOUT
    // =====================================================


    public void CreateGUI()
    {
        try
        {
            Debug.Log(
                "[The Forge] Initializing UI..."
            );


            RoKForgeCallbacks callbacks =
                new RoKForgeCallbacks
                {
                    // =================================================
                    // NAVIGATION
                    // =================================================

                    Home = () =>
                        Navigate(RoKForgeRoute.Home()),

                    Back = NavigateBack,

                    Forward = NavigateForward,

                    NavigateToHistory = NavigateToHistory,

                    OpenAsset = ShowAsset,

                    // =================================================
                    // CREATION
                    // =================================================

                    CreateAbility = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.CreateAbility
                            )
                        ),


                    CreateItem = () =>
                        Navigate(RoKForgeRoute.ForPage(
                            RoKForgePage.CreateItem)),

                    CreateTalent = () =>
                        Navigate(RoKForgeRoute.ForPage(
                            RoKForgePage.CreateTalent)),

                    CreateFavour = () =>
                        Navigate(RoKForgeRoute.ForPage(
                            RoKForgePage.CreateFavour)),


                    CreateBuff = () =>
                    {
                        recipe = RoKContentRecipe.BuffDefinition;

                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.GuidedCreator
                            )
                        );
                    },

                    CreateEffect = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.EffectRegistry
                            )
                        ),

                    AdvancedAbilityRecipes = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.AbilityRecipes
                            )
                        ),

                    GuidedCreator = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.GuidedCreator
                            )
                        ),

                    // =================================================
                    // TOOLS
                    // =================================================

                    ContentAudit = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.ContentAudit
                            )
                        ),

                    EffectRegistry = () =>
                        Navigate(
                            RoKForgeRoute.ForPage(
                                RoKForgePage.EffectRegistry
                            )
                        ),

                    RefreshLibrary = RefreshLibrary,

                    // =================================================
                    // LIBRARY
                    // =================================================

                    CategoryChanged = value =>
                    {
                        category = value;
                        RefreshLibrary();
                    },

                    SearchChanged = value =>
                    {
                        search = value;
                        RefreshLibrary();
                    }
                };


            // =============================================
            // BUILD SHELL
            // =============================================

            forgeShell.Build(
                rootVisualElement,
                category,
                search,
                callbacks
            );


            // =====================================================
            // FORGE INPUT SHORTCUTS
            // =====================================================

            navigationInput.Bind(
                this,
                rootVisualElement,
                NavigateBack,
                NavigateForward
            );


            // =============================================
            // CONNECT REFERENCES
            // =============================================

            libraryView =
                forgeShell.LibraryView;

            workspace =
                forgeShell.Workspace;

            helpLabel =
                forgeShell.HelpLabel;


            RoKForgeVisualStyle.ApplyShell(
                rootVisualElement,
                libraryView,
                workspace,
                helpLabel
            );


            // =============================================
            // INITIALIZE CONTENT
            // =============================================


            RefreshLibrary();

            RenderCurrentRoute();


            Debug.Log(
                "[The Forge] UI initialized successfully."
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            // =============================================
            // VISIBLE ERROR FALLBACK
            // =============================================

            VisualElement root =
                rootVisualElement;

            root.Clear();

            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;
            root.style.paddingTop = 16;

            Label title =
                new Label(
                    "THE FORGE - INITIALIZATION FAILED"
                );

            title.style.fontSize = 17;

            title.style.unityFontStyleAndWeight =
                FontStyle.Bold;

            title.style.color =
                new Color(1f, 0.35f, 0.35f);

            root.Add(title);

            Label message =
                new Label(
                    exception.ToString()
                );

            message.style.whiteSpace =
                WhiteSpace.Normal;

            message.style.marginTop = 12;

            root.Add(message);

            Label instructions =
                new Label(
                    "Check the Unity Console for " +
                    "the complete stack trace."
                );

            instructions.style.marginTop = 16;

            root.Add(instructions);
        }
    }


    // =====================================================
    // CENTRAL NAVIGATION
    // =====================================================

    private void Navigate(RoKForgeRoute destination)
    {
        if (destination == null)
            return;

        if (navigation.Current.SameAs(destination))
        {
            RenderCurrentRoute();
            return;
        }

        if (!CanLeaveCurrentRoute(destination))
            return;

        navigation.Push(destination);

        RenderCurrentRoute();
    }

    // =====================================================
    // BACK
    // =====================================================

    private void NavigateBack()
    {
        if (!navigation.CanGoBack)
            return;

        RoKForgeRoute destination =
            navigation.PeekBack();

        if (!CanLeaveCurrentRoute(destination))
            return;

        navigation.Back();

        RenderCurrentRoute();
    }

    // =====================================================
    // FORWARD
    // =====================================================

    private void NavigateForward()
    {
        if (!navigation.CanGoForward)
            return;

        RoKForgeRoute destination =
            navigation.PeekForward();

        if (!CanLeaveCurrentRoute(destination))
            return;

        navigation.Forward();

        RenderCurrentRoute();
    }

    // =====================================================
    // HISTORY NAVIGATION
    // =====================================================

    private void NavigateToHistory(int index)
    {
        if (index < 0 ||
            index >= navigation.History.Count ||
            index == navigation.Position)
        {
            return;
        }

        RoKForgeRoute destination =
            navigation.History[index];

        if (!CanLeaveCurrentRoute(destination))
            return;

        navigation.JumpTo(index);

        RenderCurrentRoute();
    }

    // =====================================================
    // UNSAVED DRAFT PROTECTION
    // =====================================================


    private bool CanLeaveCurrentRoute(
        RoKForgeRoute destination)
    {
        if (destination == null)
            return false;

        if (navigation.Current.SameAs(destination))
            return true;

        bool hasAbilityDraft =
            navigation.Current.Page ==
                RoKForgePage.CreateAbility &&
            abilityCreationPanel.Draft != null;

        bool hasContentDraft =
            (navigation.Current.Page == RoKForgePage.CreateItem ||
             navigation.Current.Page == RoKForgePage.CreateTalent ||
             navigation.Current.Page == RoKForgePage.CreateFavour) &&
            contentDraftSession.Draft != null;

        if (!hasAbilityDraft && !hasContentDraft)
            return true;

        bool discard = EditorUtility.DisplayDialog(
            "Discard Unsaved Content?",
            "Leaving this page will discard the current " +
            "draft and its temporary changes.\n\n" +
            "Existing project assets will not be modified.",
            "Discard Draft",
            "Keep Editing"
        );

        if (!discard)
            return false;

        if (hasAbilityDraft)
            abilityCreationPanel.Cancel();

        if (hasContentDraft)
            contentDraftSession.Cancel();

        return true;
    }


    // =====================================================
    // RENDER CURRENT ROUTE
    // =====================================================

    private void RenderCurrentRoute()
    {
        if (workspace == null ||
            helpLabel == null)
        {
            return;
        }

        RoKForgeRoute route =
            navigation.Current;


        // Remove home-only layout when editing content.

        if (route.Page != RoKForgePage.Home)
        {
            VisualElement content =
                workspace.contentContainer;

            content.RemoveFromClassList("forge-home");
            content.style.minHeight = 0;
        }


        switch (route.Page)
        {
            case RoKForgePage.Home:

                DrawWelcome();
                break;

            case RoKForgePage.Asset:

                if (route.Asset != null)
                {
                    RenderAsset(route.Asset);
                }
                else
                {
                    navigation.Push(
                        RoKForgeRoute.Home()
                    );

                    DrawWelcome();
                }

                break;

            case RoKForgePage.CreateAbility:

                DrawNewAbility();
                break;


            case RoKForgePage.CreateItem:
                DrawNewContent(typeof(ItemData), "Item", "Assets/ContentStudio/Items");
                break;

            case RoKForgePage.CreateTalent:
                DrawNewContent(typeof(TalentData), "Talent", "Assets/ContentStudio/Talents");
                break;

            case RoKForgePage.CreateFavour:
                DrawNewContent(typeof(FavourData), "Favour", "Assets/ContentStudio/Favours");
                break;


            case RoKForgePage.AbilityRecipes:

                DrawAbilityRecipes();
                break;

            case RoKForgePage.EffectRegistry:

                DrawEffectRegistry();
                break;

            case RoKForgePage.ContentAudit:

                DrawContentAudit();
                break;

            case RoKForgePage.GuidedCreator:

                DrawCreator();
                break;

            default:

                DrawWelcome();
                break;
        }


        forgeShell.UpdateNavigation(navigation);

        ScriptableObject activeAsset =
            navigation.Current.Page == RoKForgePage.Asset
                ? navigation.Current.Asset
                : null;

        RoKForgeVisualStyle.UpdateLibrarySelection(
            libraryView,
            activeAsset
        );

    }


    private void DrawItemOrTalentFields(ScriptableObject asset)
    {
        bool twoColumns = RoKForgeFormLayout.UseTwoColumns(workspace);

        if (asset is ItemData item)
            RoKForgeItemEditor.Draw(item, twoColumns);


        else if (asset is TalentData talent)
            RoKForgeTalentEditor.Draw(
                talent,
                twoColumns,
                OpenTalentEffectRegistry,
                ShowAsset
            );


    }


    private void OpenTalentEffectRegistry(TalentData talent)
    {
        if (talent == null ||
            !AssetDatabase.Contains(talent))
            return;

        effectRegistryPanel.PrepareForTalent(talent);

        Navigate(
            RoKForgeRoute.ForPage(
                RoKForgePage.EffectRegistry
            )
        );
    }


    private void DrawNewContent(Type type, string label, string folder)
    {
        if (contentDraftSession.Draft == null)
        {
            contentDraftSession.Begin(type);

            switch (contentDraftSession.Draft)
            {
                case ItemData item:
                    item.itemName = "New Item";
                    item.name = item.itemName;
                    break;

                case TalentData talent:
                    talent.talentName = "New Talent";
                    talent.name = talent.talentName;
                    break;

                case FavourData favour:
                    // DisplayName falls back to the object name.
                    // Keep the fallback empty until the user enters a name.
                    favour.name = string.Empty;
                    break;
            }
        }

        ScriptableObject draft = contentDraftSession.Draft;

        string GetDraftName()
        {
            return draft switch
            {
                ItemData item => item.itemName,
                TalentData talent => talent.talentName,
                FavourData favour => favour.DisplayName,
                _ => string.Empty
            };
        }

        forgeShell.SetWelcomeMode(false);
        workspace.Clear();
        workspace.Add(Heading("Create New " + label));

        bool isFavour = draft is FavourData;

        if (draft is FavourData favourDraft)
        {
            workspace.Add(RoKForgeFavourEditor.Create(workspace, favourDraft));
        }
        else
        {
            VisualElement header = RoKForgeIdentityHeader.Create(draft);
            header.style.maxWidth = 640;
            workspace.Add(header);
        }

        IMGUIContainer panel = new(() =>
        {
            if (draft == null || contentDraftSession.Draft != draft)
                return;

            if (!isFavour)
                DrawItemOrTalentFields(draft);

            EditorGUILayout.Space(12);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", GUILayout.Height(34)))
            {
                EditorApplication.delayCall += () =>
                {
                    if (contentDraftSession.Draft != draft)
                        return;

                    contentDraftSession.Cancel();
                    Navigate(RoKForgeRoute.Home());
                };
            }

            using (new EditorGUI.DisabledScope(
                string.IsNullOrWhiteSpace(GetDraftName()) ||
                EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Create " + label, GUILayout.Height(34)))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (draft == null ||
                            contentDraftSession.Draft != draft ||
                            EditorApplication.isPlayingOrWillChangePlaymode)
                        {
                            return;
                        }

                        string name = GetDraftName();

                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        try
                        {
                            if (draft is FavourData favour)
                            {
                                string id = favour.Id;

                                if (string.IsNullOrWhiteSpace(id))
                                {
                                    throw new InvalidOperationException(
                                        "Enter a name that produces a valid Favour ID.");
                                }

                                foreach (ScriptableObject asset in
                                    RoKContentStudioCatalog.Find(RoKContentCategory.Favours))
                                {
                                    if (asset is FavourData existing && existing.Id == id)
                                    {
                                        throw new InvalidOperationException(
                                            $"Favour ID '{id}' is already used by " +
                                            $"'{existing.DisplayName}'. Choose another name.");
                                    }
                                }
                            }

                            ScriptableObject created =
                                contentDraftSession.Commit(name, folder);

                            RefreshLibrary();
                            ShowAsset(created);

                            Selection.activeObject = created;
                            EditorGUIUtility.PingObject(created);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);

                            EditorUtility.DisplayDialog(
                                "Creation Failed",
                                exception.Message,
                                "OK");
                        }
                    };
                }
            }

            EditorGUILayout.EndHorizontal();
        });

        panel.style.width = Length.Percent(100);
        panel.style.maxWidth = 720;
        panel.style.alignSelf = Align.FlexStart;
        panel.style.marginTop = 10;

        workspace.Add(panel);

        helpLabel.text =
            "Unsaved " + label +
            " draft. Cancel discards all temporary changes.";
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
            if (asset == null)
                continue;

            ScriptableObject captured = asset;

            Button row =
                RoKForgeVisualStyle.CreateAssetRow(
                    asset,
                    () => ShowAsset(captured)
                );

            row.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                bool canDelete =
                    !EditorApplication.isPlayingOrWillChangePlaymode &&
                    TryGetDeletePath(captured, out _);

                evt.menu.AppendAction(
                    "Delete",
                    _ => QueueDeleteAsset(captured),
                    canDelete
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }));

            libraryView.Add(row);
        }

        if (assets.Count == 0)
        {
            libraryView.Add(
                new Label("No matching content.")
            );
        }

        ScriptableObject activeAsset =
            navigation.Current.Page == RoKForgePage.Asset
                ? navigation.Current.Asset
                : null;

        RoKForgeVisualStyle.UpdateLibrarySelection(
            libraryView,
            activeAsset
        );
    }




    // =====================================================
    // FORGE HOME
    // =====================================================



    private void DrawWelcome()
    {
        forgeShell.RenderWelcome();

        RoKForgeVisualStyle.StyleHome(
            workspace
        );

        helpLabel.text =
            "Search the library, open recent content, " +
            "or create something new.";
    }




    // =====================================================
    // SELECTED ASSET
    // =====================================================


    // =====================================================
    // OPEN ASSET
    // =====================================================

    private void ShowAsset(ScriptableObject asset)
    {
        if (asset == null)
            return;

        Navigate(
            RoKForgeRoute.ForAsset(asset)
        );
    }

    // =====================================================
    // RENDER EXISTING ASSET
    // =====================================================

    private void RenderAsset(ScriptableObject asset)
    {
        if (asset == null)
            return;

        selectedAsset = asset;

        forgeShell.SetWelcomeMode(false);

        forgeShell.RememberAsset(asset);

        workspace.Clear();


        // =====================================================
        // SHARED ASSET IDENTITY HEADER
        // =====================================================

        workspace.Add(
            RoKForgeIdentityHeader.Create(
                asset,
                RefreshLibrary
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

        VisualElement assetDetails = workspace;

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
                            RefreshLibrary,
                            RoKForgeFormLayout.UseTwoColumns(workspace)
                        );
                    }
                );

            composer.style.marginTop = 10;

            RoKForgeFormLayout.Constrain(composer);

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
                            RefreshLibrary,
                            RoKForgeFormLayout.UseTwoColumns(workspace)
                        );
                    }
                );

            composer.style.marginTop = 10;

            workspace.Add(composer);
        }
        else if (asset is ItemData || asset is TalentData)
        {
            IMGUIContainer editor = new(() =>
            {
                if (asset != null)
                    DrawItemOrTalentFields(asset);
            });

            editor.style.width = Length.Percent(100);
            editor.style.maxWidth = 720;
            editor.style.alignSelf = Align.FlexStart;
            editor.style.marginTop = 10;

            workspace.Add(editor);
        }
        else if (asset is FavourData favour)
        {
            workspace.Add(RoKForgeFavourEditor.Create(workspace, favour, ShowAsset));
        }

        else
        {

            InspectorElement inspector =
                new InspectorElement(asset);

            RoKForgeLinkedField.EnableNestedNavigation(
                inspector,
                asset,
                ShowAsset
            );

            inspector.style.marginTop = 10;


            assetDetails = new VisualElement();

            workspace.Add(
                RoKForgeFormLayout.CreateColumns(workspace, inspector, assetDetails));
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

        if (TryGetDeletePath(asset, out _))
        {
            workspace.Add(
                RoKForgeVisualStyle.CreateDeleteAssetButton(
                    () => QueueDeleteAsset(asset)));
        }

        helpLabel.text =
            "Inspector fields can be edited here. " +
            "Use Validation after changing references.";
    }

    private static bool TryGetDeletePath(
    ScriptableObject asset,
    out string path)
    {
        path = string.Empty;

        if (asset == null || !AssetDatabase.IsMainAsset(asset))
            return false;

        path = AssetDatabase.GetAssetPath(asset);

        return !string.IsNullOrEmpty(path) &&
               path.StartsWith("Assets/", StringComparison.Ordinal) &&
               path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);
    }

    private void QueueDeleteAsset(ScriptableObject asset)
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null || !TryGetDeletePath(asset, out string path))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Delete Asset",
                    "Exit Play Mode before deleting assets.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Asset?",
                $"Move '{asset.name}' to the system trash?\n\n" +
                $"{path}\n\n" +
                "Any content that uses this asset will have a missing reference.",
                "Yes",
                "No");

            if (!confirmed ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                !TryGetDeletePath(asset, out string currentPath) ||
                currentPath != path)
            {
                return;
            }

            bool wasSelected = selectedAsset == asset;

            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                EditorUtility.DisplayDialog(
                    "Could Not Delete Asset",
                    $"Unity could not move this asset to the trash:\n\n{path}",
                    "OK");
                return;
            }

            if (wasSelected)
                selectedAsset = null;

            navigation.ForgetAsset(asset);
            actionComposer.InvalidateCache();
            effectComposer.InvalidateCache();
            RefreshLibrary();

            if (navigation.Current.Page == RoKForgePage.Home ||
                navigation.Current.Page == RoKForgePage.Asset)
            {
                RenderCurrentRoute();
            }
            else
            {
                forgeShell.UpdateNavigation(navigation);
            }
        };
    }

    // =====================================================
    // GUIDED CREATOR
    // =====================================================


    // =====================================================
    // THE FORGE - UNIFIED ABILITY CREATION
    // =====================================================

    private void DrawNewAbility()
    {

        // Only create a new draft if none exists.
        // Re-rendering must preserve current edits.

        if (abilityCreationPanel.Draft == null)
        {
            abilityCreationPanel.Begin();
        }


        forgeShell.SetWelcomeMode(false);

        workspace.Clear();

        workspace.Add(
            Heading("Create New Ability")
        );

        // =============================================
        // SHARED IDENTITY HEADER
        // =============================================


        VisualElement identity =
            RoKForgeIdentityHeader.Create(
                abilityCreationPanel.Draft
            );

        abilityCreationPanel.UseTwoColumns =
            RoKForgeFormLayout.UseTwoColumns(workspace);

        workspace.Add(identity);


        // =============================================
        // SHARED SETTINGS + DRAFT ACTIONS
        // =============================================

        IMGUIContainer panel =
            new IMGUIContainer(
                () =>
                {


                    abilityCreationPanel.UseTwoColumns =
                        RoKForgeFormLayout.UseTwoColumns(workspace);


                    abilityCreationPanel.Draw(
                        created =>
                        {
                            RefreshLibrary();

                            ShowAsset(created);
                        },

                        () =>
                        {
                            abilityCreationPanel.Cancel();

                            Navigate(
                                RoKForgeRoute.Home()
                            );
                        }

                    );
                }
            );


        panel.style.marginTop = 10;

        // Fill the available workspace,
        // but stop growing beyond 720 px.
        panel.style.width =
            Length.Percent(100);

        panel.style.maxWidth = 720f;

        panel.style.alignSelf =
            Align.FlexStart;

        workspace.Add(panel);


        helpLabel.text =
            "Unsaved draft. No project assets are " +
            "created until Create Ability is confirmed.";
    }


    // =====================================================
    // ABILITY RECIPE BUILDER
    // =====================================================

    private void DrawAbilityRecipes()
    {

        forgeShell.SetWelcomeMode(false);


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
                        ShowAsset, RefreshLibrary, RoKForgeFormLayout.UseTwoColumns(workspace));
                }
            );

        panel.style.marginTop = 10;

        RoKForgeFormLayout.Constrain(panel);

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

        forgeShell.SetWelcomeMode(false);


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
                        ShowAsset, RefreshLibrary, RoKForgeFormLayout.UseTwoColumns(workspace));
                }
            );

        panel.style.marginTop = 10;

        RoKForgeFormLayout.Constrain(panel);

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

        forgeShell.SetWelcomeMode(false);


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
                        ShowAsset, RoKForgeFormLayout.UseTwoColumns(workspace));
                }
            );

        panel.style.marginTop = 10;

        RoKForgeFormLayout.Constrain(panel);

        workspace.Add(panel);

        helpLabel.text =
            "Content Audit is read-only. " +
            "Run a new scan after editing or creating assets.";
    }



    private void DrawCreator()
    {

        forgeShell.SetWelcomeMode(false);


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

        VisualElement setupColumn = new();

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
            setupColumn,
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
            setupColumn,
            nameField,
            "Name of the created gameplay content.\n\n" +
            "The factory automatically generates " +
            "unique asset paths, so existing files " +
            "will not be overwritten."
        );

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
                setupColumn,
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
                setupColumn,
                cooldownField,
                "Ability cooldown in seconds.\n\n" +
                "This controls how often the ability " +
                "can be activated. It does not control " +
                "how long the resulting buff remains active."
            );
        }

        if (recipe == RoKContentRecipe.EmptyAbility)
        {
            RoKForgeFormLayout.Constrain(setupColumn);
            workspace.Add(setupColumn);
        }
        else
        {
            VisualElement effectColumn = new();
            DrawEffectSettings(effectColumn);

            workspace.Add(
                RoKForgeFormLayout.CreateColumns(workspace, setupColumn, effectColumn));
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

        RoKForgeFormLayout.Constrain(create);

        workspace.Add(create);

        helpLabel.text =
            "Click the ? beside a field for " +
            "detailed documentation and examples.";
    }

    // =====================================================
    // EFFECT SETTINGS
    // =====================================================

    private void DrawEffectSettings(VisualElement effectColumn)
    {
        effectColumn.Add(Heading("Effect Configuration"));

        if (recipe != RoKContentRecipe.PassiveEffect)
        {
            ObjectField existing = new("Existing Effect")
            {
                objectType = typeof(EffectDefinition),
                allowSceneObjects = false,
                value = existingEffect
            };

            existing.RegisterValueChangedCallback(evt =>
            {
                existingEffect = evt.newValue as EffectDefinition;
                DrawCreator();
            });

            AddField(
                effectColumn,
                existing,
                "Optional: reuse an existing EffectDefinition asset.\n\n" +
                "When an existing effect is selected, " +
                "Content Studio does not create a duplicate.\n\n" +
                "Leave empty to create a new effect.");

            if (existingEffect != null)
            {
                effectColumn.Add(
                    Paragraph("Using existing effect: " + existingEffect.name));

                return;
            }
        }

        EnumField kindField = new("Effect Type", effectKind);

        kindField.RegisterValueChangedCallback(evt =>
        {
            effectKind = (RoKPassiveEffectKind)evt.newValue;
            DrawCreator();
        });

        AddField(
            effectColumn,
            kindField,
            "Select the reusable effect behaviour.\n\n" +
            "Stat Modifier changes character stats.\n" +
            "Damage Resistance reduces incoming damage.\n" +
            "Creature Damage Bonus affects selected creatures.\n" +
            "Damage Type Bonus improves one damage type.");

        if (effectKind == RoKPassiveEffectKind.StatModifier)
        {
            EnumField statField = new("Stat", stat);
            statField.RegisterValueChangedCallback(evt => stat = (StatType)evt.newValue);

            AddField(
                effectColumn,
                statField,
                "Character stat to modify.\n\n" +
                "Example: MaxHP, Strength or Armor.");

            EnumField modifierField = new("Modifier Type", modifierType);
            modifierField.RegisterValueChangedCallback(evt =>
                modifierType = (ModifierType)evt.newValue);

            AddField(
                effectColumn,
                modifierField,
                "Flat adds a numeric amount.\n\n" +
                "Percent applies a percentage according " +
                "to RöK's stat calculation stages.");
        }

        if (effectKind == RoKPassiveEffectKind.DamageResistance ||
            effectKind == RoKPassiveEffectKind.DamageTypeBonus)
        {
            EnumField damageField = new("Damage Type", damageType);
            damageField.RegisterValueChangedCallback(evt =>
                damageType = (DamageType)evt.newValue);

            AddField(
                effectColumn,
                damageField,
                "Damage type affected by the effect.\n\n" +
                "Examples: Physical, Fire, Frost, Curse.\n" +
                "This is separate from CreatureType.");
        }

        if (effectKind == RoKPassiveEffectKind.CreatureDamageBonus)
        {
            EnumFlagsField creatures = new("Creature Types", creatureTypes);
            creatures.RegisterValueChangedCallback(evt =>
                creatureTypes = (CreatureType)evt.newValue);

            AddField(
                effectColumn,
                creatures,
                "Creature classifications affected.\n\n" +
                "Multiple types can be selected.\n\n" +
                "Example: Beast | Wolf.\n" +
                "Any matching flag qualifies.");
        }

        FloatField amount = new("Effect Value")
        {
            value = effectValue
        };

        amount.RegisterValueChangedCallback(evt => effectValue = evt.newValue);

        AddField(
            effectColumn,
            amount,
            "Magnitude of the effect.\n\n" +
            "For percentage modifiers:\n" +
            "0.10 = +10%\n" +
            "0.25 = +25%\n" +
            "-0.20 = -20%\n\n" +
            "For Flat stat modifiers the value represents a numeric amount.\n\n" +
            "Remember: the effect itself owns no timer.");
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
        VisualElement parent,
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

        parent.Add(row);
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


    // =====================================================
    // WINDOW CLEANUP
    // =====================================================


    private void OnDisable()
    {
        navigationInput?.Dispose();

        abilityCreationPanel?.Dispose();

        contentDraftSession?.Cancel();

    }


}

#endif
