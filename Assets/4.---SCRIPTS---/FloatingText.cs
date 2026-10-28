using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public float moveUpSpeed = 8f;
    public float lifetime = 1.2f;

    private float timer;
    private Vector3 moveDirection;
    private CanvasGroup canvasGroup;

    private bool isCritText;
    private float critHoldTimer;
    private Vector3 originalTextScale;

    private float critPopTimer;
    private const float critPopDuration = 0.15f;

    void Awake()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();

        FloatingTextSorting.Apply(transform, text);

        timer = lifetime;

        originalTextScale =
            text.transform.localScale;

        moveDirection = new Vector3(
            Random.Range(-0.3f, 0.3f),
            1f,
            0f
        ).normalized;
    }

    void Update()
    {
        //Debug.Log($"Crit:{isCritText} Timer:{timer}");
        if (isCritText)
        {
            HandleCritAnimation();
        }
        else
        {
            transform.Translate(
                moveDirection *
                moveUpSpeed *
                Time.deltaTime
            );
        }

        timer -= Time.deltaTime;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                Mathf.Clamp01(
                    timer / lifetime
                );
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void Setup( string value,FloatingTextStyle style)
    {
        bool isCrit =
        style == FloatingTextStyle.PlayerCrit ||
        style == FloatingTextStyle.EnemyCrit;

        text.text =
            isCrit
                ? $"<b>{value}</b>"
                : value;

        switch (style)
        {
            case FloatingTextStyle.PlayerDamage:
                text.color = Color.white;
                break;

            case FloatingTextStyle.EnemyDamage:
                text.color = Color.red;/*new Color(
                    1f,
                    0.35f,
                    0.35f
                );*/
                break;

            case FloatingTextStyle.PlayerCrit:
                SetupCritStyle(
                    new Color(1f,1f ,1f)
                );
                break;

            case FloatingTextStyle.EnemyCrit:
                SetupCritStyle(
                    new Color(
                        1f,
                        0.55f,
                        0.45f
                    )
                );
                break;

            case FloatingTextStyle.Miss:
                text.color = Color.gray;
                text.fontSize *= 0.7f;
                break;

            case FloatingTextStyle.Evade:
                text.color = Color.gray;
                text.fontSize *= 0.7f;
                break;

            case FloatingTextStyle.Block:
                text.color = new Color(
                    0.5f,
                    0.8f,
                    1f
                );

                text.fontSize *= 0.9f;
                break;
        }
    }

    void SetupCritStyle(Color color)
    {

        isCritText = true;

        critHoldTimer = 0.25f;

        text.color = color;

        text.fontSize *= 2f;

        lifetime += 0.4f;

        critPopTimer = critPopDuration;

        text.transform.localScale = originalTextScale * 0.5f;

        //Debug.Log($"Lifetime: {lifetime}");
        //Debug.Log($"Timer: {timer}");
    }

    void HandleCritAnimation()
    {
        // POP ANIMATION
        if (critPopTimer > 0f)
        {
            critPopTimer -= Time.deltaTime;

            float t =  1f - (critPopTimer / critPopDuration);

            float scale;

            if (t < 0.5f)
            {
                scale = Mathf.Lerp(
                    0.5f,
                    2.0f,
                    t / 0.5f
                );
            }
            else
            {
                scale = Mathf.Lerp(
                    2.0f,
                    1.2f,
                    (t - 0.5f) / 0.5f
                );
            }

            text.transform.localScale = originalTextScale * scale;

            return;
        }

        // HOLD MOMENT
        if (critHoldTimer > 0f)
        {
            critHoldTimer -= Time.deltaTime;

            text.transform.localScale =
                originalTextScale * 1.2f;

            return;
        }

        // FLY AWAY
        transform.Translate(
            moveDirection *
            moveUpSpeed *
            Time.deltaTime
        );
    }
}






