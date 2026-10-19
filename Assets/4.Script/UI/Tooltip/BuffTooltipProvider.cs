using UnityEngine;

public class BuffTooltipProvider :
    ITooltipProvider
{
    private ActiveBuff buff;


    public BuffTooltipProvider(
        ActiveBuff buff)
    {
        this.buff =
            buff;
    }


    public TooltipData GetTooltipData(
        CharacterStats viewer)
    {
        TooltipData data =
            new TooltipData();


        if (buff == null)
        {
            return data;
        }


        data.title =
            buff.Name;

        data.description =
            buff.GetDescription(
                viewer
            );


        if (buff.stacks > 1)
        {
            data.stats.Add(
                $"Stacks: {buff.stacks}"
            );
        }


        /*
         * Permanenta buffs, exempelvis Wanderlust,
         * ska inte visa någon Remaining-rad.
         */
        if (!float.IsInfinity(
                buff.RemainingTime))
        {
            data.stats.Add(
                $"<color=white>Remaining: " +
                $"{FormatDetailedTime(buff.RemainingTime)}</color>"
            );
        }


        data.showFooter =
            false;


        return data;
    }


    private string FormatDetailedTime(
        float seconds)
    {
        int h =
            Mathf.FloorToInt(
                seconds /
                3600f
            );

        int m =
            Mathf.FloorToInt(
                (
                    seconds %
                    3600f
                ) /
                60f
            );

        int s =
            Mathf.FloorToInt(
                seconds %
                60f
            );


        if (h > 0)
        {
            return
                $"{h}h {m}m {s}s";
        }


        if (m > 0)
        {
            return
                $"{m}m {s}s";
        }


        return
            $"{s}s";
    }
}
