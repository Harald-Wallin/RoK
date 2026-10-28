using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New World Scatter Palette",
    menuName = "World/Scatter Palette"
)]
public class WorldScatterPalette :
    ScriptableObject
{
    [SerializeField]
    private List<ScatterGroup> groups =
        new();

    public IReadOnlyList<ScatterGroup> Groups =>
        groups;
}

[Serializable]
public class ScatterGroup
{
    // =========================================================
    // GENERAL
    // =========================================================

    [SerializeField]
    private bool enabled =
        true;

    [SerializeField]
    private string groupName =
        "New Group";

    [SerializeField]
    [Range(0f, 1f)]
    private float density =
        1f;

    [SerializeField]
    private List<GameObject> prefabs =
        new();

    // =========================================================
    // VARIATION
    // =========================================================

    [Header("Variation")]

    [SerializeField]
    [Tooltip(
        "Om aktiverad får varje placerad prefab en slumpmässig " +
        "uniform storlek mellan Min Scale och Max Scale."
    )]
    private bool randomizeScale =
        true;

    [SerializeField]
    [Min(0.01f)]
    private float minScale =
        0.9f;

    [SerializeField]
    [Min(0.01f)]
    private float maxScale =
        1.1f;

    [SerializeField]
    [Tooltip(
        "Om aktiverad har varje placerad prefab 50% chans " +
        "att spegelvändas horisontellt via SpriteRenderer.flipX."
    )]
    private bool randomFlipX =
        true;

    // =========================================================
    // API
    // =========================================================

    public bool Enabled =>
        enabled;

    public string GroupName =>
        groupName;

    public float Density =>
        Mathf.Clamp01(
            density
        );

    public IReadOnlyList<GameObject> Prefabs =>
        prefabs;

    public bool RandomizeScale =>
        randomizeScale;

    public float MinScale =>
        Mathf.Max(
            0.01f,
            Mathf.Min(
                minScale,
                maxScale
            )
        );

    public float MaxScale =>
        Mathf.Max(
            MinScale,
            Mathf.Max(
                minScale,
                maxScale
            )
        );

    public bool RandomFlipX =>
        randomFlipX;

    // =========================================================
    // RANDOMIZATION
    // =========================================================

    public GameObject GetRandomPrefab()
    {
        if (prefabs == null ||
            prefabs.Count == 0)
        {
            return null;
        }

        int validCount = 0;

        for (int i = 0;
             i < prefabs.Count;
             i++)
        {
            if (prefabs[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
            return null;

        int targetIndex =
            UnityEngine.Random.Range(
                0,
                validCount
            );

        int currentIndex = 0;

        for (int i = 0;
             i < prefabs.Count;
             i++)
        {
            GameObject prefab =
                prefabs[i];

            if (prefab == null)
                continue;

            if (currentIndex ==
                targetIndex)
            {
                return prefab;
            }

            currentIndex++;
        }

        return null;
    }

    public float GetRandomScale()
    {
        if (!randomizeScale)
        {
            return 1f;
        }

        return UnityEngine.Random.Range(
            MinScale,
            MaxScale
        );
    }

    public bool GetRandomFlipX()
    {
        return
            randomFlipX &&
            UnityEngine.Random.value <
            0.5f;
    }

#if UNITY_EDITOR

    public void Normalize()
    {
        density =
            Mathf.Clamp01(
                density
            );

        minScale =
            Mathf.Max(
                0.01f,
                minScale
            );

        maxScale =
            Mathf.Max(
                minScale,
                maxScale
            );

        prefabs ??=
            new List<GameObject>();
    }

#endif
}