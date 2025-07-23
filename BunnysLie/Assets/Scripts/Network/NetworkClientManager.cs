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

    //[Header("서버 설정")]
    //public string serverIP = "127.0.0.1";
    //public int serverPort_login = 9000;
    //public int serverPort_ingame = 9001;

    //[Header("프리팹")]
    //public GameObject playerPrefab;  // 네트워크 플레이어 생성용

    //// LiteNetLib 객체
    //private NetManager ThisClient;
    //private NetPeer ServerPeer;
    //private NetDataWriter Writer;

    //// 내 클라이언트 ID (서버가 할당)
    //private string Id;

    //// 접속된 모든 플레이어(내 로컬 포함)의 객체
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
    //    // (1) 데이터 직렬화 도우미
    //    Writer = new NetDataWriter();

    //    // (2) 클라이언트 매니저 생성 및 시작
    //    ThisClient = new NetManager(this)
    //    {
    //        AutoRecycle = true
    //    };
    //    ThisClient.Start();
    //    ServerPeer = ThisClient.Connect(serverIP, serverPort_login, "MMO");  // 서버에 접속 요청
    //    Debug.Log($"서버 연결 시도: {serverIP}:{serverPort_login}");
    //}

    //void Update()
    //{
    //    // 들어오는 네트워크 이벤트 처리 (PollEvents 반드시 주기 호출)
    //    ThisClient.PollEvents();
    //}

    //void OnDestroy()
    //{
    //    ThisClient.Stop();
    //}

    //#region INetEventListener 구현

    //public void OnPeerConnected(NetPeer peer)
    //{
    //    Debug.Log("서버 연결됨: " + peer.Id);
    //}

    //public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    //{
    //    Debug.Log("서버 연결 해제");
    //    // 모두 삭제
    //    foreach (var kv in Players)
    //        Destroy(kv.Value);
    //    Players.Clear();
    //}


    //public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    //public void OnConnectionRequest(ConnectionRequest request) { }

    //#endregion

    //#region 패킷 전송 함수

    ///// <summary>
    ///// 로컬 위치/회전 정보를 서버로 전송
    ///// </summary>
    //public void SendMovement(Vector3 position, float rotY)
    //{
    //    if (ServerPeer == null || ServerPeer.ConnectionState != ConnectionState.Connected)
    //        return;

    //    //Writer.Reset();
    //    //Writer.Put((byte)PacketType.Handle_Movement);  // 메시지 타입
    //    //Writer.Put(position.x);
    //    //Writer.Put(position.y);
    //    //Writer.Put(position.z);
    //    //Writer.Put(rotY);
    //    //// ReliableSequenced: 순서 보장, 중복 패킷 제거
    //    //ServerPeer.Send(Writer, DeliveryMethod.ReliableSequenced);
    //}

    ///// <summary>
    ///// 공격 명령(타겟 좌표)을 서버로 전송
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
    //    //// ReliableOrdered: 순서 중요
    //    //ServerPeer.Send(Writer, DeliveryMethod.ReliableOrdered);
    //}

    //#endregion

    //#region 상태 브로드캐스트 처리

    //void HandleStateBroadcast(NetPacketReader reader)
    //{
    //    int count = reader.GetInt();  // 플레이어 수
    //    for (int i = 0; i < count; i++)
    //    {
    //        string id = reader.GetString();
    //        if(id == Id) continue;  // 내 플레이어는 무시
    //        float px = reader.GetFloat();
    //        float py = reader.GetFloat();
    //        float pz = reader.GetFloat();
    //        float ry = reader.GetFloat();

    //        // 플레이어 오브젝트 없으면 생성, 있으면 위치/회전 갱신
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
    //        Class = eCharacterClass.Eltanin,  // 예시로 Eltanin 클래스 사용
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
    //            Debug.Log("내 클라이언트 ID: " + Id);
    //            break;
    //        case PacketType.InitializationResponse_InGame:
    //            // 내 플레이어 오브젝트 생성
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
    //                Writer.Put((byte)PacketType.InitializationRequest_InGame);  // 메시지 타입
    //                Writer.Put(ComputeSHA256(tmpUserName + tmpPassword));
    //                // ReliableSequenced: 순서 보장, 중복 패킷 제거
    //                ServerPeer.Send(Writer, DeliveryMethod.ReliableSequenced);

    //            }
    //            else
    //            {
    //                string errorMsg = reader.GetString();
    //                Debug.LogError($"로그인 실패: {errorMsg}");
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
