using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

using Random = UnityEngine.Random;

public class UserJsonData
{
    public List<string> COLUMNS { get; set; }
    public List<List<object>> DATA { get; set; }
}
public enum Direction
{
    Left,
    Right
}
public enum ColorBallType
{
    Orange,
    Red,
    Mint,
    Green,
    Pink,
    Yellow
}

public class Player
{
    public const int noneAnswer = -1;

    public Player(string name, Direction direction, string colorCode, int pieceCount, bool isClear)
    {
        _firstName = name;
        _direction = direction;
        _colorBallType = (ColorBallType)Enum.Parse(typeof(ColorBallType), colorCode);
        LedTagIndex = 0;

        Debug.Log($"_colorBallType /  {_colorBallType}");
        _pieceCount = pieceCount;
        Score = 0;
        SetAnswers();
        Debug.Log($"{_firstName}의 색상 타입이{colorCode} /  {_colorBallType}로 설정되었습니다. {_pieceCount}개의 피스를 가지고 있습니다.");

        IsAllContentPlayed = isClear;

    }
    string _firstName;


    ColorBallType _colorBallType;
    string _lastName;
    bool _isReady = false;

    bool isAllContentPlayed = false;

    int _ledTagIndx = 0;


    int _score = 0;

    int _playedContentCount = 0;

    int _pieceCount = 0;

    int _addPiece = 0;

    int[] _answers;

    bool[] scores = new bool[200];



    string passCode;

    Direction _direction;

    #region Properties
    public ColorBallType ColorBallType
    {
        get { return _colorBallType; }
        set { _colorBallType = value; }
    }

    public void SetAnswers()
    {
        Debug.Log("질문 수에 맞춰 답변 배열 초기화: " + QuestionManager.Instance.QuestionInfos.Count);
        _answers = new int[QuestionManager.Instance.QuestionInfos.Count];


        for (int i = 0; i < _answers.Length; i++)
        {
            _answers[i] = noneAnswer;
        }
    }
    public int AddScores()
    {
        scores[LedTagIndex] = true;
        return CurrentScore();
    }
    public bool[] Scores
    {
        get { return scores; }
        set { scores = value; }
    }

    public string[] CartridgeContent { get; set; }

    public int CurrentScore()
    {
        int score = 0;
        foreach (var s in scores)
        {
            if (s) score++;
        }
        Debug.Log($"{_lastName}의 현재 점수 계산: {score}점");
        return score;

    }

    public int[] Answers
    {
        get { return _answers; }
        set
        {

            _answers = value;
        }
    }
    public int AddPiece
    {
        get { return _addPiece; }
        set { _addPiece = value; }
    }

    public int PieceCount
    {
        get { return _pieceCount; }
        set { _pieceCount = value; }
    }

    public string PassCode
    {
        get { return passCode; }
        set { passCode = value; }
    }

    public Queue<string> QuestionAnswerData = new Queue<string>();

    public string FirstName
    {
        get { return _firstName; }
        set { _firstName = value; }
    }

    public string LastName
    {
        get { return _lastName; }
        set { _lastName = value; }
    }
    public Direction Direction
    {
        get { return _direction; }
        set { _direction = value; }
    }
    public bool IsReady
    {
        get { return _isReady; }
        set { _isReady = value; }
    }

    public bool IsAllContentPlayed
    {
        get { return isAllContentPlayed; }
        set { isAllContentPlayed = value; }
    }
    public int Score
    {
        get { return _score; }
        set
        {
            _score = value;
            Debug.Log($"{_lastName}의 점수가 {_score}로 설정되었습니다.");
        }
    }
    public int LedTagIndex
    {
        get { return _ledTagIndx; }
        set { _ledTagIndx = value; }
    }

    public int PlayedContentCount
    {
        get { return _playedContentCount; }
        set { _playedContentCount = value; }
    }
    #endregion




}

public class UserDataManager : MonoBehaviour
{

