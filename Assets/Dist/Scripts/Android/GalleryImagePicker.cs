using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Garunnir
{
    public class GalleryImagePicker : MonoBehaviour
{
    public Button chanage;
    public RawImage img;
    public string filename;
    private void Awake()
    {
        chanage.onClick.AddListener(()=>ChangeImage());
    }
    // 갤러리에서 이미지 선택 후 호출되는 콜백 메서드
    public void OnImagePicked(string imagePath)
    {
        //NativeGallery.SaveImageToGallery(byte[] mediaBytes, string album, string filename, MediaSaveCallback callback = null)
        //    NativeGallery.GetImageFromGallery(MediaPickCallback callback, string title = "", string mime = "image/*")
    }
    
    private void OnEnable()
    {
        if(string.IsNullOrEmpty(filename)) { filename = "mainChara"; }
        if(File.Exists(GameManager.path_img_mainP))
        img.texture = Utillity.LoadImage(GameManager.path_img_mainP);
    }
    public void ChangeImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            print("CI:" + path);
            File.Copy(path, GameManager.path_img_mainP,true);
            print("Dest:"+ GameManager.path_img_mainP);
            ImgApply(path);
        }, "캐릭터 프로필 사진 선택", "image/*");
    }
    private void ImgApply(string path)
    {
        img.texture=Utillity.LoadImage(path);
    }

}
}
