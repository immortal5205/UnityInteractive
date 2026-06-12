# Unity Interactive

An interactive system framework based on Unity, providing unified event handling and interaction case management.

## Features

- **Unified Interaction Interface**: Defines three interaction types: `IDraggable` (drag), `IFocusable` (focus), and `ISelectable` (select).
- **Input Abstraction Layer**: `EasyInput` supports both mouse and touch input with automatic device detection.
- **Interaction Case System**: Decouples interaction logic through `IInteractCase` and attribute-driven registration.
- **Singleton Management**: `UnityInteractive` provides global interaction state management and event dispatching.
- **Editor Integration**: Offers Unity Editor extensions for enhanced workflow.

## Core Interfaces

| Interface | Description |
|-----------|-------------|
| `IInteractive` | Base interaction interface |
| `IDraggable` | Drag interaction, supporting start, ongoing, and end events |
| `IFocusable` | Focus interaction, supporting focus enter and exit events |
| `ISelectable` | Selection interaction, triggered by click |

## Usage

### 1. Create an Interactive Component

Inherit from `InteractiveComponent` and override the required methods:

```csharp
using UnityEngine;
using NuoYan.Interactive;

public class DraggableItem : InteractiveComponent
{
    protected override void OnStartDrag(PointerEventData eventData)
    {
        // Logic when drag starts
    }

    protected override void OnUpdateDrag(PointerEventData eventData)
    {
        // Logic during drag
    }

    protected override void OnStopDrag(PointerEventData eventData)
    {
        // Logic when drag ends
    }
}
```

### 2. Define Interaction Cases

Register interaction cases using the `InteractCaseAttribute`:

```csharp
using UnityEngine;
using NuoYan.Interactive;

[InteractCase(typeof(DraggableItem), typeof(FocusTarget))]
public class ItemToTargetInteractCase : DragSubjectFocusTargetInteractCase
{
    public ItemToTargetInteractCase() 
        : base(typeof(DraggableItem), typeof(FocusTarget)) { }

    protected override void OnExecute(IDraggable subject, IFocusable target)
    {
        // Handle interaction when dragging onto a focus target
    }

    protected override void OnEnter(IDraggable subject, IFocusable target)
    {
        // Callback when entering the target
    }

    protected override void OnExit()
    {
        // Callback when leaving the target
    }
}
```

### 3. Configuration Steps

1. Add the `UnityInteractive` singleton to your scene.
2. Attach appropriate subclasses of `InteractiveComponent` to objects requiring interaction.
3. Ensure interactive objects have a `Collider` component for raycast detection.
4. Run the game—interaction events will be triggered automatically.

## Project Structure

```
NuoYan.Interactive/
├── Runtime/
│   ├── EasyInput.cs          # Input abstraction layer
│   ├── IInteractive.cs       # Interface definitions
│   ├── InteractiveComponent.cs  # Base interactive component class
│   ├── AbstractInteractCase.cs   # Base interaction case class
│   └── UnityInteractive.cs       # Core manager
└── Editor/
    └── UnityInteractiveEditor.cs  # Editor extension
```

## Requirements

- Unity 2019.4 or higher
- .NET Standard 2.0+

## Open Source License

MIT License