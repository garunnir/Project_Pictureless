using Garunnir;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIStatusController : UIController
{
    [SerializeField] UIStatusPageHandler pageHandler;
    List<IPage> pageList = new List<IPage>();
    int currentPageList = 0;
    int maxpage;
    private void Start()
    {
        GameManager.Instance.GetResourceManager().ResourceLoadDoneEvent+=()=> ShowData(5);
        pageList.Add(pageHandler);
        maxpage = pageHandler.GetMaxPage();
    }

    private void ShowNextPage()
    {
        var page = pageList[currentPageList];
        if (page.GetMaxPage() == page.GetCurrentPage() && pageList.Count > 1)
        {
            page.SetActive(false);
            if (currentPageList < pageList.Count)
            page = pageList[++currentPageList];
            else page = pageList[0];
            page.SetActive(true);
            page.SetPage(0);
        }
        else
        page.GetNextPage();
    }
    private void ShowPrevPage()
    {
        var page = pageList[currentPageList];
        if (page.GetCurrentPage()==0&&pageList.Count>1)
        {
            page.SetActive(false);
            if (currentPageList >0)
                page = pageList[--currentPageList];
            else page = pageList[pageList.Count-1];
            page.SetActive(true);
            page.SetPage(page.GetMaxPage());
        }
        else
        page.GetPrevPage();
    }
    private void ShowData(int id)
    {
        pageHandler.CallStatus(id);
    }
    private void ShowFieldData(List<List<Field>> fields)//패이지별로 표시
    {
        pageHandler.Callstatus(fields);
    }
    


    public override void Init()
    {
    }
    //public void Callstatus(Actor character, UICharModel model, UIVerticalView[] view)
    //{
    //    view[0].ClearComponents();
    //    view[1].ClearComponents();
    //    model.Init(view);
    //    model.currentActor = character;
    //    if (character == null)
    //    {
    //        Debug.LogWarning("actor not exist");
    //        return;
    //    }

    //    UIVerticalView viewobj = view[0];
    //    void AddHeight(UIVerticalView viewobj, float height)
    //    {
    //        if (viewobj == view[0])
    //        {
    //            if (model.leftY == 0) model.leftY += viewobj.target.padding.top;
    //            else model.leftY += viewobj.target.spacing;
    //            model.leftY += height;
    //        }
    //        else
    //        {
    //            if (model.rightY == 0) model.rightY += viewobj.target.padding.top;
    //            else model.leftY += viewobj.target.spacing;
    //            model.rightY += height;
    //        }
    //    }
    //    foreach (var item in character.fields)
    //    {
    //        if (!model.IsOpenedList.Contains(item.title)) continue;

    //        viewobj = model.leftY > model.rightY ? view[1] : view[0];
    //        float y = 0;
    //        if (model.BarExpression.Contains(item.title))
    //        {
    //            string[] str = item.value.Split('/');
    //            var obj = viewobj.ShowBar(item.value, float.Parse(str[0]) / float.Parse(str[1]), out y);
    //        }
    //        else if (model.ImgExpression.Contains(item.title))
    //        {
    //            Texture2D tex = character.portrait;
    //            var obj = viewobj.ShowImg(tex, out y);
    //        }
    //        else
    //        {
    //            var obj = viewobj.ShowText(item.title, item.value, out y);
    //        }
    //        AddHeight(viewobj, y);
    //    }
    //}
}
