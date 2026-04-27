#if (UNITY_STANDALONE_WIN || UNITY_ANDROID || UNITY_VISIONOS) && !DISABLE_UNITY_XR
#define UNITY_XR_SUPPORTED
#endif
#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && OCULUS_XR_ENABLED && UNITY_XR_SUPPORTED
#define OCULUS_SUPPORTED
#endif
#if (UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_ANDROID) && OPENXR_ENABLED && UNITY_XR_SUPPORTED
#define OPENXR_SUPPORTED
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_VISIONOS
using UnityEngine.InputSystem.XR;
#endif

namespace UnityEngineX.XR;

public class HandTrackingManager : MonoBehaviour {
    public enum HandGestures { Relax, Grab, PistolPose, Shoot, Reload, PinchIndex, PinchMiddle, PinchRing, PinchLittle }

    public enum FingerPinch { Index = 0, Middle, Ring, Little }

    const int NumHands = 2;
    const int NumFingers = 5;
    const float LowThreshold = 0.3f; const float HighThreshold = 0.7f;
    const int ThumbFingerIndex = 0; const int IndexFingerIndex = 1; const int MiddleFingerIndex = 2; const int RingFingerIndex = 3; const int LittleFingerIndex = 4;
    const float ThumbHighThreshold = 0.4f;
    const float PinchThreshold = 0.02f;

    readonly bool[] TrackingState = new bool[NumHands];
    readonly float[] LeftFingersValues = new float[NumFingers], RightFingersValues = new float[NumFingers];
    XRHandSubsystem HandSubsystem;
    Dictionary<HandGestures, bool> LeftGestures, RightGestures;
    Transform[] LeftFingerProximals = new Transform[NumFingers], RightFingerProximals = new Transform[NumFingers];

    [SerializeField] Transform origin;
    [SerializeField] XRHandSkeletonDriver leftSkeleton;
    [SerializeField] XRHandSkeletonDriver rightSkeleton;
    [SerializeField] IUILaserPointer laserPointer;
    [SerializeField] GameObject[] motionControllerGameObjects;
    [SerializeField] GameObject[] handTrackingGameObjects;

    bool _handVisible;
    public bool HandsVisible {
        get => _handVisible;
        set {
            _handVisible = value;
#if UNITY_VISIONOS
            _handVisible = false;
#endif
            if (TryGetComponent(out var visualizer)) { }
            visualizer.drawMeshes = _handVisible;
        }
    }

    public event Action<bool, bool> HandTrackingEnableChanged;
    public event Action<HandGestures, bool, bool, bool> GestureChanged;

    public bool Tracked(bool left) => TrackingState[left ? 0 : 1];

    void EnsureStarted() {
        if (LeftGestures != null) return;
        if (!XRManager.Enabled) { gameObject.SetActive(false); Destroy(gameObject); return; }
        LeftGestures = InitializeGestureArray(); RightGestures = InitializeGestureArray();
        var joins = leftSkeleton.jointTransformReferences; PopulateFingers(ref LeftFingerProximals, joins);
        joins = rightSkeleton.jointTransformReferences; PopulateFingers(ref RightFingerProximals, joins);
#if UNITY_VISIONOS
        HandsVisible = false;
#else
        var leftEvent = leftSkeleton.GetComponent<XRHandTrackingEvents>(); leftEvent.trackingChanged.AddListener(OnLeftHandTrackingChanged);
        var rightEvent = rightSkeleton.GetComponent<XRHandTrackingEvents>(); rightEvent.trackingChanged.AddListener(OnRightHandTrackingChanged);
#endif
    }

    void Start() {
        EnsureStarted();
        var loader = XRManager.GetXRLoader();
        if (loader != null) HandSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
#if UNITY_VISIONOS
        OnHandTrackingChanged(true, true); OnHandTrackingChanged(false, true);
#endif
    }

    void Update() {
        TryCheckGesturesForHand(true);
        TryCheckGesturesForHand(false);
    }

    void OnLeftHandTrackingChanged(bool tracked) {
#if !UNITY_VISIONOS
        OnHandTrackingChanged(true, tracked);
#endif
    }

    void OnRightHandTrackingChanged(bool tracked) {
#if !UNITY_VISIONOS
        OnHandTrackingChanged(false, tracked);
#endif
    }

    void OnHandTrackingChanged(bool leftHand, bool tracked) {
        EnsureStarted();
        var index = leftHand ? 0 : 1;
        TrackingState[index] = tracked;
        motionControllerGameObjects[index].SetActive(!tracked);
        handTrackingGameObjects[index].SetActive(tracked);
        if (!leftHand) {
            laserPointer.AllowExternalPressInput = tracked;
            laserPointer.ExternalPressInputValue = false;
        }
        HandTrackingEnableChanged?.Invoke(leftHand, tracked);
    }

    void OnGestureChanged(HandGestures gestures, bool leftHand, bool previewGestureState, bool newGestureState) =>
        GestureChanged?.Invoke(gestures, leftHand, previewGestureState, newGestureState);

    bool CheckGesture(HandGestures gesture, ref float[] array) {
        if (gesture == HandGestures.Relax) {
            // Don't take the Thumb
            for (var i = IndexFingerIndex; i < array.Length; i++)
                if (array[i] > LowThreshold) return false;
            return true;
        }
        if (gesture == HandGestures.Grab) {
            // Don't take the Thumb
            for (var i = IndexFingerIndex; i <= MiddleFingerIndex; i++)
                if (array[i] < HighThreshold) return false;
            return true;
        }
        if (gesture == HandGestures.PistolPose) {
            // Only Middle + Ring to prevent bad detection of Pinky
            for (var i = MiddleFingerIndex; i < array.Length - 1; i++)
                if (array[i] < HighThreshold) return false;
            return true;
        }
        if (gesture == HandGestures.Shoot) return array[IndexFingerIndex] >= HighThreshold;
        if (gesture == HandGestures.Reload) return array[ThumbFingerIndex] >= ThumbHighThreshold;
        return false;
    }

