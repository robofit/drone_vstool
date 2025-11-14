using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;
using UnityEngine;

[RequireComponent(typeof(ArcGISLocationComponent))]
public class DroneArcGIS : Drone {


    public ArcGISLocationComponent GPSLocation {
        get; private set;
    }

    public ArcGISPoint GPSLocationNoOffset => new ArcGISPoint(GPSLocation.Position.X - gpsOffset.longitude, GPSLocation.Position.Y - gpsOffset.latitude, GPSLocation.Position.Z, new ArcGISSpatialReference(4326));

    public GPS gpsOffset = new GPS() { latitude = 0, longitude = 0 };

    public override void InitDrone(DroneStaticData staticData) {
        base.InitDrone(staticData);

        GPSLocation = GetComponent<ArcGISLocationComponent>();
        GPSLocation.enabled = true;
        GPSLocation.SurfacePlacementMode = ArcGISSurfacePlacementMode.RelativeToGround;
    }

    protected override void UpdateDroneLocation(DroneFlightData flightData) {
        GPSLocation.Position = new ArcGISPoint(flightData.gps.longitude + gpsOffset.longitude, flightData.gps.latitude + gpsOffset.latitude, GPSLocation.Position.Z, new ArcGISSpatialReference(4326));
        GPSLocation.SurfacePlacementOffset = flightData.relative_altitude;
        //GPSLocation.Rotation = new ArcGISRotation(flightData.aircraft_orientation.compass, flightData.aircraft_orientation.pitch, flightData.aircraft_orientation.roll);
        DroneModel.localRotation = Quaternion.Euler(-(float) flightData.aircraft_orientation.pitch, (float) flightData.aircraft_orientation.yaw, -(float) flightData.aircraft_orientation.roll);
        //DroneVideoScreen.localRotation = Quaternion.Euler(-(float) flightData.aircraft_orientation.pitch, (float) flightData.aircraft_orientation.yaw, -(float) flightData.aircraft_orientation.roll);
        //DroneVideoScreen.localRotation = Quaternion.Euler(-(float) flightData.gimbal_orientation.pitch, (float) flightData.gimbal_orientation.yaw, -(float) flightData.gimbal_orientation.roll);
        //DroneVideoScreen.localRotation = Quaternion.Euler(-(float) flightData.gimbal_orientation.pitch, (float) flightData.gimbal_orientation.yaw_relative, -(float) flightData.gimbal_orientation.roll);
        //Debug.Log((float) flightData.aircraft_orientation.yaw + " + " + (float) flightData.gimbal_orientation.yaw_relative + " = " + ((float) flightData.aircraft_orientation.yaw + (float) flightData.gimbal_orientation.yaw_relative));
    }

    public override void SetGPSOffset(GPS offset) {
        base.SetGPSOffset(offset);

        gpsOffset = offset;
    }

    public override GPS GetDroneLocation() {
        ArcGISPoint location = GPSLocationNoOffset;
        return new GPS() { longitude = location.X, latitude = location.Y };
    }

    public override GPS GetDroneLocationCalibrated() {
        return new GPS() { longitude = GPSLocation.Position.X, latitude = GPSLocation.Position.Y };
    }

    public override float GetAMSL() {
        return (float) GPSLocation.Position.Z;
    }

    public override float GetCorrectedAMSL() {
        return (float) GPSLocation.Position.Z;
    }

    public override float GetAGL() {
        return (float) GPSLocation.SurfacePlacementOffset;
    }
}
