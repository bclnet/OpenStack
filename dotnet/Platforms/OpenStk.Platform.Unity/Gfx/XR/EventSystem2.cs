//using Demonixis.ToolboxV2.XR;
//using System.Collections;
//using Demonixis.ToolboxV2.Utils;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.InputSystem;
//using UnityEngine.InputSystem.UI;
//using Wacki;

//namespace Demonixis.ToolboxV2;

//public class BetterEventSystem : EventSystem
//{
//    [SerializeField] private bool _useFlatVisionPro;
//    [SerializeField] private InputActionAsset _visionOsInputActionAsset;

//    protected override void Start()
//    {
//        base.Start();
//        StartCoroutine(DeferredStart());
//    }

//    private IEnumerator DeferredStart()
//    {
//        yield return new WaitForEndOfFrame();

//#if UNITY_VISIONOS
//        if (_useFlatVisionPro)
//        {
//            FlatSetup();
//            yield break;
//        }
//#endif

//        if (XRManager.Enabled)
//            LegacyXRSetup();
//        else
//            FlatSetup();
//    }

//    private void LegacyXRSetup()
//    {
//        RemoveInputModule<InputSystemUIInputModule>();
//        AddInputModule<LaserPointerInputModule>();
//    }

//    private void FlatSetup()
//    {
//        RemoveInputModule<LaserPointerInputModule>();
//        AddInputModule<InputSystemUIInputModule>();

//        if (_useFlatVisionPro && XRManager.Enabled)
//            StartCoroutine(WaitForPlayerCamera());
//    }

//    private IEnumerator WaitForPlayerCamera()
//    {
//        yield return CoroutineFactory.WaitForSeconds(0.5f);
        
//        while (Camera.main == null)
//        {
//            yield return CoroutineFactory.WaitForSeconds(0.5f);
//        }

//        var mainCamera = Camera.main;
//        var module = GetComponent<InputSystemUIInputModule>();
//        module.xrTrackingOrigin = mainCamera.transform.parent;
//        module.actionsAsset = _visionOsInputActionAsset;

//        var allCanvas = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
//        foreach (var canvas in allCanvas)
//            canvas.worldCamera = mainCamera;
//    }

//    private void AddInputModule<T>() where T : MonoBehaviour
//    {
//        if (!TryGetComponent(out T t))
//        {
//            gameObject.AddComponent<T>();
//        }
//    }

//    private void RemoveInputModule<T>() where T : MonoBehaviour
//    {
//        if (TryGetComponent(out T t))
//        {
//            Destroy(t);
//        }
//    }
//}