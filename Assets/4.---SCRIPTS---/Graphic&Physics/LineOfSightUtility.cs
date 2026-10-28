using UnityEngine;

public static class LineOfSightUtility
{
    private static readonly int losMask = LayerMask.GetMask("World");

    public static bool HasLineOfSight(
    Vector2 from,
    Vector2 to
)
    {
        Debug.DrawLine(from, to, Color.red, 1f);

        Vector2 direction = to - from;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                from,
                direction.normalized,
                distance
            );

        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.gameObject.layer ==
                LayerMask.NameToLayer("World"))
            {
                return false;
            }
        }

        return true;
    }
}
