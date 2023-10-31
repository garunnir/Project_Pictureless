using Garunnir;
using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
public enum ComponentType
{
    none,img, text, bar
}
interface IPage
{
    public int GetCurrentPage();
    public int GetMaxPage();
    public void GetNextPage();
    public void GetPrevPage();
    public void SetPage(int page);
    public void SetActive(bool  active);
}
public class UIStatusPageHandler: UI,IPage
{
    [SerializeField] UIVerticalView viewTemplet;
    List<UICharModel> m_models = new List<UICharModel>();
    Dictionary<UICharModel, (UIVerticalView, UIVerticalView)> m_map = new Dictionary<UICharModel, (UIVerticalView, UIVerticalView)>();
    UICharModel prevPage = null;
    int currentPage=0;
    public int GetMaxPage() => m_models.Count-1;
    public int GetCurrentPage() => currentPage;
    private void Awake()
    {
    }
    public void GetNextPage()
    {
        if (IsExistPage(currentPage+1))
        {
            SetPage(++currentPage);
        }
        else
        {
            SetPage(0);
        }
    }
    public void GetPrevPage()
    {
        if (IsExistPage(currentPage-1))
        {
            SetPage(--currentPage);
        }
        else
        {
            SetPage(m_models.Count-1);
        }
    }
    bool IsExistPage(int idx)
    {
        return (m_models.Count > idx&&idx>=0);
    }
    public void SetPage(int page)
    {
        if (m_models.Count <= page) return;
        if(prevPage != null)
        {
            m_map[prevPage].Item1.gameObject.SetActive(false);
            m_map[prevPage].Item1.gameObject.SetActive(false);
        }
        m_map[m_models[page]].Item1.gameObject.SetActive(true);
        m_map[m_models[page]].Item2.gameObject.SetActive(true);
        currentPage = page;
    }
    void CreatePage()
    {
        (UIVerticalView, UIVerticalView) item = (Instantiate(viewTemplet, gameObject.transform), Instantiate(viewTemplet, gameObject.transform));
        item.Item1.gameObject.SetActive(false);
        item.Item2.gameObject.SetActive(false);
        UICharModel model = new UICharModel();
        model.Init(item);
        m_models.Add(model);
        m_map.Add(model, item);
    }
    void CreatePageTo(int idx)
    {
        for (int i = 0; i < m_models.Count- idx; i++)
        {
            CreatePage();
        }
    }
    UICharModel GetPage(int idx)=> m_models[idx];
    public void CallStatus(int id)
    {
        Actor cha = CharacterManager.Instance.characters.Find(x => x.id == id);
        if (cha == null)
        {
            Debug.LogWarning("isn't Exist charactor. id=" + id);
            return;
        }
        Callstatus(cha);
    }
    public void Callstatus(string name)
    {
        Actor cha = CharacterManager.Instance.characters.Find(x => x.Name == name);
        if (cha == null)
        {
            Debug.LogWarning("isn't Exist charactor. name=" + name);
            return;
        }
        Callstatus(cha);
    }
    public void Callstatus(Actor actor)
    {
        var inst = new UIEventArgs();
        if (m_models.Count == 0) CreatePage();
        Callstatus(actor, m_models[0]);
    }
    public void Callstatus(List<Field> fields)
    {
        Callstatus(fields, GetPage(0));
    }
    public void Callstatus(Actor character, UICharModel model)
    {
        Callstatus(character.fields, model);
    }
    public void Callstatus(List<Field> fields,UICharModel model)
    {
        model.Update(fields, m_map[model]);
    }
    public void Callstatus(List<List<Field>> fields)
    {
        CreatePageTo(fields.Count);
        for (int i = 0; i < fields.Count; i++)
        {
            Callstatus(fields[i], m_models[i]);
        }
    }

    public override IEnumerator AddHideAct()
    {
        yield return null;
    }

    public override IEnumerator AddShowAct()
    {
        yield return null;
    }

    public void SetActive(bool active)=>gameObject.SetActive(active);
}
public class UICharModel : UIModel
{
    public readonly string[] IsOpenedList = {"Status.Exp", "Pictures", "Character.OuterAge", "Character.FirstName"};//오픈할 변수명.
    public readonly string[] BarExpression = { "Status.Exp", };
    public readonly string[] ImgExpression = { DialogueSystemFields.Pictures, };
    public Dictionary<string, ComponentType> GetUIType = new Dictionary<string, ComponentType>();
    public float leftY = 0;
    public float rightY = 0;
    public readonly Vector4 AnchorLeft = new Vector4(0, 0, 0.5f, 1f);
    public readonly Vector4 AnchorRight = new Vector4(0.5f, 0, 1f, 1f);
    public void Init((UIVerticalView, UIVerticalView) view)
    {
        view.Item1.rect.anchorMin=new Vector2(AnchorLeft.x, AnchorLeft.y);
        view.Item1.rect.anchorMax=new Vector2(AnchorLeft.z, AnchorLeft.w);
        view.Item2.rect.anchorMin=new Vector2(AnchorRight.x, AnchorRight.y);
        view.Item2.rect.anchorMax=new Vector2(AnchorRight.z, AnchorRight.w);
        leftY= 0;
        rightY= 0;
    }

    public override void Update(object args)
    {
        throw new NotImplementedException();
    }

    public override void Init(object args)
    {
        throw new NotImplementedException();
    }
    public void Update(List<Field> fields, (UIVerticalView,UIVerticalView) view)
    {
        view.Item1.ClearComponents();
        view.Item2.ClearComponents();
        if (fields == null)
        {
            Debug.LogWarning("actor not exist");
            return;
        }

        UIVerticalView viewobj = view.Item1;
        void AddHeight(UIVerticalView viewobj, float height)
        {
            if (viewobj == view.Item1)
            {
                if (leftY == 0) leftY += viewobj.target.padding.top;
                else leftY += viewobj.target.spacing;
                leftY += height;
            }
            else
            {
                if (rightY == 0) rightY += viewobj.target.padding.top;
                else leftY += viewobj.target.spacing;
                rightY += height;
            }
        }
        foreach (var item in fields)
        {
            if (!IsOpenedList.Contains(item.title)) continue;

            viewobj = leftY > rightY ? view.Item2 : view.Item1;
            float y = 0;
            if (BarExpression.Contains(item.title))
            {
                string[] str = item.value.Split('/');
                var obj = viewobj.ShowBar(item.value, float.Parse(str[0]) / float.Parse(str[1]), out y);
            }
            else if (ImgExpression.Contains(item.title))
            {
                Texture2D tex = GameManager.Instance.GetImg(GetPicName(item));
                var obj = viewobj.ShowImg(tex, out y);
            }
            else
            {
                var obj = viewobj.ShowText(item.title, item.value, out y);
            }
            AddHeight(viewobj, y);
        }
    }
    string GetPicName(Field field)
    {
        if ((field == null) || (field.value == null))
        {
            return null;
        }
        else
        {
            string[] names = field.value.Split(new char[] { '[', ';', ']' });
            return (names.Length >= 2) ? names[1] : null;
        }
    }
}

public class UIEventArgs : EventArgs
{
    public List<Field> field;
}