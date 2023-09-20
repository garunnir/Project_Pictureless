using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.Wrappers;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIMapViewer : MonoBehaviour
{
    public int spacing = 10;
    public int square = 100;
    public MapContainer currentMap;
    int currentEntry;
    RectTransform selectRect;
    RectTransform mother;
    RectTransform thisRect;
    Vector2 offsetmin = Vector2.zero;
    Vector2 offsetmax = Vector2.zero;
    Vector2 texSize = new Vector2(50, 6);
    [SerializeField] Sprite selectedSprite;
    List<RectTransform> prevlist = new List<RectTransform>();
    Dictionary<int, RectTransform> mapdic = new Dictionary<int, RectTransform>();
    int createAmount=0;
    int createLimit;
    // Start is called before the first frame update
    void Start()
    {
        thisRect = GetComponent<RectTransform>();
        mother=transform.parent.GetComponent<RectTransform>();
        ShowMap(0);
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {

            Vector3 wp = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,Camera.main.nearClipPlane));
            //마우스 스크린포인트를 가져온다
            Debug.LogWarning(wp);

            //랙트유틸리티로 곂치는지 확인
            //마우스와 가장 근접한 랙트를 가져온다.
            //RectTransform closest = mapdic.Values.First();
            //foreach (RectTransform t in mapdic.Values)
            //{
            //    if (Vector2.Distance(wp, closest.rect.center) < Vector2.Distance(wp, t.rect.center))
            //    {
            //        closest = t;
            //    }
            //}
            //MoveTo(mapdic.Where(x => x.Value == closest).First().Key);

            foreach(RectTransform t in mapdic.Values)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(t,Input.mousePosition))
                {
                    MoveTo(mapdic.Where(x => x.Value == t).First().Key);
                    AlignCenter();
                    break;
                }
            }
        }
    }
    void AlignCenter()
    {
        //중심을 선택된 상자 위주로 재조정한다.
        //지금 선택된 상자와 중심의 위치의 차이만큼 그룹을 이동시킨다.
        //선택상자의 위치
        thisRect.position-=selectRect.position - mother.position;
    }
    void MoveTo(int entryid,bool ignoreBridge=false)
    {
        if (!ignoreBridge&&!currentMap.GetMapEntry(currentEntry).IsConnectedWith(entryid)) return;
        //셀렉트를 해당 아이디 위치로 이동한다
        selectRect.position=mapdic[entryid].position;
        currentEntry = entryid;
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
    private void ShowMap(int id)
    {

        Transform parent = transform.parent;
        thisRect.transform.parent = null;
        offsetmin = thisRect.offsetMin;
        offsetmax = thisRect.offsetMax;
        Debug.LogWarning(offsetmin);
        PosInit(thisRect);
        if (DialogueManager.masterDatabase.maps.Count == 0)
        {
            return;
        }
        currentMap = DialogueManager.masterDatabase.maps[id];
        createLimit=currentMap.mapEntries.Count() * 3;
        MapEntry First = currentMap.GetFirstMapEntry();
        currentEntry = First.id;
        CreateSprite(First, thisRect.position);

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
        thisRect.offsetMin = offsetmin;
        thisRect.offsetMax = offsetmax;
        thisRect.transform.parent = parent;
        foreach (var rect in prevlist)
        {
            rect.SetParent(thisRect);
        }
        RectTransform sr = GenSelectSprite(selectedSprite);
        sr.SetParent(thisRect);
        sr.position = prevlist[0].position;
        selectRect = sr;
    }
    void PosInit(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        //rect.anchoredPosition = Vector2.zero;
    }
    enum CP
    {
        updown, leftright,
    }
    void GenerateBridge(Vector2 point, CP dir)
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
    void CreateSprite(MapEntry entry, Vector2 vecCenter)
    {
        //이미지 생성
        if (prevlist.Where(x => x.anchoredPosition == vecCenter).Count() != 0||createAmount>createLimit) return;
        createAmount++; 
        GameObject imgobj = new GameObject(entry.Title);
        imgobj.AddComponent<RawImage>();
        RectTransform rect = imgobj.GetComponent<RectTransform>();
        PosInit(rect);

        rect.offsetMax = vecCenter+new Vector2(square / 2, square / 2);
        rect.offsetMin = vecCenter-new Vector2(square / 2, square / 2);

        mapdic.Add(entry.id, rect);
        prevlist.Add(rect);
        MapEntry target;
        Vector2 v2;
        if (entry.IsExistCardinalPoints())
        {
            if (entry.postion.upID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.upID);
                v2 = vecCenter;
                v2.y += square / 2f + spacing / 2f;
                if (prevlist.Where(x => x.anchoredPosition == v2).Count() == 0)
                    GenerateBridge(v2, CP.updown);
                v2.y += square / 2f + spacing / 2f;
                CreateSprite(target, v2);
            }
            if (entry.postion.downID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.downID);
                v2 = vecCenter;
                v2.y -= square / 2f + spacing / 2f;
                if (prevlist.Where(x => x.anchoredPosition == v2).Count() == 0)
                    GenerateBridge(v2, CP.updown);
                v2.y -= square / 2f + spacing / 2f;
                CreateSprite(target, v2);
            }
            if (entry.postion.leftID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.leftID);
                v2 = vecCenter;
                v2.x -= square / 2f + spacing / 2f;
                if (prevlist.Where(x => x.anchoredPosition == v2).Count() == 0)
                    GenerateBridge(v2, CP.leftright);
                v2.x -= square / 2f + spacing / 2f;
                CreateSprite(target, v2);
            }
            if (entry.postion.rightID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.rightID);
                v2 = vecCenter;
                v2.x += square / 2f + spacing / 2f;
                if (prevlist.Where(x => x.anchoredPosition == v2).Count() == 0)
                    GenerateBridge(v2, CP.leftright);
                v2.x += square / 2f + spacing / 2f;
                CreateSprite(target, v2);
            }
        }
    }
}