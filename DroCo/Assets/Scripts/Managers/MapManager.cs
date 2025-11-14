using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Map;
using UnityEngine;

public class MapManager : Singleton<MapManager> {

    public enum MapType {
        ArcGIS,
        Cesium
    }

    public enum Location {
        PRAGUE,
        BRNO,
        PILSEN,
        UNKNOWN
    }

    public Location DefaultLocation;
    public MapType CurrentMapType = MapType.ArcGIS;

    private MapArea currentArea;

    private List<MapArea> mapAreas;

    private void Start() {
        mapAreas = new List<MapArea> {
            new PragueData(),
            new BrnoData(),
            new PilsenData()
        };
    }

    public Location GetKnownLocation(GPS gpsPosition) {
        foreach (MapArea area in mapAreas) {
            if (area.IsPointInsideArea(gpsPosition)) {
                currentArea = area;
                return area.GetLocation();
            }
        }
        return Location.UNKNOWN;
    }

    public string[] Get3DObjectSceneLayerData() {
        return currentArea == null ? new string[] { } : currentArea.Get3DObjectSceneLayerData();
    }

    public string[] GetElevationData() {
        return currentArea == null ? new string[] { } : currentArea.GetElevationData();
    }

}

public class PragueData : MapArea {
    public static string PragueElevation = "https://tiles.arcgis.com/tiles/SBTXIEUGWbqzUecw/arcgis/rest/services/dtm1m_wgs_test1_3/ImageServer";
    public static string Prague2020 = "https://tiles.arcgis.com/tiles/SBTXIEUGWbqzUecw/arcgis/rest/services/budovy3D_2020_2/SceneServer/";

    public PragueData() {
        InitArea();
    }

    public override string[] Get3DObjectSceneLayerData() {
        return new string[] { Prague2020 };
    }

    public override string[] GetElevationData() {
        return new string[] { PragueElevation };
    }

    public override MapManager.Location GetLocation() {
        return MapManager.Location.PRAGUE;
    }

    //Points are defined in the order: left-top, right-top, right-bottom, left-bottom
    public override void InitArea() {
        Area = new Vector2[] {
            new Vector2(50.167268f, 14.197861f),
            new Vector2(50.210705f, 14.706782f),
            new Vector2(49.963444f, 14.769515f),
            new Vector2(49.918059f, 14.248815f)
        };
    }
}

public class BrnoData : MapArea {
    public static string BrnoElevation = "https://gis.brno.cz/ags1/rest/services/OMI/OMI_DMT_2019_wgs_elevation/ImageServer";
    public static string BrnoCompleteLOD2 = "https://gis.brno.cz/ags1/rest/services/OMI/OMI_3D_budovy_lod2_wgs/SceneServer";
    //public static string BrnoLOD1 = "https://gis.brno.cz/ags1/rest/services/Hosted/Brno_3D_LOD1_SJTSK/SceneServer";
    //public static string BrnoLOD2 = "https://gis.brno.cz/ags1/rest/services/Hosted/3D_budovy_LOD2_WM/SceneServer";


    public BrnoData() {
        InitArea();
    }

    public override string[] Get3DObjectSceneLayerData() {
        return new string[] { BrnoCompleteLOD2 };
    }

    public override string[] GetElevationData() {
        return new string[] { BrnoElevation };
    }

    public override MapManager.Location GetLocation() {
        return MapManager.Location.BRNO;
    }

    //Points are defined in the order: left-top, right-top, right-bottom, left-bottom
    public override void InitArea() {
        Area = new Vector2[] {
            new Vector2(49.2635479f, 16.4276505f),
            new Vector2(49.3201989f, 16.6624832f),
            new Vector2(49.1345533f, 16.7644501f),
            new Vector2(49.0819618f, 16.5268707f)
        };
    }
}

public class PilsenData : MapArea {
    public static string Pilsen1 = "https://tiles.arcgis.com/tiles/NUPWFrOotHlwVelG/arcgis/rest/services/3D_budovy_etapa_I_1/SceneServer";
    public static string Pilsen2 = "https://tiles.arcgis.com/tiles/NUPWFrOotHlwVelG/arcgis/rest/services/3D_budovy_etapa_I_2/SceneServer";
    public static string Pilsen3 = "https://tiles.arcgis.com/tiles/NUPWFrOotHlwVelG/arcgis/rest/services/3D_budovy_etapa_II/SceneServer";

    public PilsenData() {
        InitArea();
    }

    public override string[] Get3DObjectSceneLayerData() {
        return new string[] { Pilsen1, Pilsen2, Pilsen3 };
    }

    public override string[] GetElevationData() {
        return new string[] { DefaultElevation };
    }

    public override MapManager.Location GetLocation() {
        return MapManager.Location.PILSEN;
    }

    public override void InitArea() {
        Area = new Vector2[] { };
    }
}

public class OpenStreetMap : MapArea {
    public static string OSM3D = "https://basemaps3d.arcgis.com/arcgis/rest/services/OpenStreetMap3D_Buildings_v1/SceneServer";

    public override string[] Get3DObjectSceneLayerData() {
        throw new System.NotImplementedException();
    }

    public override string[] GetElevationData() {
        throw new System.NotImplementedException();
    }

    public override MapManager.Location GetLocation() {
        throw new System.NotImplementedException();
    }

    public override void InitArea() {
        throw new System.NotImplementedException();
    }
}
