
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visual presentation of The Forge.
///
/// Fixed layout, restrained fantasy styling,
/// and customizable home artwork.
///
/// No gameplay responsibilities.
/// </summary>
public static class RoKForgeVisualStyle
{
    // =====================================================
    // THEME PALETTE
    // =====================================================

    private static Color Background =>
        new(0.095f, 0.095f, 0.10f);

    private static Color Surface =>
        new(0.145f, 0.14f, 0.14f);

    private static Color Raised =>
        new(0.20f, 0.18f, 0.17f);

    private static Color HeroSurface =>
        new(0.18f, 0.145f, 0.125f);

    private static Color Bronze =>
        new(0.78f, 0.49f, 0.28f);

    private static Color Border =>
        new(0.32f, 0.24f, 0.19f);

    private static Color TextColor =>
        new(0.93f, 0.89f, 0.84f);

    private static Color Muted =>
        new(0.66f, 0.65f, 0.64f);

    private static Color Selection =>
        new(0.30f, 0.21f, 0.16f);

    private static Color Hover =>
        new(0.24f, 0.205f, 0.18f);

    // =====================================================
    // MAIN WINDOW THEME
    // =====================================================

    public static void ApplyShell(
        VisualElement root,
        ScrollView library,
        ScrollView workspace,
        Label footer)
    {
        if (root == null)
            return;

        root.style.backgroundColor = Background;


        // Keep the home content as tall as its viewport.

        if (workspace != null &&
            workspace.contentViewport != null)
        {
            workspace.contentViewport
                .RegisterCallback<GeometryChangedEvent>(
                    evt =>
                    {
                        VisualElement content =
                            workspace.contentContainer;

                        if (!content.ClassListContains("forge-home"))
                            return;

                        if (evt.newRect.height > 0f)
                        {
                            content.style.minHeight =
                                evt.newRect.height;
                        }
                    }
                );
        }


        // =============================================
        // HEADER
        // =============================================

        if (root.childCount > 0)
        {
            VisualElement header = root[0];

            header.style.backgroundColor = Surface;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;

            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Border;

            foreach (
                Button button
                in header.Query<Button>().ToList())
            {
                button.style.color = TextColor;
                button.style.backgroundColor = Raised;

                button.style.borderTopColor = Border;
                button.style.borderBottomColor = Border;
                button.style.borderLeftColor = Border;
                button.style.borderRightColor = Border;

                if (button.text == "THE FORGE")
                {
                    button.style.color = Bronze;
                }
            }
        }

        // =============================================
        // LIBRARY
        // =============================================

        if (library != null &&
            library.parent != null)
        {
            ApplyPanel(library.parent);
        }

        // =============================================
        // WORKSPACE + CREATE PANEL
        // =============================================

        if (workspace != null)
        {
            workspace.style.backgroundColor =
                Background;

            VisualElement parent =
                workspace.parent;

            if (parent != null)
            {
                foreach (
                    VisualElement child
                    in parent.Children())
                {
                    if (child == workspace)
                        continue;

                    ApplyPanel(child);

                    StyleCreateButtons(child);
                }
            }
        }

        // =============================================
        // FOOTER
        // =============================================

        if (footer != null)
        {
            footer.style.color = Muted;
            footer.style.backgroundColor = Surface;

            footer.style.paddingTop = 6;
            footer.style.paddingBottom = 6;

            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Border;
        }
    }

    // =====================================================
    // CREATE BUTTONS
    // =====================================================

    private static void StyleCreateButtons(
        VisualElement panel)
    {
        int index = 0;

        foreach (
            Button button
            in panel.Query<Button>().ToList())
        {
            if (button.text == "ABILITY")
                button.text = "✦   ABILITY";

            else if (button.text == "BUFF")
                button.text = "◆   BUFF";

            else if (button.text == "STANDALONE EFFECT")
                button.text = "✧   STANDALONE EFFECT";


            else if (button.text == "ITEM")
                button.text = "✦   ITEM";

            else if (button.text == "TALENT")
                button.text = "✧   TALENT";


            button.style.backgroundColor =
                index == 0
                    ? new Color(0.28f, 0.19f, 0.14f)
                    : Raised;

            button.style.color = TextColor;

            button.style.borderTopColor = Border;
            button.style.borderBottomColor = Border;
            button.style.borderLeftColor = Border;
            button.style.borderRightColor = Border;

            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;

            index++;
        }
    }

    // =====================================================
    // HOME PAGE
    // =====================================================

