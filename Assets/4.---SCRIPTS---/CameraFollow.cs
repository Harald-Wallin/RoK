
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;

    private bool isFrozen;

    public bool IsFrozen => isFrozen;

    public void Freeze()
    {
        isFrozen = true;
    }

    public void Unfreeze()
    {
        isFrozen = false;
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            -10f
        );
    }

    private void LateUpdate()
    {
        if (isFrozen || target == null)
            return;

        Vector3 desiredPosition =
            new Vector3(
                target.position.x,
                target.position.y,
                -10f
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
    }
}
