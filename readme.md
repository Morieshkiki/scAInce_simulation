# scAInce — A Drivable Virtual Replica of Darmstadt Traffic

Real-time SUMO–Unity co-simulation of **Luisenplatz, Darmstadt**.

Microscopic traffic simulation is normally observed from the outside, as a 2D plan
view interpreted from above. This project turns such a simulation into an
environment that can be entered: a Unity-based, real-time co-simulation in which a
user drives a car through live, microscopically simulated traffic on a real
Darmstadt street space.

Morteza Eshkiknezhad — Matrikelnr. 2262833 — Future of Mobility, Sommersemester 2026

## Study area

Luisenplatz and its immediate surroundings in the centre of Darmstadt. The square
is the central tram and bus hub of the city: several tram lines and many bus routes
cross or terminate there and share the space with car traffic on the adjoining
streets such as Rheinstrasse. It also contains the Citytunnel, which carries car
traffic underneath the square while trams and buses stay on the surface.

A small area therefore already contains dedicated tram bodies, bus movements,
ordinary car flows and a grade-separated underpass — a demanding and representative
test case without requiring a large network.

## Architecture

A three-stage pipeline on a single machine:

```
   SUMO  ──TraCI──▶  Python bridge  ──TCP :25001──▶  Unity
(traffic authority)   (0.1 s steps)                  (rendering + drivable car)
```

- **SUMO** runs the microscopic simulation and remains the single source of truth
  for the traffic.
