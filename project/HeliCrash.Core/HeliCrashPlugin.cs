using System;
using BepInEx;
using BepInEx.Bootstrap;

namespace SamSWAT.HeliCrash.ArysReloaded;

[BepInPlugin(
    "com.SamSWAT.HeliCrash.ArysReloaded",
    "SamSWAT's HeliCrash: Arys Reloaded - Core",
    ModMetadata.VERSION
)]
[BepInDependency("com.SPT.core", ModMetadata.TARGET_SPT_VERSION)]
[BepInDependency("com.arys.unitytoolkit", "2.0.1")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class HeliCrashPlugin : BaseUnityPlugin
{
    public static bool FikaEnabled { get; private set; }

    private void Awake()
    {
        FikaEnabled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
        if (FikaEnabled) { }

        new InitializeApplicationLifetimeScopePatch(this, gameObject).Enable();

        AwakeEvent?.Invoke();
    }

    public static event Action AwakeEvent;
}
