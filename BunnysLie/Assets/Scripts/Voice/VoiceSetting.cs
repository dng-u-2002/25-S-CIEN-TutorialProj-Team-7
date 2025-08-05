using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Voice
{
    public class VoiceSetting : MonoBehaviour
    {
        public VoiceSender VoiceSender;
        public VoiceReceiver VoiceReceiver;
        
        private VoiceSender localSender;
        private Dictionary<int, VoiceReceiver> receivers = new Dictionary<int, VoiceReceiver>();

        public void Initialize(NetPeer localpeer)
        {
            localSender = Instantiate(VoiceSender);
            localSender.peer = localpeer;
        }

        public void AddRemotePlayer(int playerId)
        {
            if (!receivers.ContainsKey(playerId))
            {
                var receiver = Instantiate(VoiceReceiver);
                receivers.Add(playerId, receiver);
            }
        }

        public void ReceiveVoiceData(int playerId, byte[] data)
        {
            if (receivers.TryGetValue(playerId, out var receiver))
            {
                receiver.OnVoiceDataReceived(data);
            }
        }

        public void SetMicActibe(bool isActive)
        {
            if (localSender != null)
            {
                localSender.transmitted = isActive;
            }
        }
    }
}