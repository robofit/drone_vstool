using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Esri.ArcGISMapsSDK.Components;
using Unity.Mathematics;
using UnityEngine;

public class WaypointGameObjectCesium : WaypointGameObject {

    public CesiumGlobeAnchor GPSLocation {
        get; private set;
    }

    public float AltitudeCorrection = 45f;

    private float AGL = 0f;

    private LayerMask mapLayerMask;

    public override void Start() {
        base.Start();
        mapLayerMask = LayerMask.GetMask("Map");
    }

    public override void Update() {
        base.Update();
    }

    public override float GetAMSL() {
        return Mathf.Round((float) (GPSLocation.longitudeLatitudeHeight.z) * 10.0f) * 0.1f;
    }

    public override float GetCorrectedAMSL() {
        return Mathf.Round((float) (GPSLocation.longitudeLatitudeHeight.z - AltitudeCorrection) * 10.0f) * 0.1f;
    }

    public override float GetAGL() {
        return AGL;
    }

    public override void SetLocation() {
        GPSLocation = GetComponent<CesiumGlobeAnchor>();
    }

    public override void SetAltitudeCoroutine(float altitude) {
        if (isActiveAndEnabled) {
            AGL = altitude;
            StartCoroutine(SetAltitude(altitude));
        }
    }

    private void SetAltitudeAGL(float altitudeAGL) {
        if (Physics.Raycast(transform.position + new Vector3(0, 1000, 0), transform.TransformDirection(Vector3.down), out RaycastHit hit, Mathf.Infinity, mapLayerMask)) {
            if (hit.collider != null) {
                double3 originalPosition = GPSLocation.longitudeLatitudeHeight;
                GPSLocation.longitudeLatitudeHeight = new double3(originalPosition.x, originalPosition.y, originalPosition.z - (double) (hit.distance - 1000) + altitudeAGL);
            }
        }
    }

    private IEnumerator SetAltitude(float altitude) {
        yield return new WaitForSeconds(0.5f);

        SetAltitudeAGL(altitude);
    }
}

