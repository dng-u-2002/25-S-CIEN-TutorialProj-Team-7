using Helpers;
using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VOYAGER_Server;
public enum ePacketType_InGameServer : byte
{
    None = 0,
    Error = 1,
    HandShake_S2U,
    HandShake_U2S,
    Chat_Send,
    Chat_Receive,

    Request_JoinGame,
    Response_JoinGame,


    Broadcast_StartGame,

    DistributeCardsFromServer,
    U2SResponse_SuccessfullyReceivedCards,


    S2URequest_SelectCard2Delete,
    U2SResponse_SelectCard2Delete, //유저가 삭제할 카드를 선택했을 때 서버에 전송하는 패킷
    Broadcast_SomeoneSelectedCard2Delete, //유저가 삭제할 카드를 선택했을 때 다른 유저들에게 전송하는 패킷
    Broadcast_ShowCards2Delete, //유저가 삭제할 카드를 선택했을 때 다른 유저들에게 전송하는 패킷(삭제할 카드 보여주기)



    S2URequest_RPSSelection,
    U2SResponse_RPSSelection,

    //Broadcast_ShowRPSRoundResult_Selection,
    //Broadcast_ShowRPSRoundResult_Order,

    Broadcast_RPSRoundResult,

    //Ready2RPSRematch,
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
    S2URequest_SelectCard2DeleteISR,
    U2SResponse_SelectCard2DeleteISR, //유저가 스페셜 룰에서 제거할 카드를 선택했을 때 서버에 전송하는 패킷
    Broadcast_SomeoneSelectedCard2RemoveISR, //유저가 스페셜 룰에서 제거할 카드를 선택했을 때 다른 유저들에게 전송하는 패킷
    Broadcast_ShowCards2DeleteISR, //유저가 스페셜 룰에서 제거할 카드를 선택했을 때 다른 유저들에게 전송하는 패킷(제거할 카드 보여주기)
    GoInSpecialRule,
    Broadcast_ShowSpecialRuleCards,
    Broadcast_LoserOfSpecialRule,

    Broadcast_StartNextRound,

    U2SRequest_ExchangeCardWithDeckInSpecialRule,
    S2UResponse_ExhangeCardWithDeckInSpecialRule,

    U2SRequest_ExhangeCardWithOpponentInSpecialRule,

    S2UAsk_ExhangeCardWithOpponentISR, // 상대와 카드 교환 요청
    S2UResponse_WillAcceptExhangeCardWithOpponentISR, // 상대가 카드 교환 요청에 응답
    Broadcast_IsOpponentAcceptedCardExchangeISR,

    U2SRequest_OpponentSelectedCardToExhangeISR, //상대가 카드를 골랐음
    Broadcast_ExhangeWithOpponentInSpecialRuleResult, //교환 결과
    U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule, //내가 교환한 카드가 덱에 추가됨

    Broadcast_FinalResult
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
                //My id in Server : int
                this.Id = reader.ReadInt();

                NetworkResponse_HandShake_S2U();

                Writer.CreateNewPacket((byte)ePacketType_InGameServer.HandShake_U2S);
                Writer.SendPacket(ServerPeer);
                Debug.Log("[Server Connection] Sent HandShake_U2S packet to server.");
                break;

            case ePacketType_InGameServer.Chat_Receive:
                //Id of Sender : int
                //Message : string
                {
                    int id = reader.ReadInt();
                    string message = reader.ReadString();
                    NetworkResponse_Chat_Receive(id, message);
                }
                break;

            case ePacketType_InGameServer.Response_JoinGame:
                Debug.Log("다른 플레이어를 기다리는 중...");
                break;

            case ePacketType_InGameServer.Broadcast_StartGame:
                // Id of Room : int
                // Ids of Users : List<int>
                int roomID = reader.ReadInt();
                List<int> users = new List<int>(new int[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt() });
                users.Remove(Id); //내건 지움
                NetworkResponse_Broadcast_StartGame(roomID, users);
                break;

