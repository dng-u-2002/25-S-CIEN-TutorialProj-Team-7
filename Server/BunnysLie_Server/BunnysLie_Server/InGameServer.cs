using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BunnysLie_Server
{
    public enum eRPS
    {
        Rock,
        Paper,
        Scissors
    }
    public enum eRoomState
    {
        None,
        ShouldDistributeCards,
        WaitingForSuccessfulCardReception,
        ShouldStartRPS,
        WaitingForRPSSelection
    }
    public enum eRoomGameMode
    {
        TwoCards = 2,
        ThreeCards = 3
    }
    public class Room
    {
        public List<User> Players;
        public eRoomState State = eRoomState.None;
        public eRoomGameMode Mode = eRoomGameMode.TwoCards;

        public Dictionary<int, byte> OutCounts = new Dictionary<int, byte>();
        public Dictionary<int, List<Tuple<byte, byte>>> Cards = new Dictionary<int, List<Tuple<byte, byte>>>();

        public int PlayerCounter_SuccessfullyReceivedCard = 0;
        public int? RPSFirst;
        public int? RPSSecond;
        public int? RPSThird;
        public byte NowRPSRoundCounter = 0;
        public byte PlayerCounter_SuccessfullyReceivedOrders;
        public List<int> RPSTargetPlayers = new List<int>(); // 지금 가위바위보 중인 플레이어 ID 목록
        public Dictionary<int, eRPS> RPSSelections = new Dictionary<int, eRPS>();

        public List<int> NowSpecialRulePlayers = new List<int>(); // 현재 스페셜 룰에 참여 중인 플레이어 ID 목록
        public Dictionary<int, Tuple<byte, byte>> Cards2ExchangeInSpecialRule = new Dictionary<int, Tuple<byte, byte>>();
        public byte PlayerCountWhoAcceptedExchangeInSpecialRule = 0; // 스페셜 룰에서 카드 교환을 수락한 플레이어 수

        public int GoPlayerCountInSpecialRule = 0;

        public List<int> InPlayers = new List<int>();
        public List<int> OutPlayers = new List<int>();

    }
    public class InGameServer : Server
    {
        static Random Random = new Random();
        public (byte, byte) GetRandomCard()
        {
            int type = Random.Next(0, 2); // 0 or 1
            int value = Random.Next(0, 10); // 0 to 9
            return ((byte)type, (byte)value);
        }
        public enum ePacketType
        {
            None = 0,
            Error = 1,
            HandShake_S2U,
            HandShake_U2S,
            Chat_Send,
            Chat_Receive,

            U2SRequest_JoinGame,
            S2UResponse_JoinGame,


            Broadcast_StartGame,


            DistributeCardsFromServer, //서버에서 룸의 유저들에게 카드 분배
            U2SResponse_SuccessfullyReceivedCards, //

            S2URequest_RPSSelection, //가위바위보 선택 요청
            U2SResponse_RPSSelection, //가위바위보 선택 응답(유저가 선택한 가위바위보 모양을 서버에 전송)

            //Broadcast_ShowRPSRoundResult_Selection,
            //Broadcast_ShowRPSRoundResult_Order,
            //위 2개는 Broadcast_RPSRoundResult로 통합. 나머지는 클라이언트에서 처리
            Broadcast_RPSRoundResult,

            //Ready2RPSRematch,
            //위 1개는 Broadcast_RPSStartRematch로 변경.
            Broadcast_RPSStartRematch,

            Broadcast_RPSFinalResult,
            U2SResponse_SuccessfullyReceivedOrders,




            S2URequest_SelectInOut_First,
            U2SResponse_SelectInOut_First,

            S2URequest_SelectInOut_Second,
            U2SResponse_SelectInOut_Second,
            S2URequest_SelectInOut_Third,
            U2SResponse_SelectInOut_Third,

            Broadcast_InOutRoundResult,
            Broadcast_InOutFinalResult,

            Broadcast_SendAllPlayersCardData,
            Broadcast_ShowAllCards,

            Broadcast_LoserOfThisRound,

            Broadcast_StartSpecialRule,
            GoInSpecialRule,
            Broadcast_ShowSpecialRuleCards,
            Broadcast_LoserOfSpecialRule,

            Broadcast_StartNextRound,
            U2SRequest_ExchangeCardWithDeckInSpecialRule,
            S2UResponse_ExhangeCardWithDeckInSpecialRule,

            U2SRequest_ExhangeCardWithOpponentInSpecialRule,

            S2UAsk_ExhangeCardWithOpponentInSpecialRule, // 상대와 카드 교환 요청
            S2UResponse_WillAcceptExhangeCardWithOpponentISR, // 상대가 카드 교환 요청에 응답
            Broadcast_IsOpponentAcceptedCardExchangeISR,

            U2SRequest_OpponentSelectedCardToExhangeISR, //상대가 카드를 골랐음
            Broadcast_ExhangeWithOpponentInSpecialRuleResult, //교환 결과
            U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule,

            Broadcast_FinalResult

        }

        Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        List<User> WaitingUsers_TwoMode = new List<User>();
        List<User> WaitingUsers_ThreeMode = new List<User>();

        private eRPS? GetWinningRPSChoice(IEnumerable<eRPS> choices)
        {
            var distinct = choices.Distinct().ToList();
            // 모두 같은 모양이거나 가위·바위·보가 모두 섞였으면 무승부
            if (distinct.Count == 1 || distinct.Count == 3)
                return null;
            // 두 가지 모양만 남았을 때, 이기는 쪽을 반환
            if (distinct.Contains(eRPS.Rock) && distinct.Contains(eRPS.Scissors)) return eRPS.Rock;
            if (distinct.Contains(eRPS.Scissors) && distinct.Contains(eRPS.Paper)) return eRPS.Scissors;
            if (distinct.Contains(eRPS.Paper) && distinct.Contains(eRPS.Rock)) return eRPS.Paper;
            return null;
        }

        void SendRPSRoundResult(Room room)
        {
            foreach (var p in room.Players)
            {
                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_RPSRoundResult);
                //현재 라운드 : byte
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

                    if(room.RPSFirst == rp)
                        PacketWriter.WriteByte(0); // 1등
                    else if (room.RPSSecond == rp)
                        PacketWriter.WriteByte(1); // 2등
                    else if (room.RPSThird == rp)
                        PacketWriter.WriteByte(2); // 3등
                    else
                        PacketWriter.WriteByte(3); // 아직 결정 안됨
                }
                if (room.RPSTargetPlayers.Count == 2)
                {
                    PacketWriter.WriteInt(-1);
                    PacketWriter.WriteByte(4);
                    PacketWriter.WriteByte(4); //그냥 아무 값
                }
                SendPacket(p);
            }
        }

        void CheckRPSRoundResult(Room room, System.Action<List<int>> onPlayersShouldRematch)
        {
            // 현재 라운드 응답이 다 모였으면
            const int Delay_SendRematchAfterDraw = 3000;
            const int Delay_ShowOrderAfterRPS = 1500;
            System.Action<List<int>> requstRematch = null; // 재대결 요청을 위한 콜백
            List<int> playersShouldRematch = new List<int>();
            if (room.RPSSelections.Count == room.RPSTargetPlayers.Count)
            {
                room.NowRPSRoundCounter += 1;
                var winChoice = GetWinningRPSChoice(room.RPSSelections.Values);
                if (winChoice == null)
                {
                    //무승부 같은 대상에게 다시 요청

                    //선택 결과 출력
                    Console.WriteLine($"[RPS Round {room.NowRPSRoundCounter}] Draw. Players should rematch: {string.Join(", ", room.RPSTargetPlayers)}");
                    //print rps selections
                    playersShouldRematch = new List<int>(room.RPSTargetPlayers); //vaule type의 deep copy는 new List<T>만 해도 됨
                    requstRematch = onPlayersShouldRematch;

                    SendRPSRoundResult(room);

                    //send packet after 1sec
                    Task.Delay(Delay_SendRematchAfterDraw + Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                    {
                        requstRematch?.Invoke(playersShouldRematch);
                    });
                }
                else
                {
                    var winners = room.RPSTargetPlayers
                                      .Where(id => room.RPSSelections[id] == winChoice)
                                      .ToList();
                    var losers = room.RPSTargetPlayers.Except(winners).ToList();

                    if (room.RPSTargetPlayers.Count == 3)
                    {
                        if (winners.Count == 1) //승자 1명, 패자 2명
                        {
                            int winnerID = winners[0];
                            room.RPSFirst = winnerID; // 단독 승자

                            playersShouldRematch = new List<int>(losers);
                            requstRematch = onPlayersShouldRematch;
                        }
                        else if (winners.Count == 2) //승자 2명, 패자 1명
                        {
                            int loserID = losers[0];
                            room.RPSThird = loserID; // 단독 패자

                            playersShouldRematch = new List<int>(winners);
                            requstRematch = onPlayersShouldRematch;
                        }
                    }
                    else if (room.RPSTargetPlayers.Count == 2) //재대결
                    {
                        //if(winners.Count == 1) // 승자 1명, 패자 1명. 반드시 이 경우밖에 없음.
                        //{
                        int winnerID = winners[0];
                        int loserID = losers[0];

                        if (room.RPSFirst.HasValue == true) //1등이 정해진 재대결이였을 경우 
                        {
                            room.RPSSecond = winnerID;
                            room.RPSThird = loserID;
                        }
                        else //3등이 정해진 재대결이였을 경우
                        {
                            room.RPSFirst = winnerID;
                            room.RPSSecond = loserID;
                        }
                        //}
                    }


                    #region
                    //가위바위보 선택지를 보여 주고
                    //foreach (var p in room.Players)
                    //{
                    //    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_ShowRPSRoundResult_Selection);
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
                    ////1초 있다가 순위를 보여줌
                    //Task.Delay(Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                    //{
                    //    foreach (var p in room.Players)
                    //    {
                    //        PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_ShowRPSRoundResult_Order);

                    //        PacketWriter.WriteByte(room.NowRPSRoundCounter);
                    //        // 순서대로 ID 리스트 전송
                    //        PacketWriter.WriteInt((int)(room.RPSFirst.HasValue ? room.RPSFirst : -1));  // 1등
                    //        PacketWriter.WriteInt((int)(room.RPSSecond.HasValue ? room.RPSSecond : -1));  // 2등
                    //        PacketWriter.WriteInt((int)(room.RPSThird.HasValue ? room.RPSThird : -1));  // 3등

                    //        SendPacket(p);
                    //    }
                    //});
                    #endregion
                    //위에 2개 통합, 딜레이는 클라이언트에서 처리
                    SendRPSRoundResult(room);

                    if ((room.RPSFirst.HasValue && room.RPSSecond.HasValue && room.RPSThird.HasValue) == false)
                    {
                        //send packet after 1sec
                        Task.Delay(Delay_SendRematchAfterDraw + Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                        {
                            requstRematch?.Invoke(playersShouldRematch);
                        });
                    }
                    else
                    {
                        Task.Delay(Delay_ShowOrderAfterRPS).ContinueWith(_ =>
                        {
                            foreach (var p in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_RPSFinalResult);

                                PacketWriter.WriteByte(room.NowRPSRoundCounter);
                                // 순서대로 ID 리스트 전송
                                PacketWriter.WriteInt((int)(room.RPSFirst));  // 1등
                                PacketWriter.WriteInt((int)(room.RPSSecond));  // 2등
                                PacketWriter.WriteInt((int)(room.RPSThird));  // 3등

                                SendPacket(p);
                            }
                            room.RPSSelections.Clear();
                            room.RPSTargetPlayers.Clear();
                            room.NowRPSRoundCounter = 0;
                        });
                    }
                }
            }
        }

        protected override void OnReceivePacketFromUser(User user, NetworkDataReader reader, byte channelNumber, ePacketSafefyLevel safetyLevel)
        {
            var pac = reader.ReadByte();
            var packet = (ePacketType)pac;
            Console.WriteLine($"[Packet Received] User {user.Id} sent a packet {packet}.");
            switch (packet)
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
                case ePacketType.U2SRequest_JoinGame:
                    {
                        int id = reader.ReadInt();
                        byte mode = reader.ReadByte();
                        if (mode == 2)
                        {
                            WaitingUsers_TwoMode.Add(user);
                            Console.WriteLine($"[JoinGame] User {user.Id} requested to join a 2-player game.");
                        }
                        else if (mode == 3)
                        {
                            WaitingUsers_ThreeMode.Add(user);
                            Console.WriteLine($"[JoinGame] User {user.Id} requested to join a 3-player game.");
                        }
                        else
                        {
                            Console.WriteLine($"[JoinGame] User {user.Id} requested to join a game with unknown mode {mode}.");
                        }
                        PacketWriter.CreateNewPacket((byte)ePacketType.S2UResponse_JoinGame);
                        SendPacket(user);
                    }
                    break;
                case ePacketType.U2SResponse_SuccessfullyReceivedCards:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if(room.State != eRoomState.WaitingForSuccessfulCardReception)
                            {
                                //이건 에러임
                                Console.WriteLine($"[Warning] User {user.Id} tried to receive cards in an invalid state: {room.State}.");
                                return;
                            }
                            room.PlayerCounter_SuccessfullyReceivedCard += 1;

                            if(room.PlayerCounter_SuccessfullyReceivedCard == 3)
                            {
                                room.State = eRoomState.ShouldStartRPS;
                                StartRPS(room);
                                room.PlayerCounter_SuccessfullyReceivedCard = -1;
                                room.State = eRoomState.WaitingForRPSSelection;
                            }
                        }
                    }
                    break;
                case ePacketType.U2SResponse_RPSSelection:
                    {
                        int userId = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        var room = Rooms[roomID];
                        if (room.State != eRoomState.WaitingForRPSSelection ||
                            !room.RPSTargetPlayers.Contains(userId))
                            return;

                        room.RPSSelections[userId] = (eRPS)reader.ReadByte();
                        Console.WriteLine($"[RPS Selection] User {user.Id} selected {room.RPSSelections[userId]} in Room {roomID}.");

                        CheckRPSRoundResult(room,
                            (players2Rematch) =>
                            {
                                room.RPSTargetPlayers = players2Rematch;
                                room.RPSSelections.Clear();
                                room.State = eRoomState.WaitingForRPSSelection;
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_RPSStartRematch);
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
                case ePacketType.U2SResponse_SuccessfullyReceivedOrders:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        byte order = reader.ReadByte();
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if (room.RPSFirst == id && order == 0)
                                room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                            else if (room.RPSSecond == id && order == 1)
                                room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                            else if (room.RPSThird == id && order == 2)
                                room.PlayerCounter_SuccessfullyReceivedOrders += 1;
                            else
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to receive orders in an invalid state: {room.State}.");
                                return;
                            }

                            if(room.PlayerCounter_SuccessfullyReceivedOrders == 3)
                            {
                                var fp = room.Players.Find((u) => room.RPSFirst == u.Id);
                                PacketWriter.CreateNewPacket((byte)ePacketType.S2URequest_SelectInOut_First);
                                SendPacket(fp);
                            }
                        }
                    }
                    break;
                case ePacketType.U2SResponse_SelectInOut_First:
                case ePacketType.U2SResponse_SelectInOut_Second:
                case ePacketType.U2SResponse_SelectInOut_Third:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();

                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            byte order = reader.ReadByte();
                            if (packet == ePacketType.U2SResponse_SelectInOut_First && order != 0)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                                return;
                            }
                            if (packet == ePacketType.U2SResponse_SelectInOut_Second && order != 1)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                                return;
                            }
                            if (packet == ePacketType.U2SResponse_SelectInOut_Third && order != 2)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to select In/Out in an invalid state: {room.State}.");
                                return;
                            }
                            byte selection = reader.ReadByte(); // 0: In, 1: Out
                            if (selection == 0) // In
                            {
                                room.InPlayers.Add(id);
                                Console.WriteLine($"[In/Out Selection] User {user.Id} selected In in Room {roomID}.");
                            }
                            else if (selection == 1) // Out
                            {
                                room.OutPlayers.Add(id);
                                Console.WriteLine($"[In/Out Selection] User {user.Id} selected Out in Room {roomID}.");
                            }
                            else
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to select an invalid in/out option: {selection}.");
                                return;
                            }

                            SendIOResultAllPlayers(room, false);

                            if (packet == ePacketType.U2SResponse_SelectInOut_First)
                            {
                                var fp = room.Players.Find((u) => room.RPSSecond == u.Id);
                                PacketWriter.CreateNewPacket((byte)ePacketType.S2URequest_SelectInOut_Second);
                                SendPacket(fp);
                            }
                            else if (packet == ePacketType.U2SResponse_SelectInOut_Second)
                            {
                                var fp = room.Players.Find((u) => room.RPSThird == u.Id);
                                PacketWriter.CreateNewPacket((byte)ePacketType.S2URequest_SelectInOut_Third);
                                SendPacket(fp);
                            }
                            else if (packet == ePacketType.U2SResponse_SelectInOut_Third)
                            {
                                // 세 번째 선택 후 최종 결과 전송
                                SendIOResultAllPlayers(room, true);

                                Console.WriteLine($"[In/Out Selection] Room {roomID} final result sent. In: {string.Join(", ", room.InPlayers)}, Out: {string.Join(", ", room.OutPlayers)}.");
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_SendAllPlayersCardData);
                                    foreach (var cards in room.Cards)
                                    {
                                        PacketWriter.WriteInt(cards.Key);
                                        foreach (var c in cards.Value)
                                        {
                                            PacketWriter.WriteByte(c.Item1); // 카드 타입
                                            PacketWriter.WriteByte(c.Item2); // 카드 값
                                        }
                                    }
                                    SendPacket(pl);
                                }
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_ShowAllCards);
                                    SendPacket(pl);
                                }
                                Task.Delay(2000).ContinueWith((_) =>
                                {
                                    if(CalculateResult(roomID) == true)
                                    {
                                        Task.Delay(2000).ContinueWith((_) =>
                                        {
                                            StartNextRound(room);
                                        });
                                    }
                                });
                            }
                        }
                    }
                    break;

                case ePacketType.GoInSpecialRule:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            room.GoPlayerCountInSpecialRule += 1;

                            if(room.GoPlayerCountInSpecialRule == 2)
                            {
                                Console.WriteLine("[Special Rule] Show special rule cards to players.");
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_ShowSpecialRuleCards);
                                    SendPacket(pl);
                                }

                                Task.Delay(1000).ContinueWith((_) =>
                                {
                                    int loser = CalculateSpecialRuleLoser(room);

                                    if(loser == -1) //동점
                                    {
                                        SendStartSpecialRule(room, room.NowSpecialRulePlayers[0], room.NowSpecialRulePlayers[1], reason: 10); //동점자 발생
                                        return;
                                    }

                                    if (loser != -1)
                                        room.OutCounts[loser] -= 1;
                                    foreach (var pl in room.Players)
                                    {
                                        PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_LoserOfSpecialRule);
                                        PacketWriter.WriteInt(loser);
                                        if(loser == -1)
                                            PacketWriter.WriteByte(0); // 무승부
                                        else
                                            PacketWriter.WriteByte(room.OutCounts[loser]);
                                        SendPacket(pl);
                                    }

                                    Task.Delay(3000).ContinueWith((_) =>
                                    {
                                        StartNextRound(room);
                                    });
                                });
                            }
                        }
                    }
                    break;



                case ePacketType.U2SRequest_ExchangeCardWithDeckInSpecialRule:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if (room.NowSpecialRulePlayers.Contains(id) == false)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card in an invalid state: {room.State}.");
                                return;
                            }

                            byte cardType = reader.ReadByte(); // 카드 타입
                            byte cardValue = reader.ReadByte(); // 카드 값

                            if (room.Cards[id].Contains(new Tuple<byte, byte>(cardType, cardValue)) == false)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card that does not exist in their hand: {cardType}:{cardValue}.");
                                return;
                            }
                            else
                            {
                                room.Cards[id].Remove(new Tuple<byte, byte>(cardType, cardValue)); //카드 삭제
                                Console.WriteLine($"[Special Rule] User {user.Id} exchanged card with deck. Removed card: {cardType}:{cardValue}.");
                                var newCard = CreateRandomCards(1); // 카드 1장 생성
                                room.Cards[id].Add(newCard[0]); // 새 카드 추가
                                Console.WriteLine($"[Special Rule] User {user.Id} exchanged card with deck. New card: {newCard[0].Item1}:{newCard[0].Item2}.");

                                foreach(var rp in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.S2UResponse_ExhangeCardWithDeckInSpecialRule);
                                    PacketWriter.WriteInt(id);
                                    PacketWriter.WriteByte(newCard[0].Item1); // 새 카드 타입
                                    PacketWriter.WriteByte(newCard[0].Item2); // 새 카드 값
                                    SendPacket(rp);
                                }
                            }
                        }
                    }
                    break;

                case ePacketType.U2SRequest_ExhangeCardWithOpponentInSpecialRule:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        byte cardType = reader.ReadByte(); // 카드 타입
                        byte cardValue = reader.ReadByte(); // 카드 값

                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if (room.NowSpecialRulePlayers.Contains(id) == false)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                                return;
                            }

                            room.Cards2ExchangeInSpecialRule[id] = new Tuple<byte, byte>(cardType, cardValue); // 교환할 카드 저장
                            int opponentID = -1;
                            if (room.NowSpecialRulePlayers[0] == id)
                                opponentID = room.NowSpecialRulePlayers[1];
                            else if (room.NowSpecialRulePlayers[1] == id)
                                opponentID = room.NowSpecialRulePlayers[0];
                            else
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                                return;
                            }

                            foreach(var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType.S2UAsk_ExhangeCardWithOpponentInSpecialRule);
                                PacketWriter.WriteInt(id); // 요청한 유저 ID
                                PacketWriter.WriteInt(opponentID); // 상대 유저 ID
                                SendPacket(pl);
                            }
                        }
                    }
                    break;
                case ePacketType.S2UResponse_WillAcceptExhangeCardWithOpponentISR:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        bool accept = reader.ReadBool(); // 상대가 카드 교환 요청을 수락했는지 여부

                        if(Rooms.TryGetValue(roomID, out var room))
                        {
                            int requester = -1;
                            if (room.NowSpecialRulePlayers[0] == id)
                                requester = room.NowSpecialRulePlayers[1];
                            else if (room.NowSpecialRulePlayers[1] == id)
                                requester = room.NowSpecialRulePlayers[0];
                            else
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to respond to card exchange request in an invalid state: {room.State}.");
                                return;
                            }
                            if (accept)
                            {
                                foreach(var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_IsOpponentAcceptedCardExchangeISR);
                                    PacketWriter.WriteInt(requester); // 요청한 유저 ID
                                    PacketWriter.WriteInt(id); // 요청한 유저 ID
                                    PacketWriter.WriteBool(true); // 카드 교환 수락
                                    SendPacket(pl);
                                }
                            }
                            else
                            {
                                foreach (var pl in room.Players)
                                {
                                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_IsOpponentAcceptedCardExchangeISR);
                                    PacketWriter.WriteInt(requester); // 요청한 유저 ID
                                    PacketWriter.WriteInt(id); // 요청한 유저 ID
                                    PacketWriter.WriteBool(false); // 카드 교환 거절
                                    SendPacket(pl);
                                }
                                Console.WriteLine($"[Special Rule] User {user.Id} rejected the card exchange request.");
                            }
                        }
                    }
                    break;
                case ePacketType.U2SRequest_OpponentSelectedCardToExhangeISR:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        byte cardType = reader.ReadByte(); // 카드 타입
                        byte cardValue = reader.ReadByte(); // 카드 값
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if (room.NowSpecialRulePlayers.Contains(id) == false)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                                return;
                            }
                            room.Cards2ExchangeInSpecialRule[id] = new Tuple<byte, byte>(cardType, cardValue); // 상대가 교환할 카드 저장

                            int opponentID = -1;
                            if (room.NowSpecialRulePlayers[0] == id)
                                opponentID = room.NowSpecialRulePlayers[1];
                            else if (room.NowSpecialRulePlayers[1] == id)
                                opponentID = room.NowSpecialRulePlayers[0];
                            else
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                                return;
                            }


                            foreach (var pl in room.Players)
                            {
                                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_ExhangeWithOpponentInSpecialRuleResult);
                                PacketWriter.WriteInt(id); // 요청한 유저 ID
                                if(room.Cards2ExchangeInSpecialRule.TryGetValue(id, out var card))
                                {
                                    PacketWriter.WriteByte(card.Item1); // 교환할 카드 타입
                                    PacketWriter.WriteByte(card.Item2); // 교환할 카드 값
                                }
                                else
                                {
                                    PacketWriter.WriteByte(0); // 기본값
                                    PacketWriter.WriteByte(0); // 기본값
                                    //에러 출력
                                    Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}. User's card not found.");
                                }
                                PacketWriter.WriteInt(opponentID); // 상대 유저 ID
                                if (room.Cards2ExchangeInSpecialRule.TryGetValue(opponentID, out var opponentCard))
                                {
                                    PacketWriter.WriteByte(opponentCard.Item1); // 상대가 교환할 카드 타입
                                    PacketWriter.WriteByte(opponentCard.Item2); // 상대가 교환할 카드 값
                                }
                                else
                                {
                                    PacketWriter.WriteByte(0); // 기본값
                                    PacketWriter.WriteByte(0); // 기본값
                                    //에러 출력
                                    Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}. Opponent's card not found.");
                                }
                                SendPacket(pl);
                            }
                        }
                    }
                    break;
                case ePacketType.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule:
                    {
                        int id = reader.ReadInt();
                        int roomID = reader.ReadInt();
                        if (Rooms.TryGetValue(roomID, out var room))
                        {
                            if (room.NowSpecialRulePlayers.Contains(id) == false)
                            {
                                Console.WriteLine($"[Warning] User {user.Id} tried to exchange card with opponent in an invalid state: {room.State}.");
                                return;
                            }
                            room.PlayerCountWhoAcceptedExchangeInSpecialRule += 1;

                            if (room.PlayerCountWhoAcceptedExchangeInSpecialRule == 0)
                            {
                                //모든 유저가 카드 교환을 완료했음
                                Console.WriteLine($"[Special Rule] All players have successfully exchanged cards with their opponents.");


                                //실제로 카드 변경
                                int player1 = room.NowSpecialRulePlayers[0];
                                int player2 = room.NowSpecialRulePlayers[1];

                                var player1Card = room.Cards2ExchangeInSpecialRule[player1];
                                var player2Card = room.Cards2ExchangeInSpecialRule[player2];

                                room.Cards[player1].Remove(player1Card);
                                room.Cards[player1].Add(player2Card); // 플레이어 1의 카드에 플레이어 2의 카드를 추가

                                room.Cards[player2].Remove(player2Card);
                                room.Cards[player2].Add(player1Card); // 플레이어 2의 카드에 플레이어 1의 카드를 추가
                                room.PlayerCountWhoAcceptedExchangeInSpecialRule = 0;
                            }
                        }
                    }
                    break;
                default:
                    Console.WriteLine($"[Warning] Unknown packet type received from User {user.Id}. PacketByte {pac}");
                    break;
            }
        }
        int CalculateSpecialRuleLoser(Room room)
        {
            int s1 = CalculateScore(room.Cards[room.NowSpecialRulePlayers[0]]);
            int s2 = CalculateScore(room.Cards[room.NowSpecialRulePlayers[1]]);
            //가지고 있는 카드를 먼저 출력
            Console.WriteLine($"[Special Rule] Player {room.NowSpecialRulePlayers[0]} cards: {string.Join(", ", room.Cards[room.NowSpecialRulePlayers[0]].Select(c => $"{c.Item1}:{c.Item2}"))}");
            Console.WriteLine($"[Special Rule] Player {room.NowSpecialRulePlayers[1]} cards: {string.Join(", ", room.Cards[room.NowSpecialRulePlayers[1]].Select(c => $"{c.Item1}:{c.Item2}"))}");
            Console.WriteLine($"[Special Rule] Player {room.NowSpecialRulePlayers[0]} score: {s1}, Player {room.NowSpecialRulePlayers[1]} score: {s2}.");
            if (s1 < s2)
                return room.NowSpecialRulePlayers[0];
            else if (s1 > s2)
                return room.NowSpecialRulePlayers[1];
            else
                return -1; // 무승부
        }
        void SendStartSpecialRule(Room room, int p1, int p2, byte reason)
        {
            var cards = CreateRandomCards(room.Mode == eRoomGameMode.TwoCards ? 4 : 6);
            room.GoPlayerCountInSpecialRule = 0;
            room.NowSpecialRulePlayers.Clear();
            room.NowSpecialRulePlayers.Add(p1);
            room.NowSpecialRulePlayers.Add(p2);
            room.Cards.Clear();
            if (room.Mode == eRoomGameMode.TwoCards)
            {
                room.Cards.Add(p1, new List<Tuple<byte, byte>> { cards[0], cards[1] });
                room.Cards.Add(p2, new List<Tuple<byte, byte>> { cards[2], cards[3] });
            }
            else
            {
                room.Cards.Add(p1, new List<Tuple<byte, byte>> { cards[0], cards[1], cards[2] });
                room.Cards.Add(p2, new List<Tuple<byte, byte>> { cards[3], cards[4], cards[5] });
            }
            //카드 값 출력
            Console.WriteLine($"[Special Rule] Player {p1} cards: {string.Join(", ", room.Cards[p1].Select(c => $"{c.Item1}:{c.Item2}"))}");
            Console.WriteLine($"[Special Rule] Player {p2} cards: {string.Join(", ", room.Cards[p2].Select(c => $"{c.Item1}:{c.Item2}"))}");
            //스페셜 룰
            //foreach (var pl in room.Players)
            {
                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_StartSpecialRule);
                PacketWriter.WriteByte(reason); //
                PacketWriter.WriteInt(p1);
                PacketWriter.WriteInt(p2);
                if (room.Mode == eRoomGameMode.TwoCards)
                {
                    PacketWriter.WriteByte(cards[0].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[0].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[1].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[1].Item2); // 카드 값
                }
                else
                {
                    PacketWriter.WriteByte(cards[0].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[0].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[1].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[1].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[2].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[2].Item2); // 카드 값
                }
                SendPacket(room.Players.Find((u) => u.Id == p1));

                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_StartSpecialRule);
                PacketWriter.WriteByte(reason); //
                PacketWriter.WriteInt(p2);
                PacketWriter.WriteInt(p1);
                if (room.Mode == eRoomGameMode.TwoCards)
                {
                    PacketWriter.WriteByte(cards[2].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[2].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[3].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[3].Item2); // 카드 값
                }
                else
                {
                    PacketWriter.WriteByte(cards[3].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[3].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[4].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[4].Item2); // 카드 값
                    PacketWriter.WriteByte(cards[5].Item1); // 카드 타입
                    PacketWriter.WriteByte(cards[5].Item2); // 카드 값
                }
                SendPacket(room.Players.Find((u) => u.Id == p2));


                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_StartSpecialRule);
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

            if(finalLoser != -1)
            {
                foreach (var pl in room.Players)
                {
                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_FinalResult);
                    PacketWriter.WriteInt(finalLoser);
                    SendPacket(pl);
                }
                return;
            }

            foreach (var pl in room.Players)
            {
                PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_StartNextRound);
                PacketWriter.WriteByte(reason); //0은 3명 다 인 & 동점인 경우
                SendPacket(pl);
            }
            Task.Delay(2000).ContinueWith((_) =>
            {
                if(room.Mode == eRoomGameMode.TwoCards)
                    DistributeRandom2CardsForUsers(room);


                room.State = eRoomState.WaitingForSuccessfulCardReception;
            });
        }
        bool CalculateResult(int roomID)
        {
            Room room;
            if (Rooms.TryGetValue(roomID, out room) == false)
                return false;

            int outPlayerCount = room.OutPlayers.Count;

            if (outPlayerCount == 3)
            {
                Console.WriteLine($"[Warning] All players are out in Room {roomID}. No further action taken.");
                return false;
            }

            if(outPlayerCount == 2)
            {
                SendStartSpecialRule(room, room.OutPlayers[0], room.OutPlayers[1], reason: 1); //아웃이 2명인 경우
                return false;
            }

            else if(outPlayerCount == 0)
            {
                //세명 전부 다 In
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


                if (playersWithLowersScore.Count == 1) //꼴지가 1명
                {
                    //1명만 패배
                    lowerID = playersWithLowersScore[0];
                    room.OutCounts[lowerID]--;
                    foreach (var pl in room.Players)
                    {
                        PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_LoserOfThisRound);
                        PacketWriter.WriteInt(lowerID);
                        PacketWriter.WriteByte(room.OutCounts[lowerID]);
                        SendPacket(pl);
                    }
                }
                else if (playersWithLowersScore.Count == 2) //꼴지가 2명
                {
                    //스페셜 룰

                    SendStartSpecialRule(room, playersWithLowersScore[0], playersWithLowersScore[1], reason: 2); //동점자 발생
                    return false;
                }
                else if (playersWithLowersScore.Count == 3) //꼴지가 3명
                {
                    StartNextRound(room, 1);
                }
            }
            else
            {
                //1명만 Out
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
                foreach(var player in room.InPlayers)
                {
                    var cards = room.Cards[player];
                    int score = CalculateScore(cards);
                    if(score == lowestScoreOfInPlayers)
                    {
                        playersInWithLowestScore.Add(player);
                    }
                }

                int outScore = CalculateScore(room.Cards[room.OutPlayers[0]]);

                if (outScore > lowestScoreOfInPlayers)
                {
                    //outPlayer가 패배
                    room.OutCounts[room.OutPlayers[0]]--;
                    foreach (var pl in room.Players)
                    {
                        PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_LoserOfThisRound);
                        PacketWriter.WriteInt(room.OutPlayers[0]);
                        PacketWriter.WriteByte(room.OutCounts[room.OutPlayers[0]]);
                        SendPacket(pl);
                    }
                }
                else if(outScore == lowerID)
                {
                    //스페셜 룰
                    if (playersInWithLowestScore.Count == 2) //최저 족보가 2명
                    {
                        SendStartSpecialRule(room, playersInWithLowestScore[0], playersInWithLowestScore[1], reason: 3); //동점자 발생
                        return false;
                    }
                    else if(playersInWithLowestScore.Count == 1) //최저 족보가 1명
                    {
                        //최저 족보가 패배
                        room.OutCounts[playersInWithLowestScore[0]]--;
                        foreach (var pl in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_LoserOfThisRound);
                            PacketWriter.WriteInt(playersInWithLowestScore[0]);
                            PacketWriter.WriteByte(room.OutCounts[playersInWithLowestScore[0]]);
                            SendPacket(pl);
                        }
                    }
                }
                else
                {
                    if(playersInWithLowestScore.Count == 1)
                    {
                        //최저 족보가 패배
                        room.OutCounts[playersInWithLowestScore[0]]--;
                        foreach (var pl in room.Players)
                        {
                            PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_LoserOfThisRound);
                            PacketWriter.WriteInt(playersInWithLowestScore[0]);
                            PacketWriter.WriteByte(room.OutCounts[playersInWithLowestScore[0]]);
                            SendPacket(pl);
                        }
                    }
                    else if(playersInWithLowestScore.Count == 2)
                    {
                        SendStartSpecialRule(room, playersInWithLowestScore[0], playersInWithLowestScore[1], reason: 4); // 동점자 발생
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("[Warning] Unexpected case in CalculateResult: More than 2 players with the lowest score.");
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
            if (card1.Item2 == card2.Item2) //같은 숫자
            {
                Console.WriteLine($"[Special Rule] Pair found: {card1.Item1}:{card1.Item2} and {card2.Item1}:{card2.Item2}.");
                return 100 + card1.Item2; // Special case for pairs
            }
            else
            {
                Console.WriteLine($"[Special Rule] Normal case: {card1.Item1}:{card1.Item2} and {card2.Item1}:{card2.Item2}.");
                return (card1.Item2 + card2.Item2 + 2) % 10; // Normal case
            }
        }
        private void SendIOResultAllPlayers(Room room, bool isFinal)
        {
            foreach (var rp in room.Players)
            {
                if(isFinal == true)
                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_InOutFinalResult);
                else
                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_InOutRoundResult);
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
            //매칭
            if(WaitingUsers_TwoMode.Count >= 3)
            {
                var room = new Room();
                room.Players = WaitingUsers_TwoMode.Take(3).ToList();
                WaitingUsers_TwoMode.RemoveRange(0, 3);
                Rooms.Add(room.GetHashCode(), room);

                List<int> userIDs = new List<int>();
                foreach(var user in room.Players)
                {
                    userIDs.Add(user.Id);
                    room.OutCounts[user.Id] = 3;
                }
                foreach(var user in room.Players)
                {
                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_StartGame);
                    PacketWriter.WriteInt(room.GetHashCode());
                    for(int i = 0; i < userIDs.Count; i++)
                    {
                        PacketWriter.WriteInt(userIDs[i]);
                    }
                    SendPacket(user);
                }
                Console.WriteLine($"[Game Start] A new 2-player game has started with Users: {string.Join(", ", room.Players.Select(u => u.Id))}.");


                room.State = eRoomState.ShouldDistributeCards;
                DistributeRandom2CardsForUsers(room);
                room.State = eRoomState.WaitingForSuccessfulCardReception;
            }
        }

        public void StartRPS(Room room)
        {
            bool test = false;
            test = true;
            if (test == true)
            {
                room.RPSFirst = 0;
                room.RPSSecond = 1;
                room.RPSThird = 2;
                foreach (var p in room.Players)
                {
                    PacketWriter.CreateNewPacket((byte)ePacketType.Broadcast_RPSFinalResult);

                    PacketWriter.WriteByte(room.NowRPSRoundCounter);
                    // 순서대로 ID 리스트 전송
                    PacketWriter.WriteInt((int)(room.RPSFirst));  // 1등
                    PacketWriter.WriteInt((int)(room.RPSSecond));  // 2등
                    PacketWriter.WriteInt((int)(room.RPSThird));  // 3등

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
                PacketWriter.CreateNewPacket((byte)ePacketType.S2URequest_RPSSelection);
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
                var card2 = GetRandomCard();
                room.Cards.Add(player.Id, new List<Tuple<byte, byte>> { card1.ToTuple(), card2.ToTuple() });
                PacketWriter.CreateNewPacket((byte)InGameServer.ePacketType.DistributeCardsFromServer);
                PacketWriter.WriteByte(card1.Item1);
                PacketWriter.WriteByte(card1.Item2);
                PacketWriter.WriteByte(card2.Item1);
                PacketWriter.WriteByte(card2.Item2);
                SendPacket(player);
                Console.WriteLine($"Distributing random cards to User {player.Id}.");
            }
        }
    }
}
