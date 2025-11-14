using System;
using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

public class DroneController : Singleton<DroneController> {

    public DroneCesium SimulatedDrone;
    public Transform DroneGimbal;
    public TopPanel TopPanel;
    public float StartupAltitude;
    private DroneNewInputs controls;

    private Vector3 movementDirection = Vector3.zero;
    private float verticalMovement = 0f;
    private float yawRotation = 0f;
    public float moveSpeed = 5f;
    public float yawSpeed = 50f;
    public float gimbalSpeed = 25f;
    private float gimbalRotation = 0f;

    public float sensitivity = 1.8f; // Adjust for more or less sensitivity

    private const float MAX_STICK_VALUE = 660f; // Used for normalization

    private const float GIMBAL_ROTATION_LIMIT_DOWN = 90f;
    private const float GIMBAL_ROTATION_LIMIT_UP = 30f;

    private void Awake() {
        controls = new DroneNewInputs();

        controls.DroneInputs.Movement.started += MovementStarted;
        controls.DroneInputs.Movement.performed += MovementPerformed;
        controls.DroneInputs.Movement.canceled += MovementCanceled;

        controls.DroneInputs.Vertical.started += VerticalStarted;
        controls.DroneInputs.Vertical.performed += VerticalPerformed;
        controls.DroneInputs.Vertical.canceled += VerticalCanceled;

        controls.DroneInputs.Yaw.started += YawStarted;
        controls.DroneInputs.Yaw.performed += YawPerformed;
        controls.DroneInputs.Yaw.canceled += YawCanceled;
    }

    private void Start() {
        SimulatedDrone.InitDrone(new DroneStaticData() { client_id = "simulated" , drone_name= "mavic", serial = "123"});
        StartCoroutine(SendFlightData());
    }

    private IEnumerator SendFlightData() {
        while (true) {
            ExperimentManager.Instance.SendDroneGPSFlightData(SimulatedDrone.GPSLocation.longitudeLatitudeHeight, SimulatedDrone.GPSLocation.rotationEastUpNorth, gimbalRotation);
            yield return new WaitForSeconds(1f);
        }
    }

    private void MovementStarted(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        movementDirection = ConvertInputToWorld(obj.ReadValue<Vector2>());
    }

    private void MovementPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        movementDirection = ConvertInputToWorld(obj.ReadValue<Vector2>());
    }

    private void MovementCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        movementDirection = Vector3.zero;
    }

    private void VerticalStarted(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        verticalMovement = obj.ReadValue<float>();
    }

    private void VerticalPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        verticalMovement = obj.ReadValue<float>();
    }

    private void VerticalCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        verticalMovement = 0f;
    }

    private void YawStarted(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        yawRotation = obj.ReadValue<float>();
    }

    private void YawPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        yawRotation = obj.ReadValue<float>();
    }

    private void YawCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        yawRotation = 0f;
    }

    private void Update() {
        if (SimulatedDrone == null)
            return;

        // Apply movement
        SimulatedDrone.transform.position += (movementDirection * moveSpeed + Vector3.up * verticalMovement * moveSpeed) * Time.deltaTime;

        // Apply yaw rotation
        SimulatedDrone.transform.Rotate(Vector3.up, yawRotation * yawSpeed * Time.deltaTime);

        Quaternion previousRotation = DroneGimbal.rotation;

        DroneGimbal.Rotate(Vector3.right, -gimbalRotation * gimbalSpeed * Time.deltaTime);

        // Convert eulerAngles.x to a signed range (-180, 180)
        float gimbalAngle = DroneGimbal.eulerAngles.x;
        if (gimbalAngle > 180f) {
            gimbalAngle -= 360f;
        }

        // Clamp within limits
        if (gimbalAngle < -30f || gimbalAngle > 89f) {
            DroneGimbal.rotation = previousRotation;  // Restore previous rotation if out of bounds
        }
    }

    private Vector3 ConvertInputToWorld(Vector2 input) {
        Vector3 forward = SimulatedDrone.transform.forward;
        Vector3 right = SimulatedDrone.transform.right;

        return (forward * input.y + right * input.x);
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    public void HandleReceivedRemoteControllerData(RemoteController data) {
        // Normalize stick values to (-1, 1), allowing for finer control
        float leftStickX = Mathf.Clamp(data.left_stick.x / MAX_STICK_VALUE, -1f, 1f);
        float leftStickY = Mathf.Clamp(data.left_stick.y / MAX_STICK_VALUE, -1f, 1f);
        float rightStickX = Mathf.Clamp(data.right_stick.x / MAX_STICK_VALUE, -1f, 1f);
        float rightStickY = Mathf.Clamp(data.right_stick.y / MAX_STICK_VALUE, -1f, 1f);

        // Apply a non-linear response curve for more precise control at lower input values
        leftStickX = Mathf.Sign(leftStickX) * Mathf.Pow(Mathf.Abs(leftStickX), sensitivity);
        leftStickY = Mathf.Sign(leftStickY) * Mathf.Pow(Mathf.Abs(leftStickY), sensitivity);
        rightStickX = Mathf.Sign(rightStickX) * Mathf.Pow(Mathf.Abs(rightStickX), sensitivity);
        rightStickY = Mathf.Sign(rightStickY) * Mathf.Pow(Mathf.Abs(rightStickY), sensitivity);

        // Left stick controls movement (forward/backward & left/right)
        movementDirection = ConvertInputToWorld(new Vector2(rightStickX, rightStickY));

        // Right stick controls yaw (rotation) and vertical movement (up/down)
        yawRotation = leftStickX;  // Scale yaw by sensitivity
        verticalMovement = leftStickY; // Scale vertical movement by sensitivity

        // Apply gimbal movement
        gimbalRotation = Mathf.Clamp(data.gimbal_wheel / MAX_STICK_VALUE, -1f, 1f);

        //ExperimentManager.Instance.SendDroneGPSFlightData(SimulatedDrone.GPSLocation.longitudeLatitudeHeight, SimulatedDrone.GPSLocation.rotationEastUpNorth, gimbalRotation);
        ExperimentManager.Instance.SendDroneFlightData(movementDirection, yawRotation, verticalMovement, gimbalRotation);

    }

    public void UpdateDroneGPSDataFromServer(double3 latitudeLongitudeHeight, quaternion rotation, float gimbal) {
        SimulatedDrone.GPSLocation.longitudeLatitudeHeight = latitudeLongitudeHeight;
        SimulatedDrone.GPSLocation.rotationEastUpNorth = rotation;
        gimbalRotation = gimbal;
    }

    public void UpdateDroneDataFromServer(Vector3 movement, float yaw, float vertical, float gimbal) {
        movementDirection = movement;
        yawRotation = yaw;
        verticalMovement = vertical;
        gimbalRotation = gimbal;

        TopPanel.SetAltitudeText((Mathf.Round((float) (SimulatedDrone.GPSLocation.longitudeLatitudeHeight.z - StartupAltitude) * 10.0f) * 0.1f).ToString());
    }
}
