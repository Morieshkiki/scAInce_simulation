# Textured FBX Models for Unity

This directory contains textured FBX models with Unity meta files for the TUM main campus fbx dataset. These models are plug-and-play ready for Unity projects.

## Contents

- **FBX Models**: Pre-processed 3D models in tiled format (`tum_main_bus_stops_Tile_X_Y.fbx`)
- **Materials**: Unity material files with texture mappings
- **Textures**: Texture assets for the 3D models
- **Meta Files**: Unity-specific metadata for seamless integration

## Usage

1. Copy the entire `UnityFbx` folder into your Unity project's `Assets` directory
2. Unity will automatically recognize the meta files and import settings
3. The models will be ready to use with materials and textures already applied

## Tile Structure

The models are organized in a grid-based tile system:
- Tiles are named using the pattern: `tum_main_bus_stops_Tile_X_Y`
- X and Y represent the grid coordinates
- Coverage includes tiles from (1,1) to (5,3)


![Textured Game Engine Model](docs/textured_gameEngine_model.png)