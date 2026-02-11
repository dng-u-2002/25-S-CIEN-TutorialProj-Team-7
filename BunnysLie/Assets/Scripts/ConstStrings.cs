public static class ConstStrings
{
    public const string Message_WaitingOpponent = "상대의 응답을 기다리는중..."; //상대에게 카드 교환 요청을 보냈을 때 기다리는 메세지
    public const string Message_Waiting = "기다리는 중..."; //그냥 기다릴 때 나오는 메세지
    public const string Message_WaitingRSP = "다른 플레이어가\n가위바위보를 선택하고 있습니다..."; //가위바위보 기다릴 때 나오는 메세지
    public const string Message_LoserOfThisRound = "{0}라운드 패자 : {1}"; //이번 라운드의 패자
    public const string Message_TimeOutCount = "제한 시간 초과! 랜덤으로 선택할게요!\n현재 시간 초과 횟수 : {0}/4";
    public const string message_WhenLocalShouldRematch = "무승부!\n가위바위보를 다시 선택해주세요.";
    public const string message_WhenLocalShouldNotRematch = "다른 플레이어가 가위바위보 중입니다.\n나는 {0}번째로 참가/퇴청을 정해요.";
    public const string Message_NewSpecialRule = "이전 1 vs 1에서 동점자가 발생했어요.\n1vs1을  재시작!";
    public const string Message_StartSpecialRuleObserver = "{0}와 {1}가 동점!\n1 vs. 1 룰에 관전자로 돌입해요.";
    public const string Message_StartSpecialRulePlayer = "{0}와 {1}가 동점!\n1 vs. 1 룰로 승부합시다!";
    public const string Message_OpponentAskedExchangeCard = "상대가 카드 교환을 요청했어요.";
    public const string message_WhenAccepted = "상대가 교환을 수락했어요.\n카드 선택을 기다리는 중...";
    public const string message_WhenRejected = "상대가 교환을 거절했어요.";
    public const string Message_Accept = "수락";
    public const string Message_Discard = "거절";
    public const string Message_FinalLoser = "꼴찌 : {0}\n게임이 종료!";
    public const string Message_TimeOut = "제한 시간 초과 횟수가 4회를 넘었어요.\n게임에서 퇴장합니다.";
    public const string Message_NotEnoughPlayer = "다른 플레이어가 게임을 떠났어요.\n플레이어가 부족하여 게임을 종료할게요.";
    public const string Message_SelectOut = "2명이 먼저 퇴청하였으므로,\n자동으로 참가합니다. 난 꼴찌 탈출!";
    public const string Message_AllHaveSameScore = "모든 플레이어가 동점이에요.\n다음 라운드로 넘어갑니다.";
    //TextCloud : 말풍선
    public const string TextCloud_Card2Exchange = "어떤 카드를\n교환할까?";
    public const string TextCloud_Card2Delete = "어떤 카드를 버릴까?";
    //Text : 참가/퇴청 보여주는 작은 텍스트블록에 나오는 글
    public const string Text_In = "참가";
    public const string Text_Out = "퇴청";

    public const string Message_ReGame = "다시 게임할까요?";
    public const string Text_ReGame_Yes = "이어하기";
    public const string Text_ReGame_No = "나가기";
    public const string Message_WaitingReGame = "다른 플레이어가\n이어하기를 선택하고 있습니다...";
    public const string Message_SomeoneDisagreedReGame = "다른 플레이어가\n이어하기를 거절했습니다.";
    public const string Message_AllAgreedReGame = "모든 플레이어가\n이어하기를 선택했습니다!";
}