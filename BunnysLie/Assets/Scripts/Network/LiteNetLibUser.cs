using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VOYAGER_Server;

public abstract class LiteNetLibUser : MonoBehaviour, INetEventListener
{

    [Header("���� ����")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 9000;


    // LiteNetLib ��ü
    private NetManager ThisClient;
    protected NetPeer ServerPeer;
    protected NetworkDataWriter_PUN Writer;

    public int Id { get; set; } = -1;

    public bool ConnectOnStart = false;

    void  Start()
    {
        if(ConnectOnStart)
            ConnectToServer();
    }
    public void ConnectToServer()
    {

        Writer = new NetworkDataWriter_PUN();

        ThisClient = new NetManager(this)
        {
            AutoRecycle = true
        };
        ThisClient.Start();
        ServerPeer = ThisClient.Connect(serverIP, serverPort, "key");  // ������ ���� ��û
        Debug.Log($"���� ���� �õ�: {serverIP}:{serverPort}");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        OnReceivePacketFromServer(new NetworkDataReader(reader), channelNumber, deliveryMethod);
    }

    protected abstract void OnReceivePacketFromServer(NetworkDataReader reader, byte channelNumber, DeliveryMethod deliveryMethod);

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("���� �����: " + peer.Id);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("���� ���� ����");
    }
    void Update()
    {
        // ������ ��Ʈ��ũ �̺�Ʈ ó�� (PollEvents �ݵ�� �ֱ� ȣ��)
        ThisClient?.PollEvents();
    }
}
