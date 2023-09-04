using System.IO;
using UnityEngine;
using UnityEngine.UI;

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
        img.texture = LoadImage(GameManager.path_img_mainP);
    }
    public void ChangeImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            print("CI:" + path);
            File.Copy(path, GameManager.path_img_mainP,true);
            print("Dest:"+ GameManager.path_img_mainP);
            ImgApply(path);
        }, "0", "image/*");
    }
    private void ImgApply(string path)
    {
        img.texture=LoadImage(path);
    }
    Texture2D LoadImage(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes != null)
        {
            print("findByte");
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            bool boolen=texture.LoadImage(bytes);
            if (boolen)
            {
                print("loadDone");
                return texture;
            }
            else return null;
        }
        else { return null; }
    }
}
