using UnityEngine;

public class HumanoidEquipment : MonoBehaviour
{
    [Header("Equipped Items")]
    public ItemData head;
    public ItemData chest;
    public ItemData shoulders;
    public ItemData back;
    public ItemData legs;
    public ItemData feet;
    public ItemData weapon;
    public ItemData offhand;

    [Header("Sprite Renderers")]
    public SpriteRenderer headRenderer;
    public SpriteRenderer chestRenderer;
    public SpriteRenderer shouldersRenderer;
    public SpriteRenderer backRenderer;
    public SpriteRenderer legsRenderer;
    public SpriteRenderer feetRenderer;
    public SpriteRenderer weaponRenderer;
    public SpriteRenderer offhandRenderer;

    private CharacterStats stats;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        ApplyAll();
    }

    private void ApplyAll()
    {
        ApplyVisual(head);
        ApplyVisual(chest);
        ApplyVisual(shoulders);
        ApplyVisual(back);
        ApplyVisual(legs);
        ApplyVisual(feet);
        ApplyVisual(weapon);
        ApplyVisual(offhand);
    }

    public void Equip(ItemData item)
    {
        if (item == null)
            return;

        switch (item.itemType)
        {
            case ItemType.Helmet: head = item; break;
            case ItemType.Chest: chest = item; break;
            case ItemType.Shoulders: shoulders = item; break;
            case ItemType.Back: back = item; break;
            case ItemType.Legs: legs = item; break;
            case ItemType.Feet: feet = item; break;
            case ItemType.Weapon: weapon = item; break;
            case ItemType.Offhand: offhand = item; break;
        }

        ApplyVisual(item);

        UpdateVisualDirection(Vector2.down);
    }

    public void Unequip(ItemData item)
    {
        if (item == null) return;

        SpriteRenderer renderer = GetRendererForItem(item);

        if (renderer != null)
            renderer.sprite = null;

        switch (item.itemType)
        {
            case ItemType.Helmet: head = null; break;
            case ItemType.Chest: chest = null; break;
            case ItemType.Shoulders: shoulders = null; break;
            case ItemType.Back: back = null; break;
            case ItemType.Legs: legs = null; break;
            case ItemType.Feet: feet = null; break;
            case ItemType.Weapon: weapon = null; break;
            case ItemType.Offhand: offhand = null; break;
        }

        RestoreHiddenParts();
        UpdateVisualDirection(lastDirection);

        HumanoidVisualController visual = GetComponentInChildren<HumanoidVisualController>();

        if (visual != null)
        {
            UpdateVisualDirection(visual.CurrentFacing);
        }
    }

    void ApplyVisual(ItemData item)
    {
        if (item == null)
            return;

        SpriteRenderer renderer = GetRendererForItem(item);

        if (renderer == null)
            return;

        renderer.sprite = item.frontSprite;

        HumanoidVisualController visual =
            GetComponentInChildren<HumanoidVisualController>();

        if (visual != null)
        {
            if (item.hideHair) visual.SetHairVisible(false);
            if (item.hideBeard) visual.SetBeardVisible(false);
            if (item.hideTorso) visual.SetTorsoVisible(false);
            if (item.hideArms) visual.SetArmsVisible(false);
            if (item.hideHead) visual.SetHeadVisible(false);
            if (item.hideLegs) visual.SetLegsVisible(false);
            if (item.hideFeet) visual.SetFeetVisible(false);
        }
    }

    SpriteRenderer GetRendererForItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Helmet: return headRenderer;
            case ItemType.Chest: return chestRenderer;
            case ItemType.Shoulders: return shouldersRenderer;
            case ItemType.Back: return backRenderer;
            case ItemType.Legs: return legsRenderer;
            case ItemType.Feet: return feetRenderer;
            case ItemType.Weapon: return weaponRenderer;
        }

        return null;
    }

    private Vector2 lastDirection = Vector2.down;

    public void UpdateVisualDirection(Vector2 dir)
    {
        if (dir == Vector2.zero)
            dir = lastDirection;

        lastDirection = dir;

        UpdateItemVisual(head, headRenderer, dir);
        UpdateItemVisual(chest, chestRenderer, dir);
        UpdateItemVisual(shoulders, shouldersRenderer, dir);
        UpdateItemVisual(back, backRenderer, dir);
        UpdateItemVisual(legs, legsRenderer, dir);
        UpdateItemVisual(feet, feetRenderer, dir);
        UpdateItemVisual(weapon, weaponRenderer, dir);
        UpdateItemVisual(offhand, offhandRenderer, dir);

        UpdateSorting(dir);
    }

    void UpdateItemVisual(ItemData item, SpriteRenderer renderer, Vector2 dir)
    {
        if (item == null)
            return;

        if (renderer == null)
            return;

        Sprite sprite = GetDirectionalSprite(item, dir);

        renderer.sprite = sprite;
    }

    Sprite GetDirectionalSprite(ItemData item, Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? item.rightSprite : item.leftSprite;
        else
            return dir.y > 0 ? item.backSprite : item.frontSprite;
    }

    void UpdateSorting(Vector2 dir)
    {
        bool facingUp = dir.y > 0.5f;

        if (facingUp)
        {
            // FACE UP

            if (backRenderer != null)
                backRenderer.sortingOrder = 7;

            if (shouldersRenderer != null)
                shouldersRenderer.sortingOrder = 8;
        }
        else
        {
            // FACE DOWN / SIDE

            if (backRenderer != null)
                backRenderer.sortingOrder = 0;

            if (shouldersRenderer != null)
                shouldersRenderer.sortingOrder = 7;
        }

        if (headRenderer != null)
            headRenderer.sortingOrder = 9;
    }

    void RestoreHiddenParts()
    {
        HumanoidVisualController visual =
            GetComponentInChildren<HumanoidVisualController>();

        if (visual == null)
            return;

        visual.SetHairVisible(true);
        visual.SetBeardVisible(true);
        visual.SetTorsoVisible(true);
        visual.SetArmsVisible(true);
        visual.SetHeadVisible(true);
        visual.SetLegsVisible(true);
        visual.SetFeetVisible(true);
    }

    void OnDisable()
    {}

    void OnEnable()
    {}
}
