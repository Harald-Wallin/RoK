
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// =========================================================
// FORGE CALLBACKS
// =========================================================

public sealed class RoKForgeCallbacks
{
    public Action Home;
    public Action CreateAbility;
    public Action CreateBuff;
    public Action CreateEffect;

    public Action GuidedCreator;
    public Action ContentAudit;
    public Action EffectRegistry;
    public Action RefreshLibrary;

    public Action<ScriptableObject> OpenAsset;

    public Action<RoKContentCategory> CategoryChanged;
    public Action<string> SearchChanged;
}

// =========================================================
// THE FORGE - WINDOW SHELL
// =========================================================

public sealed class RoKForgeShell
{
    private const int MaxRecentAssets = 5;

    private readonly List<string> recentGuids = new();

    private RoKForgeCallbacks callbacks;

    private VisualElement quickCreatePanel;

    public ScrollView LibraryView { get; private set; }
    public ScrollView Workspace { get; private set; }
    public Label HelpLabel { get; private set; }

    private static string RecentKey =>
        "RoK.TheForge.RecentAssets." +
        Application.dataPath;


    // =====================================================
    // BUILD ROOT
    // =====================================================

    public void Build(
        VisualElement root,
        RoKContentCategory category,
        string search,
        RoKForgeCallbacks forgeCallbacks)
    {


        if (root == null)
            throw new ArgumentNullException(nameof(root));

        callbacks = forgeCallbacks ??
            throw new ArgumentNullException(
                nameof(forgeCallbacks)
            );

        // =====================================================
        // LOAD EDITOR STATE
        // =====================================================
        //
        // EditorPrefs must not be accessed from the
        // constructor of an EditorWindow-owned object.
        //
        // Build() runs during CreateGUI(), when Unity
        // Editor APIs are safe to access.
        // =====================================================

        LoadRecent();

        root.Clear();


        root.style.flexDirection =
            FlexDirection.Column;

        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 8;
        root.style.paddingBottom = 8;

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

        Button home =
            new Button(
                () => callbacks.Home?.Invoke()
            )
            {
                text = "THE FORGE"
            };

        home.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        home.style.fontSize = 17;
        home.style.flexGrow = 1;

        home.tooltip =
            "Return to The Forge home.";

        header.Add(home);

        ToolbarMenu tools =
            new ToolbarMenu
            {
                text = "TOOLS"
            };

        tools.tooltip =
            "Additional content creation " +
            "and diagnostic tools.";

        tools.menu.AppendAction(
            "Content Audit",
            _ => callbacks.ContentAudit?.Invoke()
        );

        tools.menu.AppendAction(
            "Effect Registry",
            _ => callbacks.EffectRegistry?.Invoke()
        );

        tools.menu.AppendAction(
            "Guided Creator (Legacy)",
            _ => callbacks.GuidedCreator?.Invoke()
        );

        header.Add(tools);

        Button refresh =
            new Button(
                () => callbacks.RefreshLibrary?.Invoke()
            )
            {
                text = "Refresh"
            };

        refresh.tooltip =
            "Refresh the Content Library.";

        refresh.style.marginLeft = 6;

        header.Add(refresh);

        root.Add(header);

        // =================================================
        // RESIZABLE TWO-PANE LAYOUT
        // =================================================

        TwoPaneSplitView split =
            new TwoPaneSplitView(
                0,
                255f,
                TwoPaneSplitViewOrientation.Horizontal
            );

        split.style.flexGrow = 1;
        split.style.minHeight = 0;

        root.Add(split);

        BuildLibrary(
            split,
            category,
            search
        );

        BuildMainArea(split);

        // =================================================
        // FOOTER
        // =================================================

        HelpLabel =
            new Label(
                "Welcome to The Forge."
            );

        HelpLabel.style.whiteSpace =
            WhiteSpace.Normal;

        HelpLabel.style.marginTop = 6;
        HelpLabel.style.paddingTop = 6;
        HelpLabel.style.paddingBottom = 3;

        HelpLabel.style.fontSize = 11;
        HelpLabel.style.color = MutedColor;

        root.Add(HelpLabel);
    }

    // =====================================================
    // LIBRARY PANE
    // =====================================================

