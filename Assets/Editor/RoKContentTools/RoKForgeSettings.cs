
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Persistent visual preferences for The Forge.
///
/// Stores only editor appearance settings.
/// Never modifies gameplay content.
/// </summary>
public sealed class RoKForgeSettings : ScriptableObject
{
    private const string AssetPath =
        "Assets/Editor/RoKContentTools/" +
        "RoKForgeSettings.asset";

    [SerializeField]
    private UnityEngine.Object heroArtwork;

    [SerializeField, Range(0f, 0.85f)]
    private float heroDarkening = 0.45f;

    public UnityEngine.Object HeroArtwork =>
        heroArtwork;

    public float HeroDarkening =>
        heroDarkening;

    // =====================================================
    // LOAD OR CREATE
    // =====================================================

    public static RoKForgeSettings LoadOrCreate()
    {
        RoKForgeSettings settings =
            AssetDatabase.LoadAssetAtPath<
                RoKForgeSettings>(AssetPath);

        if (settings != null)
            return settings;

        settings =
            CreateInstance<RoKForgeSettings>();

        AssetDatabase.CreateAsset(
            settings,
            AssetPath
        );

        AssetDatabase.SaveAssets();

        return settings;
    }

    // =====================================================
    // ARTWORK
    // =====================================================

    public void SetHeroArtwork(
        UnityEngine.Object artwork)
    {
        if (artwork != null)
        {
            if (artwork is not Texture2D &&
                artwork is not Sprite)
            {
                throw new ArgumentException(
                    "Artwork must be a Texture2D or Sprite."
                );
            }

            if (!AssetDatabase.Contains(artwork))
            {
                throw new ArgumentException(
                    "Artwork must be a saved project asset."
                );
            }
        }

        if (heroArtwork == artwork)
            return;

        Undo.RecordObject(
            this,
            "Change Forge Artwork"
        );

        heroArtwork = artwork;

        Save();
    }

    // =====================================================
    // IMAGE DARKENING
    // =====================================================

    public void SetHeroDarkening(float value)
    {
        value = Mathf.Clamp(value, 0f, 0.85f);

        if (Mathf.Approximately(heroDarkening, value))
            return;

        Undo.RecordObject(
            this,
            "Change Forge Artwork Darkening"
        );

        heroDarkening = value;

        Save();
    }

    // =====================================================
    // SAVE
    // =====================================================

    private void Save()
    {
        EditorUtility.SetDirty(this);

        AssetDatabase.SaveAssets();
    }
}

#endif

