
using System;
using UnityEngine;
using Image = UnityEngine.UIElements.Image;

namespace OpenStack.Gfx.Unity.Components;

#region UICrosshairComponent

[RequireComponent(typeof(Image))]
public class UICrosshairComponent : MonoBehaviour {
    Image Crosshair = null;
    public bool Enabled { get => Crosshair.enabledSelf; set => Crosshair.enabledSelf = value; }
    public void Awake() => Crosshair = GetComponent<Image>();
    public void Start() {
        //var crosshairTexture = (Texture2D)null; // BaseEngine.instance.Asset.LoadTexture("target", true);
        //Crosshair.sprite = GUIUtils.CreateSprite(crosshairTexture);
    }
    public void SetActive(bool active) => gameObject.SetActive(active);
}

#endregion

#region PlayerComponent

public class PlayerComponent : MonoBehaviour {
    Transform CamTransform;
    Transform Transform;
    CapsuleCollider CapsuleCollider;
    Rigidbody Rigidbody;
    UICrosshairComponent Crosshair;
    bool Paused = false;
    bool IsGrounded = false;

    [Header("Movement Settings")]
    public float SlowSpeed = 3;
    public float NormalSpeed = 5;
    public float FastSpeed = 10;
    public float FlightSpeedMultiplier = 3 * 5;
    public float AirborneForceMultiplier = 5;
    public float MouseSensitivity = 3;
    public float MinVerticalAngle = -90;
    public float MaxVerticalAngle = 90;

    [Header("Misc")]
    public Light Lantern;
    public Transform LeftHand;
    public Transform RightHand;

    bool _isFlying = true;
    public bool IsFlying {
        get => _isFlying;
        set {
            _isFlying = value;
            if (!_isFlying) Rigidbody.useGravity = true;
            else Rigidbody.useGravity = false;
        }
    }

    public void Start() {
        if (Camera.main == null) throw new InvalidOperationException("Camera.main missing");
        Transform = GetComponent<Transform>();
        CamTransform = Camera.main.GetComponent<Transform>();
        CapsuleCollider = GetComponent<CapsuleCollider>() ?? throw new InvalidOperationException("Player:CapsuleCollider missing");
        Rigidbody = GetComponent<Rigidbody>() ?? throw new InvalidOperationException("Player:Rigidbody missing");
        // Setup the camera
        //var game = BaseSettings.Game;
        var camera = Camera.main;
        camera.renderingPath = RenderingPath.Forward; // game.RenderPath;
        camera.farClipPlane = 1.0f; // game.CameraFarClip;
        Crosshair = FindAnyObjectByType<UICrosshairComponent>();
    }

    public void Update() {
        if (Paused) return;
        Rotate();
        if (InputManager.GetKeyDown(KeyCode.Tab)) IsFlying = !IsFlying;
        if (IsGrounded && !IsFlying && InputManager.GetButtonDown("Jump")) { var newVelocity = Rigidbody.linearVelocity; newVelocity.y = 5; Rigidbody.linearVelocity = newVelocity; }
        if (InputManager.GetButtonDown("Light")) Lantern.enabled = !Lantern.enabled;
        //// clamp
        //var lastPostion = _transform.position;
        //if (lastPostion.y < 0) { lastPostion.y = 0; _transform.position = lastPostion; }
    }

    public void FixedUpdate() {
        IsGrounded = CalculateIsGrounded();
        if (IsGrounded || IsFlying) SetVelocity();
        else if (!IsGrounded || !IsFlying) ApplyAirborneForce();

    }

