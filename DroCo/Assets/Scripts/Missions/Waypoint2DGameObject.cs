using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class Waypoint2DGameObject : MonoBehaviour {

    [SerializeField]
    private RectTransform connectionBinder;
    public RectTransform ConnectionBinder => connectionBinder;

    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private RectTransform model; 

    private LayerMask layerMask;
    private CesiumGlobeAnchor location;


    private void Start() {
        layerMask = LayerMask.GetMask("Map");
    }

    private void SetText(string txt) {
        text.text = txt;
    }

    public void InitWaypoint(string waypointName, double offset = 0) {
        SetText(waypointName);

        location = GetComponent<CesiumGlobeAnchor>();

        if (isActiveAndEnabled) {
            StartCoroutine(GroundWaypointCoroutine(offset));
        }
    }


    private void GroundWaypoint(double offset = 0) {
        //Debug.Log("Ground Waypoint");
        if (Physics.Raycast(transform.position + new Vector3(0, 1000, 0), transform.TransformDirection(Vector3.down), out RaycastHit hit, Mathf.Infinity, layerMask)) {
            //Debug.Log("Hit " + hit.transform.name);
            if (hit.collider != null) {
                double3 originalPosition = location.longitudeLatitudeHeight;
                location.longitudeLatitudeHeight = new double3(originalPosition.x, originalPosition.y, originalPosition.z - (double) (hit.distance - 1000) + offset);
            }
        }
    }

    private IEnumerator GroundWaypointCoroutine(double offset = 0) {
        yield return new WaitForSeconds(0.5f);

        GroundWaypoint(offset);
    }
}
