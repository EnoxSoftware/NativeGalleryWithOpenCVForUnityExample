using UnityEngine;
using UnityEngine.SceneManagement;

namespace NativeGalleryWithOpenCVForUnityExample
{

    public class ShowLicense : MonoBehaviour
    {

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NativeGalleryWithOpenCVForUnityExample");
        }
    }
}
