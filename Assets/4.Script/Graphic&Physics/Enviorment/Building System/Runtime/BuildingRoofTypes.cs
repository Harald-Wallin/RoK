using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum BuildingRoofDirection
{
    EastWest = 0,
    NorthSouth = 1
}

public enum BuildingRoofPieceRole
{
    Surface,
    Ridge,
    FrontOverhang,
    SideOverhang,
    SideOverhangBottom,
    GableOrnament,

    GableFill,
    GableSlope,
    GableBottomOverhang,
    WindboardTop,
    WindboardMiddle,
    WindboardBottom
}

public enum BuildingSpriteAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

[Serializable]
public sealed class BuildingSpriteVariantSet
{
    [SerializeField]
    private List<Sprite> variants = new();

    public bool HasSprites
    {
        get
        {
            if (variants == null)
                return false;

            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i] != null)
                    return true;
            }

            return false;
        }
    }

    public Sprite GetDeterministic(int hash)
    {
        if (variants == null || variants.Count == 0)
            return null;

        int validCount = 0;

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int target = PositiveModulo(hash, validCount);
        int current = 0;

        for (int i = 0; i < variants.Count; i++)
        {
            Sprite sprite = variants[i];

            if (sprite == null)
                continue;

            if (current == target)
                return sprite;

            current++;
        }

        return null;
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}

/// <summary>
/// Gröna E/W-taket. Fältnamnen och strukturen behålls för att
/// befintliga RoofProfile-assets ska fortsätta fungera.
/// </summary>
[Serializable]
public sealed class BuildingRoofArtSet
{
    [Header("Layout")]

    [SerializeField]
    [Min(1)]
    private int tileLengthPixels = 64;

    [SerializeField]
    [Min(1)]
    private int rowAdvancePixels = 32;

    [Header("Main Roof Variants")]

    [SerializeField]
    private BuildingSpriteVariantSet surfaceVariants = new();

    [Header("Ridge Variants")]

    [SerializeField]
    private BuildingSpriteVariantSet ridgeVariants = new();

    [SerializeField]
    [Min(0)]
    private int ridgeOverlapPixels = 2;

    [Header("Gable Ornament Variants")]

    [SerializeField]
    private BuildingSpriteVariantSet gableOrnamentVariants = new();

    [Header("Front / Bottom Overhang Variants")]

    [SerializeField]
    private BuildingSpriteVariantSet frontOverhangVariants = new();

    [Header("Side Overhang Variants")]

    [SerializeField]
    private BuildingSpriteVariantSet sideOverhangVariants = new();

    [SerializeField]
    private BuildingSpriteVariantSet sideOverhangBottomVariants = new();

    [Header("Sorting")]

    [SerializeField]
    private int baseSortingOrder = 10;

    public int TileLengthPixels => Mathf.Max(1, tileLengthPixels);
    public int RowAdvancePixels => Mathf.Max(1, rowAdvancePixels);
    public int RidgeOverlapPixels => Mathf.Max(0, ridgeOverlapPixels);
    public int BaseSortingOrder => baseSortingOrder;

    public BuildingSpriteVariantSet SurfaceVariants => surfaceVariants;
    public BuildingSpriteVariantSet RidgeVariants => ridgeVariants;
    public BuildingSpriteVariantSet GableOrnamentVariants => gableOrnamentVariants;
    public BuildingSpriteVariantSet FrontOverhangVariants => frontOverhangVariants;
    public BuildingSpriteVariantSet SideOverhangVariants => sideOverhangVariants;
    public BuildingSpriteVariantSet SideOverhangBottomVariants => sideOverhangBottomVariants;
}

/// <summary>
/// N/S-taket har ett eget artwork-kontrakt och en egen geometri.
/// Inga manuella offsets behövs i Inspectorn.
///
/// Förväntade V1-assets:
/// - Main roof: 64x128
/// - Ridge: 6x68
/// - Bottom roof overhang: 21x82
/// - Gable fill: 64x32
/// - Gable slope: 32x32 (vänster sida; höger flipas)
/// - Gable bottom overhang: 64x8
/// - Windboard top: 32x32
/// - Windboard middle: 71x64
/// - Windboard bottom: 32x32
/// </summary>
[Serializable]
public sealed class BuildingNorthSouthRoofArtSet
{
    [Header("Main Roof Variants - 64x128")]