    bool IsPinching(bool left, FingerPinch index) {
        if (HandSubsystem == null) return false;
        var hand = left ? HandSubsystem.leftHand : HandSubsystem.rightHand;
        if (!hand.isTracked) return false;
        var indexJoint = index switch {
            FingerPinch.Index => XRHandJointID.IndexTip,
            FingerPinch.Middle => XRHandJointID.MiddleTip,
            FingerPinch.Ring => XRHandJointID.RingTip,
            FingerPinch.Little => XRHandJointID.LittleTip,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
        var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
        var indexTip = hand.GetJoint(indexJoint);
        if (TryToWorldPose(thumbTip, origin, out var thumbPos) &&
            TryToWorldPose(indexTip, origin, out var indexPos)) {
            var distance = Vector3.Distance(thumbPos, indexPos);
            if (distance < PinchThreshold) return true;
        }
        return false;
    }

    public bool TryToWorldPose(XRHandJoint joint, Transform origin, out Vector3 result) {
        var xrOriginPose = new Pose(origin.position, origin.rotation);
        if (joint.TryGetPose(out Pose jointPose)) { result = jointPose.GetTransformedBy(xrOriginPose).position; return true; }
        result = Vector3.zero; return false;
    }

    void TryCheckGesturesForHand(bool left) {
        if (!TrackingState[left ? 0 : 1]) return;
        var gestureArray = left ? LeftGestures : RightGestures;
        var fingerValues = left ? LeftFingersValues : RightFingersValues;
        var proximalArray = left ? LeftFingerProximals : RightFingerProximals;
        // Check
        for (var i = 0; i < proximalArray.Length; i++) {
            var boneRotX = Mathf.Abs(proximalArray[i].localEulerAngles.x);
            if (boneRotX > 90) boneRotX = 0;
            var boneRate = boneRotX / 90.0f;
            if (left) LeftFingersValues[i] = boneRate;
            else RightFingersValues[i] = boneRate;
        }
        // Relax
        var newRelaxGesture = CheckGesture(HandGestures.Relax, ref fingerValues);
        var oldRelaxGesture = gestureArray[HandGestures.Relax];
        if (newRelaxGesture != oldRelaxGesture) { gestureArray[HandGestures.Relax] = newRelaxGesture; OnGestureChanged(HandGestures.Relax, left, oldRelaxGesture, newRelaxGesture); }
        // Grab
        var newGrabGesture = CheckGesture(HandGestures.Grab, ref fingerValues);
        var oldGrabGesture = gestureArray[HandGestures.Grab];
        if (newGrabGesture != oldGrabGesture) { gestureArray[HandGestures.Grab] = newGrabGesture; OnGestureChanged(HandGestures.Grab, left, oldGrabGesture, newGrabGesture); }
        // Pistol
        var newGunGesture = CheckGesture(HandGestures.PistolPose, ref fingerValues);
        var oldGunGesture = gestureArray[HandGestures.PistolPose];
        if (newGunGesture != oldGunGesture) { gestureArray[HandGestures.PistolPose] = newGunGesture; OnGestureChanged(HandGestures.PistolPose, left, oldGunGesture, newGunGesture); }
        // Shoot
        var newShootGesture = CheckGesture(HandGestures.Shoot, ref fingerValues);
        var oldShootGesture = gestureArray[HandGestures.Shoot];
        if (newShootGesture != oldShootGesture) {
            gestureArray[HandGestures.Shoot] = newShootGesture;
            // Gun Pose Required
            if (gestureArray[HandGestures.PistolPose]) OnGestureChanged(HandGestures.Shoot, left, oldShootGesture, newShootGesture);
        }
        // Reload
        var newReloadGesture = CheckGesture(HandGestures.Reload, ref fingerValues);
        var oldReloadGesture = gestureArray[HandGestures.Reload];
        if (newReloadGesture != oldReloadGesture) {
            gestureArray[HandGestures.Reload] = newReloadGesture;
            // Gun Pose Required
            if (gestureArray[HandGestures.PistolPose]) OnGestureChanged(HandGestures.Reload, left, oldReloadGesture, newReloadGesture);
        }
        // Pinch
        CheckPinch(left, FingerPinch.Index);
        CheckPinch(left, FingerPinch.Middle);
        CheckPinch(left, FingerPinch.Ring);
        CheckPinch(left, FingerPinch.Little);
    }

    void CheckPinch(bool left, FingerPinch pinchTarget) {
        var gestureTarget = pinchTarget switch {
            FingerPinch.Index => HandGestures.PinchIndex,
            FingerPinch.Middle => HandGestures.PinchMiddle,
            FingerPinch.Ring => HandGestures.PinchRing,
            FingerPinch.Little => HandGestures.PinchLittle
        };
        var gestureArray = left ? LeftGestures : RightGestures;
        var newPinchGesture = IsPinching(left, pinchTarget);
        var oldPinchGesture = gestureArray[gestureTarget];
        if (newPinchGesture != oldPinchGesture) {
            gestureArray[gestureTarget] = newPinchGesture;
            OnGestureChanged(gestureTarget, left, oldPinchGesture, newPinchGesture);
            if (pinchTarget == FingerPinch.Index && !left && newPinchGesture) laserPointer.ExternalPressInputValue = true;
        }
    }

    static Dictionary<HandGestures, bool> InitializeGestureArray() {
        var names = Enum.GetNames(typeof(HandGestures));
        var arr = new Dictionary<HandGestures, bool>(names.Length);
        for (var i = 0; i < names.Length; i++) arr.Add((HandGestures)i, false);
        return arr;
    }

    static void PopulateFingers(ref Transform[] proximalArray, List<JointToTransformReference> joins) {
        foreach (var join in joins)
            if (join.xrHandJointID == XRHandJointID.ThumbProximal) proximalArray[ThumbFingerIndex] = join.jointTransform;
            else if (join.xrHandJointID == XRHandJointID.IndexProximal) proximalArray[IndexFingerIndex] = join.jointTransform;
            else if (join.xrHandJointID == XRHandJointID.MiddleProximal) proximalArray[MiddleFingerIndex] = join.jointTransform;
            else if (join.xrHandJointID == XRHandJointID.RingProximal) proximalArray[RingFingerIndex] = join.jointTransform;
            else if (join.xrHandJointID == XRHandJointID.LittleProximal) proximalArray[LittleFingerIndex] = join.jointTransform;
    }
}

public class XRControllerHolder : MonoBehaviour {
    public enum XRControllerSupport { None = 0, OculusQuestAndRift, OculusQuest2, OculusRiftCV1, OculusQuestPro, Steam, Standard, WindowsMR }

    [Header("Oculus")]
    [SerializeField] GameObject oculusQuestAndRiftController = null;
    [SerializeField] GameObject oculusQuest2Controller = null;
    [SerializeField] GameObject oculusRiftController = null;
    [SerializeField] GameObject oculusQuestProController = null;

    [Header("Steam")]
    [SerializeField] GameObject steamController = null;

    [Header("Standard")]
    [SerializeField] GameObject standardController = null;

    [Header("Windows MR")]
    [SerializeField] GameObject windowsMRController = null;

    void Start() {
        SetControllersVisible(XRControllerSupport.None);
        if (!XRManager.Enabled) return;
        StartCoroutine(XRManager.GetXRInfos((vendor, headset) => {
            var controllerType = XRControllerSupport.None;
            controllerType = headset switch {
                XRHeadset.WindowsMr => XRControllerSupport.WindowsMR,
                XRHeadset.OculusQuest or XRHeadset.OculusRiftS => XRControllerSupport.OculusQuestAndRift,
                XRHeadset.OculusQuest2 => XRControllerSupport.OculusQuest2,
                XRHeadset.OculusRiftCv1 => XRControllerSupport.OculusRiftCV1,
                XRHeadset.HtcVive or XRHeadset.ValveIndex => XRControllerSupport.Steam,
                XRHeadset.OculusQuestPro => XRControllerSupport.OculusQuestPro,
                XRHeadset.None => XRControllerSupport.None,
                _ => XRControllerSupport.Standard,
            };
            SetControllersVisible(controllerType);
        }));
    }

    void SetControllersVisible(XRControllerSupport id) {
        oculusQuestAndRiftController.SetActive(id == XRControllerSupport.OculusQuestAndRift);
        oculusQuest2Controller.SetActive(id == XRControllerSupport.OculusQuest2);
        oculusRiftController.SetActive(id == XRControllerSupport.OculusRiftCV1);
        oculusQuestProController.SetActive(id == XRControllerSupport.OculusQuestPro);
        steamController.SetActive(id == XRControllerSupport.Steam);
        standardController.SetActive(id == XRControllerSupport.Standard);
        windowsMRController.SetActive(id == XRControllerSupport.WindowsMR);
    }
}

public class Teleporter : MonoBehaviour {
    Transform Transform;
    GameObject Marker;
    Transform MarkerTranform;
    GameObject TeleporterLine;
    LineRenderer Renderer;
    Vector3? TargetPosition;
    Transform RootTransform;
    Transform RayPoint;
    [SerializeField] GameObject GroundMarkerPrefab;
    [SerializeField] GameObject TeleporterLinePrefab;
    [SerializeField] float MaxDistance = 15.0f;

