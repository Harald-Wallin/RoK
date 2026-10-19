Shader "RöK/Road Surface"
{
    Properties
    {
        _MainTex (
            "Surface Texture",
            2D
        ) = "white" {}

        _Color (
            "Tint",
            Color
        ) = (1,1,1,1)

        _SurfacePixelsPerUnit (
            "Pixels Per Unit",
            Float
        ) = 32

        _RoadSeed (
            "Road Seed",
            Float
        ) = 1

        _EdgeErosionEnabled (
            "Edge Erosion Enabled",
            Float
        ) = 1

        _EdgeErosionDepthPixels (
            "Erosion Depth Pixels",
            Float
        ) = 7

        _EdgeErosionStrength (
            "Erosion Strength",
            Range(0,1)
        ) = 0.9

        _HorizontalErosionScale (
            "Horizontal Erosion Scale",
            Range(0,1)
        ) = 0.1

        _InnerPinholeChance (
            "Inner Pinhole Chance",
            Range(0,0.2)
        ) = 0.015

        _InnerPinholeDepthPixels (
            "Inner Pinhole Depth Pixels",
            Float
        ) = 2

        _ShoulderEnabled (
            "Shoulder Enabled",
            Float
        ) = 1

        _ShoulderWidthPixels (
            "Shoulder Width Pixels",
            Float
        ) = 5

        _ShoulderColor (
            "Shoulder Color",
            Color
        ) = (0.025,0.018,0.012,0.65)

        _UpperShoulderStrength (
            "Upper Shoulder Strength",
            Range(0,1)
        ) = 0.85

        _LowerShoulderStrength (
            "Lower Shoulder Strength",
            Range(0,1)
        ) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            sampler2D _MainTex;

            float4 _MainTex_TexelSize;
            float4 _Color;

            float _SurfacePixelsPerUnit;
            float _RoadSeed;

            float _EdgeErosionEnabled;
            float _EdgeErosionDepthPixels;
            float _EdgeErosionStrength;
            float _HorizontalErosionScale;

            float _InnerPinholeChance;
            float _InnerPinholeDepthPixels;

            float _ShoulderEnabled;
            float _ShoulderWidthPixels;
            float4 _ShoulderColor;

            float _UpperShoulderStrength;
            float _LowerShoulderStrength;


            struct appdata
            {
                float4 vertex :
                    POSITION;

                float2 uv :
                    TEXCOORD0;

                /*
                 * x = avstånd till aktuell road-edge i world units
                 *
                 * y = aktuell edge normals signed world-Y
                 *
                 *     positiv = upper edge
                 *     negativ = lower edge
                 *
                 * z = road-side
                 *
                 *     +1 = left side
                 *     -1 = right side
                 *
                 * w = travelled distance längs vägen
                 */
                float4 edgeData :
                    TEXCOORD1;

                float2 junctionData :
                TEXCOORD2;
            };


            struct v2f
            {
                float4 position :
                    SV_POSITION;

                float2 uv :
                    TEXCOORD0;

                float4 edgeData :
                    TEXCOORD1;

                float2 junctionData :
                    TEXCOORD2;
            };


            v2f vert(
                appdata input)
            {
                v2f output;

                output.position =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.uv =
                    input.uv;

                output.edgeData =
                    input.edgeData;

                output.junctionData =
                    input.junctionData;

                return output;
            }


            float Hash1D(
    float value,
    float seed)
{
    float valueToHash =
        value *
        127.1 +
        seed *
        311.7;

    return
        frac(
            sin(
                valueToHash
            ) *
            43758.5453
        );
}


float HashPixel(
    float2 pixel,
    float seed)
{
    float valueToHash =
        dot(
            pixel,
            float2(
                127.1,
                311.7
            )
        ) +
        seed *
        74.7;

    return
        frac(
            sin(
                valueToHash
            ) *
            43758.5453
        );
}


float GetEdgeProfileNoise(
    float roadPixel,
    float sideId,
    float seed)
{
    /*
     * Ett grövre lager skapar små sammanhängande jack.
     */
    float coarsePixel =
        floor(
            roadPixel /
            3.0
        );


    float coarse =
        Hash1D(
            coarsePixel +
            sideId *
            17.0,
            seed +
            13.0
        );


    /*
     * Fine-lagret bryter upp silhuetten på
     * individuell pixelnivå.
     */
    float fine =
        Hash1D(
            roadPixel +
            sideId *
            31.0,
            seed +
            47.0
        );


    return
        saturate(
            coarse *
            0.65 +
            fine *
            0.35
        );
}


            fixed4 frag(
                v2f input) :
                SV_Target
            {
                float pixelsPerUnit =
                    max(
                        1.0,
                        _SurfacePixelsPerUnit
                    );

                   float junctionInfluence =
                    saturate(
                        input.junctionData.x
                    );


                float junctionKeep =
                    1.0 -
                    junctionInfluence;


                float edgeDistancePixels =
                    max(
                        0.0,
                        input.edgeData.x *
                        pixelsPerUnit
                    );


                /*
                 * abs(normal.y) är samma sak som hur
                 * horisontellt vägsegmentet är.
                 *
                 * horizontal = 1
                 * vertical   = 0
                 */
                float horizontalness =
                    saturate(
                        abs(
                            input.edgeData.y
                        )
                    );


                float verticalness =
                    sqrt(
                        saturate(
                            1.0 -
                            horizontalness *
                            horizontalness
                        )
                    );


                float erosionAngleScale =
                    lerp(
                        _HorizontalErosionScale,
                        1.0,
                        verticalness
                    );


               float maximumErosionDepth =
                    _EdgeErosionDepthPixels *
                    erosionAngleScale *
                    junctionKeep;


                float roadPixel =
                    floor(
                        input.edgeData.w *
                        pixelsPerUnit
                    );


                float sideId =
                    input.edgeData.z >
                    0.0
                        ? 17.0
                        : 83.0;


                float edgeNoise =
                    GetEdgeProfileNoise(
                        roadPixel,
                        sideId,
                        _RoadSeed
                    );


                /*
                 * Pow biasar majoriteten mot mindre
                 * erosion men tillåter ibland djupare jack.
                 */
                float erosionDepth =
                    maximumErosionDepth *
                    pow(
                        edgeNoise,
                        1.6
                    ) *
                    _EdgeErosionStrength;

                /*
                 * Vi vill nagga pixelart i diskreta pixelsteg,
                 * inte med sub-pixel-djup.
                 */
                erosionDepth =
                    floor(
                        erosionDepth +
                        0.5
                    );


                // -------------------------------------------------
                // MAIN EDGE EROSION
                // -------------------------------------------------

                if (_EdgeErosionEnabled >
                    0.5)
                {
                    /*
                     * Allt utanför den proceduralt naggade
                     * kanten kastas bort helt.
                     *
                     * Ground under vägen blir alltså synlig.
                     */
                    clip(
                        edgeDistancePixels -
                        erosionDepth -
                        0.001
                    );


                    // ---------------------------------------------
                    // INNER SINGLE-PIXEL HOLES
                    // ---------------------------------------------

                    // ---------------------------------------------
                    // INNER ISOLATED PIXEL HOLES
                    // ---------------------------------------------

                    float2 textureSize =
                        float2(
                            _MainTex_TexelSize.z,
                            _MainTex_TexelSize.w
                        );


                    /*
                     * UV:n är world-space-mappad.
                     *
                     * Detta ger oss den faktiska RöK-pixelcellen
                     * som fragmentet ligger i.
                     */
                    float2 worldPixel =
                        floor(
                            input.uv *
                            textureSize
                        );


                    float pinholeRange =
                        maximumErosionDepth +
                        _InnerPinholeDepthPixels *
                        erosionAngleScale;


                    float pinholeDepthFade =
                        1.0 -
                        saturate(
                            edgeDistancePixels /
                            max(
                                0.001,
                                pinholeRange
                            )
                        );


                    float pinholeChance =
                        _InnerPinholeChance *
                        pinholeDepthFade *
                        erosionAngleScale;

                     pinholeChance *=
                        junctionKeep;


                    /*
                     * Stabil 2D-slump.
                     */
                    float currentRandom =
                        HashPixel(
                            worldPixel,
                            _RoadSeed
                        );


                    /*
                     * Vi kräver dessutom att grannpixlarna INTE också
                     * har valts.
                     *
                     * Det gör pinholes verkligt isolerade och förhindrar
                     * vertikala/horizontal streaks.
                     */
                    float leftRandom =
                        HashPixel(
                            worldPixel +
                            float2(
                                -1.0,
                                0.0
                            ),
                            _RoadSeed
                        );


                    float rightRandom =
                        HashPixel(
                            worldPixel +
                            float2(
                                1.0,
                                0.0
                            ),
                            _RoadSeed
                        );


                    float downRandom =
                        HashPixel(
                            worldPixel +
                            float2(
                                0.0,
                                -1.0
                            ),
                            _RoadSeed
                        );


                    float upRandom =
                        HashPixel(
                            worldPixel +
                            float2(
                                0.0,
                                1.0
                            ),
                            _RoadSeed
                        );


                    bool isPinhole =
                        currentRandom <
                            pinholeChance &&

                        leftRandom >=
                            pinholeChance &&

                        rightRandom >=
                            pinholeChance &&

                        downRandom >=
                            pinholeChance &&

                        upRandom >=
                            pinholeChance;


                    if (isPinhole)
                    {
                        clip(-1.0);
                    }
                                    }


                // -------------------------------------------------
                // SURFACE
                // -------------------------------------------------

                fixed4 color =
                    tex2D(
                        _MainTex,
                        input.uv
                    ) *
                    _Color;


                /*
                 * Respektera eventuell alpha som finns
                 * i själva surface-texturen.
                 */
                clip(
                    color.a -
                    0.001
                );


                // -------------------------------------------------
                // SHOULDER
                // -------------------------------------------------

                if (_ShoulderEnabled >
                    0.5 &&
                    _ShoulderWidthPixels >
                    0.001)
                {
                    /*
                     * Eftersom erosionDepth är samma värde som
                     * klippte kanten ovan får shouldern exakt
                     * samma naggade kontur.
                     */
                    float distanceFromErodedEdge =
                        max(
                            0.0,
                            edgeDistancePixels -
                            erosionDepth
                        );


                    float shoulderGradient =
                        1.0 -
                        saturate(
                            distanceFromErodedEdge /
                            _ShoulderWidthPixels
                        );


                    bool isUpperEdge =
                        input.edgeData.y >
                        0.0001;


                    float sideStrength =
                        isUpperEdge
                            ? _UpperShoulderStrength
                            : _LowerShoulderStrength;


                    /*
                     * World-space upper/lower-perspektivet
                     * är starkast när vägen är horisontell.
                     *
                     * När vägen blir lodrät tonas shouldern
                     * automatiskt bort.
                     */
                    float shoulderStrength =
                        shoulderGradient *
                        sideStrength *
                        horizontalness;

                        shoulderStrength *=
                    junctionKeep;


                    float finalShoulderAmount =
                        saturate(
                            shoulderStrength *
                            _ShoulderColor.a
                        );


                    color.rgb =
                        lerp(
                            color.rgb,
                            _ShoulderColor.rgb,
                            finalShoulderAmount
                        );
                }


                return color;
            }

            ENDCG
        }
    }

    FallBack Off
}