    [SerializeField]
    private BuildingSpriteVariantSet surfaceVariants = new();

    [Header("Ridge Variants - 6x68")]

    [SerializeField]
    private BuildingSpriteVariantSet ridgeVariants = new();

    [Header("Outer / Bottom Roof Overhang Variants - 21x82")]

    [SerializeField]
    private BuildingSpriteVariantSet bottomOverhangVariants = new();

    [Header("Procedural Front Gable")]

    [SerializeField]
    private BuildingSpriteVariantSet gableFillVariants = new();

    [SerializeField]
    private BuildingSpriteVariantSet gableSlopeVariants = new();

    [SerializeField]
    private BuildingSpriteVariantSet gableBottomOverhangVariants = new();

    [Header("Windboards")]

    [SerializeField]
    private BuildingSpriteVariantSet windboardTopVariants = new();

    [SerializeField]
    private BuildingSpriteVariantSet windboardMiddleVariants = new();

    [SerializeField]
    private BuildingSpriteVariantSet windboardBottomVariants = new();

    [Header("Sorting")]

    [SerializeField]
    [Tooltip(
        "Basorder för N/S roof pieces. Generatorn räknar själv fram " +
        "front/back- och slope-ordning ovanpå detta värde."
    )]
    private int baseSortingOrder = 10;

    public int BaseSortingOrder => baseSortingOrder;

    public BuildingSpriteVariantSet SurfaceVariants => surfaceVariants;
    public BuildingSpriteVariantSet RidgeVariants => ridgeVariants;
    public BuildingSpriteVariantSet BottomOverhangVariants => bottomOverhangVariants;
    public BuildingSpriteVariantSet GableFillVariants => gableFillVariants;
    public BuildingSpriteVariantSet GableSlopeVariants => gableSlopeVariants;
    public BuildingSpriteVariantSet GableBottomOverhangVariants => gableBottomOverhangVariants;
    public BuildingSpriteVariantSet WindboardTopVariants => windboardTopVariants;
    public BuildingSpriteVariantSet WindboardMiddleVariants => windboardMiddleVariants;
    public BuildingSpriteVariantSet WindboardBottomVariants => windboardBottomVariants;
}

[Serializable]
public sealed class BuildingSectionRoofSettings
{
    [SerializeField]
    private bool enabled = true;

    [SerializeField]
    private BuildingRoofProfile profile;

    [FormerlySerializedAs("repeatAxis")]
    [SerializeField]
    private BuildingRoofDirection direction = BuildingRoofDirection.EastWest;

    [SerializeField]
    private int variationSeed = 12345;

    public bool Enabled => enabled;
    public BuildingRoofProfile Profile => profile;
    public BuildingRoofDirection Direction => direction;
    public int VariationSeed => variationSeed;
}

public readonly struct BuildingRoofBuildPiece
{
    public readonly BuildingRoofPieceRole Role;
    public readonly Sprite Sprite;
    public readonly Vector2 TargetAnchorLocal;
    public readonly BuildingSpriteAnchor Anchor;
    public readonly Vector2 AnchorPixelOffset;
    public readonly bool FlipX;
    public readonly bool FlipY;
    public readonly int SortingOrder;

    public BuildingRoofBuildPiece(
        BuildingRoofPieceRole role,
        Sprite sprite,
        Vector2 targetAnchorLocal,
        BuildingSpriteAnchor anchor,
        bool flipX,
        bool flipY,
        int sortingOrder,
        Vector2 anchorPixelOffset = default(Vector2))
    {
        Role = role;
        Sprite = sprite;
        TargetAnchorLocal = targetAnchorLocal;
        Anchor = anchor;
        AnchorPixelOffset = anchorPixelOffset;
        FlipX = flipX;
        FlipY = flipY;
        SortingOrder = sortingOrder;
    }
}

public sealed class BuildingRoofBuildPlan
{
    private readonly List<BuildingRoofBuildPiece> pieces = new();

    public IReadOnlyList<BuildingRoofBuildPiece> Pieces => pieces;

    public string Error { get; private set; }

    public bool IsValid =>
        string.IsNullOrEmpty(Error) &&
        pieces.Count > 0;

    public void Add(BuildingRoofBuildPiece piece)
    {
        pieces.Add(piece);
    }

    public void Fail(string error)
    {
        Error = error;
        pieces.Clear();
    }
}
