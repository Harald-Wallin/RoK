using UnityEngine;

public class DamageReaction : MonoBehaviour
{
    [SerializeField] private float pushDistance = 0.08f;
    [SerializeField] private float pushDuration = 0.05f;

    private bool reacting;

    public void PlayReaction(Vector3 damageSource)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (reacting)
            return;

        StartCoroutine(
            ReactionRoutine(damageSource)
        );
    }

    System.Collections.IEnumerator ReactionRoutine(
        Vector3 damageSource)
    {
        reacting = true;

        Vector3 startPos =
            transform.localPosition;

        Vector3 dir =
            (transform.position - damageSource)
            .normalized;

        Vector3 pushedPos =
            startPos +
            dir * pushDistance;

        float timer = 0f;

        while (timer < pushDuration)
        {
            timer += Time.deltaTime;

            transform.localPosition =
                Vector3.Lerp(
                    startPos,
                    pushedPos,
                    timer / pushDuration
                );

            yield return null;
        }

        timer = 0f;

        while (timer < pushDuration)
        {
            timer += Time.deltaTime;

            transform.localPosition =
                Vector3.Lerp(
                    pushedPos,
                    startPos,
                    timer / pushDuration
                );

            yield return null;
        }

        transform.localPosition =
            startPos;

        reacting = false;
    }
}
