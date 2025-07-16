using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BunnysLie_Server
{
    public class InGameServer : Server
    {
        public enum ePacketType
        {
            None = 0,
            Error = 1,
            HandShake_S2U,
            HandShake_U2S,
            Chat_Send,
            Chat_Receive
        }

        protected override void OnReceivePacketFromUser(User user, NetworkDataReader reader, byte channelNumber, ePacketSafefyLevel safetyLevel)
        {
            Console.WriteLine($"[Packet Received] User {user.Id} sent a packet on channel {channelNumber} with safety level {safetyLevel}.");
            switch ((ePacketType)reader.ReadByte())
            {
                case ePacketType.HandShake_U2S:
                    Console.WriteLine($"[HandShake] User {user.Id} sent Handshake to Server.");
                    break;
                case ePacketType.Chat_Send:
                    int sender = reader.ReadInt();
                    string message = reader.ReadString();
                    foreach (var u in ConnectedUsers.Values)
                    {
                        PacketWriter.CreateNewPacket((byte)ePacketType.Chat_Receive);
                        PacketWriter.WriteInt(sender);
                        PacketWriter.WriteString(message);
                        SendPacket(u.Item1);
                    }
                    break;
                default:
                    Console.WriteLine($"[Warning] Unknown packet type received from User {user.Id}.");
                    break;
            }
        }

        protected override void OnUserConnected(User user)
        {
            Console.WriteLine($"[User Connection] New User connected: {user.Id}");

            PacketWriter.CreateNewPacket((byte)ePacketType.HandShake_S2U);
            PacketWriter.WriteInt(user.Id);
            SendPacket(user);
            Console.WriteLine($"[HandShake] Server sent Handshake to User {user:id}");
        }

        protected override void OnUserDisconnected(User user)
        {
            Console.WriteLine($"[User Disconnection] User disconnected: {user.Id}");
        }

        protected override void Update()
        {
        }
    }
}
