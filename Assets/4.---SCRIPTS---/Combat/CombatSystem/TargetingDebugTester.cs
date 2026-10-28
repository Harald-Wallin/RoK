using UnityEngine;

/// <summary>
/// Tillfälligt testverktyg för den nya targeting-pipelinen.
///
/// Verktyget skapar ActionRequest, kör TargetResolver och visar
/// resultatet med Gizmos utan att exekvera någon ability.
///
/// Komponenten ska senare tas bort eller flyttas till ett
/// separat utvecklings-/testsystem.
/// </summary>
public sealed class TargetingDebugTester :
    MonoBehaviour
{
    [Header("Request")]

    [SerializeField]
    private CharacterStats caster;

    [SerializeField]
    private AbilityData ability;

    [SerializeField]
    [Tooltip(
        "Används av SingleTarget och kan lämnas tom för " +
        "andra targetingformer."
    )]
    private GameObject explicitTarget;

    [SerializeField]
    [Tooltip(
        "Transformens position används som RequestedAimPoint."
    )]
    private Transform aimPoint;

    [SerializeField]
    private Vector2 requestedDirection =
        Vector2.down;

    [Header("Runtime")]

    [SerializeField]
    private bool resolveContinuously;

    [SerializeField]
    private KeyCode resolveKey =
        KeyCode.T;

    [SerializeField]
    [Min(1)]
    private int geometryBufferSize = 128;

    [Header("Logging")]

    [SerializeField]
    private bool logSuccessfulResults = true;

    [SerializeField]
    private bool logInvalidResults = true;

    [Header("Gizmos")]

    [SerializeField]
    private bool drawGizmos = true;

    [SerializeField]
    [Min(0.01f)]
    private float targetMarkerRadius = 0.2f;

    private TargetResolver resolver;
    private TargetingResult lastResult;

    public TargetingResult LastResult =>
        lastResult;

    private void Awake()
    {
        CreateResolver();
    }

    private void OnValidate()
    {
        geometryBufferSize =
            Mathf.Max(
                1,
                geometryBufferSize
            );

        targetMarkerRadius =
            Mathf.Max(
                0.01f,
                targetMarkerRadius
            );
    }

    private void Update()
    {
        if (resolveContinuously ||
            Input.GetKeyDown(resolveKey))
        {
            ResolveNow();
        }
    }

    /// <summary>
    /// Kör targetingberäkningen utan att exekvera abilityn.
    /// Kan även anropas från komponentens context menu.
    /// </summary>
    [ContextMenu("Resolve Targeting")]
    public void ResolveNow()
    {
        if (resolver == null)
            CreateResolver();

        if (caster == null)
        {
            Debug.LogWarning(
                "TargetingDebugTester saknar Caster.",
                this
            );

            lastResult = null;
            return;
        }

        if (ability == null)
        {
            Debug.LogWarning(
                "TargetingDebugTester saknar Ability.",
                this
            );

            lastResult = null;
            return;
        }

        Vector2 resolvedAimPoint =
            aimPoint != null
                ? (Vector2)aimPoint.position
                : (Vector2)transform.position;

        ActionRequest request =
            new ActionRequest(
                caster,
                ability,
                resolvedAimPoint,
                explicitTarget,
                requestedDirection
            );

        lastResult =
            resolver.Resolve(request);

        LogResult(lastResult);
    }

    /// <summary>
    /// Tar bort det senast sparade debugresultatet.
    /// </summary>
    [ContextMenu("Clear Targeting Result")]
    public void ClearResult()
    {
        lastResult = null;
    }

    private void CreateResolver()
    {
        resolver =
            new TargetResolver(
                geometryBufferSize
            );
    }

    private void LogResult(
        TargetingResult result)
    {
        if (result == null)
        {
            Debug.LogWarning(
                "TargetResolver returnerade inget resultat.",
                this
            );

            return;
        }

        if (result.IsValid)
        {
            if (!logSuccessfulResults)
                return;

            Debug.Log(
                BuildLogMessage(result),
                this
            );

            return;
        }

        if (!logInvalidResults)
            return;

        Debug.LogWarning(
            BuildLogMessage(result),
            this
        );
    }

    private static string BuildLogMessage(
        TargetingResult result)
    {
        string primaryTargetName =
            result.PrimaryTarget != null
                ? result.PrimaryTarget.name
                : "None";

        return
            $"Targeting result\n" +
            $"Valid: {result.IsValid}\n" +
            $"Failure: {result.FailureReason}\n" +
            $"Mode: {result.Settings?.TargetingMode}\n" +
            $"Origin: {result.Origin}\n" +
            $"Raw aim: {result.RawAimPoint}\n" +
            $"Target point: {result.TargetPoint}\n" +
            $"Direction: {result.Direction}\n" +
            $"Distance: {result.Distance:0.###}\n" +
            $"Geometry targets: " +
            $"{result.GeometryTargets.Count}\n" +
            $"Valid targets: " +
            $"{result.ValidTargets.Count}\n" +
            $"Affected targets: " +
            $"{result.AffectedTargets.Count}\n" +
            $"Primary target: {primaryTargetName}";
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos ||
            lastResult == null ||
            lastResult.Settings == null)
        {
            return;
        }

        DrawOrigin();
        DrawAimPoints();
        DrawTargetingShape();
        DrawTargetCollections();
    }

    private void DrawOrigin()
    {
        Gizmos.color = Color.white;

        Gizmos.DrawWireSphere(
            lastResult.Origin,
            targetMarkerRadius
        );
    }

    private void DrawAimPoints()
    {
        Gizmos.color = Color.gray;

        Gizmos.DrawLine(
            lastResult.Origin,
            lastResult.RawAimPoint
        );

        Gizmos.DrawWireSphere(
            lastResult.RawAimPoint,
            targetMarkerRadius * 0.75f
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            lastResult.Origin,
            lastResult.TargetPoint
        );

        Gizmos.DrawWireSphere(
            lastResult.TargetPoint,
            targetMarkerRadius
        );
    }

    private void DrawTargetingShape()
    {
        switch (lastResult.Settings.TargetingMode)
        {
            case TargetingMode.Self:
                DrawSelfShape();
                break;

            case TargetingMode.SingleTarget:
                DrawSingleTargetShape();
                break;

            case TargetingMode.Point:
                DrawPointShape();
                break;

            case TargetingMode.Circle:
                DrawCircleShape();
                break;

            case TargetingMode.Cone:
                DrawConeShape();
                break;

            case TargetingMode.Line:
                DrawLineShape();
                break;
        }
    }

    private void DrawSelfShape()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            lastResult.Origin,
            targetMarkerRadius * 1.5f
        );
    }

    private void DrawSingleTargetShape()
    {
        if (explicitTarget == null)
            return;

        GameObject resolvedTarget =
            TargetUtility.ResolveCharacterTarget(
                explicitTarget
            );

        if (resolvedTarget == null)
            return;

        Vector2 targetPosition =
            TargetUtility.GetTargetPosition(
                resolvedTarget
            );

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            lastResult.Origin,
            targetPosition
        );

        Gizmos.DrawWireSphere(
            targetPosition,
            targetMarkerRadius * 1.5f
        );
    }

    private void DrawPointShape()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            lastResult.TargetPoint,
            targetMarkerRadius * 1.5f
        );
    }

    private void DrawCircleShape()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            lastResult.TargetPoint,
            lastResult.Settings.Radius
        );
    }

    private void DrawConeShape()
    {
        float range =
            lastResult.Settings.Range;

        float halfAngle =
            lastResult.Settings.ConeAngle *
            0.5f;

        Vector2 direction =
            lastResult.Direction;

        Vector2 leftDirection =
            Rotate(
                direction,
                halfAngle
            );

        Vector2 rightDirection =
            Rotate(
                direction,
                -halfAngle
            );

        Vector2 leftPoint =
            lastResult.Origin +
            leftDirection * range;

        Vector2 rightPoint =
            lastResult.Origin +
            rightDirection * range;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            lastResult.Origin,
            leftPoint
        );

        Gizmos.DrawLine(
            lastResult.Origin,
            rightPoint
        );

        DrawArc(
            lastResult.Origin,
            direction,
            range,
            halfAngle
        );
    }

    private void DrawLineShape()
    {
        float halfWidth =
            lastResult.Settings.LineWidth *
            0.5f;

        Vector2 direction =
            lastResult.Direction;

        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        Vector2 originLeft =
            lastResult.Origin +
            perpendicular * halfWidth;

        Vector2 originRight =
            lastResult.Origin -
            perpendicular * halfWidth;

        Vector2 endLeft =
            lastResult.TargetPoint +
            perpendicular * halfWidth;

        Vector2 endRight =
            lastResult.TargetPoint -
            perpendicular * halfWidth;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            originLeft,
            endLeft
        );

        Gizmos.DrawLine(
            endLeft,
            endRight
        );

        Gizmos.DrawLine(
            endRight,
            originRight
        );

        Gizmos.DrawLine(
            originRight,
            originLeft
        );
    }

    private void DrawTargetCollections()
    {
        DrawTargets(
            lastResult.GeometryTargets,
            Color.gray,
            targetMarkerRadius
        );

        DrawTargets(
            lastResult.ValidTargets,
            Color.yellow,
            targetMarkerRadius * 1.25f
        );

        DrawTargets(
            lastResult.AffectedTargets,
            Color.green,
            targetMarkerRadius * 1.5f
        );

        if (lastResult.PrimaryTarget == null)
            return;

        Vector2 primaryPosition =
            TargetUtility.GetTargetPosition(
                lastResult.PrimaryTarget
            );

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            primaryPosition,
            targetMarkerRadius * 2f
        );
    }

    private static void DrawTargets(
        System.Collections.Generic
            .IReadOnlyList<GameObject> targets,
        Color color,
        float radius)
    {
        if (targets == null)
            return;

        Gizmos.color = color;

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            GameObject target =
                targets[i];

            if (target == null)
                continue;

            Vector2 position =
                TargetUtility.GetTargetPosition(
                    target
                );

            Gizmos.DrawWireSphere(
                position,
                radius
            );
        }
    }

    private static Vector2 Rotate(
        Vector2 direction,
        float degrees)
    {
        float radians =
            degrees * Mathf.Deg2Rad;

        float cosine =
            Mathf.Cos(radians);

        float sine =
            Mathf.Sin(radians);

        return new Vector2(
            direction.x * cosine -
            direction.y * sine,

            direction.x * sine +
            direction.y * cosine
        );
    }

    private static void DrawArc(
        Vector2 origin,
        Vector2 direction,
        float radius,
        float halfAngle)
    {
        const int segmentCount = 24;

        Vector2 previousPoint =
            origin +
            Rotate(
                direction,
                -halfAngle
            ) * radius;

        for (int i = 1;
             i <= segmentCount;
             i++)
        {
            float progress =
                i / (float)segmentCount;

            float angle =
                Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    progress
                );

            Vector2 currentPoint =
                origin +
                Rotate(
                    direction,
                    angle
                ) * radius;

            Gizmos.DrawLine(
                previousPoint,
                currentPoint
            );

            previousPoint = currentPoint;
        }
    }
}
