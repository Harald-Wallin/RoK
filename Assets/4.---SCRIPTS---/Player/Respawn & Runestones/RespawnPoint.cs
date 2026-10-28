using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Header("Spawn Offset")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, -1.5f, 0f);

    [Header("Spawn Settings")]
    public bool isDefaultSpawn = false;

    [Header("Optional")]
    public string spawnName = "Runestone";

    public Vector3 GetSpawnPosition()
    {
        return transform.position + spawnOffset;
    }
}
