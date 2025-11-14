using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MapArea {

    public static string DefaultElevation = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

    public Vector2[] Area;

    public abstract void InitArea();

    public virtual bool IsPointInsideArea(GPS point) {
        Vector2 p = new Vector2((float)point.latitude, (float)point.longitude);
        int j = Area.Length - 1;
        bool c = false;
        for (int i = 0; i < Area.Length; j = i++)
            c ^= Area[i].y > p.y ^ Area[j].y > p.y && p.x < (Area[j].x - Area[i].x) * (p.y - Area[i].y) / (Area[j].y - Area[i].y) + Area[i].x;
        return c;
    }

    public abstract MapManager.Location GetLocation();

    public abstract string[] Get3DObjectSceneLayerData();

    public abstract string[] GetElevationData();
}
