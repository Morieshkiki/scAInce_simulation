 # Bicycle SUMO Integration Guide

## Overview

The new bicycle model now supports SUMO (Simulation of Urban MObility) integration through the `BicycleSumoController` script. This allows the bicycle to be controlled by the SUMO traffic simulation backend instead of manual input.

## Components

### 1. BicyclePhysicsController
The core physics simulation component that handles bicycle dynamics using a kinematic bicycle model. This component handles:
- Torque-based longitudinal control
- Steering dynamics with speed attenuation
- Visual wheel rotation and lean
- Pedal/crank animations

### 2. BicycleSumoController (NEW)
Wrapper component that integrates SUMO control with the `BicyclePhysicsController`. Features:
- Implements `IVehicleController` interface for SUMO integration
- Automatic switching between teleport and physics modes
- PID controllers for speed and path following
- Debug visualization with Gizmos
- Automatically disables manual input when active

### 3. BicycleInput (MODIFIED)
Manual input handler that now checks for SUMO control:
- Automatically disables when SUMO is controlling the vehicle
- Shows current control mode in debug UI
- Supports both digital and analog steering

## Setup Instructions

### For a New Bicycle GameObject:

1. **Add Required Components:**
   ```
   GameObject → Add Component → BicyclePhysicsController
   GameObject → Add Component → BicycleSumoController
   GameObject → Add Component → BicycleInput (optional, for manual testing)
   GameObject → Add Component → Rigidbody
   ```

2. **Configure Rigidbody:**
   - Mass: ~80 kg (bicycle + rider)
   - Use Gravity: Enabled
   - Is Kinematic: Will be controlled by script
   - Constraints: Freeze Rotation Y (optional, for stability)

3. **Configure BicyclePhysicsController:**
   - Assign wheel mesh references
   - Set `wheelbase` (typically ~1.02m for a bicycle)
   - Set `wheelRadius` (typically ~0.34m)
   - Set `maxSteerDeg` (typically ~30°)
   - Adjust torque and speed limits as needed

4. **Configure BicycleSumoController:**
   - Set `isSumoVehicle = true` to enable SUMO control
   - Set `isTeleportOnlyMode = false` for physics simulation
   - The `id` property will be set by SUMO at runtime

5. **Configure BicycleInput (optional):**
   - Only needed if you want manual control when SUMO is not active
   - Set `useDigitalSteer = true` for simple left/right steering

### For Migrating from Old Bicycle Model:

If you have an existing bicycle using the old `bicycleController.cs`:

1. **Keep the Old Script Temporarily:**
   - Don't delete `bicycleController.cs` yet in case you need to reference settings

2. **Add New Components:**
   - Add `BicyclePhysicsController` to the GameObject
   - Add `BicycleSumoController` to the GameObject

3. **Transfer Settings:**
   - Copy wheel reference assignments
   - Transfer `maxSpeed`, `acceleration`, `brakingForce` values
   - Transfer `wheelBase` and `wheelRadius` values
   - Transfer `maxSteeringAngle` to `maxSteerDeg`

4. **Update Prefabs:**
   - If using prefabs, update the prefab with new components
   - Remove old `bicycleController` component

5. **Test:**
   - Test with SUMO integration active
   - Verify physics behavior matches expectations
   - Check that manual input is properly disabled when SUMO is active

## Control Modes

### SUMO Control Mode (isSumoVehicle = true)
- Vehicle receives position and velocity commands from SUMO backend
- Manual input is automatically disabled
- Two sub-modes:
  - **Physics Mode** (`isTeleportOnlyMode = false`): Uses physics simulation
  - **Teleport Mode** (`isTeleportOnlyMode = true`): Direct position updates without physics

### Manual Control Mode (isSumoVehicle = false)
- Vehicle responds to keyboard/gamepad input
- SUMO integration is bypassed
- Useful for testing and debugging

## PID Controller Tuning

The SUMO integration uses two PID controllers:

### Distance Controller (Path Following)
```csharp
pidControllerDist = new PIDController(15.0f, 0.0f, 0.0f);
```
- Kp = 15.0: Controls how aggressively the bicycle follows the path
- Higher values = tighter path following, but may cause oscillation
- Lower values = smoother but less accurate path following

### Speed Controller
```csharp
pidControllerSpeed = new PIDController(1.0f, 0.0f, 0.0f);
```
- Kp = 1.0: Controls acceleration/braking response
- Higher values = faster speed adjustments
- Lower values = smoother but slower speed changes

**Note:** Adjust these values in `BicycleSumoController.cs` `InitializeSumoIntegration()` method if needed.

## Debug Visualization

When `bDrawGizmo = true` in `BicycleSumoController`:
- **Red Sphere**: Look-ahead marker (target position from SUMO)
- **Blue Sphere**: Current rigidbody position
- Visible in Scene view during gameplay

## API Reference

### BicycleSumoController Public Methods:

```csharp
// Toggle teleport-only mode
public void SetTeleportOnlyMode(bool value)

// Get current SUMO stop state
public float GetStopState()

// Check if SUMO is currently controlling this vehicle
public bool IsSumoControlled()
```

### IVehicleController Interface:

```csharp
public string id { get; set; } // SUMO vehicle identifier
```

## Troubleshooting

### Vehicle Not Responding to SUMO:
1. Check that `SumoSocketClient` exists in the scene
2. Verify `isSumoVehicle = true` on `BicycleSumoController`
3. Check that the vehicle ID is properly set by SUMO
4. Enable Gizmos to visualize target markers

### Vehicle Jumps or Teleports Unexpectedly:
1. Set `isTeleportOnlyMode = false` for physics-based control
2. Check Rigidbody constraints
3. Verify `isKinematic` is being set correctly

### Manual Input Still Active with SUMO:
1. Ensure `BicycleSumoController` is attached to the GameObject
2. Check that `IsSumoControlled()` returns `true`
3. Verify `SumoSocketClient` is found in the scene

### Physics Behavior Different from Old Model:
1. The new physics model uses a proper kinematic bicycle model
2. Adjust `steerSpeedAttenuation` for different handling at speed
3. Tune `acceleration`, `maxDriveTorque`, and `maxBrakeTorque` values
4. Check `wheelbase` and `wheelRadius` match your bicycle geometry

## Comparison: Old vs New

| Feature | Old (bicycleController) | New (BicycleSumoController) |
|---------|------------------------|----------------------------|
| Physics Model | Simple forward movement | Kinematic bicycle model |
| Steering | Direct rotation | Speed-attenuated, realistic |
| Visual Lean | No | Yes, physics-based |
| Wheel Animation | Basic rotation | Accurate angular velocity |
| Pedal Animation | No | Yes, torque-based |
| SUMO Integration | Built-in | Separate component |
| Manual Input | Built-in | Separate component |
| Modularity | Monolithic | Separated concerns |

## Notes

- The new system is more modular, separating physics, SUMO control, and manual input
- Physics behavior is more realistic with proper bicycle dynamics
- Visual feedback (lean, pedals) provides better rider presence
- PID controllers may need tuning for different bicycle types (road bike vs cargo bike)
