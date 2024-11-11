#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NativeGalleryWithOpenCVForUnityExample
{
    /// <summary>
    /// Native Gallery Video Example
    /// An example of combining an video file picker using the Native Gallery plugin with image processing by OpenCVForUnity.
    /// </summary>
    [RequireComponent(typeof(UnityVideoPlayer2MatHelper))]
    public class NativeGalleryVideoExample : MonoBehaviour
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
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        float videoRotation = 0;
        Mat rotatedRgbaMat;

        /// <summary>
        /// The unity video player to mat helper.
        /// </summary>
        UnityVideoPlayer2MatHelper unityVideoPlayerToMatHelper;

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

            // Get the WebCamTextureToMatHelper component attached to the current game object
            unityVideoPlayerToMatHelper = gameObject.GetComponent<UnityVideoPlayer2MatHelper>();
            unityVideoPlayerToMatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

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

        /// <summary>
        /// Raises the unity video player to mat helper initialized event.
        /// </summary>
        public void OnUnityVideoPlayerToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat rgbaMat = unityVideoPlayerToMatHelper.GetMat();

            int width, height;
            if (videoRotation == 90 || videoRotation == 270)
            {
                width = rgbaMat.rows();
                height = rgbaMat.cols();
                rotatedRgbaMat = new Mat(height, width, CvType.CV_8UC4);
            }
            else
            {
                width = rgbaMat.cols();
                height = rgbaMat.rows();
            }

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", width.ToString());
                fpsMonitor.Add("height", height.ToString());
                fpsMonitor.Add("fps", unityVideoPlayerToMatHelper.GetFPS().ToString());
                fpsMonitor.Add("videoRotation", videoRotation.ToString());
            }

            bgrMat = new Mat(height, width, CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the unity video player to mat helper disposed event.
        /// </summary>
        public void OnUnityVideoPlayerToMatHelperDisposed()
        {
            Debug.Log("OnUnityVideoPlayerToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose();

            if (rotatedRgbaMat != null)
                rotatedRgbaMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the unity video player to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnUnityVideoPlayerToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnUnityVideoPlayerToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {

            if (unityVideoPlayerToMatHelper.IsPlaying() && unityVideoPlayerToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = unityVideoPlayerToMatHelper.GetMat();

                if (videoRotation == 90)
                {
                    Core.rotate(rgbaMat, rotatedRgbaMat, Core.ROTATE_90_CLOCKWISE);
                    rgbaMat = rotatedRgbaMat;
                }
                else if (videoRotation == 180)
                {
                    Core.rotate(rgbaMat, rotatedRgbaMat, Core.ROTATE_180);
                    rgbaMat = rotatedRgbaMat;
                }
                else if (videoRotation == 270)
                {
                    Core.rotate(rgbaMat, rotatedRgbaMat, Core.ROTATE_90_COUNTERCLOCKWISE);
                    rgbaMat = rotatedRgbaMat;
                }

                if (objectDetector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat results = objectDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("YOLOXObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    objectDetector.visualize(rgbaMat, results, false, true);
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (objectDetector != null)
                objectDetector.dispose();

            if (unityVideoPlayerToMatHelper != null)
                unityVideoPlayerToMatHelper.Dispose();

            Utils.setDebugMode(false);
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NativeGalleryWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the get video from Gallery (VideoPlayer) button click event.
        /// </summary>
        public void OnGetVideoFromGallery_VideoPlayerButtonClick()
        {

            NativeGallery.Permission permission = NativeGallery.GetVideoFromGallery((path) =>
            {
                Debug.Log("Video path: " + path);

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "Video path: " + path + "\n";

                if (path != null)
                {
                    NativeGallery.VideoProperties properties = NativeGallery.GetVideoProperties(path);
                    videoRotation = properties.rotation;

                    Debug.Log("Video props: " + "wh:" + properties.width + "x" + properties.height + " duration:" + properties.duration + " rotation:" + properties.rotation);

                    if (fpsMonitor != null)
                        fpsMonitor.consoleText += "Video props: " + "wh:" + properties.width + "x" + properties.height + " duration:" + properties.duration + " rotation:" + properties.rotation + "\n";

                    unityVideoPlayerToMatHelper.requestedVideoFilePath = path;
                    unityVideoPlayerToMatHelper.Initialize();
                }
            });

            Debug.Log("Permission result: " + permission);

            if (fpsMonitor != null)
                fpsMonitor.consoleText += "Permission result: " + permission;
        }
    }
}

#endif