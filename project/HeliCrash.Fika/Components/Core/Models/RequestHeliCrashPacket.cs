using EFT.InventoryLogic;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Packets;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

public class RequestHeliCrashPacket : INetSerializable, IRequestPacket
{
    public enum PacketType
    {
        Request,
        Response,
    }

    public PacketType packetType;
    public Vector3 position;
    public Vector3 rotation;
    public bool hasLoot;
    public Item containerItem;
    public int netId;

    public RequestHeliCrashPacket()
    {
        packetType = PacketType.Request;
    }

    private RequestHeliCrashPacket(
        Vector3 position,
        Vector3 rotation,
        bool hasLoot,
        Item containerItem,
        int netId
    )
    {
        packetType = PacketType.Response;
        this.position = position;
        this.rotation = rotation;
        this.hasLoot = hasLoot;
        this.containerItem = containerItem;
        this.netId = netId;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(packetType);
        if (packetType == PacketType.Request)
        {
            return;
        }
        writer.PutUnmanaged(position);
        writer.PutUnmanaged(rotation);
        writer.Put(hasLoot);
        if (hasLoot)
        {
            writer.PutItem(containerItem);
            writer.Put(netId);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        packetType = reader.GetEnum<PacketType>();
        if (packetType == PacketType.Request)
        {
            return;
        }
        position = reader.GetUnmanaged<Vector3>();
        rotation = reader.GetUnmanaged<Vector3>();
        hasLoot = reader.GetBool();
        if (hasLoot)
        {
            containerItem = reader.GetItem();
            netId = reader.GetInt();
        }
    }

    public void HandleRequest(NetPeer peer, FikaServer server)
    {
        var requestEvent = HeliCrashRequestEvent.Create(
            (spawner, logger) =>
            {
                Location spawnLocation = spawner.SpawnLocation;
                Item item = spawner.ContainerItem;

                var responsePacket = new RequestHeliCrashPacket(
                    spawnLocation.Position,
                    spawnLocation.Rotation,
                    item != null,
                    item,
                    spawner.ContainerNetId
                );

                if (logger != null)
                {
                    logger.LogInfo($"HeliCrash response packet: {responsePacket}");
                    logger.LogInfo(
                        $"Sending HeliCrash response to Fika Client {peer.Id.ToString()}"
                    );
                }

                server.SendDataToPeer(ref responsePacket, DeliveryMethod.ReliableOrdered, peer);
            }
        );
        EventDispatcher<HeliCrashRequestEvent>.Dispatch(ref requestEvent);

        // RaidLifetimeScope raidLifetimeScope = LifetimeScopeExtensions.GetRaidLifetimeScope();
        //
        // var heliCrashSpawner = (ServerHeliCrashSpawner)
        //     raidLifetimeScope.Container.Resolve(typeof(ServerHeliCrashSpawner));
        //
        // heliCrashSpawner
        //     .OnReceiveRequest(logger =>
        //     {
        //         Location spawnLocation = heliCrashSpawner.SpawnLocation;
        //         Item item = heliCrashSpawner.ContainerItem;
        //
        //         var responsePacket = new RequestHeliCrashPacket(
        //             spawnLocation.Position,
        //             spawnLocation.Rotation,
        //             item != null,
        //             item,
        //             heliCrashSpawner.ContainerNetId
        //         );
        //
        //         if (logger != null)
        //         {
        //             logger.LogInfo($"HeliCrash response packet: {responsePacket}");
        //             logger.LogInfo(
        //                 $"Sending HeliCrash response to Fika Client {peer.Id.ToString()}"
        //             );
        //         }
        //
        //         server.SendDataToPeer(ref responsePacket, DeliveryMethod.ReliableOrdered, peer);
        //     })
        //     .Forget();
    }

    public void HandleResponse()
    {
        var responseEvent = HeliCrashResponseEvent.Create(this);
        EventDispatcher<HeliCrashResponseEvent>.Dispatch(ref responseEvent);

        // RaidLifetimeScope raidLifetimeScope = LifetimeScopeExtensions.GetRaidLifetimeScope();
        //
        // var heliCrashSpawner = (ClientHeliCrashSpawner)
        //     raidLifetimeScope.Container.Resolve(typeof(ClientHeliCrashSpawner));
        //
        // heliCrashSpawner.OnReceiveResponse(this);
    }

    public override string ToString()
    {
        return string.Format(
            "PacketType={0}, Position={1}, Rotation={2}, HasLoot={3}, ContainerItem={4}, NetId={5}",
            packetType.ToString(),
            position.ToString(),
            rotation.ToString(),
            hasLoot.ToString(),
            containerItem,
            netId.ToString()
        );
    }
}
