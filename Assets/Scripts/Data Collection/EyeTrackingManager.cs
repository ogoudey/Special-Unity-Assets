using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using ViveSR.anipal.Eye;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Diagnostics;
using System.Net.Http.Headers;

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
        private static Queue<string> logQueue = new Queue<string>();

        // Luminance stuff
        private bool luminanceEnabled = true;
        private bool luminanceCreated = false;
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
        private static List<float> fearIncrements;
        private static bool fearChecked = false;
        private static bool fearEnabled = true; //??

        internal class MonoPInvokeCallbackAttribute : System.Attribute
        {
            public MonoPInvokeCallbackAttribute() { }
        }

        void Start()
        {
            if (instance == null)
            {
                instance = this;
            }


            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                return;
            }
            //float waitTimeBeforeEyeDataCallbackStarted = 3.0f;
            //yield return new WaitForSecondsRealtime(waitTimeBeforeEyeDataCallbackStarted);
            SRanipal_Eye_Framework.Instance.EnableEyeDataCallback = true;

            // Find subject name, the camera is named after the subject
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

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;


            
            // Otherwise this is likely the calibrationscene

            cam = Camera.main;
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");  // e.g., 2025-10-22_14-30-00
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "AcroGenData",
                subjectName,
                sceneName,
                timestamp
            );
            Directory.CreateDirectory(logDirectory);
            string logPath = Path.Combine(logDirectory, "eye_tracking_log_test.csv");

            writer = new StreamWriter(logPath);
            writer.WriteLine("Timestamp,PupilDiameterLeft,PupilDiameterRight,GazeLeftX,GazeLeftY,GazeLeftZ,GazeRightX,GazeRightY,GazeRightZ"); // header row
            

            writer.Flush();

            if (luminanceEnabled)
            {
                CreateLuminanceCamera();
            }

            fearChecked = true;

            SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            UnityEngine.Debug.Log("EyeCallback registered and logging to: " + logPath);
        }

        void OnApplicationQuit()
        {
            SRanipal_Eye_Framework.Instance.EnableEyeDataCallback = false;
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
            }
        }

        void Update()
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
            {
                return;
            }

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback && !eye_callback_registered)
            {
                SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                UnityEngine.Debug.Log("EyeTrackingManager: EyeCallback registered (v1)");
                eye_callback_registered = true;
            }

            // Write any buffered data to file
            while (logQueue.Count > 0)
            {
                string line = logQueue.Dequeue();
                writer.WriteLine(line);
            }

            writer.Flush();
        }

        [MonoPInvokeCallback]
        private static void EyeCallback(ref EyeData eyeData)
        {
            if (callbackCount % logEveryN != 0) return;
            var timestamp = eyeData.timestamp;
            var pupilLeft = eyeData.verbose_data.left.pupil_diameter_mm;
            var pupilRight = eyeData.verbose_data.right.pupil_diameter_mm;
            var gazeLeft = eyeData.verbose_data.left.gaze_direction_normalized;
            var gazeRight = eyeData.verbose_data.right.gaze_direction_normalized;

            // Format a CSV line
            string line = $"{timestamp:F3},{pupilLeft},{pupilRight}," +
                        $"{gazeLeft.x},{gazeLeft.y},{gazeLeft.z}," +
                        $"{gazeRight.x},{gazeRight.y},{gazeRight.z},";

            logQueue.Enqueue(line);

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
            fearIncrements = new List<float>();
            StartCoroutine(PupilCalibrationCo());
        }

        private IEnumerator PupilCalibrationCo()
        {
            UnityEngine.Debug.Log("Starting Pupil Calibration");
            RawImage rawImage = calibrationScreen.GetComponent(typeof(RawImage)) as RawImage;
            UnityEngine.Debug.LogFormat("Starting pupil calibration now...");
            float luminanceDelay = 10.0f;
            int colorVal = 0;
            byte colorByte = (byte)colorVal;
            //UnityMainThreadDispatcher.Instance().Enqueue(() => PupilCalibrationLog("pupil_calibration_started", 0f));
            calibrationScreen.SetActive(true);
            for (int i = 0; i < 18; i++)
            {
                
                colorVal = i * 15;
                colorByte = (byte)colorVal;
                rawImage.color = new Color32(colorByte, colorByte, colorByte, 255);
                //UnityMainThreadDispatcher.Instance().Enqueue(() => PupilCalibrationLog(string.Format("pupil_calibration_{0}", colorVal), luminanceDelay));
                yield return new WaitForSecondsRealtime(luminanceDelay);
                UnityEngine.Debug.Log($"Light level: {colorVal}, diameter: {pupilDiameterLeft}.");
                Increment(pupilDiameterLeft, pupilDiameterRight);
                luminanceDelay = 2.0f;
            }
            calibrationScreen.SetActive(false);
            rawImage.color = new Color32(0, 0, 0, 255);
            //UnityMainThreadDispatcher.Instance().Enqueue(() => PupilCalibrationLog("pupil_calibration_ended", 0f));
            UpdateIncrements();
        }

        private static void Increment(float pupilDiameterLeft, float pupilDiameterRight)
        {
            if (fearChecked && fearEnabled)
            {
                fearIncrements.Add((pupilDiameterLeft + pupilDiameterRight) / 2);
            }
            else
            {
                UnityEngine.Debug.Log("Not adding increments properly...");
            }

        }

        public void UpdateIncrements()
        {
            UnityEngine.Debug.Log("Updateing Increments");
            string incrementsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "AcroGenData",
                subjectName
            );
            Directory.CreateDirectory(incrementsDirectory);
            string incPath = Path.Combine(incrementsDirectory, "increments.csv");

            StreamWriter incWriter = new StreamWriter(incPath);
            incWriter.WriteLine(string.Join(",", fearIncrements));
            incWriter.Flush();
            incWriter.Close();
            UnityEngine.Debug.Log("?? Fear increments configured");

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
