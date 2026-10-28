using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    public enum PatrolMode
    {
        PingPong,
        Loop
    }

    public PatrolMode patrolMode = PatrolMode.PingPong;

    public List<PatrolPoint> points = new();

    private void OnDrawGizmos()
    {
        if (points == null || points.Count < 2)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i] == null ||
                points[i + 1] == null)
                continue;

            Gizmos.DrawLine(
                points[i].transform.position,
                points[i + 1].transform.position
            );
        }

        if (patrolMode == PatrolMode.Loop)
        {
            Gizmos.DrawLine(
                points[points.Count - 1].transform.position,
                points[0].transform.position
            );
        }
    }
}
