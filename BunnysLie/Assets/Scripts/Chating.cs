using TMPro;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Unity.Collections;

public class Chating : MonoBehaviour
{
    TMP_InputField input;
    TMP_Text text;

    public NetPeer peer { get; set; }
    public NetDataWriter writer { get; set; } = new NetDataWriter();
    
    void Start()
    {
        input = GetComponentInChildren<TMP_InputField>();
        text = GetComponentInChildren<TMP_Text>();
        
        if(input==null||text==null)
            Debug.LogError("Fail");
    }

    public void SendChat()
    {
        string msg = input.text.Trim();
        if (string.IsNullOrEmpty(msg) || peer == null || peer.ConnectionState != ConnectionState.Connected)
        {
            return;
        }
        writer.Reset();
        writer.Put((byte)PacketType.Chat);
        writer.Put(msg);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public void OnChat(int sender, string message)
    {
        text.text += $"{sender}: {message}\n";
    }
    
    public enum PacketType {
        Chat = 1,
        Voice = 2,
        Login = 3
    }

}