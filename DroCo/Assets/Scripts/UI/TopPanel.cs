using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TopPanel : MonoBehaviour {

    [SerializeField]
    private TMP_Text altitudeText;

    [SerializeField]
    private TMP_Text speedText;

    public void SetAltitudeText(string text) {
        altitudeText.text = text + " m";
    }

    public void SetSpeedText(string text) {
        speedText.text = text + " m/s";
    }
}
