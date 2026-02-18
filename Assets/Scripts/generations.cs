using UnityEditor;
using UnityEngine;
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

public static class GenerationsLoader
{
    public static Dictionary<string, Dictionary<string, string>> LoadGenerations(string fileName = "Generations.json")
    {
        string path = Path.Combine(Application.dataPath, fileName);

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            UnityEngine.Debug.LogWarning($"⚠️ {fileName} is empty. Returning empty generations dictionary.");
            return new Dictionary<string, Dictionary<string, string>>();
        }
        
        GenerationsWrapper wrapper = JsonUtility.FromJson<GenerationsWrapper>(json);

        Dictionary<string, Dictionary<string, string>> generations = new Dictionary<string, Dictionary<string, string>>();
        foreach (var entry in wrapper.generations)
        {
            if (string.IsNullOrEmpty(entry.WorldName))
            {
                UnityEngine.Debug.LogWarning("⚠️ Skipping generation with missing WorldName.");
                continue;
            }
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict["Prompt"] = entry.Prompt;
            generations[entry.WorldName] = dict;
        }
        // Fill out subjects list:
        UnityEngine.Debug.Log($"✅ Generations loaded from {path}");
        return generations;
    }

    public static void DumpGeneration(string worldName, string prompt, string fileName = "Generations.json")
    {
        var generations = LoadGenerations();
        GenerationsWrapper wrapper = new GenerationsWrapper();
        foreach (var kvp in generations)
        {
            var entry = new GenerationEntry();
            entry.WorldName = kvp.Key;
            entry.Prompt = kvp.Value["Prompt"];
            wrapper.generations.Add(entry);
        }
        var newEntry = new GenerationEntry();
        newEntry.WorldName = worldName;
        newEntry.Prompt = prompt;
        wrapper.generations.Add(newEntry);

        string path = Path.Combine(Application.dataPath, fileName);
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"✅ Generations saved to {path}");


    }
}

[System.Serializable]
public class GenerationEntry
{
    public string WorldName;
    public string Prompt;
}


[System.Serializable]
public class GenerationsWrapper
{
    public List<GenerationEntry> generations = new List<GenerationEntry>();
}