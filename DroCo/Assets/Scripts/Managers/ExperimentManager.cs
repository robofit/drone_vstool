using System;
using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

[RequireComponent(typeof(ExperimentSettings))]
public class ExperimentManager : Singleton<ExperimentManager> {

    public enum RemoteControllerMode {
        None,
        NoDirection,
        UpDown,
        AllDirections
    }

    public enum StickConfiguration {
        LEFT,
        RIGHT,
        UP,
        DOWN,
        CENTER,
        NONE
    }


    public enum AppMode {
        DesktopUgCS,
        TabletARView,
        MobileTopdownView,
        DesktopUgCSMockup,
        TabletARViewMockup
    }

    public ExperimentSettings ExperimentSettings;

    public GameObject PlaceholderBackground;
    public UnityTransport UnityTransport;
    public NetworkRPCSync NetworkRPC;

    public GameObject ConnectionScreen, LoadingScreen;

    public VideoPlayerControls VideoPlayerControls;

    public event Action OnClientConnectedToServer;

    public TopPanel TopPanel;

    //public WebSocketServerExperiment WebSocketServer;
    //public WebSocketClientExperiment WebSocketClient;

    private Coroutine connectionTimeoutCoroutine;

    private bool fpvSet = false;

    public UnityEngine.UI.Image RemoteControllerConnectionImage;
    public TMP_Text LeftStickText;
    public TMP_Text RightStickText;
    public TMP_Text LeftStickCommandText;
    public TMP_Text RightStickCommandText;

    private StickConfiguration leftStickConfiguration;
    private StickConfiguration rightStickConfiguration;
    private RemoteController lastRemoteControllerData;

    private RemoteControllerMode currentRemoteControllerMode = RemoteControllerMode.NoDirection;


    public Camera MainDroneCamera;
    public Camera ARCamera;

    private Shader buildingShader;
    private Shader buildingShaderLit;

    //public Toggle PCMission1;
    //public Toggle PCMission2;
    //public Toggle PCMissionT;


    //public Toggle ARMission1;
    //public Toggle ARMission2;
    //public Toggle ARMissionT;


    //public Toggle MobMission1;
    //public Toggle MobMission2;
    //public Toggle MobMissionT;


    private void Start() {
        if (ExperimentSettings == null) {
            ExperimentSettings = GetComponent<ExperimentSettings>();
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;


        if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCS) {
            StartHost();
            WebSocketServerExperiment.Instance.StartServer();
            //SetNewStickConfiguration();
            leftStickConfiguration = StickConfiguration.CENTER;
            rightStickConfiguration = StickConfiguration.CENTER;
            LeftStickCommandText.text = leftStickConfiguration.ToString();
            RightStickCommandText.text = rightStickConfiguration.ToString();
            lastRemoteControllerData = new RemoteController { client_id = "123", left_stick = new Stick { x = 0, y = 0 }, right_stick = new Stick { x = 0, y = 0 } };
            HandleReceivedRemoteControllerData(lastRemoteControllerData);
        } else if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCSMockup) {
            StartHost();
            WebSocketServerExperiment.Instance.StartServer();
        }

