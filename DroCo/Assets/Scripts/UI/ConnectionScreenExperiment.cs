using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConnectionScreenExperiment : ConnectionScreen {


    public override void OnIPConnect() {
        ExperimentManager.Instance.StartClient(IPInputField.text);
    }

    public override void OnConnected() {

    }

    public override void OnDisconnected() {

    }
}
