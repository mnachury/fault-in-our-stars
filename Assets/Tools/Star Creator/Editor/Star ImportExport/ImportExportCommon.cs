using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class ImportExportCommon  
{
    public static ListView Populate(List<string> presetNames, Action<ChangeEvent<bool>, string> onEllementToggle)
    {
        Func<VisualElement> makeItem = () => Resources.Load<VisualTreeAsset>("StarImportExportListItem").Instantiate();
        Action<VisualElement, int> bindItem = (root, index) =>
        {
            var presetName = presetNames[index];
            root.Query<Label>("ItemLabel").First().text = presetName;
            var itemToggle = root.Query<Toggle>("ItemToggle").First();
            itemToggle.value = true;
            itemToggle.RegisterCallback<ChangeEvent<bool>>((e) => onEllementToggle(e, presetName));
        };
        const int itemHeight = 24;
        var listView = new ListView(presetNames, itemHeight, makeItem, bindItem);
        listView.selectionType = SelectionType.Single;
        listView.style.flexGrow = 1.0f;
        return listView;
    }

    public static string GetUserPath()
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
