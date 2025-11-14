using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;

public class WebSocketClientExperiment : Singleton<WebSocketClientExperiment> {

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
        public int sequence_number;
        public T data;
    }


    [Serializable]
    private class SeekRequest {
        public string type;
        public int sequence_number;
    }

    private void Update() {
        if (websocket != null && websocket.State == WebSocketState.Open)
            websocket.DispatchMessageQueue();
    }

    public async void ConnectToServer(string domain, int port) {
        Debug.Log("Starting client");
        ClosePreviousConnection();

        try {
            APIDomainWS = GetWSURI(domain, port);
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

    public void SendSeekNumber(int seekNumber) {
        SeekRequest request = new SeekRequest();
        request.type = "seek";
        request.sequence_number = seekNumber;
        SendToServer(request.ToString());
    }


    private void HandleReceivedData(byte[] message) {
        string msgstr = Encoding.Default.GetString(message);

        Debug.Log("Received data from server: " + msgstr);

        Response<string> msg = JsonUtility.FromJson<Response<string>>(msgstr);

        if (msg.type == "data_broadcast") {
            Response<DroneFlightData> resp = JsonUtility.FromJson<Response<DroneFlightData>>(msgstr);
            //ExperimentManager.Instance.HandleReceivedLogData(msg.sequence_number, resp.data);
        } else if (msg.type == "drone_connect") {
            Response<DroneStaticData> resp = JsonUtility.FromJson<Response<DroneStaticData>>(msgstr);
            //ExperimentManager.Instance.HandleReceivedDroneData(resp.data);
        }
    }

    private void OnClose(WebSocketCloseCode closeCode) {
        Debug.Log("Connection closed! " + closeCode.ToString() + " code: " + closeCode);
    }

    private void OnError(string errorMsg) {
        Debug.LogError(errorMsg);
    }

    private void OnConnected() {
        //ExperimentManager.Instance.OnClientConnected();
    }

    /// <summary>
    /// Create websocket URI from domain name and port
    /// </summary>
    /// <param name="domain">Domain name or IP address</param>
    /// <param name="port">Server port</param>
    /// <returns></returns>
    public string GetWSURI(string domain, int port) {
        return "ws://" + domain + ":" + port.ToString();
    }

    private async void OnApplicationQuit() {
        await websocket?.Close();
    }

    private async void OnDestroy() {
        await websocket?.Close();
    }
}
