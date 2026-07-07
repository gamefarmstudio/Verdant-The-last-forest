# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Verdant** is a Unity 2D game (URP 17.3.0) featuring wave-based enemy spawning, custom A* pathfinding, a direction-based enemy pathing system, a player character, and a shop economy. All scripts live in `Assets/Scripts/`.

## Unity Workflow

This project has no CLI build commands — all compilation, play-testing, and asset import happen inside the Unity Editor. Open the project by launching Unity Hub and selecting this folder.

- **Run the game:** Press Play in the Unity Editor with `Assets/Scenes/SampleScene` open.
- **Compile check:** Unity auto-compiles on file save. Check the Console window for errors.
- **Tests:** Use Unity Test Runner (Window > General > Test Runner).
- **Input asset:** Edit input bindings via `Assets/Settings/InputSystem_Actions.inputactions`. Unity regenerates `Input_system_controller.cs` automatically — **never edit that file by hand**.

## Architecture

### Enemy Pathing (direction-based, not navmesh)

Despite NavMeshPlus being installed, enemies use a manual direction-vector system:

1. **`EnemySpawner`** — holds a serialized list of enemy prefabs (`enemysequence`) and spawns them one-by-one on a timer. After instantiating a prefab it calls `AddComponent<Enemy>()` at runtime, so `Enemy` does **not** need to be on the prefab itself.
2. **`Enemy`** — moves `transform.Translate(currentDirection * speed * Time.deltaTime)` every frame. `currentDirection` is a public `Vector2` that anything can overwrite.
3. **`TurnPoint`** — scatter these GameObjects along the path in the scene. Each frame it calls `FindObjectsByType<Enemy>()`, checks proximity, then randomly picks one of its `possibleDirections[]` and assigns it to `enemy.currentDirection`. A `HashSet` prevents re-turning the same enemy instance. Yellow gizmo shows the `turnRadius`.

### Custom A* Pathfinding (Grid2D / Node2D / Pathfinding2D)

These three classes form a self-contained A* system that is separate from the enemy pathing above:

- **`Grid2D`** — placed on a GameObject, builds a 2D node grid at `Awake`. Walkable nodes are determined by checking whether each node's world position falls inside the `Bounds` of any `SpriteRenderer` tagged `"Path"`. Uses 4-way movement only (Manhattan distance).
- **`Node2D`** — plain data class (not a MonoBehaviour): `walkable`, `worldPosition`, `gridX/Y`, A* costs (`gCost`, `hCost`, `fCost`), and `parent`.
- **`Pathfinding2D`** — requires `Grid2D` on the same GameObject. Call `FindPath(startPos, targetPos)` to get a `List<Node2D>` or `null` if no path exists. The open set is a plain `List` (linear scan), so it is not optimised for large grids.

### Shop System

- **`ShopItemData`** — a `ScriptableObject` (create via `Assets > Shop > Item`). Fields: `itemName`, `price`, `icon`, `description`.
- **`ShopManager`** — singleton (`ShopManager.Instance`). Call `TryBuyItem(ShopItemData item)` to deduct gold and trigger `UnlockPurchasedItem` (stub — wire up inventory logic there). Starting gold is 500.

### Player

`Player` uses `Rigidbody2D.MovePosition` in `FixedUpdate` for physics-correct movement. Input is polled each `Update` via the generated `InputSystem_Actions` wrapper (`inputSystem.Player.Move.ReadValue<Vector2>()`). Facing direction is set by flipping `transform.localScale.x`. The `Animator` parameter `"IsRunning"` drives the run/idle transition.

### Input System

`Input_system_controller.cs` is auto-generated from `Assets/Settings/InputSystem_Actions.inputactions`. Player uses `InputSystem_Actions` (note: `Player.cs` instantiates `new InputSystem_Actions()`, not `new Input_system_controller()`). Both refer to the same underlying asset. Player action map bindings: WASD/arrows + gamepad left stick for Move; left mouse / gamepad West for Attack; E / gamepad North for Interact (Hold); Space / gamepad South for Jump; Shift for Sprint.

## Key Gotchas

- `TurnPoint.Update()` calls `FindObjectsByType<Enemy>()` every frame — performance will degrade with large enemy counts. Cache or use a subscription pattern if this becomes a bottleneck.
- `EnemySpawner` adds `Enemy` via `AddComponent` after `Instantiate`, so enemy prefabs should **not** already have an `Enemy` component attached (it would be duplicated).
- `Grid2D` reads walkable tiles by checking sprite `Bounds` at `Awake`. If tilemap tiles are added at runtime after `Awake`, the grid will not reflect them without a manual `CreateGrid()` call.
- `BoostBrightness.Boost()` is a `[ContextMenu]` utility — call it from the Inspector right-click menu on the component, not from game code.
