using System;
using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraManager : Singleton<CameraManager> {
    public enum CameraView {
        FirstPerson,
        FreeLook,
        None
    }

    [SerializeField]
    private bool UseARCameraSwitch = false;

    [SerializeField]
    private Camera MainCamera;

    [SerializeField]
    private Material OutlineMaterial;

    private ArcGISCameraControllerTouch cameraControllerTouch;
    private CesiumCameraController cesiumCameraController;
    
    private CameraFollowSimple cameraFollow;

    public bool FollowingTarget { get; private set; }

    private float pinchSpeed = 0.03f;

    public GameObject UI;
    private InteractiveObject droneFPV;

    public GameObject ARCameraBackground;
    private RawImage arCameraBackgroundImage;

    private bool vrSceneActive = false;

    public CameraView CurrentCameraView {
        get; private set;
    }

    private Shader buildingShader;
    private Shader buildingShaderLit;

    private void Start() {
        if (MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS) {
            cameraControllerTouch = MainCamera.GetComponent<ArcGISCameraControllerTouch>();
        } else if (MapManager.Instance.CurrentMapType == MapManager.MapType.Cesium) {
            cesiumCameraController = MainCamera.GetComponent<CesiumCameraController>();
        }

        cameraFollow = MainCamera.GetComponent<CameraFollowSimple>();
        FollowingTarget = false;

        arCameraBackgroundImage = ARCameraBackground.GetComponentInChildren<RawImage>();

        buildingShader = UnityEngine.Shader.Find("Custom/MobileOcclusion");
        buildingShaderLit = UnityEngine.Shader.Find("Shader Graphs/SceneNodeSurface");

        //SetCameraView(CameraView.FreeLook);
    }


    private void Update() {
        if (UseARCameraSwitch) {
            if (GetMouseScollValue() != 0f && FollowingTarget) {
                if (GameManager.Instance.CurrentDisplayState == GameManager.DisplayState.Scene3DView) {
                    StopFollowingTarget();
                }
            }

            if (Keyboard.current[Key.Numpad1].wasPressedThisFrame) {
                SetCameraView(CameraView.FirstPerson);
                //} else if (Keyboard.current[Key.Numpad2].wasPressedThisFrame) {
                //    SetCameraView(CameraView.ThirdPerson);
            } else if (Keyboard.current[Key.Numpad3].wasPressedThisFrame) {
                SetCameraView(CameraView.FreeLook);
            } 
        }
    }

    public void DisplayVRScene(bool active = true) {
        if (MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS) {

            GameObject[] buildings = FindGameObjectsInLayer(10);
            if (buildings != null) {
                if (active) {
                    foreach (GameObject building in buildings) {
                        building.GetComponent<MeshRenderer>().material.shader = buildingShaderLit;
                        //building.GetComponent<Collider>().enabled = true;
                    }
                } else {
                    foreach (GameObject building in buildings) {
                        building.GetComponent<MeshRenderer>().material.shader = buildingShader;
                        //building.GetComponent<Collider>().enabled = false;
                    }
                }
            }
        }
    }

    public void DisplayVRSceneLit(bool active = true) {
        foreach (GameObject building in FindGameObjectsInLayer(10)) {
            building.GetComponent<MeshRenderer>().enabled = active;
            building.GetComponent<Collider>().enabled = active;
        }
    }

    public void DisplayVRSceneOverlay(bool active = true) {
        if (active) {
            foreach (GameObject building in FindGameObjectsInLayer(10)) {                
                List<Material> materials = new List<Material>(building.GetComponent<MeshRenderer>().materials);
                materials.Add(OutlineMaterial);
                building.GetComponent<MeshRenderer>().materials = materials.ToArray();
            }
        } else {
            foreach (GameObject building in FindGameObjectsInLayer(10)) {
                Material[] materials = building.GetComponent<MeshRenderer>().materials;
                Material[] mat = { materials[0] };
                building.GetComponent<MeshRenderer>().materials = mat;
            }
        }
    }

    private GameObject[] FindGameObjectsInLayer(int layer) {
        var goArray = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        var goList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < goArray.Length; i++) {
            if (goArray[i].layer == layer) {
                goList.Add(goArray[i]);
            }
        }
        if (goList.Count == 0) {
            return null;
        }
        return goList.ToArray();
    }

    private float GetMouseScollValue() {
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 2) {
            UnityEngine.InputSystem.EnhancedTouch.Touch touch1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
            UnityEngine.InputSystem.EnhancedTouch.Touch touch2 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];

            Vector2 touch1Previous = touch1.screenPosition - touch1.delta;
            Vector2 touch2Previous = touch2.screenPosition - touch2.delta;

            return -pinchSpeed * (Vector2.Distance(touch1Previous, touch2Previous) - Vector2.Distance(touch1.screenPosition, touch2.screenPosition));
        } else if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 0) {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.scroll.ReadValue().normalized.y;
#else
            return Input.mouseScrollDelta.y;
#endif
        } else {
            return 0f;
        }
    }

    public void StartFollowingTarget(Transform transformToFollow, Transform cameraToAlign) {
        if (MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS) {
            cameraControllerTouch.enabled = false;
        } else if (MapManager.Instance.CurrentMapType == MapManager.MapType.Cesium) {
            cesiumCameraController.enableMovement = false;
            cesiumCameraController.enableRotation = false;
        }
        cameraFollow.enabled = true;
        cameraFollow.StartFollowing(transformToFollow, cameraToAlign);
        FollowingTarget = true;
    }

    public void StopFollowingTarget() {
        if (MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS) {
            cameraControllerTouch.enabled = true;
        } else if (MapManager.Instance.CurrentMapType == MapManager.MapType.Cesium) {
            cesiumCameraController.enableMovement = true;
            cesiumCameraController.enableRotation = true;
        }
        cameraFollow.enabled = false;
        cameraFollow.StopFollowing();
        FollowingTarget = false;
    }

    public void SetCameraView(CameraView view) {
        //if (droneFPV == null && DroneManager.Instance.Drones.Count >= 1) {
        //    IEnumerator enumerator = DroneManager.Instance.Drones.Values.GetEnumerator();
        //    enumerator.MoveNext();
        //    droneFPV = (InteractiveObject) enumerator.Current;
        //}
        if (droneFPV == null && DroneManager.Instance.Drones.Count >= 1) {
            droneFPV = DroneManager.Instance.ActiveDrone;
        }
        if (droneFPV != null) {
            switch (view) {
                case CameraView.FirstPerson:
                    UnsetFreeLook();
                    SetFPV(droneFPV);
                    break;
                case CameraView.FreeLook:
                    UnsetFPV(droneFPV);
                    SetFreeLook();
                    break;
            }
        }
    }

    private void SetFPV(InteractiveObject firstPerson) {
        firstPerson.FPVOnlyCamera.gameObject.SetActive(true);
        firstPerson.JPEGTexture.gameObject.SetActive(false);
        firstPerson.SetARBackground(arCameraBackgroundImage);
        firstPerson.FPVOnlyCamera.tag = "MainCamera";
        ARCameraBackground.SetActive(true);
        ARCameraBackground.GetComponent<Canvas>().worldCamera = firstPerson.FPVOnlyCamera;
        arCameraBackgroundImage.texture = firstPerson.GetCameraTexture();
        CurrentCameraView = CameraView.FirstPerson;
    }

    private void UnsetFPV(InteractiveObject firstPerson) {
        firstPerson.FPVOnlyCamera.gameObject.SetActive(false);
        firstPerson.JPEGTexture.gameObject.SetActive(true);
        firstPerson.SetARBackground(null);
        firstPerson.FPVOnlyCamera.tag = "Untagged";
        ARCameraBackground.SetActive(false);
        CurrentCameraView = CameraView.None;
    }

    private void SetFreeLook() {
        MainCamera.gameObject.SetActive(true);
        DroneManager.Instance.SetDroneModelsActive(true);
        UI.SetActive(true);
        MainCamera.tag = "MainCamera";
        DisplayVRScene(true);
        CurrentCameraView = CameraView.FreeLook;
    }

    private void UnsetFreeLook() {
        MainCamera.gameObject.SetActive(false);
        DroneManager.Instance.SetDroneModelsActive(false);
        UI.SetActive(false);
        MainCamera.tag = "Untagged";
        DisplayVRScene(false);
        CurrentCameraView = CameraView.None;
    }

    private Matrix4x4 CreateProjectionMatrix() {
        // Intrinsic parameters
        Matrix4x4 intrinsicMatrix = new Matrix4x4(
            new Vector4(1015.9761352539063f, 0.0f, 630.6932373046875f, 0.0f),
            new Vector4(0.0f, 1017.92529296875f, 346.8971862792969f, 0.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
        );

        // Extrinsic parameters (assumed identity rotation and zero translation)
        Matrix4x4 extrinsicMatrix = new Matrix4x4(
            new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
        );

        // Full projection matrix (P = K * [R | t])
        Matrix4x4 projectionMatrix = intrinsicMatrix * extrinsicMatrix;

        Debug.Log("Projection Matrix: " + projectionMatrix);

        return projectionMatrix;
    }

    public void SetCurrentInteractiveObject(InteractiveObject intObject) {
        droneFPV = intObject;
    }

    public void SwitchCameraView() {
        SetCameraView(CurrentCameraView == CameraView.FreeLook ? CameraView.FirstPerson : CameraView.FreeLook);
    }

    public void SetCameraFPV(bool fpvOn) {
        SetCameraView(fpvOn ? CameraView.FirstPerson : CameraView.FreeLook);
    }

}
