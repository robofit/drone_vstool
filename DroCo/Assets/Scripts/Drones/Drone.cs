using System;
using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Elevation.Base;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public abstract class Drone : InteractiveObject, IPointerNotifier {

    public DroneFlightData FlightData {
        get; set;
    }

    public DroneStaticData StaticData {
        get; set;
    }

    public VehicleDataRenderer VehicleRenderer;
    public MeshRenderer VideoScreen;

    public Transform DroneVideoScreen;
    public Transform ThirdPersonView;

    public ListItemButton DroneListItem;

    public GameObject Drone2DPrefab;
    public Drone2D Drone2DRepresentation {
        get; private set;
    }


    private Texture2D JpegPlayerTexture;

    public Material OcclusionMaterial;

    // Store the drone's current position in world coordinates
    private Vector3 dronePosition = Vector3.zero;

    private DateTime lastTime;

    public RawImage ExperimentDroneVideoFrame;

    private List<DroneFlightData> DroneFlightDataBuffer = new List<DroneFlightData>();

    //public int Delay = 10;
    //private int prevDelay = 10;


    public virtual void InitDrone(DroneStaticData staticData) {
        Drone2DRepresentation = Instantiate(Drone2DPrefab, MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS ? GameManager.Instance.Map2DViewArcGISMap.transform : GameManager.Instance.Map2DViewCesium).GetComponent<Drone2D>();
        Drone2DRepresentation.InitDrone();

        MissionManager.Instance.ChangeTargetFaceCamera(FPVOnlyCamera.transform);
        MissionManager.Instance.ChangeTarget(transform);

        JpegPlayerTexture = new Texture2D(1, 1);

        //prevDelay = Delay;
    }

    public void UpdateDroneFlightData(DroneFlightData flightData, bool droneCopy = false) {
        //if (DroneManager.Instance.UseKalman) {
        //    UpdateDroneByKalman(flightData);
        //} else {
        //    UpdateDroneByGPS(flightData);
        //}

        UpdateDroneByGPS(flightData);
    }

    private void UpdateDroneByGPS(DroneFlightData flightData) {
        FlightData = flightData;

        //DroneFlightData bufferedData;

        //DroneFlightDataBuffer.Add(flightData);

        //if (prevDelay != Delay) {
        //    DroneFlightDataBuffer.Clear();
        //    prevDelay = Delay;
        //}

        //if (DroneFlightDataBuffer.Count > Delay) {
        //    bufferedData = DroneFlightDataBuffer[0];
        //    DroneFlightDataBuffer.RemoveAt(0);
        //} else {
        //    bufferedData = flightData;
        //}

        UpdateDroneLocation(flightData);

        DroneVideoScreen.localRotation = Quaternion.Euler(-(float) flightData.gimbal_orientation.pitch, (float) (flightData.aircraft_orientation.yaw + flightData.gimbal_orientation.yaw_relative), -(float) flightData.gimbal_orientation.roll);
        ThirdPersonView.localRotation = Quaternion.Euler(0f, (float) flightData.aircraft_orientation.yaw, 0f);

        Drone2DRepresentation.UpdateFlightData(flightData);

        DroneListItem.UpdateHeight(flightData.altitude);
        DroneListItem.UpdateDistance(Vector3.Distance(Camera.main.transform.position, this.transform.position));

        // Update UI flight data info
        if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Experiment) {
            ExperimentManager.Instance.TopPanel.SetAltitudeText((Mathf.Round((float) flightData.relative_altitude * 10.0f) * 0.1f).ToString());
            ExperimentManager.Instance.TopPanel.SetSpeedText((Mathf.Round(new Vector3((float) flightData.aircraft_velocity.velocity_x, (float) flightData.aircraft_velocity.velocity_y, (float) flightData.aircraft_velocity.velocity_z).magnitude * 10.0f) * 0.1f).ToString());
        } else if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Server || GameManager.Instance.CurrentAppMode == GameManager.AppMode.ClientLocal) {
            TopDownViewGUI.Instance.TopPanel.SetAltitudeText((Mathf.Round((float) flightData.relative_altitude * 10.0f) * 0.1f).ToString());
            TopDownViewGUI.Instance.TopPanel.SetSpeedText((Mathf.Round(new Vector3((float) flightData.aircraft_velocity.velocity_x, (float) flightData.aircraft_velocity.velocity_y, (float) flightData.aircraft_velocity.velocity_z).magnitude * 10.0f) * 0.1f).ToString());
        }

        //if (flightData.frame != "") {
        if (flightData.frame != null && flightData.frame.Length > 0) {
            //byte[] frame;
            //try {
            //    frame = Convert.FromBase64String(flightData.frame);
            //} catch (Exception e) {
            //    Debug.Log(e.Message + " Skipping frame.");
            //    return;
            //}

            //JpegPlayerTexture.LoadImage(frame);
            JpegPlayerTexture.LoadImage(flightData.frame);

            if (ArCameraBackground != null) {
                ArCameraBackground.texture = JpegPlayerTexture;
            }
            VideoScreen.material.mainTexture = JpegPlayerTexture;
            OcclusionMaterial.SetTexture("_CameraFeedTexture", JpegPlayerTexture);

            if (ExperimentDroneVideoFrame != null) {
                ExperimentDroneVideoFrame.texture = JpegPlayerTexture;
            }
        }
    }

    public DroneFlightData GetUpdatedFlightData(bool includeVideo = true) {
        DroneFlightData updatedData = new DroneFlightData() {
            client_id = FlightData.client_id,
            altitude = FlightData.altitude,
            relative_altitude = FlightData.relative_altitude,
            gps = GetDroneLocationCalibrated(),
            aircraft_orientation = FlightData.aircraft_orientation,
            aircraft_velocity = FlightData.aircraft_velocity,
            gimbal_orientation = FlightData.gimbal_orientation,
            satellite_count = FlightData.satellite_count,
            gps_signal_level = FlightData.gps_signal_level,
            sticks = FlightData.sticks,
            timestamp = FlightData.timestamp,
            //frame = includeVideo ? FlightData.frame : ""
            frame = includeVideo ? FlightData.frame : null
        };

        return updatedData;
    }

    protected abstract void UpdateDroneLocation(DroneFlightData flightData);

    public abstract GPS GetDroneLocationCalibrated();

    public abstract GPS GetDroneLocation();

    public abstract float GetAMSL();

    public abstract float GetCorrectedAMSL();

    public abstract float GetAGL();

    //private void UpdateDroneByKalman(DroneFlightData flightData) {
    //    //// Extract velocity and orientation
    //    //Vector3 velocity = new Vector3(
    //    //    (float) flightData.aircraft_velocity.velocity_x,
    //    //    (float) flightData.aircraft_velocity.velocity_y,
    //    //    (float) flightData.aircraft_velocity.velocity_z
    //    //);

    //    // Extract velocity and orientation
    //    Vector3 velocity = new Vector3(
    //        -(float) flightData.aircraft_velocity.velocity_y,
    //        -(float) flightData.aircraft_velocity.velocity_z,
    //        (float) flightData.aircraft_velocity.velocity_x
    //    );

    //    float roll = (float) flightData.aircraft_orientation.roll;
    //    float pitch = (float) flightData.aircraft_orientation.pitch;
    //    float yaw = (float) flightData.aircraft_orientation.yaw;

    //    // Convert orientation to a quaternion (from drone's local space to world space)
    //    Quaternion rotation = Quaternion.Euler(-pitch, yaw, -roll);

    //    // Transform velocity from local to world coordinates
    //    Vector3 worldVelocity = rotation * velocity;

    //    // Calculate time difference between the current and last timestamp
    //    DateTime currentTime = DateTime.Parse(flightData.timestamp);
    //    TimeSpan timeDifference = currentTime - lastTime;
    //    float deltaTime = (float) timeDifference.TotalSeconds;
    //    lastTime = currentTime;

    //    // Update the drone's position in world coordinates
    //    dronePosition += worldVelocity * deltaTime;

    //    // Update the drone's position in Unity
    //    //hpTransform.LocalPosition = dronePosition;
    //    transform.localPosition = dronePosition;

    //    // Set rotation for the drone's 3D model (optional if you want to visualize its orientation)
    //    transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

    //    // Update video feed orientation
    //    DroneVideoScreen.localRotation = Quaternion.Euler(
    //        -(float) flightData.gimbal_orientation.pitch,
    //        (float) flightData.aircraft_orientation.yaw + (float) flightData.gimbal_orientation.yaw_relative,
    //        -(float) flightData.gimbal_orientation.roll
    //    );

    //    // Update third-person view (if needed)
    //    ThirdPersonView.localRotation = Quaternion.Euler(0f, yaw, 0f);

    //    // Update drone 2D representation and UI
    //    drone2DRepresentation.UpdateFlightData(flightData);
    //    DroneListItem.UpdateHeight(flightData.altitude);
    //    DroneListItem.UpdateDistance(Vector3.Distance(Camera.main.transform.position, this.transform.position));

    //    // Handle frame data for the video feed
    //    if (flightData.frame != "") {
    //        byte[] frame = Convert.FromBase64String(flightData.frame);
    //        JpegPlayerTexture.LoadImage(frame);

    //        if (ArCameraBackground != null) {
    //            ArCameraBackground.texture = JpegPlayerTexture;
    //        }

    //        VideoScreen.material.mainTexture = JpegPlayerTexture;
    //        OcclusionMaterial.SetTexture("_CameraFeedTexture", JpegPlayerTexture);
    //    }
    //}

    //private void UpdateDroneByKalmanHP(DroneFlightData flightData) {
    //    //// Extract velocity and orientation
    //    //Vector3 velocity = new Vector3(
    //    //    (float) flightData.aircraft_velocity.velocity_x,
    //    //    (float) flightData.aircraft_velocity.velocity_y,
    //    //    (float) flightData.aircraft_velocity.velocity_z
    //    //);

    //    // Extract velocity and orientation
    //    Vector3 velocity = new Vector3(
    //        -(float) flightData.aircraft_velocity.velocity_y,
    //        -(float) flightData.aircraft_velocity.velocity_z,
    //        (float) flightData.aircraft_velocity.velocity_x
    //    );

    //    float roll = (float) flightData.aircraft_orientation.roll;
    //    float pitch = (float) flightData.aircraft_orientation.pitch;
    //    float yaw = (float) flightData.aircraft_orientation.yaw;

    //    // Convert orientation to a quaternion (from drone's local space to world space)
    //    Quaternion rotation = Quaternion.Euler(-pitch, yaw, -roll);

    //    // Transform velocity from local to world coordinates
    //    Vector3 worldVelocity = rotation * velocity;

    //    // Calculate time difference between the current and last timestamp
    //    DateTime currentTime = DateTime.Parse(flightData.timestamp);
    //    TimeSpan timeDifference = currentTime - lastTime;
    //    float deltaTime = (float) timeDifference.TotalSeconds;
    //    lastTime = currentTime;

    //    // Update the drone's position in world coordinates
    //    dronePosition += worldVelocity * deltaTime;

    //    // Update the drone's position in Unity
    //    //hpTransform.LocalPosition = dronePosition;
    //    //hpTransform.LocalPosition = new Unity.Mathematics.double3((double) dronePosition.x, (double) dronePosition.y, (double) dronePosition.z);

    //    // Set rotation for the drone's 3D model (optional if you want to visualize its orientation)
    //    //hpTransform.LocalRotation = Quaternion.Euler(0f, yaw, 0f);

    //    // Update video feed orientation
    //    DroneVideoScreen.localRotation = Quaternion.Euler(
    //        -(float) flightData.gimbal_orientation.pitch,
    //        (float) flightData.aircraft_orientation.yaw + (float) flightData.gimbal_orientation.yaw_relative,
    //        -(float) flightData.gimbal_orientation.roll
    //    );

    //    // Update third-person view (if needed)
    //    ThirdPersonView.localRotation = Quaternion.Euler(0f, yaw, 0f);

    //    // Update drone 2D representation and UI
    //    drone2DRepresentation.UpdateFlightData(flightData);
    //    DroneListItem.UpdateHeight(flightData.altitude);
    //    DroneListItem.UpdateDistance(Vector3.Distance(Camera.main.transform.position, this.transform.position));

    //    // Handle frame data for the video feed
    //    if (flightData.frame != "") {
    //        byte[] frame = Convert.FromBase64String(flightData.frame);
    //        JpegPlayerTexture.LoadImage(frame);

    //        if (ArCameraBackground != null) {
    //            ArCameraBackground.texture = JpegPlayerTexture;
    //        }

    //        VideoScreen.material.mainTexture = JpegPlayerTexture;
    //        OcclusionMaterial.SetTexture("_CameraFeedTexture", JpegPlayerTexture);
    //    }
    //}

    public override Texture GetCameraTexture() {
        return JpegPlayerTexture;
    }

    public void UpdateDroneVehicleData(DroneVehicleData vehicleData) {
        //if(PipelinePlayer == null && VehicleRenderer == null) return;
        //VehicleRenderer.gameObject.SetActive(true);
        //VehicleRenderer.UpdateVehicleData(vehicleData);
    }

    public override void Highlight(bool highlight) {
        base.Highlight(highlight);
        Drone2DRepresentation.Highlight(highlight);
    }


    public void OnClicked(IPointerNotifier.ClickObject clickObject) {
        if (clickObject == IPointerNotifier.ClickObject.DroneScreen) {
            DroneManager.Instance.SetActiveDrone(this);
            CameraManager.Instance.SetCurrentInteractiveObject(this);
            CameraManager.Instance.SetCameraView(CameraManager.CameraView.FirstPerson);
        }
    }

    public void OnDestroyHandle() {
        Destroy(DroneListItem.gameObject);
        Destroy(Drone2DRepresentation.gameObject);
    }

    public virtual void SetGPSOffset(GPS offset) {
        Drone2DRepresentation.SetGPSOffset(offset);
    }
}
