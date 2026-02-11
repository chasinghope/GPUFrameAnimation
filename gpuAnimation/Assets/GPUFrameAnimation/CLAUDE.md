# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GPUFrameAnimation is a high-performance 2D frame animation system for Unity that uses GPU Instancing and texture atlases to batch-render multiple animated objects efficiently. All code is located under `Assets/GPUFrameAnimation/`.

## Architecture

### Core Components (Three-Layer Architecture)

```
GPUAnimManager (Singleton) - Material pooling & global time
        │
        ▼
GPUFrameAnimator - Multi-animation controller
        │
        ▼
GPUInstancedAnimation - Core animation component
        │
        ▼
GPUAnimFlash - Visual effect component
```

### Component Responsibilities

1. **GPUAnimManager** (`Scripts/GPUAnimManager.cs`) - Thread-safe singleton managing material cache pool and global `_UnscaledTime` shader variable. Uses texture-based material sharing to enable GPU instancing batching.

2. **GPUInstancedAnimation** (`Scripts/GPUInstancedAnimation.cs`) - Core component rendering individual frame animations using `MaterialPropertyBlock`. Supports looping/non-looping, time scale independence, and editor preview mode.

3. **GPUFrameAnimator** (`Scripts/GPUFrameAnimator.cs`) - High-level controller managing multiple `GPUAnimationParam` entries with animation switching by name.

4. **GPUAnimFlash** (`Scripts/GPUAnimFlash.cs`) - Flash effect component requiring `GPUInstancedAnimation` for visual feedback (e.g., hit reactions).

### Shader Integration

The system uses a custom shader (`Resources/Shaders/GPUFrameAnimation.shader`) with GPU instancing support. Shader properties are synchronized via `MaterialPropertyBlock` in `GPUInstancedAnimation.UpdateProperties()`.

**Critical shader properties:**
- `_Columns/_Rows` - Atlas grid layout
- `_StartFrame/_TotalFrames/_FPS` - Animation parameters
- `_Loop/_IgnoreTimeScale` - Playback controls
- `_UnscaledTime` - Global time from `GPUAnimManager`
- `_IsEditorPreview` - Editor preview mode flag

### Key Design Patterns

- **Material Pooling**: Same texture → same material → GPU instancing batch
- **MaterialPropertyBlock**: Per-instance properties without material cloning
- **Event-Driven**: `OnPlayStart`, `OnPlayFinished` events on components

## Unity Development

### Opening the Project
```bash
# Open with Unity Hub (adjust version as needed)
unityhub --project "c:\WorkSpace\GPUFrameAnimation\gpuAnimation"
```

### Demo Scenes
- `Assets/GPUFrameAnimation/Demo/GpuFrameAnimationDemo.unity` - Main demo
- `Assets/GPUFrameAnimation/Demo/GpuFrameAnimatorDemo.unity` - Animator demo
- `Assets/GPUFrameAnimation/Demo/GpuFrameAnimationDemo2.unity` - Secondary demo

### Creating Animation Objects
Use the editor menu: `GameObject > GPUFrameAnimation > GPU Animation Entity`

### Assembly Definition
The code uses `GPUFrameAnimation.asmdef` for independent module compilation. All components are in the `GPUAnimation` namespace.

## Implementation Notes

- When modifying `GPUInstancedAnimation`, ensure shader property IDs match `Shader.PropertyToID()` calls
- The `UpdateProperties()` method must be called whenever animation parameters change
- Editor preview mode uses `_IsEditorPreview` shader flag
- Time scale independence requires both `IgnoreTimeScale=true` and `GPUAnimManager` updating `_UnscaledTime`
