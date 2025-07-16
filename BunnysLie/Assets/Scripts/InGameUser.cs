using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VOYAGER_Server;
public enum ePacketType_InGameServer : byte
{
    None = 0,
    Error = 1,
    HandShake_S2U,
    HandShake_U2S,
    Chat_Send,
    Chat_Receive
}
public class InGameUser : User
{
    protected override void OnReceivePacketFromServer(NetworkDataReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        ePacketType_InGameServer packetType = (ePacketType_InGameServer)reader.ReadByte();
        Debug.Log($"[Server Connection] Received packet type: {packetType}");
        switch (packetType)
        {
            case ePacketType_InGameServer.HandShake_S2U:
                this.Id = reader.ReadInt();
                Debug.Log(Id);

                Debug.Log("[Server Connection] Received HandShake_S2U packet from server.");
                Writer.CreateNewPacket((byte)ePacketType_InGameServer.HandShake_U2S);
                Writer.SendPacket(ServerPeer);
                Debug.Log("[Server Connection] Sent HandShake_U2S packet to server.");
                break;

            case ePacketType_InGameServer.Chat_Receive:
                int id = reader.ReadInt();
                string message = reader.ReadString();
                OnChatReceived?.Invoke(id, message);
                Debug.Log($"[Chat] Received message from server: {message}");
                break;
        }
    }

    public Action<int, string> OnChatReceived;

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(200);
        GUILayout.EndVertical();
        GUILayout.Label($"서버 IP: {serverIP}:{serverPort}");
        if (GUILayout.Button("게임 시작"))
        {
            ConnectToServer();
        }
    }

    public void SendChat2Server(string message)
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.Chat_Send);
        Writer.WriteInt(Id);
        Writer.WriteString(message);
        Writer.SendPacket(ServerPeer);
        Debug.Log($"[Chat] Sent message to server: {message}");
    }
}
