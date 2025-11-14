using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PimDeWitte.UnityMainThreadDispatcher;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;


public class ExperimentServerBehavior : WebSocketBehavior {

    [Serializable]
    private class Message<T> {
        public string type;
        public T data;
    }

    [Serializable]
    private class SeekRequest {
        public string type;
        public int sequence_number;
    }

    protected override void OnOpen() {
        base.OnOpen();
        Debug.Log("Connection open");
        UnityMainThreadDispatcher.Instance().Enqueue(HandleClientConnected());
    }

    protected override void OnMessage(MessageEventArgs e) {
        base.OnMessage(e);

        //Debug.Log(e.Data);
        Message<string> msg = JsonUtility.FromJson<Message<string>>(e.Data);
        if (msg.type == "seek") {
            
            SeekRequest seekMsg = JsonUtility.FromJson<SeekRequest>(e.Data);

            //UnityMainThreadDispatcher.Instance().Enqueue(UpdateDroneFlightData(dfd.data));
        } else {
            Debug.LogError("Unknown data received! " + e.Data);
        }
    }

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close: " + e.ToString() + " reason: " + e.Reason + ", code: " + e.Code);
        UnityMainThreadDispatcher.Instance().Enqueue(HandleClientDisconnected());

    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error: " + e.Message + " ..exception: " + e.Exception);
    }

    private IEnumerator HandleClientConnected() {
        //DroneStaticData droneConnectionData = ExperimentManager.Instance.GetExperimentDroneData();
        //Send("{\"type\":\"drone_connect\", \"sequence_number\":0, \"data\":" + JsonUtility.ToJson(droneConnectionData) + "}");
        yield return null;
    }

    private IEnumerator HandleClientDisconnected() {
        //ExperimentManager.Instance.HandleClientDisconnected();
        yield return null;
    }

}

public class ExperimentRemoteControllerSticksServerBehavior : WebSocketBehavior {

    [Serializable]
    private class Message<T> {
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

        //Debug.Log(e.Data);
        Message<string> msg = JsonUtility.FromJson<Message<string>>(e.Data);
        if (msg.type == "hello") {
            DoHandshake(ID, JsonUtility.FromJson<Message<Hello>>(e.Data));
        } else if (handshake_done && msg.type == "remote_controller") {

            Message<RemoteController> dfd = JsonUtility.FromJson<Message<RemoteController>>(e.Data);

            UnityMainThreadDispatcher.Instance().Enqueue(UpdateRemoteControllerData(dfd.data));
        } else {
            Debug.LogError("Unknown data received! " + e.Data);
        }
    }

    protected override void OnClose(CloseEventArgs e) {
        base.OnClose(e);
        Debug.Log("Connection close: " + e.Reason);
        UnityMainThreadDispatcher.Instance().Enqueue(HandleRemoteControllerDisconnected());

    }

    protected override void OnError(ErrorEventArgs e) {
        base.OnError(e);
        Debug.Log("Connection error: " + e.Message + " ..exception: " + e.Exception);
        UnityMainThreadDispatcher.Instance().Enqueue(HandleRemoteControllerDisconnected());
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

        UnityMainThreadDispatcher.Instance().Enqueue(HandleRemoteControllerConnected());

        handshake_done = true;
    }

    private IEnumerator HandleRemoteControllerConnected() {
        ExperimentManager.Instance.OnRemoteControllerConnected();
        yield return null;
    }

    private IEnumerator HandleRemoteControllerDisconnected() {
        ExperimentManager.Instance.OnRemoteControllerDisconnected();
        yield return null;
    }

    private IEnumerator UpdateRemoteControllerData(RemoteController data) {
        ExperimentManager.Instance.HandleReceivedRemoteControllerData(data);
        yield return null;
    }

}


public class WebSocketServerExperiment : Singleton<WebSocketServerExperiment> {

    public string Address;
    public string Port;

    private WebSocketSharp.Server.WebSocketServer Server;

    private string clientID;

    public void StartServer() {
        Debug.Log("Starting server on port " + Port);

        try {
            Server = new WebSocketSharp.Server.WebSocketServer("ws://" + Address + ":" + Port);
            Server.AddWebSocketService<ExperimentRemoteControllerSticksServerBehavior>("/");

            Server.Start();

            Debug.Log("Server started on " + Address + " and port " + Port);

        } catch (Exception ex) {
            Debug.LogError(ex.Message);
            Debug.LogError(ex.InnerException);
            if (Server != null) {
                Server.Stop();
                Server = null;
            }

            Port = (int.Parse(Port) + 1).ToString();
            StartServer();
            return;
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

    public void SendMessageToAllClients(string message) {
        WebSocketSessionManager session = Server.WebSocketServices["/"].Sessions;
        foreach (string clientID in session.IDs) {
            session.SendTo(message, clientID);
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
