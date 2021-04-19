using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class ImportExport 
{
    public static async Task<bool> Export(string pathToExport, List<string> presetsToExport)
    {
        var assets = Directory.GetFiles(EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey));
        StringBuilder jsonOutput = new StringBuilder("{");
        foreach (string preset in presetsToExport)
        {
            var presetPath = $"{EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey)}/{preset}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(presetPath);
            if (prefab != null && prefab.TryGetComponent<StarController>(out var starController))
            {
                jsonOutput.Append($"\"{preset}\":{JsonUtility.ToJson(starController)},");
            }
            else
            {
                Debug.LogError($"Invalid preset {preset} in {presetPath}");
                return false;
            }
        }
        jsonOutput.Remove(jsonOutput.Length - 1, 1).Append("}");
        try
        {
            using (StreamWriter writer = File.CreateText(pathToExport))
            {
                await writer.WriteAsync(jsonOutput.ToString());
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not write to file {pathToExport} beacause of {e}");
            return false;
        }
        return true;
    }
}
