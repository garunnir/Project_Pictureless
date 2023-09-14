using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.Wrappers;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

public class UIMapViewer : MonoBehaviour
{
    public int spacing = 10;
    public int square = 100;
    public MapContainer currentMap;
    public RectTransform mother;
    public Vector2 offsetmin = Vector2.zero;
    public Vector2 offsetmax = Vector2.zero;
    Vector2 texSize = new Vector2(50,6);

    List<Vector2> tmpv = new List<Vector2>();
    // Start is called before the first frame update
    void Start()
    {
        mother = GetComponent<RectTransform>();
        ShowMap(0);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void ShowMap(int id)
    {
        PosInit(mother);
        offsetmin = Vector2.zero;
        offsetmax = Vector2.zero;
        if (DialogueManager.masterDatabase.maps.Count == 0)
        {
            return;
        }
        currentMap = DialogueManager.masterDatabase.maps[id];
        MapEntry First = currentMap.GetFirstMapEntry();
        List<int> prevlist = new List<int>();
        prevlist.Add(-1);
        CreateSprite(First, Vector2.zero, ref prevlist);

        mother.offsetMin = offsetmin;
        mother.offsetMax = offsetmax;
        foreach (var item in currentMap.mapEntries)
        {

            //격자 포지션으로 배치한다.
            //how? 알고리즘 궁리해보자
            //start entry가 중심이 되서 펼쳐나가자
            //펼친 랙트를 중심으로 맨 왼쪽 맨위 맨아래 맨오른쪽을 계산해낸다.
            //딱 맞는 캔버스를 만든다.
            //런타임에 요소가 추가되면
            //딱 맞는 캔버스의 좌표값과 비교한다 생성된 랙트의 위치가 이 캔버스를 벗어나면
            //크기를 변경해준다.
        }
    }
    void PosInit(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = Vector2.zero;
    }
    enum CP
    {
        updown, leftright, 
    }
    void GenerateBridge(Vector2 point, CP dir)
    {
        GameObject go = new GameObject();
        go.transform.parent = mother;
        RectTransform rect = go.AddComponent<RectTransform>();
        go.AddComponent<RawImage>();
        PosInit(rect);
        Vector2 v2;
        switch (dir)
        {
            case CP.updown:
                 v2 = point;
                v2.x += texSize.x/2;
                v2.y += texSize.y/2;
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
        tmpv.Add(point);
    }
    void CreateSprite(MapEntry entry, Vector2 vector2, ref List<int> prevID)
    {
        //이미지 생성
        if (prevID.Contains(entry.id)) return;
        GameObject imgobj = new GameObject(entry.Title);
        imgobj.AddComponent<RawImage>();
        imgobj.transform.SetParent(mother);
        RectTransform rect = imgobj.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);

        rect.offsetMax = new Vector2(square / 2, square / 2);
        rect.offsetMin = new Vector2(-square / 2, -square / 2);
        rect.anchoredPosition = vector2;

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
        prevID.Add(entry.id);
        MapEntry target;
        Vector2 v2;
        if (entry.IsExistCardinalPoints())
        {
            if (entry.postion.upID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.upID);
                v2 = vector2;
                v2.y += square / 2f + spacing / 2f;
                if(!tmpv.Contains(v2))
                    GenerateBridge(v2, CP.updown);
                v2.y += square / 2f + spacing / 2f;
                CreateSprite(target, v2, ref prevID);
            }
            if (entry.postion.downID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.downID);
                v2 = vector2;
                v2.y -= square / 2f + spacing / 2f;
                if (!tmpv.Contains(v2))
                    GenerateBridge(v2, CP.updown);
                v2.y -= square / 2f + spacing / 2f;
                CreateSprite(target, v2, ref prevID);
            }
            if (entry.postion.leftID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.leftID);
                v2 = vector2;
                v2.x -= square / 2f + spacing / 2f;
                if (!tmpv.Contains(v2))
                    GenerateBridge(v2, CP.leftright);
                v2.x -= square / 2f + spacing / 2f;
                CreateSprite(target, v2, ref prevID);
            }
            if (entry.postion.rightID != -1)
            {
                target = currentMap.GetMapEntry(entry.postion.rightID);
                v2 = vector2;
                v2.x += square / 2f + spacing / 2f;
                if (!tmpv.Contains(v2))
                    GenerateBridge(v2, CP.leftright);
                v2.x += square / 2f + spacing / 2f;
                CreateSprite(target, v2, ref prevID);
            }
        }
    }
}
