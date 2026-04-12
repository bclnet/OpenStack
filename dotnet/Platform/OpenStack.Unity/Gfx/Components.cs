
using UnityEngine;

namespace OpenStack.Gfx.Unity.Components;

public class SunCycle : MonoBehaviour {
    Transform Transform;
    Quaternion OriginalOrientation;
    [SerializeField] float RotationTime = 0.5f;

    public void Start() {
        Transform = transform;
        OriginalOrientation = Transform.rotation;
        RenderSettings.sun = GetComponent<Light>();
    }

    public void Update() => Transform.Rotate(RotationTime * Time.deltaTime, 0.0f, 0.0f);
}