    void Rotate() {
        if (Cursor.lockState != CursorLockMode.Locked) {
            if (InputManager.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;
            else return;
        }
        else if (InputManager.GetKeyDown(KeyCode.BackQuote)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        var eulerAngles = new Vector3(CamTransform.localEulerAngles.x, Transform.localEulerAngles.y, 0);
        // Make eulerAngles.x range from -180 to 180 so we can clamp it between a negative and positive angle.
        if (eulerAngles.x > 180) eulerAngles.x -= 360;
        var deltaMouse = MouseSensitivity * (new Vector2(InputManager.GetAxis("Mouse X"), InputManager.GetAxis("Mouse Y")));
        eulerAngles.x = Mathf.Clamp(eulerAngles.x - deltaMouse.y, MinVerticalAngle, MaxVerticalAngle);
        eulerAngles.y = Mathf.Repeat(eulerAngles.y + deltaMouse.x, 360);
        CamTransform.localEulerAngles = new Vector3(eulerAngles.x, 0, 0);
        Transform.localEulerAngles = new Vector3(0, eulerAngles.y, 0);
    }

    void SetVelocity() {
        Vector3 velocity;
        if (!IsFlying) { velocity = Transform.TransformVector(CalculateLocalVelocity()); velocity.y = Rigidbody.linearVelocity.y; }
        else velocity = CamTransform.TransformVector(CalculateLocalVelocity());
        Rigidbody.linearVelocity = velocity;
    }

    void ApplyAirborneForce() {
        var forceDirection = Transform.TransformVector(CalculateLocalMovementDirection());
        forceDirection.y = 0;
        forceDirection.Normalize();
        var force = AirborneForceMultiplier * Rigidbody.mass * forceDirection;
        Rigidbody.AddForce(force);
    }

    Vector3 CalculateLocalMovementDirection() {
        // Calculate the local movement direction.
        var direction = new Vector3(InputManager.GetAxis("Horizontal"), 0.0f, InputManager.GetAxis("Vertical"));
        // A small hack for French Keyboard...
        //if (Application.systemLanguage == SystemLanguage.French)
        //{
        //    // Cancel Qwerty
        //    if (Input.GetKeyDown(KeyCode.W)) direction.z = 0;
        //    else if (Input.GetKeyDown(KeyCode.A)) direction.x = 0;
        //    // Use Azerty
        //    if (Input.GetKey(KeyCode.Z)) direction.z = 1;
        //    else if (Input.GetKey(KeyCode.S)) direction.z = -1;
        //    if (Input.GetKey(KeyCode.Q)) direction.x = -1;
        //    else if (Input.GetKey(KeyCode.D)) direction.x = 1;
        //}
        return direction.normalized;
    }

    float CalculateSpeed() {
        var speed = NormalSpeed;
        if (InputManager.GetButton("Run")) speed = FastSpeed;
        else if (InputManager.GetButton("Slow")) speed = SlowSpeed;
        if (IsFlying) speed *= FlightSpeedMultiplier;
        return speed;
    }

    Vector3 CalculateLocalVelocity() => CalculateSpeed() * CalculateLocalMovementDirection();

    bool CalculateIsGrounded() {
        var playerCenter = Transform.position + CapsuleCollider.center;
        var castedSphereRadius = 0.8f * CapsuleCollider.radius;
        var sphereCastDistance = CapsuleCollider.height / 2;
        return Physics.SphereCast(new Ray(playerCenter, -Transform.up), castedSphereRadius, sphereCastDistance);
    }

    public void Pause(bool pause) {
        Paused = pause;
        Crosshair.SetActive(!Paused);
        Time.timeScale = pause ? 0.0f : 1.0f;
        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pause;
        // Used by the VR Component to enable/disable some features.
        SendMessage("OnPlayerPause", pause, SendMessageOptions.DontRequireReceiver);
    }
}

#endregion

#region SunCycle

public class SunCycleComponent : MonoBehaviour {
    Transform Transform;
    Quaternion OriginalOrientation;
    /*[SerializeField]*/
    float RotationTime = 0.5f;

    public void Start() {
        Transform = transform;
        OriginalOrientation = Transform.rotation;
        RenderSettings.sun = GetComponent<Light>();
    }

    public void Update() => Transform.Rotate(RotationTime * Time.deltaTime, 0.0f, 0.0f);
}

#endregion