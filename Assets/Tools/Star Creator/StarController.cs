using System;
using UnityEngine;

[ExecuteInEditMode]
[Serializable]
public class StarController : MonoBehaviour
{
    private const string defaultMaterial = "Diffuse";
    private Material _material;
    [SerializeField]
    private string _name;
    [SerializeField]
    private Color _color;
    [SerializeField]
    private float _radius = 1f;
    [SerializeField]
    private float _gravityWellRadius = 2f;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (isActiveAndEnabled)
                transform.name = _name;
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            if (isActiveAndEnabled)
                if (_material)
                    _material.color = _color;
        }
    }

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            if (isActiveAndEnabled)
                transform.localScale = new Vector3(_radius, _radius, _radius);
        }
    }

    public float GravityWellRadius
    {
        get => _gravityWellRadius;
        set => _gravityWellRadius = value;

    }

    // Start is called before the first frame update
    void Start()
    {
        if (_name == "")
            _name = name;

        // Color
        Renderer renderer;
        if (!TryGetComponent(out renderer))
        {
            renderer = gameObject.AddComponent<Renderer>();
        }
        _material = new Material(Shader.Find(defaultMaterial));
        renderer.sharedMaterial = _material;
        if (_color != null)
            _material.color = _color;
        else
            _color = _material.color;

        if (_radius == default(float))
            _radius = transform.localScale.x;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        // *0.6 because unity draws gizmos 1.4 times bigger
        Gizmos.DrawWireSphere(transform.position, _gravityWellRadius * _radius * 0.6f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