    public static void StyleHome(
        ScrollView workspace)
    {
        if (workspace == null)
            return;

        VisualElement content =
            workspace.contentContainer;

        if (content.childCount == 0)
            return;


        content.AddToClassList("forge-home");

        content.style.flexDirection =
            FlexDirection.Column;

        float viewportHeight =
            workspace.contentViewport.resolvedStyle.height;

        if (viewportHeight > 0f &&
            !float.IsNaN(viewportHeight))
        {
            content.style.minHeight = viewportHeight;
        }


        RoKForgeSettings settings =
            RoKForgeSettings.LoadOrCreate();

        // =============================================
        // HERO
        // =============================================

        VisualElement hero = content[0];

        ApplyPanel(hero);

        hero.style.backgroundColor = HeroSurface;

        foreach (
            Label label
            in hero.Query<Label>().ToList())
        {
            if (label.text == "RöK")
            {
                label.style.color = Bronze;
            }
            else if (
                label.text == "WELCOME TO THE FORGE")
            {
                label.style.color = TextColor;
            }
            else
            {
                label.style.color = Muted;
            }
        }

        AddHeroArtworkControls(
            hero,
            settings
        );

        // =============================================
        // RECENT ASSETS PANEL
        // =============================================

        if (content.childCount < 2)
            return;

        VisualElement recent = content[1];


        // Push Recently Opened toward the bottom.

        VisualElement spacer = new VisualElement();

        spacer.style.flexGrow = 1;
        spacer.style.minHeight = 14;

        content.Insert(1, spacer);


        ApplyPanel(recent);

        foreach (
            Button button
            in recent.Query<Button>().ToList())
        {
            button.style.backgroundColor = Raised;
            button.style.color = TextColor;


            button.style.unityTextAlign =
                TextAnchor.MiddleLeft;

            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.overflow = Overflow.Hidden;
            button.style.textOverflow = TextOverflow.Ellipsis;

            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;

            button.displayTooltipWhenElided = true;


            button.style.borderTopColor = Border;
            button.style.borderBottomColor = Border;
            button.style.borderLeftColor = Border;
            button.style.borderRightColor = Border;
        }
    }

    // =====================================================
    // HERO ARTWORK & CUSTOMIZATION
    // =====================================================

    private static void AddHeroArtworkControls(
        VisualElement hero,
        RoKForgeSettings settings)
    {
        hero.style.position = Position.Relative;
        hero.style.overflow = Overflow.Hidden;

        // =============================================
        // BACKGROUND IMAGE
        // =============================================

        Image artworkImage = new Image
        {
            scaleMode = ScaleMode.ScaleAndCrop,
            pickingMode = PickingMode.Ignore
        };

        FillElement(artworkImage);

        hero.Insert(0, artworkImage);

        // =============================================
        // DARKENING OVERLAY
        // =============================================

        VisualElement darkening =
            new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

        FillElement(darkening);

        hero.Insert(1, darkening);

        // =============================================
        // LIVE PREVIEW
        // =============================================

        void RefreshArtwork()
        {
            artworkImage.image = null;
            artworkImage.sprite = null;

            UnityEngine.Object selected =
                settings.HeroArtwork;

            if (selected is Texture2D texture)
            {
                artworkImage.image = texture;
            }
            else if (selected is Sprite sprite)
            {
                artworkImage.sprite = sprite;
            }

            bool hasArtwork =
                selected is Texture2D ||
                selected is Sprite;

            artworkImage.style.display =
                hasArtwork
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            darkening.style.display =
                hasArtwork
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            darkening.style.backgroundColor =
                new Color(
                    0.055f,
                    0.04f,
                    0.03f,
                    settings.HeroDarkening
                );
        }

        RefreshArtwork();

        // =============================================
        // CUSTOMIZE BUTTON
        // =============================================

        Button customize =
            new Button
            {
                text = "✎",
                tooltip = "Change Forge artwork"
            };

        customize.style.position =
            Position.Absolute;

        customize.style.top = 10;
        customize.style.right = 10;

        customize.style.width = 32;
        customize.style.height = 28;

        customize.style.color = Bronze;
        customize.style.backgroundColor = Raised;

        customize.style.borderTopColor = Border;
        customize.style.borderBottomColor = Border;
        customize.style.borderLeftColor = Border;
        customize.style.borderRightColor = Border;

        customize.style.borderTopLeftRadius = 5;
        customize.style.borderTopRightRadius = 5;
        customize.style.borderBottomLeftRadius = 5;
        customize.style.borderBottomRightRadius = 5;

        hero.Add(customize);

        // =============================================
        // SMALL SETTINGS PANEL
        // =============================================

        VisualElement popup =
            new VisualElement();

        popup.style.position = Position.Absolute;

        popup.style.top = 42;
        popup.style.right = 10;
        popup.style.width = 275;

        popup.style.paddingLeft = 12;
        popup.style.paddingRight = 12;
        popup.style.paddingTop = 10;
        popup.style.paddingBottom = 10;

        popup.style.backgroundColor = Surface;

        popup.style.borderTopWidth = 1;
        popup.style.borderBottomWidth = 1;
        popup.style.borderLeftWidth = 1;
        popup.style.borderRightWidth = 1;

        popup.style.borderTopColor = Bronze;
        popup.style.borderBottomColor = Border;
        popup.style.borderLeftColor = Border;
        popup.style.borderRightColor = Border;

        popup.style.borderTopLeftRadius = 6;
        popup.style.borderTopRightRadius = 6;
        popup.style.borderBottomLeftRadius = 6;
        popup.style.borderBottomRightRadius = 6;

        popup.style.display = DisplayStyle.None;

        Label heading =
            new Label("HERO ARTWORK");

        heading.style.color = Bronze;

        heading.style.unityFontStyleAndWeight =
            FontStyle.Bold;

        heading.style.marginBottom = 8;

        popup.Add(heading);

        // =============================================
        // IMAGE PICKER
        // =============================================

        ObjectField imageField =
            new ObjectField("Image")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false,
                value = settings.HeroArtwork
            };

