using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;
using System.Text;

public class WebSocketClientSimpleReceiver : Singleton<WebSocketClientSimpleReceiver> {

    /// <summary>
    /// Drone Server URI
    /// </summary>
    private string APIDomainWS = "";
    /// <summary>
    /// Websocket context
    /// </summary>
    private WebSocket websocket;

    private string ClientID;


    [Serializable]
    private class Response<T> {
        public string type;
        public T data;
    }

    private void Update() {
        if (websocket != null && websocket.State == WebSocketState.Open)
            websocket.DispatchMessageQueue();
    }

    public async void ConnectToServer(string domain, int port, bool requireVideo = true) {
        Debug.Log("Starting client");
        ClosePreviousConnection();

        try {
            APIDomainWS = GetWSURI(domain, port, requireVideo ? "/flightData" : "/flightDataNoVideo");
            Debug.Log($"{APIDomainWS}");
            websocket = new WebSocket(APIDomainWS);

            websocket.OnOpen += OnConnected;
            websocket.OnError += OnError;
            websocket.OnClose += OnClose;
            websocket.OnMessage += HandleReceivedData;

            await websocket.Connect();
        } catch (UriFormatException ex) {
            Debug.LogError(ex);
        }
    }

    public void Disconnect() {
        Debug.Log("Disconnecting client");
        if (websocket != null && websocket.State == WebSocketState.Open) {
            websocket.CancelConnection();
            websocket = null;
        }
    }

    private void ClosePreviousConnection() {
        if (websocket != null && websocket.State == WebSocketState.Open) {
            websocket.CancelConnection();
        }
    }

    public async void SendToServer(string msg) {
        if (websocket != null) {
            try {
                await websocket.SendText(msg);
            } catch (WebSocketException ex) {
                Debug.LogError(ex);
            }
        }
    }


    private void HandleReceivedData(byte[] message) {
        string msgstr = Encoding.Default.GetString(message);

        //Debug.Log("Received data from server: " + msgstr);

        Response<string> msg = JsonUtility.FromJson<Response<string>>(msgstr);

        if (msg.type == "data_broadcast") {
            Response<DroneFlightData> resp = JsonUtility.FromJson<Response<DroneFlightData>>(msgstr);
            DroneManager.Instance.HandleReceivedDroneData(resp.data, initDrone: true);
        } else if (msg.type == "drone_connect") {
            Response<DroneStaticData> resp = JsonUtility.FromJson<Response<DroneStaticData>>(msgstr);
            DroneManager.Instance.AddDrone(resp.data);
        }
    }

    private void OnClose(WebSocketCloseCode closeCode) {
        Debug.Log("Connection closed! " + closeCode.ToString() + " code: " + closeCode);
        GameManager.Instance.HandleConnectionFailed();
    }

    private void OnError(string errorMsg) {
        Debug.LogError(errorMsg);
        GameManager.Instance.HandleConnectionFailed();
    }

    private void OnConnected() {
        Debug.Log("Connection opened");
        GameManager.Instance.HandleClientLocalConnected();
    }

    /// <summary>
    /// Create websocket URI from domain name and port
    /// </summary>
    /// <param name="domain">Domain name or IP address</param>
    /// <param name="port">Server port</param>
    /// <returns></returns>
    public string GetWSURI(string domain, int port, string service) {
        return "ws://" + domain + ":" + port.ToString() + service;
    }

    private async void OnApplicationQuit() {
        await websocket?.Close();
    }

    private async void OnDestroy() {
        await websocket?.Close();
    }
}
