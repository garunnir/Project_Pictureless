using UnityEngine;

public class GalleryImagePicker : MonoBehaviour
{
    public AndroidJavaClass galleryImagePicker;

    void Start()
    {
        // Android Java Ŭ���� �ʱ�ȭ
        galleryImagePicker = new AndroidJavaClass("com.example.GalleryImagePicker");

        // ������ �̹��� ���� �޼��� ȣ��
        galleryImagePicker.CallStatic("PickImage");
    }

    // ���������� �̹��� ���� �� ȣ��Ǵ� �ݹ� �޼���
    public void OnImagePicked(string imagePath)
    {
        Debug.Log("Selected image path: " + imagePath);
        // imagePath�� ����Ͽ� �̹����� ó���մϴ�.
    }
}
