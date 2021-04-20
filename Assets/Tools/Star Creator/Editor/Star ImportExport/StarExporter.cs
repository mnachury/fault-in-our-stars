using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class StarExporter : EditorWindow
{
    private VisualElement _root;
    private Button _exportButton;
    private VisualElement _listContainer;
    private Label _title;
    private Dictionary<string, bool> _presetListDict;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarImportExport");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarImportExport");
        _root.styleSheets.Add(styleSheet);
        _exportButton = _root.Query<Button>("ExportButton").First();
        _exportButton.clicked += OnExportClicked;
        _listContainer = _root.Query<VisualElement>("PresetListContainer").First();
        _title = _root.Query<Label>("Title").First();
        _root.Query<Button>("CancelButton").First().clicked += Close;

        var buttonContainer = _root.Query<VisualElement>("ButtonContainer").First();
        buttonContainer.Remove(_root.Query<Button>("ImportButton").First());
        buttonContainer.Remove(_root.Query<Button>("LoadButton").First());
    }

    public void Init(List<string> presetNames)
    {
        _presetListDict = presetNames.ToDictionary(item => item, item => true);
        _title.text = "Preset exporter";
        var listView = ImportExportCommon.Populate(presetNames, (e, preset) => _presetListDict[preset] = e.newValue);
        _listContainer.Clear();
        _listContainer.Add(listView);
    }
   
    private async void OnExportClicked()
    {
        // Get assets to export
        var assetsToExport = _presetListDict.Where(item => item.Value)?.Select(item => item.Key)?.ToList();
        if (assetsToExport == null || assetsToExport.Count == 0)
        {
            EditorUtility.DisplayDialog("Export result", "No preset selectioned", "Ok");
            return;
        }


        var pathToExport = EditorUtility.SaveFilePanel("Select save location", ImportExportCommon.GetUserPath(), "Star Presets", "starpresets");
        if (string.IsNullOrEmpty(pathToExport))
        {
            EditorUtility.DisplayDialog("Export result", "No save file selected", "Ok");
            return;
        }

        // Export
        if (await Export(pathToExport, assetsToExport))
        {
            EditorUtility.DisplayDialog("Export result", "Export is done !", "Close");
            Close();
        }
        else
        {
            EditorUtility.DisplayDialog("Export result", "Export failed, see console log for more detailed.", "Ok");
        }
    }

    private async Task<bool> Export(string pathToExport, List<string> presetsToExport)
    {
        List<string> starJsons = new List<string>(presetsToExport.Count * 2);
        foreach (string preset in presetsToExport)
        {
            var presetPath = $"{EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey)}/{preset}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(presetPath);
            if (prefab != null && prefab.TryGetComponent<StarController>(out var starController))
            {
                starJsons.Add(preset);
                starJsons.Add(JsonUtility.ToJson(starController));
            }
            else
            {
                Debug.LogError($"Invalid preset {preset} in {presetPath}");
                return false;
            }
        }
        try
        {
            using (StreamWriter writer = File.CreateText(pathToExport))
            {
                foreach (var starjson in starJsons)
                    await writer.WriteLineAsync(starjson);
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
