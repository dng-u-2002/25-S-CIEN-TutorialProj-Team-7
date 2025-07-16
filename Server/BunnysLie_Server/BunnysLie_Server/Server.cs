using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BunnysLie_Server
{
    public class User
    {
        public User(int id)
        {
            Id = id;
        }
        public int Id { get; private set; }
    }
    public abstract class Server : INetEventListener
    {
        NetManager InternelServer;
        protected NetworkDataWriter PacketWriter { get; private set; }

        protected Dictionary<int, Tuple<User, NetPeer>> ConnectedUsers = new Dictionary<int, Tuple<User, NetPeer>>();

        protected void SendPacket(User receiver)
        {
            PacketWriter.SendPacket(ConnectedUsers[receiver.Id].Item2);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        private User GetUserFromPeer(NetPeer peer)
        {
            if (ConnectedUsers.TryGetValue(peer.Id, out var userTuple))
            {
                return userTuple.Item1;
            }
            return null;
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            OnReceivePacketFromUser(GetUserFromPeer(peer), new NetworkDataReader(reader), channelNumber, (ePacketSafefyLevel)deliveryMethod);
        }
        protected abstract void OnReceivePacketFromUser(User user, NetworkDataReader reader, byte channelNumber, ePacketSafefyLevel safetyLevel);

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnPeerConnected(NetPeer peer)
        {
            User user = new User(peer.Id);

            bool result = ConnectedUsers.TryAdd(peer.Id, new Tuple<User, NetPeer>(user, peer));
            if (result == false)
                return;
            Console.WriteLine($"[User Connection] New User connected: {user.Id}");
            OnUserConnected(user);
        }
        protected abstract void OnUserConnected(User user);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (!ConnectedUsers.TryGetValue(peer.Id, out var userTuple))
            {
                Console.WriteLine($"[User Disconnection] User {peer.Id} not found.");
                return;
            }
            Console.WriteLine($"[User Disconnection] User disconnected : {peer.Id}");
            OnUserDisconnected(GetUserFromPeer(peer));
            ConnectedUsers.Remove(peer.Id);
        }
        protected abstract void OnUserDisconnected(User user);

        public void Start(int port)
        {
            PacketWriter = new NetworkDataWriter();
            InternelServer = new NetManager(this)
            {
                AutoRecycle = true,
                DisconnectTimeout = 5000,
                UnconnectedMessagesEnabled = true,
                ChannelsCount = 2,
                MaxConnectAttempts = 3,
                PingInterval = 1000 / 20 // TICK_RATE
            };
            InternelServer.Start(port);
            Console.WriteLine($"Server started on port {port}");

        }
        DateTime nextTick;
        public void Run_SingleTick()
        {
            const int TICK_RATE = 20;
            nextTick = DateTime.UtcNow.AddMilliseconds(1000 / TICK_RATE);
            //while (true)
            {
                InternelServer.PollEvents();
                if (DateTime.UtcNow >= nextTick)
                {
                    nextTick = DateTime.UtcNow.AddMilliseconds(1000 / TICK_RATE);
                    Update();
                }
                //Thread.Sleep(1);
            }
        }

        protected abstract void Update();

        public void Stop()
        {
            InternelServer.Stop();
        }
    }
}
