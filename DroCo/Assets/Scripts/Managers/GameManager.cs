using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Samples.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Extent;
using Esri.GameEngine.Geometry;
using Esri.Unity;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Esri.GameEngine;
using PimDeWitte.UnityMainThreadDispatcher;
using Esri.GameEngine.Map;
using Esri.GameEngine.Layers.Base;
using Esri.GameEngine.Elevation.Base;

public class GameManager : Singleton<GameManager> {

    public enum AppMode {
        Client,
        Server,
        ClientLocal,
        Experiment,
        Simulator,
        MobileTopDown,
        UgCS
    }

    public enum DisplayState {
        Scene3DView,
        Map2DView
    }

    public enum ConnectionStatus {
        Connected,
        Disconnected,
        Listening,
        Closed
    }

    [SerializeField]
    private AppMode defaultAppMode = AppMode.Experiment;

    public string ServerIP = "butcluster.ddns.net";
    public int ServerPort = 5555;
    public int RTMPPort = 1935;

    public string APIKey = "";
    public ArcGISMapComponent Scene3DViewArcGISMap;
    public ArcGISMapComponent Map2DViewArcGISMap;
    public Transform Map2DViewCesium;
    public ArcGISCameraComponent MainCamera;
    public ArcGISCameraComponent MinimapCamera;
    public Camera MinimapCameraCesium;
    public GameObject Scene3DView;

    private bool carDetectorRunning = false;

    private bool mapCentered = false;
    private DroneFlightData firstDroneFlightData;

    [SerializeField]
    private MinimapUI minimapUI;
    [SerializeField]
    private ConnectionBar connectionBar;
    [SerializeField]
    private ServerStatusBar serverStatusBar;
    [SerializeField]
    private ConnectionScreen connectionScreen;

    private ArcGISCameraControllerTouch sceneViewCameraController;
    private ArcGISCameraControllerTouch minimapCameraController;

    public AppMode CurrentAppMode {
        get;
        private set;
    }

    public DisplayState CurrentDisplayState {
        get; private set;
    }


    private void Start() {
        ChangeAppMode(defaultAppMode);

        sceneViewCameraController = MainCamera?.GetComponent<ArcGISCameraControllerTouch>();
        minimapCameraController = MinimapCamera?.GetComponent<ArcGISCameraControllerTouch>();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        StartCoroutine(InitSceneView());
    }

    public void ChangeAppMode(AppMode mode) {
        CurrentAppMode = mode;
        switch (CurrentAppMode) {
            case AppMode.Client:
                CloseServerMode();
                CloseClientLocalMode();
                StartClientMode();
                break;
            case AppMode.Server:
                CloseClientMode();
                CloseClientLocalMode();
                StartServerMode();
                break;
            case AppMode.ClientLocal:
                CloseServerMode();
                CloseClientMode();
                StartClientLocalMode();
                break;
            case AppMode.Experiment:
                ExperimentManager.Instance.LoadMissionTraining(true);
                break;
            case AppMode.MobileTopDown:
                //StartClientNoVideoMode();
                break;
            case AppMode.UgCS:
                break;
            case AppMode.Simulator:
                break;
        }
    }

    private void StartClientMode() {
        connectionBar.gameObject.SetActive(true);
        serverStatusBar.gameObject.SetActive(false);

        LoadLastServerIP();
    }

    private void StartClientLocalMode() {
        connectionBar.gameObject.SetActive(true);
        serverStatusBar.gameObject.SetActive(false);

        LoadLastServerIP();
    }

    public void StartClientNoVideoMode(string ip) {
        WebSocketClientSimpleReceiver.Instance.ConnectToServer(ip, ServerPort, requireVideo: false);
    }

    private void CloseClientLocalMode() {
        WebSocketClientSimpleReceiver.Instance.Disconnect();

        // cleanup all drones
        DroneManager.Instance.DestroyDroneAll();
    }

    private void CloseClientMode() {
        WebSocketClient.Instance.Disconnect();

        // cleanup all drones
        DroneManager.Instance.DestroyDroneAll();
    }

    private void StartServerMode() {
        serverStatusBar.gameObject.SetActive(true);
        connectionBar.gameObject.SetActive(false);

        WebSocketServer.Instance.StartServer();
    }

    private void CloseServerMode() {
        WebSocketServer.Instance.CloseServer();
        serverStatusBar.SetServerStatus(ConnectionStatus.Closed);

        // cleanup all drones
        DroneManager.Instance.DestroyDroneAll();

    }

