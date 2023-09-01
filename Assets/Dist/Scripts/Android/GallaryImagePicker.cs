using UnityEngine;

public class GalleryImagePicker : MonoBehaviour
{
    public AndroidJavaClass galleryImagePicker;

    void Start()
    {
        // Android Java 클래스 초기화
        galleryImagePicker = new AndroidJavaClass("com.example.GalleryImagePicker");

        // 갤러리 이미지 선택 메서드 호출
        galleryImagePicker.CallStatic("PickImage");
    }

    // 갤러리에서 이미지 선택 후 호출되는 콜백 메서드
    public void OnImagePicked(string imagePath)
    {
        Debug.Log("Selected image path: " + imagePath);
        // imagePath를 사용하여 이미지를 처리합니다.
    }
}
