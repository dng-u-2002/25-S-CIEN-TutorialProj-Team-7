using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InGameUser))]
public class ChatSender : MonoBehaviour
{
    InGameUser User;
    [SerializeField] TMP_InputField ChatInputField;
    [SerializeField] TMP_Text ChatText;

    public void Start()
    {
        User = GetComponent<InGameUser>();
        if (User == null)
        {
            Debug.LogError("ChatSender requires an InGameUser component.");
        }

        User.OnChatReceived += OnReceiveChat;
    }

    [SerializeField] string NowText { get { return ChatInputField.text; } set { ChatInputField.text = value; } }

    [EditorCools.Button]
    public void SendNowText()
    {
        User.SendChat2Server(NowText);
        NowText = string.Empty;
    }

    public void OnReceiveChat(int sender, string message)
    {
        ChatText.text += $"{sender}: {message}\n";
    }
}
