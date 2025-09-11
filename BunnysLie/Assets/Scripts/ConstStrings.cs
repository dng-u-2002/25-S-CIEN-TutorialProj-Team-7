public static class ConstStrings
{
    public const string Message_WaitingOpponent = "상대의 응답을 기다리는중..."; //상대에게 카드 교환 요청을 보냈을 때 기다리는 메세지
    public const string Message_Waiting = "기다리는 중..."; //가위바위보 및 그냥 기다릴 때 나오는 메세지
    public const string Message_LoserOfThisRound = "{0}라운드 패자 : {1}"; //이번 라운드의 패자
    public const string Message_TimeOutCount = "제한 시간이 초과되어 랜덤으로 선택됩니다! \n현재 시간 초과 횟수 : {0}/4";
    public const string message_WhenLocalShouldRematch = "리매치에 참여합니다.\n가위바위보를 다시 선택해주세요.";
    public const string message_WhenLocalShouldNotRematch = "리매치에 참여하지 않습니다.\n다른 플레이어가 리매치 중입니다.";
    public const string Message_NewSpecialRule = "이전 스페셜 룰에서 동점자가 발생했습니다.\n새로운 스페셜 룰을 시작합니다.";
    public const string Message_StartSpecialRuleObserver = "{0}와 {1}가 동률입니다.\n1 vs. 1 룰에 관전자로 돌입합니다.";
    public const string Message_StartSpecialRulePlayer = "{0}와 {1}가 동률입니다.\n1 vs. 1 룰로 돌입합니다.";
    public const string Message_OpponentAskedExchangeCard = "상대가 카드 교환을 요청했습니다";
    public const string message_WhenAccepted = "상대가 교환을 수락했습니다.\n카드 선택을 기다리는 중...";
    public const string message_WhenRejected = "상대가 교환을 거절했습니다.";
    public const string Message_Accept = "수락";
    public const string Message_Discard = "거절";
    public const string Message_FinalLoser = "패자 : {0}\n게임이 종료되었습니다.";
    public const string Message_TimeOut = "제한 시간 초과 횟수가 4회를 넘었습니다.\n게임에서 퇴장합니다.";
    public const string Message_NotEnoughPlayer = "다른 플레이어가 게임을 떠났습니다.\n플레이어가 부족하여 게임이 종료됩니다.";
    public const string Message_SelectOut = "2명이 먼저 퇴청하였습니다.\nIN을 선택합니다.";
    //TextCloud : 말풍선
    public const string TextCloud_Card2Exchange = "어떤 카드를\n교환할까?";
    public const string TextCloud_Card2Delete = "어떤 카드를 버릴까?";
    //Text : 참가/퇴청 보여주는 작은 텍스트블록에 나오는 글
    public const string Text_In = "참가";
    public const string Text_Out = "퇴청";
}