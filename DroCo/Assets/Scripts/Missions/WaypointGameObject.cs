using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Esri.ArcGISMapsSDK.Components;
using Highlighters;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public abstract class WaypointGameObject : MonoBehaviour {

    [SerializeField]
    private RectTransform connectionBinder;
    public RectTransform ConnectionBinder => connectionBinder;

    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private MeshRenderer model;

    [SerializeField]
    private GameObject shadow;

    [SerializeField]
    private Material apMaterial;

    [SerializeField]
    private Material apErrorMaterial;

    private LayerMask layerMask;

    public float SphereRadius = 1f;

    private HighlighterRenderer highlighter;
    public bool highlighted = false;

    public Waypoint WaypointRef;


    public virtual void Start() {
        layerMask =~ LayerMask.GetMask("Mission", "DroneScreen");
        highlighter = new HighlighterRenderer(model, 1);
        if (GameManager.Instance.CurrentAppMode == GameManager.AppMode.Experiment) {
            if (ExperimentManager.Instance.ExperimentSettings.CurrentAppMode == ExperimentManager.AppMode.TabletARView) {
                text.gameObject.SetActive(true);
            } else if (ExperimentManager.Instance.ExperimentSettings.CurrentAppMode == ExperimentManager.AppMode.DesktopUgCS) {
                text.gameObject.SetActive(true);
            } else {
                text.gameObject.SetActive(false);
            }
        } else {
            text.gameObject.SetActive(true);
        }
    }

    public virtual void Update() {
        GroundShadow();
        CheckCollisions();
    }

    private void GroundShadow() {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out RaycastHit hit, Mathf.Infinity, layerMask)) {
            if (hit.collider != null) {
                shadow.transform.localScale = new Vector3(shadow.transform.localScale.x, hit.distance / 2, shadow.transform.localScale.z);
            }
        }
    }

    private void CheckCollisions() {
        if (Physics.CheckSphere(transform.position, SphereRadius, layerMask)) {
            //model.material = apErrorMaterial;
            if (!highlighted) {
                MissionManager.Instance.HighlightWaypointOccluded(highlighter);
                highlighted = true;
            }
        } else {
            //model.material = apMaterial;
            if (highlighted) {
                MissionManager.Instance.UnHighlightWaypoint(highlighter);
                highlighted = false;
            }
        }
    }

    public void SetText(string txt) {
        text.text = txt;
    }



    public virtual void SetAltitudeCoroutine(float altitude) {

    }


    public abstract void SetLocation();

    public void SetAsStartingPoint() {
        model.transform.localScale = Vector3.one;
        model.material.color = Color.green;
    }

    public void SetAsLastPoint() {
        model.transform.localScale = Vector3.one;
        model.material.color = Color.red;
    }

    public string GetName() {
        return WaypointRef.Name;
    }

    public abstract float GetAMSL();

    public abstract float GetCorrectedAMSL();

    public abstract float GetAGL();
}
