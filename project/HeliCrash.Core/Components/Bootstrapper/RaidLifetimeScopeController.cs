using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidLifetimeScopeController
{
    private ApplicationLifetimeScope _appLifetimeScope;
    private LifetimeScope _raidLifetimeScope;

    public void Initialize(ApplicationLifetimeScope appLifetimeScope)
    {
        _appLifetimeScope = appLifetimeScope;
    }

    public LifetimeScope CreateScope()
    {
        _raidLifetimeScope = _appLifetimeScope.CreateChild<RaidLifetimeScope>(
            Singleton<GameWorld>.Instance.transform,
            childScopeName: "RaidLifetimeScope"
        );

        return _raidLifetimeScope;
    }

    public void DisposeScope()
    {
        if (_raidLifetimeScope != null)
        {
            _raidLifetimeScope.Dispose();
            _raidLifetimeScope = null;
        }
    }
}
