# Sumonity Bicycle Model

A fully rigged and animated bicycle with humanoid rider for Unity, designed for integration with the SUMO (Simulation of Urban MObility) traffic simulation framework.

## Overview

This asset provides a complete bicycle model with a rigged humanoid rider, featuring realistic physics simulation, animations, and SUMO integration capabilities. The model was created by combining assets from MakeHuman and Sketchfab, with custom rigging and animation work done in Blender.

## Features

- **Realistic Physics**: Kinematic bicycle model with proper steering dynamics and speed-based handling
- **Full Animation Suite**: Pedaling, steering (left/right), and dynamic lean animations
- **SUMO Integration**: Built-in controller for SUMO traffic simulation with PID-based path following
- **Modular Architecture**: Separated components for physics, input, and SUMO control
- **Visual Feedback**: Dynamic wheel rotation, pedal animation, and physics-based lean
- **Manual & Automated Control**: Supports both keyboard/gamepad input and SUMO-driven control

## Contents

### Scripts
- `BicyclePhysicsController.cs` - Core physics simulation using kinematic bicycle model
- `BicycleSumoController.cs` - SUMO integration with PID controllers for path following
- `BicycleInput.cs` - Manual input handler for keyboard/gamepad control
- `BicycleLeanAnimator.cs` - Handles visual lean animation based on steering

### Assets
- **Prefabs**: Ready-to-use bicycle with rider prefab
- **Animations**: Pedaling, steering left/right animations with animator controller
- **Materials**: PBR materials for bicycle and rider
- **Textures**: High-quality textures including albedo, normal, roughness, metallic, and AO maps

## Quick Start

### Basic Setup

1. Drag the `bicyle_animated_human.prefab` into your scene
2. The prefab comes pre-configured with all necessary components
3. For manual control, ensure `BicycleInput` component is enabled
4. Press Play and use arrow keys or WASD to control the bicycle

### SUMO Integration Setup

1. Add the bicycle prefab to your scene
2. Configure `BicycleSumoController`:
   - Set `isSumoVehicle = true`
   - Set `isTeleportOnlyMode = false` for physics simulation
3. Ensure your scene has a `SumoSocketClient` component
4. The bicycle will automatically respond to SUMO commands

For detailed setup instructions, see [SUMO_INTEGRATION.md](SUMO_INTEGRATION.md)

## Controls (Manual Mode)

- **W / Up Arrow**: Accelerate
- **S / Down Arrow**: Brake
- **A / Left Arrow**: Steer left
- **D / Right Arrow**: Steer right

## Technical Specifications

### Physics Parameters
- **Wheelbase**: ~1.02m (typical bicycle geometry)
- **Wheel Radius**: ~0.34m
- **Maximum Steering Angle**: ~30°
- **Mass**: ~80kg (bicycle + rider)

### Performance
- Optimized for real-time simulation
- Compatible with Unity 2019.4 and newer
- Supports both Built-in and URP render pipelines

## Asset Attribution

This model combines assets from multiple sources:

### Bicycle Model
- **Source**: ["Bicycle Simple" by Pixel](https://sketchfab.com/3d-models/bicycle-simple-c0e5f00e1f3f47d2b19088ced9b85932)
- **License**: CC Attribution 4.0 International
- **Modifications**: Rigged and animated in Blender for Unity integration

### Humanoid Character
- **Source**: [MakeHuman](http://www.makehumancommunity.org/)
- **License**: CC0 1.0 Universal (Public Domain)
- **Modifications**: Rigged and integrated with bicycle model

### Scripts & Animations
- **Created by**: Sumonity Project Contributors
- **License**: CC Attribution 4.0 International

## License

This work is licensed under [Creative Commons Attribution 4.0 International License (CC BY 4.0)](LICENSE).

When using this asset, please provide attribution:

> "Bicycle Model with Humanoid Rider by Sumonity Project, based on 'Bicycle Simple' by Pixel (Sketchfab) and MakeHuman character assets. Licensed under CC BY 4.0."

## Part of Sumonity Project

This asset is part of the **Sumonity Project**, an open-source framework for integrating Unity with SUMO traffic simulation. The project aims to provide high-quality, reusable assets and tools for traffic simulation, urban planning visualization, and autonomous vehicle research.

### Related Projects
- **Sumonity Framework**: Core integration framework for Unity-SUMO communication
- **Vehicle Assets**: Additional vehicle models for traffic simulation
- **Urban Environment Assets**: Road infrastructure and city elements

## Requirements

- Unity 2019.4 or newer
- For SUMO integration: Sumonity Framework package

## Contributing

Contributions are welcome! This is an open-source project and we encourage:
- Bug reports and feature requests
- Animation improvements
- Physics parameter tuning
- Additional bicycle variants (mountain bike, cargo bike, etc.)
- Documentation improvements

## Changelog

### Version 1.0.0
- Initial release with full SUMO integration
- Kinematic bicycle physics model
- Complete animation suite
- Modular component architecture
- PID-based path following

## Support

For issues, questions, or contributions, please refer to the main Sumonity project repository.

## Acknowledgments

- **Pixel** for the original bicycle 3D model
- **MakeHuman Community** for the humanoid character creation tool
- **SUMO Development Team** for the traffic simulation framework
- **Blender Foundation** for the 3D modeling and animation software

---

Made with ❤️ by the Sumonity Project team
