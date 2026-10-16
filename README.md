# Building a VR Digital Twin of the Tormach PCNC 440 CNC Milling Machine in Unity

## Overview

This tutorial walks through the process of creating an interactive VR digital twin of the Tormach PCNC 440 CNC milling machine using Unity 6.3 LTS with the Universal Render Pipeline (URP), XR Interaction Toolkit 3.3.1, and the XR Device Simulator for desktop-based VR testing. The final result is a fully textured 3D model of the PCNC 440 that users can interact with in VR -- pointing at the machine and pressing grip triggers a realistic demo sequence where the doors swing open and close, the spindle plunges down, the table shifts to simulate cutting, and everything resets.

## What's in this repo

- `tormachpcnc440.unitypackage` -- The Unity asset package containing the PCNC 440 prefab, materials, and scripts.
- `Scripts/` -- Extracted C# source scripts from the package:
  - `Scripts/Editor/PCNC440MaterialTagger.cs` -- Unity editor window that walks through 180+ mesh bodies on the PCNC 440 model and assigns URP/Built-in materials by category (gray panel, glass door, dark interior, decal, dark trim, machine parts).
  - `Scripts/PCNC440Controller.cs` -- Main controller script (class name `PCNC440Controllerv3`). XR grip triggers a state-machine demo sequence: doors open, spindle plunges, table moves to simulate cutting, then everything resets. Final version.
  - `Scripts/PCNC440DoorController.cs` -- Simpler door-only controller. XR grip toggles the enclosure doors open and closed.

## Prerequisites

- **Unity 6.3 LTS** (or later) with Universal Render Pipeline
- **XR Interaction Toolkit 3.3.1** (installed via Package Manager)
- **XR Device Simulator** sample (imported from XR Interaction Toolkit samples)
- **Starter Assets** sample (imported from XR Interaction Toolkit samples)
- A 3D model of the Tormach PCNC 440 (e.g., from GrabCAD, exported as .fbx or .obj)

## Part 1: Importing the Model

1. Download or obtain a 3D model of the Tormach PCNC 440. Common sources include GrabCAD or university-provided CAD files.
2. Import the model into Unity by dragging the file into your `Assets` folder in the Project panel.
3. Drag the model from the Project panel into the Hierarchy to place it in your scene.
4. Position and rotate the model as needed. In this project, the machine was placed at Position (-0.918, 0.45, 6.031) with a Y rotation of 90 degrees to align with the makerspace layout.

## Part 2: Applying Materials with the Material Tagger

The imported model arrives with default materials and no texturing. The PCNC 440 has several distinct surface types -- gray metal panels, transparent glass doors, darker interior surfaces, and mechanical parts. Rather than manually assigning materials to 180+ individual mesh bodies, we built a custom Unity Editor tool to streamline the process.

### Setting Up the Material Tagger

1. Create an `Editor` folder inside `Assets` (this is required for Editor scripts).
2. Copy `Scripts/Editor/PCNC440MaterialTagger.cs` from this repo into `Assets/Editor` in your Unity project.
3. In Unity, go to **Tools → PCNC 440 Material Tagger** to open the tool window.

### Using the Material Tagger

1. Drag your PCNC 440 root object from the Hierarchy into the **Root Object** field.
2. Click **Scan for Bodies** -- the tool will find all mesh children (in this project, 182 bodies were found).
3. The tool selects and zooms to each body in the Scene view one at a time. For each body, click one of the category buttons:
   - **Gray Panel** -- Light gray matte metal (body panels, cabinet, screws, legs)
   - **Glass Door** -- Transparent polycarbonate enclosure doors
   - **Dark Interior** -- Darker surfaces visible inside the glass enclosure
   - **Decal / Logo** -- Side panels with "PCNC 440" and "tormach.com" branding
   - **Dark Trim** -- Handles, step stool, dark edges
   - **Machine Parts** -- Spindle, vise, internal mechanical components
   - **Skip** -- Parts that don't need a material
