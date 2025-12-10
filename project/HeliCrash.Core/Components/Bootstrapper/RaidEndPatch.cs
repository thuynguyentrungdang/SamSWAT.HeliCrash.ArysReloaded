using System.Reflection;
using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Patching;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidEndPatch : ModulePatch
{
    private static ConfigurationService s_configService;
    private static Logger s_logger;
    private static RaidLifetimeScopeController s_raidLifetimeScopeController;

    public RaidEndPatch(
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
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.Dispose));
    }

    [PatchPrefix]
    private static void PatchPrefix(GameWorld __instance)
    {
        s_raidLifetimeScopeController.DisposeScope();

        if (s_configService.LoggingEnabled.Value)
        {
            s_logger.LogInfo("Raid ended, disposed of RaidLifetimeScope");
        }
    }
}
