
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

// =========================================================
// FORGE PAGE
// =========================================================

public enum RoKForgePage
{
    Home,
    Asset,
    CreateAbility,
    AbilityRecipes,
    EffectRegistry,
    ContentAudit,
    GuidedCreator
}

// =========================================================
// NAVIGATION ENTRY
// =========================================================

public sealed class RoKForgeRoute
{
    public RoKForgePage Page { get; }

    public ScriptableObject Asset { get; }

    private RoKForgeRoute(
        RoKForgePage page,
        ScriptableObject asset = null)
    {
        Page = page;
        Asset = asset;
    }

    public static RoKForgeRoute Home()
    {
        return new RoKForgeRoute(RoKForgePage.Home);
    }

    public static RoKForgeRoute ForPage(
        RoKForgePage page)
    {
        if (page == RoKForgePage.Asset)
        {
            throw new ArgumentException(
                "Use ForAsset() for asset navigation."
            );
        }

        return new RoKForgeRoute(page);
    }

    public static RoKForgeRoute ForAsset(
        ScriptableObject asset)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        return new RoKForgeRoute(
            RoKForgePage.Asset,
            asset
        );
    }

    public bool SameAs(RoKForgeRoute other)
    {
        return other != null &&
               Page == other.Page &&
               Asset == other.Asset;
    }

    public string Label
    {
        get
        {
            return Page switch
            {
                RoKForgePage.Home => "Home",

                RoKForgePage.Asset =>
                    Asset != null
                        ? Asset.name
                        : "Missing Asset",

                RoKForgePage.CreateAbility =>
                    "New Ability",

                RoKForgePage.AbilityRecipes =>
                    "Ability Recipes",

                RoKForgePage.EffectRegistry =>
                    "Effect Registry",

                RoKForgePage.ContentAudit =>
                    "Content Audit",

                RoKForgePage.GuidedCreator =>
                    "Guided Creator",

                _ => Page.ToString()
            };
        }
    }
}

// =========================================================
// NAVIGATION HISTORY
// =========================================================

public sealed class RoKForgeNavigation
{
    private const int MaxHistory = 50;

    private readonly List<RoKForgeRoute> history =
        new()
        {
            RoKForgeRoute.Home()
        };

    private int position;

    public IReadOnlyList<RoKForgeRoute> History =>
        history;

    public int Position => position;

    public RoKForgeRoute Current =>
        history[position];

    public bool CanGoBack =>
        position > 0;

    public bool CanGoForward =>
        position < history.Count - 1;

    public RoKForgeRoute PeekBack()
    {
        return CanGoBack
            ? history[position - 1]
            : null;
    }

    public RoKForgeRoute PeekForward()
    {
        return CanGoForward
            ? history[position + 1]
            : null;
    }

    // =====================================================
    // PUSH NEW ROUTE
    // =====================================================

    public void Push(RoKForgeRoute route)
    {
        if (route == null)
            return;

        if (Current.SameAs(route))
            return;

        // Navigating after Back discards the old
        // forward branch, like a normal browser.

        if (CanGoForward)
        {
            history.RemoveRange(
                position + 1,
                history.Count - position - 1
            );
        }

        history.Add(route);

        position = history.Count - 1;

        if (history.Count > MaxHistory)
        {
            history.RemoveAt(0);
            position--;
        }
    }

    // =====================================================
    // BACK / FORWARD / JUMP
    // =====================================================

    public RoKForgeRoute Back()
    {
        if (CanGoBack)
            position--;

        return Current;
    }

    public RoKForgeRoute Forward()
    {
        if (CanGoForward)
            position++;

        return Current;
    }

    public RoKForgeRoute JumpTo(int index)
    {
        if (index < 0 || index >= history.Count)
            return Current;

        position = index;

        return Current;
    }
}

#endif

