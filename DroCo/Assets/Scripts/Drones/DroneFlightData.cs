using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GPS {
    public double latitude;
    public double longitude;

    public override string ToString() {
        return $"{{latitude:{latitude}, longitude:{longitude}}}";
    }
}

[Serializable]
public class AircraftOrientation {
    public double pitch;
    public double roll;
    public double yaw;
    public double compass;
    
    public override string ToString() {
        return $"{{pitch:{pitch}, roll:{roll}, yaw:{yaw}, compass:{compass}}}";
    }
}

[Serializable]
public class AircraftVelocity {
    public double velocity_x;
    public double velocity_y;
    public double velocity_z;

    public override string ToString() {
        return $"{{x:{velocity_x}, y:{velocity_y}, z:{velocity_z}}}";
    }
}

[Serializable]
public class GimbalOrientation {
    public double pitch;
    public double roll;
    public double yaw;
    public double yaw_relative;

    public override string ToString() {
        return $"{{pitch:{pitch}, roll:{roll}, yaw:{yaw}, yaw_relative:{yaw_relative}}}";
    }
}

[Serializable]
public class DroneFlightData {
    public string client_id;
    public double altitude;
    public double relative_altitude;
    public GPS gps;
    public AircraftOrientation aircraft_orientation;
    public AircraftVelocity aircraft_velocity;
    public GimbalOrientation gimbal_orientation;
    public int satellite_count;
    public string gps_signal_level;
    public Sticks sticks;
    public string timestamp;
    //public string frame;
    public byte[] frame;

    public DroneFlightData() {
        client_id = "unset";
        altitude = 0;
    }

    public override string ToString() {
        return $"{{client_id:{client_id}, altitude:{altitude}, relative_altitude:{relative_altitude}, gps:{gps}, aircraft_orientation:{aircraft_orientation}, gimbal_orientation:{gimbal_orientation}, satellite_count:{satellite_count}, gps_signal_level:{gps_signal_level}, sticks:{sticks}, timestamp:{timestamp}}}";
    }

    //public void SetData(double height, double latitude, double longitute, double pitch, double roll, double yaw, double compass) {
    //    Altitude = height;
    //    Latitude = latitude;
    //    Longitude = longitute;
    //    Pitch = pitch;
    //    Roll = roll;
    //    Yaw = yaw;
    //    Compass = compass;
    //}
}

[Serializable]
public class Sticks {
    public Stick left_stick;
    public Stick right_stick;

    public override string ToString() {
        return $"{{left_stick:{left_stick}, right_stick:{right_stick}}}";
    }
}

[Serializable]
public class Stick {
    public int x;
    public int y;

    public override string ToString() {
        return $"{{x:{x}, y:{y}}}";
    }
}

[Serializable]
public class RemoteController {
    public string client_id;
    public Stick left_stick;
    public Stick right_stick;
    public int gimbal_wheel;

    public override string ToString() {
        return $"{{client_id:{client_id}, left_stick:{left_stick}, right_stick:{right_stick}, gimabl_wheel:{gimbal_wheel}}}";
    }

}
