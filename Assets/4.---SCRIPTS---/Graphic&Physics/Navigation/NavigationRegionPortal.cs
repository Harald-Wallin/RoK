using UnityEngine;

/// <summary>
/// Representerar en sammanhängande walkable passage mellan
/// två intilliggande NavigationRegions.
///
/// En portal är INTE ett GameObject.
///
/// Till skillnad från den gamla implementationen representerar
/// portalen hela den sammanhängande öppningen, inte bara dess mitt.
///
/// Crossing-punkter väljs bland de verkliga border-celler som
/// NavigationWorld redan verifierat som walkable och traverserbara.
/// </summary>
public sealed class NavigationRegionPortal
{
    // =========================================================
    // REGIONS
    // =========================================================

    public NavigationRegion RegionA
    {
        get;
    }

    public NavigationRegion RegionB
    {
        get;
    }


    // =========================================================
    // PORTAL SPAN
    // =========================================================

    /// <summary>
    /// Första säkra cellpunkten i RegionA.
    /// </summary>
    public Vector2 PointAStart
    {
        get;
    }


    /// <summary>
    /// Sista säkra cellpunkten i RegionA.
    /// </summary>
    public Vector2 PointAEnd
    {
        get;
    }


    /// <summary>
    /// Första säkra cellpunkten i RegionB.
    /// </summary>
    public Vector2 PointBStart
    {
        get;
    }


    /// <summary>
    /// Sista säkra cellpunkten i RegionB.
    /// </summary>
    public Vector2 PointBEnd
    {
        get;
    }


    // =========================================================
    // LEGACY / CENTER ACCESS
    // =========================================================

    /// <summary>
    /// Portalens mittpunkt inne i RegionA.
    ///
    /// Behålls eftersom world-pathfinding och debug fortfarande
    /// kan använda portalens centrum som grov representation.
    /// </summary>
    public Vector2 PointA
    {
        get;
    }


    /// <summary>
    /// Portalens mittpunkt inne i RegionB.
    /// </summary>
    public Vector2 PointB
    {
        get;
    }


    /// <summary>
    /// Mitten av hela portalöppningen på regiongränsen.
    /// </summary>
    public Vector2 WorldCenter
    {
        get;
    }


    // =========================================================
    // PORTAL DATA
    // =========================================================

    /// <summary>
    /// Antal verkliga verifierade cellpar i öppningen.
    ///
    /// Exempel:
    ///
    /// SampleCount = 1
    /// => endast en crossing-cell.
    ///
    /// SampleCount = 20
    /// => NPC:n kan välja mellan 20 säkra crossing-punkter.
    /// </summary>
    public int SampleCount
    {
        get;
    }


    /// <summary>
    /// Ungefärlig fysisk bredd på den sammanhängande öppningen.
    /// </summary>
    public float Width
    {
        get;
    }


    /// <summary>
    /// Grid-riktningen från RegionA till RegionB.
    ///
    /// right / left:
    /// portalen löper vertikalt.
    ///
    /// up / down:
    /// portalen löper horisontellt.
    /// </summary>
    public Vector2Int DirectionFromA
    {
        get;
    }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public NavigationRegionPortal(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2 pointAStart,
        Vector2 pointAEnd,
        Vector2 pointBStart,
        Vector2 pointBEnd,
        int sampleCount,
        float width,
        Vector2Int directionFromA)
    {
        RegionA =
            regionA;

        RegionB =
            regionB;


        PointAStart =
            pointAStart;

        PointAEnd =
            pointAEnd;

        PointBStart =
            pointBStart;

        PointBEnd =
            pointBEnd;


        PointA =
            (
                PointAStart +
                PointAEnd
            ) *
            0.5f;


        PointB =
            (
                PointBStart +
                PointBEnd
            ) *
            0.5f;


        WorldCenter =
            (
                PointA +
                PointB
            ) *
            0.5f;


        SampleCount =
            Mathf.Max(
                1,
                sampleCount
            );


        Width =
            Mathf.Max(
                0f,
                width
            );


        DirectionFromA =
            directionFromA;
    }


    // =========================================================
    // CONNECTION QUERY
    // =========================================================

    public bool Connects(
        NavigationRegion region)
    {
        return
            region != null &&
            (
                region == RegionA ||
                region == RegionB
            );
    }


    public bool Connects(
        NavigationRegion first,
        NavigationRegion second)
    {
        if (first == null ||
            second == null)
        {
            return false;
        }


        return
            (
                RegionA == first &&
                RegionB == second
            ) ||
            (
                RegionA == second &&
                RegionB == first
            );
    }


    public NavigationRegion GetOtherRegion(
        NavigationRegion region)
    {
        if (region == RegionA)
            return RegionB;


        if (region == RegionB)
            return RegionA;


        return null;
    }


    // =========================================================
    // CENTER QUERY
    // =========================================================

    /// <summary>
    /// Returnerar portalens MITTPUNKT i den angivna regionen.
    ///
    /// Detta är fortfarande användbart för:
    /// - high-level world pathfinding
    /// - debug
    ///
    /// NPCNavigationAgent ska däremot använda
    /// TryGetBestCrossingPoints().
    /// </summary>
    public Vector2 GetPointForRegion(
        NavigationRegion region)
    {
        if (region == RegionA)
            return PointA;


        if (region == RegionB)
            return PointB;


        return WorldCenter;
    }


