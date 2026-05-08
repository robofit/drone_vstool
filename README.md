# DroCo – V2
Branch DroCoV2 presents a completely new and clean version 2.0 of the original DroCo, currently still in development. Tested on Unity version 2022.3.62.

## What's new
 - Possibility to switch to AR.
 - Synchronized video stream with flight data.
 - New map base layer – [ArcGIS](https://developers.arcgis.com/unity/).
 - Supports Google 3D dataset using [Cesium](https://cesium.com/learn/unity/unity-photorealistic-3d-tiles/).
 - New GUI design.

## DroCo – Multi-Drone Control Vizualization Tool
[DroCo (VSTool)](https://www.fit.vut.cz/research/product/647/.en) is a tool for effective drone remote control using mixed reality that also supports communication and cooperation on a mission with multiple drones. The proposed solution is developed by [Robo@FIT, Brno University of Technology](https://www.fit.vut.cz/research/group/robo/.en) research group, and is inspired by the high mental load of the pilot in the control of the drone, especially in the performance of more complex missions (multiple drones, remote target, proximity to infrastructure etc.). The system is based on the extension of the 3D virtual model with real data (augmented virtuality). It uses temporal and spatial registration of:
 1) off-line data (map data, elevation data, 3D building models) – currently supports ArcGIS and Cesium.
 2) online data (video-stream, reconstructed 3D structures, location information, flight data)
 3) virtual control objects (navigation points and directions, spatial areas, geo-fences, position of other drones, distance to nearby objects, preview map, or view from other drones). 
 
The system thus allows you to pilot the drone in FPV (first-person-view), but at any time, it can switch to TPV (third-person-view) so that one can look around freely in a situation with poor orientation, further directing the pilot to other mission objectives, points out close objects or other drones, etc. The system is currently being expanded with the functions of multiple drones, sharing more sensory information across the system and increasing network communication security. The development also aims to use a system for drone control training for pilots, increase the realism of drone behavior in simulated mode, more efficient mission management, and visualization of the status for the operator of the whole event.

The system supports importing planned missions from the mission planning software – [UgCS](https://www.sphengineering.com/flight-planning/ugcs):
<img src=AVmission.png />

The mission can also be displayed in augmented reality:
<img src=arView.png />

Multi-drone use cases are also supported:
<img src=droco_gui.png />

## Installation
 - Install [UnxUtils](https://sourceforge.net/projects/unxutils/) to be able to patch ArcGIS scripts using patch_arcgis.bat script.
 - Clone this repo:
   ```bash
   git clone git@github.com:robofit/drone_vstool.git
   ```
 - Get submodules:
   ```bash
   cd drone_vstool
   git submodule update --init
   ```
 - Download multimedia files from LFS:
   ```bash
   git lfs install
   git lfs pull
   ```
 - Create a symlink of the submodules to the Assets folder:
   ```bash
   cd scripts
   .\link_submodules.bat
   ```
 - Load project in Unity and open **MainScene**, located in Assets/Scenes.
### Setup ArcGIS
 - Create ArcGIS developer account and [create your API Key](https://developers.arcgis.com/documentation/security-and-authentication/api-key-authentication/tutorials/create-an-api-key/).
 - Paste the API Key to `ProjectSettings -> ArcGIS Maps SDK -> API Key`.

### Setup GStreamer (optional, not required)
 - Install GStreamer [1.20.1](https://gstreamer.freedesktop.org/data/pkg/windows/1.20.1/) – install both, regular and devel version based on your computer's architecture (msvc and x86_64 works for me).
 - Add gstreamer binary folder path to System Environment Variables – `Computer -> System properties -> Advanced System Settings -> Advanced Tab -> Environment Variables... -> System Variables -> Variable: Path -> Edit -> New -> C:\gstreamer\1.0\msvc_x86_64\bin`
 - Create new system variable – `New Variable: GST_SDK_PATH= C:\gstreamer\1.0\x86_64\`
 - If GStreamer is still not working inside Unity, try to install or reinstall the latest [MSVC redistributable libraries](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170).

## Publications
 - [HUBINÁK, Róbert. Application for Efficient Drone Control Using Augmented Virtuality. Brno, 2020. Bachelor's thesis. Brno University of Technology, Faculty of Information Technology. Supervised by Beran Vítězslav.](https://www.fit.vut.cz/study/thesis-file/22839/22839.pdf)
 - [SEDLMAJER Kamil, BAMBUŠEK Daniel a BERAN Vítězslav. Effective Remote Drone Control Using Augmented Virtuality. In: Proceedings of the 3rd International Conference on Computer-Human Interaction Research and Applications 2019. Vienna: SciTePress - Science and Technology Publications, 2019, s. 177-182. ISBN 978-989-758-376-6.](https://www.fit.vut.cz/research/publication/12006/.en)
 - [SEDLMAJER, Kamil. User interface for drone control using augmented virtuality. Brno, 2019. Master's Thesis. Brno University of Technology, Faculty of Information Technology. 2019-06-14. Supervised by Beran Vítězslav.](https://www.fit.vut.cz/study/thesis-file/16730/16730.pdf)
