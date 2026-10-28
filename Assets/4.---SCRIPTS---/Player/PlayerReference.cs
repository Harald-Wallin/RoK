using UnityEngine;

public class PlayerReference : MonoBehaviour
{
    public static PlayerStats Player { get; private set; }

    private void Awake()
    {
        Player = GetComponent<PlayerStats>();
    }
}
