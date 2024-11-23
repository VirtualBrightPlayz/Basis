﻿using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking.Compression;
using Basis.Scripts.Networking.Factorys;
using Basis.Scripts.Networking.NetworkedAvatar;
using Basis.Scripts.Networking.NetworkedPlayer;
using BasisSerializer.OdinSerializer;
using DarkRift;
using DarkRift.Server.Plugins.Commands;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using static SerializableDarkRift;
namespace Basis.Scripts.Networking
{
    public static class BasisNetworkLocalCreation
    {
        public static async Task HandleAuthSuccess(Transform Parent)
        {
            BasisNetworkedPlayer NetworkedPlayer = await BasisPlayerFactoryNetworked.CreateNetworkedPlayer(new InstantiationParameters(Parent.position, Parent.rotation, Parent));
            ushort playerID = BasisNetworkManagement.Instance.Client.ID;
            BasisLocalPlayer BasisLocalPlayer = BasisLocalPlayer.Instance;
            NetworkedPlayer.ReInitialize(BasisLocalPlayer, playerID);
            if (BasisNetworkManagement.AddPlayer(NetworkedPlayer))
            {

                Debug.Log("added local Player " + playerID);
            }
            else
            {
                Debug.LogError("Cant add " + playerID);
            }
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                byte[] Information = BasisBundleConversionNetwork.ConvertBasisLoadableBundleToBytes(BasisLocalPlayer.AvatarMetaData);
                BasisNetworkAvatarCompressor.CompressAvatarData(NetworkedPlayer.NetworkSend, BasisLocalPlayer.Avatar.Animator);

                BasisNetworkManagement.Instance.readyMessage.localAvatarSyncMessage = NetworkedPlayer.NetworkSend.LASM;
                BasisNetworkManagement.Instance.readyMessage.clientAvatarChangeMessage = new ClientAvatarChangeMessage
                {
                    byteArray = Information,
                    loadMode = BasisLocalPlayer.AvatarLoadMode,
                };
                BasisNetworkManagement.Instance.readyMessage.playerMetaDataMessage = new PlayerMetaDataMessage
                {
                    playerUUID = BasisLocalPlayer.UUID,
                    playerDisplayName = BasisLocalPlayer.DisplayName
                };
                writer.Write(BasisNetworkManagement.Instance.readyMessage);
                Message ReadyMessage = Message.Create(BasisTags.ReadyStateTag, writer);
                BasisNetworkManagement.Instance.Client.SendMessage(ReadyMessage, BasisNetworking.EventsChannel, DeliveryMethod.ReliableOrdered);
                BasisNetworkManagement.OnLocalPlayerJoined?.Invoke(NetworkedPlayer, BasisLocalPlayer);
                BasisNetworkManagement.HasSentOnLocalPlayerJoin = true;
            }
        }

        public static void HandleAuthChallenge(DarkRiftReader reader)
        {
            byte[] data = reader.ReadBytes();
            byte[] response = BasisNetworkManagement.Instance.clientAuth.RespondToChallenge(data);
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(response);
                Message ResponseMessage = Message.Create(BasisTags.AuthChallengeResponse, writer);
                BasisNetworkManagement.Instance.Client.SendMessage(ResponseMessage, BasisNetworking.EventsChannel, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}