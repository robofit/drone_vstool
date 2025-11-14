using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ListItemButton : MonoBehaviour {

    [SerializeField]
    private TMP_Text UnitIDText;
    [SerializeField]
    private TMP_Text UnitTypeText;
    [SerializeField]
    private TMP_Text DistanceText;
    [SerializeField]
    private TMP_Text AltitudeText;
    [SerializeField]
    private GameObject CameraView;
    [SerializeField]
    private RawImage CameraViewImage;
    [SerializeField]
    private TMP_Text DelayText;

    private InteractiveObject interactiveObject;
    private Button button;
    private bool buttonSelected = false;

    private float lastClick = 0f;
    private float doubleClickInterval = 0.4f;



    private void Awake() {
        CameraView.SetActive(false);
        button = GetComponent<Button>();
    }

    public void OnClick() {

    }

    public void OnPointerEnter() {
        if (!buttonSelected) {
            CameraView.SetActive(true);
            interactiveObject.Highlight(true);
        }
    }

    public void OnPointerExit() {
        if (!buttonSelected) {
            CameraView.SetActive(false);
            interactiveObject.Highlight(false);
        }
    }

    public void OnPointerClick() {
        if ((lastClick + doubleClickInterval) > Time.time) {
            interactiveObject.FocusCamera();
            OnDeselect();
        }
        lastClick = Time.time;
    }

    public void OnSelect() {
        buttonSelected = true;
        CameraView.SetActive(true);
        interactiveObject.Highlight(true);
        interactiveObject.SetCameras();
    }

    public void OnDeselect() {
        buttonSelected = false;
        CameraView.SetActive(false);
        interactiveObject.Highlight(false);
    }

    public void InitUnitData(string unitID, string unitType, InteractiveObject intObject) {
        UnitIDText.text = unitID;
        UnitTypeText.text = unitType;
        interactiveObject = intObject;
    }

    public void UpdateHeight(double height) {
        AltitudeText.text = "H:" + height.ToString() + "m";
    }

    public void UpdateDistance(float distance) {
        DistanceText.text = "D:" + distance.ToString() + "m";
    }

    public void InitCameraViewTexture(RenderTexture texture) {
        CameraViewImage.texture = texture;
    }

}
