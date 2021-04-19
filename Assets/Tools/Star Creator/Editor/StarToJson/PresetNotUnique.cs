using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PresetNotUnique : EditorWindow
{
    private VisualElement _root;
    private Choice _choice;
    private bool _remember;
    private bool _choosed = false;
    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("PresetNotUnique");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("PresetNotUnique");
        _root.styleSheets.Add(styleSheet);
        _root.Query<Button>("KeepBoth").First().clicked += () => ChoiceMade(Choice.KeepBoth);
        _root.Query<Button>("Replace").First().clicked += () => ChoiceMade(Choice.Replace);
        _root.Query<Button>("Skip").First().clicked += () => ChoiceMade(Choice.Skip);
        _root.Query<Toggle>("RememberToggle").First().RegisterCallback<ChangeEvent<bool>>(OnRememberChange);
    }

    private void OnRememberChange(ChangeEvent<bool> evt)
    {
        _remember = evt.newValue;
    }

    private void ChoiceMade(Choice choice)
    {
        _choice = choice;
        _choosed = true;
        Close();
    }

    public async Task<(Choice choice, bool remember)> WaitForChoiceAsync()
    {
        while (!_choosed)
            await Task.Delay(10);
        return (_choice, _remember);
    }

    public enum Choice
    {
        KeepBoth,
        Replace,
        Skip
    }
}
