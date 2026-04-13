using System;
using System.Collections;
using UnityEngine;
using static OpenStack.CellManager;

namespace OpenStack.Gfx.Unity;

#region Extensions

// UnityCellManager
public class UnityCellManager(IQuery query, CoroutineQueue queue, Func<ICellRecord, ILandRecord, object, object, IEnumerator> coroutine) : CellManager(query, queue, coroutine) {
    public override (object, object) GfxCreateContainers(string name) {
        var cellObj = new GameObject(name) { tag = "Cell" };
        var contObj = new GameObject("objects"); contObj.transform.parent = cellObj.transform;
        return (contObj, cellObj);
    }

    public override void GfxSetVisible(object container, bool visible) {
        var c = (GameObject)container;
        if (visible) { if (!c.activeSelf) c.SetActive(true); }
        else { if (c.activeSelf) c.SetActive(false); }
    }
}

public class UnityCellBuilder : CellBuilder<GameObject, object, object, Shader> {
}

#endregion

#region InputManager

public static class InputManager {
    //struct XRButtonMapping(XRButton button, bool left) {
    //    public XRButton Button { get; set; } = button;
    //    public bool LeftHand { get; set; } = left;
    //}

    //static Dictionary<string, XRButtonMapping> XRMapping = new()
    //{
    //    { "Jump", new XRButtonMapping(XRButton.Thumbstick, true) },
    //    { "Light", new XRButtonMapping(XRButton.Thumbstick, false) },
    //    { "Run", new XRButtonMapping(XRButton.Grip, true) },
    //    { "Slow", new XRButtonMapping(XRButton.Grip, false) },
    //    { "Attack", new XRButtonMapping(XRButton.Trigger, false) },
    //    { "Recenter", new XRButtonMapping(XRButton.Menu, false) },
    //    { "Use", new XRButtonMapping(XRButton.Trigger, true) },
    //    { "Menu", new XRButtonMapping(XRButton.Menu, true) }
    //};

    public static float GetAxis(string axis) {
        var result = 1.0f; // Input.GetAxis(axis);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (axis == "Horizontal") result += input.GetAxis(XRAxis.ThumbstickX, true);
        //    else if (axis == "Vertical") result += input.GetAxis(XRAxis.ThumbstickY, true);
        //    else if (axis == "Mouse X") result += input.GetAxis(XRAxis.ThumbstickX, false);
        //    else if (axis == "Mouse Y") result += input.GetAxis(XRAxis.ThumbstickY, false);
        //    // Deadzone
        //    if (Mathf.Abs(result) < 0.15f) result = 0.0f;
        //}
        return result;
    }

    public static bool GetButton(string button) {
        var result = false; // Input.GetButtonDown(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) {
        //        var mapping = XRMapping[button];
        //        result |= input.GetButton(mapping.Button, mapping.LeftHand);
        //    }
        //}
        return result;
    }

    public static bool GetButtonUp(string button) {
        var result = false; // UnityEngine.Input.GetButtonUp(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) { var mapping = XRMapping[button]; result |= input.GetButtonUp(mapping.Button, mapping.LeftHand); }
        //}
        return result;
    }

    public static bool GetButtonDown(string button) {
        var result = false; // UnityEngine.Input.GetButtonDown(button);
        //if (XRSettings.enabled) {
        //    var input = XRInput.Instance;
        //    if (XRMapping.ContainsKey(button)) { var mapping = XRMapping[button]; result |= input.GetButtonDown(mapping.Button, mapping.LeftHand); }
        //}
        return result;
    }

    internal static bool GetKeyDown(KeyCode tab) {
        throw new NotImplementedException();
    }

    internal static bool GetMouseButtonDown(int v) {
        throw new NotImplementedException();
    }
}

#endregion