    public void Start() {
        Transform = transform;

        Marker = Instantiate(GroundMarkerPrefab);
        Marker.SetActive(false);
        MarkerTranform = Marker.transform;

        TeleporterLine = Instantiate(TeleporterLinePrefab);
        TeleporterLine.SetActive(false);
        Renderer = TeleporterLine.GetComponent<LineRenderer>();

        RayPoint = new GameObject("TeleporterRayPoint").transform;
        RayPoint.parent = transform;
        RayPoint.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
        RayPoint.localRotation = Quaternion.Euler(-35.0f, 0.0f, 0.0f);
        RootTransform = Transform.root;
    }

    public void InputIsPressed() {
        Debug.DrawRay(RayPoint.transform.position, -RayPoint.transform.up * 10);
        if (Physics.Raycast(RayPoint.transform.position, -RayPoint.transform.up, out var hit)) {
            var target = hit.point;
            target.y = 0;
            Renderer.SetPosition(0, Transform.position);
            // Limit the distance.
            if (Vector3.Distance(RootTransform.position, target) <= MaxDistance) {
                TargetPosition = target;
                MarkerTranform.position = target;
                Renderer.SetPosition(1, target);
            }
            else TargetPosition = null;
        }
        else TargetPosition = null;
        SetMarkerActive(TargetPosition != null);
    }

    void SetMarkerActive(bool active) {
        if (Marker.activeSelf != active) Marker.SetActive(active);
        if (TeleporterLine.activeSelf != active) TeleporterLine.SetActive(active);
    }

    public void InputWasJustReleased() {
        if (TargetPosition.HasValue) { transform.root.position = TargetPosition.Value; TargetPosition = null; }
        SetMarkerActive(false);
    }
}

public class IUILaserPointer : MonoBehaviour {
    public enum AutoInitializeModes { None, InitAndHide, InitAndShow }
    GameObject HitPoint;
    GameObject Pointer;
    float DistanceLimit;
    bool Enabled = true;
    bool Initialized;
    bool Locked;
    bool InputPressed;
    bool Ready;
    bool InputReading;

    [SerializeField] float LaserThickness = 0.002f;
    [SerializeField] float LaserHitScale = 0.02f;
    [SerializeField] Color Color = Color.blue;
    [SerializeField] Material LaserMaterial;
    [SerializeField] AutoInitializeModes AutoInitializeMode = AutoInitializeModes.None;
    [SerializeField] bool ForceLaserHidden;
    [SerializeField] bool VisionOsDisabled = true;
    [SerializeField] InputActionReference PressActionRef;

    public bool AllowExternalPressInput { get; set; }
    public bool ExternalPressInputValue { get; set; }
    public bool HapicEnabled { get; set; } = true;

    public bool ShouldBypass {
        get {
#if UNITY_VISIONOS
            return VisionOsDisabled;
#else
            return false;
#endif
        }
    }

    public bool IsActive {
        get => Enabled;
        set {
            if (!Initialized) Initialize();
            Enabled = value;
            Locked = false;
            HitPoint.SetActive(value && !ForceLaserHidden);
            Pointer.SetActive(value && !ForceLaserHidden);
            if (value) { InputReading = true; SetInputActionEnabled(true); }
            else { InputReading = false; SetInputActionEnabled(false); }
        }
    }

    public bool LineVisible {
        get => Pointer.activeSelf;
        set {
            if (!Initialized) Initialize();
            Pointer.GetComponent<MeshRenderer>().enabled = value && !ForceLaserHidden;
        }
    }

    public Material SharedMaterial {
        get => HitPoint.GetComponent<MeshRenderer>().sharedMaterial;
        set {
            Pointer.GetComponent<MeshRenderer>().sharedMaterial = value;
            HitPoint.GetComponent<MeshRenderer>().sharedMaterial = value;
        }
    }

    public GameObject CurrentWidget { get; set; }

    void Awake() {
        if (AutoInitializeMode == AutoInitializeModes.None) return;
        Initialize();
        SetInputActionEnabled(true);
    }

    void SetInputActionEnabled(bool inputEnabled) {
        if (ShouldBypass || PressActionRef == null) return;
        if (inputEnabled) PressActionRef.action?.Enable();
        else PressActionRef.action?.Disable();
        InputReading = inputEnabled;
    }

    void OnEnable() { SetInputActionEnabled(true); Locked = false; }

    void OnDisable() { SetInputActionEnabled(false); Locked = false; }

    void OnApplicationPause(bool pause) => Locked = false;

    public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation) {
        var transform1 = transform;
        position = transform1.position;
        rotation = transform1.rotation;
    }

    public void Initialize() {
        if (Initialized) return;
        Pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Pointer.transform.SetParent(transform, false);
        Pointer.transform.localScale = new Vector3(LaserThickness, LaserThickness, 100.0f);
        Pointer.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);

        HitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        HitPoint.transform.SetParent(transform, false);
        HitPoint.transform.localScale = new Vector3(LaserHitScale, LaserHitScale, LaserHitScale);
        HitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);
        HitPoint.SetActive(false);

        // remove the colliders on our primitives
        Destroy(HitPoint.GetComponent<SphereCollider>());
        Destroy(Pointer.GetComponent<BoxCollider>());

        Pointer.GetComponent<MeshRenderer>().sharedMaterial = LaserMaterial;
        HitPoint.GetComponent<MeshRenderer>().sharedMaterial = LaserMaterial;

        if (PressActionRef != null && !ShouldBypass) {
            var action = PressActionRef.action;
            if (!action.enabled) action.Enable();
            action.started += _ => InputPressed = true;
            action.canceled += _ => InputPressed = false;
        }

        Initialized = true;

        if (AutoInitializeMode == AutoInitializeModes.InitAndHide) SetActive(false);
    }

    public void SetActive(bool isActive) { gameObject.SetActive(isActive); IsActive = isActive; }

    void Update() {
        if (!Enabled || Pointer == null || ShouldBypass) return;
        if (!Ready && LaserPointerInputModule.TryGetInstance(out var module)) { module.AddController(this); Ready = true; }

        var transform1 = transform;
        var origin = transform1.position;
        var direction = transform1.forward;
        var ray = new Ray(origin, direction);
        var hit = Physics.Raycast(ray, out var hitInfo);
        var distance = hit ? hitInfo.distance : 100.0f;
        if (DistanceLimit > 0.0f) { distance = Mathf.Min(distance, DistanceLimit); hit = true; }

        Pointer.transform.localScale = new Vector3(LaserThickness, LaserThickness, distance);
        Pointer.transform.localPosition = new Vector3(0.0f, 0.0f, distance * 0.5f);

        if (hit) { HitPoint.SetActive(true); HitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, distance); }
        else HitPoint.SetActive(false);
        // reset the previous distance limit
        DistanceLimit = -1.0f;
    }

    public void LimitLaserDistance(float distance) {
        if (distance < 0.0f) return;
        DistanceLimit = distance < 0.0f ? distance : Mathf.Min(DistanceLimit, distance);
    }

    public void OnEnterControl(GameObject control) {
        if (!HapicEnabled) return;
        XRManager.Vibrate(this, XRNode.LeftHand, 0.1f, 0.15f);
        XRManager.Vibrate(this, XRNode.RightHand, 0.1f, 0.15f);
    }

    public void OnExitControl(GameObject control) { }

    public bool ButtonDown() {
        if (ShouldBypass) return false;
        var isPressed = InputPressed;
        if (AllowExternalPressInput && InputReading) { isPressed |= ExternalPressInputValue; ExternalPressInputValue = false; }
        return isPressed && !Locked;
    }

    public bool ButtonUp() {
        if (ShouldBypass) return false;
        var isUp = !InputPressed;
        if (AllowExternalPressInput && InputReading) isUp |= ExternalPressInputValue;
        return isUp && Locked;
    }

    public void RequestLockControls() {
        if (!Locked) StartCoroutine(LockControls());
    }

    IEnumerator LockControls() {
        Locked = true;
        yield return CoroutineFactory.WaitForSeconds(0.5f);
        Locked = false;
    }
}

