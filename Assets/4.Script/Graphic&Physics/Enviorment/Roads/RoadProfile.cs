using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Data-driven definition av en vägtyp.
///
/// RoadProfile beskriver hur vägen ska genereras och presenteras.
/// Själva vägformen sparas separat i RoadPath.
/// </summary>
[CreateAssetMenu(
    fileName = "RoadProfile_",
    menuName = "World/Road Profile"
)]
public sealed class RoadProfile :
    ScriptableObject
{
    // =========================================================
    // SHAPE
    // =========================================================

    [Header("Shape")]

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Vägens fulla grundbredd i world units när vägen går lodrätt."
    )]
    private float baseWidth =
        3.5f;


    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
        "Hur bred vägen visuellt blir när den går helt horisontellt. " +
        "0.7 betyder 70 % av normal bredd."
    )]
    private float horizontalWidthScale =
        0.7f;


    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Storskalig mjuk variation i själva vägformen. " +
        "Pixelnaggning hanteras separat av Edge Erosion."
    )]
    private float edgeRoughness =
        0.04f;


    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Hur långsamt den storskaliga kantvariationen förändras längs vägen."
    )]
    private float edgeNoiseLength =
        0.8f;


    // =========================================================
    // SURFACE
    // =========================================================

    [Header("Surface")]

    [SerializeField]
    [Tooltip(
        "Valfritt custom material. För att Edge Erosion och Shoulder " +
        "ska fungera måste materialet använda RöK/Road Surface-shadern. " +
        "Lämna normalt detta tomt."
    )]
    private Material surfaceMaterial;


    [SerializeField]
    [Tooltip(
        "Sömlös world-space-texture för själva vägytan."
    )]
    private Texture2D surfaceTexture;


    [SerializeField]
    [Min(1f)]
    [Tooltip(
        "Pixel density för road surface i world space. " +
        "32 betyder 32 texture-pixlar per world unit."
    )]
    private float surfacePixelsPerUnit =
        32f;


    // =========================================================
    // EDGE EROSION
    // =========================================================

    [Header("Edge Erosion")]

    [SerializeField]
    private bool enableEdgeErosion =
        true;


    [SerializeField]
    [Range(0f, 16f)]
    [Tooltip(
        "Maximalt djup i pixlar som vägkanten får naggas in. " +
        "6-8 px är en bra start."
    )]
    private float edgeErosionDepthPixels =
        7f;


    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur aggressiv huvudnaggningen är."
    )]
    private float edgeErosionStrength =
        0.9f;


    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur mycket erosion återstår när vägen är helt horisontell. " +
        "0.1 betyder cirka 10 % av normal effekt."
    )]
    private float horizontalErosionScale =
        0.1f;


    [SerializeField]
    [Range(0f, 0.2f)]
    [Tooltip(
        "Chans för enstaka bortklippta pixlar strax innanför " +
        "den huvudsakliga naggningen."
    )]
    private float innerPinholeChance =
        0.015f;


    [SerializeField]
    [Range(0f, 8f)]
    [Tooltip(
        "Hur många extra pixlar innanför huvuderosionen där " +
        "enstaka pinholes får förekomma."
    )]
    private float innerPinholeDepthPixels =
        2f;


    // =========================================================
    // SHOULDER
    // =========================================================

    [Header("Shoulder")]

    [SerializeField]
    [FormerlySerializedAs("enableUpperShoulder")]
    private bool enableShoulder =
        true;


    [SerializeField]
    [Range(0f, 12f)]
    [Tooltip(
        "Bredd på contact-shadow/shoulder räknat i pixels från " +
        "den faktiska naggade vägkanten."
    )]
    private float shoulderWidthPixels =
        5f;


    [SerializeField]
    [FormerlySerializedAs("upperShoulderColor")]
    [Tooltip(
        "Färg som blandas in närmast den naggade kanten. " +
        "Använd Alpha för att styra hur mörk effekten får bli."
    )]
    private Color shoulderColor =
        new Color(
            0.025f,
            0.018f,
            0.012f,
            0.65f
        );


    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Shoulder-styrka på world-space-övre/bortre kanten " +
        "när vägen är horisontell."
    )]
    private float upperShoulderStrength =
        0.85f;


    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Shoulder-styrka på world-space-nedre/främre kanten " +
        "när vägen är horisontell."
    )]
    private float lowerShoulderStrength =
        0.15f;


    // =========================================================
    // SPLATTER
    // =========================================================

    [Header("Splatter")]

    [SerializeField]
    private bool enableSplatter =
        true;


    [SerializeField]
    [Tooltip(
        "Små lera/jord-sprites som sprids automatiskt längs vägkanterna."
    )]
    private Sprite[] splatterSprites =
        System.Array.Empty<Sprite>();


    [SerializeField]
    [Min(0f)]
    private float splatterDensity =
        0.3f;


    [SerializeField]
    [Min(0f)]
    private float splatterMinEdgeOffset =
        0.02f;


    [SerializeField]
    [Min(0f)]
    private float splatterMaxEdgeOffset =
        0.3f;


    [SerializeField]
    [Range(0f, 1f)]
    private float upperEdgeSplatterOffsetScale =
        0.25f;


    [SerializeField]
    [Range(0.25f, 1f)]
    private float upperEdgeSplatterScale =
        0.75f;


    [SerializeField]
    private Vector2 splatterScaleRange =
        new Vector2(
            0.85f,
            1.15f
        );

    // =========================================================
    // EDGE DOODADS
    // =========================================================

    [Header("Edge Doodads")]

    [SerializeField]
    [Tooltip(
        "Små gräsbitar, småsten och andra detaljer som placeras " +
        "automatiskt längs vägkanterna."
    )]
    private bool enableEdgeDoodads =
        true;


    [SerializeField]
    [Tooltip(
        "Små sprites som får placeras längs vägkanten. " +
        "Alla genererade doodads kombineras till en gemensam mesh."
    )]
    private Sprite[] edgeDoodadSprites =
        System.Array.Empty<Sprite>();


    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Ungefärligt totalt antal doodads per world unit väg. " +
        "Doodads fördelas över båda vägkanterna."
    )]
    private float edgeDoodadDensity =
        3f;


    [SerializeField]
    [Tooltip(
        "Minsta offset från vägkanten. Negativt värde innebär " +
        "att doodaden får sticka in över själva vägen."
    )]
    private float edgeDoodadMinEdgeOffset =
        -0.08f;


    [SerializeField]
    [Tooltip(
        "Största offset utanför vägkanten."
    )]
    private float edgeDoodadMaxEdgeOffset =
        0.06f;


    [SerializeField]
    [Tooltip(
        "Min/max storlek för Edge Doodads."
    )]
    private Vector2 edgeDoodadScaleRange =
        new Vector2(
            0.85f,
            1.15f
        );


    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur mycket doodad-offset på world-space-övre/bortre kanten " +
        "behålls när vägen går helt horisontellt."
    )]
    private float upperEdgeDoodadOffsetScale =
        0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
    "Hur mycket av normal doodad-offset som behålls på " +
    "world-space-nedre/främre kanten när vägen går helt horisontellt. " +
    "0 betyder att doodaden placeras exakt på vägkanten. " +
    "Effekten tonas bort när vägen blir lodrät."
)]
    private float lowerEdgeDoodadOffsetScale =
    0f;


    [SerializeField]
    [Range(0.25f, 1f)]
    [Tooltip(
        "Storlek på doodads på world-space-övre/bortre kanten " +
        "när vägen går helt horisontellt."
    )]
    private float upperEdgeDoodadScale =
        0.75f;


    [SerializeField]
    [Tooltip(
        "Om aktiv kan sprites slumpmässigt spegelvändas horisontellt."
    )]
    private bool edgeDoodadRandomFlipX =
        true;


    [SerializeField]
    [Tooltip(
        "Om aktiv kan doodads roteras i 90-graderssteg. " +
        "Lämna normalt avstängt för gräs och andra upprätta sprites."
    )]
    private bool edgeDoodadRandomQuarterTurns =
        false;


    // =========================================================
    // TRAVERSAL
    // =========================================================

    [Header("Traversal")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Movement speed-multiplikator för SPELAREN när spelaren står " +
        "på denna vägtyp. 1.10 betyder +10 %. NPCs använder inte värdet."
    )]
    private float playerMovementSpeedMultiplier =
        1.10f;


    // =========================================================
    // GROUND CLUTTER
    // =========================================================

    [Header("Ground Clutter")]

    [SerializeField]
    private bool clearGroundClutter =
        true;


    [SerializeField]
    [Range(0.1f, 1f)]
    private float groundClutterClearWidthScale =
        0.75f;


    // =========================================================
    // RENDERING
    // =========================================================

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";


    [SerializeField]
    private int sortingOrder =
        0;


    // =========================================================
    // PUBLIC
    // =========================================================

    public float BaseWidth =>
        baseWidth;

    public float HorizontalWidthScale =>
        horizontalWidthScale;

    public float EdgeRoughness =>
        edgeRoughness;

    public float EdgeNoiseLength =>
        edgeNoiseLength;


    public Material SurfaceMaterial =>
        surfaceMaterial;

    public Texture2D SurfaceTexture =>
        surfaceTexture;

    public float SurfacePixelsPerUnit =>
        surfacePixelsPerUnit;


    public bool EnableEdgeErosion =>
        enableEdgeErosion;

    public float EdgeErosionDepthPixels =>
        edgeErosionDepthPixels;

    public float EdgeErosionStrength =>
        edgeErosionStrength;

    public float HorizontalErosionScale =>
        horizontalErosionScale;

    public float InnerPinholeChance =>
        innerPinholeChance;

    public float InnerPinholeDepthPixels =>
        innerPinholeDepthPixels;


    public bool EnableShoulder =>
        enableShoulder;

    public float ShoulderWidthPixels =>
        shoulderWidthPixels;

    public Color ShoulderColor =>
        shoulderColor;

    public float UpperShoulderStrength =>
        upperShoulderStrength;

    public float LowerShoulderStrength =>
        lowerShoulderStrength;


    public bool EnableSplatter =>
        enableSplatter;

    public Sprite[] SplatterSprites =>
        splatterSprites;

    public float SplatterDensity =>
        splatterDensity;

    public float SplatterMinEdgeOffset =>
        splatterMinEdgeOffset;

    public float SplatterMaxEdgeOffset =>
        splatterMaxEdgeOffset;

    public float UpperEdgeSplatterOffsetScale =>
        upperEdgeSplatterOffsetScale;

    public float UpperEdgeSplatterScale =>
        upperEdgeSplatterScale;

    public Vector2 SplatterScaleRange =>
        splatterScaleRange;


    public bool ClearGroundClutter =>
        clearGroundClutter;

    public float GroundClutterClearWidthScale =>
        groundClutterClearWidthScale;


    public string SortingLayerName =>
        sortingLayerName;

    public int SortingOrder =>
        sortingOrder;

    public bool EnableEdgeDoodads =>
    enableEdgeDoodads;

    public Sprite[] EdgeDoodadSprites =>
        edgeDoodadSprites;

    public float EdgeDoodadDensity =>
        edgeDoodadDensity;

    public float EdgeDoodadMinEdgeOffset =>
        edgeDoodadMinEdgeOffset;

    public float EdgeDoodadMaxEdgeOffset =>
        edgeDoodadMaxEdgeOffset;

    public Vector2 EdgeDoodadScaleRange =>
        edgeDoodadScaleRange;

    public float UpperEdgeDoodadOffsetScale =>
        upperEdgeDoodadOffsetScale;

    public float UpperEdgeDoodadScale =>
        upperEdgeDoodadScale;

    public bool EdgeDoodadRandomFlipX =>
        edgeDoodadRandomFlipX;

    public bool EdgeDoodadRandomQuarterTurns =>
        edgeDoodadRandomQuarterTurns;

    public float LowerEdgeDoodadOffsetScale =>
    lowerEdgeDoodadOffsetScale;


    public float PlayerMovementSpeedMultiplier =>
        playerMovementSpeedMultiplier;


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        baseWidth =
            Mathf.Max(
                0.05f,
                baseWidth
            );

        horizontalWidthScale =
            Mathf.Clamp(
                horizontalWidthScale,
                0.1f,
                1f
            );

        edgeRoughness =
            Mathf.Max(
                0f,
                edgeRoughness
            );

        edgeNoiseLength =
            Mathf.Max(
                0.05f,
                edgeNoiseLength
            );

        surfacePixelsPerUnit =
            Mathf.Max(
                1f,
                surfacePixelsPerUnit
            );


        edgeErosionDepthPixels =
            Mathf.Clamp(
                edgeErosionDepthPixels,
                0f,
                16f
            );

        edgeErosionStrength =
            Mathf.Clamp01(
                edgeErosionStrength
            );

        horizontalErosionScale =
            Mathf.Clamp01(
                horizontalErosionScale
            );

        innerPinholeChance =
            Mathf.Clamp(
                innerPinholeChance,
                0f,
                0.2f
            );

        innerPinholeDepthPixels =
            Mathf.Clamp(
                innerPinholeDepthPixels,
                0f,
                8f
            );


        shoulderWidthPixels =
            Mathf.Clamp(
                shoulderWidthPixels,
                0f,
                12f
            );

        upperShoulderStrength =
            Mathf.Clamp01(
                upperShoulderStrength
            );

        lowerShoulderStrength =
            Mathf.Clamp01(
                lowerShoulderStrength
            );


        splatterDensity =
            Mathf.Max(
                0f,
                splatterDensity
            );

        splatterMinEdgeOffset =
            Mathf.Max(
                0f,
                splatterMinEdgeOffset
            );

        splatterMaxEdgeOffset =
            Mathf.Max(
                splatterMinEdgeOffset,
                splatterMaxEdgeOffset
            );

        upperEdgeSplatterOffsetScale =
            Mathf.Clamp01(
                upperEdgeSplatterOffsetScale
            );

        upperEdgeSplatterScale =
            Mathf.Clamp(
                upperEdgeSplatterScale,
                0.25f,
                1f
            );

        splatterScaleRange.x =
            Mathf.Max(
                0.01f,
                splatterScaleRange.x
            );

        splatterScaleRange.y =
            Mathf.Max(
                splatterScaleRange.x,
                splatterScaleRange.y
            );


        playerMovementSpeedMultiplier =
            Mathf.Max(
                0f,
                playerMovementSpeedMultiplier
            );


        groundClutterClearWidthScale =
            Mathf.Clamp(
                groundClutterClearWidthScale,
                0.1f,
                1f
            );

        edgeDoodadDensity =
            Mathf.Max(
                0f,
                edgeDoodadDensity
            );

        lowerEdgeDoodadOffsetScale =
            Mathf.Clamp01(
                lowerEdgeDoodadOffsetScale
            );


        if (edgeDoodadMaxEdgeOffset <
            edgeDoodadMinEdgeOffset)
        {
            edgeDoodadMaxEdgeOffset =
                edgeDoodadMinEdgeOffset;
        }


        edgeDoodadScaleRange.x =
            Mathf.Max(
                0.01f,
                edgeDoodadScaleRange.x
            );


        edgeDoodadScaleRange.y =
            Mathf.Max(
                edgeDoodadScaleRange.x,
                edgeDoodadScaleRange.y
            );


        upperEdgeDoodadOffsetScale =
            Mathf.Clamp01(
                upperEdgeDoodadOffsetScale
            );


        upperEdgeDoodadScale =
            Mathf.Clamp(
                upperEdgeDoodadScale,
                0.25f,
                1f
            );
    }
}