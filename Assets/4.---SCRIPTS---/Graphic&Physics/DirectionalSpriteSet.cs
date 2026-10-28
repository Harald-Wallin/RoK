using UnityEngine;

[System.Serializable]
public class DirectionalSpriteSet
{
    public Sprite[] down;
    public Sprite[] left;
    public Sprite[] right;
    public Sprite[] up;

    public Sprite GetSprite(Vector2 dir, int frame)
    {
        Sprite[] set;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            set = dir.x > 0 ? right : left;
        else
            set = dir.y > 0 ? up : down;

        if (set == null || set.Length == 0)
            return null;

        frame %= set.Length;

        return set[frame];
    }

    public int GetFrameCount(Vector2 dir)
    {
        Sprite[] set;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            set = dir.x > 0 ? right : left;
        else
            set = dir.y > 0 ? up : down;

        return set != null ? set.Length : 0;
    }
}
