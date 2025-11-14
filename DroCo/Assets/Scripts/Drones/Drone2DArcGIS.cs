using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;

[RequireComponent(typeof(ArcGISLocationComponent))]
public class Drone2DArcGIS : Drone2D {

    private ArcGISLocationComponent GPSLocation;

    public ArcGISPoint GPSLocationNoOffset => new ArcGISPoint(GPSLocation.Position.X - gpsOffset.longitude, GPSLocation.Position.Y - gpsOffset.latitude, GPSLocation.Position.Z, new ArcGISSpatialReference(4326));

    private GPS gpsOffset = new GPS() { latitude = 0, longitude = 0 };

    public override void InitDrone() {
        base.InitDrone();

        GPSLocation = GetComponent<ArcGISLocationComponent>();
        GPSLocation.enabled = true;
    }

    public override void UpdateFlightData(DroneFlightData flightData) {
        GPSLocation.Position = new ArcGISPoint(flightData.gps.longitude + gpsOffset.longitude, flightData.gps.latitude + gpsOffset.latitude, 0, new ArcGISSpatialReference(4326));
        GPSLocation.Rotation = new ArcGISRotation(flightData.aircraft_orientation.yaw, 0, 0);
    }

    public override void SetGPSOffset(GPS offset) {
        gpsOffset = offset;
    }

    public override GPS GetDroneLocation() {
        ArcGISPoint location = GPSLocationNoOffset;
        return new GPS() { longitude = location.X, latitude = location.Y };
    }
}
