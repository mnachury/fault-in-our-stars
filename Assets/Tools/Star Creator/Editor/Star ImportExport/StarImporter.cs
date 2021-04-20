using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class StarImporter : EditorWindow
{
    private VisualElement _root;
    private Button _importButton;
    private Button _loadFileButton;
    private VisualElement _listContainer;
    private Label _title;
    private Dictionary<string, bool> _presetListDict;
    private List<(string preset, string json)> _starPresetToImport;
    private Action _refresh;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarImportExport");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarImportExport");
        _root.styleSheets.Add(styleSheet);

        _importButton = _root.Query<Button>("ImportButton").First();
        _importButton.clicked += OnImportClicked;
        _loadFileButton = _root.Query<Button>("LoadButton").First();
        _loadFileButton.clicked += OnLoadClicked;
        _listContainer = _root.Query<VisualElement>("PresetListContainer").First();
        _title = _root.Query<Label>("Title").First();
        _root.Query<Button>("CancelButton").First().clicked += Close;

        var exportButton = _root.Query<Button>("ExportButton").First();
        _root.Query<VisualElement>("ButtonContainer").First().Remove(exportButton);
    }

    public void Init(Action refresh)
    {
        _refresh = refresh;
        _importButton.visible = false;
        _title.text = "Preset importer";
    }

    #region callbacks
    private void OnLoadClicked()
    {
        var file = EditorUtility.OpenFilePanel("Select file location", ImportExportCommon.GetUserPath(), "starpresets");
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

        _presetListDict = _starPresetToImport.ToDictionary(item => item.preset, item => true);
        var listView = ImportExportCommon.Populate(
            _presetListDict.Select(item => item.Key).ToList(),
            (e, preset) => _presetListDict[preset] = e.newValue);
        _listContainer.Clear();
        _listContainer.Add(listView);
        _importButton.visible = true;
    }

    private async void OnImportClicked()
    {
        if (_starPresetToImport == null || _starPresetToImport.Count == 0)
        {
            EditorUtility.DisplayDialog("Import result", "No preset to import. First load a file", "Ok");
            return;
        }
        var starPresetToImport = _starPresetToImport.Where(item => _presetListDict[item.preset]).ToArray();
        if (starPresetToImport == null || starPresetToImport.Length == 0)
        {
            EditorUtility.DisplayDialog("Import result", "No preset selected to import. Select some or cancel import", "Ok");
            return;
        }
        var savePath = EditorPrefs.GetString(StarManager.StarPresetLocationEditorPrefKey);
        var starDefaultPrefabPath = AssetDatabase.GetAssetPath(Resources.Load<GameObject>("DefaultStarPreset"));
        var alreadyExistingPreset = Directory.EnumerateFiles(savePath).ToList();
        ConflictStrategy conflictStrat = ConflictStrategy.Undecided;
        bool rememberConflictStrat = false;
        // Filter only ticked presets
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
    #endregion
}