public class LaserPointerEventData(EventSystem e) : PointerEventData(e), PointerEventData {
    public GameObject current;
    public IUILaserPointer controller;
    public override void Reset() { current = null; controller = null; base.Reset(); }
}

public class LaserPointerInputModule : BaseInputModule {
    static LaserPointerInputModule Instance;

    public LayerMask LayerMask = new() { value = 1 << 5 };

    // storage class for controller specific data
    class ControllerData {
        public LaserPointerEventData pointerEvent;
        public GameObject currentPoint;
        public GameObject currentPressed;
        public GameObject currentDragging;
    }

    Camera UICamera;
    PhysicsRaycaster raycaster;
    HashSet<IUILaserPointer> _controllers;
    // controller data
    Dictionary<IUILaserPointer, ControllerData> _controllerData = new();

    public static bool TryGetInstance(out LaserPointerInputModule instance) {
        instance = null;
        Instance ??= FindFirstObjectByType<LaserPointerInputModule>();
        if (Instance != null) instance = Instance;
        return Instance != null;
    }

    protected override void Awake() {
        base.Awake();
        if (Instance != null && Instance != this) {
            Debug.LogWarning("Trying to instantiate multiple LaserPointerInputModule.");
            Debug.Log(Instance.name); enabled = false;
            return;
        }
        Instance = this;
    }

    protected override void Start() {
        base.Start();

        // Create a new camera that will be used for raycasts
        UICamera = new GameObject("UI Camera").AddComponent<Camera>();
        // Added PhysicsRaycaster so that pointer events are sent to 3d objects
        raycaster = UICamera.gameObject.AddComponent<PhysicsRaycaster>();
        UICamera.clearFlags = CameraClearFlags.Nothing;
        UICamera.stereoTargetEye = StereoTargetEyeMask.None;
        UICamera.enabled = false;
        UICamera.fieldOfView = 5;
        UICamera.nearClipPlane = 0.01f;

        // Find canvases in the scene and assign our custom
        // UICamera to them
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases) canvas.worldCamera = UICamera;
    }

    public void AddController(IUILaserPointer controller) {
        if (!_controllerData.ContainsKey(controller)) _controllerData.Add(controller, new ControllerData());
    }

    public void RemoveController(IUILaserPointer controller) {
        if (_controllerData.ContainsKey(controller)) _controllerData.Remove(controller);
    }

    protected void UpdateCameraPosition(IUILaserPointer controller) {
        controller.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        var cameraTransform = UICamera.transform;
        cameraTransform.position = position;
        cameraTransform.rotation = rotation;
    }

    // clear the current selection
    public void ClearSelection() {
        if (eventSystem.currentSelectedGameObject) eventSystem.SetSelectedGameObject(null);
    }

    // select a game object
    void Select(GameObject go) {
        ClearSelection();
        if (ExecuteEvents.GetEventHandler<ISelectHandler>(go)) eventSystem.SetSelectedGameObject(go);
    }

    public override void Process() {
        raycaster.eventMask = LayerMask;

        foreach (var pair in _controllerData) {
            var controller = pair.Key;
            var data = pair.Value;
            controller.CurrentWidget = null;

            if (!controller.IsActive || !controller.enabled || !controller.gameObject.activeSelf || !controller.gameObject.activeInHierarchy)
                continue;

            // Test if UICamera is looking at a GUI element
            UpdateCameraPosition(controller);

            if (data.pointerEvent == null) data.pointerEvent = new LaserPointerEventData(eventSystem);
            else data.pointerEvent.Reset();
            data.pointerEvent.controller = controller;
            data.pointerEvent.delta = Vector2.zero;
            data.pointerEvent.position = new Vector2(UICamera.pixelWidth * 0.5f, UICamera.pixelHeight * 0.5f);
            //data.pointerEvent.scrollDelta = Vector2.zero;

            // trigger a raycast
            eventSystem.RaycastAll(data.pointerEvent, m_RaycastResultCache);
            data.pointerEvent.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();

            // make sure our controller knows about the raycast result
            // we add 0.01 because that is the near plane distance of our camera and we want the correct distance
            //if (data.pointerEvent.pointerCurrentRaycast.distance > 0.0f)
            //controller.LimitLaserDistance(data.pointerEvent.pointerCurrentRaycast.distance + 0.01f);

            // stop if no UI element was hit
            //if(pointerEvent.pointerCurrentRaycast.gameObject == null)
            //return;

            // Send control enter and exit events to our controller
            var hitControl = data.pointerEvent.pointerCurrentRaycast.gameObject;
            controller.CurrentWidget = hitControl;

            if (data.currentPoint != hitControl) {
                if (data.currentPoint != null) controller.OnExitControl(data.currentPoint);
                if (hitControl != null) controller.OnEnterControl(hitControl);
            }

            data.currentPoint = hitControl;

            // Handle enter and exit events on the GUI controlls that are hit
            HandlePointerExitAndEnter(data.pointerEvent, data.currentPoint);

            if (controller.ButtonDown()) {
                ClearSelection();

                data.pointerEvent.pressPosition = data.pointerEvent.position;
                data.pointerEvent.pointerPressRaycast = data.pointerEvent.pointerCurrentRaycast;
                data.pointerEvent.pointerPress = null;

                // update current pressed if the curser is over an element
                if (data.currentPoint != null) {
                    controller.RequestLockControls();
                    data.currentPressed = data.currentPoint;
                    data.pointerEvent.current = data.currentPressed;
                    GameObject newPressed = ExecuteEvents.ExecuteHierarchy(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerDownHandler);
                    if (newPressed == null) {
                        // some UI elements might only have click handler and not pointer down handler
                        newPressed = ExecuteEvents.ExecuteHierarchy(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                        if (newPressed != null) {
                            data.currentPressed = newPressed;
                        }
                    }
                    else {
                        data.currentPressed = newPressed;
                        // we want to do click on button down at same time, unlike regular mouse processing
                        // which does click when mouse goes up over same object it went down on
                        // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                        ExecuteEvents.Execute(newPressed, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerClickHandler);

                    }

                    if (newPressed != null) {
                        data.pointerEvent.pointerPress = newPressed;
                        data.currentPressed = newPressed;
                        Select(data.currentPressed);
                    }

                    ExecuteEvents.Execute(data.currentPressed, data.pointerEvent, ExecuteEvents.beginDragHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.beginDragHandler);

                    data.pointerEvent.pointerDrag = data.currentPressed;
                    data.currentDragging = data.currentPressed;
                }
            }// button down end

            if (controller.ButtonUp()) {
                if (data.currentDragging != null) {
                    data.pointerEvent.current = data.currentDragging;
                    ExecuteEvents.Execute(data.currentDragging, data.pointerEvent, ExecuteEvents.endDragHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.endDragHandler);
                    if (data.currentPoint != null) {
                        ExecuteEvents.ExecuteHierarchy(data.currentPoint, data.pointerEvent, ExecuteEvents.dropHandler);
                    }
                    data.pointerEvent.pointerDrag = null;
                    data.currentDragging = null;
                    controller.RequestLockControls();
                }
                if (data.currentPressed) {
                    data.pointerEvent.current = data.currentPressed;
                    ExecuteEvents.Execute(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerUpHandler);
                    data.pointerEvent.rawPointerPress = null;
                    data.pointerEvent.pointerPress = null;
                    data.currentPressed = null;
                    controller.RequestLockControls();
                }
            }

            // drag handling
            if (data.currentDragging != null) {
                data.pointerEvent.current = data.currentPressed;
                ExecuteEvents.Execute(data.currentDragging, data.pointerEvent, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.dragHandler);
            }

            // update selected element for keyboard focus
            if (eventSystem.currentSelectedGameObject != null) {
                data.pointerEvent.current = eventSystem.currentSelectedGameObject;
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
                //ExecuteEvents.Execute(controller.gameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
            }
        }
    }
}

