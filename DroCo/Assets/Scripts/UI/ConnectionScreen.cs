using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class ConnectionScreen : MonoBehaviour {

    public TMP_InputField IPInputField;

    private void Start() {
        IPInputField.text = PlayerPrefs.GetString("server_ip");
    }

    public void SaveIPAddress(string ip) {
        PlayerPrefs.SetString("server_ip", ip);
    }

    public void OpenConnectionScreen(bool active) {
        gameObject.SetActive(active);
    }

    public abstract void OnIPConnect();

    public abstract void OnConnected();

    public abstract void OnDisconnected();
}