    // =========================================================
    // DYNAMIC CROSSING
    // =========================================================

    /// <summary>
    /// Väljer ett naturligt och säkert crossing-cellpar
    /// för just denna resa.
    ///
    /// Crossing-punkten försöker ligga där den raka linjen mellan
    /// fromPosition och towardPosition skulle korsa regiongränsen.
    ///
    /// Resultatet snappas därefter till en verklig verifierad
    /// border-cell i portalens span.
    /// </summary>
    public bool TryGetBestCrossingPoints(
        NavigationRegion fromRegion,
        Vector2 fromPosition,
        Vector2 towardPosition,
        out Vector2 currentSidePoint,
        out Vector2 otherSidePoint)
    {
        currentSidePoint =
            WorldCenter;

        otherSidePoint =
            WorldCenter;


        if (!Connects(
                fromRegion))
        {
            return false;
        }


        float portalT =
            CalculateIdealPortalT(
                fromPosition,
                towardPosition
            );


        portalT =
            SnapToSafeSample(
                portalT
            );


        Vector2 pointA =
            Vector2.Lerp(
                PointAStart,
                PointAEnd,
                portalT
            );


        Vector2 pointB =
            Vector2.Lerp(
                PointBStart,
                PointBEnd,
                portalT
            );


        if (fromRegion == RegionA)
        {
            currentSidePoint =
                pointA;

            otherSidePoint =
                pointB;

            return true;
        }


        currentSidePoint =
            pointB;

        otherSidePoint =
            pointA;

        return true;
    }


    // =========================================================
    // CROSSING SELECTION
    // =========================================================

    private float CalculateIdealPortalT(
        Vector2 fromPosition,
        Vector2 towardPosition)
    {
        Vector2 borderStart =
            (
                PointAStart +
                PointBStart
            ) *
            0.5f;


        Vector2 borderEnd =
            (
                PointAEnd +
                PointBEnd
            ) *
            0.5f;


        /*
         * En portal mellan east/west-regions har en
         * vertikal regiongräns.
         */
        if (DirectionFromA.x != 0)
        {
            float borderX =
                WorldCenter.x;


            float deltaX =
                towardPosition.x -
                fromPosition.x;


            float desiredY;


            if (Mathf.Abs(
                    deltaX) >
                0.0001f)
            {
                float travelT =
                    (
                        borderX -
                        fromPosition.x
                    ) /
                    deltaX;


                Vector2 crossing =
                    Vector2.LerpUnclamped(
                        fromPosition,
                        towardPosition,
                        travelT
                    );


                desiredY =
                    crossing.y;
            }
            else
            {
                desiredY =
                    (
                        fromPosition.y +
                        towardPosition.y
                    ) *
                    0.5f;
            }


            float startY =
                borderStart.y;

            float endY =
                borderEnd.y;


            float denominator =
                endY -
                startY;


            if (Mathf.Abs(
                    denominator) <=
                0.0001f)
            {
                return 0f;
            }


            return Mathf.Clamp01(
                (
                    desiredY -
                    startY
                ) /
                denominator
            );
        }


        /*
         * En portal mellan north/south-regions har en
         * horisontell regiongräns.
         */
        float borderY =
            WorldCenter.y;


        float deltaY =
            towardPosition.y -
            fromPosition.y;


        float desiredX;


        if (Mathf.Abs(
                deltaY) >
            0.0001f)
        {
            float travelT =
                (
                    borderY -
                    fromPosition.y
                ) /
                deltaY;


            Vector2 crossing =
                Vector2.LerpUnclamped(
                    fromPosition,
                    towardPosition,
                    travelT
                );


            desiredX =
                crossing.x;
        }
        else
        {
            desiredX =
                (
                    fromPosition.x +
                    towardPosition.x
                ) *
                0.5f;
        }


        float startX =
            borderStart.x;

        float endX =
            borderEnd.x;


        float horizontalDenominator =
            endX -
            startX;


        if (Mathf.Abs(
                horizontalDenominator) <=
            0.0001f)
        {
            return 0f;
        }


        return Mathf.Clamp01(
            (
                desiredX -
                startX
            ) /
            horizontalDenominator
        );
    }


    /// <summary>
    /// Snappar till en faktisk verifierad crossing-cell.
    ///
    /// Vi väljer alltså inte en godtycklig interpolerad punkt
    /// mellan två celler.
    /// </summary>
    private float SnapToSafeSample(
        float portalT)
    {
        if (SampleCount <= 1)
        {
            return 0f;
        }


        int stepCount =
            SampleCount -
            1;


        float sampleIndex =
            Mathf.Round(
                Mathf.Clamp01(
                    portalT
                ) *
                stepCount
            );


        return
            sampleIndex /
            stepCount;
    }


    // =========================================================
    // DIRECTION
    // =========================================================

    public Vector2Int GetDirectionFrom(
        NavigationRegion region)
    {
        if (region == RegionA)
        {
            return DirectionFromA;
        }


        if (region == RegionB)
        {
            return -DirectionFromA;
        }


        return Vector2Int.zero;
    }
}