    private void BuildLibrary(
        TwoPaneSplitView split,
        RoKContentCategory category,
        string search)
    {
        VisualElement pane =
            new VisualElement();

        pane.style.flexDirection =
            FlexDirection.Column;

        pane.style.minWidth = 180;
        pane.style.maxWidth = 430;
        pane.style.minHeight = 0;

        pane.style.paddingRight = 8;

        Label heading =
            SectionLabel("CONTENT LIBRARY");

        pane.Add(heading);

        EnumField filter =
            new EnumField(
                "Filter",
                category
            );

        filter.tooltip =
            "Filter content by category.\n\n" +
            "Does not change any assets.";

        filter.RegisterValueChangedCallback(
            evt =>
            {
                callbacks.CategoryChanged?.Invoke(
                    (RoKContentCategory)evt.newValue
                );
            }
        );

        pane.Add(filter);

        TextField searchField =
            new TextField
            {
                value = search ?? string.Empty
            };

        searchField.style.marginTop = 6;

        searchField.tooltip =
            "Search existing RöK content assets " +
            "by name across the project.";

        searchField.RegisterValueChangedCallback(
            evt =>
            {
                callbacks.SearchChanged?.Invoke(
                    evt.newValue
                );
            }
        );

        Label searchLabel =
            new Label("SEARCH");

        searchLabel.style.fontSize = 10;
        searchLabel.style.color = MutedColor;
        searchLabel.style.marginTop = 8;

        pane.Add(searchLabel);
        pane.Add(searchField);

        Label listLabel =
            SectionLabel("ASSETS");

        listLabel.style.marginTop = 12;

        pane.Add(listLabel);

        LibraryView = new ScrollView();

        LibraryView.style.flexGrow = 1;
        LibraryView.style.minHeight = 0;

        pane.Add(LibraryView);

        split.Add(pane);
    }

    // =====================================================
    // MAIN WORKSPACE + QUICK CREATE
    // =====================================================

    private void BuildMainArea(
        TwoPaneSplitView split)
    {
        VisualElement content =
            new VisualElement();

        content.style.flexDirection =
            FlexDirection.Row;

        content.style.flexGrow = 1;
        content.style.minWidth = 0;
        content.style.minHeight = 0;

        // Primary workspace

        Workspace = new ScrollView();

        Workspace.style.flexGrow = 1;
        Workspace.style.minWidth = 0;
        Workspace.style.minHeight = 0;

        Workspace.style.paddingLeft = 12;
        Workspace.style.paddingRight = 8;

        content.Add(Workspace);

        // Home-only create panel

        quickCreatePanel =
            new VisualElement();

        quickCreatePanel.style.width = 215;
        quickCreatePanel.style.flexShrink = 0;
        quickCreatePanel.style.minHeight = 0;
        quickCreatePanel.style.marginLeft = 8;

        ScrollView createScroll =
            new ScrollView();

        createScroll.style.flexGrow = 1;
        createScroll.style.minHeight = 0;

        createScroll.Add(
            SectionLabel("CREATE NEW")
        );

        createScroll.Add(
            QuickButton(
                "ABILITY",
                "Create an ability through " +
                "the Ability Recipe Builder.",
                () => callbacks.CreateAbility?.Invoke()
            )
        );

        createScroll.Add(
            QuickButton(
                "BUFF",
                "Create a BuffDefinition and " +
                "its initial passive effect.",
                () => callbacks.CreateBuff?.Invoke()
            )
        );

        createScroll.Add(
            QuickButton(
                "STANDALONE EFFECT",
                "Create a reusable effect through " +
                "the dynamic Effect Registry.",
                () => callbacks.CreateEffect?.Invoke()
            )
        );

        Label upcoming =
            new Label(
                "PLANNED\n" +
                "Talent · Title · Oath · Item"
            );

        upcoming.style.whiteSpace =
            WhiteSpace.Normal;

        upcoming.style.fontSize = 11;
        upcoming.style.color = MutedColor;

        upcoming.style.marginTop = 16;

        createScroll.Add(upcoming);

        quickCreatePanel.Add(createScroll);

        content.Add(quickCreatePanel);

        split.Add(content);
    }

    // =====================================================
    // HOME / EDITOR MODE
    // =====================================================

