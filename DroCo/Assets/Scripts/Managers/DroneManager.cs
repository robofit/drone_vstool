using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CesiumForUnity;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class DroneManager : Singleton<DroneManager> {

    public bool UseKalman = false;

    public IDictionary<string, Drone> Drones = new Dictionary<string, Drone>();

    private Drone activeDrone;
    public Drone ActiveDrone {
        get {
            if (activeDrone == null && Drones.Count >= 1) {
                IEnumerator enumerator = Drones.Values.GetEnumerator();
                enumerator.MoveNext();
                activeDrone = (Drone) enumerator.Current;
            }
            return activeDrone;
        }
        private set => activeDrone = value;
    }

    public event EventHandler<float> ActiveDroneFlightDataChanged;

    public GameObject DronePrefab;
    public GameObject DronePrefabNoGPS;
    public GameObject DronePrefabGoogle;
    public Transform Scene3DView;
    public Transform SceneCesium;

    public List<ArcGISLocationComponent> CalibrationPoints = new List<ArcGISLocationComponent>();
    private int lastCalibrationPointIndex = 0;

    public RawImage ExperimentDroneVideoFrame;

    public void HandleReceivedDroneList(DroneStaticData[] droneStaticDatas) {
        List<string> dronesToKeep = new List<string>();
        List<string> dronesToRemove = new List<string>();

        foreach (DroneStaticData dsd in droneStaticDatas) {
            if (Drones.ContainsKey(dsd.client_id)) {
                Drones[dsd.client_id].StaticData = dsd;
            } else {
                AddDrone(dsd);
            }

            dronesToKeep.Add(dsd.client_id);
        }

        foreach (KeyValuePair<string, Drone> drone in Drones) {
            if (!dronesToKeep.Contains(drone.Key)) {
                dronesToRemove.Add(drone.Key);
            }
        }

        foreach (string droneId in dronesToRemove) {
            Drones.TryGetValue(droneId, out Drone droneToBeRemoved);
            Drones.Remove(droneId);
            Destroy(droneToBeRemoved.gameObject);
        }
    }


    public void HandleReceivedDroneData(DroneFlightData flightData, bool initDrone) {
        if (initDrone && !Drones.ContainsKey(flightData.client_id)) {
            DroneStaticData newDrone = new DroneStaticData {
                client_id = flightData.client_id,
                drone_name = flightData.client_id,
                serial = "123456"
            };

            AddDrone(newDrone);
        }

        HandleReceivedDroneData(flightData);
    }

    public void HandleReceivedDroneDataAndForward(DroneFlightData flightData) {
        if (Drones.ContainsKey(flightData.client_id)) {
            GameManager.Instance.CenterMap(flightData);
            Drones[flightData.client_id].UpdateDroneFlightData(flightData);
            // Send notification to all line height indicators that active drone flight data has changed
            if (ActiveDrone.FlightData.client_id == flightData.client_id) {
                ActiveDroneFlightDataChanged?.Invoke(this, ActiveDrone.GetAMSL());
            }

            // Forward the data to other connected clients
            WebSocketServer.Instance.SendFlightDataMessageToClients(JsonUtility.ToJson(new WebSocketServerBehavior.Message<DroneFlightData> { type = "data_broadcast", data = Drones[flightData.client_id].GetUpdatedFlightData(includeVideo: true) }));
            WebSocketServer.Instance.SendFlightDataNoVideoMessageToClients(JsonUtility.ToJson(new WebSocketServerBehavior.Message<DroneFlightData> { type = "data_broadcast", data = Drones[flightData.client_id].GetUpdatedFlightData(includeVideo: false) }));

        } else { //prisla data s neznamym drone ID -> pozadame server o novy seznam dronu
            if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Client) {
                WebSocketClient.Instance.SendDroneListRequest();
            }
        }
    }


    public void HandleReceivedDroneData(DroneFlightData flightData) {
        if (Drones.ContainsKey(flightData.client_id)) {
            GameManager.Instance.CenterMap(flightData);
            Drones[flightData.client_id].UpdateDroneFlightData(flightData);
            // Send notification to all line height indicators that active drone flight data has changed
            if (ActiveDrone.FlightData.client_id == flightData.client_id) {
                ActiveDroneFlightDataChanged?.Invoke(this, ActiveDrone.GetAMSL());
            }
        } else { //prisla data s neznamym drone ID -> pozadame server o novy seznam dronu
            if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Client) {
                WebSocketClient.Instance.SendDroneListRequest();
            }
        }
    }

    public void HandleReceivedVehicleData(DroneVehicleData vehicleData) {
        if (Drones.ContainsKey(vehicleData.client_id)) {
            Drones[vehicleData.client_id].UpdateDroneVehicleData(vehicleData);
        } else { //prisla data s neznamym drone ID -> pozadame server o novy seznam dronu
            if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Client) {
                WebSocketClient.Instance.SendDroneListRequest();
            }
        }
    }

    public void AddDrone(DroneStaticData dsd) {
        if (UseKalman) {
            Debug.Log("adding new drone with id: " + dsd.client_id);

            GameObject newDroneGameObj = Instantiate(DronePrefabNoGPS, CalibrationPoints[1]?.transform, false);
            Drone newDrone = newDroneGameObj.GetComponent<Drone>();
            newDrone.InitDrone(dsd);
            Drones.Add(dsd.client_id, newDrone);

            // Init drone's UI
            newDrone.DroneListItem = UIManager.Instance.MainScreen.UnitList.SpawnListItemDrone(dsd, newDrone);
            RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
            newDrone.TPVCamera.targetTexture = renderTexture;
            newDrone.DroneListItem.InitCameraViewTexture(renderTexture);

        } else {
            Debug.Log("adding new drone with id: " + dsd.client_id);            

            // If ArcGIS is being used, instantiate DronePrefab, if Cesium (cesium) is being used, instantiate DronePrefabGoogle
            GameObject newDroneGameObj = MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS ? Instantiate(DronePrefab, Scene3DView) : Instantiate(DronePrefabGoogle, SceneCesium);
            Drone newDrone = newDroneGameObj.GetComponent<Drone>();
            newDrone.InitDrone(dsd);
            newDrone.ExperimentDroneVideoFrame = ExperimentDroneVideoFrame;
            Drones.Add(dsd.client_id, newDrone);

            // Init drone's UI
            newDrone.DroneListItem = UIManager.Instance.MainScreen.UnitList.SpawnListItemDrone(dsd, newDrone);
            RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
            newDrone.TPVCamera.targetTexture = renderTexture;
            newDrone.DroneListItem.InitCameraViewTexture(renderTexture);
        }
    }

    public void SetDroneModelsActive(bool active) {
        foreach (KeyValuePair<string, Drone> drone in Drones) {
            drone.Value.DroneModel.gameObject.SetActive(active);
        }
    }

    public void DestroyDroneAll() {
        //if (GameManager.Instance.CurrentAppMode != GameManager.AppMode.Experiment) {
            foreach (KeyValuePair<string, Drone> drone in Drones) {
                DestroyDrone(drone.Value);
            }
            Drones.Clear();
        //}
    }

    public void DestroyDrone(Drone drone) {
        drone.OnDestroyHandle();
        Destroy(drone.gameObject);
    }

    public void CalibrateDrone() {
        if (ActiveDrone != null) {
            double shortestDistance = math.INFINITY;
            double distance = 0;
            ArcGISGeodeticDistanceResult distanceArc = null;
            ArcGISLocationComponent closestPoint = null;
            GPS droneLocation = ActiveDrone.GetDroneLocation();

            foreach (ArcGISLocationComponent point in CalibrationPoints) {
                distanceArc = ArcGISGeometryEngine.DistanceGeodetic(point.Position, new ArcGISPoint(droneLocation.longitude, droneLocation.latitude), new ArcGISLinearUnit(ArcGISLinearUnitId.Meters), new ArcGISAngularUnit(ArcGISAngularUnitId.Degrees), ArcGISGeodeticCurveType.Geodesic);
                distance = distanceArc.Distance;
                if (distance < shortestDistance) {
                    shortestDistance = distance;
                    closestPoint = point;
                }
            }

            Debug.Log("Closest calibration point is " + closestPoint.gameObject.name + " ..distance: " + shortestDistance);

            ActiveDrone.SetGPSOffset(new GPS() { latitude = closestPoint.Position.Y - droneLocation.latitude, longitude = closestPoint.Position.X - droneLocation.longitude });
        }
    }

    public void CalibrateDroneOnPoint(ArcGISLocationComponent point) {
        GPS droneLocation = ActiveDrone.GetDroneLocation();

        double latitudeOffset = point.Position.Y - droneLocation.latitude;
        double longitudeOffset = point.Position.X - droneLocation.longitude;

        ActiveDrone.SetGPSOffset(new GPS() { latitude = latitudeOffset, longitude = longitudeOffset });

        Debug.Log("Calibrated on " + point.gameObject.name + ", offset is [latitude, longitude] = [" + latitudeOffset + ", " + longitudeOffset + "]");
    }

    public void CalibrateDroneOnPoint(CesiumGlobeAnchor point) {
        GPS droneLocation = ActiveDrone.GetDroneLocation();

        double latitudeOffset = point.longitudeLatitudeHeight.y - droneLocation.latitude;
        double longitudeOffset = point.longitudeLatitudeHeight.x - droneLocation.longitude;

        ActiveDrone.SetGPSOffset(new GPS() { latitude = latitudeOffset, longitude = longitudeOffset });

        Debug.Log("Calibrated on " + point.gameObject.name + ", offset is [latitude, longitude] = [" + latitudeOffset + ", " + longitudeOffset + "]");
    }

    public void CalibrateDroneOnPoint2D(CesiumGlobeAnchor point) {
        GPS droneLocation = ActiveDrone.Drone2DRepresentation.GetDroneLocation();

        double latitudeOffset = point.longitudeLatitudeHeight.y - droneLocation.latitude;
        double longitudeOffset = point.longitudeLatitudeHeight.x - droneLocation.longitude;

        ActiveDrone.SetGPSOffset(new GPS() { latitude = latitudeOffset, longitude = longitudeOffset });

        Debug.Log("Calibrated on " + point.gameObject.name + ", offset is [latitude, longitude] = [" + latitudeOffset + ", " + longitudeOffset + "]");
    }

    public void ResetOffset() {
        if (ActiveDrone != null) {
            ActiveDrone.SetGPSOffset(new GPS() { latitude = 0, longitude = 0 });
        }
    }

    public void CalibrateDroneOnPoint(TMP_Text nextPointText) {
        GPS droneLocation = ActiveDrone.GetDroneLocation();
        ArcGISLocationComponent nextCalibPoint = CalibrationPoints[lastCalibrationPointIndex >= CalibrationPoints.Count ? lastCalibrationPointIndex = 0 : lastCalibrationPointIndex];

        ActiveDrone.SetGPSOffset(new GPS() { latitude = nextCalibPoint.Position.Y - droneLocation.latitude, longitude = nextCalibPoint.Position.X - droneLocation.longitude });
        lastCalibrationPointIndex++;
        Debug.Log("Calibrated on " + nextCalibPoint.gameObject.name);

        nextCalibPoint = CalibrationPoints[lastCalibrationPointIndex >= CalibrationPoints.Count ? lastCalibrationPointIndex = 0 : lastCalibrationPointIndex];
        nextPointText.text = "Calib to " + nextCalibPoint.gameObject.name;
    }

    public void SetActiveDrone(Drone drone) {
        ActiveDrone = drone;
    }
}
