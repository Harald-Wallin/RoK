using UnityEngine;

/// <summary>
/// Den aktuella runtime-instansen av en channel.
/// </summary>
public sealed class ChannelRuntime
{
    internal ChannelRuntime(ChannelRequest request)
    {
        Request = request;
    }

    public ChannelRequest Request
    {
        get;
    }

    public object Owner =>
        Request?.Owner;

    public string DisplayName =>
        Request != null
            ? Request.DisplayName
            : "Channeling";

    public Sprite Icon =>
        Request?.Icon;

    public float Duration =>
        Request != null
            ? Request.Duration
            : 0f;

    public float TickInterval =>
        Request != null
            ? Request.TickInterval
            : 0f;

    public bool IsReversed =>
    Request != null &&
    Request.IsReversed;

    public float ElapsedTime
    {
        get;
        internal set;
    }

    public int TickCount
    {
        get;
        internal set;
    }

    public float RemainingTime =>
        Mathf.Max(
            0f,
            Duration -
            ElapsedTime);

    public float NormalizedProgress
    {
        get
        {
            if (Duration <= 0f)
                return 1f;

            return Mathf.Clamp01(
                ElapsedTime /
                Duration);
        }
    }


}