    public void SetWelcomeMode(bool isWelcome)
    {
        if (quickCreatePanel == null)
            return;

        quickCreatePanel.style.display =
            isWelcome
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    // =====================================================
    // WELCOME PAGE
    // =====================================================

    public void RenderWelcome()
    {
        if (Workspace == null)
            return;

        SetWelcomeMode(true);

        Workspace.Clear();

        // =============================================
        // HERO
        // =============================================

        VisualElement hero =
            Card();

        hero.style.minHeight = 220;
        hero.style.justifyContent =
            Justify.Center;

        hero.style.alignItems =
            Align.Center;

        Label symbol =
            new Label("RöK");

        symbol.style.fontSize = 40;
        symbol.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        hero.Add(symbol);

        Label title =
            new Label("WELCOME TO THE FORGE");

        title.style.fontSize = 16;
        title.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        title.style.marginTop = 10;

        hero.Add(title);

        Label description =
            new Label(
                "Create, shape and refine " +
                "the world of RöK."
            );

        description.style.color =
            MutedColor;

        description.style.whiteSpace =
            WhiteSpace.Normal;

        description.style.unityTextAlign =
            TextAnchor.MiddleCenter;

        description.style.marginTop = 8;

        hero.Add(description);

        Workspace.Add(hero);

        // =============================================
        // RECENT CONTENT
        // =============================================

        VisualElement recent =
            Card();

        recent.style.marginTop = 12;

        recent.Add(
            SectionLabel("RECENTLY OPENED")
        );

        int count = 0;

        foreach (string guid in recentGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            ScriptableObject asset =
                AssetDatabase.LoadAssetAtPath<
                    ScriptableObject>(path);

            if (asset == null)
                continue;

            ScriptableObject captured = asset;

            Button recentButton =
                new Button(
                    () => callbacks.OpenAsset?.Invoke(captured)
                )
                {
                    text =
                        $"{asset.name}  " +
                        $"[{asset.GetType().Name}]"
                };

            recentButton.tooltip = path;

            recentButton.style.marginTop = 4;

            recent.Add(recentButton);

            count++;
        }

        if (count == 0)
        {
            Label empty =
                new Label(
                    "Your recently opened content " +
                    "will appear here."
                );

            empty.style.color = MutedColor;
            empty.style.marginTop = 8;

            recent.Add(empty);
        }

        Workspace.Add(recent);
    }

    // =====================================================
    // RECENT ASSET HISTORY
    // =====================================================

    public void RememberAsset(
        ScriptableObject asset)
    {
        if (asset == null)
            return;

        string path =
            AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(path))
            return;

        string guid =
            AssetDatabase.AssetPathToGUID(path);

        if (string.IsNullOrEmpty(guid))
            return;

        recentGuids.Remove(guid);

        recentGuids.Insert(0, guid);

        while (recentGuids.Count > MaxRecentAssets)
        {
            recentGuids.RemoveAt(
                recentGuids.Count - 1
            );
        }

        EditorPrefs.SetString(
            RecentKey,
            string.Join("|", recentGuids)
        );
    }

    private void LoadRecent()
    {
        recentGuids.Clear();

        string stored =
            EditorPrefs.GetString(
                RecentKey,
                string.Empty
            );

        if (string.IsNullOrEmpty(stored))
            return;

        string[] guids =
            stored.Split('|');

        foreach (string guid in guids)
        {
            if (string.IsNullOrWhiteSpace(guid))
                continue;

            if (recentGuids.Contains(guid))
                continue;

            recentGuids.Add(guid);

            if (recentGuids.Count >= MaxRecentAssets)
                break;
        }
    }

    // =====================================================
    // PRESENTATION HELPERS
    // =====================================================

    private static Button QuickButton(
        string title,
        string tooltip,
        Action action)
    {
        Button button =
            new Button(action)
            {
                text = title
            };

        button.tooltip = tooltip;

        button.style.height = 48;
        button.style.marginTop = 10;

        button.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        return button;
    }

    private static Label SectionLabel(
        string text)
    {
        Label label =
            new Label(text);

        label.style.fontSize = 11;

        label.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        label.style.marginBottom = 8;

        return label;
    }

    private static VisualElement Card()
    {
        VisualElement card =
            new VisualElement();

        card.style.backgroundColor =
            CardColor;

        card.style.paddingLeft = 16;
        card.style.paddingRight = 16;
        card.style.paddingTop = 14;
        card.style.paddingBottom = 14;

        card.style.borderTopLeftRadius = 7;
        card.style.borderTopRightRadius = 7;
        card.style.borderBottomLeftRadius = 7;
        card.style.borderBottomRightRadius = 7;

        card.style.borderTopWidth = 1;
        card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1;
        card.style.borderRightWidth = 1;

        card.style.borderTopColor = BorderColor;
        card.style.borderBottomColor = BorderColor;
        card.style.borderLeftColor = BorderColor;
        card.style.borderRightColor = BorderColor;

        return card;
    }

    private static Color CardColor =>
        EditorGUIUtility.isProSkin
            ? new Color(0.19f, 0.20f, 0.22f)
            : new Color(0.90f, 0.91f, 0.92f);

    private static Color BorderColor =>
        EditorGUIUtility.isProSkin
            ? new Color(0.30f, 0.31f, 0.33f)
            : new Color(0.75f, 0.76f, 0.78f);

    private static Color MutedColor =>
        EditorGUIUtility.isProSkin
            ? new Color(0.67f, 0.69f, 0.72f)
            : new Color(0.36f, 0.39f, 0.43f);
}

#endif

