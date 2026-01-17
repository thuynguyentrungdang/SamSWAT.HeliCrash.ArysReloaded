using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;
using UnityEngine;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[UsedImplicitly]
public sealed class ClientHeliCrashSpawner : HeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;
    private readonly LootContainerFactory _lootContainerFactory;

    private RequestHeliCrashPacket _cachedPacket;

    public ClientHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        HeliCrashLocationService locationService,
        LootContainerFactory lootContainerFactory
    )
        : base(configService, logger, locationService)
    {
        _configService = configService;
        _logger = logger;
        _lootContainerFactory = lootContainerFactory;
    }

    protected override async UniTask<bool> ShouldSpawnCrashSite(
        CancellationToken cancellationToken = default
    )
    {
        using var requestHandler = new RequestHandler(_configService, _logger);

        _cachedPacket = await requestHandler.HandleRequest(
            timeoutSeconds: 300,
            cancellationToken: cancellationToken
        );

        return _cachedPacket.shouldSpawn;
    }

    protected override async UniTask SpawnCrashSite(CancellationToken cancellationToken = default)
    {
        GameObject choppa = await InstantiateCrashSiteObject(cancellationToken: cancellationToken);

        Door[] doors = choppa.GetComponentsInChildren<Door>();

        for (var i = 0; i < doors.Length; i++)
        {
            doors[i].NetId = _cachedPacket.doorNetIds[i];
            Singleton<GameWorld>.Instance.RegisterWorldInteractionObject(doors[i]);
        }

        var container = choppa.GetComponentInChildren<LootableContainer>();

        if (_cachedPacket.hasLoot)
        {
            container.NetId = _cachedPacket.containerNetId;

            await _lootContainerFactory.CreateContainer(
                container,
                _cachedPacket.containerItem,
                cancellationToken
            );
        }
        else
        {
            // Disable the container game object
            container.transform.parent.gameObject.SetActive(false);
        }

        choppa.transform.SetPositionAndRotation(
            _cachedPacket.position,
            Quaternion.Euler(_cachedPacket.rotation)
        );

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogWarning($"Heli crash site spawned at {_cachedPacket.position.ToString()}");
        }

        choppa.SetActive(true);
    }

    private class RequestHandler : IDisposable
    {
        private readonly ConfigurationService _configService;
        private readonly Logger _logger;

        private UniTaskCompletionSource<RequestHeliCrashPacket> _tcs;

        public RequestHandler(ConfigurationService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;

            _tcs = new UniTaskCompletionSource<RequestHeliCrashPacket>();

            EventDispatcher<HeliCrashResponseEvent>.Subscribe(OnReceiveResponse);
        }

        public void Dispose()
        {
            EventDispatcher<HeliCrashResponseEvent>.Unsubscribe(OnReceiveResponse);

            UniTaskCompletionSource<RequestHeliCrashPacket> tcs = Interlocked.Exchange(
                ref _tcs,
                null
            );

            tcs?.TrySetCanceled();
        }

        public async UniTask<RequestHeliCrashPacket> HandleRequest(
            int timeoutSeconds,
            CancellationToken cancellationToken = default
        )
        {
            UniTaskCompletionSource<RequestHeliCrashPacket> tcs = _tcs;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var requestPacket = new RequestHeliCrashPacket();

            if (_configService.LoggingEnabled.Value)
            {
                _logger.LogInfo("Sending HeliCrash request to Fika Server...");
            }

            Singleton<FikaClient>.Instance.SendData(
                ref requestPacket,
                DeliveryMethod.ReliableOrdered
            );

            (bool isTimeout, RequestHeliCrashPacket responsePacket) =
                await tcs.Task.TimeoutWithoutException(
                    TimeSpan.FromSeconds(timeoutSeconds),
                    DelayType.Realtime,
                    taskCancellationTokenSource: cts
                );

            cancellationToken.ThrowIfCancellationRequested();

            if (!isTimeout)
            {
                return responsePacket;
            }

            throw new TimeoutException(
                "Timed out while waiting for HeliCrash request from Fika Server! No helicopter crash site will be spawned!"
            );
        }

        private void OnReceiveResponse(ref HeliCrashResponseEvent responseEvent)
        {
            UniTaskCompletionSource<RequestHeliCrashPacket> tcs = _tcs;

            if (tcs == null)
            {
                _logger.LogError(
                    "Received response from Fika Server but the requesting CompletionSource is now invalid! Please report this error to the mod developer!"
                );
                return;
            }

            if (tcs.TrySetResult(responseEvent.packet))
            {
                if (_configService.LoggingEnabled.Value)
                {
                    _logger.LogInfo(
                        $"Received response from Fika Server: ({responseEvent.packet})"
                    );
                }
            }
        }
    }
}
