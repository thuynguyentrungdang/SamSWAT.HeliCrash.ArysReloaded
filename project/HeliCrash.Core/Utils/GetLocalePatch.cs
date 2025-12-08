using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal class GetLocalePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocaleClass), nameof(LocaleClass.ReloadBackendLocale));
    }

    [PatchPostfix]
    private static async void PatchPostfix(string locale, Task __result)
    {
        try
        {
            LocalizationService.LoadMappings(locale);
            HeliCrashPlugin.SetupMainConfigBindings();
            HeliCrashPlugin.SetupDebugConfigBindings();

            await __result;
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError(
                $"[SamSWAT.HeliCrash.ArysReloaded] Error patching {nameof(LocaleClass.ReloadBackendLocale)}: {ex.Message}\n{ex.StackTrace})"
            );
        }
    }
}
