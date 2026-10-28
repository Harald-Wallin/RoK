using UnityEngine;
using UnityEngine.UI;

public class WardSlotUI : MonoBehaviour
{
    [SerializeField]
    private Image icon;

    [SerializeField]
    private Sprite activeSprite;

    [SerializeField]
    private Sprite inactiveSprite;

    public void SetFilled(bool filled)
    {
        if (icon == null)
            return;

        icon.sprite =
            filled
            ? activeSprite
            : inactiveSprite;
    }
}
