using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class StarExporter : EditorWindow
{
    private VisualElement _root;
    private Dictionary<string, bool> _presetToExport;
    private List<string> _presetNameToExport;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("StarExporter");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("StarExporter");
        _root.styleSheets.Add(styleSheet);
    }

    public void Init(List<string> presetNameToExport)
    {
        _presetNameToExport = presetNameToExport;
        _presetToExport = presetNameToExport.ToDictionary(item => item, item => true);

        _root.Query<Button>("ExportButton").First().clicked += OnExportClicked;
        _root.Query<Button>("Cancel").First().clicked += Close;

        // Build list
        Func<VisualElement> makeItem = () => Resources.Load<VisualTreeAsset>("ExportListItem").Instantiate();
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
    private async void OnExportClicked()
    {
        // Get assets to export
        var assetsToExport = _presetToExport.Where(item => item.Value)?.Select(item => item.Key)?.ToList();
        if (assetsToExport == null || assetsToExport.Count == 0) {
            EditorUtility.DisplayDialog("Export result", "No preset selectioned", "Ok");
            return;
        }

        // Get path
        string userPath = null;
        if (Application.platform == RuntimePlatform.WindowsEditor)
            userPath = Environment.GetEnvironmentVariable("USERPROFILE");
        else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor)
            userPath = "~";

        if (userPath == null)
            userPath = "";
        var pathToExport = EditorUtility.SaveFilePanel("Select save location", userPath, "Unity star preset export", "json");
        if (string.IsNullOrEmpty(pathToExport)) {
            EditorUtility.DisplayDialog("Export result", "No save file selected", "Ok");
            return;
        }

        // Export
        if (await ImportExport.Export(pathToExport, assetsToExport))
        {
            EditorUtility.DisplayDialog("Export result", "Export is done !", "Close");
            Close();
        }
        else {
            EditorUtility.DisplayDialog("Export result", "Export failed, see console log for more detailed.", "Ok");
        }
    }

    private void OnToggleClicked(ChangeEvent<bool> evt, int index)
    {
        _presetToExport[_presetNameToExport[index]] = evt.newValue;
    }
    #endregion
}