        imageField.tooltip =
            "Choose a Texture2D or Sprite " +
            "from your Unity project.";

        imageField.RegisterValueChangedCallback(
            evt =>
            {
                UnityEngine.Object chosen =
                    evt.newValue;

                if (chosen != null &&
                    chosen is not Texture2D &&
                    chosen is not Sprite)
                {
                    imageField.SetValueWithoutNotify(
                        settings.HeroArtwork
                    );

                    EditorUtility.DisplayDialog(
                        "Invalid Artwork",
                        "Choose a Texture2D or Sprite.",
                        "OK"
                    );

                    return;
                }

                try
                {
                    settings.SetHeroArtwork(chosen);

                    RefreshArtwork();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    imageField.SetValueWithoutNotify(
                        settings.HeroArtwork
                    );
                }
            }
        );

        popup.Add(imageField);

        // =============================================
        // IMAGE DARKENING
        // =============================================

        Slider opacity =
            new Slider(
                "Darken image",
                0f,
                0.85f
            )
            {
                value = settings.HeroDarkening
            };

        opacity.style.marginTop = 10;

        opacity.RegisterValueChangedCallback(
            evt =>
            {
                settings.SetHeroDarkening(
                    evt.newValue
                );

                RefreshArtwork();
            }
        );

        popup.Add(opacity);

        // =============================================
        // RESET + CLOSE
        // =============================================

        VisualElement buttons =
            new VisualElement();

        buttons.style.flexDirection =
            FlexDirection.Row;

        buttons.style.marginTop = 10;

        Button reset =
            new Button(
                () =>
                {
                    settings.SetHeroArtwork(null);

                    imageField.SetValueWithoutNotify(
                        null
                    );

                    RefreshArtwork();
                }
            )
            {
                text = "Reset Image"
            };

        reset.style.flexGrow = 1;

        Button close =
            new Button(
                () =>
                {
                    popup.style.display =
                        DisplayStyle.None;
                }
            )
            {
                text = "Close"
            };

        close.style.flexGrow = 1;

        buttons.Add(reset);
        buttons.Add(close);

        popup.Add(buttons);

        hero.Add(popup);

        // =============================================
        // TOGGLE PANEL
        // =============================================