    private static UserDataManager instance;

    public static UserDataManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<UserDataManager>();
            return instance;
        }
    }

    private Dictionary<string, string> userDataCache = null;

    private bool isCurrentSessionEmpty = false;
    Player[] players = new Player[2];

    public string[] contentCodes;


    bool _isUsingRoom = false;

    public bool IsUsingRoom
    {
        //Todo 이걸로 둘 다 태그하세요 값 설정
        get { return _isUsingRoom; }
        set { _isUsingRoom = value; }
    }

    const int PlayAbleContentNum = 4;
    private Action onUserUIDSet;

    public Direction CurrentDirection = Direction.Left;

    Coroutine userInitializeCoroutine = null;

    const int contentNum = 4;

    public int ContentNum { get { return contentNum; } }



    public int[] stamp { get; private set; } = new int[contentNum];

    public int deviceNum = 1;

    int _lastAddScoreIndex = -1;

    void Start()
    {
        ArduinoTouchManager.Instance.OnAllPlayerTouchStateChanged += Page5DoubleCheck;

    }

    public void AddUserUIDSet(Action action)
    {
        onUserUIDSet += action;
    }

    public string FindValue(string _Key)
    {
        if (userDataCache == null)
        {
            Debug.Log(" userDataCache = null");
            return null;
        }
        return userDataCache[_Key];
    }


    public IEnumerator RequestUserDataUpdate(int _question, int _value, Direction direction)
    {
        string side = "";
        //http://192.168.0.252:8500/api/getUser.cfm?uid=
        if (IsUser() == false)
        {
            Debug.Log("RequestUserDataUpdate: 사용자 데이터가 없습니다. 질문 업데이트를 요청할 수 없습니다.");

            yield break;
        }
        if (direction == Direction.Left)
        {
            side = "left";
        }
        else
        {
            side = "right";
        }
        Debug.Log($"RequestUserDataUpdate: userdata =  {userDataCache["IDX_USER"]} question={_question}, value={_value}, direction={direction}, contentCode={ServerData.Instance.Code}");
        Debug.Log($"http://192.168.0.252:8500/api/updateValue.cfm?idx_user={userDataCache["IDX_USER"]}&q_no={_question}&side={side}&code={ServerData.Instance.Code}&value={_value}");
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/updateValue.cfm?idx_user={userDataCache["IDX_USER"]}&q_no={_question}&side={side}&code={ServerData.Instance.Code}&value={_value}", Answer);
    }
    public IEnumerator IsUserTagRequest()
    {
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/checkRoomState.cfm?code={ServerData.Instance.Code}", RoomUsingTest);
    }
    public IEnumerator ResetUserCoroutine()
    {
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/resetStart.cfm?idx_user={userDataCache["IDX_USER"]}&code={ServerData.Instance.Code}", Answer);

    }
    public IEnumerator RequestUserTagAll()
    {
        yield return StartCoroutine(IsUserTagRequest());
        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        if (_isUsingRoom)
        {
            yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/getCurrentRoomUser.cfm?code={ServerData.Instance.Code}", ParseCurrentSessionData);

        }
    }


    public IEnumerator RequestInitializeUserData(string userUID)
    {
        yield return ServerData.Instance.RequestDataCoroutine("http://211.110.44.104:8500/api/" + $"checkIDX.cfm?uid={userUID}&device={ServerData.Instance.DeviceNum}&Code={ServerData.Instance.Code}", ParseJsonData);
    }

    public IEnumerator RequestInitializeUserDataTest(string userUID)
    {
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/getUser.cfm?uid={userUID}", ParseJsonData);
    }



    public IEnumerator RequestRoomClear()
    {
        if (userDataCache == null) yield break;
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/exitRoom.cfm?code={ServerData.Instance.Code}&idx_user={userDataCache["IDX_USER"]}", Answer);

    }

    public IEnumerator RequestCartridgeInfo()
    {
        if (userDataCache == null) yield break;
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/getCartridgeContent.cfm?cartridge={userDataCache["CARTRIDGE"]}", SetCartridge);
    }

    public IEnumerator RequestPieceDataUpdate()
    {
        if (userDataCache == null) yield break;
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/updatePiece.cfm?idx_user={userDataCache["IDX_USER"]}&code={ServerData.Instance.Code}&value={players[0].AddPiece}", Answer);
    }

    public IEnumerator RequestContentEnd()
    {
        if (userDataCache == null) yield break;
        yield return ServerData.Instance.RequestDataCoroutine($"http://192.168.0.252:8500/api/updateTime.cfm?idx_user={userDataCache["IDX_USER"]}&option=end&code={ServerData.Instance.Code}", Answer);
    }
    public void UserResetRequest()
    {
        if (userDataCache == null) return;
        StartCoroutine(UserStartResetCoroutine());
    }
    public void UserPieceUpdate()
    {
        if (userDataCache == null) return;
        StartCoroutine(RequestPieceDataUpdate());
    }
    IEnumerator UserStartResetCoroutine()
    {
        if (userDataCache == null) yield break;
        yield return StartCoroutine(ResetUserCoroutine());
        yield return StartCoroutine(RequestRoomClear());
        Reset();
        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

        PageController.Instance.RequestResetOpenPage(0);

    }




    public void EndRequest()
    {
        StartCoroutine(EndRequestCoroutine());
    }

    IEnumerator EndRequestCoroutine()
    {
        if (userDataCache == null) yield break;
        yield return StartCoroutine(RequestContentEnd());
        yield return StartCoroutine(RequestRoomClear());
        Reset();
        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        PageController.Instance.RequestResetOpenPage(0);

    }

    public void Answer(string _an)
    {
        Debug.Log("Server : " + _an);
    }
    public void SetCartridge(string _an)
    {
        contentCodes = _an.Split(',');
        for (int i = 0; i < contentCodes.Length; i++)
            contentCodes[i] = contentCodes[i].Trim();

        Debug.Log("CartridgeContent : " + string.Join(", ", contentCodes));

        SetPlayers();

    }

    // public bool IsLastContent()
    // {
    //     string[] contentCodes = { "A1", "B1", "C1", "D1" };

    //     bool result = true;

    //     int clearContentCount = 0;

    //     foreach (var code in contentCodes)
    //     {
    //         if (code == ServerData.Instance.Code)
    //         {
    //             continue; // 현재 콘텐츠는 검사에서 제외
    //         }
    //         string pieceValue = FindValue("PIECE_" + code);
    //         if (pieceValue == null || pieceValue == "null")
    //         {
    //             Debug.Log($"IsLastContent: END_{code} 값이 없습니다.");
    //             result = false;
    //             break;
    //         }
    //         else
    //         {
    //             Debug.Log($"IsLastContent: END_{code} 값 = {pieceValue}");
    //             clearContentCount++;
    //         }

    //     }

    //     if (clearContentCount ==)
    //         return result;
    // }

    public void RoomUsingTest(string message)
    {
        //Debug.Log("RoomUsingTest / Server : " + message);
        string[] lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string trimmed = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("<!--") && line.EndsWith("-->"))
            {
                continue;
            }

            trimmed = line;
            break;
        }
        //Debug.Log("RoomUsingTest / Server : " + trimmed);
        if (trimmed == "EMPTY")
        {
            //Debug.Log("현재 세션 사용자 없음 (EMPTY)");
        }
        else
        {
            _isUsingRoom = true;
            Debug.Log("현재 세션 사용자 있음 (HAS_USER)");

        }

    }


    public bool IsUser()
    {
        if (players[0] != null)
        {
            return true;
        }
        return false;
    }


    public void DebugWWW(string url)
    {
        Debug.Log("Requesting URL: " + url);
    }

    public void ParseJsonData(string jsonText)
    {

        Debug.Log("ParseJsonData : " + jsonText);
        try
        {
            // 우선 클래스로 파싱
            UserJsonData parsedData = JsonConvert.DeserializeObject<UserJsonData>(jsonText);

            if (parsedData == null || parsedData.COLUMNS == null || parsedData.DATA == null || parsedData.DATA.Count == 0)
            {
                Debug.LogError("JSON 구조가 잘못되었습니다.");
                userDataCache = null;
                return; //false;
            }

            List<string> columns = parsedData.COLUMNS;
            List<object> dataRow = parsedData.DATA[0]; // 첫번째 데이터 행을 사용한다고 가정

            if (columns.Count != dataRow.Count)
            {
                Debug.LogError("COLUMNS와 DATA의 개수가 맞지 않습니다.");
                userDataCache = null;
                return;//false;
            }

            // Dictionary 생성
            userDataCache = new Dictionary<string, string>();

            for (int i = 0; i < columns.Count; i++)
            {
                string key = columns[i];
                string value = dataRow[i]?.ToString() ?? "null";
                userDataCache[key] = value;
            }
            if (userDataCache != null && userDataCache.Count > 0)
            {
                if (onUserUIDSet != null) onUserUIDSet.Invoke();
            }
            StartCoroutine(RequestCartridgeInfo());


            LEDData.Instance.SetLedPair();


        }
        catch (JsonException ex)
        {
            Debug.LogError("JSON 파싱 중 에러 발생: " + ex.Message);
            userDataCache = null;
            return;// false;
        }
    }
    public void Page5DoubleCheck(bool touchLeft, bool touchRight)
    {
        if (PageController.Instance.CurrentPage != 5) return;
        if (touchLeft && touchRight)
        {
            if (_lastAddScoreIndex == LEDData.Instance.GetLEDIndex()) return;

            players[0].Score += 1;

            _lastAddScoreIndex = LEDData.Instance.GetLEDIndex();
        }

    }

    public void ParseCurrentSessionData(string responseText)
    {
        //Debug.Log("서버 데이터 : " + responseText);
        if (userDataCache != null)
        {
            return;
        }
        isCurrentSessionEmpty = false;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            //Debug.Log("아직 사용자 두명 태그 안함");
            return;
        }

        string[] lines = responseText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string trimmed = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("<!--") && line.EndsWith("-->"))
            {
                continue;
            }

            trimmed = line;
            break;
        }

        if (string.IsNullOrEmpty(trimmed))
        {
            Debug.LogError("현재 세션 응답에서 유효한 데이터 라인을 찾지 못했습니다.");
            return;
        }

        if (trimmed.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            // isCurrentSessionEmpty = true;
            // currentSessionCache = new Dictionary<string, string>
            // {
            //     { "STATE", "EMPTY" }
            // };
            //Debug.Log("현재 세션 사용자 없음 (EMPTY)");
            return;
        }

        string[] parts = trimmed.Split(',');
        if (parts.Length < 3)
        {
            Debug.LogError("현재 세션 응답 형식 오류: " + trimmed);
            return;
        }

        string idxContentText = parts[2].Trim();
        if (!int.TryParse(idxContentText, out int idxContent))
        {
            Debug.LogError("현재 세션 IDX_CONTENT 파싱 오류: " + idxContentText);
            return;
        }



        userDataCache = new Dictionary<string, string>
        {
            { "UID", parts[0].Trim() },
            { "CODE", parts[1].Trim() },
            { "IDX_CONTENT", idxContent.ToString() },
            { "STATE", "HAS_USER" }
        };
        userInitializeCoroutine = StartCoroutine(RequestInitializeUserDataTest(userDataCache["UID"]));
        LEDData.Instance.SetLedPair();
        _lastAddScoreIndex = -1;

        Debug.Log($"현재 세션 캐시 완료: uid={userDataCache["UID"]}, code={userDataCache["CODE"]}, idx_content={userDataCache["IDX_CONTENT"]}");
    }

    public int GetStamp(int _contentIDX)
    {
        if (stamp.Length > _contentIDX)
        {
            return stamp[_contentIDX];
        }

        else
        {
            Debug.Log("GetStamp Error ");
            return -1;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestKey();
        }
    }
    public void Reset()
    {
        players[0] = null;
        players[1] = null;
        contentCodes = null;
        userDataCache = null;
        _isUsingRoom = false;
    }
    public void TestKey()
    {
        Reset();
        StartCoroutine(RequestInitializeUserDataTest("6733904752"));

        //SetPlayers("길동");
    }

    public void SetPlayers()
    {

        int pieceCount = 0;

        int clearContentCount = 0;
        string pieceValue;

        foreach (var code in contentCodes)
        {
            Debug.Log($"콘텐츠 코드 {code}에 대한 피스 수 계산 시작.");
            pieceValue = FindValue("PIECE_" + code);

            if (string.IsNullOrEmpty(pieceValue) || pieceValue == "null")
            {
                Debug.Log($"코드 {code}의 PIECE_{code} 값이 없습니다. 피스 수 계산에서 0으로 간주됩니다.");
                continue;
            }

            Debug.Log($"코드 {code}의 피스 {pieceValue}");

            if (code == ServerData.Instance.Code)
            {
                Debug.Log($"현재 콘텐츠 코드 {code}는 피스 계산에서 제외됩니다.");
                continue;
            }
            pieceCount += int.TryParse(pieceValue, out int result) ? result : 0;
        }

        bool isLastContent = true;
        foreach (var code in contentCodes)
        {
            if (code == ServerData.Instance.Code)
            {
                Debug.Log($"현재 콘텐츠 코드 {code}는 피스 계산에서 제외됩니다.");
                continue;
            }
            string endValue = FindValue("END_" + code);
            if (string.IsNullOrEmpty(endValue) || endValue == "null")
            {
                Debug.Log($"코드 {code}의 END_{code} 값이 없습니다.마지막 체험이 아님");
                isLastContent = false;
                break;
            }
            else
            {
                Debug.Log($"코드 {code}의 END_{code} 값 = {endValue}");
                clearContentCount++;
            }



        }


        Debug.Log($"총 피스 수 계산: {pieceCount}, 클리어된 콘텐츠 수: {clearContentCount} ");


        players[0] = new Player(FindValue("RESERVATION_FIRST_NAME_LEFT"), Direction.Left, FindValue("COLOR_LEFT"), pieceCount, isLastContent);
        Debug.Log($"players[0] 생성: 이름={players[0].FirstName}, 방향={players[0].Direction}, 색상={players[0].ColorBallType}, 피스 수={players[0].PieceCount}, 모든 콘텐츠 클리어={isLastContent}");
        players[1] = new Player(FindValue("RESERVATION_FIRST_NAME_RIGHT"), Direction.Right, FindValue("COLOR_RIGHT"), pieceCount, isLastContent);
        Debug.Log($"players[1] 생성: 이름={players[1].FirstName}, 방향={players[1].Direction}, 색상={players[1].ColorBallType}, 피스 수={players[1].PieceCount}, 모든 콘텐츠 클리어={isLastContent}");


        players[0].AddPiece = 0;
        players[1].AddPiece = 0;


        QuestionManager.Instance.CurrentIndex = 0;

    }
    public Player GetPlayer(Direction direction)
    {
        if (direction == Direction.Left)
        {
            return players[0];
        }
        else
        {
            return players[1];
        }
    }
    public Player GetPlayer()
    {
        if (CurrentDirection == Direction.Left)
        {
            return players[0];
        }
        else
        {
            return players[1];
        }
    }

    public int GetCurrentPlayersNum()
    {
        int Length = 0;
        foreach (var player in players)
        {
            if (player != null)
            {
                Length++;
            }
        }
        return Length;
    }




}