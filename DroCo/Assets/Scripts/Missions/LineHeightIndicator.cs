using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineHeightIndicator : MonoBehaviour {

    [Serializable]
    public enum LineOrientation {
        TopToDown,
        DownToTop
    }

    public Connection ParentConnection;
    private LineRenderer line;

    public float firstWaypointAMSL = 0f;
    public float secondWaypointAMSL = 0f;
    public LineOrientation lineOrientation = LineOrientation.TopToDown;
    public float DroneAMSL = 0f;
    public float FillAmount = 0f;
    private bool lineShouldBeEnabled = false;


    private void Start() {
        line = GetComponent<LineRenderer>();
        //if (ExperimentManager.Instance.ExperimentSettings.CurrentAppMode == ExperimentManager.AppMode.TabletARView) {
            line.enabled = true;
            lineShouldBeEnabled = true;
        //} else {
        //    line.enabled = false;
        //    lineShouldBeEnabled = false;
        //}
    }

    private void OnEnable() {
        //if (ExperimentManager.Instance.ExperimentSettings.CurrentAppMode == ExperimentManager.AppMode.TabletARView) {
            DroneManager.Instance.ActiveDroneFlightDataChanged += OnActiveDroneFlightDataChanged;
        //} 
    }

    private void OnDisable() {
        //if (DroneManager.Instance && ExperimentManager.Instance.ExperimentSettings.CurrentAppMode == ExperimentManager.AppMode.TabletARView) {
            DroneManager.Instance.ActiveDroneFlightDataChanged -= OnActiveDroneFlightDataChanged;
        //}
    }

    private void OnActiveDroneFlightDataChanged(object sender, float droneAMSL) {
        float fillAmount = 0;
        DroneAMSL = droneAMSL;
        if (lineOrientation == LineOrientation.DownToTop) {
            //fillAmount = 1 - ((secondWaypointAMSL - droneAMSL) / 10);
            fillAmount = Mathf.Clamp01((droneAMSL - firstWaypointAMSL) / (secondWaypointAMSL - firstWaypointAMSL));
        } else {
            //fillAmount = ((firstWaypointAMSL - droneAMSL) / 10);
            fillAmount = Mathf.Clamp01(1 - (droneAMSL - secondWaypointAMSL) / (firstWaypointAMSL - secondWaypointAMSL));
        }

        FillAmount = fillAmount;

        SetFillAmount(fillAmount);
    }

    private void Update() {
        if (ParentConnection.isValid && lineShouldBeEnabled) {
            if (!line.enabled) {
                line.enabled = true;
            }
            if (ParentConnection.target[0].hasChanged || ParentConnection.target[1].hasChanged) {
                CopyLineRendererParameters();
                RecomputeLineEndpointsAGL();
            }
        } else {
            line.enabled = false;
        }
    }

    private void CopyLineRendererParameters() {
        Vector3[] positions = new Vector3[ParentConnection.line.positionCount];
        ParentConnection.line.GetPositions(positions);
        ParentConnection.line.sortingOrder = 1;

        line.SetPositions(positions);
        line.sortingOrder = 2;
    }

    private void RecomputeLineEndpointsAGL() {
        firstWaypointAMSL = ParentConnection.target[0].parent.parent.GetComponent<WaypointGameObject>().GetAMSL();
        secondWaypointAMSL = ParentConnection.target[1].parent.parent.GetComponent<WaypointGameObject>().GetAMSL();

        if (firstWaypointAMSL > secondWaypointAMSL) {
            lineOrientation = LineOrientation.TopToDown;
        } else {
            lineOrientation = LineOrientation.DownToTop;
        }
    }

    public void SetFillAmount(float fill) {
        if (lineOrientation == LineOrientation.DownToTop) {
            line.material.SetFloat("_End", fill);
        } else {
            line.material.SetFloat("_Start", fill);
        }
    }


}
