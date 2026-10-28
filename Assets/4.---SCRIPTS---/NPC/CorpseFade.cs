using System.Collections;
using UnityEngine;

public class CorpseFade : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float fadeDuration = 2f;

    private SpriteRenderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        // ⏳ Vänta innan fade
        yield return new WaitForSeconds(lifetime);

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);

            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}


