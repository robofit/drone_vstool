using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipWaypoints : MonoBehaviour {

    [SerializeField]
    private Vector2 Offset = Vector2.one;
    [SerializeField]
    private GameObject Model;

    [SerializeField]
    private TMP_Text WaypointName;
    [SerializeField]
    private TMP_Text AMSL;
    [SerializeField]
    private TMP_Text AGL;


    private Vector2 halfScreenSize;
    private LayerMask layerMask;



    private void Start() {
        halfScreenSize = new Vector2(Screen.width / 2, Screen.height / 2);
        Model.SetActive(false);

        layerMask = LayerMask.GetMask("Mission");
    }

    private void Update() {

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) {

            SetTooltipText(hit.transform.parent.GetComponent<WaypointGameObject>());

            Model.SetActive(true);
            transform.localPosition = new Vector2(mouseScreenPosition.x - halfScreenSize.x - Offset.x, mouseScreenPosition.y - halfScreenSize.y - Offset.y);
            return;
        }

        Model.SetActive(false);
    }

    private void SetTooltipText(WaypointGameObject waypoint) {
        if (waypoint != null) {
            WaypointName.text = "Waypoint #" + waypoint.GetName();
            AMSL.text = "AMSL alt.: " + waypoint.GetCorrectedAMSL().ToString("F1") + " m";
            AGL.text = "AGL alt.: " + waypoint.GetAGL().ToString("F1") + " m";
        }
    }

}