public class UIIgnoreRaycast : MonoBehaviour, ICanvasRaycastFilter {
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => false;
}


#region Enumerations

public enum XRVendor { None = 0, Meta, WindowsMr, OpenVR, Sony, Pico, WaveVR, Apple, AndroidXR, Unknown }

public enum XRHeadset { None = 0, OculusRiftCv1, OculusRiftS, OculusQuest, OculusQuest2, OculusQuest3, OculusQuest3S, OculusQuestPro, OculusGo, HtcVive, ValveIndex, WindowsMr, Psvr, Psvr2, PicoNeo3, PicoNeo4, ViveFocus3, ViveXRElite, AppleVisionPro, AndroidXR, Unknown }

#endregion

public static class XRManager {
    static bool? _xrEnabled;
    static readonly List<InputDevice> InputDevices = new();
    public static bool Enabled => IsXREnabled(false);
    public static XRVendor Vendor { get; set; }
    public static XRHeadset Headset { get; set; }

    public static bool OpenXREnabled => IsOpenXREnabled();

    public static bool IsOpenXREnabled() {
#if OPENXR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return false;
        return xrLoader is UnityEngine.XR.OpenXR.OpenXRLoader;
#else
        return false;
#endif
    }

    public static bool HandTrackingSupported() {
#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_WIN
        return Vendor is XRVendor.Meta or XRVendor.Pico or XRVendor.Apple or XRVendor.AndroidXR;
#elif UNITY_VISIONOS
        return true;
#else
        return false;
#endif
    }

    public static bool HasMotionControllers()
        => Headset != XRHeadset.AppleVisionPro && Headset != XRHeadset.AndroidXR;

    public static XRVendor GetVendor() {
#if UNITY_VISIONOS
        return XRVendor.Apple;
#endif
#if UNITY_XR_SUPPORTED
        if (!_xrEnabled.HasValue) IsXREnabled();
        return Vendor;
#else
        return XRVendor.None;
#endif
    }

    public static XRLoader GetXRLoader() {
#if UNITY_XR_SUPPORTED
        var settings = XRGeneralSettings.Instance;
        if (settings == null) return null;
        var manager = settings.Manager;
        return manager != null ? manager.activeLoader : null;
#else
        return null;
#endif
    }

    public static XRInputSubsystem GetXRInput() {
#if UNITY_XR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return null;
        return xrLoader.GetLoadedSubsystem<XRInputSubsystem>();
#else
        return null;
#endif
    }

    public static XRDisplaySubsystem GetDisplay() {
#if UNITY_XR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return null;
        return xrLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
#else
        return null;
#endif
    }

    public static bool IsControllerConnected(bool left) {
#if UNITY_XR_SUPPORTED
        var input = GetXRInput();
        input.TryGetInputDevices(InputDevices);
        foreach (var device in InputDevices)
        {
            if (!device.isValid) continue;
            if (device.characteristics == InputDeviceCharacteristics.Left && left) return true;
            if (device.characteristics == InputDeviceCharacteristics.Right && !left) return true;
        }
#endif
        return false;
    }

    public static void SetRenderScale(float scale) {
#if UNITY_XR_SUPPORTED
        //XRSettings.renderViewportScale = Mathf.Clamp(scale, 0.5f, 1.0f);
#endif
    }

