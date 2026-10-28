using UnityEngine;

[CreateAssetMenu(
    fileName = "Building Structure Style",
    menuName = "RöK/Building/Structure Style"
)]
public sealed class BuildingStructureStyle : ScriptableObject
{
    // =========================================================
    // RENDERING
    // =========================================================

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName = "Default";


    // =========================================================
    // FLOOR
    // =========================================================

    [Header("Floor - 32x32")]

    [SerializeField]
    private Sprite floorSprite;


    // =========================================================
    // HORIZONTAL WALLS
    // =========================================================

    [Header("Horizontal Exterior Wall - 96px high")]

    [SerializeField]
    private Sprite exteriorWall64;

    [SerializeField]
    private Sprite exteriorWall128;


    [Header("Horizontal Interior-Facing Wall - 96px high")]

    [SerializeField]
    private Sprite interiorWall64;

    [SerializeField]
    private Sprite interiorWall128;


    // =========================================================
    // VERTICAL / NORTH-SOUTH WALLS
    // =========================================================

    [Header("Vertical Side Wall - logical 32px step")]

    [SerializeField]
    [Tooltip(
        "Ett vertikalt sidoväggssegment. Artwork får sticka ut utanför " +
        "sin 32px logiska höjd, men generatorn avancerar alltid 32px."
    )]
    private Sprite sideWallSegment;

    [SerializeField]
    [Tooltip(
        "Om aktiv speglas samma side-wall artwork automatiskt på östra sidan."
    )]
    private bool flipSideWallOnEast = true;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "Visuell Y-förskjutning för hela N/S-sidewall-systemet. " +
        "Väggsegment och side-pillars flyttas tillsammans, medan footprint, " +
        "golv och collision förblir på sina strukturella positioner. " +
        "64px motsvarar två structural cells och är rekommenderad startpunkt."
    )]
    private int sideWallVisualLiftPixels = 64;


    // =========================================================
    // DOORS / OPENINGS
    // =========================================================

    [Header("Doors - North / South")]

    [SerializeField]
    private Sprite southDoorSprite;

    [SerializeField]
    [Tooltip(
        "Optional rear/North door sprite. If empty, South Door Sprite is reused."
    )]
    private Sprite northDoorSprite;

    [SerializeField]
    private Sprite southDoorThresholdSprite;

    [SerializeField]
    private Sprite northDoorThresholdSprite;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "South/front door artwork is visually lowered by this many pixels. " +
        "The logical/sorting anchor stays on the real wall baseline."
    )]
    private int southDoorVisualDropPixels = 2;


    [Header("Doors - East / West")]

    [SerializeField]
    [Tooltip(
        "Vertical 16px-outward x 64px-high threshold/entrance marker used on side doors."
    )]
    private Sprite sideDoorThresholdSprite;

    [SerializeField]
    [Tooltip(
        "Mirror the same side-threshold artwork automatically on the East wall."
    )]
    private bool flipSideDoorThresholdOnEast = true;


    // =========================================================
    // PILLARS + SHADOWS
    // =========================================================

    [Header("Pillars")]

    [SerializeField]
    private Sprite pillarSprite;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "Full pelare på horisontell vägg skjuts ned så foten får djup."
    )]
    private int horizontalPillarDropPixels = 7;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "Extra lift för side-pelaren relativt den redan visuellt lyfta side-wall-seamen."
    )]
    private int sidePillarLiftPixels = 16;

    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Önskad rytm på sidoväggspelare i structural cells. 2 = ungefär var 64px, " +
        "men generatorn speglar placeringarna symmetriskt från båda ändar."
    )]
    private int sidePillarSpacingCells = 2;


    [Header("Pillar Shadow - horizontal walls")]

    [SerializeField]
    private Sprite beamShadowSprite;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "Avstånd från seam/pelarmitt till shadow-spritens mitt."
    )]
    private int beamShadowOffsetPixels = 9;


    // =========================================================
    // REAR / INTERIOR GABLE
    // =========================================================

    [Header("Rear Interior Gable")]

    [SerializeField]
    private Sprite rearGableFill64x32;

    [SerializeField]
    private Sprite rearGableSlope32x32;

    [SerializeField]
    private Sprite rearGableBaseTrim32x7;


    // =========================================================
    // SORTING - V1 INTERNAL BUILDING ORDER
    // =========================================================

    [Header("Sorting - Structure V1")]

    [SerializeField]
    private int floorSortingOrder = 1;

    [SerializeField]
    private int rearWallSortingOrder = 2;

    [SerializeField]
    private int sideWallBackSortingOrder = 3;

    [SerializeField]
    private int sideWallFrontSortingOrder = 6;

    [SerializeField]
    private int frontWallSortingOrder = 7;


    // =========================================================
    // PUBLIC
    // =========================================================

    public string SortingLayerName => sortingLayerName;

    public Sprite FloorSprite => floorSprite;

    public Sprite ExteriorWall64 => exteriorWall64;
    public Sprite ExteriorWall128 => exteriorWall128;

    public Sprite InteriorWall64 => interiorWall64;
    public Sprite InteriorWall128 => interiorWall128;

    public Sprite SideWallSegment => sideWallSegment;
    public bool FlipSideWallOnEast => flipSideWallOnEast;

    public int SideWallVisualLiftPixels =>
        Mathf.Max(0, sideWallVisualLiftPixels);

    public Sprite SouthDoorSprite => southDoorSprite;

    public Sprite NorthDoorSprite =>
        northDoorSprite != null
            ? northDoorSprite
            : southDoorSprite;

    public Sprite SouthDoorThresholdSprite =>
        southDoorThresholdSprite;

    public Sprite NorthDoorThresholdSprite =>
        northDoorThresholdSprite;

    public int SouthDoorVisualDropPixels =>
        Mathf.Max(0, southDoorVisualDropPixels);

    public Sprite SideDoorThresholdSprite =>
        sideDoorThresholdSprite;

    public bool FlipSideDoorThresholdOnEast =>
        flipSideDoorThresholdOnEast;

    public Sprite PillarSprite => pillarSprite;

    public int HorizontalPillarDropPixels =>
        Mathf.Max(0, horizontalPillarDropPixels);

    public int SidePillarLiftPixels =>
        Mathf.Max(0, sidePillarLiftPixels);

    public int SidePillarSpacingCells =>
        Mathf.Max(1, sidePillarSpacingCells);

    public Sprite BeamShadowSprite => beamShadowSprite;

    public int BeamShadowOffsetPixels =>
        Mathf.Max(0, beamShadowOffsetPixels);

    public Sprite RearGableFill64x32 => rearGableFill64x32;
    public Sprite RearGableSlope32x32 => rearGableSlope32x32;
    public Sprite RearGableBaseTrim32x7 => rearGableBaseTrim32x7;

    public int FloorSortingOrder => floorSortingOrder;
    public int RearWallSortingOrder => rearWallSortingOrder;
    public int SideWallBackSortingOrder => sideWallBackSortingOrder;
    public int SideWallFrontSortingOrder => sideWallFrontSortingOrder;
    public int FrontWallSortingOrder => frontWallSortingOrder;
}
