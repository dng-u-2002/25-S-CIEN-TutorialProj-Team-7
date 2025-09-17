using System;
using System.Collections;
using System.Text;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HTTPLoginManager : MonoBehaviour
{
    public static HTTPLoginManager Instance { get; private set; }
    
    [Header("Server Settings")]
    public string serverURL = "http://localhost:8080";
    
    [Header("Login UI")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public TMPro.TMP_InputField loginIdInput;
    public TMPro.TMP_InputField loginPasswordInput;
    public Button loginButton;
    public Button showRegisterButton;
    public TMPro.TMP_Text loginErrorText;
    
    [Header("Register UI")]
    public TMPro.TMP_InputField regEmailInput;
    public TMPro.TMP_InputField regLoginIdInput;
    public TMPro.TMP_InputField regPasswordInput;
    public TMPro.TMP_InputField regNameInput;
    public Button registerButton;
    public Button showLoginButton;
    public TMPro.TMP_Text registerErrorText;
    
    [Header("Loading UI")]
    public GameObject loadingPanel;
    public TMPro.TMP_Text loadingText; 
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {

    
    }

    bool LoginFlag = false;
    private void Update()
    {
        if (PhotonNetwork.IsConnectedAndReady == false)
        {
            return;
        }
        if(LoginFlag == true)
        {
            return;
        }
        var id = PlayerPrefs.GetString("LoginId", "NULL");
        var name = PlayerPrefs.GetString("Nickname", "Guest User");
        if (id == "NULL")
        {
            SetupUI();
            //ShowLoginPanel();
        }
        else
        {
            OnLoginSuccess(id, name);
        }
        LoginFlag = true;
    }

    private void SetupUI()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        showRegisterButton.onClick.AddListener(ShowRegisterPanel);
        showLoginButton.onClick.AddListener(ShowLoginPanel);
        
        ClearErrorMessages();
        HideLoadingPanel();
    }
    
    public void OnLoginButtonClicked()
    {
        string loginId = loginIdInput.text.Trim();
        string password = loginPasswordInput.text.Trim();
        
        if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("아이디와 비밀번호를 입력해주세요.");
            return;
        }
        
        StartCoroutine(LoginCoroutine(loginId, password));
    }
    
    public void OnRegisterButtonClicked()
    {
        string email = regEmailInput.text.Trim();
        string loginId = regLoginIdInput.text.Trim();
        string password = regPasswordInput.text.Trim();
        string name = regNameInput.text.Trim();
        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(loginId) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
        {
            ShowRegisterError("모든 필드를 입력해주세요.");
            return;
        }
        
        StartCoroutine(RegisterCoroutine(email, loginId, password, name));
    }
    
    private IEnumerator LoginCoroutine(string loginId, string password)
    {
        ShowLoadingPanel("로그인 중...");
        ClearErrorMessages();
        
        string jsonData = $"{{\"loginId\":\"{loginId}\",\"password\":\"{password}\"}}";
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/login", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            HideLoadingPanel();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200)
                {
                    string responseText = request.downloadHandler.text;
                    OnLoginSuccess(loginId, ExtractNameFromResponse(responseText));
                }
                else
                {
                    ShowLoginError("로그인에 실패했습니다.");
                }
            }
            else
            {
                HandleLoginError(request);
            }
        }
    }
    
    private IEnumerator RegisterCoroutine(string email, string loginId, string password, string name)
    {
        ShowLoadingPanel("회원가입 중...");
        ClearErrorMessages();
        
        string jsonData = $"{{\"email\":\"{email}\",\"loginId\":\"{loginId}\",\"password\":\"{password}\",\"name\":\"{name}\"}}";
        
        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/register", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            HideLoadingPanel();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200 || request.responseCode == 201)
                {
                    OnRegisterSuccess();
                }
                else
                {
                    ShowRegisterError("회원가입에 실패했습니다.");
                }
            }
            else
            {
                HandleRegisterError(request);
            }
        }
    }
    
    private string ExtractNameFromResponse(string jsonResponse)
    {
        try
        {
            int nameStart = jsonResponse.IndexOf("\"name\":\"") + 8;
            int nameEnd = jsonResponse.IndexOf("\"", nameStart);
            return jsonResponse.Substring(nameStart, nameEnd - nameStart);
        }
        catch
        {
            return "User";
        }
    }

    System.Random rnd = new();
    public void SuccessLogin_EDITOR()
    {
        OnLoginSuccess("LoginWithoutAccount", "Test User" + rnd.Next().ToString());
    }
    private void OnLoginSuccess(string loginId, string name)
    {
        PlayerPrefs.SetString("LoginId", loginId);
        PlayerPrefs.SetString("Nickname", name);
        PlayerPrefs.SetString("LoginTime", DateTime.Now.ToString());
        PlayerPrefs.Save();
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LocalPlayer.NickName = name;
            Debug.Log("PhotonNetwork NickName set to: " + PhotonNetwork.NickName);
        }
        
        HideAllPanels();
        
        if (GameLobbyManager.Instance != null)
        {
            GameLobbyManager.Instance.OnLoginSuccess();
        }
        
        gameObject.SetActive(false);
    }
    
    private void OnRegisterSuccess()
    {
        ClearRegisterInputs();
        ShowLoginPanel();
        ShowLoginError("회원가입이 완료되었습니다. 로그인해주세요.", Color.green);
    }
    
    private void HandleLoginError(UnityWebRequest request)
    {
        if (request.responseCode == 401)
        {
            ShowLoginError("아이디 또는 비밀번호가 틀렸습니다.");
        }
        else if (request.responseCode == 400)
        {
            ShowLoginError("입력 정보를 확인해주세요.");
        }
        else
        {
            ShowLoginError("서버 연결에 실패했습니다.");
        }
    }
    
    private void HandleRegisterError(UnityWebRequest request)
    {
        if (request.responseCode == 409)
        {
            ShowRegisterError("이미 존재하는 아이디입니다.");
        }
        else if (request.responseCode == 400)
        {
            ShowRegisterError("입력 정보를 확인해주세요.");
        }
        else
        {
            ShowRegisterError("서버 연결에 실패했습니다.");
        }
    }
    
    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        loginPanel.transform.localPosition = Vector3.zero;
        ClearErrorMessages();
    }
    
    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        registerPanel.transform.localPosition = Vector3.zero;
        ClearErrorMessages();
    }
    
    private void HideAllPanels()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
    }
    
    private void ShowLoadingPanel(string message)
    {
        loadingPanel.SetActive(true);
        loadingText.text = message;
        loadingPanel.transform.localPosition = Vector3.zero;
    }
    
    private void HideLoadingPanel()
    {
        loadingPanel.SetActive(false);
    }
    
    private void ShowLoginError(string message, Color? color = null)
    {
        loginErrorText.text = message;
        loginErrorText.color = color ?? Color.red;
    }
    
    private void ShowRegisterError(string message)
    {
        registerErrorText.text = message;
        registerErrorText.color = Color.red;
    }
    
    private void ClearErrorMessages()
    {
        loginErrorText.text = "";
        registerErrorText.text = "";
    }
    
    private void ClearRegisterInputs()
    {
        regEmailInput.text = "";
        regLoginIdInput.text = "";
        regPasswordInput.text = "";
        regNameInput.text = "";
    }
    
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString("LoginId", ""));
    }
    
    public string GetLoggedInUserId()
    {
        return PlayerPrefs.GetString("LoginId", "");
    }
    
    public string GetLoggedInUserName()
    {
        return PlayerPrefs.GetString("Nickname", "");
    }
    
    public void Logout()
    {
        PlayerPrefs.DeleteKey("LoginId");
        PlayerPrefs.DeleteKey("Nickname");
        PlayerPrefs.DeleteKey("LoginTime");
        PlayerPrefs.Save();
        
        ShowLoginPanel();
        gameObject.SetActive(true);
    }
}