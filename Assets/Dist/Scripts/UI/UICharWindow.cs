using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum ComponentType
{
    none,img, text, bar
}

public class UICharWindow : MonoBehaviour
{
    //�������ͽ� â�� ǥ�����ش�.
    //ĳ������ ������ ���� ǥ�ó����� �ٸ� �� �ְ� �Ѵ�.
    // Start is called before the first frame update
    public GameObject prf_component;
    public GameObject prf_text;
    public GameObject prf_img;
    public GameObject prf_bar;
    public VerticalLayoutGroup target;
    UIComponent ui_component;

    void Start()
    {
        //ShowUp(ComponentType.img,ComponentType.img,ComponentType.text);
        //��Ÿ�ӿ� ���������� �߰��� �� ������? �õ��غ���.
    }

    // Update is called once per frame
    void Update()
    {
    }
    void ClearComponents()
    {
        for (int i = 0; i < target.transform.childCount; i++)
        {
            Destroy(target.transform.GetChild(i).gameObject);
        }
    }
    void ShowUp(Garunnir.Character character)
    {
        ClearComponents();
        if(character == null)
        {
            Debug.LogWarning("actor not exist");
            return;
        }
        GameObject obj = null;
        //Ư�� Ű���尡 ������ �ʵ忡 ������ Ŭ���ؼ� ���������� �Ѿ �� �ְ� �Ѵ�.
        //H�������ͽ� ���� �������ͽ� �������� ������(�θ𴩸��� �����ִ� �������� �̵��Ѵ�.)
        //foreach (var item in character.profiles)
        //{

        //    //ù�������� �˻�
        //    switch (item.componentType)
        //    {
        //        case ComponentType.img:
        //            obj = Instantiate(prf_img);
        //            break; 
        //        case ComponentType.text:
        //            obj = Instantiate(prf_text);
        //            TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
        //            text.text=item.title+": "+item.value;
        //            text.font= UILocalizationManager.instance.localizedFonts.GetTextMeshProFont(Localization.language);
        //            break;
        //        case ComponentType.bar:
        //            string[] str= item.value.Split('/');
        //            obj = Instantiate(prf_bar);
        //            obj.GetComponentsInChildren<Image>().Last().fillAmount = float.Parse(str[0]) / float.Parse(str[1]);
        //            TMP_Text text1 = obj.GetComponentInChildren<TMP_Text>();
        //            text1.text = item.title;
        //            text1.enabled = true;
        //            text1.font=UILocalizationManager.instance.localizedFonts.GetTextMeshProFont(Localization.language);
        //            break;

        //    }
        //    //� ���Ұ� ������� �Ǻ�
        //    //�´� ���ҿ� ���� ������ ����
        //    if (obj) obj.transform.SetParent(target.transform);
        //}


    }
    void ShowUp(params ComponentType[] uIComponents)
    {
        //�̰��� ����Ǹ� �ش� �����͸� �����������Ѵ�.
        foreach (var uIComponent in uIComponents)
        {
            //CreateComponent(uIComponent);
        }
    }
    void CreateComponent(ComponentType component, out object etc)
    {
        GameObject obj = null;
        etc = null;
        switch (component)
        {
            case ComponentType.img:
                obj = Instantiate(prf_img);
                break;
            case ComponentType.text:
                obj = Instantiate(prf_text);
                etc = obj.GetComponentInChildren<TMPro.TMP_Text>();
                break;
            case ComponentType.bar:
                obj = Instantiate(prf_bar);
                break;
            default:
                break;
        }
    }
}