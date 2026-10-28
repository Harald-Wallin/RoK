using UnityEngine;

public enum FavourMarkerVisualState
{
    Hidden,
    Bronze,
    Silver,
    Gold
}

public sealed class FavourMarker :
    MonoBehaviour
{
    [Header("Visuals")]

    [SerializeField]
    private GameObject visualRoot;

    [SerializeField]
    private Transform ringTransform;

    [SerializeField]
    private SpriteRenderer ringRenderer;

    [SerializeField]
    private Transform orbitNode;

    [SerializeField]
    private SpriteRenderer orbitNodeRenderer;

    [SerializeField]
    private ParticleSystem[] orbitParticles;

    [Header("Placement")]

    [SerializeField]
    private Vector2 markerOffset =
        Vector2.zero;

    [SerializeField]
    [Min(0.01f)]
    private float markerScale =
        1f;

    [SerializeField]
    private Vector2 baseOrbitRadius =
        new Vector2(
            0.5f,
            0.18f
        );

    [Header("Orbit")]

    [SerializeField]
    private float orbitSpeed =
        100f;

    [SerializeField]
    [Range(0f, 360f)]
    private float startingAngle =
        0f;

    [Header("Local Sorting")]

    [SerializeField]
    [Tooltip(
        "Sorting Order för noden när den befinner sig " +
        "på ringens bakre halva.")]
    private int orbitBackSortingOrder =
        0;

    [SerializeField]
    [Tooltip(
        "Sorting Order för noden när den befinner sig " +
        "på ringens främre halva.")]
    private int orbitFrontSortingOrder =
        20;

    [Header("State Colours")]

    [SerializeField]
    private Color bronzeColor =
        new Color(
            0.72f,
            0.40f,
            0.16f,
            1f
        );

    [SerializeField]
    private Color silverColor =
        new Color(
            0.78f,
            0.84f,
            0.88f,
            1f
        );

    [SerializeField]
    private Color goldColor =
        new Color(
            1f,
            0.72f,
            0.16f,
            1f
        );

    [Header("Opacity")]

    [SerializeField]
    [Range(0f, 1f)]
    private float ringOpacity =
    0.25f;

    [SerializeField]
    [Range(0f, 1f)]
    private float nodeOpacity =
        1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float particleOpacity =
        1f;

    private FavourMarkerVisualState
        currentState =
            FavourMarkerVisualState.Hidden;

    private float currentAngle;

    private ParticleSystemRenderer[]
        particleRenderers;

    public FavourMarkerVisualState CurrentState =>
        currentState;

    private void Awake()
    {
        currentAngle =
            startingAngle;

        CacheParticleRenderers();

        ApplyLayout();

        SetState(
            FavourMarkerVisualState.Hidden,
            true
        );
    }

    private void Update()
    {
        if (currentState ==
            FavourMarkerVisualState.Hidden)
        {
            return;
        }

        UpdateOrbit();
    }

    public void SetState(
        FavourMarkerVisualState state)
    {
        SetState(
            state,
            false
        );
    }

    private void SetState(
        FavourMarkerVisualState state,
        bool force)
    {
        if (!force &&
            currentState == state)
        {
            return;
        }

        currentState =
            state;

        bool visible =
            state !=
            FavourMarkerVisualState.Hidden;

        if (visualRoot != null)
        {
            visualRoot.SetActive(
                visible
            );
        }

        if (!visible)
        {
            StopParticles();
            return;
        }

        Color colour =
            GetStateColour(
                state
            );

        ApplyColour(
            colour
        );

        UpdateOrbit();
        RestartParticles();
    }

    private void CacheParticleRenderers()
    {
        if (orbitParticles == null)
        {
            particleRenderers =
                System.Array.Empty<
                    ParticleSystemRenderer>();

            return;
        }

        particleRenderers =
            new ParticleSystemRenderer[
                orbitParticles.Length
            ];

        for (int i = 0;
             i < orbitParticles.Length;
             i++)
        {
            ParticleSystem particles =
                orbitParticles[i];

            if (particles == null)
                continue;

            particleRenderers[i] =
                particles.GetComponent<
                    ParticleSystemRenderer>();
        }
    }

    private void UpdateOrbit()
    {
        if (orbitNode == null)
            return;

        currentAngle +=
            orbitSpeed *
            Time.deltaTime;

        if (currentAngle >= 360f)
        {
            currentAngle -=
                360f;
        }
        else if (currentAngle < 0f)
        {
            currentAngle +=
                360f;
        }

        float radians =
            currentAngle *
            Mathf.Deg2Rad;

        float radiusX =
            baseOrbitRadius.x *
            markerScale;

        float radiusY =
            baseOrbitRadius.y *
            markerScale;

        Vector2 orbitOffset =
            new Vector2(
                Mathf.Cos(radians) *
                radiusX,

                Mathf.Sin(radians) *
                radiusY
            );

        orbitNode.localPosition =
            new Vector3(
                markerOffset.x +
                orbitOffset.x,

                markerOffset.y +
                orbitOffset.y,

                orbitNode.localPosition.z
            );

        UpdateOrbitSorting(
            orbitOffset.y
        );
    }

    private void UpdateOrbitSorting(
    float localOrbitY)
    {
        int sortingOrder =
            localOrbitY > 0f
                ? orbitBackSortingOrder
                : orbitFrontSortingOrder;

        if (orbitNodeRenderer != null)
        {
            orbitNodeRenderer.sortingOrder =
                sortingOrder;
        }

        if (particleRenderers == null)
            return;

        foreach (ParticleSystemRenderer renderer
                 in particleRenderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingOrder =
                sortingOrder;
        }
    }

    private void ApplyLayout()
    {
        if (ringTransform != null)
        {
            ringTransform.localPosition =
                new Vector3(
                    markerOffset.x,
                    markerOffset.y,
                    ringTransform
                        .localPosition.z
                );

            ringTransform.localScale =
                Vector3.one *
                markerScale;
        }
    }

    private void ApplyColour(
        Color colour)
    {
        if (ringRenderer != null)
        {
            Color ringColour =
                colour;

            ringColour.a *=
                ringOpacity;

            ringRenderer.color =
                ringColour;
        }

        if (orbitNodeRenderer != null)
        {
            Color nodeColour =
                colour;

            nodeColour.a *=
                nodeOpacity;

            orbitNodeRenderer.color =
                nodeColour;
        }

        if (orbitParticles == null)
            return;

        Color particleColour =
            colour;

        particleColour.a *=
            particleOpacity;

        foreach (ParticleSystem particles
                 in orbitParticles)
        {
            if (particles == null)
                continue;

            ParticleSystem.MainModule main =
                particles.main;

            main.startColor =
                particleColour;
        }
    }



    private Color GetStateColour(
        FavourMarkerVisualState state)
    {
        switch (state)
        {
            case FavourMarkerVisualState
                .Bronze:

                return bronzeColor;

            case FavourMarkerVisualState
                .Silver:

                return silverColor;

            case FavourMarkerVisualState
                .Gold:

                return goldColor;

            default:
                return Color.white;
        }
    }

    private void RestartParticles()
    {
        if (orbitParticles == null)
            return;

        foreach (ParticleSystem particles
                 in orbitParticles)
        {
            if (particles == null)
                continue;

            particles.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );

            particles.Play(
                true
            );
        }
    }

    private void StopParticles()
    {
        if (orbitParticles == null)
            return;

        foreach (ParticleSystem particles
                 in orbitParticles)
        {
            if (particles == null)
                continue;

            particles.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }
    }

    public void AnchorToColliderBottom(
    Collider2D targetCollider)
    {
        if (targetCollider == null)
            return;

        Bounds bounds =
            targetCollider.bounds;

        Vector3 worldPosition =
            new Vector3(
                bounds.center.x,
                bounds.min.y,
                transform.position.z
            );

        Transform parent =
            transform.parent;

        if (parent != null)
        {
            Vector3 localPosition =
                parent.InverseTransformPoint(
                    worldPosition
                );

            localPosition.z =
                transform.localPosition.z;

            transform.localPosition =
                localPosition;
        }
        else
        {
            transform.position =
                worldPosition;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        markerScale =
            Mathf.Max(
                0.01f,
                markerScale
            );

        ApplyLayout();
    }
#endif
}
