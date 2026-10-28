using TMPro;
using UnityEngine;

public class CorpseLabel : MonoBehaviour
{
    public TMP_Text label;

    void Start()
    {
        if (label != null)
            label.text = "Dead";
    }
}