4. After tagging the distinctive parts (glass, interior, machine parts), use the **"Tag ALL remaining as Gray Panel"** button to bulk-assign the majority of bodies.
5. Click the green **"Apply All Materials"** button to generate and assign all materials.

### Material Properties

The script auto-detects URP vs Built-in rendering and creates materials with these properties:

| Material | Base Color | Metallic | Smoothness | Notes |
|----------|-----------|----------|------------|-------|
| Gray Panel | RGB(0.69, 0.69, 0.69) | 0.45 | 0.3 | Matte metal finish |
| Glass Door | RGB(0.85, 0.92, 1.0), 12% alpha | 0.1 | 0.88 | Transparent surface type |
| Dark Interior | RGB(0.22, 0.22, 0.25) | 0.5 | 0.35 | Visible behind glass |
| Decal Panel | RGB(0.52, 0.52, 0.55) | 0.4 | 0.25 | For branded side panels |
| Dark Trim | RGB(0.12, 0.12, 0.12) | 0.3 | 0.2 | Handles, dark accents |
| Machine Parts | RGB(0.48, 0.48, 0.50) | 0.7 | 0.5 | Shiny machined metal |

Materials are saved to `Assets/PCNC440_Materials/` and can be tweaked in the Inspector after creation.

### Fixing Individual Bodies

If a body is tagged incorrectly:

1. Click the body in the Hierarchy -- it highlights in the Scene view.
2. In the Inspector, expand **Materials** under the **Mesh Renderer** component.
3. Click the circle next to the material name to open the material picker.
4. Search for "PCNC440" and select the correct material.

## Part 3: Setting Up Animatable Parts with Pivot Objects

The imported model has all bodies at Position (0, 0, 0) with geometry offsets baked into the mesh data. This means you cannot simply move a body's transform to animate it -- you need to create parent "pivot" objects positioned at the correct hinge/center points.

### Unpacking the Prefab

Before reparenting any bodies, you must unpack the prefab:

1. Right-click the PCNC 440 root object in the Hierarchy.
2. Select **Prefab → Unpack Completely**.

This breaks the prefab connection but allows you to freely rearrange the hierarchy.

### Creating Pivot Objects

For each moving part, create an empty GameObject as a child of the PCNC 440 root, position it at the correct pivot point, then drag the body mesh onto it as a child.

**Left Door Pivot:**
1. Right-click the PCNC 440 → **Create Empty** → name it `LeftDoorPivot`.
2. Position it at the left door's hinge edge -- centered vertically between the top and bottom hinges, at the left edge of the door panel.
3. Drag `Body1:159` onto `LeftDoorPivot` as a child.

**Right Door Pivot:**
1. Create Empty → name it `RightDoorPivot`.
2. Position it at the right door's hinge edge (mirror of left door position).
3. Drag `Body1:160` onto `RightDoorPivot` as a child.

**Spindle Pivot:**
1. Create Empty → name it `SpindlePivot`.
2. Position it near the center of the spindle head.
3. Drag `Body1:119` onto `SpindlePivot` as a child.

**Table Pivot:**
1. Create Empty → name it `TablePivot`.
2. Position it near the center of the table/bed.
3. Drag `Body1:100` onto `TablePivot` as a child.

### Critical: Uncheck Static

Any body that needs to move at runtime must have the **Static** checkbox unchecked in the Inspector (top-right corner). Static meshes are baked for performance and will not move during Play mode. Uncheck Static on:
- Body1:159 (left door)
- Body1:160 (right door)
- Body1:119 (spindle)
- Body1:100 (table)

### Testing Pivots

Verify each pivot works by manually changing its transform values:

- **LeftDoorPivot**: Change Rotation to (-20, 110, 15) -- the door should swing open on its hinge. The door swing requires rotation on all three axes due to the machine's orientation in the scene.
- **RightDoorPivot**: Change Rotation to (-20, -110, -15) -- mirror of left door.
- **SpindlePivot**: Change Position Y to a negative value -- the spindle should move down.
- **TablePivot**: Change Position X -- the table should shift sideways. Change Position Z -- the table should shift forward/backward.

