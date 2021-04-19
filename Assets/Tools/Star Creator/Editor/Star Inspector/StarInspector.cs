using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(StarController))]
[ExecuteInEditMode]
public class StarInspector : Editor
{
    private StarController _star;
    private VisualTreeAsset UI;
    private StyleSheet StyleSheet;
    private VisualElement _root;

    private TextField _name;
    private FloatField _radiusInput;
    private Slider _radiusSlider;
    private Slider _gravityWellSlider;

    public void ChangeTarget(StarController star)
    {
        _star = star;
    }

    private void OnEnable()
    {
        try
        {
            _star = target as StarController;
        }
        catch { }


        _root = new VisualElement();
        UI = Resources.Load<VisualTreeAsset>("StarInspector");
        StyleSheet = Resources.Load<StyleSheet>("StarInspector");
        _root.styleSheets.Add(StyleSheet);
    }

    public override VisualElement CreateInspectorGUI()
    {
        var root = _root;
        UI.CloneTree(_root);

        // Name
        _name = root.Query<TextField>("Name").First();
        _name.value = _star.Name;
        _name.RegisterCallback<ChangeEvent<string>>(OnNameChange);

        // Color
        var _color = root.Query<ColorField>("Color").First();
        _color.value = _star.Color;
        _color.RegisterCallback<ChangeEvent<Color>>(OnColorChange);


        // Radius's power slider
        _radiusSlider = root.Query<Slider>("RadiusSlider").First();
        _radiusSlider.value = CalcSliderPosFromRadius(_star.Radius);
        _radiusSlider.RegisterCallback<ChangeEvent<float>>(OnRadiusSliderChange);

        _radiusInput = root.Query<FloatField>("RadiusInput").First();
        _radiusInput.value = _star.Radius;
        _radiusInput.RegisterCallback<ChangeEvent<float>>(OnRadiusInputChange);
        _radiusInput.maxLength = 5;

        // Gravity well radius
        _gravityWellSlider = root.Query<Slider>("GravityWellSlider").First();
        _gravityWellSlider.value = _star.GravityWellRadius;
        _gravityWellSlider.RegisterCallback<ChangeEvent<float>>(OnGravityWellSliderChange);

        return root;
    }

    #region Callbacks
    private void OnNameChange(ChangeEvent<string> evt)
    {
        _star.Name = evt.newValue;
        EditorUtility.SetDirty(_star);
    }

    private void OnColorChange(ChangeEvent<Color> evt)
    {
        _star.Color = evt.newValue;
        EditorUtility.SetDirty(_star);
    }

    private void OnRadiusSliderChange(ChangeEvent<float> evt)
    {
        var value = Math.Pow(evt.newValue, 10) * 10000;
        if (value >= 10)
            value = Math.Round(value, 0);
        else
            value = Math.Round(value, 4);
        _radiusInput.SetValueWithoutNotify((float)value);
        _star.Radius = (float)value;
        EditorUtility.SetDirty(_star);
    }
    private float CalcSliderPosFromRadius(float radius) => (float)Math.Pow(radius / 10000f, 1f / 10f);
    private void OnRadiusInputChange(ChangeEvent<float> evt)
    {
        _star.Radius = (float)evt.newValue;
        var value = CalcSliderPosFromRadius(evt.newValue);
        _radiusSlider.SetValueWithoutNotify((float)value);
        EditorUtility.SetDirty(_star);
    }
    private void OnGravityWellSliderChange(ChangeEvent<float> evt)
    {
        _star.GravityWellRadius = evt.newValue;
        EditorUtility.SetDirty(_star);
    }
    #endregion

}
