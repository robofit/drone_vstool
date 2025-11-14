using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Net.NetworkInformation;
using Unity.VisualScripting;

public class TestBehavior : WebSocketBehavior {
    protected override void OnOpen() {
        base.OnOpen();
        Debug.Log("Connection open");
    }

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close: " + e.Reason);
    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error: " + e.Message + " ..exception: " + e.Exception);
    }

    protected override void OnMessage(MessageEventArgs e) {
        base.OnMessage(e);
        Debug.Log(e.Data);

        try {
            Send("hello response");
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        }
    }
}

public class FlightDataForwardBehavior : WebSocketBehavior {
    protected override void OnOpen() {
        base.OnOpen();
        Debug.Log("Connection open on /flightData");
    }

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close on /flightData: " + e.Reason);
    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error on /flightData: " + e.Message + " ..exception: " + e.Exception);
    }

    protected override void OnMessage(MessageEventArgs e) {
        base.OnMessage(e);
        Debug.Log(e.Data);

        try {
            Send("hello response");
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        }
    }
}

public class FlightDataNoVideoForwardBehavior : WebSocketBehavior {
    protected override void OnOpen() {
        base.OnOpen();
        Debug.Log("Connection open on /flightDataNoVideo");
    }

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close on /flightDataNoVideo: " + e.Reason);
    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error on /flightDataNoVideo: " + e.Message + " ..exception: " + e.Exception);
    }

    protected override void OnMessage(MessageEventArgs e) {
        base.OnMessage(e);
        Debug.Log(e.Data);

        try {
            Send("hello response");
        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        }
    }
}



public class WebSocketServerBehavior : WebSocketBehavior {

    [Serializable]
    public class Message<T> {
        public string type;
        public T data;
    }

    [Serializable]
    private class Hello {
        public string ctype;
        public string drone_name;
        public string serial;
    }

    [Serializable]
    private class HelloResponse {
        public string client_id;
        public string rtmp_port;
    }

    private bool handshake_done = false;
    

    protected override void OnOpen() {
        base.OnOpen();
        Debug.Log("Connection open");
    }

    protected override void OnMessage(MessageEventArgs e) {
        base.OnMessage(e);

        if (e.IsBinary) {
            HandleBinaryMessage(e.RawData);
        } else if (e.IsText) {
            HandleTextMessage(e.Data);
        } else {
            Debug.LogError("Unknown WebSocket message format!");
        }
    }

    private void HandleTextMessage(string data) {
        //Debug.Log("Received Text Message, size " + data.Length);

        Message<string> msg = JsonUtility.FromJson<Message<string>>(data);
        if (msg.type == "hello") {
            DoHandshake(ID, JsonUtility.FromJson<Message<Hello>>(data));
        } else {
            Debug.LogError("Unexpected text message type: " + msg.type);
        }
    }

    private void HandleBinaryMessage(byte[] data) {
        //Debug.Log($"Received Binary Message, size: {data.Length}");

        if (!handshake_done) {
            Debug.LogWarning("Binary message received before handshake done, ignoring.");
            return;
        }

        // 1. First 4 bytes = JSON length
        int jsonLength = System.BitConverter.ToInt32(data, 0);
        jsonLength = System.Net.IPAddress.NetworkToHostOrder(jsonLength); // convert big-endian to little-endian

        if (data.Length < 4 + jsonLength) {
            Debug.LogError("Invalid binary message: JSON length larger than payload.");
            return;
        }

        // 2. Extract JSON bytes
        byte[] jsonBytes = new byte[jsonLength];
        System.Buffer.BlockCopy(data, 4, jsonBytes, 0, jsonLength);

        string jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        //Debug.Log("Received JSON: " + jsonString);

        // 3. Parse DroneFlightData
        Message<DroneFlightData> dfd = JsonUtility.FromJson<Message<DroneFlightData>>(jsonString);
        if (dfd == null) {
            Debug.LogError("Failed to parse DroneFlightData!");
            return;
        }

        // 4.Extract JPEG image bytes(if any)
        int jpegStart = 4 + jsonLength + 4;
        int jpegLength = data.Length - jpegStart;

        if (jpegLength > 0) {
            byte[] jpegBytes = new byte[jpegLength];
            System.Buffer.BlockCopy(data, jpegStart, jpegBytes, 0, jpegLength);
            dfd.data.frame = jpegBytes;
        } else {
            Debug.LogWarning("No JPEG image found in binary message.");
        }

        DroneDataPoller.Instance?.PushLatestData(dfd.data);
    }

    //protected override void OnMessage(MessageEventArgs e) {
    //    base.OnMessage(e);

    //    //Debug.Log(e.Data);
    //    Message<string> msg = JsonUtility.FromJson<Message<string>>(e.Data);
    //    if (msg.type == "hello") {
    //        DoHandshake(ID, JsonUtility.FromJson<Message<Hello>>(e.Data));
    //    } else if (handshake_done && msg.type == "data_broadcast") {

    //        Message<DroneFlightData> dfd = JsonUtility.FromJson<Message<DroneFlightData>>(e.Data);

