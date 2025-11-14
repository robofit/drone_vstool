using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
public class ExperimentSettings : MonoBehaviour {
    public enum GPSOrigin {
        BRNO,
        PRAGUE
    }

    [Header("Select App Mode Loadout")]
    public ExperimentManager.AppMode CurrentAppMode;

    [Header("GameObjects by App Mode")]
    public List<GameObject> DesktopUgCSObjects = new List<GameObject>();
    public List<GameObject> TabletARViewObjects = new List<GameObject>();
    public List<GameObject> MobileTopdownViewObjects = new List<GameObject>();
    public List<GameObject> DesktopUgCSMockupObjects = new List<GameObject>();
    public List<GameObject> TabletARViewMockupObjects = new List<GameObject>();

    [Header("Additional Managers to be Set")]
    public MapManager MapManager;
    public ExperimentManager ExperimentManager;
    public CesiumGeoreference CesiumGeoreferenceOrigin;
    public CesiumGlobeAnchor CameraGPS;

    [Header("Connection Materials")]
    public Material ConnectionMaterial;
    public Material ConnectionMaterial2D;
    public LineRenderer ConnectionPrefab;

    [Header("GPS Origin")]
    public GPSOrigin Origin;

    [Header("Brno GPS Map Origin")]
    public double LatitudeB;
    public double LongitudeB;
    public double HeightB;
    public double CameraHeightB;

    [Header("Prague GPS Map Origin")]
    public double LatitudeP;
    public double LongitudeP;
    public double HeightP;
    public double CameraHeightP;

    private ExperimentManager.AppMode previousAppMode;
    private GPSOrigin previousOrigin;

    // Automatically called when a value is changed in the Inspector
    private void OnValidate() {
        // Check if the value of selectedOption has actually changed
        if (CurrentAppMode != previousAppMode) {
            previousAppMode = CurrentAppMode; // Update the previous option
            ApplyActivation();
        }

        if (Origin != previousOrigin) {
            previousOrigin = Origin;
            ChangeOrigin();
        }
    }

    // Method to activate/deactivate GameObjects based on the enum
    public void ApplyActivation() {
        switch (CurrentAppMode) {
            case ExperimentManager.AppMode.DesktopUgCS:
                ActivateObjects(TabletARViewObjects, false);
                ActivateObjects(MobileTopdownViewObjects, false);
                ActivateObjects(TabletARViewMockupObjects, false);
                ActivateObjects(DesktopUgCSMockupObjects, false);
                ActivateObjects(DesktopUgCSObjects, true);
                ExperimentManager.VideoPlayerControls = DesktopUgCSObjects.First().GetComponent<VideoPlayerControls>();
                ConnectionPrefab.materials = new Material[] { ConnectionMaterial };
                break;
            case ExperimentManager.AppMode.TabletARView:
                ActivateObjects(MobileTopdownViewObjects, false);
                ActivateObjects(DesktopUgCSObjects, false);
                ActivateObjects(TabletARViewMockupObjects, false);
                ActivateObjects(DesktopUgCSMockupObjects, false);
                ActivateObjects(TabletARViewObjects, true);
                ExperimentManager.VideoPlayerControls = TabletARViewObjects.First().GetComponent<VideoPlayerControls>();
                ConnectionPrefab.materials = new Material[] { ConnectionMaterial };
                break;
            case ExperimentManager.AppMode.MobileTopdownView:
                ActivateObjects(TabletARViewObjects, false);
                ActivateObjects(DesktopUgCSObjects, false);
                ActivateObjects(TabletARViewMockupObjects, false);
                ActivateObjects(DesktopUgCSMockupObjects, false);
                ActivateObjects(MobileTopdownViewObjects, true);
                ExperimentManager.VideoPlayerControls = MobileTopdownViewObjects.First().GetComponent<VideoPlayerControls>();
                ConnectionPrefab.materials = new Material[] { ConnectionMaterial2D };
                break;
            case ExperimentManager.AppMode.DesktopUgCSMockup:
                ActivateObjects(TabletARViewObjects, false);
                ActivateObjects(MobileTopdownViewObjects, false);
                ActivateObjects(TabletARViewMockupObjects, false);
                ActivateObjects(DesktopUgCSObjects, false);
                ActivateObjects(DesktopUgCSMockupObjects, true);
                ExperimentManager.VideoPlayerControls = DesktopUgCSObjects.First().GetComponent<VideoPlayerControls>();
                ConnectionPrefab.materials = new Material[] { ConnectionMaterial };
                break;
            case ExperimentManager.AppMode.TabletARViewMockup:
                ActivateObjects(TabletARViewObjects, false);
                ActivateObjects(DesktopUgCSObjects, false);
                ActivateObjects(MobileTopdownViewObjects, false);
                ActivateObjects(DesktopUgCSMockupObjects, false);
                ActivateObjects(TabletARViewMockupObjects, true);
                ExperimentManager.VideoPlayerControls = DesktopUgCSObjects.First().GetComponent<VideoPlayerControls>();
                ConnectionPrefab.materials = new Material[] { ConnectionMaterial };
                break;
        }

        // Only ARView uses ArcGIS, rest uses Cesium Tilesets
        MapManager.CurrentMapType = CurrentAppMode == ExperimentManager.AppMode.TabletARView ? MapManager.MapType.ArcGIS : MapManager.MapType.Cesium;
    }

    public void ChangeOrigin() {
        switch (Origin) {
            case GPSOrigin.BRNO:
                CesiumGeoreferenceOrigin.SetOriginLongitudeLatitudeHeight(LongitudeB, LatitudeB, HeightB);
                CameraGPS.longitudeLatitudeHeight = new double3(LongitudeB, LatitudeB, CameraHeightB);
                break;
            case GPSOrigin.PRAGUE:
                CesiumGeoreferenceOrigin.SetOriginLongitudeLatitudeHeight(LongitudeP, LatitudeP, HeightP);
                CameraGPS.longitudeLatitudeHeight = new double3(LongitudeP, LatitudeP, CameraHeightP);
                break;
        }
    }

    private void ActivateObjects(List<GameObject> objects, bool activate) {
        foreach (GameObject obj in objects) {
            if (obj != null) {
                obj.SetActive(activate);
            }
        }
    }
    
}
