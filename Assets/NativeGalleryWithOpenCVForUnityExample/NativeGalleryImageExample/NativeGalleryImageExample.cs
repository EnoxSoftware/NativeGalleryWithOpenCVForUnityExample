#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnityExample.DnnModel;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NativeGalleryWithOpenCVForUnityExample
{
    /// <summary>
    /// Native Gallery Image Example
    /// An example of combining an image file picker using the Native Gallery plugin with image processing by OpenCVForUnity.
    /// </summary>
    public class NativeGalleryImageExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        [Header("ObjectDetecton")]
        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model = "yolox_tiny.onnx";

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config = "";

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes = "coco.names";

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.25f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold = 0.45f;

        [TooltipAttribute("Maximum detections per image.")]
        public int topK = 1000;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth = 416;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight = 416;

        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;


        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The YOLOX ObjectDetector.
        /// </summary>
        YOLOXObjectDetector objectDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = await Utils.getFilePathAsyncTask("NativeGalleryWithOpenCVForUnityExample/" + classes, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/NativeGalleryWithOpenCVForUnityExample”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = await Utils.getFilePathAsyncTask("NativeGalleryWithOpenCVForUnityExample/" + config, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/NativeGalleryWithOpenCVForUnityExample”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = await Utils.getFilePathAsyncTask("NativeGalleryWithOpenCVForUnityExample/" + model, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/NativeGalleryWithOpenCVForUnityExample”.");
            }

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded.");
            }
            else
            {
                objectDetector = new YOLOXObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void DetectObjectAndVisualize(Mat bgrMat)
        {
            if (objectDetector == null)
            {
                Imgproc.putText(bgrMat, "model file is not loaded.", new Point(5, bgrMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(bgrMat, "Please read console message.", new Point(5, bgrMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                Mat results = objectDetector.infer(bgrMat);

                objectDetector.visualize(bgrMat, results, false, false);
            }
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (objectDetector != null)
                objectDetector.dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            Utils.setDebugMode(false);

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NativeGalleryWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the get Image from Gallery (LoadImageAtPath) button click event.
        /// </summary>
        public void OnGetImageFromGallery_LoadImageAtPathButtonClick()
        {

            int maxSize = 1280;

            NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
            {
                Debug.Log("Image path: " + path);

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "Image path: " + path + "\n";

                if (path != null)
                {

                    // Create Texture from selected image
                    Texture2D tex = NativeGallery.LoadImageAtPath(path, maxSize, false, false);
                    if (tex == null)
                    {
                        Debug.Log("Couldn't load texture from " + path);

                        if (fpsMonitor != null)
                            fpsMonitor.consoleText += "Couldn't load texture from " + path + "\n";

                        return;
                    }

                    if (texture == null || (tex.width != texture.width || tex.height != texture.height))
                    {
                        if (texture != null)
                            Texture2D.Destroy(texture);

                        texture = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                    }

                    using (Mat bgrMat = new Mat(texture.height, texture.width, CvType.CV_8UC3))
                    using (Mat rgbaMat = new Mat(texture.height, texture.width, CvType.CV_8UC4))
                    {
                        Utils.texture2DToMat(tex, rgbaMat);
                        Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                        DetectObjectAndVisualize(bgrMat);

                        Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
                        Utils.matToTexture2D(rgbaMat, texture);
                    }

                    resultPreview.texture = texture;
                    resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

                    // If a procedural texture is not destroyed manually, 
                    // it will only be freed after a scene change
                    Destroy(tex);
                    //
                }
            });

            Debug.Log("Permission result: " + permission);

            if (fpsMonitor != null)
                fpsMonitor.consoleText += "Permission result: " + permission;
        }

        /// <summary>
        /// Raises the get Image from Gallery (OpneCVImread) button click event.
        /// </summary>
        public void OnGetImageFromGallery_OpneCVImreadButtonClick()
        {

            NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
            {
                Debug.Log("Image path: " + path);

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "Image path: " + path + "\n";

                if (path != null)
                {
                    // Image reading by OpenCV.imread method.
                    using (Mat bgrMat = Imgcodecs.imread(path, Imgcodecs.IMREAD_COLOR))
                    {
                        if (bgrMat.empty())
                        {
                            Debug.Log("Couldn't load image to mat from " + path);

                            if (fpsMonitor != null)
                                fpsMonitor.consoleText += "Couldn't load image to mat from " + path + "\n";

                            return;
                        }

                        if (texture == null || (bgrMat.width() != texture.width || bgrMat.height() != texture.height))
                        {
                            if (texture != null)
                                Texture2D.Destroy(texture);

                            texture = new Texture2D(bgrMat.width(), bgrMat.height(), TextureFormat.RGBA32, false);
                        }

                        using (Mat rgbaMat = new Mat(texture.height, texture.width, CvType.CV_8UC4))
                        {
                            DetectObjectAndVisualize(bgrMat);

                            Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
                            Utils.matToTexture2D(rgbaMat, texture);
                        }

                        resultPreview.texture = texture;
                        resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
                    }
                }
            });

            Debug.Log("Permission result: " + permission);

            if (fpsMonitor != null)
                fpsMonitor.consoleText += "Permission result: " + permission;
        }

        /// <summary>
        /// Raises the save image to gallery button click event.
        /// </summary>
        public void OnSaveImageToGalleryButtonClick()
        {
            // Save the screenshot to Gallery/Photos
            NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(texture, "NativeGalleryWithOpenCVForUnityExample", "NativeGalleryImageExample.png", (success, path) =>
            {
                Debug.Log("Media save result: " + success + " " + path);

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "Media save result: " + success + " " + path;
            });


            Debug.Log("Permission result: " + permission);

            if (fpsMonitor != null)
                fpsMonitor.consoleText += "Permission result: " + permission;
        }
    }
}

#endif