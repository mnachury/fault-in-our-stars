using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PresetNotUnique : EditorWindow
{
    private VisualElement _root;
    private Label _label;
    private ConflictStrategy _conflictStrat;
    private bool _remember;
    private bool _choosed = false;

    public void CreateGUI()
    {
        _root = rootVisualElement;
        var UI = Resources.Load<VisualTreeAsset>("PresetNotUnique");
        UI.CloneTree(_root);
        var styleSheet = Resources.Load<StyleSheet>("PresetNotUnique");
        _root.styleSheets.Add(styleSheet);
        _root.Query<Button>("KeepBoth").First().clicked += () => ChoiceMade(ConflictStrategy.KeepBoth);
        _root.Query<Button>("Replace").First().clicked += () => ChoiceMade(ConflictStrategy.Replace);
        _root.Query<Button>("Skip").First().clicked += () => ChoiceMade(ConflictStrategy.Skip);
        _root.Query<Toggle>("RememberToggle").First().RegisterCallback<ChangeEvent<bool>>(OnRememberChange);
        _label = _root.Query<Label>("Label").First();
    }

    public void Init(string presetName)
    {
        _label.text = $"A preset named {presetName} already exist";
    }

    private void OnRememberChange(ChangeEvent<bool> evt)
    {
        _remember = evt.newValue;
    }

    private void ChoiceMade(ConflictStrategy choice)
    {
        _conflictStrat = choice;
        _choosed = true;
        Close();
    }

    public async Task<(ConflictStrategy choice, bool remember)> WaitForChoiceAsync()
    {
        while (!_choosed)
            await Task.Delay(10);
        return (_conflictStrat, _remember);
    }

}
