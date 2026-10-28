using UnityEngine;

public class PlayerWindowToggle : MonoBehaviour
{
    void Start()
    {
        // Starta gömt
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}

