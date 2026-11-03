
using System;
using UnityEngine;

/// <summary>
/// Presentation-only cursor for invalid ground targeting.
///
/// Displays a small red ring at RawAimPoint when
/// a Circle or Point ability is aimed out of range.
///
/// Does not affect targeting, gameplay or execution.
///
/// The marker is independent of the large AoE
/// preview hierarchy so it remains visible when
/// that geometry is hidden.
/// </summary>
public sealed class TargetingOutOfRangeCursor :
    IDisposable
{
    // =====================================================
    // CONSTANTS
    // =====================================================

    private const int TextureSize = 48;
    private const float PixelsPerUnit = 128f;

    private const float OuterRadius = 16f;
    private const float InnerRadius = 12f;

    // =====================================================
    // RUNTIME OBJECTS
    // =====================================================

    private readonly Transform owner;

    private readonly GameObject marker;
    private readonly Texture2D texture;
    private readonly Sprite sprite;

    private bool disposed;

    // =====================================================
    // CONSTRUCTION
    // =====================================================

    public TargetingOutOfRangeCursor(
        Transform owner,
        string sortingLayerName,
        int sortingOrder)
    {
        this.owner = owner;

        // Generate sprite once.
        texture = CreateRingTexture();

        sprite = Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                TextureSize,
                TextureSize
            ),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit
        );

        sprite.name =
            "Targeting Out Of Range Ring";

        // Independent root object.
        // Must NOT be a child of ActivePreviewRoot
        // or the player's SortingGroup.
        marker = new GameObject(
            "Out Of Range Cursor (Runtime)"
        );

        SpriteRenderer renderer =
            marker.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;

        renderer.sortingLayerName =
            sortingLayerName;

        renderer.sortingOrder =
            sortingOrder;

        renderer.color = Color.white;

        marker.SetActive(false);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    public void Update(
        TargetingResult targeting)
    {
        if (disposed || marker == null)
            return;

        if (targeting == null ||
            targeting.Settings == null)
        {
            Hide();
            return;
        }

        TargetingMode mode =
            targeting.Settings.TargetingMode;

        bool isGroundTargeting =
            mode == TargetingMode.Circle ||
            mode == TargetingMode.Point;

        if (!isGroundTargeting)
        {
            Hide();
            return;
        }

        // The authoritative resolver decides
        // whether the requested position is valid.
        bool isOutOfRange =
            targeting.FailureReason ==
                TargetingFailureReason.OutOfRange;

        if (!isOutOfRange)
        {
            Hide();
            return;
        }

        // RawAimPoint follows the actual mouse.
        // TargetPoint may be clamped to max range.
        Vector2 position =
            targeting.RawAimPoint;

        float z =
            owner != null
                ? owner.position.z
                : 0f;

        marker.transform.position =
            new Vector3(
                position.x,
                position.y,
                z
            );

        if (!marker.activeSelf)
        {
            marker.SetActive(true);
        }
    }

    // =====================================================
    // VISIBILITY
    // =====================================================

    public void Hide()
    {
        if (disposed || marker == null)
            return;

        if (marker.activeSelf)
        {
            marker.SetActive(false);
        }
    }

    // =====================================================
    // PROCEDURAL SPRITE
    // =====================================================

    private static Texture2D CreateRingTexture()
    {
        Texture2D result =
            new Texture2D(
                TextureSize,
                TextureSize,
                TextureFormat.RGBA32,
                false
            );

        result.name =
            "Targeting Out Of Range Texture";

        result.filterMode =
            FilterMode.Bilinear;

        result.wrapMode =
            TextureWrapMode.Clamp;

        Color red =
            new Color(
                1f,
                0.12f,
                0.12f,
                1f
            );

        float center =
            (TextureSize - 1) * 0.5f;

        Color[] pixels =
            new Color[
                TextureSize * TextureSize
            ];

        for (int y = 0;
             y < TextureSize;
             y++)
        {
            for (int x = 0;
                 x < TextureSize;
                 x++)
            {
                float dx = x - center;
                float dy = y - center;

                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy
                    );

                // Smooth anti-aliased outer edge.
                float outerAlpha =
                    Mathf.Clamp01(
                        OuterRadius +
                        0.5f -
                        distance
                    );

                // Smooth anti-aliased inner edge.
                float innerAlpha =
                    Mathf.Clamp01(
                        distance -
                        InnerRadius +
                        0.5f
                    );

                float alpha =
                    outerAlpha *
                    innerAlpha;

                pixels[
                    y * TextureSize + x
                ] = new Color(
                    red.r,
                    red.g,
                    red.b,
                    alpha
                );
            }
        }

        result.SetPixels(pixels);

        result.Apply(
            false,
            false
        );

        return result;
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        DestroyObject(marker);
        DestroyObject(sprite);
        DestroyObject(texture);
    }

    private static void DestroyObject(
        UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(
                target
            );
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(
                target
            );
        }
    }
}