    public static bool IsXREnabled(bool force = false) {
#if UNITY_VISIONOS
        Vendor = XRVendor.Apple;
        Headset = XRHeadset.AppleVisionPro;
        return true;
#endif
#if UNITY_XR_SUPPORTED
        if (_xrEnabled.HasValue && !force) return _xrEnabled.Value;
        var loader = GetXRLoader();
        _xrEnabled = loader != null;
#if OPENXR_SUPPORTED
        if (loader is UnityEngine.XR.OpenXR.OpenXRLoader)
        {
            Vendor = ParseVendor(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
            Headset = ParseHeadset(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
        }
#endif
#if OCULUS_SUPPORTED
        if (loader is Unity.XR.Oculus.OculusLoader)
        {
            Vendor = XRVendor.Meta;
            switch (Unity.XR.Oculus.Utils.GetSystemHeadsetType())
            {
                case Unity.XR.Oculus.SystemHeadset.Oculus_Quest:
                case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest:
                    Headset = XRHeadset.OculusQuest;
                    break;
                case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest_2:
                case Unity.XR.Oculus.SystemHeadset.Oculus_Quest_2:
                    Headset = XRHeadset.OculusQuest2;
                    break;
                case Unity.XR.Oculus.SystemHeadset.Rift_CV1:
                    Headset = XRHeadset.OculusRiftCV1;
                    break;
                case Unity.XR.Oculus.SystemHeadset.Rift_S:
                    Headset = XRHeadset.OculusRiftS;
                    break;
                case Unity.XR.Oculus.SystemHeadset.Meta_Quest_3:
                case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_3:
                    Headset = XRHeadset.OculusQuest3;
                    break;
                case Unity.XR.Oculus.SystemHeadset.Meta_Quest_Pro:
                case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_Pro:
                    Headset = XRHeadset.OculusQuestPro;
                    break;
                default:
                    Headset = XRHeadset.OculusQuest2;
                    break;
            }
        }
#endif
        return _xrEnabled.Value;
#else
        return false;
#endif
    }

    public static void TryInitialize() {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
        var manager = XRGeneralSettings.Instance.Manager;
        if (manager == null) return;
        if (manager.activeLoader != null) return;
        manager.InitializeLoaderSync();
        manager.StartSubsystems();
#endif
    }

    public static void TryShutdown() {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
        var manager = XRGeneralSettings.Instance.Manager;
        if (manager == null) return;
        if (manager.activeLoader == null) return;
        manager.DeinitializeLoader();
        manager.StopSubsystems();
#endif
    }

    static bool InArray(string word, params string[] terms) {
        foreach (var term in terms)
            if (word.Contains(term)) return true;
        return false;
    }

    static bool InArrayAnd(string word, params string[] terms) {
        var counter = 0;
        foreach (var term in terms)
            if (word.Contains(term)) counter++;
        return counter == terms.Length;
    }

    public static XRVendor ParseVendor(string name) {
#if UNITY_XR_SUPPORTED
        if (string.IsNullOrEmpty(name)) return XRVendor.None;
        var mobile = PlatformUtility.IsMobilePlatform();
        name = name.ToLower();
#if UNITY_EDITOR
        if (name == "meta xr simulator") return XRVendor.Meta;
#endif
        if (name.Contains("oculus")) return XRVendor.Meta;
        if (name.Contains("android") && name.Contains("xr")) return XRVendor.AndroidXR;
        if (InArray(name, "apple", "vision", "polyspatial")) return XRVendor.Apple;
        if (InArray(name, "windows", "mixedreality", "holographic")) return XRVendor.WindowsMr;
        if (mobile && InArray(name, "xr elite", "focus", "vive")) return XRVendor.WaveVR;
        if (!mobile && InArray(name, "valve", "vive", "steam", "openvr")) return XRVendor.OpenVR;
        if (InArray(name, "sony", "psvr")) return XRVendor.Sony;
        if (InArray(name, "pico")) return XRVendor.Pico;
#endif
        return XRVendor.Unknown;
    }

    public static XRHeadset ParseHeadset(string name) {
#if UNITY_XR_SUPPORTED
        if (string.IsNullOrEmpty(name)) return XRHeadset.None;
        name = name.ToLower();
#if UNITY_EDITOR
        if (name == "meta xr simulator") return XRHeadset.OculusQuest3;
#endif
        if (name.Contains("android") && name.Contains("xr")) return XRHeadset.AndroidXR;
        if (name.Contains("oculus") || name.Contains("meta")) {
            if (InArray(name, "rift", "cv1")) return XRHeadset.OculusRiftCv1;
            if (InArray(name, "rift", "rift s", "rift-s")) return XRHeadset.OculusRiftS;
            if (InArray(name, "go")) return XRHeadset.OculusGo;
            if (name.Contains("quest")) {
                if (name.Contains("pro")) return XRHeadset.OculusQuestPro;
                if (name.Contains("2")) return XRHeadset.OculusQuest2;
                if (name.Contains("3s")) return XRHeadset.OculusQuest3S;
                if (name.Contains("3")) return XRHeadset.OculusQuest3;
                return XRHeadset.OculusQuest;
            }
            return XRHeadset.OculusQuest;
        }
        if (InArray(name, "apple", "vision", "polyspatial")) return XRHeadset.AppleVisionPro;
        if (InArray(name, "windows", "acer", "samsung", "reverb")) return XRHeadset.WindowsMr;
        if (name.Contains("vive")) {
            if (name.Contains("focus")) return XRHeadset.ViveFocus3;
            if (InArray(name, "vive", "xr", "elite")) return XRHeadset.ViveXRElite;
            return XRHeadset.HtcVive;
        }
        if (InArray("valve", "index")) return XRHeadset.ValveIndex;
        if (InArray("sony", "psvr")) {
            if (name.Contains("2")) return XRHeadset.Psvr2;
            return XRHeadset.Psvr;
        }
        if (name.Contains("pico")) {
            if (name.Contains("3")) return XRHeadset.PicoNeo3;
            return XRHeadset.PicoNeo4;
        }
#endif
        return XRHeadset.Unknown;
    }

    public static IEnumerator GetXRInfos(Action<XRVendor, XRHeadset> callback) {
#if UNITY_XR_SUPPORTED
        var vendor = XRVendor.None;
        var headset = XRHeadset.None;
        var head = new List<InputDevice>();
        var left = new List<InputDevice>();
        var right = new List<InputDevice>();
        do {
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, head);
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.TrackedDevice, left);
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.TrackedDevice, right);
            yield return null;
        } while (head.Count + left.Count + right.Count < 2);
        foreach (var h in head) { vendor = ParseVendor(h.name); headset = ParseHeadset(h.name); }
        Vendor = Vendor;
        Headset = headset;
        callback(Vendor, Headset);
#else
        callback(XRVendor.None, XRHeadset.None);
        yield break;
#endif
    }

    public static bool SetTrackingOriginMode(TrackingOriginModeFlags origin, bool recenter) {
#if UNITY_XR_SUPPORTED
        var xrInput = GetXRInput();
        if (xrInput == null) return false;
        if (xrInput.TrySetTrackingOriginMode(origin)) {
            if (recenter) return Recenter();
            return true;
        }
#endif

        return false;
    }

    public static bool Recenter() {
#if UNITY_XR_SUPPORTED
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var subsystem in subsystems)
            if (!subsystem.TryRecenter()) return false;
        return true;
#else
        return false;
#endif
    }

    public static void Vibrate(MonoBehaviour target, XRNode node, float amplitude = 0.5f, float seconds = 0.25f) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        if (!target.gameObject.activeSelf || !target.gameObject.activeInHierarchy) return;
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities)) return;
        if (capabilities.supportsBuffer) {
            byte[] buffer = { };
            if (GenerateBuzzClip(seconds, node, ref buffer)) device.SendHapticBuffer(0, buffer);
        }
        else if (capabilities.supportsImpulse) device.SendHapticImpulse(0, amplitude, seconds);
        target.StartCoroutine(StopVibrationCoroutine(device, seconds));
#endif
    }

    static IEnumerator StopVibrationCoroutine(InputDevice device, float delay) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        if (delay > 0) yield return CoroutineFactory.WaitForSeconds(delay);
        device.StopHaptics();
#else
        yield break;
#endif
    }

    public static bool GenerateBuzzClip(float seconds, XRNode node, ref byte[] clip) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        var result = device.TryGetHapticCapabilities(out HapticCapabilities caps);
        if (result) {
            var clipCount = (int)(caps.bufferFrequencyHz * seconds);
            clip = new byte[clipCount];
            for (var i = 0; i < clipCount; i++) clip[i] = byte.MaxValue;
        }
        return result;
#else
        clip = null;
        return false;
#endif
    }
}


public class XRPawn : MonoBehaviour {
    bool LocalPlayer;
    bool CanRecenter;
    [SerializeField] TrackingOriginModeFlags TrackingSpaceType;
    [SerializeField] float HeadHeight;
    [SerializeField] Transform TrackingSpace;
    [SerializeField] Transform MainCamera;
    [SerializeField] bool AutoInitialize;
    [SerializeField] Transform[] ControllerOffsets;

    void Start() {
        if (AutoInitialize) InitializeLocalPlayer();
    }

    void OnDestroy() {
        if (!LocalPlayer) return;
        var map = InputSystemManager.Disable("XR");
        map["Recenter"].started -= OnRecenter;
    }

