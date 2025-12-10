using System.Reflection;
using EFT;
using EFT.Airdrop;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Patching;
using ZLinq;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidStartPatch : ModulePatch
{
    private static ConfigurationService s_configService;
    private static Logger s_logger;
    private static RaidLifetimeScopeController s_raidLifetimeScopeController;

    public RaidStartPatch(
        ConfigurationService configService,
        Logger logger,
        RaidLifetimeScopeController raidLifetimeScopeController
    )
    {
        s_configService = configService;
        s_logger = logger;
        s_raidLifetimeScopeController = raidLifetimeScopeController;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    private static void PatchPostfix(GameWorld __instance)
    {
        string location = __instance.MainPlayer.Location;

        bool crashAvailable =
            location.ToLower() == "sandbox"
            || LocationScene.GetAll<AirdropPoint>().AsValueEnumerable().Any();

        bool shouldSpawnCrash =
            s_configService.SpawnAllCrashSites.Value
            || BlessRNG.RngBool(s_configService.HeliCrashChance.Value);

        if (crashAvailable && shouldSpawnCrash)
        {
            if (s_configService.LoggingEnabled.Value)
            {
                s_logger.LogInfo("Can spawn heli crash site, creating RaidLifetimeScope");
            }

            s_raidLifetimeScopeController.CreateScope().Build();
        }
    }
}
