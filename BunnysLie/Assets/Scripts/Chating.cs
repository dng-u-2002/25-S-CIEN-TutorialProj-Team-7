using TMPro;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;

public class Chating : MonoBehaviour
{
    public bool IsFocusing
    {
        get { return input.isFocused; }
    }
    [SerializeField] TMP_InputField input;
    [SerializeField] TMP_Text text;

    //public NetPeer peer { get; set; }
    //public NetDataWriter writer { get; set; } = new NetDataWriter();

    void Start()
    {
        //채팅창 on/off 동기화
        int isActive = PlayerPrefs.GetInt("IsActive_TextChat", 1); 
        
        if (isActive == 0)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(true);
        }
    }
    
    void Awake() 
    { 
        input = GetComponentInChildren<TMP_InputField>();
        text = GetComponentInChildren<TMP_Text>();
        
        if(input==null||text==null)
            Debug.LogError("Fail");

        input.onSubmit.AddListener((string msg) => SendChat());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && input != null)
        {
            input.ActivateInputField();
            input.Select();
            //EventSystem.current.SetSelectedGameObject(input.gameObject, null);
            //input.OnPointerClick(new PointerEventData(EventSystem.current));
        }
        if (Input.GetKeyDown(KeyCode.Escape) && input != null)
        {
            input.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);
            //EventSystem.current.SetSelectedGameObject(input.gameObject, null);
            //input.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }

    public void SendChat()
    {
        string msg = input.text.Trim();
        Debug.Log(msg);
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        InGameManager.Instance.ClientSendChat(msg);
        input.text = "";
        //writer.Reset();
        //writer.Put((byte)PacketType.Chat);
        //writer.Put(msg);
        //peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public void OnChat(string sender, string message)
    {
        text.text += $"{sender}: {message}\n";
    }
    
    public enum PacketType {
        Chat = 1,
        Voice = 2,
        Login = 3
    }

}