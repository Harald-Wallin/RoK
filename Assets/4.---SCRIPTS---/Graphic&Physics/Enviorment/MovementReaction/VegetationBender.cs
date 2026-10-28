using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class VegetationBender :
    MonoBehaviour
{
    [Header("Visual")]

    [SerializeField]
    [Tooltip(
        "Transformen som faktiskt ska svaja. " +
        "Om tom används objektets egen Transform."
    )]
    private Transform visualTransform;

    [Header("Bending")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur många grader vegetationen maximalt böjs."
    )]
    private float maxBendAngle = 10f;

    [SerializeField]
    [Tooltip(
        "Om karaktären huvudsakligen går uppåt eller nedåt " +
        "väljs slumpmässigt vänster eller höger."
    )]
    private bool randomizeVerticalMovement = true;

    [Header("Spring")]

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Hur starkt vegetationen fjädrar tillbaka."
    )]
    private float springStrength = 45f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur mycket fjäderrörelsen dämpas."
    )]
    private float damping = 8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Extra knuff när vegetationen släpps. " +
        "Ger ett tydligare rassel/svaj."
    )]
    private float releaseKick = 3.5f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Maximal tid som vegetationen får fortsätta " +
        "svaja efter att sista karaktären lämnat den."
    )]
    private float maximumSpringDuration = 1.25f;

    [Header("Sleep")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "När vinkeln är mindre än detta räknas plantan " +
        "som nästan stilla."
    )]
    private float sleepAngleThreshold = 0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "När rotationshastigheten är mindre än detta " +
        "kan plantan gå tillbaka till vila."
    )]
    private float sleepVelocityThreshold = 0.5f;

    private readonly Dictionary<
        Transform,
        Vector3>
        interactorPositions =
            new();

    private Coroutine springRoutine;

    private float currentAngle;
    private float angularVelocity;

    private Quaternion baseLocalRotation;

    private void Awake()
    {
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        baseLocalRotation =
            visualTransform.localRotation;
    }

    private void OnDisable()
    {
        if (springRoutine != null)
        {
            StopCoroutine(
                springRoutine
            );

            springRoutine = null;
        }

        interactorPositions.Clear();

        currentAngle = 0f;
        angularVelocity = 0f;

        ApplyRotation();
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        Transform interactor =
            ResolveInteractor(
                other
            );

        if (interactor == null)
            return;

        if (!interactorPositions.ContainsKey(
                interactor))
        {
            interactorPositions.Add(
                interactor,
                interactor.position
            );
        }

        StopSpring();
    }

    private void OnTriggerStay2D(
        Collider2D other)
    {
        Transform interactor =
            ResolveInteractor(
                other
            );

        if (interactor == null)
            return;

        if (!interactorPositions.TryGetValue(
                interactor,
                out Vector3 previousPosition))
        {
            interactorPositions[
                interactor
            ] = interactor.position;

            return;
        }

        Vector3 currentPosition =
            interactor.position;

        Vector2 movement =
            currentPosition -
            previousPosition;

        interactorPositions[
            interactor
        ] = currentPosition;

        if (movement.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        BendFromMovement(
            movement
        );
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        Transform interactor =
            ResolveInteractor(
                other
            );

        if (interactor == null)
            return;

        interactorPositions.Remove(
            interactor
        );

        /*
         * Börja inte fjädra tillbaka förrän
         * sista karaktären lämnat plantan.
         */
        if (interactorPositions.Count > 0)
            return;

        StartSpring();
    }

    private void BendFromMovement(
        Vector2 movement)
    {
        float horizontalDirection;

        bool primarilyVertical =
            Mathf.Abs(movement.y) >
            Mathf.Abs(movement.x);

        if (primarilyVertical &&
            randomizeVerticalMovement)
        {
            horizontalDirection =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }
        else
        {
            if (Mathf.Abs(movement.x) <
                0.0001f)
            {
                return;
            }

            horizontalDirection =
                Mathf.Sign(
                    movement.x
                );
        }

        /*
         * Positiv Z-rotation lutar åt vänster,
         * därför inverteras rörelseriktningen.
         */
        float targetAngle =
            -horizontalDirection *
            maxBendAngle;

        /*
         * Sätt plantan direkt i böjt läge.
         * Ingen coroutine behövs medan en
         * karaktär fortfarande går genom den.
         */
        currentAngle =
            targetAngle;

        angularVelocity = 0f;

        StopSpring();

        ApplyRotation();
    }

    private void StartSpring()
    {
        if (Mathf.Abs(currentAngle) <=
            sleepAngleThreshold)
        {
            ReturnToRest();
            return;
        }

        StopSpring();

        /*
         * Första knuffen går tillbaka genom
         * viloläget så att plantan får ett
         * tydligt rassel:
         *
         * höger -> vänster -> höger -> vila
         */
        angularVelocity =
            -currentAngle *
            releaseKick;

        springRoutine =
            StartCoroutine(
                SpringBack()
            );
    }

    private IEnumerator SpringBack()
    {
        float elapsedTime = 0f;

        while (elapsedTime <
               maximumSpringDuration)
        {
            /*
             * Om någon går in igen behöver
             * efterrörelsen inte fortsätta.
             */
            if (interactorPositions.Count > 0)
            {
                springRoutine = null;
                yield break;
            }

            float deltaTime =
                Time.deltaTime;

            elapsedTime +=
                deltaTime;

            float acceleration =
                -currentAngle *
                springStrength;

            acceleration -=
                angularVelocity *
                damping;

            angularVelocity +=
                acceleration *
                deltaTime;

            currentAngle +=
                angularVelocity *
                deltaTime;

            ApplyRotation();

            bool angleAtRest =
                Mathf.Abs(
                    currentAngle
                ) <=
                sleepAngleThreshold;

            bool velocityAtRest =
                Mathf.Abs(
                    angularVelocity
                ) <=
                sleepVelocityThreshold;

            if (angleAtRest &&
                velocityAtRest)
            {
                ReturnToRest();

                springRoutine = null;

                yield break;
            }

            yield return null;
        }

        /*
         * Säkerhetsgräns:
         * även om fjädervärdena är konstiga
         * får plantan aldrig animera i flera
         * sekunder utan visuell nytta.
         */
        ReturnToRest();

        springRoutine = null;
    }

    private void StopSpring()
    {
        if (springRoutine == null)
            return;

        StopCoroutine(
            springRoutine
        );

        springRoutine = null;
    }

    private void ReturnToRest()
    {
        currentAngle = 0f;
        angularVelocity = 0f;

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (visualTransform == null)
            return;

        visualTransform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(
                0f,
                0f,
                currentAngle
            );
    }

    private static Transform ResolveInteractor(
        Collider2D other)
    {
        if (other == null ||
            other.isTrigger)
        {
            return null;
        }

        CharacterStats character =
            other.GetComponentInParent<
                CharacterStats>();

        if (character == null)
            return null;

        return character.transform;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        maxBendAngle =
            Mathf.Max(
                0f,
                maxBendAngle
            );

        springStrength =
            Mathf.Max(
                0.01f,
                springStrength
            );

        damping =
            Mathf.Max(
                0f,
                damping
            );

        releaseKick =
            Mathf.Max(
                0f,
                releaseKick
            );

        maximumSpringDuration =
            Mathf.Max(
                0.05f,
                maximumSpringDuration
            );

        sleepAngleThreshold =
            Mathf.Max(
                0f,
                sleepAngleThreshold
            );

        sleepVelocityThreshold =
            Mathf.Max(
                0f,
                sleepVelocityThreshold
            );
    }

#endif
}