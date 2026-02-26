using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.InteropServices;
using ViveSR.anipal.Eye;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Diagnostics;
using System.Net.Http.Headers;

public class Calibrator : EditorWindow
{
    [MenuItem("Calibration/StartCalibration")]
    private static void StartCalibration()
    {
        if (EyeTrackingManager.instance != null)
        {
            // Call the non-static method on the singleton instance
            EyeTrackingManager.instance.PupilCalibration();
        }
        else
        {
            UnityEngine.Debug.LogError("EyeTrackingManager instance is not available.");
        }

    }
}

namespace ViveSR.anipal.Eye
{
    public class EyeTrackingManager : MonoBehaviour
    {
        public static EyeTrackingManager instance;
        private string subjectName;
        private static EyeData eyeData = new EyeData();

        public static float pupilDiameterLeft;
        public static float pupilDiameterRight;

        private bool eye_callback_registered = false; // This should have a better interface

        private static int callbackCount = 0;
        private const int logEveryN = 10;
        private static string logPath;
        private static StreamWriter writer;
        private static StreamWriter incrementsWriter;
        private static Queue<string> logQueue = new Queue<string>();
        private static Queue<string> incQueue = new Queue<string>();

        // Luminance stuff
        private bool luminanceEnabled = true;
        private bool luminanceCreated = false;
        private bool callbackReceived = false;
        private int luminanceWidth = 256;
        private int luminanceHeight = 144;
        private Camera luminanceCamera;
        private Texture2D luminanceTexture;
        private RenderTexture luminenceRenderTexture;
        public static float luminance = -1.0f;
        private float luminanceTime = -1.0f;
        private float luminanceRate = 1.0f / 10.0f;
        public GameObject calibrationScreen;
        private Camera cam;
        public float fearPeriod = 30.0f; //seconds
        private static int fearStart = -1;
        private static List<float> increments;
        private static bool fearChecked = false;
        public bool CalibrateOnStart = true;
        private bool calibrated = false;
        private bool calibrating = false;
        string logDirectory;
        internal class MonoPInvokeCallbackAttribute : System.Attribute
        {
            public MonoPInvokeCallbackAttribute() { }
        }

        public bool isCalibrated()
        {
            return calibrated;
        }
        void Start()
        {
            if (instance == null)
                instance = this;

            if (!SRanipal_Eye_Framework.Instance.EnableEye)
                return;

            SRanipal_Eye_Framework.Instance.EnableEyeDataCallback = true;

            Camera cameraComponent = UnityEngine.Object.FindObjectOfType<Camera>();

            if (cameraComponent != null)
            {
                GameObject cameraObject = cameraComponent.gameObject;

                subjectName = cameraObject.name;
                UnityEngine.Debug.Log($"Subject is {subjectName}.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("No GameObject with a Camera component was found in the scene.");
                return;
            }

            cam = Camera.main;
            CreateLuminanceCamera();

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");  // e.g., 2025-10-22_14-30-00
            logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "AcroGenData",
                subjectName,
                sceneName,
                timestamp
            );
            Directory.CreateDirectory(logDirectory);

            SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            UnityEngine.Debug.Log("EyeCallback registered");

            if (CalibrateOnStart)
                PupilCalibration();

            
            string logPath = Path.Combine(logDirectory, "eye_tracking_log_test.csv");

            writer = new StreamWriter(logPath);
            writer.WriteLine("Timestamp,Luminance,PupilDiameterLeft,PupilDiameterRight,GazeLeftX,GazeLeftY,GazeLeftZ,GazeRightX,GazeRightY,GazeRightZ"); // header row
            writer.Flush();

            UnityEngine.Debug.Log("EyeCallback logging to: " + logPath);     
        }

        void OnApplicationQuit()
        {
            SRanipal_Eye_Framework.Instance.EnableEyeDataCallback = false;
            writer.Flush();
            writer.Close();
            incrementsWriter.Flush();
            incrementsWriter.Close();
        }

        void Update()
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
            {
                UnityEngine.Debug.Log("Update returning...");
                return;
            }
            
            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback && !eye_callback_registered)
            {
                SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                UnityEngine.Debug.Log("EyeTrackingManager: EyeCallback registered (v1)");
                eye_callback_registered = true;
            }
            
            if (!callbackReceived)
                UnityEngine.Debug.Log("No callback yet...");
            
