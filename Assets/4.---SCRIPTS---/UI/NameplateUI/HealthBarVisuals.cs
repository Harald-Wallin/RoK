using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HealthBarVisual :
    MonoBehaviour
{
    [Header("Images")]

    [Tooltip(
        "Den riktiga nuvarande health-fillen.")]
    [SerializeField]
    private Image healthFill;

    [Tooltip(
        "FIllen bakom health-fillen som visar nyligen förlorad HP.")]
    [SerializeField]
    private Image damageTrailFill;


    [Header("Damage Trail")]

    [Tooltip(
        "Hur länge den vita damage-markeringen ligger kvar " +
        "innan den börjar sjunka.")]
    [SerializeField, Min(0f)]
    private float damageTrailDelay =
        0.25f;

    [Tooltip(
        "Hur lång tid damage-markeringen tar på sig att " +
        "sjunka till aktuell HP.")]
    [SerializeField, Min(0.01f)]
    private float damageTrailDuration =
        0.45f;


    private Coroutine damageTrailRoutine;

    private bool initialized;

    private float displayedHealth;


    private void Awake()
    {
        ConfigureImage(
            healthFill
        );

        ConfigureImage(
            damageTrailFill
        );
    }


    private void OnDisable()
    {
        StopDamageTrail();
    }


    public void SetHealth(
        float currentHealth,
        float maximumHealth,
        bool immediate = false)
    {
        float normalized =
            maximumHealth > 0f
                ? Mathf.Clamp01(
                    currentHealth /
                    maximumHealth
                )
                : 0f;


        if (!initialized ||
            immediate)
        {
            initialized =
                true;

            displayedHealth =
                normalized;

            StopDamageTrail();

            SetFillAmount(
                healthFill,
                normalized
            );

            SetFillAmount(
                damageTrailFill,
                normalized
            );

            return;
        }


        float previousHealth =
            displayedHealth;

        displayedHealth =
            normalized;


        /*
         * Den riktiga HP-baren ändras omedelbart.
         */
        SetFillAmount(
            healthFill,
            normalized
        );


        if (damageTrailFill == null)
            return;


        /*
         * Healing / ökad HP:
         *
         * Ingen damage-trail behövs.
         * Båda fills går direkt till nya värdet.
         */
        if (normalized >=
            previousHealth)
        {
            StopDamageTrail();

            damageTrailFill.fillAmount =
                normalized;

            return;
        }


        /*
         * DAMAGE:
         *
         * Behåll trailen på föregående HP-nivå.
         *
         * Om ytterligare damage kommer medan trailen redan
         * sjunker får vi aldrig dra den bakåt/uppåt på ett
         * konstigt sätt.
         */
        damageTrailFill.fillAmount =
            Mathf.Max(
                damageTrailFill.fillAmount,
                previousHealth
            );


        StopDamageTrail();

        damageTrailRoutine =
            StartCoroutine(
                AnimateDamageTrail(
                    normalized
                )
            );
    }


    public void SetHealthColor(
    Color color)
    {
        if (healthFill == null)
            return;

        color.a =
            1f;

        healthFill.color =
            color;
    }


    private IEnumerator AnimateDamageTrail(
        float targetAmount)
    {
        if (damageTrailDelay > 0f)
        {
            yield return new WaitForSeconds(
                damageTrailDelay
            );
        }


        float startAmount =
            damageTrailFill != null
                ? damageTrailFill.fillAmount
                : targetAmount;

        float elapsed =
            0f;


        while (elapsed <
               damageTrailDuration)
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    damageTrailDuration
                );

            /*
             * Lite mjukare än en helt linjär transition.
             */
            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (damageTrailFill != null)
            {
                damageTrailFill.fillAmount =
                    Mathf.Lerp(
                        startAmount,
                        targetAmount,
                        eased
                    );
            }

            yield return null;
        }


        if (damageTrailFill != null)
        {
            damageTrailFill.fillAmount =
                targetAmount;
        }

        damageTrailRoutine =
            null;
    }


    private void StopDamageTrail()
    {
        if (damageTrailRoutine == null)
            return;

        StopCoroutine(
            damageTrailRoutine
        );

        damageTrailRoutine =
            null;
    }


    private static void SetFillAmount(
        Image image,
        float amount)
    {
        if (image == null)
            return;

        image.fillAmount =
            Mathf.Clamp01(
                amount
            );
    }


    private static void ConfigureImage(
        Image image)
    {
        if (image == null)
            return;

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Horizontal;

        image.fillOrigin =
            (int)Image.OriginHorizontal.Left;
    }
}

