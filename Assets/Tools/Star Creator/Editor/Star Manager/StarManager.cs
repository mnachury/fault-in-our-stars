using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.IO;

public class StarManager : EditorWindow
{
    /// <summary>
    /// Editor pref key for the location of the star presets saved as prefab in the project
    /// </summary>
    public const string StarPresetLocationEditorPrefKey = "StarCreatorPresetLocation";
    private const string _starPrefabDefaultSaveLocation = "Assets/Prefabs/Star prefabs";
    private string _starDefaultPrefabPath;
    private VisualElement _root;
    private List<string> _starPresets;
    private ListView _listview;
    private VisualElement _inspectorContainer;
    private VisualElement _presetEditorContainer;
    private Label _presetSelectedLabel;
    private StarToJson _starToJsonWindow;

    public void OnFocus()
    {
        RefreshPresetList();
    }

    [MenuItem("Window/Star Creator")]
    public static void ShowWindow()
    {
        StarManager wnd = GetWindow<StarManager>();
        wnd.titleContent = new GUIContent("Star creator");
    }

    public void CreateGUI()
    {
        if (!EditorPrefs.HasKey(StarPresetLocationEditorPrefKey))
            EditorPrefs.SetString(StarPresetLocationEditorPrefKey, _starPrefabDefaultSaveLocation);
        _starDefaultPrefabPath = AssetDatabase.GetAssetPath(Resources.Load<GameObject>("DefaultStarPreset"));
        if (!MakeSureFolderExist(EditorPrefs.GetString(StarPresetLocationEditorPrefKey)))
            return;
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarManager");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarManager");
        _root.styleSheets.Add(styleSheet);

        BuildListView();

        var newButton = _root.Query<Button>("NewButton").First();
        newButton.clicked += OnNewClicked;

        _inspectorContainer = _root.Query<VisualElement>("InspectorContainer").First();

        _presetEditorContainer = _root.Query<VisualElement>("PresetEditorContainer").First();
        _presetEditorContainer.visible = false;

        _presetSelectedLabel = _root.Query<Label>("PresetSelectedLabel").First();

        _root.Query<Button>("ExportButton").First().clicked += OnExportButtonClicked;
        _root.Query<Button>("ImportButton").First().clicked += OnImportButtonClicked;

