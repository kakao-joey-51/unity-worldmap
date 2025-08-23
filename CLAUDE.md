# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D project (version 2022.3.62f1) focused on creating a world map simulation with ship navigation. The project uses Universal Render Pipeline (URP) and includes terrain collision detection, ship physics, and camera following systems.

## Build and Development Commands

### Unity Editor
- Open the project by loading `/Users/joey51/works/WorldMap` in Unity Editor 2022.3.62f1
- Main scene: `Assets/World.unity`
- Demo scenes available in `Assets/Scenes/SampleScene.unity`

### Project Structure
- **Assets/Scripts/**: Core gameplay scripts
  - `ShipController.cs`: Main ship physics and movement controller with Rigidbody-based navigation
  - `SimpleCameraFollower.cs`: Third-person camera system that follows ship with configurable distance and damping
  - **Assets/Scripts/Util/**: Utility classes with CAU namespace
    - `Interfaces.cs`: Comprehensive interface definitions for managers, controllers, singletons, and design patterns
    - `MonoSingletone.cs`: Thread-safe MonoBehaviour singleton implementation with DontDestroyOnLoad
    - `NormalSingletone.cs`: Non-MonoBehaviour singleton with IDisposable pattern

## Architecture Patterns

**CAU Namespace Convention:**
The project uses a custom "CAU" namespace with structured interfaces for different component types:
- `IManager`: Singleton managers that create, cache, and control objects of the same parent class (e.g., UIPanelManager managing BaseUIPanel objects)
- `IController`: Controllers for managing and caching multiple object types with initialize/release lifecycle
- `ISceneController`: Top-level Unity Scene controllers that manage 3D space objects and cache child GameObjects
- `ISingleton<T>`: Generic singleton interface with Release() method
- `IUtility`: Common utility method collections for complex shared logic
- `IUpdateInvokeObject`: Interface for delegating Update logic to manager classes
- `IBehaviourTreeNode`: AI behavior tree nodes with Success/Failure/Running states
- `IObjectPool` & `IGenericPool<T>`: Object pooling interfaces for performance optimization

**Singleton Implementations:**
- `MonoSingleton<T>`: Thread-safe MonoBehaviour singleton with DontDestroyOnLoad and ghost object prevention
- `NormalSingleton<T>`: Non-MonoBehaviour singleton with IDisposable pattern for code-only access

**Ship Control System:**
- Physics-based movement using `Rigidbody.AddRelativeForce()` and `AddTorque()`
- Terrain collision detection via layer-based system (Terrain layer) at ShipController.cs:103
- Maximum angular velocity limiting (rb.maxAngularVelocity) to prevent excessive rotation during wandering at ShipController.cs:39
- Input handling through Unity's Input.GetAxis() system in Update(), physics in FixedUpdate()
- Collision response zeroes velocity to prevent bouncing off terrain at ShipController.cs:106-107

**Camera System:**
- `SimpleCameraFollower` provides smooth third-person camera following
- Configurable distance, height, and damping values for camera positioning
- LateUpdate-based camera positioning for stable following behavior after physics
- Target-relative positioning with rotation interpolation using Mathf.LerpAngle()

## Key Technical Details

### Physics Configuration
- Ship uses BoxCollider with very small size (0.01f³) for collision detection at ShipController.cs:48
- Physics materials: `HighFrictionTerrainMaterial` and `LowFrictionTerrainMaterial` in Assets/Materials/
- Collision response includes velocity zeroing to prevent bouncing off terrain
- Uses Time.fixedDeltaTime for physics calculations in FixedUpdate()

### Scene Setup
- Main world scene: `Assets/World.unity`
- Sample scene: `Assets/Scenes/SampleScene.unity`
- URP configuration in `Assets/Settings/` with multiple quality presets (Performant, Balanced, HighFidelity)
- Terrain collision detection configured via LayerMask.NameToLayer("Terrain") system
- Extensive terrain tile system in `Assets/Terrains/` with raw heightmap files

### Asset Organization
- **Assets/Models/**: 3D ship models with textures and materials
- **Assets/Materials/**: Physics materials for terrain interaction
- **Assets/Low Poly Nature Pack Lite/**: Environmental assets for world building
- **Assets/PanoramicCartoonSkybox/**: Sky rendering assets with day/night demo scenes
- **Assets/IgniteCoders/**: Simple Water Shader components

## Development Notes

- All scripts use Korean comments for documentation
- Follows Unity component architecture with RequireComponent attributes
- Thread-safe singleton implementations prevent memory leaks and ghost objects
- Camera updates run in LateUpdate() for stable positioning after physics
- Interface-driven design pattern throughout the CAU namespace utilities
- Behavior tree and object pooling interfaces prepared for future AI and performance systems