using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class StarToJson : EditorWindow
{
    private VisualElement _root;
    private Dictionary<string, bool> _presetToExport;
    private List<string> _presetNameToExport;
    private List<(string preset, string json)> _starPresetToImport;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarToJson");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarToJson");
        _root.styleSheets.Add(styleSheet);
    }

    public void Init(bool import)
    {
        if (import)
        {
            _root.Query<Label>("PopupTitle").First().text = "Preset Importer";
            _root.Query<VisualElement>("ExportContainer").First().visible = false;
            _root.Query<Button>("ImportButton").First().clicked += OnImportClicked;
            _root.Query<Button>("LoadButton").First().clicked += OnLoadClicked;
        }
        else
        {
            _root.Query<Label>("PopupTitle").First().text = "Preset Exporter";
            _root.Query<VisualElement>("ImportContainer").First().visible = false;
            _root.Query<Button>("ExportButton").First().clicked += OnExportClicked;
        }
        _root.Query<Button>("CancelButton").First().clicked += Close;


    }

    public void Populate(List<string> presetNames)
    {
        _presetNameToExport = presetNames;
        _presetToExport = presetNames.ToDictionary(item => item, item => true);

        // Build list
        Func<VisualElement> makeItem = () => Resources.Load<VisualTreeAsset>("StarToJsonListItem").Instantiate();
        Action<VisualElement, int> bindItem = (root, index) =>
        {
            root.Query<Label>("ItemLabel").First().text = _presetNameToExport[index];
            var itemToggle = root.Query<Toggle>("ItemToggle").First();
            itemToggle.value = true;
            itemToggle.RegisterCallback<ChangeEvent<bool>>((e) => OnToggleClicked(e, index));
        };
        const int itemHeight = 24;
        var listView = new ListView(_presetNameToExport, itemHeight, makeItem, bindItem);
        listView.selectionType = SelectionType.Single;
        listView.style.flexGrow = 1.0f;
        var list = _root.Query<VisualElement>("PresetListContainer").First();
        list.Add(listView);
    }

    #region callbacks
    private void OnLoadClicked()
    {
        var file = EditorUtility.OpenFilePanel("Select file location", GetUserPath(), "starpresets");
        if (string.IsNullOrEmpty(file))
        {
            EditorUtility.DisplayDialog("Import result", "No file selected", "Ok");
            return;
        }
        var lines = File.ReadAllLines(file);
        _starPresetToImport = new List<(string name, string preset)>(lines.Length / 2);
        for (int i = 0; i < lines.Length - 1; i += 2)
        {
            _starPresetToImport.Add((lines[i], lines[i + 1]));
        }
        Populate(_starPresetToImport.Select(item => item.preset).ToList());
    }

    private async void OnImportClicked()
    {
        if (_starPresetToImport == null || _starPresetToImport.Count == 0)
        {
            EditorUtility.DisplayDialog("Import result", "No preset to import. First load a file", "Ok");
            return;
        }
        var savePath = EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey) ;
        var starDefaultPrefabPath = AssetDatabase.GetAssetPath(Resources.Load<GameObject>("DefaultStarPreset"));
        var alreadyExistingPreset = Directory.GetFiles(savePath).ToList();
        bool alwaysReplace = false;
        bool alwaysKeepBoth = false;
        bool alwaysSkip = false;
        foreach (var starPreset in _starPresetToImport)
        {
            // Make sure asset is unique or that we know what to do
            bool isPresetNameUnique;
            bool skip = false;
            bool replace = false;
            bool keepBoth = false;
            int i = 0;
            string initialName = starPreset.preset;
            string presetName = initialName;
            do
            {
                isPresetNameUnique = !alreadyExistingPreset.Contains($"{savePath}\\{presetName}.prefab");
                // If preset already exist
                if (!isPresetNameUnique)
                {
                    // If we don't now what to do
                    if (!alwaysReplace && !alwaysKeepBoth && !alwaysSkip)
                    {
                        // We ask
                        var presetNotUniqueModal = CreateInstance<PresetNotUnique>();
                        presetNotUniqueModal.ShowUtility();
                        var response = await presetNotUniqueModal.WaitForChoiceAsync();
                        switch (response.choice)
                        {
                            case PresetNotUnique.Choice.KeepBoth:
                                if (response.remember)
                                    alwaysKeepBoth = true;
                                else
                                    keepBoth = true;
                                break;
                            case PresetNotUnique.Choice.Replace:
                                if (response.remember)
                                    alwaysReplace = true;
                                else
                                    replace = true;
                                break;
                            case PresetNotUnique.Choice.Skip:
                                if (response.remember)
                                    alwaysSkip = true;
                                else
                                    skip = true;
                                break;
                        }
                    }
                    // if we need to find a new name
                    if (keepBoth || alwaysKeepBoth)
                        presetName = $"{initialName} ({++i})";

                }
                // If preset is unique or we plan to replace/skip. We can continue
            } while (!isPresetNameUnique && (!alwaysReplace && !alwaysSkip && !replace && !skip));
            if (!isPresetNameUnique && (alwaysSkip || skip))
                continue;

            var newAssetPath = $"{savePath}/{presetName}.prefab";
            if (!AssetDatabase.CopyAsset(starDefaultPrefabPath, newAssetPath))
            {
                Debug.LogWarning($"Import of asset {presetName} failed, skipping preset. Check console for more details.");
                continue;
            }
            alreadyExistingPreset.Add(newAssetPath);
            var newAsset = AssetDatabase.LoadAssetAtPath<GameObject>(newAssetPath);
            newAsset.TryGetComponent<StarController>(out var starController);
            JsonUtility.FromJsonOverwrite(starPreset.json, starController);
        }
        EditorUtility.DisplayDialog("Import result", "Import is done !", "Close");
        Close();
    }

    private async void OnExportClicked()
    {
        // Get assets to export
        var assetsToExport = _presetToExport.Where(item => item.Value)?.Select(item => item.Key)?.ToList();
        if (assetsToExport == null || assetsToExport.Count == 0)
        {
            EditorUtility.DisplayDialog("Export result", "No preset selectioned", "Ok");
            return;
        }


        var pathToExport = EditorUtility.SaveFilePanel("Select save location", GetUserPath(), "Unity star preset export", "starpresets");
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

    private void OnToggleClicked(ChangeEvent<bool> evt, int index)
    {
        _presetToExport[_presetNameToExport[index]] = evt.newValue;
    }
    #endregion

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

    private string GetUserPath()
    {
        string userPath = null;
        if (Application.platform == RuntimePlatform.WindowsEditor)
            userPath = Environment.GetEnvironmentVariable("USERPROFILE");
        else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor)
            userPath = "~";
        if (userPath == null)
            userPath = "";
        return userPath;
    }
}
