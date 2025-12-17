using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Bootstrapper;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[BepInPlugin(
    "com.samswat.helicrash.arysreloaded.fika",
    "SamSWAT's HeliCrash: Arys Reloaded - Fika Sync",
    ModMetadata.VERSION
)]
[BepInDependency("com.SPT.core", ModMetadata.TARGET_SPT_VERSION)]
[BepInDependency("com.arys.unitytoolkit", "2.0.1")]
[BepInDependency("com.fika.core")]
public class HeliCrashFikaPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        if (!DetectCoreMod())
        {
            throw new DllNotFoundException(
                "HeliCrash's Fika Sync addon is installed but the HeliCrash mod is not installed in the same directory. Please check you have installed the mod and/or the Fika Sync addon correctly!"
            );
        }

        HeliCrashPlugin.PostAwake += Initialize;
    }

    private static bool DetectCoreMod()
    {
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string[] assemblies = Directory.GetFiles(directory, "*.dll");
        return assemblies.Any(assembly => assembly.Contains("SamSWAT.HeliCrash.ArysReloaded.Core"));
    }

    private static void Initialize()
    {
        new OverrideRaidLifetimeScopePatch().Enable();
        new HeadlessRaidLoadScreenPatch().Enable();

        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(
            OnFikaNetworkManagerCreated
        );
    }

    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent @event)
    {
        switch (@event.Manager)
        {
            case FikaServer server:
#if DEBUG
                FikaGlobals.LogInfo("Registering RequestHeliCrashPacket on Fika Server");
#endif
                server.RegisterPacket<RequestHeliCrashPacket, NetPeer>(OnHeliCrashRequest);
                break;
            case FikaClient client:
#if DEBUG
                FikaGlobals.LogInfo("Registering RequestHeliCrashPacket on Fika Client");
#endif
                client.RegisterPacket<RequestHeliCrashPacket>(OnHeliCrashResponse);
                break;
        }
    }

    private static void OnHeliCrashRequest(RequestHeliCrashPacket packet, NetPeer peer)
    {
#if DEBUG
        FikaGlobals.LogInfo(
            $"Received HeliCrash request from Fika Client {peer.Id.ToString()}. Handling request..."
        );
#endif
        packet.HandleRequest(peer, Singleton<FikaServer>.Instance);
    }

    private static void OnHeliCrashResponse(RequestHeliCrashPacket packet)
    {
#if DEBUG
        FikaGlobals.LogInfo("Received HeliCrash response from Fika Server. Handling response...");
#endif
        packet.HandleResponse();
    }
}
