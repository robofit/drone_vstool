using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public abstract class Drone2D : MonoBehaviour {

    [SerializeField]
    protected Image droneImage;

    [SerializeField]
    private Camera Map2DCamera;
    [SerializeField]
    private float FixedSize = 0.005f;
    [SerializeField]
    private bool AdaptSizeDynamically = false;


    private void Update() {
        if (Map2DCamera != null && AdaptSizeDynamically) {
            AdaptSize();
        }
    }

    public virtual void InitDrone() {
        Map2DCamera = MapManager.Instance.CurrentMapType == MapManager.MapType.ArcGIS ? GameManager.Instance.MinimapCamera.GetComponent<Camera>() : GameManager.Instance.MinimapCameraCesium.transform.GetChild(0).GetComponent<Camera>();
    }

    public abstract void UpdateFlightData(DroneFlightData flightData);

    public void Highlight(bool highlight) {
        droneImage.color = highlight ? Color.green : Color.black;
    }

    private void AdaptSize() {
        float distance = (Map2DCamera.transform.position - transform.position).magnitude;
        float size = distance * FixedSize * Map2DCamera.fieldOfView;
        droneImage.transform.localScale = Vector3.one * size;
        //droneImage.transform.forward = droneImage.transform.position - Map2DCamera.transform.position;
    }

    public abstract void SetGPSOffset(GPS offset);

    public abstract GPS GetDroneLocation();
}