    public void HandleServerRunning(string ip) {
        serverStatusBar.SetServerIP(ip);
        serverStatusBar.SetServerStatus(ConnectionStatus.Listening);
    }

    public void HandleClientDisconnected() {
        serverStatusBar.SetServerStatus(ConnectionStatus.Listening);
    }
    public void HandleClientConnected() {
        serverStatusBar.SetServerStatus(ConnectionStatus.Connected);
    }

    private void LoadLastServerIP() {
        ServerIP = PlayerPrefs.GetString("serverIP", null);
        connectionBar.SetConnectionStatus(ConnectionStatus.Disconnected);
        if (!string.IsNullOrEmpty(ServerIP)) {
            if (CurrentAppMode == AppMode.Client) {
                WebSocketClient.Instance.ConnectToServer(ServerIP, ServerPort);
            } else if (CurrentAppMode == AppMode.ClientLocal) {
                WebSocketClientSimpleReceiver.Instance.ConnectToServer(ServerIP, ServerPort);
            }
            connectionBar.SetServerIP(ServerIP);
        }
    }

    public void SaveServerIP(string serverIP) {
        ServerIP = serverIP;
        PlayerPrefs.SetString("serverIP", ServerIP);
        if (CurrentAppMode == AppMode.Client) {
            WebSocketClient.Instance.ConnectToServer(ServerIP, ServerPort);
        } else if (CurrentAppMode == AppMode.ClientLocal) {
            WebSocketClientSimpleReceiver.Instance.ConnectToServer(ServerIP, ServerPort);
        }
    }

    private IEnumerator InitSceneView() {
        yield return new WaitForEndOfFrame();
        Open3DSceneView();
    }

    public void SwitchSceneMapView() {
        switch (CurrentDisplayState) {
            case DisplayState.Map2DView:
                Open3DSceneView();
                break;
            case DisplayState.Scene3DView:
                Open2DMapView();
                break;
        }
    }

    private void Open3DSceneView() {
        CurrentDisplayState = DisplayState.Scene3DView;
        minimapUI.SetSceneView();

        sceneViewCameraController.enabled = CameraManager.Instance.FollowingTarget ? false : true;
        minimapCameraController.enabled = false;
    }

    private void Open2DMapView() {
        CurrentDisplayState = DisplayState.Map2DView;
        minimapUI.SetMinimapView();

        sceneViewCameraController.enabled = false;
        minimapCameraController.enabled = true;
    }


    private void ListAllGameObjects() {
        Object[] tempList = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        foreach (object go in tempList) {
            if (go is GameObject goObj) {
                Debug.Log(goObj.name + " hide flags: " + goObj.hideFlags);
                goObj.hideFlags = HideFlags.DontSaveInEditor;
            }
        }
    }

    public void HandleClientLocalConnected() {
        connectionBar.SetConnectionStatus(ConnectionStatus.Connected);
        if (CurrentAppMode == AppMode.MobileTopDown || CurrentAppMode == AppMode.UgCS) {
            connectionScreen.OnConnected();
            connectionScreen.OpenConnectionScreen(false);
        }
    }

    public void HandleHandshakeDone() {
        WebSocketClient.Instance.SendDroneListRequest();
        connectionBar.SetConnectionStatus(ConnectionStatus.Connected);
    }

    public void HandleConnectionFailed() {
        connectionBar.SetConnectionStatus(ConnectionStatus.Disconnected);
        if (CurrentAppMode == AppMode.MobileTopDown || CurrentAppMode == AppMode.UgCS) {
            connectionScreen.OnDisconnected();
            //connectionScreen.OpenConnectionScreen(true);
            DroneManager.Instance.DestroyDroneAll();
        }
    }

    public void RestartServer() {
        CloseServerMode();
        CameraManager.Instance.SetCameraFPV(false);
        StartServerMode();
    }

