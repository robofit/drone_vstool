using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour {

    [SerializeField]
    private Camera CameraToControl;
    [SerializeField]
    private CesiumCameraController CesiumCamController;

    private void Start() {
        CesiumCamController.enableRotation = false;
        CesiumCamController.enableMovement = false;
    }

    private void Update() {
        if (Mouse.current.rightButton.isPressed) {
            CesiumCamController.enableRotation = true;
        } else if (Mouse.current.rightButton.wasReleasedThisFrame) {
            CesiumCamController.enableRotation = false;
        }

        if (Mouse.current.leftButton.isPressed) {
            CesiumCamController.enableMovement = true;
        } else if (Mouse.current.leftButton.wasReleasedThisFrame) {
            CesiumCamController.enableMovement= false;
        }

        if (Keyboard.current[Key.Z].isPressed) {
            CameraToControl.transform.Translate(Vector3.up);
        }

        if (Keyboard.current[Key.X].isPressed) {
            CameraToControl.transform.Translate(Vector3.down);
        }
    }

}
