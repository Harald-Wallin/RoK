using System;
using UnityEngine;

/// <summary>
/// Generell runtime-controller för channeling.
///
/// Används av exempelvis:
/// - mat
/// - channel-abilities
/// - crafting
/// - gathering
/// - world interactions
///
/// Controllern ansvarar endast för channelens tid och events.
/// Det ägande systemet ansvarar för avbrottsregler och effekt.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class CharacterChannelController :
    MonoBehaviour
{
    private ChannelRuntime activeChannel;

    private float nextTickTime;

    public ChannelRuntime ActiveChannel =>
        activeChannel;

    public bool IsChanneling =>
        activeChannel != null;

    public float NormalizedProgress =>
        activeChannel != null
            ? activeChannel.NormalizedProgress
            : 0f;

    public float RemainingTime =>
        activeChannel != null
            ? activeChannel.RemainingTime
            : 0f;

    public event Action<ChannelRuntime>
        ChannelStarted;

    public event Action<ChannelRuntime>
        ChannelProgressChanged;

    public event Action<ChannelRuntime, int>
        ChannelTicked;

    public event Action<ChannelRuntime>
        ChannelCompleted;

    public event Action<ChannelRuntime>
        ChannelCancelled;

    private void Update()
    {
        if (activeChannel == null)
            return;

        activeChannel.ElapsedTime =
            Mathf.Min(
                activeChannel.Duration,
                activeChannel.ElapsedTime +
                Time.deltaTime);

        ProcessTicks();

        ChannelProgressChanged?.Invoke(
            activeChannel);

        if (activeChannel.ElapsedTime >=
            activeChannel.Duration)
        {
            CompleteChannel();
        }
    }

    public bool TryStartChannel(
        ChannelRequest request)
    {
        if (request == null ||
            request.Owner == null)
        {
            return false;
        }

        if (IsChanneling)
            return false;

        activeChannel =
            new ChannelRuntime(
                request);

        nextTickTime =
            request.HasTicks
                ? request.TickInterval
                : float.PositiveInfinity;

        ChannelStarted?.Invoke(
            activeChannel);

        ChannelProgressChanged?.Invoke(
            activeChannel);

        return true;
    }

    public bool IsOwnedBy(
        object owner)
    {
        return
            owner != null &&
            activeChannel != null &&
            ReferenceEquals(
                activeChannel.Owner,
                owner);
    }

    /// <summary>
    /// Avbryter channelingen endast om den ägs av angivet
    /// system.
    /// </summary>
    public bool CancelChannel(
        object owner)
    {
        if (!IsOwnedBy(owner))
            return false;

        return CancelActiveChannel();
    }

    /// <summary>
    /// Tvingar den aktiva channelingen att avbrytas oavsett
    /// ägare.
    ///
    /// Använd sparsamt, exempelvis vid death/reset.
    /// </summary>
    public bool CancelActiveChannel()
    {
        if (activeChannel == null)
            return false;

        ChannelRuntime cancelled =
            activeChannel;

        ClearRuntime();

        ChannelCancelled?.Invoke(
            cancelled);

        return true;
    }

    private void ProcessTicks()
    {
        if (activeChannel == null ||
            activeChannel.TickInterval <= 0f)
        {
            return;
        }

        /*
         * while används så att inga ticks tappas vid tillfälligt
         * låg framerate.
         *
         * En channel på 9 sekunder med intervallet 3 ger ticks
         * vid 3, 6 och 9 sekunder.
         */
        while (
            activeChannel.ElapsedTime >=
                nextTickTime &&
            nextTickTime <=
                activeChannel.Duration +
                0.0001f)
        {
            activeChannel.TickCount++;

            ChannelTicked?.Invoke(
                activeChannel,
                activeChannel.TickCount);

            nextTickTime +=
                activeChannel.TickInterval;
        }
    }

    private void CompleteChannel()
    {
        if (activeChannel == null)
            return;

        ChannelRuntime completed =
            activeChannel;

        ClearRuntime();

        ChannelCompleted?.Invoke(
            completed);
    }

    private void ClearRuntime()
    {
        activeChannel = null;
        nextTickTime = 0f;
    }

    private void OnDisable()
    {
        CancelActiveChannel();
    }
}
