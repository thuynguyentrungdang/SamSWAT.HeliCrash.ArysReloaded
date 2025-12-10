using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class HeliCrashLocationService
{
    private readonly HeliCrashLocations _crashLocations;

    public HeliCrashLocationService()
    {
        string crashSitesJsonPath = Path.Combine(FileUtil.Directory, "HeliCrashLocations.json");
        _crashLocations = FileUtil.LoadJson<HeliCrashLocations>(crashSitesJsonPath);
    }

    public List<Location> GetCrashLocations(string map)
    {
        return map.ToLower() switch
        {
            "bigmap" => _crashLocations.Customs,
            "interchange" => _crashLocations.Interchange,
            "rezervbase" => _crashLocations.Rezerv,
            "shoreline" => _crashLocations.Shoreline,
            "woods" => _crashLocations.Woods,
            "lighthouse" => _crashLocations.Lighthouse,
            "tarkovstreets" => _crashLocations.StreetsOfTarkov,
            "sandbox" => _crashLocations.GroundZero,
            "develop" => _crashLocations.Develop,
            _ => null,
        };
    }
}
