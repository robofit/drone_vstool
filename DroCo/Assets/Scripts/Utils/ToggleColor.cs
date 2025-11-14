using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleColor : MonoBehaviour {

    public Color OnColor;
    public Color OffColor;
    public Image ToggleBackground;

    private Toggle toggle;

    private void Start() {
        toggle = GetComponent<Toggle>();
        ChangeToggleColor(toggle.isOn);
    }

    public void ChangeToggleColor(bool toggleOn) {
        ToggleBackground.color = toggleOn ? OnColor : OffColor;
    }
}
