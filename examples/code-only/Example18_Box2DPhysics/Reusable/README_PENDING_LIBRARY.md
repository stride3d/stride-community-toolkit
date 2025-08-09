# Reusable Box2D Wrapper (Extraction Candidate)

This folder contains early scaffolding of code that is intended to become a generic, reusable
Box2D integration layer for the Stride Community Toolkit. Nothing here is wired into the
example yet; these files exist so that the eventual extraction to a standalone library/project
is a simple move operation.

Goals for the reusable layer:

1. Keep the core physics world wrapper free of Stride `Entity` / `Component` types
2. Provide a slim API surface for:
   - World lifecycle (create / dispose)
   - Stepping with fixed timestep logic
   - Gravity and time‑scaling adjustments
   - Query helpers (raycasts, overlaps)
   - Event dispatch abstraction (contact + sensor) decoupled from rendering/game objects
3. Bridge Stride specifics (entity transform sync, body mapping) via a separate adapter class
4. Remain allocation‑lean and educational (clear XML docs & small examples)

Current files (placeholders):

| File | Purpose |
|------|---------|
| Core/PhysicsWorld2D.cs | Core world + stepping (no Stride references) |
| Core/PhysicsStepSettings.cs | Configuration record for stepping parameters |
| Events/ContactEvents.cs | Event data & interfaces (namespaced to avoid clashes) |
| Events/SensorEvents.cs | Sensor event data & interfaces |
| Queries/PhysicsQueries2D.cs | Raycast & overlap query helpers (stateless wrapper) |

Migration Plan (High Level):

Phase A: Extract query + event logic from `Box2DSimulation` into these placeholders.
Phase B: Split `Box2DSimulation` into:
  - `PhysicsWorld2D` (generic)
  - `Box2DStrideBridge` (entity mapping + transform sync; will stay in the example initially)
Phase C: Update example code to use the bridge + generic world wrapper.
Phase D: Move this folder into a new toolkit project (e.g. `Stride.CommunityToolkit.Box2D`).

NOTE: Existing comments in the original files will be preserved until final cleanup.

// TODO: Populate these placeholders during refactor phases (do not delete this README until extraction is complete).
