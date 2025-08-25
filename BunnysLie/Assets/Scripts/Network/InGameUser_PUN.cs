using ExitGames.Client.Photon;
using Helpers;
using LiteNetLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor.VersionControl;
#endif
using UnityEngine;
using VOYAGER_Server;
using static InGameServer_PUN;

public class InGameUser_PUN : MonoBehaviourPunCallbacks, IOnEventCallback
{
    bool AutoStartGame = false;

    private void Start()
    {
//#if UNITY_EDITOR

        if(AutoStartGame)
        {
#if UNITY_EDITOR
            Photon.Pun.PhotonNetwork.NickName = ParrelSync.ClonesManager.GetArgument();
#else
            Photon.Pun.PhotonNetwork.NickName = "Player" + UnityEngine.Random.Range(1000, 9999).ToString();
#endif
            Debug.Log("NickName: " + Photon.Pun.PhotonNetwork.NickName);
            //#endif
            PhotonNetwork.GameVersion = "0.1.0";
            PhotonNetwork.AutomaticallySyncScene = false;

            // 고정 리전 지정 (둘 다 같은 값으로!)
            var app = PhotonNetwork.PhotonServerSettings.AppSettings;
            app.FixedRegion = "asia";      // 필요 시 "kr" 등 팀이 쓰는 실제 코드로 통일
                                           // UDP가 막힌 환경 회피 (선택)
            app.Protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;

            //Debugger.text += ($"[PHOTON] AppId prefix: {app.AppIdRealtime?.Substring(0, 6)}");
            //Debugger.text += ($"[PHOTON] GameVersion: {PhotonNetwork.GameVersion}");
            //Debugger.text += ($"[PHOTON] FixedRegion: {app.FixedRegion}");
            Debugger.text += ($"[PHOTON] CloudRegion(connected): {PhotonNetwork.CloudRegion}");
            Debugger.text += ($"[PHOTON] State: {PhotonNetwork.NetworkClientState}");
            PhotonNetwork.ConnectUsingSettings();
        }


        //        Photon.Pun.PhotonNetwork.ConnectUsingSettings();
        else
        {
            InGameManager.Instance.Mode = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("GameMode", out var mode) ? (eGameMode)(int)mode : eGameMode.TwoCards;
        }

        Writer = new NetworkDataWriter_PUN();
    }
    [SerializeField] TMP_Text Debugger;
    int RoundCounter;
    [SerializeField] AudioSource CardDistributionSound;

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        if(AutoStartGame)
        {
            DelayedFunctionHelper.InvokeDelayed(() =>
            {
                Photon.Pun.PhotonNetwork.JoinOrCreateRoom("Bunny", new Photon.Realtime.RoomOptions { MaxPlayers = 3 }, null);
            }, 0.1f);
        }
    }
    //public override void OnJoinRandomFailed(short returnCode, string message)
    //{
    //    Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
    //    Debugger.text += "OnJoinRandomFailed: No random room available, creating a new room.\n";

    //    PhotonNetwork.CreateRoom("Bunny", new RoomOptions());
    //}

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        //Debug.Log("Joined Lobby");
        //Photon.Pun.PhotonNetwork.JoinOrCreateRoom("Bunny", new Photon.Realtime.RoomOptions { MaxPlayers = 3 }, null);
        //Debugger.text += "Joined Lobby and trying to join or create room 'Bunny'.\n";
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Joined Room: " + Photon.Pun.PhotonNetwork.CurrentRoom.Name);
        Debug.Log($"Is Master Client: {Photon.Pun.PhotonNetwork.IsMasterClient}");
        Debugger.text += "Joined Room: " + Photon.Pun.PhotonNetwork.CurrentRoom.Name + "\n";

        foreach (var p in Photon.Pun.PhotonNetwork.CurrentRoom.Players)
        {
            Debug.Log(p.Value.NickName);
            Debugger.text += $"Nickname: {p.Value.NickName}, ID: {p.Value.ActorNumber}\n";
        }
    }
    NetworkDataWriter_PUN Writer = new NetworkDataWriter_PUN();
    public int Id => PhotonNetwork.LocalPlayer.ActorNumber;

    User ServerPeer
    {
        get
        {
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

    public void SendLocalEmoticonData2Server(int index)
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.SelfEmoticon);
        Writer.WriteInt(Id);
        Writer.WriteInt(InGameManager.Instance.RoomID);
        //선택한 카드를 패킷에 포함
        Writer.WriteByte((byte)index);
        Writer.SendPacket(ServerPeer);
    }

    void Update()
    {
        if(AutoStartGame == false)
        {
            if (PhotonNetwork.LevelLoadingProgress < 1.0f && IsLoaded == false)
            {
                Debugger.text = $"Loading: {PhotonNetwork.LevelLoadingProgress * 100}%";
            }
            if (PhotonNetwork.IsMessageQueueRunning == true && IsLoaded == false)
            {
                Debugger.text = "Loading complete.";
                IsLoaded = true;
                PhotonNetwork.CurrentRoom.Players[PhotonNetwork.LocalPlayer.ActorNumber].SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { "IsReady", true }
            });
            }
        }
        else if(PhotonNetwork.IsMessageQueueRunning == true && IsLoaded == false && PhotonNetwork.InRoom)
        {
            IsLoaded = true;
            PhotonNetwork.CurrentRoom.Players[PhotonNetwork.LocalPlayer.ActorNumber].SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { "IsReady", true }
            });
        }
    }
    bool IsLoaded = false;


    //protected override void OnReceivePacketFromServer(NetworkDataReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    public void OnEvent(EventData photonEvent)
    {
        if (Photon.Pun.PhotonNetwork.CurrentRoom == null)
            return;

        NetworkDataReader_PUN reader = new NetworkDataReader_PUN(photonEvent.CustomData);
        byte pac = photonEvent.Code;

        ePacketType_InGameServer packetType = (ePacketType_InGameServer)pac;
        Debug.Log($"[Server Connection] Received packet type: {packetType}");
        Debugger.text += $"Received packet type: {packetType}\n";

        switch (packetType)
        {
            case ePacketType_InGameServer.HandShake_S2U:
                {
                    int i = reader.ReadInt();
                    if (Id !=  i)
                    {
                        Debug.LogError($"[InGameUser_PUN] Self ID mismatch! Expected: {Id}, Received: {i}");
                        return;
                    }
                    Debug.Log("My ID in Server: " + this.Id);

                    Debug.Log("[Server Connection] Received HandShake_S2U packet from server.");
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.HandShake_U2S);
                    Writer.SendPacket(ServerPeer);
                }                
                break;

            case ePacketType_InGameServer.Chat_Receive:
                {
                    int id = reader.ReadInt();
                    string message = reader.ReadString();
                    NetworkResponse_Chat_Receive(id, message);
                }
                break;

            case ePacketType_InGameServer.Broadcast_Emoticon:
                {
                    int id = reader.ReadInt();
                    byte index = reader.ReadByte();

                    if(id == Id) //내가 보낸 이모티콘
                    {
                        Debug.Log($"[InGameUser_PUN] Received my own emoticon: {index}");
                    }
                    else //다른 사람이 보낸 이모티콘
                    {
                        Debug.Log($"[InGameUser_PUN] Received emoticon from player {id}: {index}");
                        InGameManager.Instance.FindDrawerByIDExceptLocal(id).PlayEmoticon(index);
                    }
                }
                break;

            case ePacketType_InGameServer.Response_JoinGame:
                Debug.Log("다른 플레이어를 기다리는 중...");
                break;

            case ePacketType_InGameServer.Broadcast_StartGame:
                //방, 방 안에 있는 유저의 ID, 캐릭터의 정보를 받아서 게임을 시작하기 위한 준비를 실행
                {
                    int roomID = reader.ReadInt();
                    List<int> users = new List<int>(new int[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt() });
                    List<byte> characters = new List<byte>(new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() });

                    if (users.Contains(Id) == false)
                    {
                        Debug.LogError($"[InGameUser_PUN] User ID {Id} not found in the user list for room {roomID}, " + "Current User IDs: " + string.Join(", ", users));
                        return;
                    }
                    Debug.Log("My ID in Server: " + this.Id);

                    var idx = users.IndexOf(Id);
                    InGameManager.Instance.SetLocalPlayerCharacter(characters[idx]);

                    users.Remove(Id);         //내건 지움
                    characters.RemoveAt(idx); //내건 지움

                    Debug.Log("Starting game with Room ID: " + roomID);
                    InGameManager.Instance.RoomID = roomID;
                    InGameManager.Instance.SetRemotePlayersIDAndCharacters(users, characters);
                    InGameManager.Instance.StartGame();

                    foreach(var p in Photon.Pun.PhotonNetwork.CurrentRoom.Players)
                    {
                        Debug.Log($"Player ID: {p.Value.ActorNumber}, Nickname: {p.Value.NickName}");
                    }
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        d.SetNickName();
                    });
                }
                break;

            case ePacketType_InGameServer.DistributeCardsFromServer:
                //서버에서 카드 분배 요청(실제 카드 데이터)를 받고, 카드 오브젝트들을 생성
                //로컬은 실제 카드로, 리모트는 더미 카드로 생성 및 에니메이션 실행
                {
                    if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                    {
                        //type : byte, value : byte
                        // * 2
                        byte cardType1 = reader.ReadByte();
                        byte cardValue1 = reader.ReadByte();
                        byte cardType2 = reader.ReadByte();
                        byte cardValue2 = reader.ReadByte();
                        NetworkResponse_DistributeCardsFromServer_TwoCardsMode(cardType1, cardValue1, cardType2, cardValue2);
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
                        NetworkResponse_DistributeCardsFromServer_ThreeCardsMode(cardType1, cardValue1, cardType2, cardValue2, cardType3, cardValue3);
                    }

                    //카드를 잘 받았음을 확인
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedCards);
                    Writer.WriteInt(Id);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.SendPacket(ServerPeer);
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectCard2Delete:
                //3장 모드에서 성공적으로 카드를 받은 사람이 3명이라면, 이 패킷이 날라옴.
                //버릴 카드를 선택하고, 서버에 선택한 카드의 정보를 보내야함
                //이전에 카드 분배 에니메이션이 실행되고 있을 것이므로, 에니메이션을 위한 딜레이를 걸어야 함.
                const float delayTime_WaitForCardDistubution = 3.0f; //카드 에니메이션이 지속되는 시간(으로 예상되는 값)
                {
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.SelectCard2Delete((card) =>
                        {
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2Delete);
                            Writer.WriteInt(Id);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            //선택한 카드를 패킷에 포함
                            Writer.WriteByte((byte)card.Type);
                            Writer.WriteByte(card.Value);
                            Writer.SendPacket(ServerPeer);


                            //에니메이션 처리
                            int idx1 = card.CardGameObject.transform.GetSiblingIndex();
                            int idxLast = InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2).CardGameObject.transform.GetSiblingIndex();

                            if(idx1 != idxLast) //선택한 카드가 마지막 카드(3장 모드이므로, 두 번째 인덱스임이 보장됨)가 아닌 경우
                            {
                                //두 카드의 순서를 변경
                                //버리고자 선택한 카드가 마지막 자식 인덱스의 카드가 되도록 수정
                                InGameManager.Instance.LocalPlayerUIDrawer.ChangeCardSibilingIndicesAndKeepMoverPosition(card, InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2));

                                //이 에니메이션을 실행해야 카드가 2장만 있는 것으로 보임
                                InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2).CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.5f, ePosition.Local);
                            }

                            //버릴 카드는 버리고자 하는 위치로 이동
                            card.CardGameObject.MoveMovementTransformPosition(InGameManager.Instance.LocalPlayerUIDrawer.DeletedCardContainer.position, 0.5f, ePosition.World);
                        });
                    }, delayTime_WaitForCardDistubution); //카드 분배 딜레이
                }
                break;
            case ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2Delete:
                //3장 모드에서 누군가가 버릴 카드를 선택했을 때 이 패킷이 날라옴.
                //로컬 것도 날라오기 때문에, 에니메이션 중복되지 않도록 로컬에 대한 패킷은 무시
                {
                    int playerId = reader.ReadInt();
                    if (playerId != InGameManager.Instance.LocalPlayer.ID) //로컬 패킷 무시
                    {
                        //2번째 카드(마지막 카드)를 0.5초동안의 에니메이션으로 DelectedCardContainer 위치로 이동
                        InGameManager.Instance.FindDrawerByIDExceptLocal(playerId).MoveCardPosition2DeletedCardContainerPosition(2, 0.5f);
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_ShowCards2Delete:
                //3장 모드에서 모든 플레이어가 버릴 카드를 선택했을 때 이 패킷이 날라옴.
                //누가 뭘 버렸는지도 같이 전달됨
                const float animationDuration_ShowCards2Delete = 2.5f; //카드 보여주는 에니메이션 시간
                {
                    int id1 = reader.ReadInt();
                    Card c1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    int id2 = reader.ReadInt();
                    Card c2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    int id3 = reader.ReadInt();
                    Card c3 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                    InGameManager.Instance.ShowCards2Delete(id1, c1, id2, c2, id3, c3);
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.RemoveCards(id1, c1, id2, c2, id3, c3);
                    }, animationDuration_ShowCards2Delete);
                }
                break;
            case ePacketType_InGameServer.S2URequest_RPSSelection:
                //2장 모드에서 모든 플레이어가 카드를 분배 받은 이후, packetDelayTime_FromCardReception_ToRPSStart초 후에 이 패킷이 날라옴. 검색하면 값을 찾을 수 있음.
                //3장 모드에서 모든 플레이어가 버릴 카드를 선택한 이후, packetDelayTime_FromShowingCards2Delete_ToRPSStart초 후에  이 패킷이 날라옴. 검색하면 값을 찾을 수 있음.
                float animationDelay_ToRPSSelect = 1.0f + (InGameManager.Instance.Mode == eGameMode.ThreeCards ? animationDuration_ShowCards2Delete : 0.0f);
                {
                    //animationDuration_ShowCards2Delete가 packetDelayTime_FromShowingCards2Delete_ToRPSStart보다 작으면 이상한 카드가 지워짐. 주의해야함.
                    //if (InGameManager.Instance.Mode == eGameMode.ThreeCards) 
                    //    //전체 플레이어에 대해, 카드가 3장이라면 반드시 2번째 카드를 삭제함. 카드가 2장이 되는걸 보장하기 위함. 서버에서도 마지막 카드를 삭제했을 것임.
                    //    InGameManager.Instance.EnsureAllPlayersDeletedCards();

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.LocalPlayer.StartSelectRPS((rps) =>
                        {
                            SendRPSAsMySelection2Server(rps);
                        });
                    }, animationDelay_ToRPSSelect); //카드 에니메이션 딜레이
                }
                break;

            case ePacketType_InGameServer.Broadcast_RPSRoundResult:
                //모든 플레이어가 자신의 가위바위보를 선택했을 때 이 패킷이 날라옴.
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
            case ePacketType_InGameServer.Broadcast_RPSStartRematch:
                //2/3명의 가위바위보 결과가 다 모인 이후, 가위바위보를 리매치해야 할 때, packetDelay_SendRematchAfterDraw초 후에 이 패킷이 날라옴.
                const float delay_StartRematch = 1.0f;
                const string message_WhenLocalShouldRematch = "리매치에 참여합니다.\n가위바위보를 다시 선택해주세요.";
                const string message_WhenLocalShouldNotRematch = "리매치에 참여하지 않습니다.\n다른 플레이어가 리매치 중입니다.";
                const float panelShowingTime_WhenLocalShouldRematch = 1.0f; //메세지 보여주는 시간
                {
                    int playersCount2Rematch = reader.ReadByte();
                    if(playersCount2Rematch < 2 || playersCount2Rematch > 3)
                    {
                        Debug.LogError($"[InGameUser_PUN] Invalid number of players for rematch: {playersCount2Rematch}. Expected 2 or 3.");
                        return;
                    }

                    //리매치하는 플레이어들을 받아옴(2명 또는 3명)
                    List<int> players2Rematch = new List<int>();
                    for (int i = 0; i < playersCount2Rematch; i++) 
                        players2Rematch.Add(reader.ReadInt()); 

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        if (players2Rematch.Contains(InGameManager.Instance.LocalPlayer.ID) == true)// 내가 리매치
                        {
                            //화면에 메세지 띄우고,
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message_WhenLocalShouldRematch, 1);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
                            //메세지를 panelShowingTime_WhenLocalShouldRematch초 동안 보여줬다가
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                //메세지를 끄고
                                InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                //가위바위로 고르기 시작
                                InGameManager.Instance.LocalPlayer.StartSelectRPS((rps) =>
                                {
                                    SendRPSAsMySelection2Server(rps);
                                });
                            }, panelShowingTime_WhenLocalShouldRematch); //리매치 시작 딜레이
                        }
                        else //내가 리매치하지 않음
                        {
                            //화면에 메세지 띄우고 그냥 둠
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message_WhenLocalShouldNotRematch, 0);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
                            InGameManager.Instance.LocalPlayer.IsOrderDetermined = true;
                            return;
                        }
                    }, delay_StartRematch);
                }
                break;

            case ePacketType_InGameServer.Broadcast_RPSFinalResult:
                //2/3명의 가위바위보 결과가 다 모인 이후, 최종 가위바위보 순위가 결정되었을 때, delay_ShowFinalOrderAfterRPS초 후에 이 패킷이 날라옴.
                {
                    int round = reader.ReadByte();
                    int firstPlayerId = reader.ReadInt();
                    int secondPlayerId = reader.ReadInt();
                    int thirdPlayerId = reader.ReadInt();

                    Debug.Log($"RPSOrder: {firstPlayerId} / {secondPlayerId} / {thirdPlayerId}");

                    bool isAllPlayerRanked = InGameManager.Instance.SetRPSResult(firstPlayerId, secondPlayerId, thirdPlayerId);
                    if(isAllPlayerRanked == false)
                    {
                        Debug.LogError("[InGameUser_PUN] Not all players are ranked in RPS final result. " +
                            $"Expected 3 players, but it is  {firstPlayerId} / {secondPlayerId} / {thirdPlayerId}.");
                        return;
                    }

                    //내 순위를 다시 전송해서 순위가 잘 결정되었음을 보장함.
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyReceivedOrders);
                    Writer.WriteInt(Id);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.WriteByte((byte)InGameManager.Instance.LocalPlayer.Order);
                    Writer.SendPacket(ServerPeer);
                    Debug.Log(message: $"[RPS] Sending my order: {InGameManager.Instance.LocalPlayer.Order}");
                }
                break;

            case ePacketType_InGameServer.S2URequest_SelectInOut_First:
                //3명의 플레이어 모두 자신이 받은 Order와와 서버의 Order가 동일할 때, 이 패킷이 날라옴.
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
                //누군가가 In/Out을 골랐으면, 지금까지 누적된 In/Out과 함께 서버가 알려줌. (최종 결과는 아님)
                {
                    //데이터를 받고
                    List<int> ins = new List<int>();
                    List<int> outs = new List<int>();
                    bool isInPlayerEnd = false;
                    byte length = reader.ReadByte();
                    while (ins.Count + outs.Count < length)
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
                            ins.Add(i); 
                        else 
                            outs.Add(i); 
                    }

                    //화면에 보여줌
                    InGameManager.Instance.ShowIOResult(ins, outs, false); //false : not final
                    Debug.Log($"[InOut] In Players: {string.Join(", ", ins)}, Out Players: {string.Join(", ", outs)}");
                }
                break;
            case ePacketType_InGameServer.Broadcast_InOutFinalResult:
                //모든 플레이어가 In/Out을 골랐으면, 최종 결과를 서버가 알려줌.
                {
                    //데이터를 받고
                    List<int> ins = new List<int>();
                    List<int> outs = new List<int>();
                    bool isInPlayerEnd = false;
                    byte length = reader.ReadByte();
                    while (ins.Count + outs.Count < length)
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
                            ins.Add(i);
                        else
                            outs.Add(i);
                    }

                    //화면에 보여줌
                    InGameManager.Instance.ShowIOResult(ins, outs, true); //true : final
                    Debug.Log($"[InOut] In Players: {string.Join(", ", ins)}, Out Players: {string.Join(", ", outs)}");
                }
                break;

            case ePacketType_InGameServer.Broadcast_SendAllPlayersCardData:
                //Broadcast_InOutFinalResult 패킷을 받고 나서, packetDelay_FromAllPlayerSelectedIOToSendAllPlayersCardData초 이후에, 이 패킷이 날라옴.
                {
                    //카드 데이터를 받아서 (id - cards) 형태로 정리하고
                    Dictionary<int, List<Card>> allCards = new Dictionary<int, List<Card>>();
                    for (int i = 0; i < 3; i++)
                    {
                        int playerID = reader.ReadInt();
                        List<Card> cs = new List<Card>();
                        if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                        {
                            for (int c = 0; c < 2; c++)
                            {
                                byte type = reader.ReadByte();
                                byte value = reader.ReadByte();
                                cs.Add(new Card((Card.CardType)type, value));
                            }
                        }
                        else if (InGameManager.Instance.Mode == eGameMode.ThreeCards)
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

                    //카드 데이터를 덮어씀(로컬 제외)
                    InGameManager.Instance.SetRemotePlayersAllCards(allCards);
                }
                break;

            case ePacketType_InGameServer.Broadcast_ShowAllCards:
                //Broadcast_SendAllPlayersCardData 패킷을 받고 나서, packetDelay_FromSendAllPlayersCardDataToShowAllCards초 이후에, 이 패킷이 날라옴.
                {
                    //카드를 보여줌(덮어쓰여진 실제 카드들)
                    InGameManager.Instance.ShowAllCardsWithAnimation();
                }
                break;

            case ePacketType_InGameServer.Broadcast_LoserOfThisRound:
                {
                    int loserId = reader.ReadInt();
                    byte outCount = reader.ReadByte();
                    RoundCounter++;
                    InGameManager.Instance.ShowLoserOfThisRound(loserId, outCount, RoundCounter);
                }
                break;
            case ePacketType_InGameServer.Broadcast_SomeoneSelectedCard2RemoveISR:
                //3장 모드에서 누군가가 버릴 카드를 선택했을 때 이 패킷이 날라옴.
                //에니메이션만 처리하면 됨
                {
                    int playerId = reader.ReadInt();
                    if (playerId != InGameManager.Instance.LocalPlayer.ID)
                    {
                        InGameManager.Instance.FindDrawerByIDExceptLocal(playerId).MoveCardPosition2DeletedCardContainerPosition(2, 0.5f);
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
                        InGameManager.Instance.RemoveCards(id1, c1, id2, c2, -1, new Card(Card.CardType.Dummy, 100));
                        InGameManager.Instance.LocalPlayerUIDrawer.SetActiveAllSpecialRuleButtons(true);
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartSpecialRule:
                {
                    byte reason = reader.ReadByte();
                    if (reason == 10)//이전 스페셜 룰에서 동점자 발생
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("이전 스페셜 룰에서 동점자가 발생했습니다.\n새로운 스페셜 룰을 시작합니다.", 1);
                    }

                    int id = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    if (id != InGameManager.Instance.LocalPlayer.ID)
                    {
                        string message = $"{id}와 {opponentId}가 동률입니다.\n1 vs. 1 룰에 관전자로 돌입합니다.";
                        //내가 스페셜 룰을 시작하는게 아님
                        IEnumerator specialRuleObserverEnumerator()
                        {
                            List<Card> dummyCards = new List<Card>();
                            if (InGameManager.Instance.Mode == eGameMode.TwoCards)
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
                            if (InGameManager.Instance.Mode == eGameMode.TwoCards)
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

                            //화면에 2초 동안 메세지를 띄우고
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message, 1);
                            yield return new WaitForSeconds(2.0f);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);

                            InGameManager.Instance.LoopForAllPlayerDrawers((drawer) => drawer.Target.ThisDeck.RemoveAllCards());
                            //카드를 추가함
                            InGameManager.Instance.FindDrawerByID(id).Target.ThisDeck.AddCards(dummyCards.ToArray());
                            InGameManager.Instance.FindDrawerByID(opponentId).Target.ThisDeck.AddCards(dummyCards2.ToArray());
                            InGameManager.Instance.FindDrawerByID(id).UpdateCardsLayout();
                            InGameManager.Instance.FindDrawerByID(opponentId).UpdateCardsLayout();

                            dummyCards.AddRange(dummyCards2);
                            foreach (var c in dummyCards) c.CardGameObject.gameObject.SetActive(false);

                            if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                                InGameManager.Instance.StartSpecialRule_TwoCardMode(id, opponentId,
                            () =>
                            {
                            },
                            (card) =>
                            {
                            },
                            () =>
                            {
                            },
                            (card) =>
                            {
                            });
                            else if (InGameManager.Instance.Mode == eGameMode.ThreeCards)
                                InGameManager.Instance.StartSpecialRule_ThreeCardMode((card2Delete) =>
                                {
                                },
                                id, opponentId,
                                () =>
                                {
                                },
                                (card) =>
                                {
                                },
                                () =>
                                {
                                },
                                (card) =>
                                {
                                });

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActiveAllSpecialRuleButtons(false); //버튼은 무조건 꺼야 함
                            yield return new WaitForSeconds(1.1f);
                            foreach (var c in dummyCards) c.CardGameObject.gameObject.SetActive(true);
                            //에니메이션도 실행
                            PlayCardsAnimation_MoveFromDeck2LocalPoisition(dummyCards, 0, 0);
                        }
                        StartCoroutine(specialRuleObserverEnumerator());
                    }
                    else
                    {
                        List<Card> cards2Animated = new List<Card>();

                        //내 카드 추가
                        List<Card> myCards = new List<Card>();
                        if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                        {
                            var card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            myCards.Add(card1); myCards.Add(card2);
                            cards2Animated.Add(card1); cards2Animated.Add(card2);
                        }
                        else
                        {
                            var card1 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card2 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            var card3 = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                            myCards.Add(card1); myCards.Add(card2); myCards.Add(card3);
                            cards2Animated.Add(card1); cards2Animated.Add(card2); cards2Animated.Add(card3);
                        }
                        //상대 카드 추가
                        List<Card> user2Cards = new List<Card>();
                        if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                        {
                            var c1 = new Card(Card.CardType.Dummy, 100);
                            var c2 = new Card(Card.CardType.Dummy, 100);
                            user2Cards.Add(c1); user2Cards.Add(c2);
                            cards2Animated.Add(c1); cards2Animated.Add(c2);
                        }
                        else
                        {
                            var c1 = new Card(Card.CardType.Dummy, 100);
                            var c2 = new Card(Card.CardType.Dummy, 100);
                            var c3 = new Card(Card.CardType.Dummy, 100);
                            user2Cards.Add(c1); user2Cards.Add(c2); user2Cards.Add(c3);
                            cards2Animated.Add(c1); cards2Animated.Add(c2); cards2Animated.Add(c3);
                        }


                        string message = $"{id}와 {opponentId}가 동률입니다.\n1 vs. 1 룰로 돌입합니다.";


                        IEnumerator specialRuneEnumerator()
                        {
                            //화면에 2초 동안 메세지를 띄우고
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message, 1);
                            yield return new WaitForSeconds(2.0f);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);

                            InGameManager.Instance.LoopForAllPlayerDrawers((drawer) => drawer.Target.ThisDeck.RemoveAllCards());
                            //카드를 추가함
                            InGameManager.Instance.FindDrawerByID(id).Target.ThisDeck.AddCards(myCards.ToArray());
                            InGameManager.Instance.FindDrawerByID(opponentId).Target.ThisDeck.AddCards(user2Cards.ToArray());
                            
                            //글쎄... 암튼 이렇게 하면 에니메이션이 정상적으로 덱 위치에서부터 시작됨
                            foreach (var c in cards2Animated) c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
                            InGameManager.Instance.FindDrawerByID(id).UpdateCardsLayout();
                            InGameManager.Instance.FindDrawerByID(opponentId).UpdateCardsLayout();
                            foreach (var c in cards2Animated) c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);

                            foreach (var c in cards2Animated) c.CardGameObject.MovementTransform.gameObject.SetActive(false);

                            if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                                InGameManager.Instance.StartSpecialRule_TwoCardMode(id, opponentId,
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

                                card.CardGameObject.MoveMovementTransformPosition(InGameManager.Instance.DeckTransform.position, 0.75f, ePosition.World);
                                card.CardGameObject.MoveMovementTransformScale(Vector3.one * 0.5f, 0.75f);
                                InGameManager.Instance.LocalPlayer.Card2Exchange = card;
                            },
                            () =>
                            {
                                //교환 버튼을 누름
                            },
                            (card) =>
                            {
                                //exchange with opponent
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExhangeCardWithOpponentInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);
                            });
                            else if(InGameManager.Instance.Mode == eGameMode.ThreeCards)
                                InGameManager.Instance.StartSpecialRule_ThreeCardMode((card2Delete) =>
                                {
                                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectCard2DeleteISR);
                                    Writer.WriteInt(Id);
                                    Writer.WriteInt(InGameManager.Instance.RoomID);
                                    Writer.WriteByte((byte)card2Delete.Type);
                                    Writer.WriteByte(card2Delete.Value);
                                    Writer.SendPacket(ServerPeer);


                                    int idx1 = card2Delete.CardGameObject.transform.GetSiblingIndex();
                                    int idxLast = InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2).CardGameObject.transform.GetSiblingIndex();

                                    if (idx1 != idxLast)  //선택한 카드가 마지막 카드(3장 모드이므로, 두 번째 인덱스임이 보장됨)가 아닌 경우
                                    {
                                        InGameManager.Instance.LocalPlayerUIDrawer.ChangeCardSibilingIndicesAndKeepMoverPosition(card2Delete, InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2));
                                        InGameManager.Instance.LocalPlayerUIDrawer.Target.ThisDeck.GetCard(2).CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.5f, ePosition.Local);
                                    }
                                    //버릴 카드는 DeletedContainer 위치로 이동
                                    card2Delete.CardGameObject.MoveMovementTransformPosition(InGameManager.Instance.LocalPlayerUIDrawer.DeletedCardContainer.position, 0.5f, ePosition.World);
                                },
                                id, opponentId,
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

                                    card.CardGameObject.MoveMovementTransformPosition(InGameManager.Instance.DeckTransform.position, 0.75f, ePosition.World);
                                    card.CardGameObject.MoveMovementTransformScale(Vector3.one * 0.5f, 0.75f);
                                    InGameManager.Instance.LocalPlayer.Card2Exchange = card;
                                    //InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(card);
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


                            yield return new WaitForSeconds(1.1f);
                            foreach (var c in cards2Animated) c.CardGameObject.MovementTransform.gameObject.SetActive(true);
                            //에니메이션도 실행
                            PlayCardsAnimation_MoveFromDeck2LocalPoisition(cards2Animated, 0, InGameManager.Instance.Mode == eGameMode.TwoCards ? 2 : 3);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActiveAllSpecialRuleButtons(false);
                            if (InGameManager.Instance.Mode == eGameMode.TwoCards)
                            {
                                yield return new WaitForSeconds(3.0f); //카드 에니메이션이 끝난 이후에 버튼들을 활성화
                                InGameManager.Instance.LocalPlayerUIDrawer.SetActiveAllSpecialRuleButtons(true);
                            }
                            //3장 모드의 버튼 활성화는 Broadcast_ShowCards2DeleteISR 패킷을 받고 나서 처리
                        }
                        StartCoroutine(specialRuneEnumerator());
                    }
                }
                break;
            case ePacketType_InGameServer.S2UAsk_ExhangeCardWithOpponentInSpecialRule:
                //스페셜 룰에서 상대가 카드 교환을 요청했을 때, 이 패킷이 날라옴.
                //내가 요청받은게 맞는지 확인하고, 맞다면 수락 / 거절을 선택 및 서버에게 전송
                //수락했다면, 교환할 카드를 선택해서 서버에게 전송
                {
                    int asker = reader.ReadInt();
                    int asked = reader.ReadInt();
                    if (asked != InGameManager.Instance.LocalPlayer.ID)
                    {
                        //내가 요청받은게 아님
                        return;
                    }

                    //내가 요청받은거임
                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenterWithButtons("상대가 카드 교환을 요청했습니다", 8, "수락", "거절",
                        () =>
                        {
                            //수락
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExhangeCardWithOpponentISR);
                            Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(true); //수락
                            Writer.SendPacket(ServerPeer);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                            //교환할 카드를 선택
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
                            //거절
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
                //상대방이 카드 교환 수락/거절 버튼을 누른 후에 이 패킷이 날라옴.
                {
                    int requesterID = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    bool isAccepted = reader.ReadBool();

                    const string message_WhenAccepted = "상대가 교환을 수락했습니다.\n카드 선택을 기다리는 중...";
                    const string message_WhenRejected = "상대가 교환을 거절했습니다.";

                    if (InGameManager.Instance.LocalPlayer.ID == requesterID)
                    {
                        //내가 보낸 요청임
                        if (isAccepted)
                        {
                            //교환 수락
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message_WhenAccepted, 0);
                            InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId).SetIOText("수락");
                        }
                        else
                        {
                            //교환 거절
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(message_WhenRejected, 0);

                            //상대 Drawer UI 수정
                            var rp = InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId);
                            string originText = rp.GetIOText();
                            rp.SetIOText("거절");
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                rp.SetIOText(originText);
                                InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                                InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                            }, 2.0f);

                            //내 카드를 원상태로 돌림
                            InGameManager.Instance.LocalPlayerUIDrawer.SetAllCards2DefaultState();
                        }
                    }
                }
                break;

            case ePacketType_InGameServer.Broadcast_ExhangeWithOpponentInSpecialRuleResult:
                //스페셜 룰에서 최종적으로 카드 교환이 이루어져야 할 때 이 패킷이 날라옴.
                //네 GPT가 해줬습니다. 매우 편합니다.
                {
                    // ─────────────────────────────────────────────────────────────────────────────
                    // 1) 패킷 파싱
                    // ─────────────────────────────────────────────────────────────────────────────
                    int id1 = reader.ReadInt();
                    byte type1 = reader.ReadByte();
                    byte value1 = reader.ReadByte();
                    int id2 = reader.ReadInt();
                    byte type2 = reader.ReadByte();
                    byte value2 = reader.ReadByte();

                    Card card1 = new Card((Card.CardType)type1, value1);
                    Card card2 = new Card((Card.CardType)type2, value2);

                    int localId = InGameManager.Instance.LocalPlayer.ID;

                    // 내 교환이 아니면(=관전 등) 원래 코드도 아무것도 안 했으므로 그대로 탈출
                    if (id1 != localId && id2 != localId)
                        break;

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 2) "내가 상대에게 내준 카드"와 "내가 받는 카드"를 통일된 관점으로 정리
                    //    - localOutCardSpec : 내가 넘긴 카드의 (타입/값) 스펙
                    //    - localInCard      : 내가 받는 카드(실제 Card 인스턴스, AddCard 후 GameObject 부여)
                    //    - opponentId       : 교환 상대의 ID
                    // ─────────────────────────────────────────────────────────────────────────────
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

                    // 내가 실제로 들고 있는 "넘길 카드"를 찾는다
                    Card localOutCard = InGameManager.Instance.LocalPlayer.ThisDeck.GetCardByTypeAndValue(outType, outValue);
                    if (localOutCard == null)
                    {
                        Debug.LogError($"[InGameUser] Local player does not have the card {outType}-{outValue} to exchange.");
                        break;
                    }

                    // 상대 UI 드로어 가져오기
                    var oppDrawer = InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId);

                    // 상대 덱에서 임시로 꺼내둘 카드(원본 코드와 동일하게 index=1 사용)
                    var opponentTempCard = oppDrawer.Target.ThisDeck.GetCard(1);

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 3) 레이아웃/애니메이션을 위한 기존 위치/스케일 백업
                    // ─────────────────────────────────────────────────────────────────────────────
                    int originSiblingIndex = localOutCard.CardGameObject.transform.GetSiblingIndex();

                    // 상대 카드의 원위치(내가 받을 카드를 여기서부터 출발시키기 위해)
                    Vector3 oppOriginPos = opponentTempCard.CardGameObject.GetMoverPosition();
                    Vector3 oppOriginScl = opponentTempCard.CardGameObject.GetMoverScale();

                    // 내가 넘길 카드의 원위치(상대가 받을 카드의 출발점)
                    Vector3 localOriginPos = localOutCard.CardGameObject.GetMoverPosition();
                    Vector3 localOriginScl = localOutCard.CardGameObject.GetMoverScale();

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 4) 덱 갱신 (원본 코드 동일 순서 유지)
                    //    - 내 덱: 넘길 카드 제거 → 받는 카드 추가
                    //    - 상대 덱: 임시 카드 제거 → 내가 넘긴 카드 추가
                    // ─────────────────────────────────────────────────────────────────────────────
                    InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(localOutCard);
                    InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(localInCard);

                    oppDrawer.Target.ThisDeck.RemoveCard(opponentTempCard);
                    oppDrawer.Target.ThisDeck.AddCard(localOutCard);

                    // 내가 받은 카드를 기존 자리(넘긴 카드의 인덱스)로 배치
                    localInCard.CardGameObject.transform.SetSiblingIndex(originSiblingIndex);

                    // 레이아웃 갱신
                    InGameManager.Instance.LocalPlayerUIDrawer.UpdateCardsLayout();
                    oppDrawer.UpdateCardsLayout();

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 5) 서버에 성공 응답 전송 (원본과 동일)
                    // ─────────────────────────────────────────────────────────────────────────────
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule);
                    Writer.WriteInt(localId);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.SendPacket(ServerPeer);

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 6) 연출(앞/뒷면 + 이동/스케일 애니메이션)
                    //    - 내가 받은 카드는 뒷면 → 애니 후 앞면
                    //    - 내가 넘긴 카드는 앞면 → 애니 후 뒷면
                    //    - 타이밍/지속시간은 원본과 동일
                    // ─────────────────────────────────────────────────────────────────────────────
                    const float moveDur = 0.75f;
                    const float faceFlipDelay = 0.5f;
                    const float faceFlipDur = 1.2f;
                    const float faceFlipEase = 0.4f;

                    localInCard.CardGameObject.SetFace(false);
                    localOutCard.CardGameObject.SetFace(true);

                    IEnumerator AnimateSwap()
                    {
                        // 프레임 끝에서 Transform 초기 세팅(원본과 동일한 타이밍 보장)
                        yield return new WaitForEndOfFrame();

                        // 시작점 세팅
                        localInCard.CardGameObject.SetMovementTransformPosition(oppOriginPos);
                        localInCard.CardGameObject.SetMovementTransformScale(oppOriginScl);

                        localOutCard.CardGameObject.SetMovementTransformPosition(localOriginPos);
                        localOutCard.CardGameObject.SetMovementTransformScale(localOriginScl);

                        // 내 카드 자리로 이동 (로컬 좌표 기준)
                        localInCard.CardGameObject.MoveMovementTransformPosition(Vector3.zero, moveDur, ePosition.Local);
                        localInCard.CardGameObject.MoveMovementTransformScale(localOriginScl, moveDur);

                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            localInCard.CardGameObject.SetFaceAnimated(true, faceFlipDur, faceFlipEase);
                        }, faceFlipDelay);

                        // 상대 쪽으로 보낼 카드 이동
                        localOutCard.CardGameObject.MoveMovementTransformPosition(Vector3.zero, moveDur, ePosition.Local);
                        localOutCard.CardGameObject.MoveMovementTransformScale(oppOriginScl, moveDur);

                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            localOutCard.CardGameObject.SetFaceAnimated(false, faceFlipDur, faceFlipEase);
                            InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                            InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                        }, faceFlipDelay);
                    }
                    StartCoroutine(AnimateSwap());

                    // ─────────────────────────────────────────────────────────────────────────────
                    // 7) 중앙 패널 UI 정리 (원본 동일)
                    // ─────────────────────────────────────────────────────────────────────────────
                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);

                }
                break;

            case ePacketType_InGameServer.S2UResponse_ExhangeCardWithDeckInSpecialRule:
                //스페셜 룰에서 덱과 카드 교환을 요청했을 때, 이 패킷이 날라옴. (버튼 누른 즉시, 딜레이 없이)
                //내가 보낸 요청이면 실제 데이터 교환 + 애니메이션을 처리하고, 상대가 보낸 요청이면 애니메이션만 처리하면 됨
                {
                    int id = reader.ReadInt();
                    if (id == InGameManager.Instance.LocalPlayer.ID)
                    {
                        //내가 요청한 교환임
                        Card card = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            int originIdx = InGameManager.Instance.LocalPlayer.Card2Exchange.CardGameObject.transform.GetSiblingIndex();
                            //기존 카드 삭제 및 새 카드 추가
                            InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(InGameManager.Instance.LocalPlayer.Card2Exchange);
                            InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(card);

                            //순서 바꾸기
                            card.CardGameObject.transform.SetSiblingIndex(originIdx);
                            InGameManager.Instance.LocalPlayerUIDrawer.UpdateCardsLayout();

                            InGameManager.Instance.LocalPlayerUIDrawer.MoveCardPosition_FromDeckPosition2LocalPosition(card, 0.75f);

                            //작은 상태에서 시작해 원본 크기로 돌아옴
                            Vector3 originScale = card.CardGameObject.GetMoverScale();
                            card.CardGameObject.SetMovementTransformScale(originScale * 0.5f);
                            card.CardGameObject.MoveMovementTransformScale(originScale, 0.75f);

                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                card.CardGameObject.SetFaceAnimated(true, 1.2f, 0.4f);
                                InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                                InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                            }, 0.9f);
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                FindObjectOfType<CardDeck>().PlayShuffleAnimation();
                            }, 1.0f);
                        }, 0.8f); //약간의 딜레이 추가(화면이 너무 빠르게 깜빡이는걸 방지)
                    }
                    else
                    {
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            //내가 요청한건 아님. 에니메이션만 처리하면 됨
                            var rp = InGameManager.Instance.FindDrawerByIDExceptLocal(id);

                            //굳이 카드 추가했다 삭제하지 말고, 그냥 겉으로 보기에만 움직이면 됨
                            var c = rp.Target.ThisDeck.GetCard(1);
                            rp.MoveCardPosition_FromDeckPosition2LocalPosition(c, 0.75f);
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                FindObjectOfType<CardDeck>().PlayShuffleAnimation();
                            }, 1.0f);
                        }, 0.8f);
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_ShowSpecialRuleCards:
                //스페셜 룰에서 상대방의 카드를 보여줘야 할 때 이 패킷이 날라옴. (두 명 모두 Go를 누른 이후에, 딜레이 없이)
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
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.SetRemotePlayersAllCards(specialRuleCards);
                        InGameManager.Instance.ShowAllCardsWithAnimation();
                    }, 1.0f); //약간의 딜레이 추가(화면이 너무 빠르게 깜빡이는걸 방지)
                }
                break;
            case ePacketType_InGameServer.Broadcast_LoserOfSpecialRule:
                {
                    int loser = reader.ReadInt();
                    byte outCount = reader.ReadByte();
                    RoundCounter++;
                    InGameManager.Instance.ShowLoserOfThisRound(loser, outCount, RoundCounter);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartNextRound:
                {
                    byte reason = reader.ReadByte();
                    if (reason == 0)//전판이 3명 다 인 & 동점인 상황
                    {

                    }
                        InGameManager.Instance.StartNextRound(reason);
                }
                break;
            case ePacketType_InGameServer.Broadcast_FinalResult:
                {
                    int loserID = reader.ReadInt();
                    if(loserID == InGameManager.Instance.LocalPlayerUIDrawer.Target.ID)
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter($"Loser: {loserID}\n게임이 종료되었습니다.", InGameManager.Instance.LocalPlayerUIDrawer.Character + 5);
                    else
                    {
                        foreach(var rp in InGameManager.Instance.RemotePlayerUIDrawers)
                        {
                            if(rp.Target.ID == loserID) 
                                InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter($"Loser: {loserID}\n게임이 종료되었습니다.", rp.Character + 5); 
                        }
                    }

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        PhotonNetwork.LeaveRoom();
                        PhotonNetwork.LoadLevel("Lobby");
                    }, 2.0f);
                }
                break;
        }
    }

    private void SendRPSAsMySelection2Server(eRPS rps)
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_RPSSelection);
        Writer.WriteInt(Id);
        Writer.WriteInt(InGameManager.Instance.RoomID);
        Writer.WriteByte((byte)rps);
        Writer.SendPacket(ServerPeer);
    }

    private void NetworkResponse_SelectIO()
    {
        if (InGameManager.Instance.LocalPlayer.Order == 2 && InGameManager.Instance.OutPlayerCount == 2)
        {
            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter("2명이 먼저 OUT을 골랐습니다.\nIN을 선택합니다.", 0);
            DelayedFunctionHelper.InvokeDelayed(() =>
            {
                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SelectInOut_Third);
                Writer.WriteInt(Id);
                Writer.WriteInt(InGameManager.Instance.RoomID);
                Writer.WriteByte((byte)InGameManager.Instance.LocalPlayer.Order);
                Writer.WriteByte((byte)eIO.In);

                Writer.SendPacket(ServerPeer);
            }, 2.0f);
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

    private void NetworkResponse_DistributeCardsFromServer_ThreeCardsMode(byte cardType1, byte cardValue1, byte cardType2, byte cardValue2, byte cardType3, byte cardValue3)
    {
        List<Card> cards2Animated = new List<Card>();
        //매개변수로 내 카드를 생성
        Card card1 = new Card((Card.CardType)cardType1, cardValue1);
        Card card2 = new Card((Card.CardType)cardType2, cardValue2);
        Card card3 = new Card((Card.CardType)cardType3, cardValue3);

        InGameManager.Instance.LocalPlayer.ThisDeck.AddCards(new Card[] { card1, card2, card3 });
        for (int i = 0; i < 3; i++)
            cards2Animated.Add(InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(i));


        //상대방 카드 데이터 추가
        foreach (var rp in InGameManager.Instance.RemotePlayers)
        {
            //상대방 카드는 더미로 생성(데이터는 현재 중요하지 않음)
            rp.ThisDeck.AddCards(new Card[] { new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100) });

            for (int i = 0; i < 3; i++)
                cards2Animated.Add(rp.ThisDeck.GetCard(i));
        }

        PlayCardsAnimation_MoveFromDeck2LocalPoisition(cards2Animated, 0, 3); //내 카드는 0, 1, 2 인덱스
    }

    private void NetworkResponse_DistributeCardsFromServer_TwoCardsMode(byte cardType1, byte cardValue1, byte cardType2, byte cardValue2)
    {
        List<Card> cards2Animated = new List<Card>();

        //매개변수로 내 카드를 생성
        Card card1 = new Card((Card.CardType)cardType1, cardValue1);
        Card card2 = new Card((Card.CardType)cardType2, cardValue2);

        //내 카드 데이터 추가
        InGameManager.Instance.LocalPlayer.ThisDeck.AddCards(new Card[] { card1, card2 });
        for (int i = 0; i < 2; i++)
            cards2Animated.Add(InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(i));

        //상대방 카드 데이터 추가
        foreach (var rp in InGameManager.Instance.RemotePlayers)
        {
            //상대방 카드는 더미로 생성(데이터는 현재 중요하지 않음)
            rp.ThisDeck.AddCards(new Card[] { new Card(Card.CardType.Dummy, 100), new Card(Card.CardType.Dummy, 100) });

            for (int i = 0; i < 2; i++)
                cards2Animated.Add(rp.ThisDeck.GetCard(i));
        }
        

        PlayCardsAnimation_MoveFromDeck2LocalPoisition(cards2Animated, 0, 2); //로컬 카드는 0, 1 인덱스
    }

    void PlayCardsAnimation_MoveFromDeck2LocalPoisition(List<Card> cards2Animated, int localCardsFrom, int localCardsTo)
    {
        for (int i = 0; i < cards2Animated.Count; i++) //모든 카드를 비활성화
        {
            var card = cards2Animated[i];
            card.CardGameObject.ActiveSelection(false, null);
            card.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
            card.CardGameObject.SetFace(false);
        }


        IEnumerator animation()
        {
            CardDistributionSound.Play();
            for (int i = 0; i < cards2Animated.Count; i++)
            {
                var card = cards2Animated[i];
                card.CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.4f, ePosition.Local);
                yield return new WaitForSeconds(0.3f);
            }

            for (int i = localCardsFrom; i < localCardsTo; i++)
            {
                var card = cards2Animated[i];
                card.CardGameObject.SetFaceAnimated(true, 1.2f, 0.2f); //로컬 카드는 앞면으로 설정
                yield return new WaitForSeconds(0.1f);
            }
            FindObjectOfType<CardDeck>().PlayShuffleAnimation();
            CardDistributionSound.Stop();
        }
        InGameManager.Instance.StartCoroutine(animation());
    }

    public void SendChat2Server(string message)
    {
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.Chat_Send);
        Writer.WriteInt(Id);
        Writer.WriteString(message);
        Writer.SendPacket(ServerPeer);
        Debug.Log($"[Chat] Sent message to server: {message}");
    }
    private void NetworkResponse_Chat_Receive(int id, string message)
    {
        OnChatReceived?.Invoke(id, message);
        Debug.Log($"[Chat] Received message from server: {message}");
    }

    public Action<int, string> OnChatReceived;
}