    public void InitializeLocalPlayer() {
        if (!XRManager.Enabled) { enabled = false; return; }
        var handTrackingManager = GetComponentInChildren<HandTrackingManager>();
        handTrackingManager?.GestureChanged += HandleHandGestureChanged;
        var eyeSpace = TrackingSpaceType == TrackingOriginModeFlags.Device;
        var beta = XRManager.Vendor == XRVendor.Meta;
#if OCULUS_BUILD
        if (beta && TryGetComponent(out var ovrManager)) ovrManager.trackingOriginType =  eyeSpace ? OVRManager.TrackingOrigin.EyeLevel : OVRManager.TrackingOrigin.Stage;
#else
        if (beta && XRManager.IsOpenXREnabled())
            foreach (var s in ControllerOffsets) s.localRotation = Quaternion.Euler(45, 0, 0);
#endif
        var originOk = XRManager.SetTrackingOriginMode(TrackingSpaceType, true);
        var headOffset = eyeSpace ? HeadHeight : 0;
        TrackingSpace.localPosition = new Vector3(0.0f, headOffset, 0.0f);
        if (!originOk) Recenter();
        var map = InputSystemManager.Enable("XR");
        map["Recenter"].started += OnRecenter;
        Invoke(nameof(Recenter), 1.0f);
        LocalPlayer = true;
    }

    public void SetPassthroughEnabled(bool passthrough) {
#if OCULUS_BUILD
        if (mainCamera.TryGetComponent(out var layer)) layer.enabled = passthrough;
#endif
        var target = MainCamera.GetComponent<Camera>();
        target.clearFlags = passthrough ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        target.backgroundColor = passthrough ? Color.clear : Color.black;
    }

    #region Recenter

    public void Recenter() {
#if OCULUS_BUILD
        if (OVRManager.display != null) { OVRManager.display.RecenterPose(); return; }
#endif
        if (!XRManager.Recenter()) HardRecenter();
    }

    void OnRecenter(InputAction.CallbackContext context) => Recenter();

    void HardRecenter() {
        var bEyeSpace = TrackingSpaceType == TrackingOriginModeFlags.Device;
        var headOffset = bEyeSpace ? HeadHeight : 0;
        var trackingLoc = -MainCamera.transform.localPosition;
        trackingLoc.y += headOffset;
        TrackingSpace.localPosition = trackingLoc;
    }

    #endregion

    #region Event Handlers

    void HandleHandGestureChanged(HandTrackingManager.HandGestures gesture, bool left, bool prev, bool now) {
        var littlePinch = gesture == HandTrackingManager.HandGestures.PinchLittle && now;
        if (!littlePinch) return;
        if (left || !CanRecenter) return;
        Recenter();
        StartCoroutine(WaitForRecenter());
    }

    IEnumerator WaitForRecenter() {
        CanRecenter = false;
        yield return CoroutineFactory.WaitForSecondsUnscaled(1.0f);
        CanRecenter = true;
    }

    #endregion
}

#region Enumerations

public enum XRVendor {
    None = 0,
    Meta,
    WindowsMr,
    OpenVR,
    Sony,
    Pico,
    WaveVR,
    Apple,
    AndroidXR,
    Unknown
}

public enum XRHeadset {
    None = 0,
    OculusRiftCv1,
    OculusRiftS,
    OculusQuest,
    OculusQuest2,
    OculusQuest3,
    OculusQuest3S,
    OculusQuestPro,
    OculusGo,
    HtcVive,
    ValveIndex,
    WindowsMr,
    Psvr,
    Psvr2,
    PicoNeo3,
    PicoNeo4,
    ViveFocus3,
    ViveXRElite,
    AppleVisionPro,
    AndroidXR,
    Unknown
}

#endregion

public static class XRManager {
    static bool? XrEnabled;
    static readonly List<InputDevice> InputDevices = new();
    public static bool Enabled => IsXREnabled(false);
    public static XRVendor Vendor { get; private set; }
    public static XRHeadset Headset { get; private set; }

    public static bool OpenXREnabled => IsOpenXREnabled();

    public static bool IsOpenXREnabled() {
#if OPENXR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return false;
        return xrLoader is UnityEngine.XR.OpenXR.OpenXRLoader;
#else
        return false;
#endif
    }

    public static bool HandTrackingSupported() {
#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_WIN
        return Vendor is XRVendor.Meta or XRVendor.Pico or XRVendor.Apple or XRVendor.AndroidXR;
#elif UNITY_VISIONOS
        return true;
#else
        return false;
#endif
    }

    public static bool HasMotionControllers() 
        => Headset != XRHeadset.AppleVisionPro && Headset != XRHeadset.AndroidXR;

    public static XRVendor GetVendor() {
#if UNITY_VISIONOS
        return XRVendor.Apple;
#endif
#if UNITY_XR_SUPPORTED
        if (!XrEnabled.HasValue) IsXREnabled();
        return Vendor;
#else
        return XRVendor.None;
#endif
    }

    public static XRLoader GetXRLoader() {
#if UNITY_XR_SUPPORTED
        var settings = XRGeneralSettings.Instance;
        if (settings == null) return null;
        var manager = settings.Manager;
        return manager != null ? manager.activeLoader : null;
#else
        return null;
#endif
    }

    public static XRInputSubsystem GetXRInput() {
#if UNITY_XR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return null;
        return xrLoader.GetLoadedSubsystem<XRInputSubsystem>();
#else
        return null;
#endif
    }

    public static XRDisplaySubsystem GetDisplay() {
#if UNITY_XR_SUPPORTED
        var xrLoader = GetXRLoader();
        if (xrLoader == null) return null;
        return xrLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
#else
        return null;
#endif
    }

    public static bool IsControllerConnected(bool left) {
#if UNITY_XR_SUPPORTED
        var input = GetXRInput();
        input.TryGetInputDevices(InputDevices);
        foreach (var device in InputDevices)
        {
            if (!device.isValid) continue;
            if (device.characteristics == InputDeviceCharacteristics.Left && left) return true;
            if (device.characteristics == InputDeviceCharacteristics.Right && !left) return true;
        }
#endif
        return false;
    }

    public static void SetRenderScale(float scale) {
#if UNITY_XR_SUPPORTED
        //XRSettings.renderViewportScale = Mathf.Clamp(scale, 0.5f, 1.0f);
#endif
    }