    //        DroneDataPoller.Instance?.PushLatestData(dfd.data);
    //        //UnityMainThreadDispatcher.Instance().Enqueue(UpdateDroneFlightData(dfd.data));
    //    } else {
    //        Debug.LogError("Unknown data received! " + e.Data);
    //    }
    //}

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close: " + e.Reason);
        UnityMainThreadDispatcher.Instance().Enqueue(HandleClientDisconnected());

    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error: " + e.Message + " ..exception: " + e.Exception);
    }

    private void DoHandshake(string clientID, Message<Hello> droneData) {
        Message<HelloResponse> helloResponse = new Message<HelloResponse> {
            data = new HelloResponse()
        };
        helloResponse.type = "hello_resp";
        helloResponse.data.client_id = clientID;
        helloResponse.data.rtmp_port = "1935";

        string msg = JsonUtility.ToJson(helloResponse);
        Debug.Log("Sending:" + msg);

        Send(msg);

        handshake_done = true;

        DroneStaticData newDrone = new DroneStaticData {
            client_id = clientID,
            drone_name = droneData.data.drone_name,
            serial = droneData.data.serial
        };

        UnityMainThreadDispatcher.Instance().Enqueue(HandleClientConnected());
        UnityMainThreadDispatcher.Instance().Enqueue(AddDrone(newDrone));
    }

    private IEnumerator HandleClientConnected() {
        GameManager.Instance.HandleClientConnected();
        yield return null;
    }

    private IEnumerator HandleClientDisconnected() {
        GameManager.Instance.HandleClientDisconnected();
        yield return null;
    }

    private IEnumerator AddDrone(DroneStaticData newDrone) {
        DroneManager.Instance.AddDrone(newDrone);
        yield return null;
    }

    private IEnumerator UpdateDroneFlightData(DroneFlightData flightData) {
        DroneManager.Instance.HandleReceivedDroneDataAndForward(flightData);
        yield return null;
    }
}

public class WebSocketServer : Singleton<WebSocketServer> {

    public string Address;
    public string Port;

    private WebSocketSharp.Server.WebSocketServer Server;

    private string clientID;

    public void StartServer() {
        Debug.Log("Starting server");

        try {
            Server = new WebSocketSharp.Server.WebSocketServer("ws://" + Address + ":" + Port);
            Server.AddWebSocketService<WebSocketServerBehavior>("/");
            Server.AddWebSocketService<FlightDataForwardBehavior>("/flightData");
            Server.AddWebSocketService<FlightDataNoVideoForwardBehavior>("/flightDataNoVideo");

            //Server.AddWebSocketService<TestBehavior>("/test");

            Server.Start();

            Debug.Log("Server started on " + Address + " and port " + Port);

        } catch (Exception ex) {
            Debug.LogError(ex.Message);
            Port = (int.Parse(Port) + 1).ToString();
            StartServer();
            return;
        }

        GameManager.Instance.HandleServerRunning(GetLocalIPAddress() + ":" + Port);

    }

    private string GetLocalIPAddress() {
        try {
            // Get all network interfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            string ethernetIP = null;
            string wifiIP = null;
            string otherIP = null;

            foreach (NetworkInterface ni in interfaces) {
                // Check if the network interface is up and has IP addresses
                if (ni.OperationalStatus == OperationalStatus.Up) {
                    foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses) {
                        // We're only interested in IPv4 addresses
                        if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork) {
                            // Check if it's Ethernet
                            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                                ethernetIP = ipInfo.Address.ToString();
                            }
                            // Check if it's Wi-Fi
                            else if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) {
                                wifiIP = ipInfo.Address.ToString();
                            }
                            // Store other network interfaces
                            else if (otherIP == null) {
                                otherIP = ipInfo.Address.ToString();
                            }
                        }
                    }
                }
            }

            // Prioritize Ethernet, then Wi-Fi, then others
            if (ethernetIP != null)
                return ethernetIP;
            if (wifiIP != null)
                return wifiIP;
            if (otherIP != null)
                return otherIP;

            throw new System.Exception("No valid network adapters found!");
        } catch (System.Exception e) {
            Debug.LogError("Error retrieving local IP address: " + e.Message);
            return "0.0.0.0";
        }
    }

    public void CloseServer() {
        Debug.Log("Closing server");
        if (Server != null) {
            Server.Stop();
            Server = null;
        }
    }

    private void SendMessageToClient() {
        clientID = Server.WebSocketServices["/test"].Sessions.IDs.First();
        Debug.Log("Sending test hello message to client " + clientID);
        try {
            Server.WebSocketServices["/test"].Sessions.SendTo("test hello message", clientID);
        } catch (Exception e) {
            Debug.LogError(e);
        }

    }

    public void SendFlightDataMessageToClients(string message) {
        foreach (string clientID in Server.WebSocketServices["/flightData"].Sessions.IDs) {
            Server.WebSocketServices["/flightData"].Sessions.SendTo(message, clientID);
        }
    }

    public void SendFlightDataNoVideoMessageToClients(string message) {
        foreach (string clientID in Server.WebSocketServices["/flightDataNoVideo"].Sessions.IDs) {
            Server.WebSocketServices["/flightDataNoVideo"].Sessions.SendTo(message, clientID);
        }
    }

    private void OnApplicationQuit() {
        if (Server != null) {
            Server.Stop();
            Server = null;
        }
    }

    private void OnDestroy() {
        if (Server != null) {
            Server.Stop();
            Server = null;
        }
    }
}
