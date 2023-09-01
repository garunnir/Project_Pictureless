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
    //스테이터스 창을 표시해준다.
    //캐릭터의 성질에 따라 표시내용이 다를 수 있게 한다.
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
        //런타임에 액터정보를 추가할 수 있을까? 시도해본다.
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
        //특정 키워드가 유아이 필드에 있으면 클릭해서 다음장으로 넘어갈 수 있게 한다.
        //H스테이터스 전투 스테이터스 인적사항 프로필(부모누르면 보여주는 프로필을 이동한다.)
        //foreach (var item in character.profiles)
        //{

        //    //첫번쨰부터 검사
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
        //    //어떤 원소가 들었는지 판별
        //    //맞는 원소에 따라 유아이 생성
        //    if (obj) obj.transform.SetParent(target.transform);
        //}


    }
    void ShowUp(params ComponentType[] uIComponents)
    {
        //이것이 실행되면 해당 데이터를 비쥬얼라이즈한다.
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