        customize.clicked += () =>
        {
            popup.style.display =
                popup.style.display.value ==
                    DisplayStyle.None
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        };
    }

    // =====================================================
    // LIBRARY ASSET ROW
    // =====================================================

    public static Button CreateAssetRow(
        ScriptableObject asset,
        Action onClick)
    {
        Button row =
            new Button(onClick);

        row.userData = asset;

        row.style.flexDirection =
            FlexDirection.Row;

        row.style.alignItems =
            Align.Center;

        row.style.minHeight = 44;
        row.style.marginBottom = 3;

        row.style.paddingLeft = 7;
        row.style.paddingRight = 7;

        row.tooltip =
            AssetDatabase.GetAssetPath(asset);

        // =============================================
        // ICON
        // =============================================

        VisualElement iconBox =
            new VisualElement();

        iconBox.style.width = 31;
        iconBox.style.height = 31;
        iconBox.style.marginRight = 8;
        iconBox.style.flexShrink = 0;

        iconBox.style.alignItems = Align.Center;
        iconBox.style.justifyContent = Justify.Center;

        iconBox.style.backgroundColor = Raised;

        Sprite icon = ReadIcon(asset);

        if (icon != null)
        {
            Image iconImage =
                new Image
                {
                    sprite = icon,
                    scaleMode = ScaleMode.ScaleToFit
                };

            iconImage.style.width = 28;
            iconImage.style.height = 28;

            iconBox.Add(iconImage);
        }
        else
        {
            Label initials =
                new Label(GetAbbreviation(asset));

            initials.style.color = Bronze;
            initials.style.fontSize = 11;

            initials.style.unityFontStyleAndWeight =
                FontStyle.Bold;

            iconBox.Add(initials);
        }

        row.Add(iconBox);

        // =============================================
        // ASSET DETAILS
        // =============================================

        VisualElement details =
            new VisualElement();


        details.style.flexGrow = 1;
        details.style.flexShrink = 1;
        details.style.minWidth = 0;
        details.style.overflow = Overflow.Hidden;


        Label name =
            new Label(asset.name);

        name.style.color = TextColor;
        name.style.fontSize = 12;

        name.style.whiteSpace =
            WhiteSpace.NoWrap;

        name.style.overflow = Overflow.Hidden;
        name.style.textOverflow = TextOverflow.Ellipsis;
        name.style.unityTextAlign = TextAnchor.MiddleLeft;
        name.displayTooltipWhenElided = true;


        Label type =
            new Label(asset.GetType().Name);

        type.style.color = Muted;
        type.style.fontSize = 10;

        type.style.whiteSpace = WhiteSpace.NoWrap;
        type.style.overflow = Overflow.Hidden;
        type.style.textOverflow = TextOverflow.Ellipsis;


        details.Add(name);
        details.Add(type);

        row.Add(details);

        SetSelected(row, false);

        // =============================================
        // HOVER
        // =============================================

        row.RegisterCallback<MouseEnterEvent>(
            _ =>
            {
                if (!row.ClassListContains(
                    "forge-selected"))
                {
                    row.style.backgroundColor = Hover;
                }
            }
        );

        row.RegisterCallback<MouseLeaveEvent>(
            _ =>
            {
                SetSelected(
                    row,
                    row.ClassListContains(
                        "forge-selected"
                    )
                );
            }
        );

        return row;
    }

    // =====================================================
    // LIBRARY SELECTION
    // =====================================================

    public static void UpdateLibrarySelection(
        ScrollView library,
        ScriptableObject selectedAsset)
    {
        if (library == null)
            return;

        foreach (
            VisualElement element
            in library.contentContainer.Children())
        {
            if (element is not Button row)
                continue;

            if (row.userData is not ScriptableObject asset)
                continue;

            SetSelected(
                row,
                asset == selectedAsset
            );
        }
    }

    private static void SetSelected(
        Button row,
        bool selected)
    {
        row.EnableInClassList(
            "forge-selected",
            selected
        );

        row.style.backgroundColor =
            selected ? Selection : Surface;

        row.style.borderLeftWidth = 3;
        row.style.borderLeftColor =
            selected ? Bronze : Border;

        row.style.borderTopColor = Border;
        row.style.borderBottomColor = Border;
        row.style.borderRightColor = Border;
    }

    // =====================================================
    // ICON LOOKUP
    // =====================================================

    private static Sprite ReadIcon(
        ScriptableObject asset)
    {
        SerializedObject serialized =
            new SerializedObject(asset);

        SerializedProperty icon =
            serialized.FindProperty("icon");

        return icon?.objectReferenceValue as Sprite;
    }

    private static string GetAbbreviation(
        ScriptableObject asset)
    {
        if (asset is AbilityData)
            return "AB";

        if (asset is BuffDefinition)
            return "BU";

        if (asset is AbilityEffect)
            return "AC";

        if (asset is EffectDefinition)
            return "EF";

        if (asset is TalentData)
            return "TA";


        if (asset is ItemData)
            return "IT";


        return "RK";
    }

    // =====================================================
    // SHARED STYLING
    // =====================================================

    private static void ApplyPanel(
        VisualElement element)
    {
        element.style.backgroundColor = Surface;

        element.style.borderTopWidth = 1;
        element.style.borderBottomWidth = 1;
        element.style.borderLeftWidth = 1;
        element.style.borderRightWidth = 1;

        element.style.borderTopColor = Border;
        element.style.borderBottomColor = Border;
        element.style.borderLeftColor = Border;
        element.style.borderRightColor = Border;

        element.style.borderTopLeftRadius = 6;
        element.style.borderTopRightRadius = 6;
        element.style.borderBottomLeftRadius = 6;
        element.style.borderBottomRightRadius = 6;
    }

    private static void FillElement(
        VisualElement element)
    {
        element.style.position = Position.Absolute;

        element.style.left = 0;
        element.style.right = 0;
        element.style.top = 0;
        element.style.bottom = 0;
    }
}

#endif