            // Write any buffered data to file
            if (calibrated)
            {
                while (logQueue.Count > 0)
                {
                    string line = logQueue.Dequeue();
                    writer.WriteLine(line);
                }
                writer.Flush();
            }
            else
            {
                while (incQueue.Count > 0)
                {
                    string line = incQueue.Dequeue();
                    incrementsWriter.WriteLine(line);
                }
                incrementsWriter.Flush();
            } 
        }

        [MonoPInvokeCallback]
        private static void EyeCallback(ref EyeData eyeData)
        {
            callbackCount += 1;
            if (callbackCount % logEveryN != 0) return;
            instance.callbackReceived = true;
            var timestamp = eyeData.timestamp;
            var pupilLeft = eyeData.verbose_data.left.pupil_diameter_mm;
            var pupilRight = eyeData.verbose_data.right.pupil_diameter_mm;
            var gazeLeft = eyeData.verbose_data.left.gaze_direction_normalized;
            var gazeRight = eyeData.verbose_data.right.gaze_direction_normalized;
            string line = $"{timestamp:F3},{luminance},{pupilLeft},{pupilRight}," +
                    $"{gazeLeft.x},{gazeLeft.y},{gazeLeft.z}," +
                    $"{gazeRight.x},{gazeRight.y},{gazeRight.z},";
            UnityEngine.Debug.Log($"Line: {line}");
            if (!instance.calibrated & instance.calibrating)
            {
                incQueue.Enqueue(line);
            }
            else
            {
                logQueue.Enqueue(line);
            }
        }
        
        private void FixedUpdate()
        {
            if (luminanceEnabled && luminanceCreated && Time.time - luminanceTime >= luminanceRate)
            {
                EyeTrackingManager.luminance = GetLuminance(luminanceCamera.targetTexture);
                //Debug.LogFormat("ETM Luminance {0}", EyeTrackingManager.luminance);
                luminanceTime = Time.time;
            }
        }
        

        private float GetLuminance(RenderTexture renderTexture)
        {
            RenderTexture oldRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false);
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0, false);
            texture2D.Apply();

            Color[] allColors = texture2D.GetPixels();

            float totalLuminance = 0f;

            foreach (Color color in allColors)
            {
                totalLuminance += (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            }

            float averageLuminance = totalLuminance / allColors.Length;

            RenderTexture.active = oldRenderTexture;
            UnityEngine.Object.Destroy(texture2D);

            return averageLuminance;
        }

        private void CreateLuminanceCamera()
        {
            GameObject cameraChild = new GameObject();
            cameraChild.name = "Luminance Camera";
            cameraChild.transform.parent = cam.gameObject.transform;
            luminanceCamera = cameraChild.AddComponent(typeof(Camera)) as Camera;
            luminanceCamera.fieldOfView = cam.fieldOfView;
            luminanceCamera.targetTexture = new RenderTexture(luminanceWidth, luminanceHeight, 24);
            cameraChild.transform.localPosition = new Vector3(0, 0, 0);
            cameraChild.transform.localRotation = new Quaternion(0, 0, 0, 0);
            luminanceCreated = true;
        }

        public void PupilCalibration()
        {
            increments = new List<float>();
            StartCoroutine(PupilCalibrationCo());
        }

        private IEnumerator PupilCalibrationCo()
        {
            UnityEngine.Debug.Log("Starting Pupil Calibration");
            string incPath = Path.Combine(logDirectory, "increments.csv");
            incrementsWriter = new StreamWriter(incPath);
            UnityEngine.Debug.Log("Calibration logging to: " + incPath);    
            incrementsWriter.WriteLine("Timestamp,Luminance,PupilDiameterLeft,PupilDiameterRight,GazeLeftX,GazeLeftY,GazeLeftZ,GazeRightX,GazeRightY,GazeRightZ"); // header row
            incrementsWriter.Flush();
            
            RawImage rawImage = calibrationScreen.GetComponent(typeof(RawImage)) as RawImage;
            float luminanceDelay = 10.0f;
            int colorVal = 0;
            byte colorByte = (byte)colorVal;
            //UnityMainThreadDispatcher.Instance().Enqueue(() => PupilCalibrationLog("pupil_calibration_started", 0f));
            Canvas canvas = calibrationScreen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; // Make sure it's Screen Space - Camera
            canvas.worldCamera = luminanceCamera;
            calibrationScreen.SetActive(true);
            calibrating = true;
            for (int i = 0; i < 18; i++)
            {
                colorVal = i * 15;
                colorByte = (byte)colorVal;
                rawImage.color = new Color32(colorByte, colorByte, colorByte, 255);
                yield return new WaitForSecondsRealtime(luminanceDelay);
                luminanceDelay = 2.0f;
            }
            calibrationScreen.SetActive(false);
            rawImage.color = new Color32(0, 0, 0, 255);
            calibrated = true;
            calibrating = false;
            
        }
    }
}

[System.Serializable]
public class Increments
{
    public string increments;
    public Increments (List<float> values)
    {
        increments = string.Join(",", values);
    }
}

[System.Serializable]
public class Fear
{
    public string pupil;
    public string luminance;
    public Fear (List<float> pupilValues, List<float> luminanceValues)
    {
        pupil = string.Join(",", pupilValues);
        luminance = string.Join(",", luminanceValues);
    }
}
