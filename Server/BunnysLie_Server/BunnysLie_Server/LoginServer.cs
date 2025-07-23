using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BunnysLie_Server
{
    public class LoginServer : Server
    {
        public enum ePacketType_LoginServer
        {

        }
        protected override void OnReceivePacketFromUser(User user, NetworkDataReader reader, byte channelNumber, ePacketSafefyLevel safetyLevel)
        {
            byte packet = reader.ReadByte();
            ePacketType_LoginServer packetType_LoginServer = (ePacketType_LoginServer)packet;
            switch(packetType_LoginServer)
            {
                default:
                    Console.WriteLine($"Unknown packet type: {packet}");
                    break;
            }
        }

        protected override void OnUserConnected(User user)
        {
        }

        protected override void OnUserDisconnected(User user)
        {
        }

        protected override void Update()
        {
            //매 Tick마다 실행
        }
    }
}
