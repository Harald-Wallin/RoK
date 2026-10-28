using UnityEngine;

public class SpellbookTabButton : MonoBehaviour
{
    [SerializeField]
    private float selectedScale = 1.08f;

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void SetSelected(bool selected)
    {
        transform.localScale =
            selected
            ? originalScale * selectedScale
            : originalScale;
    }
}




//KAN ANVÄNDAS OM MAN TAR BORT VERTICAL LAYOUT GROUP OCH PLACERAR TABSEN MANUELLT
/*using UnityEngine;

public class SpellbookTabButton : MonoBehaviour
{
    [SerializeField]
    private float selectedOffset = -5f;

    private Vector2 originalPos;

    void Start()
    {
        originalPos =
            GetComponent<RectTransform>()
            .anchoredPosition;

        Debug.Log($"{name} original pos = {originalPos}");
    }


    public void SetSelected(bool selected)
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        rect.anchoredPosition =
            selected
            ? originalPos + Vector2.left * selectedOffset
            : originalPos;
    }
}
*/