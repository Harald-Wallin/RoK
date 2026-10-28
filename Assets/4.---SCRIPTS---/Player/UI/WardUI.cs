using UnityEngine;

public class WardUI : MonoBehaviour
{
    private WardSlotUI[] slots;

    private WardSystem wardSystem;

    void Start()
    {
        PlayerStats player = PlayerReference.Player;
        slots = GetComponentsInChildren<WardSlotUI>();

        if (player == null)
            return;

        wardSystem =
            player.GetComponent<WardSystem>();

        if (wardSystem == null)
            return;

        wardSystem.OnWardChanged += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (wardSystem != null)
        {
            wardSystem.OnWardChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (wardSystem == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            bool unlocked = i < wardSystem.VisibleWardSlots;

            slots[i].gameObject.SetActive(
                unlocked
            );

            if (!unlocked)
                continue;

            bool filled =
                i < wardSystem.CurrentWard;

            slots[i].SetFilled(
                filled
            );
        }
    }
}