Reset all values after testing.

## Part 4: Setting Up XR Interaction

### Making the Machine Interactable

The PCNC 440 root object needs three components for XR interaction:

1. **Box Collider** -- Defines the clickable area. Add via Add Component, then adjust Center and Size so the green wireframe covers the entire machine in Scene view.
2. **Rigidbody** -- Required by XR interactables. Set `Use Gravity` to off and `Is Kinematic` to on.
3. **XR Simple Interactable** -- Responds to controller select events (grip button). The Interaction Layer Mask should be set to `Default`.

Make sure the PCNC 440's Layer (top of Inspector) is set to **Default**, not "Ignore Raycast".

## Part 5: The Controller Script

### Script Setup

1. Create a `Scripts` folder inside `Assets`.
2. Copy `Scripts/PCNC440Controller.cs` from this repo into `Assets/Scripts` in your Unity project (the file is named `PCNC440Controller.cs` but the class inside is `PCNC440Controllerv3`).
3. Drag the script onto the PCNC 440 root object.
4. In the Inspector, assign all four pivot references: `LeftDoorPivot`, `RightDoorPivot`, `SpindlePivot`, `TablePivot`.

### How the Script Works

The script uses Unity's `XRSimpleInteractable` component to detect when a VR controller selects (grips) the machine. When triggered, it runs a step-by-step demo sequence using a state machine pattern.

**Key concepts:**

- **`AddListener`**: Connects the XR select event to a callback method. When the player grips the machine, `selectEntered` fires and calls `OnMachineSelected()`.
- **State machine**: The `step` variable tracks which phase of the demo is active. Each `Update()` frame checks the current step and performs the appropriate animation.
- **`Quaternion.Slerp`**: Smoothly interpolates between door rotations (closed → open) over time.
- **`Vector3.MoveTowards`**: Moves the spindle and table at a fixed speed per frame toward a target position, which is more reliable than `Lerp` for small distances.

### Demo Sequence

| Step | Action | Duration |
|------|--------|----------|
| 1 | Doors swing open | 3 seconds |
| 2 | Doors close (simulating workpiece loading) | 3 seconds |
| 3 | Spindle plunges down | 3 seconds |
| 4 | Table shifts right (cutting) | 2 seconds |
| 5 | Table returns to center | 2 seconds |
| 6 | Spindle raises back up | 3 seconds |
| 7 | Doors swing open (job complete) | 3 seconds |
| 8 | Doors close and reset | 3 seconds |

After the sequence completes, the user can grip the machine again to replay. The script resets all positions and rotations before each replay.

### Adjustable Parameters

All animation parameters are exposed in the Inspector:

- **Left/Right Door Open Rotation**: Euler angles for the fully open position. For this model: (-20, 110, 15) and (-20, -110, -15).
- **Door Speed**: How fast doors animate (default: 3).
- **Spindle Plunge Distance**: How far the spindle moves down (default: 0.1, adjust based on model scale).
- **Spindle Speed**: Movement speed for the spindle (default: 0.2).
- **Table Shift Distance**: How far the table moves sideways (default: 0.05).
- **Table Speed**: Movement speed for the table (default: 0.2).

## Part 6: Testing with the XR Device Simulator

1. Press Play in Unity.
2. Click inside the Game view to ensure it has focus.
3. Use WASD to move toward the machine.
4. Hold right mouse button and move the mouse to look around.
5. Press T to toggle between left and right controller.
6. Aim the controller ray at the machine.
7. Press G (grip) to start the demo sequence.
8. After the demo completes, grip again to replay.

## Part 7: Exporting as a Package

To share the project with teammates or upload to a repository:

1. Create a `Prefabs` folder in Assets.
2. Drag the PCNC 440 root object from the Hierarchy into the Prefabs folder to create a prefab.
3. Right-click the prefab → **Export As Asset Package**.
4. Ensure all dependencies are checked (materials, scripts, model files).
5. Click Export and save the `.unitypackage` file.

Teammates can import via **Assets → Import Package → Custom Package**.
