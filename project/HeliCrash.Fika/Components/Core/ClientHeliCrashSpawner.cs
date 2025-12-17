using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
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

    private UniTaskCompletionSource<RequestHeliCrashPacket> _currentRequest;

    public ClientHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        LootContainerFactory lootContainerFactory
    )
        : base(configService, logger)
    {
        _configService = configService;
        _logger = logger;
        _lootContainerFactory = lootContainerFactory;

        EventDispatcher<HeliCrashResponseEvent>.Subscribe(OnReceiveResponse);
    }

    public override void Dispose()
    {
        EventDispatcher<HeliCrashResponseEvent>.UnsubscribeAll();
        base.Dispose();
    }

    protected override async UniTask SpawnCrashSite(CancellationToken cancellationToken = default)
    {
        GameObject choppa = await InstantiateCrashSiteObject(cancellationToken: cancellationToken);

        RequestHeliCrashPacket responsePacket = await RequestDataFromServer(cancellationToken);

        var container = choppa.GetComponentInChildren<LootableContainer>();

        if (responsePacket.hasLoot)
        {
            container.NetId = responsePacket.netId;

            await _lootContainerFactory.CreateContainer(
                container,
                responsePacket.containerItem,
                cancellationToken
            );
        }
        else
        {
            // Disable the container game object
            container.transform.parent.gameObject.SetActive(false);
        }

        choppa.transform.SetPositionAndRotation(
            responsePacket.position,
            Quaternion.Euler(responsePacket.rotation)
        );

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogWarning($"Heli crash site spawned at {responsePacket.position.ToString()}");
        }

        choppa.SetActive(true);
    }

    private async UniTask<RequestHeliCrashPacket> RequestDataFromServer(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _currentRequest = new UniTaskCompletionSource<RequestHeliCrashPacket>();

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
                await _currentRequest.Task.TimeoutWithoutException(
                    TimeSpan.FromSeconds(20),
                    DelayType.Realtime,
                    taskCancellationTokenSource: cts
                );

            if (isTimeout)
            {
                var timeoutException = new TimeoutException(
                    "HeliCrash Fika Client request timed out waiting for response from the Fika Server!"
                );
                _currentRequest.TrySetException(timeoutException);
                return await _currentRequest.Task;
            }

            return responsePacket;
        }
        finally
        {
            _currentRequest = null;
        }
    }

    private void OnReceiveResponse(ref HeliCrashResponseEvent responseEvent)
    {
        if (!_currentRequest.TrySetResult(responseEvent.packet))
        {
            _logger.LogError("Failed to set UniTaskCompletionSource result!");
            _currentRequest.TrySetException(new InvalidPacketException(""));
            return;
        }

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogInfo(
                $"Setting UniTaskCompletionSource result = RequestHeliCrashPacket ({responseEvent.packet})"
            );
        }
    }
}
