using TMPro;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Chating : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] TMP_Text text;

    //public NetPeer peer { get; set; }
    //public NetDataWriter writer { get; set; } = new NetDataWriter();
    
    void Start()
    {
        input = GetComponentInChildren<TMP_InputField>();
        text = GetComponentInChildren<TMP_Text>();
        
        if(input==null||text==null)
            Debug.LogError("Fail");

        input.onSubmit.AddListener((string msg) => SendChat());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            EventSystem.current.SetSelectedGameObject(input.gameObject, null);
            input.OnPointerClick(null);
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