using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConnectionScreenTopDownMode : ConnectionScreen {

    public Image ConnectionStatusImage;

    public override void OnIPConnect() {
        GameManager.Instance.StartClientNoVideoMode(IPInputField.text);
    }

    public override void OnConnected() {
        ConnectionStatusImage.color = Color.green;
    }

    public override void OnDisconnected() {
        ConnectionStatusImage.color = Color.red;
    }
}
