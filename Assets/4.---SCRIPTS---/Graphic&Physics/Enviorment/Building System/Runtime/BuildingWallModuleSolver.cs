using System.Collections.Generic;
using UnityEngine;

public readonly struct BuildingWallModuleChoice
{
    public readonly int LengthPixels;
    public readonly Sprite Sprite;

    public BuildingWallModuleChoice(
        int lengthPixels,
        Sprite sprite)
    {
        LengthPixels = lengthPixels;
        Sprite = sprite;
    }
}

public static class BuildingWallModuleSolver
{
    private const int SmallModulePixels = 64;
    private const int LargeModulePixels = 128;

    /// <summary>
    /// Löser en horisontell wall-run med exakt total längd.
    ///
    /// Prioritet:
    /// 1. exakt fill
    /// 2. symmetri
    /// 3. större moduler
    ///
    /// Med både 64 och 128 tillgängliga blir t.ex. 320px:
    /// 128 + 64 + 128.
    /// </summary>
    public static bool TrySolve(
        int runLengthPixels,
        Sprite sprite64,
        Sprite sprite128,
        out List<BuildingWallModuleChoice> result)
    {
        result = new List<BuildingWallModuleChoice>();

        if (runLengthPixels <= 0 ||
            runLengthPixels % SmallModulePixels != 0)
        {
            return false;
        }

        int units =
            runLengthPixels /
            SmallModulePixels;

        bool has64 =
            sprite64 != null;

        bool has128 =
            sprite128 != null;

        if (!has64 && !has128)
            return false;

        // Only 64px artwork available.
        if (has64 && !has128)
        {
            for (int i = 0; i < units; i++)
            {
                result.Add(
                    new BuildingWallModuleChoice(
                        SmallModulePixels,
                        sprite64
                    )
                );
            }

            return true;
        }

        // Only 128px artwork available.
        if (!has64 && has128)
        {
            if (units % 2 != 0)
                return false;

            for (int i = 0; i < units / 2; i++)
            {
                result.Add(
                    new BuildingWallModuleChoice(
                        LargeModulePixels,
                        sprite128
                    )
                );
            }

            return true;
        }

        // Both available.
        if (units % 2 == 0)
        {
            // Even 64px-units are perfectly symmetric as all 128px.
            for (int i = 0; i < units / 2; i++)
            {
                result.Add(
                    new BuildingWallModuleChoice(
                        LargeModulePixels,
                        sprite128
                    )
                );
            }

            return true;
        }

        /*
         * Odd total:
         * reserve one 64px center module, then mirror both sides.
         *
         * 320px = 5 units:
         * left half = 2 units = 128
         * center = 64
         * right = mirror
         * => 128 + 64 + 128
         *
         * 448px = 7 units:
         * left half = 3 units = 128 + 64
         * center = 64
         * right = 64 + 128
         * => 128 + 64 + 64 + 64 + 128
         */
        int halfUnits =
            (units - 1) /
            2;

        List<BuildingWallModuleChoice> left =
            new();

        while (halfUnits >= 2)
        {
            left.Add(
                new BuildingWallModuleChoice(
                    LargeModulePixels,
                    sprite128
                )
            );

            halfUnits -= 2;
        }

        if (halfUnits == 1)
        {
            left.Add(
                new BuildingWallModuleChoice(
                    SmallModulePixels,
                    sprite64
                )
            );
        }

        for (int i = 0; i < left.Count; i++)
        {
            result.Add(left[i]);
        }

        result.Add(
            new BuildingWallModuleChoice(
                SmallModulePixels,
                sprite64
            )
        );

        for (int i = left.Count - 1; i >= 0; i--)
        {
            result.Add(left[i]);
        }

        return true;
    }
}
