using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Väljer vilka av de redan validerade targetobjekten som
/// faktiskt ska påverkas av actionen.
///
/// Klassen ansvarar endast för targeturval.
/// Den känner inte till fysik, faction, livsstatus,
/// line of sight eller targetingform.
/// </summary>
public sealed class TargetSelector
{
    private readonly List<GameObject> randomBuffer =
        new();

    private readonly System.Random random;

    /// <summary>
    /// Skapar en selector med en lokal random-generator.
    ///
    /// Den använder inte UnityEngine.Random och förändrar därför
    /// inte spelets globala random-state.
    /// </summary>
    public TargetSelector()
        : this(Environment.TickCount)
    {
    }

    /// <summary>
    /// Skapar en selector med ett explicit seed.
    ///
    /// Detta kan senare användas för deterministisk AI,
    /// multiplayer eller reproducerbara tester.
    /// </summary>
    public TargetSelector(int randomSeed)
    {
        random =
            new System.Random(randomSeed);
    }

    /// <summary>
    /// Väljer AffectedTargets från resultatets ValidTargets
    /// enligt abilityns TargetSelectionMode och MaximumTargets.
    ///
    /// ValidTargets muteras aldrig.
    /// </summary>
    public void Select(
        TargetingResult result)
    {
        if (result == null)
            return;

        result.ClearAffectedTargets();
        result.PrimaryTarget = null;

        AbilityTargetingSettings settings =
            result.Settings;

        if (settings == null)
            return;

        IReadOnlyList<GameObject> validTargets =
            result.ValidTargets;

        if (validTargets == null ||
            validTargets.Count == 0)
        {
            return;
        }

        int targetLimit =
            GetTargetLimit(
                settings.MaximumTargets,
                validTargets.Count
            );

        if (targetLimit <= 0)
            return;

        switch (settings.SelectionMode)
        {
            case TargetSelectionMode.All:
                SelectAll(
                    validTargets,
                    targetLimit,
                    result
                );
                break;

            case TargetSelectionMode.ClosestToCaster:
                SelectClosest(
                    validTargets,
                    targetLimit,
                    result.Origin,
                    result
                );
                break;

            case TargetSelectionMode.ClosestToTargetPoint:
                SelectClosest(
                    validTargets,
                    targetLimit,
                    result.TargetPoint,
                    result
                );
                break;

            case TargetSelectionMode.Random:
                SelectRandom(
                    validTargets,
                    targetLimit,
                    result
                );
                break;

            default:
                SelectAll(
                    validTargets,
                    targetLimit,
                    result
                );
                break;
        }

        if (result.AffectedTargets.Count > 0)
        {
            result.PrimaryTarget =
                result.AffectedTargets[0];
        }
    }

    /// <summary>
    /// Lägger till targets i den ordning de förekommer i
    /// ValidTargets.
    ///
    /// Listan muteras inte.
    /// </summary>
    private static void SelectAll(
        IReadOnlyList<GameObject> validTargets,
        int targetLimit,
        TargetingResult result)
    {
        int count =
            Mathf.Min(
                targetLimit,
                validTargets.Count
            );

        for (int i = 0; i < count; i++)
        {
            result.AddAffectedTarget(
                validTargets[i]
            );
        }
    }

    /// <summary>
    /// Väljer targets närmast en angiven referenspunkt.
    ///
    /// Selection-sort används medvetet här:
    /// - ingen tillfällig lista behöver allokeras
    /// - ValidTargets behöver inte sorteras eller muteras
    /// - MaximumTargets är normalt ett litet värde
    /// </summary>
    private static void SelectClosest(
        IReadOnlyList<GameObject> validTargets,
        int targetLimit,
        Vector2 referencePoint,
        TargetingResult result)
    {
        int selectedCount = 0;

        while (selectedCount < targetLimit)
        {
            GameObject closestTarget = null;
            float closestDistanceSquared =
                float.PositiveInfinity;

            int closestOriginalIndex =
                int.MaxValue;

            for (int i = 0;
                 i < validTargets.Count;
                 i++)
            {
                GameObject candidate =
                    validTargets[i];

                if (candidate == null)
                    continue;

                if (Contains(
                        result.AffectedTargets,
                        candidate))
                {
                    continue;
                }

                Vector2 closestPoint =
                    TargetUtility.GetClosestPoint(
                        candidate,
                        referencePoint
                    );

                float distanceSquared =
                    (closestPoint - referencePoint)
                    .sqrMagnitude;

                bool isCloser =
                    distanceSquared <
                    closestDistanceSquared;

                bool hasEqualDistance =
                    Mathf.Approximately(
                        distanceSquared,
                        closestDistanceSquared
                    );

                // Originalindex används som tie-breaker så att två
                // targets med samma avstånd får ett stabilt urval.
                bool winsTie =
                    hasEqualDistance &&
                    i < closestOriginalIndex;

                if (!isCloser && !winsTie)
                    continue;

                closestTarget = candidate;

                closestDistanceSquared =
                    distanceSquared;

                closestOriginalIndex = i;
            }

            if (closestTarget == null)
                break;

            result.AddAffectedTarget(
                closestTarget
            );

            selectedCount++;
        }
    }

    /// <summary>
    /// Väljer slumpmässiga unika targets utan att mutera
    /// ValidTargets.
    ///
    /// En återanvändbar buffer används för att undvika en ny
    /// lista varje gång previewn uppdateras.
    /// </summary>
    private void SelectRandom(
        IReadOnlyList<GameObject> validTargets,
        int targetLimit,
        TargetingResult result)
    {
        randomBuffer.Clear();

        for (int i = 0;
             i < validTargets.Count;
             i++)
        {
            GameObject target =
                validTargets[i];

            if (target == null)
                continue;

            if (randomBuffer.Contains(target))
                continue;

            randomBuffer.Add(target);
        }

        int count =
            Mathf.Min(
                targetLimit,
                randomBuffer.Count
            );

        // Partiell Fisher-Yates.
        //
        // Vi behöver bara slumpa de första platserna som faktiskt
        // ska användas, inte hela listan.
        for (int i = 0; i < count; i++)
        {
            int randomIndex =
                random.Next(
                    i,
                    randomBuffer.Count
                );

            GameObject selected =
                randomBuffer[randomIndex];

            randomBuffer[randomIndex] =
                randomBuffer[i];

            randomBuffer[i] =
                selected;

            result.AddAffectedTarget(
                selected
            );
        }

        randomBuffer.Clear();
    }

    /// <summary>
    /// MaximumTargets <= 0 betyder obegränsat.
    /// </summary>
    private static int GetTargetLimit(
        int maximumTargets,
        int availableTargets)
    {
        if (availableTargets <= 0)
            return 0;

        if (maximumTargets <= 0)
            return availableTargets;

        return Mathf.Min(
            maximumTargets,
            availableTargets
        );
    }

    private static bool Contains(
        IReadOnlyList<GameObject> targets,
        GameObject candidate)
    {
        if (targets == null ||
            candidate == null)
        {
            return false;
        }

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            if (targets[i] == candidate)
                return true;
        }

        return false;
    }
}
