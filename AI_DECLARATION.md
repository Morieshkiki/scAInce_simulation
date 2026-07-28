# AI Assistance Declaration

AI Assistance was used to find the errors, fix the errors, and to make the project functional.

Individual code sections that were adjusted with AI assistance are marked inline
with the note: "This section, was adjusted using AI assistance".

AI Assistance was used in the coding part and to come up with the solutions.

Some changes cannot carry an inline note, because Unity rewrites the files and
strips comments from them. These are recorded here instead:

- `Assets/Scenes/MainScene.unity`: missing `MeshCollider` components were added to
  the building meshes under `BakedBuildings` that did not have one, so the car
  collides with every building instead of driving through most of them. The
  disabled `TUM_Campus_Container`, the Cyberith treadmill rig and the unused
  `TUD/PlayerManager` rig were removed from the scene.
- `Assets/building_materials/CesiumGeoreference.prefab`: the Cesium ion access
  token was cleared so that no credential is published with the repository.
- `Packages/manifest.json` and `Packages/packages-lock.json`: editor-tooling
  packages that are not part of the simulation were removed.
- `.gitignore`, `.gitattributes` and the setup scripts were adjusted so that a
  clone of this repository is self-contained and runs without extra steps.