    public void CenterMap(DroneFlightData flightData) {
        if (!mapCentered && MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS) {
            mapCentered = true;
            firstDroneFlightData = flightData;

            //DestroyCurrentArcGISMap();

            //Scene3DViewArcGISMap = Scene3DView.AddComponent<ArcGISMapComponent>();

            // Set scene view map and load all possible 3d structures and layers
            Scene3DViewArcGISMap.OriginPosition = new ArcGISPoint(firstDroneFlightData.gps.longitude, firstDroneFlightData.gps.latitude, firstDroneFlightData.altitude, new ArcGISSpatialReference(4326));
            Scene3DViewArcGISMap.MapType = ArcGISMapType.Global;
            Scene3DViewArcGISMap.MapTypeChanged += new ArcGISMapComponent.MapTypeChangedEventHandler(CreateArcGISMap);
            CreateArcGISMap();

            // Set 2d minimap and center it to the position of the first drone
            Map2DViewArcGISMap.OriginPosition = new ArcGISPoint(firstDroneFlightData.gps.longitude, firstDroneFlightData.gps.latitude, 0, new ArcGISSpatialReference(4326));
            Map2DViewArcGISMap.MapType = ArcGISMapType.Global;

            // Center the main camera
            ArcGISLocationComponent cameraLocationComponent = MainCamera.GetComponent<ArcGISLocationComponent>();
            cameraLocationComponent.enabled = true;
            cameraLocationComponent.Position = new ArcGISPoint(firstDroneFlightData.gps.longitude, firstDroneFlightData.gps.latitude, firstDroneFlightData.altitude + 50, new ArcGISSpatialReference(4326));
            //cameraLocationComponent.Rotation = new ArcGISRotation(65, 68, 0);

            // Center the minimap camera
            ArcGISLocationComponent minimapCameraLocationComponent = MinimapCamera.GetComponent<ArcGISLocationComponent>();
            minimapCameraLocationComponent.enabled = true;
            minimapCameraLocationComponent.Position = new ArcGISPoint(firstDroneFlightData.gps.longitude, firstDroneFlightData.gps.latitude, 500, new ArcGISSpatialReference(4326));

        }
    }


    private void DestroyCurrentArcGISMap() {
        Scene3DViewArcGISMap.enabled = false;
        DestroyImmediate(Scene3DViewArcGISMap);
        //DestroyImmediate(Scene3DViewArcGISMap.GetComponentInChildren<ArcGISRendererComponent>().gameObject);
    }


    public void CreateArcGISMap() {
        Debug.Log("CREATE ARCGIS MAP");

        ArcGISMap arcGISm = Scene3DViewArcGISMap.View.Map;

        // Check whether the drone is flying in some known location
        MapManager.Location knownLocation = MapManager.Instance.GetKnownLocation(firstDroneFlightData.gps);

        // Load Scene Layer data for the known location
        foreach (string layerData in MapManager.Instance.Get3DObjectSceneLayerData()) {
            ArcGISLayer layer = IsLayerInMap(layerData);
            if (layer != null) {
                layer.IsVisible = true;
            } else {
                arcGISm.Layers.Add(new Esri.GameEngine.Layers.ArcGIS3DObjectSceneLayer(layerData, knownLocation + "_3DObjectSceneLayer", 1.0f, true, ""));
            }
        }

        // If location has some specific or more detailed elevation layer, add it on top of the base layer
        foreach (string elevationData in MapManager.Instance.GetElevationData()) {
            ArcGISElevationSource elevationSource = IsElevationSourceInMap(elevationData);
            if (elevationSource != null) {
                elevationSource.IsEnabled = true;
            } else {
                arcGISm.Elevation.ElevationSources.Add(new Esri.GameEngine.Elevation.ArcGISImageElevationSource(elevationData, knownLocation + "_elevation", ""));
            }
        }

        Scene3DViewArcGISMap.View.Map = arcGISm;
    }

    private ArcGISLayer IsLayerInMap(string layerData) {
        ArcGISMap map = Scene3DViewArcGISMap.View.Map;        
        int i_max = (int)map.Layers.GetSize();
        for (int i = 0; i < i_max; i++) {
            ArcGISLayer layer = map.Layers.At((ulong) i);

            if (layer.Source.Equals(layerData)) {
                return layer;
            }
        }
        return null;
    }

    private ArcGISElevationSource IsElevationSourceInMap(string elevationData) {
        ArcGISMap map = Scene3DViewArcGISMap.View.Map;
        int i_max = (int) map.Elevation.ElevationSources.GetSize();
        for (int i = 0; i < i_max; i++) {
            ArcGISElevationSource elevationSource = map.Elevation.ElevationSources.At((ulong) i);

            if (elevationSource.Source.Equals(elevationData)) {
                return elevationSource;
            }
        }
        return null;
    }

}

