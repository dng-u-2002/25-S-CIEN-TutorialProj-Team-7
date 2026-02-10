using ExitGames.Client.Photon;
using Helpers;
using Photon.Pun;
using System.Collections.Generic;
using static InGameServer_PUN;
using UnityEngine;
using static Card;
using System;

[System.Serializable]
public class Bot
{
    User ServerPeer
    {
        get
        {
            if (PhotonNetwork.InRoom == false || PhotonNetwork.CurrentRoom == null)
                return null;
            foreach (var u in Photon.Pun.PhotonNetwork.CurrentRoom.Players)
            {
                if (u.Value.ActorNumber == Photon.Pun.PhotonNetwork.CurrentRoom.MasterClientId)
                {
                    return new User(u.Value);
                }
            }
            return null;
        }
    }
    NetworkDataWriter_PUN Writer = new NetworkDataWriter_PUN();
    List<Card> NowCards = new List<Card>();
    List<Tuple<int, eIO>> IOResults = new List<Tuple<int, eIO>>();
    eRPS LastRPSChoice = eRPS.None;
    bool IsDoingSomethingISR = false;
    public void OnEvent(byte packetType, List<object> evtData, InGameServer_PUN.User user)
    {
        var packet = (ePacketType_InGameServer)packetType;

        NetworkDataReader_PUN reader = new NetworkDataReader_PUN(evtData.ToArray());
        var bot = InGameManager.Instance.FindRemotePlayerById(user.Id); //여기서 뜨는 Warning은 일단 무시해도 됨
        switch (packet)
        {
            case ePacketType_InGameServer.DistributeCardsFromServer:
                NowCards.Clear();
                if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                {
                    //type : byte, value : byte
                    // * 2
                    byte cardType1 = reader.ReadByte();
                    byte cardValue1 = reader.ReadByte();
                    byte cardType2 = reader.ReadByte();
                    byte cardValue2 = reader.ReadByte();
                    var c1 = new Card((Card.CardType)cardType1, cardValue1);
                    var c2 = new Card((Card.CardType)cardType2, cardValue2);
                    NowCards.Add(c1);
                    NowCards.Add(c2);
                }
                else if (InGameManager.Instance.Mode == eGameMode.ThreeCards)
                {
                    //type : byte, value : byte
                    // * 3
                    byte cardType1 = reader.ReadByte();
                    byte cardValue1 = reader.ReadByte();
                    byte cardType2 = reader.ReadByte();
                    byte cardValue2 = reader.ReadByte();
                    byte cardType3 = reader.ReadByte();
                    byte cardValue3 = reader.ReadByte();
                    var c1 = new Card((Card.CardType)cardType1, cardValue1);
                    var c2 = new Card((Card.CardType)cardType2, cardValue2);
                    var c3 = new Card((Card.CardType)cardType3, cardValue3);
                    NowCards.Add(c1);
                    NowCards.Add(c2);
                    NowCards.Add(c3);
                }

                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedCards);
                Writer.WriteInt(user.Id);
                Writer.WriteInt(InGameManager.Instance.RoomID);
                Writer.SendPacket(ServerPeer);
                break;
            case ePacketType_InGameServer.S2URequest_RPSSelection:
                {
                    LastRPSChoice = eRPS.None;
                    float delay = UnityEngine.Random.Range(0, 2.0f);
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        SendRandomRPSChoice(user.Id);
                    }, delay);
                }
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

                    if (bot.IsOrderDetermined == false)
                    {
                        int botOrder = -1;
                        if (firstPlayerId == user.Id)
                            botOrder = 2;
                        else if (secondPlayerId == user.Id)
                            botOrder = 1;
                        else if (thirdPlayerId == user.Id)
                            botOrder = 0;

                        if (botOrder == -1)
                            bot.IsOrderDetermined = false;
                        else
                        {
                            bot.Order = botOrder;
                            bot.IsOrderDetermined = true;
                        }
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_RPSStartRematch:
                {
                    int playersCount2Rematch = reader.ReadByte();
                    List<int> players2Rematch = new List<int>();
                    for (int i = 0; i < playersCount2Rematch; i++)
                        players2Rematch.Add(reader.ReadInt());
                    if (players2Rematch.Contains(user.Id) == true)
                    {
                        float delay = UnityEngine.Random.Range(0, 2.0f);
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            SendRandomRPSChoice(user.Id);
                        }, delay);
                    }
                    else
                    {
                        bot.IsOrderDetermined = true;
                    }
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectCard2Delete:
                {
                    float delay = 3.5f + UnityEngine.Random.Range(0, 2.0f);
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2Delete);
                        Writer.WriteInt(user.Id);
                        Writer.WriteInt(InGameManager.Instance.RoomID);

                        int deleteIndex = UnityEngine.Random.Range(0, NowCards.Count);
                        Card deleteCard = NowCards[deleteIndex];
                        NowCards.RemoveAt(deleteIndex);

                        //선택한 카드를 패킷에 포함
                        Writer.WriteByte((byte)deleteCard.Type);
                        Writer.WriteByte(deleteCard.Value);
                        Writer.SendPacket(ServerPeer);
                    }, delay);
                }
                break;
            case ePacketType_InGameServer.Broadcast_RPSFinalResult:
                {
                    int round = reader.ReadByte();
                    int firstPlayerId = reader.ReadInt();
                    int secondPlayerId = reader.ReadInt();
                    int thirdPlayerId = reader.ReadInt();

                    int botOrder = -1;
                    if (firstPlayerId == user.Id)
                        botOrder = 2;
                    else if (secondPlayerId == user.Id)
                        botOrder = 1;
                    else if (thirdPlayerId == user.Id)
                        botOrder = 0;
                    bot.Order = botOrder;

                    Debug.Log($"Final Order of Bot (ID: {user.Id}): {bot.Order}");
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedOrders);
                    Writer.WriteInt(user.Id);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.WriteByte((byte)botOrder);
                    Writer.SendPacket(ServerPeer);
                }
                break;
            case ePacketType_InGameServer.Broadcast_InOutRoundResult:
                {
                    bool isInPlayerEnd = false;
                    byte length = reader.ReadByte();
                    IOResults.Clear();
                    while (IOResults.Count < length)
                    {
                        int i = reader.ReadInt();
                        if (i == -1) //-1을 기준으로 In/Out을 구분함
                        {
                            //이전까지는 In 플레이어, 이후부터는 Out 플레이어
                            if (isInPlayerEnd == true)
                            {
                                Debug.LogError("[InOut] Received -1 but already in player end state.");
                                return;
                            }
                            isInPlayerEnd = true;
                            continue;
                        }

                        if (isInPlayerEnd == false)
                            IOResults.Add(new Tuple<int, eIO>(i, eIO.In));
                        else
                            IOResults.Add(new Tuple<int, eIO>(i, eIO.Out));
                    }

                    //Debug.Log($"InOut Round Result - Ins: {string.Join(", ", ins)}, Outs: {string.Join(", ", outs)}");
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_First:
                //3명의 플레이어 모두 자신이 받은 Order와와 서버의 Order가 동일할 때, 이 패킷이 날라옴.
                {
                    if (bot.Order != 0)
                        return;
                    List<bool> ios = new List<bool>(); //내가 1번이면 아무것도 없음
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        SendRandomIOChoice(bot.Order, user.Id);
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Second:
                {
                    if (bot.Order != 1)
                        return;
                    List<bool> ios = new List<bool>(); //내가 2번이면 앞에 하나
                    //ios.Add
                    DelayedFunctionHelper.InvokeDelayed( () =>
                    {
                        SendRandomIOChoice(bot.Order, user.Id);
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Third:
                {
                    if (bot.Order != 2)
                        return;
                    List<bool> ios = new List<bool>(); //내가 3번이면 앞에 둘
                    //ios.Add
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        SendRandomIOChoice(bot.Order, user.Id);
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartSpecialRule:
                {
                    NowCards.Clear();
                    byte reason = reader.ReadByte();
                    int id = reader.ReadInt();
                    int opponentId = reader.ReadInt();

                    if (id != user.Id && opponentId != user.Id)
                        break;

                    var card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    var card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    NowCards.Add(card1); NowCards.Add(card2);

                    if (InGameManager.Instance.Mode == eGameMode.ThreeCards)
                    {
                        var card3 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        NowCards.Add(card3);
                    }

                    float additionalDelay = 0.0f;
                    if(InGameManager.Instance.Mode == eGameMode.ThreeCards)
                    {
                        additionalDelay = 5.5f;
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2DeleteISR);
                            Writer.WriteInt(user.Id);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            int deleteIndex = UnityEngine.Random.Range(0, NowCards.Count);
                            Card deleteCard = NowCards[deleteIndex];
                            NowCards.RemoveAt(deleteIndex);

                            //선택한 카드를 패킷에 포함
                            Writer.WriteByte((byte)deleteCard.Type);
                            Writer.WriteByte(deleteCard.Value);
                            Writer.SendPacket(ServerPeer);
                        }, 5.5f);
                    }
                    bool isFightingBot = false;
                    var bots = UnityEngine.GameObject.FindFirstObjectByType<InGameServer_PUN>().GetUsers();
                    foreach (var p in bots)
                    {
                        if (p.IsBot == true && p.Id != id)
                        {
                            if (p.Id == opponentId)
                                isFightingBot = true;
                        }
                    }
                    if(isFightingBot == false)
                    {
                        additionalDelay += UnityEngine.Random.Range(1.5f, 3.0f);
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            if (IsDoingSomethingISR == false)
                            {
                                int nowScore = InGameServer_PUN.CalculateScore(NowCards);
                                if ((UnityEngine.Random.Range(0, 100) < ConvertScore2ExchangeWithDeckProability(nowScore)))
                                {
                                    //카드 교환 요청
                                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithDeckInSpecialRule);
                                    Writer.WriteInt(user.Id);
                                    Writer.WriteInt(InGameManager.Instance.RoomID);
                                    int deleteIndex = UnityEngine.Random.Range(0, NowCards.Count);
                                    Card deleteCard = NowCards[deleteIndex];
                                    NowCards.RemoveAt(deleteIndex);

                                    Writer.WriteByte((byte)deleteCard.Type);
                                    Writer.WriteByte(deleteCard.Value);
                                    Writer.SendPacket(ServerPeer);
                                }
                            }
                        }, additionalDelay);
                    }

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.GoInSpecialRule);
                        Writer.WriteInt(user.Id);
                        Writer.WriteInt(InGameManager.Instance.RoomID);
                        Writer.SendPacket(ServerPeer);
                    }, additionalDelay + UnityEngine.Random.Range(2.1f, 4.0f));
                }
                break;
            case ePacketType_InGameServer.S2UAsk_ExchangeCardWithOpponentInSpecialRule:
                {
                    int asker = reader.ReadInt();
                    int asked = reader.ReadInt();
                    if (asked != user.Id)
                    {
                        //내가 요청받은게 아님
                        return;
                    }
                    IsDoingSomethingISR = true;
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        bool accept = (UnityEngine.Random.Range(0, 2) == 0) ? true : false;
                        accept = true; //반드시 수락
                        if (accept)
                        {
                            //수락
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExchangeCardWithOpponentISR);
                            Writer.WriteInt(user.Id);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(true); //수락
                            Writer.SendPacket(ServerPeer);

                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_OpponentSelectedCardToExchangeISR);
                                Writer.WriteInt(user.Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);

                                int deleteIndex = UnityEngine.Random.Range(0, NowCards.Count);
                                Card deleteCard = NowCards[deleteIndex];
                                //NowCards.RemoveAt(deleteIndex); 여기서 말고 최종에서 지움

                                Writer.WriteByte((byte)deleteCard.Type);
                                Writer.WriteByte((byte)deleteCard.Value);
                                Writer.SendPacket(ServerPeer);
                                //IsDoingSomethingISR = false; //이건 카드 교환 결과에서 처리함
                            }, 2.0f);
                        }
                        else
                        {
                            //거절
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExchangeCardWithOpponentISR);
                            Writer.WriteInt(user.Id);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(false); //거절
                            Writer.SendPacket(ServerPeer);
                            IsDoingSomethingISR = false;
                        }
                    }, 1.8f);
                }
                break;

            case ePacketType_InGameServer.Broadcast_ExchangeWithOpponentInSpecialRuleResult:
                {
                    int id1 = reader.ReadInt();
                    byte type1 = reader.ReadByte();
                    byte value1 = reader.ReadByte();
                    int id2 = reader.ReadInt();
                    byte type2 = reader.ReadByte();
                    byte value2 = reader.ReadByte();

                    Card card1 = new Card((Card.CardType)type1, value1);
                    Card card2 = new Card((Card.CardType)type2, value2);

                    int localId = user.Id;

                    // 내 교환이 아니면(=관전 등) 원래 코드도 아무것도 안 했으므로 그대로 탈출
                    if (id1 != localId && id2 != localId)
                        break;


                    Card.CardType outType; byte outValue;
                    Card localInCard; int opponentId;

                    if (id1 == localId)
                    {
                        // 내가 card1을 넘기고 card2를 받는다
                        outType = card1.Type;
                        outValue = card1.Value;
                        localInCard = card2;
                        opponentId = id2;
                    }
                    else
                    {
                        // 내가 card2를 넘기고 card1을 받는다
                        outType = card2.Type;
                        outValue = card2.Value;
                        localInCard = card1;
                        opponentId = id1;
                    }

                    NowCards.Remove(NowCards.Find((c) => c.Type == outType && c.Value == outValue));
                    NowCards.Add(new Card(localInCard.Type, localInCard.Value));

                    IsDoingSomethingISR = false;
                }
                break;
        }
    }
    int ConvertScore2ExchangeWithDeckProability(int score)
    {
        if (score <= 1) return 100;
        if (score <= 4) return 90;
        if (score <= 6) return 80;
        if (score <= 8) return 70;
        if (score <= 9) return 60;
        
        {
            if (score <= 101) return 50;
            if (score <= 103) return 40;
            if (score == 104) return 30;
            if (score == 105) return 20;
            if (score <= 109) return 10;
            return 0;
        }
    }
    int ConvertScore2InProability(int score)
    {
        if(score >= 100)
        {
            if (score >= 107) //8별
                return 90;
            if (score >= 102) //3별
                return 80;
            return 70;
        }
        else
        {
            if (score >= 7) //7달
                return 60;
            if (score >= 4) //4달
                return 50;
            if (score == 3) //3달
                return 60;
            return 70;
        }
    }
    void SendRandomIOChoice(int order, int id)
    {
        int myScore = InGameServer_PUN.CalculateScore(NowCards);

        eIO selection = eIO.In;
        int inProbability = ConvertScore2InProability(myScore);
        selection = UnityEngine.Random.Range(0, 100) < inProbability ? eIO.In : eIO.Out;


        if(order == 2)
        {
            if (IOResults[0].Item2 == eIO.Out && IOResults[1].Item2 == eIO.Out)
            {
                selection = eIO.In;
            }
        }

        if (order == 0)
            Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_First);
        else if (order == 1)
            Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Second);
        else if (order == 2)
            Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Third);
        Writer.WriteInt(id);
        Writer.WriteInt(InGameManager.Instance.RoomID);
        Writer.WriteByte((byte)order);
        Writer.WriteByte((byte)selection);
        
        Writer.SendPacket(ServerPeer);
    }
    void SendRandomRPSChoice(int id)
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_RPSSelection);
        Writer.WriteInt(id);
        Writer.WriteInt(InGameManager.Instance.RoomID);
        int rpsChoice = UnityEngine.Random.Range(0, 3); // 0: Rock, 1: Paper, 2: Scissors
        var bots = UnityEngine.GameObject.FindFirstObjectByType<InGameServer_PUN>().GetUsers();
        foreach (var p in bots)
        {
            if (p.IsBot == true && p.Id != id && p.BotInstance.LastRPSChoice != eRPS.None)
            {
                List<int> options = new List<int>(new int[3] { 0, 1, 2 });
                options.RemoveAt((int)p.BotInstance.LastRPSChoice);
                rpsChoice = options[UnityEngine.Random.Range(0, 2)];
                break;
            }
        }
        LastRPSChoice = (eRPS)rpsChoice;
        Writer.WriteByte((byte)rpsChoice);
        Writer.SendPacket(ServerPeer);
    }
}