    public static bool IsXREnabled(bool force = false) {
#if UNITY_VISIONOS
        Vendor = XRVendor.Apple;
        Headset = XRHeadset.AppleVisionPro;
        return true;
#endif
#if UNITY_XR_SUPPORTED
        if (XrEnabled.HasValue && !force) return XrEnabled.Value;
        var loader = GetXRLoader();
        XrEnabled = loader != null;
#if OPENXR_SUPPORTED
        if (loader is UnityEngine.XR.OpenXR.OpenXRLoader) {
            Vendor = ParseVendor(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
            Headset = ParseHeadset(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
        }
#endif
#if OCULUS_SUPPORTED
        if (loader is Unity.XR.Oculus.OculusLoader) {
            Vendor = XRVendor.Meta;
            switch (Unity.XR.Oculus.Utils.GetSystemHeadsetType()) {
                case Unity.XR.Oculus.SystemHeadset.Oculus_Quest:
                case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest: Headset = XRHeadset.OculusQuest; break;
                case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest_2:
                case Unity.XR.Oculus.SystemHeadset.Oculus_Quest_2:  Headset = XRHeadset.OculusQuest2;  break;
                case Unity.XR.Oculus.SystemHeadset.Rift_CV1: Headset = XRHeadset.OculusRiftCV1; break;
                case Unity.XR.Oculus.SystemHeadset.Rift_S: Headset = XRHeadset.OculusRiftS; break;
                case Unity.XR.Oculus.SystemHeadset.Meta_Quest_3:
                case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_3: Headset = XRHeadset.OculusQuest3; break;
                case Unity.XR.Oculus.SystemHeadset.Meta_Quest_Pro:
                case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_Pro: Headset = XRHeadset.OculusQuestPro; break;
                default: Headset = XRHeadset.OculusQuest2; break;
            }
        }
#endif
        return XrEnabled.Value;
#else
        return false;
#endif
    }

    public static void TryInitialize() {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
        var manager = XRGeneralSettings.Instance.Manager;
        if (manager == null) return;
        if (manager.activeLoader != null) return;
        manager.InitializeLoaderSync();
        manager.StartSubsystems();
#endif
    }

    public static void TryShutdown() {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
        var manager = XRGeneralSettings.Instance.Manager;
        if (manager == null) return;
        if (manager.activeLoader == null) return;
        manager.DeinitializeLoader();
        manager.StopSubsystems();
#endif
    }

    private static bool InArray(string word, params string[] terms) {
        foreach (var term in terms)
            if (word.Contains(term)) return true;
        return false;
    }

    private static bool InArrayAnd(string word, params string[] terms) {
        var counter = 0;
        foreach (var term in terms)
            if (word.Contains(term)) counter++;
        return counter == terms.Length;
    }

    public static XRVendor ParseVendor(string name) {
#if UNITY_XR_SUPPORTED
        if (string.IsNullOrEmpty(name)) return XRVendor.None;
        var mobile = PlatformUtility.IsMobilePlatform();
        name = name.ToLower();
#if UNITY_EDITOR
        if (name == "meta xr simulator") return XRVendor.Meta;
#endif
        if (name.Contains("oculus")) return XRVendor.Meta;
        if (name.Contains("android") && name.Contains("xr")) return XRVendor.AndroidXR;
        if (InArray(name, "apple", "vision", "polyspatial")) return XRVendor.Apple;
        if (InArray(name, "windows", "mixedreality", "holographic")) return XRVendor.WindowsMr;
        if (mobile && InArray(name, "xr elite", "focus", "vive")) return XRVendor.WaveVR;
        if (!mobile && InArray(name, "valve", "vive", "steam", "openvr")) return XRVendor.OpenVR;
        if (InArray(name, "sony", "psvr")) return XRVendor.Sony;
        if (InArray(name, "pico")) return XRVendor.Pico;
#endif

        return XRVendor.Unknown;
    }

    public static XRHeadset ParseHeadset(string name) {
#if UNITY_XR_SUPPORTED
        if (string.IsNullOrEmpty(name)) return XRHeadset.None;
        name = name.ToLower();
#if UNITY_EDITOR
        if (name == "meta xr simulator") return XRHeadset.OculusQuest3;
#endif
        if (name.Contains("android") && name.Contains("xr")) return XRHeadset.AndroidXR;
        if (name.Contains("oculus") || name.Contains("meta"))
        {
            if (InArray(name, "rift", "cv1")) return XRHeadset.OculusRiftCv1;
            if (InArray(name, "rift", "rift s", "rift-s"))  return XRHeadset.OculusRiftS;
            if (InArray(name, "go")) return XRHeadset.OculusGo;
            if (name.Contains("quest"))
            {
                if (name.Contains("pro")) return XRHeadset.OculusQuestPro;
                if (name.Contains("2")) return XRHeadset.OculusQuest2;
                if (name.Contains("3s")) return XRHeadset.OculusQuest3S;
                if (name.Contains("3")) return XRHeadset.OculusQuest3;
                return XRHeadset.OculusQuest;
            }
            return XRHeadset.OculusQuest;
        }

        if (InArray(name, "apple", "vision", "polyspatial")) return XRHeadset.AppleVisionPro;
        if (InArray(name, "windows", "acer", "samsung", "reverb")) return XRHeadset.WindowsMr;
        if (name.Contains("vive"))
        {
            if (name.Contains("focus")) return XRHeadset.ViveFocus3;
            if (InArray(name, "vive", "xr", "elite")) return XRHeadset.ViveXRElite;
            return XRHeadset.HtcVive;
        }

        if (InArray("valve", "index")) return XRHeadset.ValveIndex;

        if (InArray("sony", "psvr"))
        {
            if (name.Contains("2")) return XRHeadset.Psvr2;
            return XRHeadset.Psvr;
        }

        if (name.Contains("pico"))
        {
            if (name.Contains("3")) return XRHeadset.PicoNeo3;
            return XRHeadset.PicoNeo4;
        }
#endif
        return XRHeadset.Unknown;
    }

    public static IEnumerator GetXRInfos(Action<XRVendor, XRHeadset> callback) {
#if UNITY_XR_SUPPORTED
        var vendor = XRVendor.None;
        var headset = XRHeadset.None;
        var head = new List<InputDevice>();
        var left = new List<InputDevice>();
        var right = new List<InputDevice>();
        do {
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, head);
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.TrackedDevice, left);
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.TrackedDevice, right);
            yield return null;
        } while (head.Count + left.Count + right.Count < 2);
        foreach (var h in head) { vendor = ParseVendor(h.name); headset = ParseHeadset(h.name); }
        Vendor = Vendor;
        Headset = headset;
        callback(Vendor, Headset);
#else
        callback(XRVendor.None, XRHeadset.None);
        yield break;
#endif
    }

    public static bool SetTrackingOriginMode(TrackingOriginModeFlags origin, bool recenter) {
#if UNITY_XR_SUPPORTED
        var xrInput = GetXRInput();
        if (xrInput == null) return false;
        if (xrInput.TrySetTrackingOriginMode(origin)) {
            if (recenter) return Recenter();
            return true;
        }
#endif

        return false;
    }

    public static bool Recenter() {
#if UNITY_XR_SUPPORTED
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var subsystem in subsystems)
            if (!subsystem.TryRecenter()) return false;
        return true;
#else
        return false;
#endif
    }

    public static void Vibrate(MonoBehaviour target, XRNode node, float amplitude = 0.5f, float seconds = 0.25f) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        if (!target.gameObject.activeSelf || !target.gameObject.activeInHierarchy) return;
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities)) return;
        if (capabilities.supportsBuffer) {
            byte[] buffer = { };
            if (GenerateBuzzClip(seconds, node, ref buffer)) device.SendHapticBuffer(0, buffer);
        }
        else if (capabilities.supportsImpulse) device.SendHapticImpulse(0, amplitude, seconds);
        target.StartCoroutine(StopVibrationCoroutine(device, seconds));
#endif
    }

    static IEnumerator StopVibrationCoroutine(InputDevice device, float delay) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        if (delay > 0) yield return CoroutineFactory.WaitForSeconds(delay);
        device.StopHaptics();
#else
        yield break;
#endif
    }

    public static bool GenerateBuzzClip(float seconds, XRNode node, ref byte[] clip) {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        var result = device.TryGetHapticCapabilities(out HapticCapabilities caps);
        if (result)  {
            var clipCount = (int)(caps.bufferFrequencyHz * seconds);
            clip = new byte[clipCount];
            for (int i = 0; i < clipCount; i++)  clip[i] = byte.MaxValue;
        }

        return result;
#else
        clip = null;
        return false;
#endif
    }
}