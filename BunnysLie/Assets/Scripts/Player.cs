using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum eRPS
    {
        Rock,
        Paper,
        Scissors,
        None
    }
public enum eIO
{
    In,
    Out,
    None
}

public class Player : MonoBehaviour
{
    public Card Card2Exchange;
    public Deck ThisDeck { get; private set; }
    int NowOutcount = 0;

    public int _ID;
    public int ID
    {
        get
        {
            if(IsLocal)
            {
                return PhotonNetwork.LocalPlayer.ActorNumber;
            }
            return _ID;
        }
        set
        {
           if(IsLocal)
            {
                Debug.LogError("Not allowed to set id of local player");
            }
           else
            {
                _ID = value;
            }
        }
    }
    public bool IsLocal = false;

    public bool IsOrderDetermined = false;
    public int Order;
    public eIO IO;

    private void Update()
    {
        if(KeyboardCallback != null)
        {
            KeyboardCallback?.Invoke();
        }
    }

    public void StartSelectIO(Action<eIO> onSelected)
    {
        var u = FindObjectOfType<LocalPlayerUIDrawer>();
        u.RemoveAllListenersFromIOButtons();
        u.SetIOButtonsActive(true);

        u.InOutButton_In.onClick.AddListener(() =>
        {
            IO = eIO.In;
            onSelected?.Invoke(eIO.In);
            u.SetActivePanelOnScreenCenter(true);
            u.ShowPanelOnScreenCenter(ConstStrings.Message_Waiting, 0);
            u.IOSelectSound.Play();
            u.RemoveAllListenersFromIOButtons();
            u.SetIOButtonsActive(false);
        });
        u.InOutButton_Out.onClick.AddListener(() =>
        {
            IO = eIO.Out;
            onSelected?.Invoke(eIO.Out);
            u.SetActivePanelOnScreenCenter(true);
            u.ShowPanelOnScreenCenter(ConstStrings.Message_Waiting, 0);
            u.IOSelectSound.Play();

            u.RemoveAllListenersFromIOButtons();
            u.SetIOButtonsActive(false);
        });
        KeyboardCallback = Callback_SelectIOByKeyboard;
    }
    void Callback_SelectIOByKeyboard()
    {
        var chat = FindObjectOfType<Chating>();
        if (chat != null)
        {
            if (chat.IsFocusing == true)
                return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            InGameManager.Instance.LocalPlayerUIDrawer.InOutButton_In.onClick.Invoke();
            KeyboardCallback = null;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            InGameManager.Instance.LocalPlayerUIDrawer.InOutButton_Out.onClick.Invoke();
            KeyboardCallback = null;
        }
    }

    public void StartSelectRPS(Action<eRPS> onSelected)
    {
        foreach(var rp in InGameManager.Instance.RemotePlayerUIDrawers)
        {
            rp.SetRPSTextBox(false, eRPS.None);
        }

        var u = FindObjectOfType<LocalPlayerUIDrawer>();
        u.RemoveAllListenersFromRPSButtons();
        u.SetRPSButtonsActive(true);


        IsOrderDetermined = false;
        u.SetActivePanelOnScreenCenter(false);
        u.SetRPSTextBox(false, eRPS.None);

        void v(eRPS rps)
        {
            u.RemoveAllListenersFromRPSButtons();
            u.SetRPSButtonsActive(false);
            u.SetActivePanelOnScreenCenter(true);
            u.ShowPanelOnScreenCenter(ConstStrings.Message_WaitingRSP, 0);
            u.RPSSelectSound.Play();

            u.SetRPSTextBox(true, rps);
        }

        u.RPSButton_R.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Rock);  v(eRPS.Rock); });
        u.RPSButton_P.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Paper); v(eRPS.Paper); });
        u.RPSButton_S.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Scissors); v(eRPS.Scissors); });

        KeyboardCallback = Callback_SelectRPSByKeyboard;
    }
    System.Action KeyboardCallback;
    void Callback_SelectRPSByKeyboard()
    {
        var chat = FindObjectOfType<Chating>();
        if (chat != null)
        {
            if (chat.IsFocusing == true)
                return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_R.onClick.Invoke();
            KeyboardCallback = null;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_S.onClick.Invoke();
            KeyboardCallback = null;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            InGameManager.Instance.LocalPlayerUIDrawer.RPSButton_P.onClick.Invoke();
            KeyboardCallback = null;
        }
    }

    public Player()
    {
        ThisDeck = new Deck();
    }
}
