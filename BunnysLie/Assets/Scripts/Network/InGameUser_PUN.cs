using ExitGames.Client.Photon;
using Helpers;
using LiteNetLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
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
using UnityEngine.UI;
using Voice;
using VOYAGER_Server;
using static InGameServer_PUN;

[System.Serializable]
public class InGameUser_PUN : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static InGameUser_PUN Instance { get; private set; }

    [SerializeField] bool AutoStartGame = false;
    [SerializeField] Image FullscreenBlackImage;
    [SerializeField] AudioSource OnFinalGameEnd;

    private void Start()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        Debugger.text = "InGameUser_PUN started.\n";
        FullscreenBlackImage.gameObject.SetActive(true);
        //#if UNITY_EDITOR

        if (AutoStartGame)
        {
#if UNITY_EDITOR
            Photon.Pun.PhotonNetwork.NickName = ParrelSync.ClonesManager.GetArgument();
#else
            Photon.Pun.PhotonNetwork.NickName = "Player" + UnityEngine.Random.Range(1000, 9999).ToString();
#endif
            Debug.Log("NickName: " + Photon.Pun.PhotonNetwork.NickName);
            //#endif
            PhotonNetwork.GameVersion = "0.2.0";
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
            InGameManager.Instance.IsRandomMode = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RandomMode", out var rnd) ? (bool)rnd : false;
        }

        Writer = new NetworkDataWriter_PUN();
    }
    [SerializeField] TMP_Text Debugger;
    public int RoundCounter { get; private set; }
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

    int TimeLimitCounter = 0;
    bool CheckMyTimeLimitCounter()
    {
        if(TimeLimitCounter >= 4)
        {
            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_TimeOut, 3);
            PhotonNetwork.LeaveRoom();
            DelayedFunctionHelper.InvokeDelayed(() =>
            {
                PhotonNetwork.LoadLevel("Lobby");
            }, 2.0f);
            return true;
        }
        return false;
    }
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

        if (PhotonNetwork.InRoom == true && InGameManager.Instance.IsStarted == true && PreviousPlayerCount != PhotonNetwork.CurrentRoom.PlayerCount)
        {
            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_NotEnoughPlayer, 3);
            AchievementManager.AddToStat_INT(StatsId.SUDDEN_END_COUNT, 1);
            var d = FindFirstObjectByType<PunVoiceClient>();
            if (d != null)
                Destroy(d.gameObject);
            PhotonNetwork.LeaveRoom();
            DelayedFunctionHelper.InvokeDelayed(() =>
            {
                PhotonNetwork.LoadLevel("Lobby");
            }, 2.0f);
        }
        PreviousPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
    }
    int PreviousPlayerCount;
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
            case ePacketType_InGameServer.Voice_Receive:
                {
                    int id = reader.ReadInt();
                    int length = reader.ReadInt();
                    byte[] voiceData = reader.ReadByteArray(length);
                    FindObjectOfType<VoiceSetting>().ReceiveVoiceData(id, voiceData);
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
                    AchievementManager.AddToStat_INT(StatsId.GAMES_PLAYED, 1);

                    int roomID = reader.ReadInt();
                    List<int> users = new List<int>(new int[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt() });
                    List<byte> characters = new List<byte>(new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() });
                    //List<bool> isTmpUsers = new List<bool>(new bool[] { reader.ReadBool(), reader.ReadBool(), reader.ReadBool() });
                    if (users.Contains(Id) == false)
                    {
                        Debug.LogError($"[InGameUser_PUN] User ID {Id} not found in the user list for room {roomID}, " + "Current User IDs: " + string.Join(", ", users));
                        return;
                    }
                    Debug.Log("My ID in Server: " + this.Id);

                    var idx = users.IndexOf(Id);
                    InGameManager.Instance.SetLocalPlayerCharacter(characters[idx]);
                    if(PlayerPrefs.GetString("LoginId") == "LoginWithoutAccount") //내거
                    {
                        if (characters[idx] == 0)
                        {
                            PhotonNetwork.LocalPlayer.NickName = "계순이";
                        }
                        else if (characters[idx] == 1)
                        {
                            PhotonNetwork.LocalPlayer.NickName = "달래";
                        }
                        else
                        {
                            PhotonNetwork.LocalPlayer.NickName = "덕구";
                        }
                    }

                    users.Remove(Id);         //내건 지움
                    characters.RemoveAt(idx); //내건 지움
                    //isTmpUsers.RemoveAt(idx); //내건 지금

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

                    FindObjectOfType<Recorder>().StopRecordingWhenPaused = false;
                    FindObjectOfType<Recorder>().RecordingEnabled = true;
                    FindObjectOfType<Recorder>().TransmitEnabled = true;
                    FindObjectOfType<Recorder>().RestartRecording();
                    //FindObjectOfType<PunVoiceClient>().SpeakerLinked += (speaker) =>
                    //{
                    //    speaker.record
                    //    //InGameManager.Instance.FindDrawerByID(speaker.RemoteVoice.PlayerId).GetComponent<RemovePlayerUIDrawer>().SetSpeaker(speaker);
                    //};

                    Helpers.ObjectMoveHelper.ChangeAlpha(FullscreenBlackImage, 0.0f, 1.0f);
                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        FullscreenBlackImage.gameObject.SetActive(false);
                    }, 1.0f);


                    QuestController.ResetValuesPerGame();
                    QuestController.ResetValuesPerRound();
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
                            InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
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
                        if(AutoStartGame == false)
                        {
                            InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                            {
                                TimeLimitCounter++;
                                if (CheckMyTimeLimitCounter()) return;
                                InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(0).CardGameObject.SelectButton.onClick.Invoke();
                                InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_TimeOutCount, TimeLimitCounter), 0);
                                DelayedFunctionHelper.InvokeDelayed(() =>
                                {
                                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                }, 2.0f);
                            });
                        }
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
                            InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                            SendRPSAsMySelection2Server(rps);
                        });
                        InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                        {
                            TimeLimitCounter++;
                            if (CheckMyTimeLimitCounter()) return;
                            switch ((eRPS)UnityEngine.Random.Range(0, 3))
                            {
                                case eRPS.Rock:
                                    InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_R.onClick.Invoke();
                                    break;
                                case eRPS.Paper:
                                    InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_P.onClick.Invoke();
                                    break;
                                case eRPS.Scissors:
                                    InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_S.onClick.Invoke();
                                    break;
                            }
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_TimeOutCount, TimeLimitCounter), 0);
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                            }, 2.0f);
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

                    Debug.Log(round + "Round RPS Result Received: " +
                        $"{firstPlayerId}({firstPlayerRPS},{firstPlayerOrder}) / " +
                        $"{secondPlayerId}({secondPlayerRPS},{secondPlayerOrder}) / " +
                        $"{thirdPlayerId}({thirdPlayerRPS},{thirdPlayerOrder})");

                    DelayedFunctionHelper.InvokeDelayed(() =>
                    {
                        InGameManager.Instance.ShowRPSRoundResult(round, firstPlayerId, firstPlayerRPS, firstPlayerOrder,
                        secondPlayerId, secondPlayerRPS, secondPlayerOrder,
                        thirdPlayerId, thirdPlayerRPS, thirdPlayerOrder);
                    }, 1.5f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_RPSStartRematch:
                //2/3명의 가위바위보 결과가 다 모인 이후, 가위바위보를 리매치해야 할 때, packetDelay_SendRematchAfterDraw초 후에 이 패킷이 날라옴.
                const float delay_StartRematch = 2.0f;
                const float panelShowingTime_WhenLocalShouldRematch = 2.0f; //메세지 보여주는 시간
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
                            QuestController.OnDrawRPS(); //이전에 비긴걸 여기서 처리

                            //화면에 메세지 띄우고,
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.message_WhenLocalShouldRematch, 1);
                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
                            //메세지를 panelShowingTime_WhenLocalShouldRematch초 동안 보여줬다가
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                //메세지를 끄고
                                InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                //가위바위로 고르기 시작
                                InGameManager.Instance.LocalPlayer.StartSelectRPS((rps) =>
                                {
                                    InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                                    SendRPSAsMySelection2Server(rps);
                                });
                                InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                                {
                                    TimeLimitCounter++;
                                    if (CheckMyTimeLimitCounter()) return;
                                    switch ((eRPS)UnityEngine.Random.Range(0, 3))
                                    {
                                        case eRPS.Rock:
                                            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_R.onClick.Invoke();
                                            break;
                                        case eRPS.Paper:
                                            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_P.onClick.Invoke();
                                            break;
                                        case eRPS.Scissors:
                                            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_S.onClick.Invoke();
                                            break;
                                    }
                                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_TimeOutCount, TimeLimitCounter), 0);
                                });
                            }, panelShowingTime_WhenLocalShouldRematch); //리매치 시작 딜레이
                        }
                        else //내가 리매치하지 않음
                        {
                            //화면에 메세지 띄우고 그냥 둠
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.message_WhenLocalShouldNotRematch, InGameManager.Instance.LocalPlayerUIDrawer.GetOrderText()), 0);
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
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        d.ShowGrayPanel(true);
                    });
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        if(d.Target.Order == 0)
                            d.ShowGrayPanel(false);
                    });
                    if (InGameManager.Instance.LocalPlayer.Order != 0)
                        return;
                    NetworkResponse_SelectIO();
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Second:
                {
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        d.ShowGrayPanel(true);
                    });
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        if (d.Target.Order == 1)
                            d.ShowGrayPanel(false);
                    });
                    if (InGameManager.Instance.LocalPlayer.Order != 1)
                        return;
                    NetworkResponse_SelectIO();
                }
                break;
            case ePacketType_InGameServer.S2URequest_SelectInOut_Third:
                {
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        d.ShowGrayPanel(true);
                    });
                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        if (d.Target.Order == 2)
                            d.ShowGrayPanel(false);
                    });
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

                    InGameManager.Instance.LoopForAllPlayerDrawers((d) =>
                    {
                        d.ShowGrayPanel(false);
                    });
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

                    ///////////////////////////////////////////////////////////////////////////////
                    ///퀘스트 처리
                    if (loserId == InGameManager.Instance.LocalPlayer.ID)
                        QuestController.OnLoseRoundGame();
                    else
                        QuestController.OnWinRoundGame();

                    AchievementManager.AddToStat_INT(StatsId.TOTAL_ROUNDS, 1);
                    ///////////////////////////////////////////////////////////////////////////////
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
                        InGameManager.Instance.LocalPlayerUIDrawer.ReStart30sClock2GoInSpecialRule();
                    }, 2.0f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_StartSpecialRule:
                {
                    byte reason = reader.ReadByte();
                    if (reason == 10)//이전 스페셜 룰에서 동점자 발생
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_NewSpecialRule, 1);
                    }

                    int id = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    if (id != InGameManager.Instance.LocalPlayer.ID)
                    {
                        string message = string.Format(ConstStrings.Message_StartSpecialRuleObserver, InGameManager.Instance.FindDrawerByID(id).GetNickName(), InGameManager.Instance.FindDrawerByID(opponentId).GetNickName());
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


                        string message = string.Format(ConstStrings.Message_StartSpecialRulePlayer, InGameManager.Instance.FindDrawerByID(id).GetNickName(), InGameManager.Instance.FindDrawerByID(opponentId).GetNickName());


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
                                InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
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
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithOpponentInSpecialRule);
                                Writer.WriteInt(Id);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte(card.Value);
                                Writer.SendPacket(ServerPeer);
                            });
                            else if (InGameManager.Instance.Mode == eGameMode.ThreeCards)
                            {
                                InGameManager.Instance.StartSpecialRule_ThreeCardMode((card2Delete) =>
                                {
                                    InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
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
                                    InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
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
                                    //Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithOpponentInSpecialRule);
                                    //Writer.WriteInt(Id);
                                    //Writer.WriteInt(InGameManager.Instance.RoomID);
                                    //Writer.SendPacket(ServerPeer);
                                },
                                (card) =>
                                {
                                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_ExchangeCardWithOpponentInSpecialRule);
                                    Writer.WriteInt(Id);
                                    Writer.WriteInt(InGameManager.Instance.RoomID);
                                    Writer.WriteByte((byte)card.Type);
                                    Writer.WriteByte(card.Value);
                                    Writer.SendPacket(ServerPeer);
                                    //exchange with opponent
                                });
                                InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                            }

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
                            else
                            {
                                InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                                {
                                    TimeLimitCounter++;
                                    if (CheckMyTimeLimitCounter()) return;
                                    InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(0).CardGameObject.SelectButton.onClick.Invoke();
                                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_TimeOutCount, TimeLimitCounter), 0);
                                    DelayedFunctionHelper.InvokeDelayed(() =>
                                    {
                                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                                    }, 2.0f);
                                });

                            }
                            //3장 모드의 버튼 활성화는 Broadcast_ShowCards2DeleteISR 패킷을 받고 나서 처리
                        }
                        StartCoroutine(specialRuneEnumerator());
                    }
                }
                break;
            case ePacketType_InGameServer.S2UAsk_ExchangeCardWithOpponentInSpecialRule:
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

                    InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                    {                            
                        //거절
                        Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExchangeCardWithOpponentISR);
                        Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                        Writer.WriteInt(InGameManager.Instance.RoomID);
                        Writer.WriteBool(false); //거절
                        Writer.SendPacket(ServerPeer);

                        InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                        InGameManager.Instance.LocalPlayerUIDrawer.ReStart30sClock2GoInSpecialRule();
                    });
                    //내가 요청받은거임
                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenterWithButtons(ConstStrings.Message_OpponentAskedExchangeCard, 8, ConstStrings.Message_Accept, ConstStrings.Message_Discard,
                        () =>
                        {
                            InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                            //수락
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExchangeCardWithOpponentISR);
                            Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(true); //수락
                            Writer.SendPacket(ServerPeer);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                            //교환할 카드를 선택
                            InGameManager.Instance.LocalPlayerUIDrawer.SelectCard2Exchange((card) =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SRequest_OpponentSelectedCardToExchangeISR);
                                Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                                Writer.WriteInt(InGameManager.Instance.RoomID);
                                Writer.WriteByte((byte)card.Type);
                                Writer.WriteByte((byte)card.Value);
                                Writer.SendPacket(ServerPeer);
                            });
                        },
                        () =>
                        {
                            InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                            //거절
                            Writer.CreateNewPacket((byte)ePacketType_InGameServer.S2UResponse_WillAcceptExchangeCardWithOpponentISR);
                            Writer.WriteInt(InGameManager.Instance.LocalPlayer.ID);
                            Writer.WriteInt(InGameManager.Instance.RoomID);
                            Writer.WriteBool(false); //거절
                            Writer.SendPacket(ServerPeer);

                            InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                            InGameManager.Instance.LocalPlayerUIDrawer.ReStart30sClock2GoInSpecialRule();
                        });
                }
                break;
            case ePacketType_InGameServer.Broadcast_IsOpponentAcceptedCardExchangeISR:
                //상대방이 카드 교환 수락/거절 버튼을 누른 후에 이 패킷이 날라옴.
                {
                    int requesterID = reader.ReadInt();
                    int opponentId = reader.ReadInt();
                    bool isAccepted = reader.ReadBool();

                    if (InGameManager.Instance.LocalPlayer.ID == requesterID)
                    {
                        //내가 보낸 요청임
                        if (isAccepted)
                        {
                            //교환 수락
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.message_WhenAccepted, 0);
                            InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId).SetIOText(ConstStrings.Message_Accept);
                        }
                        else
                        {
                            InGameManager.Instance.LocalPlayerUIDrawer.ReStart30sClock2GoInSpecialRule();
                            //교환 거절
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.message_WhenRejected, 0);

                            //상대 Drawer UI 수정
                            var rp = InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId);
                            string originText = rp.GetIOText();
                            rp.SetIOText(ConstStrings.Message_Discard);
                            DelayedFunctionHelper.InvokeDelayed(() =>
                            {
                                rp.SetIOText(originText);
                                InGameManager.Instance.LocalPlayerUIDrawer.AlreadyExchangedWithOpponent = true;
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

            case ePacketType_InGameServer.Broadcast_ExchangeWithOpponentInSpecialRuleResult:
                //스페셜 룰에서 최종적으로 카드 교환이 이루어져야 할 때 이 패킷이 날라옴.
                {
                    int id1 = reader.ReadInt();
                    byte type1 = reader.ReadByte();
                    byte value1 = reader.ReadByte();
                    int id2 = reader.ReadInt();
                    byte type2 = reader.ReadByte();
                    byte value2 = reader.ReadByte();

                    Card card1 = new Card((Card.CardType)type1, value1);
                    Card card2 = new Card((Card.CardType)type2, value2);
                    int localId = InGameManager.Instance.LocalPlayer.ID;

                    if (id1 != localId && id2 != localId) break;

                    InGameManager.Instance.LocalPlayerUIDrawer.ReStart30sClock2GoInSpecialRule();

                    // 변수 정리
                    Card.CardType outType; byte outValue;
                    Card localInCard; int opponentId;

                    if (id1 == localId) { outType = card1.Type; outValue = card1.Value; localInCard = card2; opponentId = id2; }
                    else { outType = card2.Type; outValue = card2.Value; localInCard = card1; opponentId = id1; }

                    Card localOutCard = InGameManager.Instance.LocalPlayer.ThisDeck.GetCardByTypeAndValue(outType, outValue);
                    if (localOutCard == null) break;

                    var oppDrawer = InGameManager.Instance.FindDrawerByIDExceptLocal(opponentId);
                    var opponentTempCard = oppDrawer.Target.ThisDeck.GetCard(1);

                    // --- [애니메이션 위치 저장] ---
                    int originSiblingIndex = localOutCard.CardGameObject.transform.GetSiblingIndex();
                    Vector3 oppOriginPos = opponentTempCard.CardGameObject.GetMoverPosition();
                    Vector3 oppOriginScl = opponentTempCard.CardGameObject.GetMoverScale();
                    Vector3 localOriginPos = localOutCard.CardGameObject.GetMoverPosition();
                    Vector3 localOriginScl = localOutCard.CardGameObject.GetMoverScale();

                    // 1. 내 덱 처리 (나가는 카드 삭제 -> 여기서 CardGameObject가 파괴될 수 있음)
                    InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(localOutCard);
                    InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(localInCard);

                    // 2. 상대 덱 처리 (임시 카드 삭제, *복사본* 추가)
                    oppDrawer.Target.ThisDeck.RemoveCard(opponentTempCard);
                    Card opponentCopyCard = new Card(localOutCard.Type, localOutCard.Value); 
                    oppDrawer.Target.ThisDeck.AddCard(opponentCopyCard); 

                    // 3. UI 갱신
                    InGameManager.Instance.LocalPlayerUIDrawer.UpdateCardsLayout();
                    oppDrawer.UpdateCardsLayout();

                    // 4. 새로 받은 카드 위치 잡기
                    if (localInCard.CardGameObject != null)
                    {
                        localInCard.CardGameObject.transform.SetSiblingIndex(originSiblingIndex);
                        localInCard.CardGameObject.SetMovementTransformPosition(oppOriginPos);
                        localInCard.CardGameObject.SetMovementTransformScale(oppOriginScl);
                        localInCard.CardGameObject.SetFace(false);
                    }

                    // 5. 나가는 카드 처리 (이미 파괴되었을 수 있으므로 null 체크)
                    if (localOutCard.CardGameObject != null)
                    {
                        localOutCard.CardGameObject.SetMovementTransformPosition(localOriginPos);
                        localOutCard.CardGameObject.SetMovementTransformScale(localOriginScl);
                        localOutCard.CardGameObject.SetFace(true);
                    }

                    // 서버 응답
                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2SResponse_SuccessfullyExchangedCardWithOpponentInSpecialRule);
                    Writer.WriteInt(localId);
                    Writer.WriteInt(InGameManager.Instance.RoomID);
                    Writer.SendPacket(ServerPeer);

                    // --- [애니메이션 및 게임 재개] ---
                    const float moveDur = 0.75f;
                    const float faceFlipDelay = 0.5f;

                    IEnumerator AnimateSwap()
                    {
                        yield return new WaitForEndOfFrame();

                        // 내 카드(받은 것) 이동
                        if (localInCard.CardGameObject != null)
                        {
                            localInCard.CardGameObject.MoveMovementTransformPosition(Vector3.zero, moveDur, ePosition.Local);
                            localInCard.CardGameObject.MoveMovementTransformScale(localOriginScl, moveDur);
                            DelayedFunctionHelper.InvokeDelayed(() => {
                                if(localInCard.CardGameObject != null) 
                                    localInCard.CardGameObject.SetFaceAnimated(true, 1.0f, 0.4f);
                            }, faceFlipDelay);
                        }

                        // 상대 카드(보낸 것) 이동 - 살아있다면 이동
                        if (localOutCard.CardGameObject != null)
                        {
                            localOutCard.CardGameObject.MoveMovementTransformPosition(Vector3.zero, moveDur, ePosition.Local);
                            localOutCard.CardGameObject.MoveMovementTransformScale(oppOriginScl, moveDur);
                            DelayedFunctionHelper.InvokeDelayed(() => {
                                if(localOutCard.CardGameObject != null) 
                                    localOutCard.CardGameObject.SetFaceAnimated(false, 1.2f, 0.4f);
                            }, faceFlipDelay);
                        }

                        // ★ [핵심 수정] 게임 재개 코드를 if문 밖으로 뺐습니다.
                        // 카드가 파괴되었든 아니든, 시간이 지나면 무조건 UI가 풀리도록 보장합니다.
                        DelayedFunctionHelper.InvokeDelayed(() => {
                            InGameManager.Instance.LocalPlayerUIDrawer.AlreadyExchangedWithOpponent = true;
                            InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                            InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                        }, faceFlipDelay);
                    }
                    StartCoroutine(AnimateSwap());

                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                    QuestController.OnExchangeCardWithOpponent();
                }
                break;

            case ePacketType_InGameServer.S2UResponse_ExchangeCardWithDeckInSpecialRule:
                //스페셜 룰에서 덱과 카드 교환을 요청했을 때, 이 패킷이 날라옴.
                {
                    int id = reader.ReadInt();
                    if (id == InGameManager.Instance.LocalPlayer.ID)
                    {
                        //내가 요청한 교환임
                        Card card = new Card((Card.CardType)reader.ReadByte(), reader.ReadByte());
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            try // [안전장치]
                            {
                                // 1. 유효성 검사 (실패 시 에러를 던져서 catch 블록으로 이동시킴 -> UI 복구 보장)
                                if (InGameManager.Instance.LocalPlayer.Card2Exchange == null)
                                {
                                    throw new Exception("교환할 카드(Card2Exchange)가 null입니다.");
                                }
                                if (InGameManager.Instance.LocalPlayer.Card2Exchange.CardGameObject == null)
                                {
                                    // 만약 GameObject가 없다면, 덱의 첫 번째 카드로 대체 시도 (최후의 수단)
                                    var fallbackCard = InGameManager.Instance.LocalPlayer.ThisDeck.GetCard(0);
                                    if (fallbackCard != null && fallbackCard.CardGameObject != null)
                                    {
                                        Debug.LogWarning("[Warning] Card2Exchange GameObject missing. Using fallback card.");
                                        InGameManager.Instance.LocalPlayer.Card2Exchange = fallbackCard;
                                    }
                                    else
                                    {
                                        throw new Exception("교환할 카드의 GameObject가 없고, 대체할 카드도 없습니다.");
                                    }
                                }

                                int originIdx = InGameManager.Instance.LocalPlayer.Card2Exchange.CardGameObject.transform.GetSiblingIndex();
                                
                                // 2. 덱 데이터 갱신
                                InGameManager.Instance.LocalPlayer.ThisDeck.RemoveCard(InGameManager.Instance.LocalPlayer.Card2Exchange);
                                InGameManager.Instance.LocalPlayer.ThisDeck.AddCard(card);

                                // 3. UI 및 애니메이션 처리
                                if(card.CardGameObject != null)
                                {
                                    card.CardGameObject.transform.SetSiblingIndex(originIdx);
                                    InGameManager.Instance.LocalPlayerUIDrawer.UpdateCardsLayout();
                                    
                                    // 애니메이션
                                    InGameManager.Instance.LocalPlayerUIDrawer.MoveCardPosition_FromDeckPosition2LocalPosition(card, 0.75f);

                                    // 스케일 애니메이션
                                    Vector3 originScale = card.CardGameObject.GetMoverScale();
                                    card.CardGameObject.SetMovementTransformScale(originScale * 0.5f);
                                    card.CardGameObject.MoveMovementTransformScale(originScale, 0.75f);
                                }
                                else
                                {
                                     InGameManager.Instance.LocalPlayerUIDrawer.UpdateCardsLayout();
                                }

                                // 4. 마무리 (성공 시)
                                DelayedFunctionHelper.InvokeDelayed(() =>
                                {
                                    if (card.CardGameObject != null)
                                        card.CardGameObject.SetFaceAnimated(true, 1.2f, 0.4f);
                                    
                                    // ★ 여기가 실행되어야 멈춤이 풀립니다.
                                    InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                                    InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                                }, 0.9f);
                                
                                DelayedFunctionHelper.InvokeDelayed(() =>
                                {
                                    FindObjectOfType<CardDeck>().PlayShuffleAnimation();
                                }, 1.0f);
                            }
                            catch (System.Exception e)
                            {
                                // ★ 에러 발생 시 여기서 UI를 강제로 복구하여 멈춤 방지
                                Debug.LogError($"[Exception] 덱 교환 중 오류 발생: {e.Message}\n{e.StackTrace}");
                                InGameManager.Instance.LocalPlayerUIDrawer.RollBackSpecialRuleExchangeButtons();
                                InGameManager.Instance.LocalPlayerUIDrawer.ActivateGoButton();
                            }

                        }, 0.8f);
                    }
                    else
                    {
                        // 상대방이 교환한 경우 (애니메이션만)
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            var rp = InGameManager.Instance.FindDrawerByIDExceptLocal(id);
                            var c = rp.Target.ThisDeck.GetCard(1); // 상대방 덱의 아무 카드나 잡아서 애니메이션
                            
                            if(c != null && c.CardGameObject != null)
                            {
                                rp.MoveCardPosition_FromDeckPosition2LocalPosition(c, 0.75f);
                            }
                            
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
                    if(reason == 125)
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_AllHaveSameScore, 0);
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            InGameManager.Instance.StartNextRound(reason);

                            QuestController.ResetValuesPerRound();
                        }, 2.0f);
                    }
                    else
                    {
                        InGameManager.Instance.StartNextRound(reason);

                        QuestController.ResetValuesPerRound();
                    }
                }
                break;
            case ePacketType_InGameServer.Broadcast_FinalResult:
                {
                    int loserID = reader.ReadInt();
                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_FinalLoser, InGameManager.Instance.FindDrawerByID(loserID).GetNickName()), InGameManager.Instance.LocalPlayerUIDrawer.Character + 5);

                    ///////////////////////////////////////////////////////////////////////////////
                    ///퀘스트 처리
                    if (loserID == InGameManager.Instance.LocalPlayer.ID)
                        QuestController.OnLoseTotalGame();
                    else
                        QuestController.OnWinTotalGame();

                    AchievementManager.AddToStat_INT(StatsId.GAMES_FINISHED, 1);
                    ///////////////////////////////////////////////////////////////////////////////

                    DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            OnFinalGameEnd.Play();
                            InGameManager.Instance.LocalPlayerUIDrawer.StartClock_10s(() =>
                            {
                                Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2S_DisagreedReGame);
                                Writer.WriteInt(Id);
                                Writer.SendPacket(ServerPeer);

                                InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                                InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_WaitingReGame, 0);
                            });
                            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenterWithButtons(ConstStrings.Message_ReGame, 0, ConstStrings.Text_ReGame_Yes, ConstStrings.Text_ReGame_No,
                                () =>
                                {
                                    InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2S_AgreedReGame);
                                    Writer.WriteInt(Id);
                                    Writer.SendPacket(ServerPeer);

                                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_WaitingReGame, 0);
                                },
                                () =>
                                {
                                    InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
                                    Writer.CreateNewPacket((byte)ePacketType_InGameServer.U2S_DisagreedReGame);
                                    Writer.WriteInt(Id);
                                    Writer.SendPacket(ServerPeer);

                                    InGameManager.Instance.LocalPlayerUIDrawer.SetActivePanelOnScreenCenterWithButtons(false);
                                    InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_WaitingReGame, 0);
                                });
                        }, 3.0f);
                }
                break;
            case ePacketType_InGameServer.Broadcast_ReGameResult:
                {
                    int playerCountAgreedReGame = reader.ReadByte();
                    Debug.Log(playerCountAgreedReGame + "명의 플레이어가 재게임에 동의");
                    if(playerCountAgreedReGame == 100)
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_SomeoneDisagreedReGame, 0);
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            PhotonNetwork.LeaveRoom();
                            PhotonNetwork.LoadLevel("Lobby");
                        }, 3.0f);
                    }
                    else if(playerCountAgreedReGame == 3)
                    {
                        InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_AllAgreedReGame, 0);
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            PhotonNetwork.LoadLevel("MainPlayScene");
                            IsLoaded = false;
                        }, 3.0f);
                    }
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
            InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(ConstStrings.Message_SelectOut, 0);
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
                InGameManager.Instance.LocalPlayerUIDrawer.StopClock();
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

            InGameManager.Instance.LocalPlayerUIDrawer.StartClock_30s(() =>
            {
                TimeLimitCounter++;
                if (CheckMyTimeLimitCounter()) return;
                switch((eIO)UnityEngine.Random.Range(0, 2))
                {
                    case eIO.In:
                        InGameManager.Instance.LocalPlayerUIDrawer.InOutButton_In.onClick.Invoke();
                        break;
                    case eIO.Out:
                        InGameManager.Instance.LocalPlayerUIDrawer.InOutButton_Out.onClick.Invoke();
                        break;
                }
                InGameManager.Instance.LocalPlayerUIDrawer.ShowPanelOnScreenCenter(string.Format(ConstStrings.Message_TimeOutCount, TimeLimitCounter), 0);
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
        Debug.Log($"[Chat] Received message from server: {message}");
        if(!(PlayerPrefs.GetInt("IsActive_TextChat", 1) == 1)) //1이면 작동함
        {
            return;
        }
        OnChatReceived?.Invoke(id, message);
        FindObjectOfType<Chating>().OnChat(InGameManager.Instance.FindDrawerByID(id).GetNickName(), message);
    }

    internal void SendVoice2Server(byte[] bytes)
    {
        if(PhotonNetwork.InRoom == false)
            return;
        Writer.CreateNewPacket((byte)ePacketType_InGameServer.Voice_Send);
        Writer.WriteInt(Id);
        Debug.Log("Sending voice data, bytes length: " + bytes.Length);
        Writer.WriteInt(bytes.Length);
        Writer.WriteByteArray(bytes);
        Writer.SendPacket(ServerPeer);
    }

    public Action<int, string> OnChatReceived;
}
