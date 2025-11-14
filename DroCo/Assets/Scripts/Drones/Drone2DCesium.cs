using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(CesiumGlobeAnchor))]
public class Drone2DCesium : Drone2D {


    private CesiumGlobeAnchor GPSLocation;
    private LayerMask layerMask;

    public double3 GPSLocationNoOffset => new double3(GPSLocation.longitudeLatitudeHeight.x - gpsOffset.longitude, GPSLocation.longitudeLatitudeHeight.y - gpsOffset.latitude, GPSLocation.longitudeLatitudeHeight.z);

    private GPS gpsOffset = new GPS() { latitude = 0, longitude = 0 };

    private double lastGroundHeight = 0;

    public override void InitDrone() {
        base.InitDrone();

        layerMask = ~LayerMask.GetMask("Mission", "DroneScreen");

        GPSLocation = GetComponent<CesiumGlobeAnchor>();
        GPSLocation.enabled = true;
    }

    public override void UpdateFlightData(DroneFlightData flightData) {
        GPSLocation.longitudeLatitudeHeight = new double3(flightData.gps.longitude + gpsOffset.longitude, flightData.gps.latitude + gpsOffset.latitude, GetGroundHeight());
        droneImage.transform.localRotation = Quaternion.Euler(new Vector3(90f, (float)flightData.aircraft_orientation.yaw, 0));
        if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Experiment) {
            ExperimentManager.Instance.TopPanel.SetAltitudeText((Mathf.Round((float) flightData.relative_altitude * 10.0f) * 0.1f).ToString());
            ExperimentManager.Instance.TopPanel.SetSpeedText((Mathf.Round(new Vector3((float) flightData.aircraft_velocity.velocity_x, (float) flightData.aircraft_velocity.velocity_y, (float) flightData.aircraft_velocity.velocity_z).magnitude * 10.0f) * 0.1f).ToString());
        } else if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.MobileTopDown) {
            TopDownViewGUI.Instance.TopPanel.SetAltitudeText((Mathf.Round((float) flightData.relative_altitude * 10.0f) * 0.1f).ToString());
            TopDownViewGUI.Instance.TopPanel.SetSpeedText((Mathf.Round(new Vector3((float) flightData.aircraft_velocity.velocity_x, (float) flightData.aircraft_velocity.velocity_y, (float) flightData.aircraft_velocity.velocity_z).magnitude * 10.0f) * 0.1f).ToString());
        }
    }

    public double GetGroundHeight() {
        if (Physics.Raycast(transform.position + new Vector3(0, 1000, 0), transform.TransformDirection(Vector3.down), out RaycastHit hit, Mathf.Infinity, layerMask)) {
            if (hit.collider != null && hit.distance < 50000) {
                lastGroundHeight = GPSLocation.longitudeLatitudeHeight.z - (double) (hit.distance - 1000);
                return lastGroundHeight;
            }
        }
        return lastGroundHeight;
    }

    public override void SetGPSOffset(GPS offset) {
        gpsOffset = offset;
    }

    public override GPS GetDroneLocation() {
        return new GPS() { longitude = GPSLocationNoOffset.x, latitude = GPSLocationNoOffset.y };
    }
}
