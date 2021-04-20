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
    private Button _exportButton;
    private Button _importButton;
    private Button _loadFileButton;
    private VisualElement _listContainer;
    private VisualElement _buttonContainer;
    private Label _title;
    private Dictionary<string, bool> _presetListDict;
    private List<string> _presetNames;
    private List<(string preset, string json)> _starPresetToImport;
    private Action _refresh;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarToJson");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarToJson");
        _root.styleSheets.Add(styleSheet);
        _exportButton = _root.Query<Button>("ExportButton").First();
        _exportButton.clicked += OnExportClicked;
        _importButton = _root.Query<Button>("ImportButton").First();
        _importButton.clicked += OnImportClicked;
        _loadFileButton = _root.Query<Button>("LoadButton").First();
        _loadFileButton.clicked += OnLoadClicked;
        _listContainer = _root.Query<VisualElement>("PresetListContainer").First();
        _buttonContainer = _root.Query<VisualElement>("ButtonContainer").First();
        _title = _root.Query<Label>("Title").First();
        _root.Query<Button>("CancelButton").First().clicked += Close;
    }

    public void Init(bool import, Action refresh = null)
    {
        _refresh = refresh;
        if (import)
        {
            _importButton.visible = false;
            _buttonContainer.Remove(_exportButton);
            _title.text = "Preset importer";
        }
        else
        {
            _buttonContainer.Remove(_importButton);
            _buttonContainer.Remove(_loadFileButton);
            _title.text = "Preset exporter";
        }
    }

    public void Populate(List<string> presetNames)
    {
        _presetNames = presetNames;
        _presetListDict = presetNames.ToDictionary(item => item, item => true);

        // Build list
        Func<VisualElement> makeItem = () => Resources.Load<VisualTreeAsset>("StarToJsonListItem").Instantiate();
        Action<VisualElement, int> bindItem = (root, index) =>
        {
            root.Query<Label>("ItemLabel").First().text = _presetNames[index];
            var itemToggle = root.Query<Toggle>("ItemToggle").First();
            itemToggle.value = true;
            itemToggle.RegisterCallback<ChangeEvent<bool>>((e) => OnToggleClicked(e, index));
        };
        const int itemHeight = 24;
        var listView = new ListView(_presetNames, itemHeight, makeItem, bindItem);
        listView.selectionType = SelectionType.Single;
        listView.style.flexGrow = 1.0f;
        _listContainer.Clear();
        _listContainer.Add(listView);
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
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("Import result", "No preset in file", "Ok");
            return;
        }
        _starPresetToImport = new List<(string name, string preset)>(lines.Length / 2);
        for (int i = 0; i < lines.Length - 1; i += 2)
        {
            _starPresetToImport.Add((lines[i], lines[i + 1]));
        }
        Populate(_starPresetToImport.Select(item => item.preset).ToList());
        _importButton.visible = true;
    }

    private async void OnImportClicked()
    {
        if (_starPresetToImport == null || _starPresetToImport.Count == 0)
        {
            EditorUtility.DisplayDialog("Import result", "No preset to import. First load a file", "Ok");
            return;
        }
        var savePath = EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey);
        var starDefaultPrefabPath = AssetDatabase.GetAssetPath(Resources.Load<GameObject>("DefaultStarPreset"));
        var alreadyExistingPreset = Directory.EnumerateFiles(savePath).ToList();
        ConflictStrategy conflictStrat = ConflictStrategy.Undecided;
        bool rememberConflictStrat = false;
        // Filter only ticked presets
        var starPresetToImport = _starPresetToImport.Where(item => _presetListDict[item.preset]);
        foreach (var starPreset in starPresetToImport)
        {
            // Make sure asset is unique or that we know what to do
            if (!rememberConflictStrat)
                conflictStrat = ConflictStrategy.Undecided;
            bool isPresetNameUnique;
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
                    if (conflictStrat == ConflictStrategy.Undecided)
                    {
                        // We ask
                        var presetNotUniqueModal = CreateInstance<PresetNotUnique>();
                        presetNotUniqueModal.ShowUtility();
                        presetNotUniqueModal.Init(initialName);
                        var response = await presetNotUniqueModal.WaitForChoiceAsync();
                        rememberConflictStrat = response.remember;
                        conflictStrat = response.choice;
                    }
                    // if we need to find a new name, we try
                    if (conflictStrat == ConflictStrategy.KeepBoth)
                        presetName = $"{initialName} ({++i})";
                }
                // If preset is unique or we plan to replace/skip. We can continue
            } while (!isPresetNameUnique && (conflictStrat != ConflictStrategy.Replace && conflictStrat != ConflictStrategy.Skip));
            if (!isPresetNameUnique && conflictStrat == ConflictStrategy.Skip)
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
        _refresh();
        Close();
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


        var pathToExport = EditorUtility.SaveFilePanel("Select save location", GetUserPath(), "Star Presets", "starpresets");
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
        _presetListDict[_presetNames[index]] = evt.newValue;
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
