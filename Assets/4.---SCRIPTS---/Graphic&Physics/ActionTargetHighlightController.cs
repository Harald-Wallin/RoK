using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synkroniserar action-previewns AffectedTargets med targets
/// visuella TargetHighlight-komponenter.
/// </summary>
[RequireComponent(typeof(CharacterActionController))]
public sealed class ActionTargetHighlightController :
    MonoBehaviour
{
    [Header("Highlight")]

    [SerializeField]
    private Color affectedTargetColor =
        new Color(
            1f,
            0.65f,
            0.35f,
            1f
        );

    [SerializeField]
    [Tooltip(
        "Skapar automatiskt TargetHighlight på targets som " +
        "saknar komponenten. Manuellt tillagda komponenter " +
        "rekommenderas när en specifik Visual Root behövs."
    )]
    private bool addMissingHighlightComponents = true;

    private readonly HashSet<TargetHighlight>
        currentHighlights =
            new();

    private readonly HashSet<TargetHighlight>
        nextHighlights =
            new();

    private CharacterActionController actionController;

    private void Awake()
    {
        actionController =
            GetComponent<CharacterActionController>();
    }

    private void OnEnable()
    {
        if (actionController == null)
        {
            actionController =
                GetComponent<CharacterActionController>();
        }

        if (actionController == null)
            return;

        actionController.OnPreviewStarted +=
            HandlePreviewStarted;

        actionController.OnTargetingUpdated +=
            HandleTargetingUpdated;

        actionController.OnPhaseChanged +=
            HandlePhaseChanged;

        actionController.OnActionCancelled +=
            HandleActionEnded;

        actionController.OnActionCompleted +=
            HandleActionEnded;
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.OnPreviewStarted -=
                HandlePreviewStarted;

            actionController.OnTargetingUpdated -=
                HandleTargetingUpdated;

            actionController.OnPhaseChanged -=
                HandlePhaseChanged;

            actionController.OnActionCancelled -=
                HandleActionEnded;

            actionController.OnActionCompleted -=
                HandleActionEnded;
        }

        ClearAllHighlights();
    }

    private void HandlePreviewStarted(
        ActionContext context)
    {
        RefreshHighlights(
            context
        );
    }

    private void HandleTargetingUpdated(
        ActionContext context)
    {
        if (context == null ||
            context.Phase != ActionPhase.Preview)
        {
            return;
        }

        RefreshHighlights(
            context
        );
    }

    private void HandlePhaseChanged(
        ActionContext context,
        ActionPhase phase)
    {
        if (phase != ActionPhase.Preview)
        {
            ClearAllHighlights();
        }
    }

    private void HandleActionEnded(
        ActionContext context)
    {
        ClearAllHighlights();
    }

    private void RefreshHighlights(
        ActionContext context)
    {
        nextHighlights.Clear();

        if (context != null)
        {
            IReadOnlyList<GameObject> targets =
                context.AffectedTargets;

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                GameObject target =
                    targets[i];

                TargetHighlight highlight =
                    ResolveHighlight(
                        target
                    );

                if (highlight == null)
                    continue;

                nextHighlights.Add(
                    highlight
                );
            }
        }

        foreach (
            TargetHighlight previousHighlight
            in currentHighlights)
        {
            if (previousHighlight == null)
                continue;

            if (nextHighlights.Contains(
                    previousHighlight))
            {
                continue;
            }

            previousHighlight.ClearHighlight();
        }

        foreach (
            TargetHighlight nextHighlight
            in nextHighlights)
        {
            if (nextHighlight == null)
                continue;

            nextHighlight.SetHighlight(
                affectedTargetColor
            );
        }

        currentHighlights.Clear();

        foreach (
            TargetHighlight highlight
            in nextHighlights)
        {
            if (highlight != null)
            {
                currentHighlights.Add(
                    highlight
                );
            }
        }
    }

    private TargetHighlight ResolveHighlight(
        GameObject target)
    {
        if (target == null)
            return null;

        TargetHighlight highlight =
            target.GetComponent<TargetHighlight>();

        if (highlight != null)
            return highlight;

        highlight =
            target.GetComponentInChildren<
                TargetHighlight
            >(true);

        if (highlight != null)
            return highlight;

        highlight =
            target.GetComponentInParent<
                TargetHighlight
            >();

        if (highlight != null)
            return highlight;

        if (!addMissingHighlightComponents)
            return null;

        return target.AddComponent<
            TargetHighlight
        >();
    }

    private void ClearAllHighlights()
    {
        foreach (
            TargetHighlight highlight
            in currentHighlights)
        {
            if (highlight != null)
            {
                highlight.ClearHighlight();
            }
        }

        currentHighlights.Clear();
        nextHighlights.Clear();
    }
}
