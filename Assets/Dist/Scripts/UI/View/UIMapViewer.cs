using Garunnir;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(ScrollRect))]
public class UIMapViewer : MonoBehaviour
{
    public event UnityAction<int,bool> MoveEvent;
    public event UnityAction<int> CreatedEvent;
    public bool IsIgnoreBridge = false;
    public int spacing = 10;
    public int square = 100;
    RectTransform selectRect;
    RectTransform mother;
    RectTransform contentRect;
    Vector2 offsetmin = Vector2.zero;
    Vector2 offsetmax = Vector2.zero;
    Vector2 texSize = new Vector2(50, 6);
    [SerializeField] Sprite selectedSprite;
    public List<RectTransform> prevlist = new List<RectTransform>();
    public Dictionary<int, RectTransform> mapdic = new Dictionary<int, RectTransform>();
    int createAmount=0;
    public int createLimit;
    [SerializeField] Button toggleBtn;
    [SerializeField] RectTransform window;
    [SerializeField] RectTransform minRect;
    [SerializeField] RectTransform maxRect;
    int currentRectLv=0;
    // Start is called before the first frame update
    void Awake()
    {
        contentRect = new GameObject("MapContent").AddComponent<RectTransform>();
        mother = transform.parent.GetComponent<RectTransform>();
        GetComponent<ScrollRect>().content = contentRect;
        toggleBtn.onClick.AddListener(() => MapToggle());
        window.gameObject.SetActive(false);
    }
    public void SelectPositionMoveTo(int idx)
    {
        selectRect.position = mapdic[idx].position;
    }
    private void MapToggle()
    {
        currentRectLv++;
        if (currentRectLv == 3)
        {
            currentRectLv = 0;
        }

        switch (currentRectLv)
        {
            case 0:
                window.gameObject.SetActive(false);
                break;
            case 1:
                window.gameObject.SetActive(true);
                UIManager.CopyValues(window, minRect);
                break;
            case 2:
                UIManager.CopyValues(window,maxRect);
                break;
        }
    }


    // Update is called once per frame

    public void AlignCenter()
    {
        //중심을 선택된 상자 위주로 재조정한다.
        //지금 선택된 상자와 중심의 위치의 차이만큼 그룹을 이동시킨다.
        //선택상자의 위치
        contentRect.position-=selectRect.position - mother.position;
    }

    RectTransform GenSelectSprite(Sprite sprite)
    {
        RectTransform rect = new GameObject("Select").AddComponent<RectTransform>();
        Image img = rect.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        PosInit(rect);
        rect.offsetMin = new Vector2(-square / 2f, -square / 2f);
        rect.offsetMax = new Vector2(square / 2f, square / 2f);
        
        return rect;
    }
    public void ShowMap(string entryTitle, int entryid)
    {
        offsetmin = contentRect.offsetMin;
        offsetmax = contentRect.offsetMax;
        Debug.LogWarning(offsetmin);
        PosInit(contentRect);

        CreateSprite(entryTitle, entryid, contentRect.position);

        foreach (var rect in prevlist)
        {
            if (offsetmin.y > rect.offsetMin.y)
            {
                offsetmin.y = rect.offsetMin.y;
            }

            if (offsetmax.y < rect.offsetMax.y)
            {
                offsetmax.y = rect.offsetMax.y;
            }

            if (offsetmin.x > rect.offsetMin.x)
            {
                offsetmin.x = rect.offsetMin.x;
            }

            if (offsetmax.x < rect.offsetMax.x)
            {
                offsetmax.x = rect.offsetMax.x;
            }
            //Vector2 v2 = item.anchoredPosition;
            //v2.y += Screen.height;
            //item.anchoredPosition = v2;
            //rect.SetParent(mother);
            //격자 포지션으로 배치한다.
            //how? 알고리즘 궁리해보자
            //start entry가 중심이 되서 펼쳐나가자
            //펼친 랙트를 중심으로 맨 왼쪽 맨위 맨아래 맨오른쪽을 계산해낸다.
            //딱 맞는 캔버스를 만든다.
            //런타임에 요소가 추가되면
            //딱 맞는 캔버스의 좌표값과 비교한다 생성된 랙트의 위치가 이 캔버스를 벗어나면
            //크기를 변경해준다.
        }
        contentRect.offsetMin = offsetmin;
        contentRect.offsetMax = offsetmax;
        contentRect.transform.SetParent(transform);
        foreach (var rect in prevlist)
        {
            rect.SetParent(contentRect);
        }
        RectTransform sr = GenSelectSprite(selectedSprite);
        sr.SetParent(contentRect);
        sr.position = prevlist[0].position;
        selectRect = sr;
    }
    void PosInit(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        //rect.anchoredPosition = Vector2.zero;
    }
    public enum CP
    {
        updown, leftright,
    }
    public void GenerateBridge(Vector2 point, CP dir)
    {
        GameObject go = new GameObject();
        RectTransform rect = go.AddComponent<RectTransform>();
        go.AddComponent<RawImage>();
        PosInit(rect);
        Vector2 v2;
        switch (dir)
        {
            case CP.updown:
                v2 = point;
                v2.x += texSize.x / 2;
                v2.y += texSize.y / 2;
                rect.offsetMax = v2;
                v2.x -= texSize.x;
                v2.y -= texSize.y;
                rect.offsetMin = v2;
                break;
            case CP.leftright:
                v2 = point;
                v2.x += texSize.y / 2;
                v2.y += texSize.x / 2;
                rect.offsetMax = v2;
                v2.x -= texSize.y;
                v2.y -= texSize.x;
                rect.offsetMin = v2;
                break;
        }
        prevlist.Add(rect);
    }
    public void CreateSprite(string entryTitle ,int entryid, Vector2 vecCenter)
    {
        //이미지 생성
        if (createAmount>createLimit|| mapdic.ContainsKey(entryid) || prevlist.Where(x => x.anchoredPosition == vecCenter).Count() != 0) return;
        createAmount++; 
        GameObject imgobj = new GameObject(entryTitle);
        imgobj.AddComponent<RawImage>();
        RectTransform rect = imgobj.GetComponent<RectTransform>();
        PosInit(rect);

        rect.offsetMax = vecCenter+new Vector2(square / 2, square / 2);
        rect.offsetMin = vecCenter-new Vector2(square / 2, square / 2);

        mapdic.Add(entryid, rect);
        prevlist.Add(rect);
        CreatedEvent(entryid);
    }
    internal void CheckMoveable()
    {
        foreach (RectTransform t in mapdic.Values)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(t, Input.mousePosition))
            {
                MoveEvent(mapdic.Where(x => x.Value == t).First().Key, IsIgnoreBridge);
                break;
            }
        }
    }
}
