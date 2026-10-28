using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public RespawnPoint CurrentRespawnPoint { get; private set; }

    private RespawnPoint defaultSpawn;

    void Start()
    {
        FindDefaultSpawn();
    }

    void FindDefaultSpawn()
    {
        RespawnPoint[] all = FindObjectsByType<RespawnPoint>(
            FindObjectsSortMode.None
        );

        foreach (var point in all)
        {
            if (point.isDefaultSpawn)
            {
                defaultSpawn = point;
                CurrentRespawnPoint = point;
                return;
            }
        }

        Debug.LogError("NO DEFAULT RESPAWN POINT FOUND");
    }

    public void SetRespawnPoint(RespawnPoint point)
    {
        if (point == null)
            return;

        CurrentRespawnPoint = point;

        Debug.Log($"Respawn point set to: {point.spawnName}");
    }

    public Vector3 GetRespawnPosition()
    {
        if (CurrentRespawnPoint != null)
            return CurrentRespawnPoint.GetSpawnPosition();

        if (defaultSpawn != null)
            return defaultSpawn.GetSpawnPosition();

        return Vector3.zero;
    }
}