- **The bridge** is a Python process built on the [Sumonity](https://github.com/TUM-VT/Sumonity)
  interface (Pechinger & Lindner, 2024), adapted for this project. It opens a local
  TCP server on port `25001`, starts SUMO through TraCI, then on each iteration
  advances the simulation by one time step of 0.1 s, reads the position, heading,
  speed and class of every vehicle, and streams that state to Unity — roughly twenty
  updates per second.
- **Unity** is the client. It connects to the socket, spawns one object per vehicle
  chosen by the class SUMO reports, and smooths the received positions so vehicles
  glide between updates instead of snapping.

Startup order matters: the bridge begins listening first, then Unity connects.

### The 3D world

The world is assembled from two matched sources so the rendered city corresponds to
the simulated one:

- **Roads** are generated from the same SUMO network, using a road-building step
  based on SUMO2Unity (Mohammadi et al., 2024). Because roads and simulation come
  from one network, a vehicle reported by SUMO sits on the visible road rather than
  beside it.
- **Buildings** come from the official Hessian **LoD2** city model, brought in
  through Cesium for Unity and placed in the correct geographic frame. They are left
  untextured on purpose — the priority at this stage is correct geometry and
  footprints, not photorealistic facades.

Alignment is the key correctness condition: SUMO works in a local projected
coordinate system, and the scene is built in that same local frame, so no per-vehicle
correction is needed.

### The drivable vehicle

A keyboard-driven car with a spring-based suspension, five gears and steering that
tightens as speed increases, followed by a third-person chase camera. The driven car
is **not** part of the SUMO simulation — it is controlled by Unity physics, while all
surrounding vehicles remain under SUMO's control. Collisions with simulated vehicles
are detected and dent the affected body, so an impact is visible rather than abstract.

### Traffic demand

Demand is expressed as static flows: passenger cars on the through streets, trams and
buses along their real corridors. Speeds are capped to the ~30 km/h regime typical of
these inner-city streets, with trams slightly lower. The pattern is deliberately
**hypothetical rather than calibrated** against measured counts — its purpose is to
keep the square continuously populated with a plausible mix so that a driver always
encounters traffic. Because the demand lives in static files inside the project, the
same traffic unfolds on every launch.

## Prerequisites

- Unity **6000.4.1f1**
- SUMO 1.21 or later — important: the installed `traci` must be the same version
- Python 3.11 (later versions run into compatibility issues)
- Windows 10/11
- Git Bash (required for the vcs tooling)

## Installation

This repository is **self-contained**: the co-simulation bridge, the SUMO network
and demand, and all vehicle models are committed with the project. A clone has
everything needed to run — there is no `vcs import` step.

The repository uses **Git LFS** for its meshes and textures, so install that first:

```bash
git lfs install
git clone https://github.com/Morieshkiki/scAInce_simulation.git
```

Then run the setup script in PowerShell as Administrator:

```powershell
.\setup.ps1
```

### Python environment for the bridge

The only thing not committed is the Python virtual environment for TraCI, which
has to be created locally:

```bash
cd Assets/Sumonity/SumoTraCI
pip install virtualenv
python3.11 -m venv venv
./venv/Scripts/activate
pip install -r requirements.txt
```

If script execution is blocked, run PowerShell as Administrator:

```powershell
Set-ExecutionPolicy Unrestricted
```

## Running

Open the project in Unity and open `Assets/Scenes/MainScene.unity`.

Press **Play**. That single action launches the co-simulation bridge, which starts
SUMO on the fixed Luisenplatz configuration and begins streaming; the drivable scene
comes up at the same moment. There is no need to start SUMO, the bridge and the scene
separately.

Drive with the keyboard; the chase camera follows the car, with a speed and gear
indicator on screen.

## Repository layout

| Path | Contents |
|------|----------|
| `Assets/Scenes/MainScene.unity` | The Luisenplatz scene — entry point |
| `Assets/Scripts/PlayerCarController.cs` | Drivable car: suspension, gears, steering |
| `Assets/Scripts/ThirdPersonFollowCamera.cs` | Chase camera |
| `Assets/Scripts/Crash/` | Collision handling and mesh deformation |
| `Assets/Scripts/PositionAccuracyLogger.cs` | Logs SUMO-vs-Unity position error |
| `Assets/Scripts/CITesting/AutomatedTesting.cs` | Headless CI run driver |
| `Assets/Editor/RoadBeautifier.cs` | Road geometry post-processing |
| `Assets/building_materials/` | Cesium georeference for the LoD2 city model |
| `Assets/Sumonity/SumoTraCI/socketServer.py` | The bridge — TraCI loop, TCP server on `25001` |
| `Assets/Sumonity/SumoTraCI/sumoProject/opensource.sumocfg` | Active SUMO config |
| `Assets/Sumonity/SumoTraCI/sumoProject/net_files/` | Corrected network (`osm.base.net.xml`) |
| `Assets/Sumonity/SumoTraCI/sumoProject/demand_files/` | Static demand (`darmstadt_manual.rou.xml`) |
| `Assets/Sumonity-PassengerCars/`, `BusModel/`, `TaxiModel/` | Models spawned per SUMO vehicle class |
| `Assets/Vladislav Simakov/` | Tram and bus meshes |
| `Assets/3d_model/` | Road decor meshes, curb/sidewalk materials, tree prefabs |
| `assets.repos` | Provenance record of the upstream repos these folders came from |

The SUMO scenario is wired as `socketServer.py` → `opensource.sumocfg` →
`net_files/osm.base.net.xml` + `demand_files/darmstadt_manual.rou.xml`, with a
warm-up state in `warm_up/warm_up_state.xml`.

> **Note on `Assets/3d_model`.** The `tum_main_*` tiles from the tum2twin dataset
> (~1.4 GB) are *not* committed. They are referenced only by `TUM_Campus_Container`,
> which is disabled in `MainScene` and left over from the upstream base project; the
> Luisenplatz environment uses `BakedBuildings` and Cesium instead. Run
> `download_unity_fbx.ps1` if you ever want to re-enable that container.

## Continuous integration

`.github/workflows/unity-test.yml` runs on a self-hosted Windows runner. It executes
`setup.ps1`, then `run_scene_automated.ps1`, which drives the scene headlessly and
checks the streamed vehicle positions against SUMO with an error threshold of 1.5 m.
Position-accuracy statistics and logs are uploaded as build artifacts.

## Scope and limitations

The scope was kept deliberately narrow so the result could be finished and verified:

- One district, one representative demand pattern.
- Motorised modes only — passenger cars, buses and trams. Pedestrians, cyclists and
  public-transport stops are out of scope.
- The demand is hypothetical, not calibrated against measured counts.

This work is an **integration of adapted open components rather than a new
algorithm**: a working, launch-and-drive virtual replica of one Darmstadt district,
built to run reproducibly.

## Notes on inherited scripts

This project is based on the [Sumonity-UnityBaseProject](https://github.com/TUM-VT/Sumonity-UnityBaseProject)
from TUM-VT. Two leftovers from that base project remain:

- `download_unity_fbx.ps1` fetches the TUM main campus model from tum2twin. The
  Luisenplatz environment takes its buildings from the Hessian LoD2 model via
  Cesium instead, so this is only needed for the disabled `TUM_Campus_Container`.
- `assets.repos` and `vcs_import.sh` describe how the bridge and vehicle models
  used to be pulled. Those folders are now committed directly, because the bridge
  and the SUMO scenario were adapted for Luisenplatz and the upstream repositories
  do not contain those changes. `setup.ps1` skips the import when the folders are
  already present.

## References

- Lopez et al. (2018) — *Microscopic Traffic Simulation using SUMO*
- Pechinger & Lindner (2024) — *Sumonity*
- Mohammadi et al. (2024) — *SUMO2Unity*
- Nagy et al. (2025) — *SUMITY*

See [`AI_DECLARATION.md`](AI_DECLARATION.md) for the AI assistance declaration.
