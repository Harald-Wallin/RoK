using UnityEngine;

public static class ReputationColorUtility
{
    public static Color GetColor(int level)
    {
        switch (level)
        {
            case 1: return new Color(0.49f, 0.02f, 0.02f);        // Hated
            case 2: return new Color(0.92f, 0.56f, 0.07f);        // Untrusted
            case 3: return new Color(1f, 0.74f, 0.08f);           // Neutral
            case 4: return new Color(0.44f, 0.67f, 0.25f);        // Favoured
            case 5: return new Color(0.36f, 0.63f, 0.12f);        // Renowned
            case 6: return new Color(0.28f, 0.56f, 0.03f);        // Praised
            case 7: return new Color(0.2f, 0.43f, 0f);            // Oathbearer
            case 8: return new Color(0.65f, 0.17f, 0.64f);        // Revered
            case 9: return new Color(0.486f, 0.174f, 0.651f);     // Paragon
            default: return Color.white;
        }
    }
}