            case ePacketType_InGameServer.DistributeCardsFromServer:
                if(InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                {
                    //type : byte, value : byte
                    // * 2
                    byte cardType1 = reader.ReadByte();
                    byte cardValue1 = reader.ReadByte();
                    byte cardType2 = reader.ReadByte();
                    byte cardValue2 = reader.ReadByte();
                    NetworkResponse_DistributeCardsFromServer_TwoCardsMode(cardType1, cardValue1, cardType2, cardValue2);
                }
                else if (InGameManager.Instance.Mode == InGameManager.eGameMode.ThreeCards)
                {
                    //type : byte, value : byte
                    // * 3
                    byte cardType1 = reader.ReadByte();
                    byte cardValue1 = reader.ReadByte();
                    byte cardType2 = reader.ReadByte();
                    byte cardValue2 = reader.ReadByte();
                    byte cardType3 = reader.ReadByte();
                    byte cardValue3 = reader.ReadByte();
                    NetworkResponse_DistributeCardsFromServer_ThreeCardsMode(cardType1, cardValue1, cardType2, cardValue2, cardType3, cardValue3);
                }

                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedCards);
                Writer.WriteInt(Id);
                Writer.WriteInt(InGameManager.Instance.RoomID);
                Writer.SendPacket(ServerPeer);
                break;
            case ePacketType_InGameServer.S2URequest_SelectCard2Delete:
                {
                    InGameManager.Instance.LocalPlayerUIDrawer.SelectCard2Delete((card) =>
                    {
                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2Delete);
                        Writer.WriteInt(Id);
                        Writer.WriteInt(InGameManager.Instance.RoomID);
                        Writer.WriteByte((byte)card.Type);
                        Writer.WriteByte(card.Value);
                        Writer.SendPacket(ServerPeer);


                        InGameManager.Instance.LocalPlayerUIDrawer.SelectedCard2Delete(card);
                    });
                }
                break;
            case ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2Delete:
                {
                    int playerId = reader.ReadInt();
                    if(playerId != InGameManager.Instance.LocalPlayer.ID)
                    {
                        InGameManager.Instance.SomeoneSelectedCard2Delete(playerId);
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_ShowCards2Delete:
                {
                    int id1 = reader.ReadInt();
                    Card c1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    int id2 = reader.ReadInt();
                    Card c2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    int id3 = reader.ReadInt();
                    Card c3 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    InGameManager.Instance.ShowCards2Delete(id1, c1, id2, c2, id3, c3);
                }
                break;
            case ePacketType_InGameServer.S2URequest_RPSSelection:
                if(InGameManager.Instance.Mode == InGameManager.eGameMode.ThreeCards)
                {
                    InGameManager.Instance.RemoveAllCard2Delete();
                }

                void SendMyRPSSelection2Server(eRPS rps)
                {
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_RPSSelection);
                    Writer.WriteInt(Id);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.WriteByte((byte)rps);
                    Writer.SendPacket(ServerPeer);
                }
                InGameManager.Instance.LocalPlayer.StartSelectRPS((rps) =>
                {
                    SendMyRPSSelection2Server(rps);
                });
                break;

            case ePacketType_InGameServer.Broadcast_RPSRoundResult:
                {
                    int round = reader.ReadByte();
                    int firstPlayerId = reader.ReadInt();
                    eRPS firstPlayerRPS = (eRPS)reader.ReadByte();
                    byte firstPlayerOrder = reader.ReadByte();
                    int secondPlayerId = reader.ReadInt();
                    eRPS secondPlayerRPS = (eRPS)reader.ReadByte();
                    byte secondPlayerOrder = reader.ReadByte();
                    int thirdPlayerId = reader.ReadInt();
                    eRPS thirdPlayerRPS = (eRPS)reader.ReadByte();
                    byte thirdPlayerOrder = reader.ReadByte();

                    InGameManager.Instance.ShowRPSRoundResult(round, firstPlayerId, firstPlayerRPS, firstPlayerOrder,
                        secondPlayerId, secondPlayerRPS, secondPlayerOrder,
                        thirdPlayerId, thirdPlayerRPS, thirdPlayerOrder);
                }
                break;
            //case ePacketType_InGameServer.Broadcast_ShowRPSRoundResult_Selection:
            //    {
            //        int nowRound = reader.ReadByte();

            //        int firstPlayerId = reader.ReadInt();
            //        eRPS firstPlayerRPS = (eRPS)reader.ReadByte();
            //        int secondPlayerId = reader.ReadInt();
            //        eRPS secondPlayerRPS = (eRPS)reader.ReadByte();
            //        int thirdPlayerId = reader.ReadInt();
            //        eRPS thirdPlayerRPS = (eRPS)reader.ReadByte();

            //        InGameManager.Instance.ShowRPSRoundResult_Selection(firstPlayerId, secondPlayerId, thirdPlayerId, firstPlayerRPS, secondPlayerRPS, thirdPlayerRPS);
            //    }
            //    break;
            //case ePacketType_InGameServer.Broadcast_ShowRPSRoundResult_Order:
            //    {
            //        int round = reader.ReadByte();
            //        int firstPlayerId = reader.ReadInt();
            //        int secondPlayerId = reader.ReadInt();
            //        int thirdPlayerId = reader.ReadInt();

            //        InGameManager.Instance.ShowRPSResult_Order_Accumulated(firstPlayerId, secondPlayerId, thirdPlayerId);
            //    }
            //    break;
            //case ePacketType_InGameServer.Ready2RPSRematch:
            //    {
            //        var LocalPlayerUIDrawer = FindObjectOfType<LocalPlayerUIDrawer>();
            //        LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
            //        LocalPlayerUIDrawer.ShowPanelOnScreenCenter("기다리는 중 2");
            //        InGameManager.Instance.LocalPlayer.IsOrderDetermined = true;
            //    }
            //    break;

            case ePacketType_InGameServer.Broadcast_RPSStartRematch:
                {
                    int playersCount2Rematch = reader.ReadByte();
                    List<int> players2Rematch = new List<int>();
                    for (int i = 0; i < playersCount2Rematch; i++)
                    {
                        players2Rematch.Add(reader.ReadInt());
                    }
                    if(players2Rematch.Contains(InGameManager.Instance.LocalPlayer.ID) == true) // 내가 리매치
                    {
                        InGameManager.Instance.LocalPlayer.StartSelectRPS((rps) =>
                        {
                            SendMyRPSSelection2Server(rps);
                        });
                    }
                    else
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("리매치에 참여하지 않습니다.\n다른 플레이어가 리매치 중입니다.");
                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
                        InGameManager.Instance.LocalPlayer.IsOrderDetermined = true;
                        return;
                    }
                }
                break;

            case ePacketType_InGameServer.Broadcast_RPSFinalResult:
                {
                    if (InGameManager.Instance.Mode == InGameManager.eGameMode.ThreeCards)
                    {
                        InGameManager.Instance.RemoveAllCard2Delete();
                    }
                    int round = reader.ReadByte();
                    int firstPlayerId = reader.ReadInt();
                    int secondPlayerId = reader.ReadInt();
                    int thirdPlayerId = reader.ReadInt();

                    InGameManager.Instance.SetRPSResult(firstPlayerId, secondPlayerId, thirdPlayerId);

                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedOrders);
                    Writer.WriteInt(Id);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.WriteByte((byte)InGameManager.Instance.LocalPlayer.Order);
                    Debug.Log(message: $"[RPS] Sending my order: {InGameManager.Instance.LocalPlayer.Order}");
                    Writer.SendPacket(ServerPeer);
                }
                break;

            case ePacketType_InGameServer.S2URequest_SelectInOut_First:
                {
                    if (InGameManager.Instance.LocalPlayer.Order != 0)
                        return;
                    NetworkResponse_SelectIO();
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Second:
                {
                    if (InGameManager.Instance.LocalPlayer.Order != 1)
                        return;
                    NetworkResponse_SelectIO();
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Third:
                {
                    if (InGameManager.Instance.LocalPlayer.Order != 2)
                        return;
                    NetworkResponse_SelectIO();
                }
                break;

            case ePacketType_InGameServer.Broadcast_InOutRoundResult:
                {
                    List<int> ins = new List<int>();
                    List<int> outs = new List<int>();
                    bool isInPlayerEnd = false;
                    byte length = reader.ReadByte();
                    while(ins.Count + outs.Count < length)
                    {
                        int i = reader.ReadInt();
                        if(i == -1)
                        {
                            isInPlayerEnd = true;
                            continue;
                        }
                        if(isInPlayerEnd == false)
                        {
                            ins.Add(i);
                        }
                        else
                        {
                            outs.Add(i);
                        }
                    }
                    Debug.Log($"[InOut] In Players: {string.Join(", ", ins)}, Out Players: {string.Join(", ", outs)}");
                    InGameManager.Instance.ShowIOResult(ins, outs, false);
                }
                break;
            case ePacketType_InGameServer.Broadcast_InOutFinalResult:
                {
                    List<int> ins = new List<int>();
                    List<int> outs = new List<int>();
                    bool isInPlayerEnd = false;
                    byte length = reader.ReadByte();
                    while (ins.Count + outs.Count < length)
                    {
                        int i = reader.ReadInt();
                        if (i == -1)
                        {
                            isInPlayerEnd = true;
                            continue;
                        }
                        if (isInPlayerEnd == false)
                        {
                            ins.Add(i);
                        }
                        else
                        {
                            outs.Add(i);
                        }
                    }
                    Debug.Log($"[InOut] In Players: {string.Join(", ", ins)}, Out Players: {string.Join(", ", outs)}");

                    InGameManager.Instance.ShowIOResult(ins, outs, true);
                }
                break;

            case ePacketType_InGameServer.Broadcast_SendAllPlayersCardData:
                {
                    Dictionary<int, List<Card>> allCards = new Dictionary<int, List<Card>>();
                    for (int i = 0; i < 3; i++)
                    {
                        int playerID = reader.ReadInt();
                        List<Card> cs = new List<Card>();
                        if(InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                        {
                            for (int c = 0; c < 2; c++)
                            {
                                byte type = reader.ReadByte();
                                byte value = reader.ReadByte();
                                cs.Add(new Card((Card.CardType)type, value));
                            }
                        }
                        else if (InGameManager.Instance.Mode == InGameManager.eGameMode.ThreeCards)
                        {
                            for (int c = 0; c < 2; c++)
                            {
                                byte type = reader.ReadByte();
                                byte value = reader.ReadByte();
                                cs.Add(new Card((Card.CardType)type, value));
                            }
                        }
                        allCards.Add(playerID, cs);
                    }
                    InGameManager.Instance.SetAllCards(allCards);
                }
                break;

            case ePacketType_InGameServer.Broadcast_ShowAllCards:
                {
                    InGameManager.Instance.ShowAllCardsByOrder();
                }
                break;

            case ePacketType_InGameServer.Broadcast_LoserOfThisRound:
                {
                    int loserId = reader.ReadInt();
                    byte outCount = reader.ReadByte();
                    InGameManager.Instance.ShowLoserOfThisRound(loserId, outCount);
                }
                break;
            case ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2RemoveISR:
                {
                    int playerId = reader.ReadInt();
                    if (playerId != InGameManager.Instance.LocalPlayer.ID)
                    {
                        InGameManager.Instance.SomeoneSelectedCard2Delete(playerId);
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_ShowCards2DeleteISR:
                {
                    int id1 = reader.ReadInt();
                    Card c1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    int id2 = reader.ReadInt();
                    Card c2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    InGameManager.Instance.ShowCards2Delete(id1, c1, id2, c2, -1, new Card(Card.CardType.Dummy, 100));

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.RemoveAllCard2Delete();
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartSpecialRule:
                {
                    byte reason = reader.ReadByte();
                    Debug.Log(reason);
                    if(reason == 10)//이전 스페셜 룰에서 동점자 발생
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("이전 스페셜 룰에서 동점자가 발생했습니다.\n새로운 스페셜 룰을 시작합니다.");
                    }

                    int id = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    if (id != InGameManager.Instance.LocalPlayer.ID)
                    {
                        //내가 스페셜 룰을 시작하는게 아님
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("스페셜 룰이 시작되었습니다.\n잠시 기다려주세요.");
                        IEnumerator S()
                        {
                            yield return new WaitForSeconds(2.0f);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                            List<Card> dummyCards = new List<Card>();
                            if (InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                            {
                                dummyCards.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards.Add(new Card(Card.CardType.Dummy, 100));
                            }
                            else
                            {
                                dummyCards.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards.Add(new Card(Card.CardType.Dummy, 100));
                            }
                            List<Card> dummyCards2 = new List<Card>();
                            if (InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                            {
                                dummyCards2.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards2.Add(new Card(Card.CardType.Dummy, 100));
                            }
                            else
                            {
                                dummyCards2.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards2.Add(new Card(Card.CardType.Dummy, 100));
                                dummyCards2.Add(new Card(Card.CardType.Dummy, 100));
                            }
                            InGameManager.Instance.StartSpecialRule(id, opponentId, dummyCards, dummyCards2,
                            () =>
                            {
                                //go
                            },
                            (card) =>
                            {
                                //exchange with deck
                            },
                            () =>
                            {

                            },
                            (card) =>
                            {
                                //exchange with opponent
                            });
                        }
                        StartCoroutine(S());
                    }
                    else
                    {
                        List<Card> myCards = new List<Card>();
                        if (InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                        {
                            var card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            myCards.Add(card1); myCards.Add(card2);
                        }
                        else
                        {
                            var card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card3 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            myCards.Add(card1); myCards.Add(card2); myCards.Add(card3);
                        }
                        List<Card> user2Cards = new List<Card>();
                        if (InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                        {
                            user2Cards.Add(new Card(Card.CardType.Dummy, 100));
                            user2Cards.Add(new Card(Card.CardType.Dummy, 100));
                        }
                        else
                        {
                            user2Cards.Add(new Card(Card.CardType.Dummy, 100));
                            user2Cards.Add(new Card(Card.CardType.Dummy, 100));
                            user2Cards.Add(new Card(Card.CardType.Dummy, 100));
                        }
                        //카드 타입-값 출력
                        IEnumerator S2()
                        {
                            yield return new WaitForSeconds(2.0f);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                            InGameManager.Instance.StartSpecialRule(id, opponentId, myCards, user2Cards,
                            () =>
                            {
                                //go
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.GoInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.SendPacket(ServerPeer);
                            },
                            (card) =>
                            {
                                //exchange with deck
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithDeckInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);

                                InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(card);
                                //InGameManager.Instance.LocalPlayerUIDrawer.Card2Exchange = null;
                            },
                            () =>
                            {
                                //교환 버튼을 누름
                                //Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule);
                                //Writer.WriteInt(Id);
                                //Writer.WriteInt(InGameManager.Instance.RoomID);
                                //Writer.SendPacket(ServerPeer);
                            },
                            (card) =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);
                                //exchange with opponent
                            });
                        }
                        IEnumerator S3()
                        {
                            yield return new WaitForSeconds(2.0f);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                            InGameManager.Instance.StartSpecialRule_ThreeCardMode((card2Delete) =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2DeleteISR);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card2Delete.Type);
                                Writer.WriteByte(card2Delete.Value);
                                Writer.SendPacket(ServerPeer);
                                InGameManager.Instance.LocalPlayerUIDrawer.SelectedCard2Delete(card2Delete);
                            },
                                id, opponentId, myCards, user2Cards,
                            () =>
                            {
                                //go
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.GoInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.SendPacket(ServerPeer);
                            },
                            (card) =>
                            {
                                //exchange with deck
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithDeckInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);

                                InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(card);
                                //InGameManager.Instance.LocalPlayerUIDrawer.Card2Exchange = null;
                            },
                            () =>
                            {
                                //교환 버튼을 누름
                                //Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule);
                                //Writer.WriteInt(Id);
                                //Writer.WriteInt(InGameManager.Instance.RoomID);
                                //Writer.SendPacket(ServerPeer);
                            },
                            (card) =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);
                                //exchange with opponent
                            });
                        }
                        if (InGameManager.Instance.Mode == InGameManager.eGameMode.TwoCards)
                        {
                            StartCoroutine(S2());
                        }
                        else if (InGameManager.Instance.Mode == InGameManager.eGameMode.ThreeCards)
                        {
                            StartCoroutine(S3());
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.S2UAsk_ExhangeCardWithOpponentISR:
                {
                    int asker = reader.ReadInt();
                    int myId = reader.ReadInt();
                    if(myId != InGameManager.Instance.LocalPlayer.ID)
                    {
                        //Debug.LogWarning(Equals(asker, InGameManager.Instance.LocalPlayer.ID) + " " + myId + " " + InGameManager.Instance.LocalPlayer.ID);
                        //내가 요청받은게 아님
                        return;
                    }

                    //내가 요청받은거임
                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenterWithButtons("상대가 카드 교환을 요청했습니다", "수락", "거절",
                        () =>
                        {
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExhangeCardWithOpponentISR);
                            Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(true); //수락
                            Writer.SendPacket(ServerPeer);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                            InGameManager.Instance.LocalPlayerUIDrawer.SelectCard2Exchange((card) =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_OpponentSelectedCardToExhangeISR);
                                Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte((byte)card.Value);
                                Writer.SendPacket(ServerPeer);
                            });
                        },
                        () =>
                        {
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExhangeCardWithOpponentISR);
                            Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(false); //거절
                            Writer.SendPacket(ServerPeer);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                        });
                }
                break;
            case ePacketType_InGameServer.Broadcast_IsOpponentAcceptedCardExchangeISR:
                {
                    int requesterID = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    bool isAccepted = reader.ReadBool();

                    if(InGameManager.Instance.LocalPlayer.ID == requesterID)
                    {

                        if (isAccepted)
                        {
                            //교환 수락
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter($"상대가 교환을 수락했습니다.\n카드 선택을 기다리는 중...");
                            foreach (var rp in InGameManager.Instance.RemotePlayerUIDrawers)
                            {
                                if (rp.Target.ID == opponentId)
                                {
                                    rp.SetIOText("교환 수락");
                                }
                            }
                        }
                        else
                        {
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter($"상대가 교환을 거절했습니다.");
                            foreach (var rp in InGameManager.Instance.RemotePlayerUIDrawers)
                            {
                                if (rp.Target.ID == opponentId)
                                {
                                    string originText = rp.GetIOText();
                                    rp.SetIOText("교환 거절");
                                    DelayedFunctionHelper.InvokeDelayed(() =>
                                    {
                                        rp.SetIOText(originText);
                                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                    }, 2.0f);
                                }
                            }
                            //교환 거절
                            //InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("상대가 교환을 거절했습니다.");
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.Broadcast_ExhangeWithOpponentInSpecialRuleResult:
                {
                    int id1 = reader.ReadInt();
                    byte type1 = reader.ReadByte();
                    byte value1 = reader.ReadByte();
                    int id2 = reader.ReadInt();
                    byte type2 = reader.ReadByte();
                    byte value2 = reader.ReadByte();
                    Card card1 = new Card((Card.CardType)type1, value1);
                    Card card2 = new Card((Card.CardType)type2, value2);

                    if(id1 == InGameManager.Instance.LocalPlayer.ID)
                    {
                        Card localCard = InGameManager.Instance.LocalPlayer.ThisDeck.GetCardByTypeAndValue(card1.Type, card1.Value);
                        if(localCard == null)
                        {
                            Debug.LogError($"[InGameUser] Local player does not have the card {card1.Type}-{card1.Value} to exchange.");
                            return;
                        }
                        InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(localCard);
                        //내가 교환한 카드
                        InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(card2);
                        card2.CardGameObject.SetFace(true);

                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule);
                        Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                        Writer.WriteInt(InGameManager.Instance.RoomID);
                        Writer.SendPacket(ServerPeer);

                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                    }
                    else if (id2 == InGameManager.Instance.LocalPlayer.ID)
                    {
                        Card localCard = InGameManager.Instance.LocalPlayer.ThisDeck.GetCardByTypeAndValue(card2.Type, card2.Value);
                        if (localCard == null)
                        {
                            Debug.LogError($"[InGameUser] Local player does not have the card {card2.Type}-{card2.Value} to exchange.");
                            return;
                        }
                        InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(localCard);
                        //상대가 교환한 카드
                        InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(card1);
                        card1.CardGameObject.SetFace(true);

                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule);
                        Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                        Writer.WriteInt(InGameManager.Instance.RoomID);
                        Writer.SendPacket(ServerPeer);

                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                    }

                }
                break;
            case ePacketType_InGameServer.S2UResponse_ExhangeCardWithDeckInSpecialRule:
                {
                    int id = reader.ReadInt();
                    if(id == InGameManager.Instance.LocalPlayer.ID)
                    {
                        //내가 요청한 교환임
                        Card card = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(card);
                        card.CardGameObject.SetFace(true);
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_ShowSpecialRuleCards:
                {
                    Dictionary<int, List<Card>> specialRuleCards = new Dictionary<int, List<Card>>();
                    for (int i = 0; i < 2; i++)
                    {
                        int id = reader.ReadInt();
                        Card card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        Card card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        List<Card> cards = new List<Card> { card1, card2 };
                        specialRuleCards[id] = cards;
                    }
                    InGameManager.Instance.SetAllCards(specialRuleCards);
                    InGameManager.Instance.ShowAllCards();
                }
                break;
            case ePacketType_InGameServer.Broadcast_LoserOfSpecialRule:
                {
                    int loser = reader.ReadInt();
                    byte outCount = reader.ReadByte();  
                    InGameManager.Instance.ShowLoserOfThisRound(loser, outCount);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartNextRound:
                {
                    byte reason = reader.ReadByte();
                    if(reason == 0) //전판이 3명 다 인 & 동점인 상황
                    {

                    }

                    InGameManager.Instance.StartNextRound(reason);
                }
                break;
            case ePacketType_InGameServer.Broadcast_FinalResult:
                {
                    int loserID = reader.ReadInt();
                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter($"Loser: {loserID}\n게임이 종료되었습니다.");
                }
                break;
        }
    }

    private void NetworkResponse_SelectIO()
    {
        if(InGameManager.Instance.LocalPlayer.Order == 2 && InGameManager.Instance.OutPlayerCount == 2)
        {
            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("2명이 먼저 OUT을 골랐습니다.\nIN을 선택합니다.");
            Task.Delay(2000).ContinueWith((_) =>
            {
                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Third);
                Writer.WriteInt(Id);
                Writer.WriteInt(InGameManager.Instance.RoomID);
                Writer.WriteByte((byte)InGameManager.Instance.LocalPlayer.Order);
                Writer.WriteByte((byte)eIO.In);

                Writer.SendPacket(ServerPeer);
            });
        }
        else
        {
            InGameManager.Instance.LocalPlayer.StartSelectIO((io) =>
            {
                if (InGameManager.Instance.LocalPlayer.Order == 0)
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_First);
                else if (InGameManager.Instance.LocalPlayer.Order == 1)
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Second);
                else if (InGameManager.Instance.LocalPlayer.Order == 2)
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Third);
                Writer.WriteInt(Id);
                Writer.WriteInt(InGameManager.Instance.RoomID);
                Writer.WriteByte((byte)InGameManager.Instance.LocalPlayer.Order);
                Writer.WriteByte((byte)io);

                Writer.SendPacket(ServerPeer);
            });
        }
    }

    private static void NetworkResponse_DistributeCardsFromServer_ThreeCardsMode(byte cardType1, byte cardValue1, byte cardType2, byte cardValue2, byte cardType3, byte cardValue3)
    {
        Card card1 = new Card((Card.CardType)cardType1, cardValue1);
        Card card2 = new Card((Card.CardType)cardType2, cardValue2);
        Card card3 = new Card((Card.CardType)cardType3, cardValue3);
        InGameManager.Instance.LocalPlayer.ThisDeck.AddCards(new Card[] { card1, card2, card3 });
        for (int i = 0; i < 3; i++)
        {
            InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(i).CardGameObject.SetFace(true);
        }
        foreach (var rp in InGameManager.Instance.RemotePlayers)
        {
            rp.ThisDeck.AddCards(new Card[] { new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100) });

            for (int i = 0; i < 3; i++)
            {
                rp.ThisDeck.GetCard(i).CardGameObject.SetFace(false);
            }
        }
    }

    private static void NetworkResponse_DistributeCardsFromServer_TwoCardsMode(byte cardType1, byte cardValue1, byte cardType2, byte cardValue2)
    {
        Card card1 = new Card((Card.CardType)cardType1, cardValue1);
        Card card2 = new Card((Card.CardType)cardType2, cardValue2);
        InGameManager.Instance.LocalPlayer.ThisDeck.AddCards(new Card[] { card1, card2 });
        for(int i = 0; i < 2; i++)
        {
            InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(i).CardGameObject.SetFace(true);
        }
        foreach (var rp in InGameManager.Instance.RemotePlayers)
        {
            rp.ThisDeck.AddCards(new Card[] { new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100) });

            for (int i = 0; i < 2; i++)
            {
                rp.ThisDeck.GetCard(i).CardGameObject.SetFace(false);
            }
        }
    }

    private void NetworkResponse_Broadcast_StartGame(int roomID, List<int> remotePlayers)
    {
        Debug.Log("게임이 시작되었습니다!");
        InGameManager.Instance.RoomID = roomID;
        InGameManager.Instance.StartGame();
        InGameManager.Instance.LocalPlayer.ID = Id;
        InGameManager.Instance.SetRemotePlayersID(remotePlayers);
    }

    private void NetworkResponse_Chat_Receive(int id, string message)
    {
        OnChatReceived?.Invoke(id, message);
        Debug.Log($"[Chat] Received message from server: {message}");
    }

    private void NetworkResponse_HandShake_S2U()
    {
        Debug.Log("[Server Connection] Received HandShake_S2U packet from server.");
    }

    public Action<int, string> OnChatReceived;

    private void Start()
    {
        ConnectToServer();
        Invoke("StartGame", 0.5f);
    }

    void StartGame()
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.Request_JoinGame);
        Writer.WriteInt(Id);
        Writer.WriteByte((byte)InGameManager.Instance.Mode);
        Writer.SendPacket(ServerPeer);
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(200);
        GUILayout.EndVertical();
        GUILayout.Label($"서버 IP: {serverIP}:{serverPort}");
        if (GUILayout.Button("게임 시작"))
        {
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
