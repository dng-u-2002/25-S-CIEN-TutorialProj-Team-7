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

    public int ID;
    public bool IsLocal = false;

    public bool IsOrderDetermined = false;
    public int Order;
    public eIO IO;

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
            u.ShowPanelOnScreenCenter("기다리는 중...");
            u.IOSelectSound.Play();
            u.RemoveAllListenersFromIOButtons();
            u.SetIOButtonsActive(false);
        });
        u.InOutButton_Out.onClick.AddListener(() =>
        {
            IO = eIO.Out;
            onSelected?.Invoke(eIO.Out);
            u.SetActivePanelOnScreenCenter(true);
            u.ShowPanelOnScreenCenter("기다리는 중...");
            u.IOSelectSound.Play();

            u.RemoveAllListenersFromIOButtons();
            u.SetIOButtonsActive(false);
        });
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
            u.ShowPanelOnScreenCenter("기다리는 중...");
            u.RPSSelectSound.Play();

            u.SetRPSTextBox(true, rps);
        }

        u.RPSButton_R.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Rock);  v(eRPS.Rock); });
        u.RPSButton_P.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Paper); v(eRPS.Paper); });
        u.RPSButton_S.onClick.AddListener(() => { onSelected?.Invoke(eRPS.Scissors); v(eRPS.Scissors); });
    }

    public Player()
    {
        ThisDeck = new Deck();
    }
}