        buildingShader = UnityEngine.Shader.Find("Custom/MobileOcclusion");
        buildingShaderLit = UnityEngine.Shader.Find("Shader Graphs/SceneNodeSurface");
    }

    //public void StartHost() {
    //    WebSocketServerExperiment.Instance.StartServer();
    //}

    //public void StartClient(string ipAddress) {
    //    WebSocketClientExperiment.Instance.ConnectToServer(ipAddress, 5558);
    //}

    //public void HandleReceivedLogData(int sequenceNumber, DroneFlightData data) {
    //    OnPlayFlightLog();
    //    DroneManager.Instance.HandleReceivedDroneData(data);
    //}

    //public void HandleReceivedDroneData(DroneStaticData data) {
    //    DroneManager.Instance.AddDrone(data);
    //}

    //public DroneStaticData GetExperimentDroneData() {
    //    string logLine = FlightLogPlayerManager.Instance.GetLogMessage(0);
    //    DroneFlightData flightData = JsonUtility.FromJson<DroneFlightData>(logLine);

    //    DroneStaticData drone = new DroneStaticData {
    //        client_id = flightData.client_id,
    //        drone_name = "experiment_test_drone",
    //        serial = "experiment_serial"
    //    };

    //    return drone;
    //}

    //public void HandleClientDisconnected() {

    //}

    //public void OnClientConnected() {
    //    Debug.Log("Client successfully connected to the server!");
    //    ConnectionScreen.SetActive(false);
    //}





    public void StartHost() {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient(string ipAddress) {
        UnityTransport.ConnectionData.Address = ipAddress;
        LoadingScreen.SetActive(true);
        NetworkManager.Singleton.StartClient();
        connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeout(5f));
    }

    private IEnumerator ConnectionTimeout(float timeoutDuration) {
        float timer = 0f;

        while (timer < timeoutDuration) {
            // If the client is connected, cancel the timeout
            if (NetworkManager.Singleton.IsConnectedClient) {

                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Timeout has expired, stop the client
        Debug.LogWarning("Timeout! Failed to connect the client to the server " + UnityTransport.ConnectionData.Address);
        //NetworkManager.Singleton.Shutdown();
        LoadingScreen.SetActive(false);
        // Clear the coroutine reference
        connectionTimeoutCoroutine = null;
    }

    private void OnClientConnected(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            Debug.Log("Client successfully connected to the server!");
            // If the timeout coroutine is running, stop it
            if (connectionTimeoutCoroutine != null) {
                StopCoroutine(connectionTimeoutCoroutine);
                connectionTimeoutCoroutine = null;
            }
            ConnectionScreen.SetActive(false);
            //NetworkRPC.RequestVideoPlayerStatusRpc();
            OnClientConnectedToServer?.Invoke();
        } else {
            // Init drone on clients
            //NetworkRPC.SendDroneConnectionMessage(FlightLogPlayerManager.Instance.GetLogMessage(0));
            Debug.Log("Server registered new client connection.");
        }
    }

    // Called on clients only.
    public void UpdateVideoPlayerStatus(bool isPlaying) {
        VideoPlayerControls.UpdateStatus(isPlaying);
    }

    private void OnClientDisconnected(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            Debug.Log("Client disconnected from the server.");
            ConnectionScreen.SetActive(true);
            LoadingScreen.SetActive(false);
        } else {
            Debug.Log($"A client with ID {clientId} disconnected.");
        }
    }

    // Called on clients from server.
    public void OnFrameNumberResponse(int frameNumber) {
        // Do the setting of FPV
        OnPlayFlightLog();

        FlightLogPlayerManager.Instance.PlayLogMessage(frameNumber);
        VideoPlayerControls.UpdateProgressBar(frameNumber);
    }

    private void OnPlayFlightLog() {
        if (ExperimentSettings.CurrentAppMode == AppMode.TabletARView && !fpvSet) {
            PlaceholderBackground.SetActive(false);
            CameraManager.Instance.SetCameraFPV(true);
            CameraManager.Instance.DisplayVRSceneLit(false);
            fpvSet = true;
        }
    }

    public void OnPlayButtonPressed() {
        OnPlayFlightLog();
        NetworkRPC.SendPlayRpc();
    }

    public void OnPauseButtonPressed() {
        NetworkRPC.SendPauseRpc();
    }

    // Called on clients from server.
    public void OnPlayRpcReceived() {
        VideoPlayerControls.OnPlayButton();
    }

    // Called on clients from server.
    public void OnPauseRpcReceived() {
        VideoPlayerControls.OnPauseButton();
    }

    public void SyncVideoPlayerControls(int frameNumber) {
        NetworkRPC.SendFrameNumberRpc(frameNumber);
    }

    public void RequestPlayRpc() {
        NetworkRPC.RequestPlayRpc();
    }

    public void RequestPauseRpc() {
        NetworkRPC.RequestPauseRpc();
    }

    public void RequestSeekRpc(int frameNumber) {
        NetworkRPC.RequestSeekRpc(frameNumber);
    }

    public void OnReceiveRequestPlay() {
        VideoPlayerControls.OnPlayButton();
    }

    public void OnReceiveRequestPause() {
        VideoPlayerControls.OnPauseButton();
    }

    public void OnReceiveRequestSeek(int frame) {
        VideoPlayerControls.UpdateProgressBar(frame);
    }

    //public void SendFlightLogMessage(string message, int frameNumber) {
    //    NetworkRPC.SendFlightLogMessage(message);

    //    //WebSocketServerExperiment.Instance.SendMessageToAllClients("{\"type\":\"data_broadcast\", \"sequence_number\":" + frameNumber + ", \"data\":" + message + "}");
    //}


    public bool IsLogPlaying() {
        return VideoPlayerControls.IsPlaying;
    }

    private void OnDisable() {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public void SetNewStickConfiguration() {
        leftStickConfiguration = (StickConfiguration) UnityEngine.Random.Range(0, 4);
        rightStickConfiguration = (StickConfiguration) UnityEngine.Random.Range(0, 4);
        LeftStickCommandText.text = leftStickConfiguration.ToString();
        RightStickCommandText.text = rightStickConfiguration.ToString();

        // Invalidate current stick configuration with zero data
        HandleReceivedRemoteControllerData(new RemoteController() {
            client_id = "", left_stick = new Stick() { x = 0, y = 0 }, right_stick = new Stick() { x = 0, y = 0 }
        });
    }

    public StickConfiguration ComputeLeftStickConfiguration(DroneFlightData data) {
        // velocity Z  -Z (nahoru), 0 (stoji), Z (dolu)
        // yaw         -X (tocim se doleva), 0 (stoji), X (tocim se doprava)
        if (data.aircraft_velocity.velocity_z < 0) {
            return StickConfiguration.UP;
        } else if (data.aircraft_velocity.velocity_z > 0) {
            return StickConfiguration.DOWN;
        } else {
            return StickConfiguration.CENTER;
        }
    }

    public StickConfiguration ComputeRightStickConfiguration(DroneFlightData data) {
        // velocity X  -X (dopredu), 0 (stoji), X (dozadu)
        // velocity Y  -Y (doprava), 0 (stoji), Y (doleva)

        if (data.aircraft_velocity.velocity_x > data.aircraft_velocity.velocity_y) {
            if (data.aircraft_velocity.velocity_x < 0) {
                return StickConfiguration.UP;
            } else if (data.aircraft_velocity.velocity_x > 0) {
                return StickConfiguration.DOWN;
            } else {
                return StickConfiguration.CENTER;
            }
        } else {
            if (data.aircraft_velocity.velocity_y < 0) {
                return StickConfiguration.RIGHT;
            } else if (data.aircraft_velocity.velocity_y > 0) {
                return StickConfiguration.LEFT;
            } else {
                return StickConfiguration.CENTER;
            }
        }
    }

    public void SetNewStickConfiguration(Sticks sticks, DroneFlightData[] nextLogs = null) {
        // Use only in AllDirections mode
        if (currentRemoteControllerMode == RemoteControllerMode.AllDirections) {
            leftStickConfiguration = GetStickConfiguration(sticks.left_stick);
            rightStickConfiguration = GetStickConfiguration(sticks.right_stick);

            LeftStickCommandText.text = leftStickConfiguration.ToString();
            RightStickCommandText.text = rightStickConfiguration.ToString();

            // Reevaluate current controller data
            HandleReceivedRemoteControllerData(lastRemoteControllerData);
        }
    }

    public void HandleReceivedRemoteControllerData(RemoteController data) {
        if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCS) {
            switch (currentRemoteControllerMode) {
                case RemoteControllerMode.NoDirection:

                    break;
                case RemoteControllerMode.UpDown:
                    StickConfiguration left = GetStickConfiguration(data.left_stick);
                    StickConfiguration right = GetStickConfiguration(data.right_stick);
                    // play forward
                    if (left == StickConfiguration.UP && right == StickConfiguration.UP) {
                        if (!VideoPlayerControls.IsPlaying) {
                            VideoPlayerControls.OnPlayButton();
                        }
                    }
                    // play backward
                    else if (left == StickConfiguration.DOWN && right == StickConfiguration.DOWN) {
                        if (!VideoPlayerControls.IsPlayingBackward) {
                            VideoPlayerControls.OnPlayBackward();
                        }
                    } else {
                        VideoPlayerControls.OnPauseButton();
                    }
                    break;
                case RemoteControllerMode.AllDirections:
                    if (GetStickConfiguration(data.left_stick) == leftStickConfiguration && GetStickConfiguration(data.right_stick) == rightStickConfiguration) {
                        if (!VideoPlayerControls.IsPlaying) {
                            VideoPlayerControls.OnPlayButton();
                        }
                    } else {
                        VideoPlayerControls.OnPauseButton();
                    }
                    break;
            }

            LeftStickText.text = data.left_stick.ToString();
            RightStickText.text = data.right_stick.ToString();

            lastRemoteControllerData = data;
        } else if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCSMockup) {
            DroneController.Instance.HandleReceivedRemoteControllerData(data);
        }
    }

    private StickConfiguration GetStickConfiguration(Stick stick) {
        if (Math.Abs(stick.x) > Math.Abs(stick.y)) {
            if (stick.x >= 0) {
                return StickConfiguration.RIGHT;
            } else if (stick.x <= -50) {
                return StickConfiguration.LEFT;
            }
        } else {
            if (stick.y >= 50) {
                return StickConfiguration.UP;
            } else if (stick.y <= -50) {
                return StickConfiguration.DOWN;
            }
        }

        if (stick.x < 50 && stick.x > -50 && stick.y < 50 && stick.y > -50) {
            return StickConfiguration.CENTER;
        }

        return StickConfiguration.CENTER;
    }

    public void OnRemoteControllerConnected() {
        RemoteControllerConnectionImage.color = Color.green;
    }

    public void OnRemoteControllerDisconnected() {
        RemoteControllerConnectionImage.color = Color.red;
    }

    public void LoadMission1(bool active) {
        //MissionManager.Instance.DisplayMission1(active);
        FlightLogPlayerManager.Instance.LoadFlightLog("mission1_flight.txt");
        if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCS) {
            VideoFramePlayer.Instance.LoadMission1();
        }

        //switch (ExperimentSettings.CurrentAppMode) {
        //    case AppMode.DesktopUgCS:
        //        PCMission1.isOn = true;
        //        break;
        //    case AppMode.MobileTopdownView:
        //        MobMission1.isOn = true;
        //        break;
        //    case AppMode.TabletARView:
        //        ARMission1.isOn = true;
        //        break;
        //}
    }

    public void LoadMission2(bool active) {
        //MissionManager.Instance.DisplayMission2(active);
        FlightLogPlayerManager.Instance.LoadFlightLog("mission2_flight.txt");
        if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCS) {
            VideoFramePlayer.Instance.LoadMission2();
        }

        //switch (ExperimentSettings.CurrentAppMode) {
        //    case AppMode.DesktopUgCS:
        //        PCMission2.isOn = true;
        //        break;
        //    case AppMode.MobileTopdownView:
        //        MobMission2.isOn = true;
        //        break;
        //    case AppMode.TabletARView:
        //        ARMission2.isOn = true;
        //        break;
        //}
    }

    public void LoadMissionTraining(bool active) {
        FlightLogPlayerManager.Instance.LoadFlightLog("mission_training.txt");
        if (ExperimentSettings.CurrentAppMode == AppMode.DesktopUgCS) {
            VideoFramePlayer.Instance.LoadMissionTraining();
        }

        //switch (ExperimentSettings.CurrentAppMode) {
        //    case AppMode.DesktopUgCS:
        //        PCMissionT.isOn = true;
        //        break;
        //    case AppMode.MobileTopdownView:
        //        MobMissionT.isOn = true;
        //        break;
        //    case AppMode.TabletARView:
        //        ARMissionT.isOn = true;
        //        break;
        //}
    }

    public void ChangeRemoteControllerMode(TMP_Text text) {
        // Load next configuration
        currentRemoteControllerMode = GetNextMode(currentRemoteControllerMode);

        switch (currentRemoteControllerMode) {

            // change the configuration to NoDirection (video will play on button click in desktop app)
            case RemoteControllerMode.NoDirection:
                text.text = "X";
                break;

            // change the configuration to UpDown mode (video will play only if both sticks are held up (down for backward))
            case RemoteControllerMode.UpDown:
                text.text = "M1";
                break;

            // change the configuration to AllDirections mode (video will play as the real drone was flying)
            case RemoteControllerMode.AllDirections:
                text.text = "M2";
                break;
        }
    }

    private RemoteControllerMode GetNextMode(RemoteControllerMode mode) {
        switch (currentRemoteControllerMode) {
            case RemoteControllerMode.NoDirection:
                return RemoteControllerMode.UpDown;
            case RemoteControllerMode.UpDown:
                return RemoteControllerMode.AllDirections;
            case RemoteControllerMode.AllDirections:
                return RemoteControllerMode.NoDirection;
            default:
                return RemoteControllerMode.None;
        }
    }

    public void EnableOclussionsMockup(bool enable) {
        if (enable) {
            ARCamera.enabled = false;
            MainDroneCamera.cullingMask = ~0;            
        } else {
            ARCamera.enabled = true;
            MainDroneCamera.cullingMask = ~LayerMask.GetMask("Mission");
        }

        GameObject[] buildings = FindGameObjectsInLayer(15);
        if (buildings != null) {
            if (enable) {
                foreach (GameObject building in buildings) {
                    building.GetComponentInChildren<MeshRenderer>().material.shader = buildingShaderLit;
                    //building.GetComponent<Collider>().enabled = true;
                }
            } else {
                foreach (GameObject building in buildings) {
                    building.GetComponentInChildren<MeshRenderer>().material.shader = buildingShader;
                    //building.GetComponent<Collider>().enabled = false;
                }
            }
        }
    }

    private GameObject[] FindGameObjectsInLayer(int layer) {
        var goArray = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        var goList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < goArray.Length; i++) {
            if (goArray[i].layer == layer) {
                goList.Add(goArray[i]);
            }
        }
        if (goList.Count == 0) {
            return null;
        }
        return goList.ToArray();
    }


    public void SendDroneGPSFlightData(double3 latitudeLongitudeHeight, quaternion rotation, float gimbal) {
        NetworkRPC.SendDroneGPSFlightDataRpc(latitudeLongitudeHeight.x, latitudeLongitudeHeight.y, latitudeLongitudeHeight.z, rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w, gimbal);
    }

    public void SendDroneFlightData(Vector3 movement, float yaw, float vertical, float gimbal) {
        NetworkRPC.SendDroneFlightDataRpc(movement, yaw, vertical, gimbal);
    }

}
