using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//v1.0 23-02-16
namespace Garunnir.UnityModule
{
    public class PermissionController : MonoBehaviour
    {
        [SerializeField] Button execute;
        private string[] permissionList;
        string nextSceneName="firstScene";

        private void Awake()
        {
            execute.onClick.AddListener(() => OnClick());
#if UNITY_EDITOR
            SceneManager.LoadScene(nextSceneName);
#else
        CheckPermissionList();
#endif

        }
        private void CheckPermissionList()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkv = version.GetStatic<int>("SDK_INT");
                if (sdkv >= 33)
                {
                    permissionList = new string[] {  "android.permission.READ_MEDIA_VIDEO" };
                }
                else
                {
                    permissionList = new string[] {  Permission.ExternalStorageRead, Permission.ExternalStorageWrite };
                }
            }
            bool allConfirm = true;
            for (int i = 0; i < permissionList.Length; i++)
            {
                if (!Check(permissionList[i]))
                {
                    allConfirm = false;
                    break;
                }
            }
            if (allConfirm)
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        public void OnClick()
        {
            StartCoroutine(StartPemission(permissionList));
        }

        IEnumerator StartPemission(string[] permissionList)
        {
            print("startP");
            foreach (string permis in permissionList)
            {
                if (!Permission.HasUserAuthorizedPermission(permis))
                {
                    Permission.RequestUserPermission(permis);
                }
                yield return new WaitUntil(() => Check(permis));
            }
            print("endp");
            SceneManager.LoadScene(nextSceneName);
        }

        private bool Check(string what)
        {
            if (Permission.HasUserAuthorizedPermission(what))
                return true;
            else
            {
                StopCoroutine(StartPemission(permissionList));
                return false;
            }
        }
    }
}