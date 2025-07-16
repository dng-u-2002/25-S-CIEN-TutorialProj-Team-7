using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UIElements;


public class NetworkClientManager : MonoBehaviour//, INetEventListener
{


    //[SerializeField] string tmpUserName;
    //[SerializeField] string tmpPassword;
    //public static NetworkClientManager Instance { get; private set; }

    //[Header("���� ����")]
    //public string serverIP = "127.0.0.1";
    //public int serverPort_login = 9000;
    //public int serverPort_ingame = 9001;

    //[Header("������")]
    //public GameObject playerPrefab;  // ��Ʈ��ũ �÷��̾� ������

    //// LiteNetLib ��ü
    //private NetManager ThisClient;
    //private NetPeer ServerPeer;
    //private NetDataWriter Writer;

    //// �� Ŭ���̾�Ʈ ID (������ �Ҵ�)
    //private string Id;

    //// ���ӵ� ��� �÷��̾�(�� ���� ����)�� ��ü
    //private ConcurrentDictionary<string, GameObject> Players
    //    = new ConcurrentDictionary<string, GameObject>();

    //void Awake()
    //{
    //    if (Instance == null) Instance = this;
    //    else Destroy(gameObject);
    //    DontDestroyOnLoad(gameObject);
    //}

    //void Start()
    //{
    //    // (1) ������ ����ȭ �����
    //    Writer = new NetDataWriter();

    //    // (2) Ŭ���̾�Ʈ �Ŵ��� ���� �� ����
    //    ThisClient = new NetManager(this)
    //    {
    //        AutoRecycle = true
    //    };
    //    ThisClient.Start();
    //    ServerPeer = ThisClient.Connect(serverIP, serverPort_login, "MMO");  // ������ ���� ��û
    //    Debug.Log($"���� ���� �õ�: {serverIP}:{serverPort_login}");
    //}

    //void Update()
    //{
    //    // ������ ��Ʈ��ũ �̺�Ʈ ó�� (PollEvents �ݵ�� �ֱ� ȣ��)
    //    ThisClient.PollEvents();
    //}

    //void OnDestroy()
    //{
    //    ThisClient.Stop();
    //}

    //#region INetEventListener ����

    //public void OnPeerConnected(NetPeer peer)
    //{
    //    Debug.Log("���� �����: " + peer.Id);
    //}

    //public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    //{
    //    Debug.Log("���� ���� ����");
    //    // ��� ����
    //    foreach (var kv in Players)
    //        Destroy(kv.Value);
    //    Players.Clear();
    //}


    //public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    //public void OnConnectionRequest(ConnectionRequest request) { }

    //#endregion

    //#region ��Ŷ ���� �Լ�

    ///// <summary>
    ///// ���� ��ġ/ȸ�� ������ ������ ����
    ///// </summary>
    //public void SendMovement(Vector3 position, float rotY)
    //{
    //    if (ServerPeer == null || ServerPeer.ConnectionState != ConnectionState.Connected)
    //        return;

    //    //Writer.Reset();
    //    //Writer.Put((byte)PacketType.Handle_Movement);  // �޽��� Ÿ��
    //    //Writer.Put(position.x);
    //    //Writer.Put(position.y);
    //    //Writer.Put(position.z);
    //    //Writer.Put(rotY);
    //    //// ReliableSequenced: ���� ����, �ߺ� ��Ŷ ����
    //    //ServerPeer.Send(Writer, DeliveryMethod.ReliableSequenced);
    //}

    ///// <summary>
    ///// ���� ���(Ÿ�� ��ǥ)�� ������ ����
    ///// </summary>
    //public void SendAttack(Vector3 targetPos)
    //{
    //    if (ServerPeer == null || ServerPeer.ConnectionState != ConnectionState.Connected)
    //        return;

    //    //Writer.Reset();
    //    //Writer.Put((byte)PacketType.AttackRequest);
    //    //Writer.Put(targetPos.x);
    //    //Writer.Put(targetPos.y);
    //    //Writer.Put(targetPos.z);
    //    //// ReliableOrdered: ���� �߿�
    //    //ServerPeer.Send(Writer, DeliveryMethod.ReliableOrdered);
    //}

    //#endregion

    //#region ���� ��ε�ĳ��Ʈ ó��

    //void HandleStateBroadcast(NetPacketReader reader)
    //{
    //    int count = reader.GetInt();  // �÷��̾� ��
    //    for (int i = 0; i < count; i++)
    //    {
    //        string id = reader.GetString();
    //        if(id == Id) continue;  // �� �÷��̾�� ����
    //        float px = reader.GetFloat();
    //        float py = reader.GetFloat();
    //        float pz = reader.GetFloat();
    //        float ry = reader.GetFloat();

    //        // �÷��̾� ������Ʈ ������ ����, ������ ��ġ/ȸ�� ����
    //        var go = Players.GetOrAdd(id, _ => CreatePlayerObject(id, isLocal: false));
    //        go.transform.position = new Vector3(px, py, pz);
    //        go.transform.rotation = Quaternion.Euler(0, ry, 0);
    //    }
    //}

    //GameObject CreatePlayerObject(string id, bool isLocal)
    //{
    //    Debug.Log("A");
    //    var go = Instantiate(playerPrefab);
    //    go.name = isLocal ? "Player_Local" : $"Player_{id}";
    //    var v = go.GetComponent<PlayerCharacterCreator>();
    //    var chara= v.CreateCharacter(new CharacterData
    //    {
    //        Class = eCharacterClass.Eltanin,  // ���÷� Eltanin Ŭ���� ���
    //        Name = id
    //    });
    //    //go.GetComponent<PlayerInput>()?.Initialize(isLocal);
    //    return chara.gameObject;
    //}

    //public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    //{
    //}

    //public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    //{
    //    var msgType = (PacketType)reader.GetByte();
    //    switch (msgType)
    //    {
    //        case PacketType.HandShake:
    //            Id = reader.GetString();
    //            Debug.Log("�� Ŭ���̾�Ʈ ID: " + Id);
    //            break;
    //        case PacketType.InitializationResponse_InGame:
    //            // �� �÷��̾� ������Ʈ ����
    //            var p = CreatePlayerObject(Id, isLocal: true);
    //            Players.TryAdd(Id, p);
    //            break;

    //        case PacketType.Broadcast_InGame:
    //            HandleStateBroadcast(reader);
    //            break;


    //        case PacketType.LoginResponse:
    //            bool success = reader.GetBool();
    //            if (success)
    //            {
    //                Writer.Reset();
    //                Writer.Put((byte)PacketType.InitializationRequest_InGame);  // �޽��� Ÿ��
    //                Writer.Put(ComputeSHA256(tmpUserName + tmpPassword));
    //                // ReliableSequenced: ���� ����, �ߺ� ��Ŷ ����
    //                ServerPeer.Send(Writer, DeliveryMethod.ReliableSequenced);

    //            }
    //            else
    //            {
    //                string errorMsg = reader.GetString();
    //                Debug.LogError($"�α��� ����: {errorMsg}");
    //            }
    //            break;
    //    }
    //    reader.Recycle();
    //}

    //public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    //{
    //}

    //#endregion
}
