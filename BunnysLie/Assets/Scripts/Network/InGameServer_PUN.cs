using ExitGames.Client.Photon;
using Helpers;
using LiteNetLib.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using VOYAGER_Server;
using static InGameServer_PUN;
using static UnityEngine.Rendering.DebugUI;



public class InGameServer_PUN : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public enum eRoomState
    {
        None,
        ShouldDistributeCards,
        WaitingForSuccessfulCardReception,
        ShouldPlayersSelectCard2Delete,
        WaitingForPlayersToSelectCard2Delete,
        WaitingForPlayersToSelectCard2RemoveISR,
        ShouldStartRPS,
        WaitingForRPSSelection
    }
    public class User
    {
        public Photon.Realtime.Player Player;
        public int Id => Player.ActorNumber; //�ϴ���

        public User(Photon.Realtime.Player player)
        {
            Player = player;
        }
    }
    public class Room
    {
        public List<User> Players;
        public eRoomState State = eRoomState.None;
        public eGameMode Mode = eGameMode.TwoCards; //2�� ��� / 3�� ���

        public Dictionary<int, byte> OutCounts = new Dictionary<int, byte>(); //�ƿ�ī��Ʈ(���� ���)
        /// <summary>
        /// ���� �������� ī��. �Ϲ� ���� / ����� �� �����ϰ� ����.
        /// </summary>
        public Dictionary<int, List<Tuple<byte, byte>>> Cards = new Dictionary<int, List<Tuple<byte, byte>>>();
        public Dictionary<int, Tuple<byte, byte>> Cards2Delete = new Dictionary<int, Tuple<byte, byte>>();

        public int PlayerCounter_SelectedCard2Delete = 0;
        public int PlayerCounter_SuccessfullyReceivedCard = 0; //���������� ī�带 ���� �÷��̾� ��. 3���� ��� ī�带 ������ RPS ����
        public int? RPSFirst; //���������� 1�� �÷��̾� ID
        public int? RPSSecond; //���������� 2�� �÷��̾� ID
        public int? RPSThird; //���������� 3�� �÷��̾� ID
        public byte NowRPSRoundCounter = 0; //���� ���������� ����
        public byte PlayerCounter_SuccessfullyReceivedOrders; // ���������� ������ ���������� ���� �÷��̾� ��. 3���� ��� ������ In/Out ���� ����

        public List<int> RPSTargetPlayers = new List<int>(); // ���� ���������� ���� �÷��̾� ID ���
        public Dictionary<int, eRPS> RPSSelections = new Dictionary<int, eRPS>(); // ���������� ���� ���. Key: �÷��̾� ID, Value: ������ ���������� ���

        public List<int> NowSpecialRulePlayers = new List<int>(); // ���� ����� �꿡 ���� ���� �÷��̾� ID ���
        public Dictionary<int, Tuple<byte, byte>> Cards2ExchangeInSpecialRule = new Dictionary<int, Tuple<byte, byte>>(); // ����� �꿡�� ��ȯ�� ī��. Key: �÷��̾� ID, Value: (ī�� Ÿ��, ī�� ��)
        public byte PlayerCountWhoAcceptedExchangeInSpecialRule = 0; // ����� �꿡�� ī�� ��ȯ�� ������ �÷��̾� ��

        public int GoPlayerCountInSpecialRule = 0; // ����� �꿡�� Go�� ������ �÷��̾� ��. 2���� �Ǹ� ����� �� ����

        public List<int> InPlayers = new List<int>(); // In �÷��̾� ID ���
        public List<int> OutPlayers = new List<int>(); // Out �÷��̾� ID ���
    }

    Room ThisRoomData;

    NetworkDataWriter_PUN PacketWriter;
    void Start()
    {
        PacketWriter = new NetworkDataWriter_PUN();
    }
    void SendPacket(User u)
    {
        PacketWriter.SendPacket(u);
        PacketWriter.Clear();
    }

    [SerializeField] TMP_Text Debugger;
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        if (Photon.Pun.PhotonNetwork.IsMasterClient == false)
        {
            Debug.LogWarning("[Warning] Only Master Client can handle events.");
            return;
        }
        Debugger.text += $"[User Connection] New User connected: {newPlayer.ActorNumber}\n";
        Debug.Log($"[User Connection] New User connected: {newPlayer.ActorNumber}");

        PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.HandShake_S2U);
        PacketWriter.WriteInt(newPlayer.ActorNumber);
        SendPacket(new User(newPlayer));
        Debug.Log($"[HandShake] Server sent Handshake to User {newPlayer.ActorNumber}");

        if (Photon.Pun.PhotonNetwork.IsMasterClient == false)
            return;
        Debug.Log($"Now Player Count : {PhotonNetwork.CurrentRoom.PlayerCount}");
        Debugger.text += $"[User Connection] Now Player Count : {PhotonNetwork.CurrentRoom.PlayerCount}\n";
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            var room = new Room();
            room.Players = new List<User>();
            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                room.Players.Add(new User(p.Value));
                if (room.Players.Count >= 3)
                {
                    break;
                }
            }
            room.Mode = InGameManager.Instance.Mode;
            //Rooms.Add(room.GetHashCode(), room);
            ThisRoomData = room;
            Rooms.Add(room.GetHashCode(), ThisRoomData);

            List<int> userIDs = new List<int>();
            foreach (var user in room.Players)
            {
                userIDs.Add(user.Id);
                room.OutCounts[user.Id] = 3;
            }
            byte character = 0;
            foreach (var user in room.Players)
            {
                //Photon.Pun.PhotonNetwork.RaiseEvent((byte)ePacketType_InGameServer.Broadcast_StartGame,)
                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_StartGame);
                PacketWriter.WriteInt(room.GetHashCode());
                for (int i = 0; i < userIDs.Count; i++)
                {
                    PacketWriter.WriteInt(userIDs[i]);
                }
                character = 0;
                for (int i = 0; i < userIDs.Count; i++)
                {
                    PacketWriter.WriteByte(character);
                    character++;
                }
                
                SendPacket(user);
            }
            Debug.Log($"[Game Start] A new 2-player game has started with Users: {string.Join(", ", room.Players.Select(u => u.Id))}.");


            room.State = eRoomState.ShouldDistributeCards;
            if (room.Mode == eGameMode.TwoCards)
                DistributeRandom2CardsForUsers(room);
            if (room.Mode == eGameMode.ThreeCards)
                DistributeRandom3CardsForUsers(room);
            room.State = eRoomState.WaitingForSuccessfulCardReception;
        }
    }


    static System.Random Random = new System.Random();
    public (byte, byte) GetRandomCard(List<Tuple<byte, byte>> excection = null)
    {
        List<Tuple<byte, byte>> cards = new List<Tuple<byte, byte>>();
        foreach(var kv in ThisRoomData.Cards)
        {
            foreach(var c in kv.Value)
                cards.Add(new Tuple<byte, byte>(c.Item1, c.Item2));
        }
        foreach (var kv in ThisRoomData.Cards2Delete)
        {
            cards.Add(new Tuple<byte, byte>(kv.Value.Item1, kv.Value.Item2));
        }
        foreach(var kv in ThisRoomData.Cards2ExchangeInSpecialRule)
        {
            cards.Add(new Tuple<byte, byte>(kv.Value.Item1, kv.Value.Item2));
        }
        if(excection != null)
        cards.AddRange(excection);
        int type = 0;
        int value = 0;
        while(true)
        {
            type = Random.Next(0, 2); // 0 or 1
            value = Random.Next(0, 10); // 0 to 9

            if (cards.Contains(new Tuple<byte, byte>((byte)type, (byte)value)) == false)
            {
                // ī�尡 �ߺ����� ������ ��ȯ
                break;
            }
        }
        return ((byte)type, (byte)value);
    }
    //public enum ePacketType_InGameServer
    //{
    //    None = 0,
    //    Error = 1,
    //    HandShake_S2U,
    //    HandShake_U2S,
    //    Chat_Send,
    //    Chat_Receive,

    //    U2SRequest_JoinGame,
    //    S2UResponse_JoinGame,


    //    Broadcast_StartGame,


    //    DistributeCardsFromServer, //�������� ���� �����鿡�� ī�� �й�
    //    U2SResponse_SuccessfullyReceivedCards, //

    //    S2URequest_SelectCard2Delete,
    //    U2SResponse_SelectCard2Delete, //������ ������ ī�带 �������� �� ������ �����ϴ� ��Ŷ
    //    Broadcast_SomeoneSelectedCard2Delete, //������ ������ ī�带 �������� �� �ٸ� �����鿡�� �����ϴ� ��Ŷ
    //    Broadcast_ShowCards2Delete, //������ ������ ī�带 �������� �� �ٸ� �����鿡�� �����ϴ� ��Ŷ(������ ī�� �����ֱ�)

    //    S2URequest_RPSSelection, //���������� ���� ��û
    //    U2SResponse_RPSSelection, //���������� ���� ����(������ ������ ���������� ����� ������ ����)

    //    //Broadcast_ShowRPSRoundResult_Selection,
    //    //Broadcast_ShowRPSRoundResult_Order,
    //    //�� 2���� Broadcast_RPSRoundResult�� ����. �������� Ŭ���̾�Ʈ���� ó��
    //    Broadcast_RPSRoundResult,

    //    //Ready2RPSRematch,
    //    //�� 1���� Broadcast_RPSStartRematch�� ����.
    //    Broadcast_RPSStartRematch,

    //    Broadcast_RPSFinalResult,
    //    U2SResponse_SuccessfullyReceivedOrders,




    //    S2URequest_SelectInOut_First,
    //    U2SResponse_SelectInOut_First,

    //    S2URequest_SelectInOut_Second,
    //    U2SResponse_SelectInOut_Second,
    //    S2URequest_SelectInOut_Third,
    //    U2SResponse_SelectInOut_Third,

    //    Broadcast_InOutRoundResult,
    //    Broadcast_InOutFinalResult,

    //    Broadcast_SendAllPlayersCardData,
    //    Broadcast_ShowAllCards,

    //    Broadcast_LoserOfThisRound,

    //    Broadcast_StartSpecialRule,
    //    S2URequest_SelectCard2DeleteISR,
    //    U2SResponse_SelectCard2DeleteISR, //������ ����� �꿡�� ������ ī�带 �������� �� ������ �����ϴ� ��Ŷ
    //    Broadcast_SomeoneSelectedCard2RemoveISR, //������ ����� �꿡�� ������ ī�带 �������� �� �ٸ� �����鿡�� �����ϴ� ��Ŷ
    //    Broadcast_ShowCards2DeleteISR, //������ ����� �꿡�� ������ ī�带 �������� �� �ٸ� �����鿡�� �����ϴ� ��Ŷ(������ ī�� �����ֱ�)
    //    GoInSpecialRule,
    //    Broadcast_ShowSpecialRuleCards,
    //    Broadcast_LoserOfSpecialRule,

    //    Broadcast_StartNextRound,
    //    U2SRequest_ExchangeCardWithDeckInSpecialRule,
    //    S2UResponse_ExhangeCardWithDeckInSpecialRule,

    //    U2SRequest_ExhangeCardWithOpponentInSpecialRule,

    //    S2UAsk_ExhangeCardWithOpponentInSpecialRule, // ���� ī�� ��ȯ ��û
    //    S2UResponse_WillAcceptExhangeCardWithOpponentISR, // ��밡 ī�� ��ȯ ��û�� ����
    //    Broadcast_IsOpponentAcceptedCardExchangeISR,

    //    U2SRequest_OpponentSelectedCardToExhangeISR, //��밡 ī�带 �����
    //    Broadcast_ExhangeWithOpponentInSpecialRuleResult, //��ȯ ���
    //    U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule,

    //    Broadcast_FinalResult

    //}

    Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

    List<User> WaitingUsers_TwoMode = new List<User>();
    List<User> WaitingUsers_ThreeMode = new List<User>();

    private eRPS? GetWinningRPSChoice(IEnumerable<eRPS> choices)
    {
        var distinct = choices.Distinct().ToList();
        // ��� ���� ����̰ų� ���������������� ��� �������� ���º�
        if (distinct.Count == 1 || distinct.Count == 3)
            return null;
        // �� ���� ��縸 ������ ��, �̱�� ���� ��ȯ
        if (distinct.Contains(eRPS.Rock) && distinct.Contains(eRPS.Scissors)) return eRPS.Rock;
        if (distinct.Contains(eRPS.Scissors) && distinct.Contains(eRPS.Paper)) return eRPS.Scissors;
        if (distinct.Contains(eRPS.Paper) && distinct.Contains(eRPS.Rock)) return eRPS.Paper;
        return null;
    }

    void SendRPSRoundResult(Room room)
    {
        foreach (var p in room.Players)
        {
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_RPSRoundResult);
            //���� ���� : byte
            //id : int
            //byte : selection : default is 4
            //byte : order : default is 3
            PacketWriter.WriteByte(room.NowRPSRoundCounter);
            foreach (var rp in room.RPSTargetPlayers)
            {
                PacketWriter.WriteInt(rp);
                if (room.RPSSelections.TryGetValue(rp, out var selection))
                    PacketWriter.WriteByte((byte)(selection));
                else
                    PacketWriter.WriteByte(4);

                if (room.RPSFirst == rp)
                    PacketWriter.WriteByte(2); // 1��
                else if (room.RPSSecond == rp)
                    PacketWriter.WriteByte(1); // 2��
                else if (room.RPSThird == rp)
                    PacketWriter.WriteByte(0); // 3��
                else
                    PacketWriter.WriteByte(3); // ���� ���� �ȵ�
            }
            if (room.RPSTargetPlayers.Count == 2)
            {
                PacketWriter.WriteInt(-1);
                PacketWriter.WriteByte(4);
                PacketWriter.WriteByte(4); //�׳� �ƹ� ��
            }
            SendPacket(p);
        }
    }

    void CheckRPSRoundResult(Room room, System.Action<List<int>> onPlayersShouldRematch)
    {
        // ���� ���� ������ �� ������
        const float packetDelay_SendRematchAfterDraw = 1.0f;
        System.Action<List<int>> requstRematch = null; // ���� ��û�� ���� �ݹ�
        List<int> playersShouldRematch = new List<int>();
        if (room.RPSSelections.Count == room.RPSTargetPlayers.Count)
        {
            room.NowRPSRoundCounter += 1;
            var winChoice = GetWinningRPSChoice(room.RPSSelections.Values);
            if (winChoice == null)
            {
                //���º� ���� ��󿡰� �ٽ� ��û

                //���� ��� ���
                Debug.Log($"[RPS Round {room.NowRPSRoundCounter}] Draw. Players should rematch: {string.Join(", ", room.RPSTargetPlayers)}");
                //print rps selections
                playersShouldRematch = new List<int>(room.RPSTargetPlayers); //vaule type�� deep copy�� new List<T>�� �ص� ��
                requstRematch = onPlayersShouldRematch;

                SendRPSRoundResult(room);

                //send packet after 1sec
                //Task.Delay(Delay_SendRematchAfterDraw).ContinueWith(_ =>
                DelayedFunctionHelper.InvokeDelayed(()=>
                {
                    requstRematch?.Invoke(playersShouldRematch);
                }, packetDelay_SendRematchAfterDraw);
            }
            else
            {
                var winners = room.RPSTargetPlayers
                                  .Where(id => room.RPSSelections[id] == winChoice)
                                  .ToList();
                var losers = room.RPSTargetPlayers.Except(winners).ToList();

                if (room.RPSTargetPlayers.Count == 3)
                {
                    if (winners.Count == 1) //���� 1��, ���� 2��
                    {
                        int winnerID = winners[0];
                        room.RPSFirst = winnerID; // �ܵ� ����

                        playersShouldRematch = new List<int>(losers);
                        requstRematch = onPlayersShouldRematch;
                    }
                    else if (winners.Count == 2) //���� 2��, ���� 1��
                    {
                        int loserID = losers[0];
                        room.RPSThird = loserID; // �ܵ� ����

                        playersShouldRematch = new List<int>(winners);
                        requstRematch = onPlayersShouldRematch;
                    }
                }
                else if (room.RPSTargetPlayers.Count == 2) //����
                {
                    //if(winners.Count == 1) // ���� 1��, ���� 1��. �ݵ�� �� ���ۿ� ����.
                    //{
                    int winnerID = winners[0];
                    int loserID = losers[0];

                    if (room.RPSFirst.HasValue == true) //1���� ������ �����̿��� ��� 
                    {
                        room.RPSSecond = winnerID;
                        room.RPSThird = loserID;
                    }
                    else //3���� ������ �����̿��� ���
                    {
                        room.RPSFirst = winnerID;
                        room.RPSSecond = loserID;
                    }
                    //}
                }


                #region
                //���������� �������� ���� �ְ�
                //foreach (var p in room.Players)
                //{
                //    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowRPSRoundResult_Selection);
                //    PacketWriter.WriteByte(room.NowRPSRoundCounter);
                //    foreach(var rp in room.RPSTargetPlayers)
                //    {
                //        PacketWriter.WriteInt(rp);
                //        if (room.RPSSelections.TryGetValue(rp, out var selection))
                //            PacketWriter.WriteByte((byte)(selection));
                //        else
                //            PacketWriter.WriteByte(4);
                //    }
                //    if (room.RPSTargetPlayers.Count == 2)
                //    {
                //        PacketWriter.WriteInt(-1);
                //        PacketWriter.WriteByte(4);
                //    }
                //    SendPacket(p);
                //}
                ////1�� �ִٰ� ������ ������
                //Task.Delay(Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                //{
                //    foreach (var p in room.Players)
                //    {
                //        PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowRPSRoundResult_Order);

                //        PacketWriter.WriteByte(room.NowRPSRoundCounter);
                //        // ������� ID ����Ʈ ����
                //        PacketWriter.WriteInt((int)(room.RPSFirst.HasValue ? room.RPSFirst : -1));  // 1��
                //        PacketWriter.WriteInt((int)(room.RPSSecond.HasValue ? room.RPSSecond : -1));  // 2��
                //        PacketWriter.WriteInt((int)(room.RPSThird.HasValue ? room.RPSThird : -1));  // 3��

                //        SendPacket(p);
                //    }
                //});
                #endregion
                //���� 2�� ����, �����̴� Ŭ���̾�Ʈ���� ó��
                SendRPSRoundResult(room);

                if ((room.RPSFirst.HasValue && room.RPSSecond.HasValue && room.RPSThird.HasValue) == false)
                {
                    //send packet after 1sec
                    //Task.Delay(Delay_SendRematchAfterDraw).ContinueWith(_ =>
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        requstRematch?.Invoke(playersShouldRematch);
                    }, packetDelay_SendRematchAfterDraw);
                }
                else
                {
                    const float delay_ShowFinalOrderAfterRPS = 2.0f;
                    //Task.Delay(Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        foreach (var p in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_RPSFinalResult);

                            PacketWriter.WriteByte(room.NowRPSRoundCounter);
                            // ������� ID ����Ʈ ����
                            PacketWriter.WriteInt((int)(room.RPSFirst));  // 1��
                            PacketWriter.WriteInt((int)(room.RPSSecond));  // 2��
                            PacketWriter.WriteInt((int)(room.RPSThird));  // 3��

                            SendPacket(p);
                        }
                        room.RPSSelections.Clear();
                        room.RPSTargetPlayers.Clear();
                        room.NowRPSRoundCounter = 0;
                    }, delay_ShowFinalOrderAfterRPS);
                }
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    //protected override void OnReceivePacketFromUser(User user, NetworkDataReader reader, byte channelNumber, ePacketSafefyLevel safetyLevel)
    {
        if(Photon.Pun.PhotonNetwork.IsMasterClient == false)
        {
            Debug.LogWarning("[Warning] Only Master Client can handle events.");
            return;
        }

        if(ThisRoomData == null)
        {
            Debug.LogWarning("[Warning] ThisRoomData is null. Cannot process event.");
            return;
        }


        NetworkDataReader_PUN reader = new NetworkDataReader_PUN(photonEvent.CustomData);

        User user = ThisRoomData.Players.Find((u) => photonEvent.Sender == u.Id);
        var pac = photonEvent.Code;
        var packet = (ePacketType_InGameServer)pac;
        Debug.Log($"[Packet Received] User {user.Id} sent a packet {packet}.");


        Debugger.text += $"[Packet Received] User {user.Id} sent a packet {packet}.\n";

        switch (packet)
        {
            case ePacketType_InGameServer.HandShake_U2S:
                Debug.Log($"[HandShake] User {user.Id} sent Handshake to Server.");
                break;
            case ePacketType_InGameServer.Chat_Send:
                int sender = reader.ReadInt();
                string message = reader.ReadString();
                //foreach (var u in ConnectedUsers.Values)
                //{
                //    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Chat_Receive);
                //    PacketWriter.WriteInt(sender);
                //    PacketWriter.WriteString(message);
                //    SendPacket(u.Item1);
                //}
                break;
            case ePacketType_InGameServer.SelfEmoticon:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        byte idx = reader.ReadByte();


                        foreach(var pl in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_Emoticon);
                            PacketWriter.WriteInt(id); // ���� ��� ID
                            PacketWriter.WriteByte(idx); // �̸�Ƽ�� �ε���
                            SendPacket(pl);
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.Request_JoinGame:
                {
                    int id = reader.ReadInt();
                    byte mode = reader.ReadByte();
                    if (mode == 2)
                    {
                        WaitingUsers_TwoMode.Add(user);
                        Debug.Log($"[JoinGame] User {user.Id} requested to join a 2-player game.");
                    }
                    else if (mode == 3)
                    {
                        WaitingUsers_ThreeMode.Add(user);
                        Debug.Log($"[JoinGame] User {user.Id} requested to join a 3-player game.");
                    }
                    else
                    {
                        Debug.Log($"[JoinGame] User {user.Id} requested to join a game with unknown mode {mode}.");
                    }
                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Response_JoinGame);
                    SendPacket(user);
                }
                break;
            case ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedCards:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.State != eRoomState.WaitingForSuccessfulCardReception)
                        {
                            //�̰� ������
                            Debug.Log($"[Warning] User {user.Id} tried to receive cards in an invalid state: {room.State}.");
                            return;
                        }
                        room.PlayerCounter_SuccessfullyReceivedCard += 1;
                        Debug.Log(message: $"[Card Reception] User {user.Id} successfully received cards in Room {roomID}. Total: {room.PlayerCounter_SuccessfullyReceivedCard}/3");

                        if (room.PlayerCounter_SuccessfullyReceivedCard == 3 && room.Mode == eGameMode.TwoCards)
                        {
                            const float packetDelayTime_FromCardReception_ToRPSStart = 1.0f;
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                room.State = eRoomState.ShouldStartRPS;
                                StartRPS(room);
                                room.PlayerCounter_SuccessfullyReceivedCard = -1;
                                room.State = eRoomState.WaitingForRPSSelection;
                            }, packetDelayTime_FromCardReception_ToRPSStart);
                        }
                        else if (room.PlayerCounter_SuccessfullyReceivedCard == 3 && room.Mode == eGameMode.ThreeCards)
                        {
                            room.State = eRoomState.ShouldPlayersSelectCard2Delete;
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_SelectCard2Delete);
                                SendPacket(pl);
                            }
                            //StartRPS(room);
                            room.PlayerCounter_SuccessfullyReceivedCard = -1;
                            room.State = eRoomState.WaitingForPlayersToSelectCard2Delete;
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.U2SResponse_SelectCard2Delete:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.State != eRoomState.WaitingForPlayersToSelectCard2Delete)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select card to delete in an invalid state: {room.State}.");
                            return;
                        }
                        byte cardType = reader.ReadByte(); // ī�� Ÿ��
                        byte cardValue = reader.ReadByte(); // ī�� ��
                        if (room.Cards[id].Contains(new Tuple<byte, byte>(cardType, cardValue)) == false)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select card that does not exist in their hand: {cardType}:{cardValue}.");
                            return;
                        }
                        else
                        {
                            room.Cards[id].Remove(new Tuple<byte, byte>(cardType, cardValue));
                            room.Cards2Delete[id] = new Tuple<byte, byte>(cardType, cardValue); // ������ ī�� ����
                            room.PlayerCounter_SelectedCard2Delete += 1;
                            Debug.Log($"[Card Selection] User {user.Id} selected card {cardType}:{cardValue} to delete in Room {roomID}.");

                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2Delete);
                                PacketWriter.WriteInt(id); // ������ �÷��̾� ID
                                SendPacket(pl);
                            }

                            if (room.PlayerCounter_SelectedCard2Delete == 3)
                            {
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowCards2Delete);
                                    foreach (var op in room.Cards2Delete)
                                    {
                                        PacketWriter.WriteInt(op.Key); // ������ �÷��̾� ID
                                        var card2Delete = op.Value;
                                        PacketWriter.WriteByte(card2Delete.Item1); // ī�� Ÿ��
                                        PacketWriter.WriteByte(card2Delete.Item2); // ī�� ��
                                    }
                                    SendPacket(pl);
                                }

                                room.Cards2Delete.Clear();
                                //ī�尡 2���� �ǵ��� ����
                                foreach(var pc in room.Cards)
                                {
                                    if (pc.Value.Count == 3)
                                        pc.Value.RemoveAt(2);
                                }

                                const float packetDelayTime_FromShowingCards2Delete_ToRPSStart = 1.0f;
                                DelayedFunctionHelper.InvokeDelayed(()=>
                                {
                                    room.State = eRoomState.ShouldStartRPS;
                                    StartRPS(room);
                                    room.PlayerCounter_SelectedCard2Delete = -1;
                                    room.State = eRoomState.WaitingForRPSSelection;
                                }, packetDelayTime_FromShowingCards2Delete_ToRPSStart);
                            }
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.U2SResponse_RPSSelection:
                {
                    int userId = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    var room = Rooms[roomID];
                    if (room.State != eRoomState.WaitingForRPSSelection ||
                        !room.RPSTargetPlayers.Contains(userId))
                        return;

                    room.RPSSelections[userId] = (eRPS)reader.ReadByte();
                    Debug.Log($"[RPS Selection] User {user.Id} selected {room.RPSSelections[userId]} in Room {roomID}.");

                    CheckRPSRoundResult(room,
                        (players2Rematch) =>
                        {
                            room.RPSTargetPlayers = players2Rematch;
                            room.RPSSelections.Clear();
                            room.State = eRoomState.WaitingForRPSSelection;
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_RPSStartRematch);
                                PacketWriter.WriteByte((byte)players2Rematch.Count);
                                foreach (var player2rematch in players2Rematch)
                                {
                                    PacketWriter.WriteInt(player2rematch);
                                }
                                SendPacket(pl);
                            }
                        });
                }
                break;
            case ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedOrders:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    byte order = reader.ReadByte();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.RPSFirst == id && order == 2)
                            room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                        else if (room.RPSSecond == id && order == 1)
                            room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                        else if (room.RPSThird == id && order == 0)
                            room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                        else
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to receive orders in an invalid state: {room.State}.");
                            return;
                        }

                        if (room.PlayerCounter_SuccessfullyReceivedOrders == 3)
                        {
                            var fp = room.Players.Find((u) => room.RPSThird == u.Id);
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_SelectInOut_First);
                            SendPacket(fp);
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.U2SResponse_SelectInOut_First:
            case ePacketType_InGameServer.U2SResponse_SelectInOut_Second:
            case ePacketType_InGameServer.U2SResponse_SelectInOut_Third:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();

                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        byte order = reader.ReadByte();
                        if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_First && order != 0)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                            return;
                        }
                        if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_Second && order != 1)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                            return;
                        }
                        if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_Third && order != 2)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                            return;
                        }
                        byte selection = reader.ReadByte(); // 0: In, 1: Out
                        if (selection == 0) // In
                        {
                            room.InPlayers.Add(id);
                            Debug.Log($"[In/Out Selection] User {user.Id} selected In in Room {roomID}.");
                        }
                        else if (selection == 1) // Out
                        {
                            room.OutPlayers.Add(id);
                            Debug.Log($"[In/Out Selection] User {user.Id} selected Out in Room {roomID}.");
                        }
                        else
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select an invalid in/out option: {selection}.");
                            return;
                        }

                        SendIOResultAllPlayers(room, false);

                        if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_First)
                        {
                            var fp = room.Players.Find((u) => room.RPSSecond == u.Id);
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_SelectInOut_Second);
                            SendPacket(fp);
                        }
                        else if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_Second)
                        {
                            var fp = room.Players.Find((u) => room.RPSFirst == u.Id);
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_SelectInOut_Third);
                            SendPacket(fp);
                        }
                        else if (packet == ePacketType_InGameServer.U2SResponse_SelectInOut_Third)
                        {
                            // �� ��° ���� �� ���� ��� ����
                            SendIOResultAllPlayers(room, true);

                            const float packetDelay_FromAllPlayerSelectedIOToSendAllPlayersCardData = 1.0f;
                            Debug.Log($"[In/Out Selection] Room {roomID} final result sent. In: {string.Join(", ", room.InPlayers)}, Out: {string.Join(", ", room.OutPlayers)}.");
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_SendAllPlayersCardData);
                                    foreach (var cards in room.Cards)
                                    {
                                        PacketWriter.WriteInt(cards.Key);
                                        foreach (var c in cards.Value)
                                        {
                                            PacketWriter.WriteByte(c.Item1); // ī�� Ÿ��
                                            PacketWriter.WriteByte(c.Item2); // ī�� ��
                                        }
                                    }
                                    SendPacket(pl);
                                }

                                const float packetDelay_FromSendAllPlayersCardDataToShowAllCards = 1.0f;
                                DelayedFunctionHelper.InvokeDelayed(() =>
                                {
                                    foreach (var pl in room.Players)
                                    {
                                        PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowAllCards);
                                        SendPacket(pl);
                                    }
                                }, packetDelay_FromSendAllPlayersCardDataToShowAllCards);
                            }, packetDelay_FromAllPlayerSelectedIOToSendAllPlayersCardData);

                            //Task.Delay(3500).ContinueWith((_) =>
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                if (CalculateResult(roomID) == true)
                                {
                                    //Task.Delay(3500).ContinueWith((_) =>
                                    //DelayedFunctionHelper.InvokeDelayed(() =>
                                    //{
                                    //    StartNextRound(room);
                                    //}, 3.5f);
                                }
                            }, 3.5f);
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.GoInSpecialRule:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        room.GoPlayerCountInSpecialRule += 1;

                        if (room.GoPlayerCountInSpecialRule == 2)
                        {
                            Debug.Log("[Special Rule] Show special rule cards to players.");
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowSpecialRuleCards);
                                foreach (var sp in room.NowSpecialRulePlayers)
                                {
                                    PacketWriter.WriteInt(sp); // �÷��̾� ID
                                    foreach (var c in room.Cards[sp])
                                    {
                                        PacketWriter.WriteByte(c.Item1); // ī�� Ÿ��
                                        PacketWriter.WriteByte(c.Item2); // ī�� ��
                                    }
                                }
                                SendPacket(pl);
                            }

                            //Task.Delay(2000).ContinueWith((_) =>
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                int loser = CalculateSpecialRuleLoser(room);

                                if (loser == -1) //����
                                {
                                    SendStartSpecialRule(room, room.NowSpecialRulePlayers[0], room.NowSpecialRulePlayers[1], reason: 10); //������ �߻�
                                    return;
                                }

                                if (loser != -1)
                                    room.OutCounts[loser] -= 1;
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_LoserOfSpecialRule);
                                    PacketWriter.WriteInt(loser);
                                    if (loser == -1)
                                        PacketWriter.WriteByte(0); // ���º�
                                    else
                                        PacketWriter.WriteByte(room.OutCounts[loser]);
                                    SendPacket(pl);
                                }

                                //Task.Delay(3000).ContinueWith((_) =>
                                DelayedFunctionHelper.InvokeDelayed(() =>
                                {
                                    StartNextRound(room);
                                }, 3.0f);
                            }, 2.0f);
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.U2SResponse_SelectCard2DeleteISR:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.State != eRoomState.WaitingForPlayersToSelectCard2RemoveISR)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select card to remove in an invalid state: {room.State}.");
                            return;
                        }
                        byte cardType = reader.ReadByte(); // ī�� Ÿ��
                        byte cardValue = reader.ReadByte(); // ī�� ��
                        if (room.Cards[id].Contains(new Tuple<byte, byte>(cardType, cardValue)) == false)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to select card that does not exist in their hand: {cardType}:{cardValue}.");
                            return;
                        }
                        else
                        {
                            room.Cards[id].Remove(new Tuple<byte, byte>(cardType, cardValue));
                            room.Cards2Delete[id] = new Tuple<byte, byte>(cardType, cardValue); // ������ ī�� ����
                            room.PlayerCounter_SelectedCard2Delete += 1;
                            Debug.Log($"[Card Selection] User {user.Id} selected card {cardType}:{cardValue} to remove in Room {roomID}.");
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2RemoveISR);
                                PacketWriter.WriteInt(id); // ������ �÷��̾� ID
                                SendPacket(pl);
                            }
                            if (room.PlayerCounter_SelectedCard2Delete == 2)
                            {
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ShowCards2DeleteISR);
                                    foreach (var op in room.Cards2Delete)
                                    {
                                        PacketWriter.WriteInt(op.Key); // ������ �÷��̾� ID
                                        var card2Remove = op.Value;
                                        PacketWriter.WriteByte(card2Remove.Item1); // ī�� Ÿ��
                                        PacketWriter.WriteByte(card2Remove.Item2); // ī�� ��
                                    }
                                    SendPacket(pl);
                                }
                                room.Cards2Delete.Clear();
                            }
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.U2SRequest_ExchangeCardWithDeckInSpecialRule:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.NowSpecialRulePlayers.Contains(id) == false)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to exchange card in an invalid state: {room.State}.");
                            return;
                        }

                        byte cardType = reader.ReadByte(); // ī�� Ÿ��
                        byte cardValue = reader.ReadByte(); // ī�� ��

                        if (room.Cards[id].Contains(new Tuple<byte, byte>(cardType, cardValue)) == false)
                        {
                            Debug.LogError($"[Warning] User {user.Id} tried to exchange card that does not exist in their hand: {cardType}:{cardValue}.");
                            return;
                        }
                        else
                        {
                            room.Cards[id].Remove(new Tuple<byte, byte>(cardType, cardValue)); //ī�� ����
                            Debug.Log($"[Special Rule] User {user.Id} exchanged card with deck. Removed card: {cardType}:{cardValue}.");
                            var newCard = GetRandomCard(new List<Tuple<byte, byte>>(new[] { new Tuple<byte, byte>(cardType, cardValue) })); // ī�� 1�� ����
                            room.Cards[id].Add(new Tuple<byte, byte>(newCard.Item1, newCard.Item2)); // �� ī�� �߰�
                            Debug.Log($"[Special Rule] User {user.Id} exchanged card with deck. New card: {newCard.Item1}:{newCard.Item2}.");

                            foreach (var rp in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_ExhangeCardWithDeckInSpecialRule);
                                PacketWriter.WriteInt(id);
                                PacketWriter.WriteByte(newCard.Item1); // �� ī�� Ÿ��
                                PacketWriter.WriteByte(newCard.Item2); // �� ī�� ��
                                SendPacket(rp);
                            }
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    byte cardType = reader.ReadByte(); // ī�� Ÿ��
                    byte cardValue = reader.ReadByte(); // ī�� ��

                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.NowSpecialRulePlayers.Contains(id) == false)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                            return;
                        }

                        room.Cards2ExchangeInSpecialRule[id] = new Tuple<byte, byte>(cardType, cardValue); // ��ȯ�� ī�� ����
                        int opponentID = -1;
                        if (room.NowSpecialRulePlayers[0] == id)
                            opponentID = room.NowSpecialRulePlayers[1];
                        else if (room.NowSpecialRulePlayers[1] == id)
                            opponentID = room.NowSpecialRulePlayers[0];
                        else
                        {
                            Debug.LogError($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                            return;
                        }

                        foreach (var pl in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2UAsk_ExhangeCardWithOpponentInSpecialRule);
                            PacketWriter.WriteInt(id); // ��û�� ���� ID
                            PacketWriter.WriteInt(opponentID); // ��� ���� ID
                            SendPacket(pl);
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.S2UResponse_WillAcceptExhangeCardWithOpponentISR:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    bool accept = reader.ReadBool(); // ��밡 ī�� ��ȯ ��û�� �����ߴ��� ����

                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        int requester = -1;
                        if (room.NowSpecialRulePlayers[0] == id)
                            requester = room.NowSpecialRulePlayers[1];
                        else if (room.NowSpecialRulePlayers[1] == id)
                            requester = room.NowSpecialRulePlayers[0];
                        else
                        {
                            Debug.LogError($"[Warning] User {user.Id} tried to respond to card exchange request in an invalid state: {room.State}.");
                            return;
                        }
                        if (accept)
                        {
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_IsOpponentAcceptedCardExchangeISR);
                                PacketWriter.WriteInt(requester); // ��û�� ���� ID
                                PacketWriter.WriteInt(id); // ��û�� ���� ID
                                PacketWriter.WriteBool(true); // ī�� ��ȯ ����
                                SendPacket(pl);
                            }
                        }
                        else
                        {
                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_IsOpponentAcceptedCardExchangeISR);
                                PacketWriter.WriteInt(requester); // ��û�� ���� ID
                                PacketWriter.WriteInt(id); // ��û�� ���� ID
                                PacketWriter.WriteBool(false); // ī�� ��ȯ ����
                                SendPacket(pl);
                            }
                            Debug.Log($"[Special Rule] User {user.Id} rejected the card exchange request.");
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.U2SRequest_OpponentSelectedCardToExhangeISR:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    byte cardType = reader.ReadByte(); // ī�� Ÿ��
                    byte cardValue = reader.ReadByte(); // ī�� ��
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.NowSpecialRulePlayers.Contains(id) == false)
                        {
                            Debug.Log($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                            return;
                        }
                        room.Cards2ExchangeInSpecialRule[id] = new Tuple<byte, byte>(cardType, cardValue); // ��밡 ��ȯ�� ī�� ����

                        int opponentID = -1;
                        if (room.NowSpecialRulePlayers[0] == id)
                            opponentID = room.NowSpecialRulePlayers[1];
                        else if (room.NowSpecialRulePlayers[1] == id)
                            opponentID = room.NowSpecialRulePlayers[0];
                        else
                        {
                            Debug.LogError($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                            return;
                        }


                        foreach (var pl in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_ExhangeWithOpponentInSpecialRuleResult);
                            PacketWriter.WriteInt(id); // ��û�� ���� ID
                            if (room.Cards2ExchangeInSpecialRule.TryGetValue(id, out var card))
                            {
                                PacketWriter.WriteByte(card.Item1); // ��ȯ�� ī�� Ÿ��
                                PacketWriter.WriteByte(card.Item2); // ��ȯ�� ī�� ��
                            }
                            else
                            {
                                PacketWriter.WriteByte(0); // �⺻��
                                PacketWriter.WriteByte(0); // �⺻��
                                                           //���� ���
                                Debug.LogError($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}. User's card not found.");
                            }
                            PacketWriter.WriteInt(opponentID); // ��� ���� ID
                            if (room.Cards2ExchangeInSpecialRule.TryGetValue(opponentID, out var opponentCard))
                            {
                                PacketWriter.WriteByte(opponentCard.Item1); // ��밡 ��ȯ�� ī�� Ÿ��
                                PacketWriter.WriteByte(opponentCard.Item2); // ��밡 ��ȯ�� ī�� ��
                            }
                            else
                            {
                                PacketWriter.WriteByte(0); // �⺻��
                                PacketWriter.WriteByte(0); // �⺻��
                                                           //���� ���
                                Debug.LogError($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}. Opponent's card not found.");
                            }
                            SendPacket(pl);
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule:
                {
                    int id = reader.ReadInt();
                    int roomID = reader.ReadInt();
                    if (Rooms.TryGetValue(roomID, out var room))
                    {
                        if (room.NowSpecialRulePlayers.Contains(id) == false)
                        {
                            Debug.LogError($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                            return;
                        }
                        room.PlayerCountWhoAcceptedExchangeInSpecialRule += 1;

                        if (room.PlayerCountWhoAcceptedExchangeInSpecialRule == 2)
                        {
                            //��� ������ ī�� ��ȯ�� �Ϸ�����
                            Debug.Log($"[Special Rule] All players have successfully exchanged cards with their opponents.");


                            //������ ī�� ����
                            int player1 = room.NowSpecialRulePlayers[0];
                            int player2 = room.NowSpecialRulePlayers[1];

                            var player1Card = room.Cards2ExchangeInSpecialRule[player1];
                            var player2Card = room.Cards2ExchangeInSpecialRule[player2];

                            room.Cards[player1].Remove(player1Card);
                            room.Cards[player1].Add(player2Card); // �÷��̾� 1�� ī�忡 �÷��̾� 2�� ī�带 �߰�

                            room.Cards[player2].Remove(player2Card);
                            room.Cards[player2].Add(player1Card); // �÷��̾� 2�� ī�忡 �÷��̾� 1�� ī�带 �߰�
                            room.PlayerCountWhoAcceptedExchangeInSpecialRule = 0;
                        }
                    }
                }
                break;
            default:
                Debug.Log($"[Warning] Unknown packet type received from User {user.Id}. PacketByte {pac}");
                break;
        }
    }
    int CalculateSpecialRuleLoser(Room room)
    {
        int s1 = CalculateScore(room.Cards[room.NowSpecialRulePlayers[0]]);
        int s2 = CalculateScore(room.Cards[room.NowSpecialRulePlayers[1]]);
        //������ �ִ� ī�带 ���� ���
        Debug.Log($"[Special Rule] Player {room.NowSpecialRulePlayers[0]} cards: {string.Join(", ", room.Cards[room.NowSpecialRulePlayers[0]].Select(c => $"{c.Item1}:{c.Item2}"))}");
        Debug.Log($"[Special Rule] Player {room.NowSpecialRulePlayers[1]} cards: {string.Join(", ", room.Cards[room.NowSpecialRulePlayers[1]].Select(c => $"{c.Item1}:{c.Item2}"))}");
        Debug.Log($"[Special Rule] Player {room.NowSpecialRulePlayers[0]} score: {s1}, Player {room.NowSpecialRulePlayers[1]} score: {s2}.");
        if (s1 < s2)
            return room.NowSpecialRulePlayers[0];
        else if (s1 > s2)
            return room.NowSpecialRulePlayers[1];
        else
            return -1; // ���º�
    }
    void SendStartSpecialRule(Room room, int p1, int p2, byte reason)
    {
        var cards = CreateRandomCards(room.Mode == eGameMode.TwoCards ? 4 : 6);
        room.GoPlayerCountInSpecialRule = 0;
        room.PlayerCounter_SelectedCard2Delete = 0;
        room.NowSpecialRulePlayers.Clear();
        room.NowSpecialRulePlayers.Add(p1);
        room.NowSpecialRulePlayers.Add(p2);
        room.Cards.Clear();
        if (room.Mode == eGameMode.TwoCards)
        {
            room.Cards.Add(p1, new List<Tuple<byte, byte>> { cards[0], cards[1] });
            room.Cards.Add(p2, new List<Tuple<byte, byte>> { cards[2], cards[3] });
        }
        else
        {
            room.Cards.Add(p1, new List<Tuple<byte, byte>> { cards[0], cards[1], cards[2] });
            room.Cards.Add(p2, new List<Tuple<byte, byte>> { cards[3], cards[4], cards[5] });
        }
        //ī�� �� ���
        Debug.Log($"[Special Rule] Player {p1} cards: {string.Join(", ", room.Cards[p1].Select(c => $"{c.Item1}:{c.Item2}"))}");
        Debug.Log($"[Special Rule] Player {p2} cards: {string.Join(", ", room.Cards[p2].Select(c => $"{c.Item1}:{c.Item2}"))}");
        //����� ��

        //foreach (var pl in room.Players)
        if (room.Mode == eGameMode.ThreeCards)
            room.State = eRoomState.WaitingForPlayersToSelectCard2RemoveISR;
        {
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_StartSpecialRule);
            PacketWriter.WriteByte(reason); //
            PacketWriter.WriteInt(p1);
            PacketWriter.WriteInt(p2);
            if (room.Mode == eGameMode.TwoCards)
            {
                PacketWriter.WriteByte(cards[0].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[0].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[1].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[1].Item2); // ī�� ��
            }
            else
            {
                PacketWriter.WriteByte(cards[0].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[0].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[1].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[1].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[2].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[2].Item2); // ī�� ��
            }
            SendPacket(room.Players.Find((u) => u.Id == p1));

            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_StartSpecialRule);
            PacketWriter.WriteByte(reason); //
            PacketWriter.WriteInt(p2);
            PacketWriter.WriteInt(p1);
            if (room.Mode == eGameMode.TwoCards)
            {
                PacketWriter.WriteByte(cards[2].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[2].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[3].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[3].Item2); // ī�� ��
            }
            else
            {
                PacketWriter.WriteByte(cards[3].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[3].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[4].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[4].Item2); // ī�� ��
                PacketWriter.WriteByte(cards[5].Item1); // ī�� Ÿ��
                PacketWriter.WriteByte(cards[5].Item2); // ī�� ��
            }
            SendPacket(room.Players.Find((u) => u.Id == p2));


            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_StartSpecialRule);
            PacketWriter.WriteByte(reason); //
            PacketWriter.WriteInt(p1);
            PacketWriter.WriteInt(p2);
            SendPacket(room.Players.Find((u) => (u.Id != p2 && u.Id != p1)));
        }
    }
    void StartNextRound(Room room, byte reason = 0)
    {
        room.State = eRoomState.ShouldDistributeCards;
        room.PlayerCounter_SuccessfullyReceivedCard = 0;
        room.RPSFirst = null;
        room.RPSSecond = null;
        room.RPSThird = null;
        room.NowRPSRoundCounter = 0;
        room.PlayerCounter_SuccessfullyReceivedOrders = 0;
        room.PlayerCounter_SelectedCard2Delete = 0;
        room.RPSTargetPlayers.Clear();
        room.RPSSelections.Clear();
        room.InPlayers.Clear();
        room.OutPlayers.Clear();

        int finalLoser = -1;
        foreach (var pl in room.Players)
        {
            if (room.OutCounts.TryGetValue(pl.Id, out var count) && count <= 0)
            {
                finalLoser = pl.Id;
                break;
            }
        }

        if (finalLoser != -1)
        {
            foreach (var pl in room.Players)
            {
                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_FinalResult);
                PacketWriter.WriteInt(finalLoser);
                SendPacket(pl);
            }
            return;
        }

        foreach (var pl in room.Players)
        {
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_StartNextRound);
            PacketWriter.WriteByte(reason); //0�� 3�� �� �� & ������ ���
            SendPacket(pl);
        }
        //Task.Delay(2000).ContinueWith((_) =>
        DelayedFunctionHelper.InvokeDelayed(() =>
        {
            room.State = eRoomState.ShouldDistributeCards;
            if (room.Mode == eGameMode.TwoCards)
                DistributeRandom2CardsForUsers(room);
            if (room.Mode == eGameMode.ThreeCards)
                DistributeRandom3CardsForUsers(room);


            room.State = eRoomState.WaitingForSuccessfulCardReception;
        }, 2.0f);
    }
    bool CalculateResult(int roomID)
    {
        Room room;
        if (Rooms.TryGetValue(roomID, out room) == false)
            return false;

        int outPlayerCount = room.OutPlayers.Count;

        if (outPlayerCount == 3)
        {
            Debug.Log($"[Warning] All players are out in Room {roomID}. No further action taken.");
            return false;
        }

        if (outPlayerCount == 2)
        {
            SendStartSpecialRule(room, room.OutPlayers[0], room.OutPlayers[1], reason: 1); //�ƿ��� 2���� ���
            return false;
        }

        else if (outPlayerCount == 0)
        {
            //���� ���� �� In
            int lowestScore = 1000;
            int lowerID = -1;
            List<int> playersWithLowersScore = new List<int>();
            foreach (var player in room.InPlayers)
            {
                var cards = room.Cards[player];
                int score = CalculateScore(cards);
                if (score < lowestScore)
                {
                    lowestScore = score;
                    lowerID = player;
                }
            }
            foreach (var player in room.InPlayers)
            {
                var cards = room.Cards[player];
                int score = CalculateScore(cards);
                if (score == lowestScore)
                {
                    playersWithLowersScore.Add(player);
                }
            }


            if (playersWithLowersScore.Count == 1) //������ 1��
            {
                //1���� �й�
                lowerID = playersWithLowersScore[0];
                room.OutCounts[lowerID]--;
                foreach (var pl in room.Players)
                {
                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_LoserOfThisRound);
                    PacketWriter.WriteInt(lowerID);
                    PacketWriter.WriteByte(room.OutCounts[lowerID]);
                    SendPacket(pl);
                }

                StartNextRound(room, 5); //5�� 3�� �� �� & ���� 1���� ���
                return false;
            }
            else if (playersWithLowersScore.Count == 2) //������ 2��
            {
                //����� ��

                SendStartSpecialRule(room, playersWithLowersScore[0], playersWithLowersScore[1], reason: 2); //������ �߻�
                return false;
            }
            else if (playersWithLowersScore.Count == 3) //������ 3��
            {
                StartNextRound(room, 4); //4�� 3�� �� �� & ������ ���
                return false;
            }
        }
        else
        {
            //1���� Out
            int lowestScoreOfInPlayers = 1000;
            int lowerID = -1;
            List<int> playersInWithLowestScore = new List<int>();
            foreach (var player in room.InPlayers)
            {
                var cards = room.Cards[player];
                int score = CalculateScore(cards);
                if (score < lowestScoreOfInPlayers)
                {
                    lowestScoreOfInPlayers = score;
                    lowerID = player;
                }
            }
            foreach (var player in room.InPlayers)
            {
                var cards = room.Cards[player];
                int score = CalculateScore(cards);
                if (score == lowestScoreOfInPlayers)
                {
                    playersInWithLowestScore.Add(player);
                }
            }

            int outScore = CalculateScore(room.Cards[room.OutPlayers[0]]);

            if (outScore > lowestScoreOfInPlayers)
            {
                //outPlayer�� �й�
                room.OutCounts[room.OutPlayers[0]]--;
                foreach (var pl in room.Players)
                {
                    PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_LoserOfThisRound);
                    PacketWriter.WriteInt(room.OutPlayers[0]);
                    PacketWriter.WriteByte(room.OutCounts[room.OutPlayers[0]]);
                    SendPacket(pl);
                }
                return false;
            }
            else if (outScore == lowerID)
            {
                //����� ��
                if (playersInWithLowestScore.Count == 2) //���� ������ 2��
                {
                    SendStartSpecialRule(room, playersInWithLowestScore[0], playersInWithLowestScore[1], reason: 3); //������ �߻�
                    return false;
                }
                else if (playersInWithLowestScore.Count == 1) //���� ������ 1��
                {
                    //���� ������ �й�
                    room.OutCounts[playersInWithLowestScore[0]]--;
                    foreach (var pl in room.Players)
                    {
                        PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_LoserOfThisRound);
                        PacketWriter.WriteInt(playersInWithLowestScore[0]);
                        PacketWriter.WriteByte(room.OutCounts[playersInWithLowestScore[0]]);
                        SendPacket(pl);
                    }
                }
                return false;
            }
            else
            {
                if (playersInWithLowestScore.Count == 1)
                {
                    //���� ������ �й�
                    room.OutCounts[playersInWithLowestScore[0]]--;
                    foreach (var pl in room.Players)
                    {
                        PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_LoserOfThisRound);
                        PacketWriter.WriteInt(playersInWithLowestScore[0]);
                        PacketWriter.WriteByte(room.OutCounts[playersInWithLowestScore[0]]);
                        SendPacket(pl);
                    }
                    return false;
                }
                else if (playersInWithLowestScore.Count == 2)
                {
                    SendStartSpecialRule(room, playersInWithLowestScore[0], playersInWithLowestScore[1], reason: 4); // ������ �߻�
                    return false;
                }
                else
                {
                    Debug.Log("[Warning] Unexpected case in CalculateResult: More than 2 players with the lowest score.");
                }
            }
        }
        return true;
    }
    int CalculateScore(List<Tuple<byte, byte>> cards)
    {
        if (cards.Count < 2)
        {
            return 0;
        }
        var card1 = cards[0];
        var card2 = cards[1];
        if (card1.Item2 == card2.Item2) //���� ����
        {
            Debug.Log($"[Special Rule] Pair found: {card1.Item1}:{card1.Item2} and {card2.Item1}:{card2.Item2}.");
            return 100 + card1.Item2; // Special case for pairs
        }
        else
        {
            Debug.Log($"[Special Rule] Normal case: {card1.Item1}:{card1.Item2} and {card2.Item1}:{card2.Item2}.");
            return (card1.Item2 + card2.Item2 + 2) % 10; // Normal case
        }
    }
    private void SendIOResultAllPlayers(Room room, bool isFinal)
    {
        foreach (var rp in room.Players)
        {
            if (isFinal == true)
                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_InOutFinalResult);
            else
                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_InOutRoundResult);
            PacketWriter.WriteByte((byte)(room.InPlayers.Count + room.OutPlayers.Count));
            foreach (var inPlayer in room.InPlayers)
            {
                PacketWriter.WriteInt(inPlayer);
            }
            PacketWriter.WriteInt(-1);
            foreach (var outPlayer in room.OutPlayers)
            {
                PacketWriter.WriteInt(outPlayer);
            }
            SendPacket(rp);
        }
    }

    public void StartSelectCard2Delete(Room room)
    {
        room.PlayerCounter_SelectedCard2Delete = 0;
        foreach (var player in room.Players)
        {
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_SelectCard2Delete);
            SendPacket(player);
        }
    }
    public void StartRPS(Room room)
    {
        bool test = false;
        test = false;
        if (test == true)
        {
            room.RPSFirst = room.Players[0].Id;
            room.RPSSecond = room.Players[1].Id;
            room.RPSThird = room.Players[2].Id;
            //room.State = eRoomState.
            foreach (var p in room.Players)
            {
                PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.Broadcast_RPSFinalResult);

                PacketWriter.WriteByte(room.NowRPSRoundCounter);
                // ������� ID ����Ʈ ����
                PacketWriter.WriteInt((int)(room.RPSFirst));  // 1��
                PacketWriter.WriteInt((int)(room.RPSSecond));  // 2��
                PacketWriter.WriteInt((int)(room.RPSThird));  // 3��

                SendPacket(p);
            }
            return;
        }
        room.State = eRoomState.WaitingForRPSSelection;
        room.RPSTargetPlayers = room.Players.Select(u => u.Id).ToList();
        room.RPSSelections.Clear();

        foreach (var pid in room.RPSTargetPlayers)
        {
            var player = room.Players.First(u => u.Id == pid);
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.S2URequest_RPSSelection);
            SendPacket(player);
        }
    }

    public List<Tuple<byte, byte>> CreateRandomCards(int count)
    {
        List<Tuple<byte, byte>> cards = new List<Tuple<byte, byte>>();
        for (int i = 0; i < count; i++)
        {
            cards.Add(GetRandomCard().ToTuple());
        }
        return cards;
    }
    public void DistributeRandom2CardsForUsers(Room room)
    {
        room.Cards.Clear();
        foreach (var player in room.Players)
        {
            var card1 = GetRandomCard();
            var card2 = GetRandomCard(new List<Tuple<byte, byte>>(new [] { card1.ToTuple() }));

            room.Cards.Add(player.Id, new List<Tuple<byte, byte>> { card1.ToTuple(), card2.ToTuple() });
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.DistributeCardsFromServer);
            PacketWriter.WriteByte(card1.Item1);
            PacketWriter.WriteByte(card1.Item2);
            PacketWriter.WriteByte(card2.Item1);
            PacketWriter.WriteByte(card2.Item2);
            SendPacket(player);
            Debug.Log($"Distributing random cards to User {player.Id}.");
        }
    }
    public void DistributeRandom3CardsForUsers(Room room)
    {
        room.Cards.Clear();
        foreach (var player in room.Players)
        {
            var card1 = GetRandomCard();
            var card2 = GetRandomCard(new List<Tuple<byte, byte>>(new[] { card1.ToTuple() }));
            var card3 = GetRandomCard(new List<Tuple<byte, byte>>(new[] { card1.ToTuple(), card2.ToTuple() }));
            room.Cards.Add(player.Id, new List<Tuple<byte, byte>> { card1.ToTuple(), card2.ToTuple(), card3.ToTuple() });
            PacketWriter.CreateNewPacket((byte)ePacketType_InGameServer.DistributeCardsFromServer);
            PacketWriter.WriteByte(card1.Item1);
            PacketWriter.WriteByte(card1.Item2);
            PacketWriter.WriteByte(card2.Item1);
            PacketWriter.WriteByte(card2.Item2);
            PacketWriter.WriteByte(card3.Item1);
            PacketWriter.WriteByte(card3.Item2);
            SendPacket(player);
            Debug.Log($"Distributing random cards to User {player.Id}.");
        }
    }
}
