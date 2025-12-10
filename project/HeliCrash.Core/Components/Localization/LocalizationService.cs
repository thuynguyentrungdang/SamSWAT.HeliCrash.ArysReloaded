using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class LocalizationService
{
    private Dictionary<string, string> _locales;

    public void LoadLocale(string locale)
    {
        if (_locales != null)
        {
            return;
        }

        string path = Path.Combine(FileUtil.Directory, "Locales.jsonc");

        var locales = FileUtil.LoadJson<Dictionary<string, Dictionary<string, string>>>(path);

        _locales = locales[locale];
    }

    public string Localize(string key)
    {
        if (_locales == null)
        {
            throw new InvalidOperationException(
                "[SamSWAT.HeliCrash.ArysReloaded] Localization mappings not yet loaded! Load it first with LocalizationService.LoadMappings()"
            );
        }

        return _locales[key];
    }
}
