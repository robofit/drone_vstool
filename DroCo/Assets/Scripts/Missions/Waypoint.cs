using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Waypoint {

    public GPS Coordinates {
        get; private set;
    }

    public double Altitude {
        get; private set;
    }

    public string Name {
        get; private set;
    }

    private GameObject objectVisual;
    private GameObject objectVisual2D;

    public Waypoint(GPS coordinates, double altitude) {
        Coordinates = coordinates;
        Altitude = altitude;
    }

    public void SetVisual(GameObject visual) {
        objectVisual = visual;
    }

    public void SetVisual2D(GameObject visual2D) {
        objectVisual2D = visual2D;
    }

    public void SetName(string name) {
        Name = name;
    }

    public void DestroyVisual() {
        GameObject.Destroy(objectVisual);
        GameObject.Destroy(objectVisual2D);
    }
}
