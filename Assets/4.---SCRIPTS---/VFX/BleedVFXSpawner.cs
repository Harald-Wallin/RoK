using UnityEngine;

public class BleedVFXSpawner : MonoBehaviour
{
    public static BleedVFXSpawner Instance;

    [SerializeField] private GameObject bleedPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void Spawn(Vector3 position)
    {
        if (bleedPrefab == null)
            return;

        GameObject obj = Instantiate(
            bleedPrefab,
            position,
            Quaternion.identity
        );

        Destroy(obj, 2f);
    }
}
