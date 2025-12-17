using System;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

public readonly struct HeliCrashResponseEvent : IEvent
{
    public readonly RequestHeliCrashPacket packet;

    [Obsolete("Use the static Create method instead")]
    public HeliCrashResponseEvent()
    {
        throw new InvalidOperationException("Please use the static Create method instead!");
    }

    private HeliCrashResponseEvent(RequestHeliCrashPacket packet)
    {
        this.packet = packet;
    }

    public static HeliCrashResponseEvent Create(RequestHeliCrashPacket packet)
    {
        return new HeliCrashResponseEvent(packet);
    }
}
