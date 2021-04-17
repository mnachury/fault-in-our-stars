using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StarController : MonoBehaviour
{
    private const string defaultMaterial = "Diffuse";
    private string _name;
    private Material _material;
    private Color _color;
    private float _radius = 1f;
    private float _gravityWellRadius = 2f;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            transform.name = _name;
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
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
            transform.localScale = new Vector3(_radius, _radius, _radius);
        }
    }

    public float GravityWellRadius
    {
        get => _gravityWellRadius;
        set
        {
            _gravityWellRadius = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _name = name;

        // Color
        _material =  new Material(Shader.Find(defaultMaterial));
        Renderer renderer;
        if (!TryGetComponent(out renderer))
        {
            renderer = gameObject.AddComponent<Renderer>();
        }
        renderer.material = _material;
        _color = _material.color;

        _radius = transform.localScale.x;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _gravityWellRadius * _radius);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
