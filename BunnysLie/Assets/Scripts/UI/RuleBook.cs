using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RuleBook : MonoBehaviour
{
    [SerializeField]
    RectTransform Background;
    [SerializeField] RectTransform[] Pages;
    int NowPage;
    bool IsShowing = false;
    [SerializeField] Button LeftButton;
    [SerializeField] Button RightButton;

    private void Start()
    {
        LeftButton.onClick.AddListener(() =>
        {
            NowPage--;
            ShowRuleBook(NowPage);
        });
        RightButton.onClick.AddListener(() =>
        {
            NowPage++;
            ShowRuleBook(NowPage);
        });

        OffRuleBook();
    }

    public void OnRulebookButtonClicked()
    {
        IsShowing = !IsShowing;

        if(IsShowing)
        {
            ShowRuleBook(NowPage);
        }
        else
        {
            OffRuleBook();
        }
    }

    void ActiveLeftButton(bool active)
    {
        LeftButton.gameObject.SetActive(active);
    }
    void ActiveRightButton(bool active)
    {
        RightButton.gameObject.SetActive(active);
    }

    void ShowRuleBook(int page)
    {
        if (page <= 0)
            page = 0;
        if(page >= Pages.Length)
            page = Pages.Length - 1;

        NowPage = page;

        foreach (var p in Pages)
        {
            p.gameObject.SetActive(false);
        }
        Pages[page].gameObject.SetActive(true);
        Background.gameObject.SetActive(true);

        IsShowing = true;
        ActiveLeftButton(true);
        ActiveRightButton(true);
        if(page == 0)
        {
            ActiveLeftButton(false);
        }
        if(page == Pages.Length - 1)
        {
            ActiveRightButton(false);
        }
    }
    void OffRuleBook()
    {
        IsShowing = false;
        ActiveLeftButton(false);
        ActiveRightButton(false);
        Background.gameObject.SetActive(false);
        foreach (var p in Pages)
        {
            p.gameObject.SetActive(false);
        }
    }
}
