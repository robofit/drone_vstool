using UnityEngine;

public class DroneDataPoller : MonoBehaviour {
    public static DroneDataPoller Instance;

    private DroneFlightData latestFlightData = null;
    private object dataLock = new object();

    private void Awake() {
        Instance = this;
    }

    public void PushLatestData(DroneFlightData data) {
        lock (dataLock) {
            latestFlightData = data;
        }
    }

    private void Update() {
        DroneFlightData toProcess = null;

        lock (dataLock) {
            if (latestFlightData != null) {
                toProcess = latestFlightData;
                latestFlightData = null;
            }
        }

        if (toProcess != null) {
            DroneManager.Instance.HandleReceivedDroneDataAndForward(toProcess);
        }
    }
}
