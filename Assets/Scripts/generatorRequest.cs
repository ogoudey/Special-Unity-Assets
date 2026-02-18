using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class GeneratorRequest
{
    public string worldName;
    public MultiSceneMode multiSceneMode;
    public string prompt;
    public SubjectType subjectType;
    public bool useDataCollectionAssets;

    public async void Send()
    {
        {
            /* OLD
            string url =
                "http://127.0.0.1:5000/generate" +
                $"?world_name={UnityWebRequest.EscapeURL(worldName)}" +
                $"&prompt={UnityWebRequest.EscapeURL(promptText)}" +
                $"&assets={UnityWebRequest.EscapeURL(Application.dataPath)}";
            */
            
            string url =
                "http://127.0.0.1:5000/generate" +
                $"?world_name={UnityWebRequest.EscapeURL(worldName)}" +
                $"&multi_scene_mode={UnityWebRequest.EscapeURL(multiSceneMode.ToString())}" +
                $"&prompt={UnityWebRequest.EscapeURL(prompt)}" +
                $"&subject_type={UnityWebRequest.EscapeURL(subjectType.ToString())}" +
                $"&assets={UnityWebRequest.EscapeURL(Application.dataPath)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                // Unity 2019.4 error handling
                if (request.isNetworkError || request.isHttpError)
                {
                    UnityEngine.Debug.LogError(request.error);
                }
            }          
        }
    }
}