        RefreshPresetList();
    }

    #region ListView
    private void RefreshPresetList()
    {
        _starPresets.Clear();
        var assets = Directory.GetFiles(EditorPrefs.GetString(StarPresetLocationEditorPrefKey));
        foreach (string asset in assets)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            if (prefab != null && prefab.TryGetComponent<StarController>(out var starController))
                _starPresets.Add(prefab.name);
        }
        _listview.Refresh();
    }

    private void BuildListView()
    {
        _starPresets = new List<string>();

        Func<VisualElement> makeItem = () => Resources.Load<VisualTreeAsset>("StarPresetListItem").Instantiate();
        Action<VisualElement, int> bindItem = (root, index) =>
        {
            var presetName = root.Query<TextField>("PresetName").First();
            presetName.value = _starPresets[index];
            presetName.RegisterCallback<FocusOutEvent>((e) => OnPresetNameChange(presetName, index));

            root.Query<Button>("Add").First().clicked += () => OnAddClicked(index);
            root.Query<Button>("Edit").First().clicked += () => OnEditClicked(index);
            root.Query<Button>("Copy").First().clicked += () => OnCopyClicked(index);
            root.Query<Button>("Delete").First().clicked += () => OnDeleteClicked(index);
        };
        const int itemHeight = 24;
        _listview = new ListView(_starPresets, itemHeight, makeItem, bindItem);
        _listview.selectionType = SelectionType.Single;
        _listview.style.flexGrow = 1.0f;

        var list = _root.Query<VisualElement>("ListViewContainer").First();
        list.Add(_listview);
    }
    #endregion

    #region callbacks
    private void OnImportButtonClicked()
    {
        if (_starToJsonWindow != null)
            _starToJsonWindow.Close();
        _starToJsonWindow = CreateInstance<StarToJson>();
        _starToJsonWindow.position = position;
        _starToJsonWindow.ShowPopup();
        _starToJsonWindow.Init(true);
    }

    private void OnExportButtonClicked()
    {
        if (_starPresets == null || _starPresets.Count == 0)
        {
            EditorUtility.DisplayDialog("Export result", "No preset to export, create some first.", "Ok");
            return;
        }
        if (_starToJsonWindow != null)
            _starToJsonWindow.Close();
        _starToJsonWindow = CreateInstance<StarToJson>();
        _starToJsonWindow.position = position;
        _starToJsonWindow.ShowPopup();
        _starToJsonWindow.Init(false);
        _starToJsonWindow.Populate(_starPresets);
    }

    private void OnAddClicked(int index)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{_starPresets[index]}.prefab");
        asset.TryGetComponent<StarController>(out var controller);
        var prefab = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        prefab.name = controller.Name;
        PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        EditorUtility.SetDirty(prefab);
    }

    private void OnCopyClicked(int index)
    {
        var newPreset = CopyPrefab($"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{_starPresets[index]}.prefab");
        if (newPreset != "")
            OnEditClicked(_starPresets.IndexOf(newPreset));
    }

    private void OnEditClicked(int index)
    {
        _inspectorContainer.Clear();

        var starInspector = CreateInstance<StarInspector>();
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{_starPresets[index]}.prefab");
        asset.TryGetComponent<StarController>(out var controller);
        starInspector.ChangeTarget(controller);
        _inspectorContainer.Add(starInspector.CreateInspectorGUI());
        _presetSelectedLabel.text = $"Editing {_starPresets[index]}";
        _presetEditorContainer.visible = true;
    }

    private void OnPresetNameChange(TextField textField, int index)
    {
        if (_starPresets[index] != textField.value)
        {
            var error = AssetDatabase.MoveAsset($"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{_starPresets[index]}.prefab", $"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{textField.value}.prefab");
            if (error != "")
            {
                Debug.LogWarning($"Invalid name");
                textField.value = _starPresets[index];
            }
            else
            {
                RefreshPresetList();
                OnEditClicked(_starPresets.IndexOf(textField.value));
            }
        }

    }

    private void OnNewClicked()
    {
        var newPreset = CopyPrefab(_starDefaultPrefabPath);
        if (newPreset != "")
            OnEditClicked(_starPresets.IndexOf(newPreset));
    }

    private void OnDeleteClicked(int index)
    {
        if (EditorUtility.DisplayDialog("Delete confirmation", $"Are you sure that you want to delete the preset {_starPresets[index]}", "Delete", "Cancel"))
        {
            AssetDatabase.DeleteAsset($"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{_starPresets[index]}.prefab");
            _presetEditorContainer.visible = false;
            RefreshPresetList();
        }
    }

    #endregion

    #region Utils
    private bool MakeSureFolderExist(string path)
    {
        var folders = path.Split('/');
        var validFoldersFromRoot = "Assets";
        // Skipping the first folder cause it's always assets
        for (int i = 1; i < folders.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder($"{validFoldersFromRoot}/{folders[i]}"))
            {
                if (AssetDatabase.CreateFolder(validFoldersFromRoot, folders[i]) == "")
                {
                    Debug.LogError($"Could not create the folder {validFoldersFromRoot}/{folders[i]}. Create it manually or change the starPrefabSaveLocation in StarManager");
                    return false;
                }
            }
            validFoldersFromRoot += $"/{folders[i]}";
        }
        return true;
    }

    private string CopyPrefab(string prefabPath)
    {
        string starName;
        int i = 0;
        do
        {
            starName = $"Star preset {++i}";
        } while (_starPresets.Contains(starName));

        if (!AssetDatabase.CopyAsset(prefabPath, $"{EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{starName}.prefab"))
        {
            Debug.LogError($"Asset copy failed, make sure you have acces to {EditorPrefs.GetString(StarPresetLocationEditorPrefKey)} in your asset folder");
            return "";
        }
        var newPreset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ EditorPrefs.GetString(StarPresetLocationEditorPrefKey)}/{ starName}.prefab");
        if (newPreset.TryGetComponent<StarController>(out var newStarController))
            newStarController.Name = $"Star {i}";
        newPreset.name = starName;

        RefreshPresetList();
        return starName;
    }
    #endregion
}