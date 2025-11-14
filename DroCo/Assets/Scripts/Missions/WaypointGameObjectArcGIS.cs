using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using UnityEngine;

public class WaypointGameObjectArcGIS : WaypointGameObject {

    public ArcGISLocationComponent GPSLocation {
        get; private set;
    }

    public override void Start() {
        base.Start();
    }

    public override void Update() {
        base.Update();
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

    public override void SetLocation() {
        GPSLocation = GetComponent<ArcGISLocationComponent>();
    }
}
