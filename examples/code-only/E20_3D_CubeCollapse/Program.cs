using CubeCollapse;
using Stride.CommunityToolkit.Engine;
using Stride.Engine;

using var game = new Game();

var cubeCollapse = new CubeCollapseGame(game);

game.Run(start: cubeCollapse.Start, update: cubeCollapse.Update);

/*
---example-metadata
slug: cube-collapse
title:
  en: Game - Cube Collapse
  cs: Hra - Cube Collapse
level: Advanced
category: Game
complexity: 5
order: 180
description:
  en: |-
    A colour-match collapse puzzle built entirely from code. A platform of cubes, 5x5x5 at level one and growing to 10x10x10, builds
    itself one layer at a time, clicking a cube clears every same-coloured cube connected to it, and
    what is left above drops into the gap.
    Shows how to structure a whole game without the editor: scene setup split from gameplay, a custom
    Bepu body that constrains cubes to their own column, screen-space text drawn without the UI
    system, and mouse picking through a physics raycast.
  cs: |-
    Logická hra na skládání barev postavená výhradně v kódu. Platforma z kostek, 5x5x5 v první úrovni a postupně až 10x10x10, se staví
    po vrstvách, kliknutí na kostku odstraní všechny propojené kostky stejné barvy a to, co zůstane
    nad nimi, spadne do vzniklé mezery.
    Ukazuje, jak strukturovat celou hru bez editoru: oddělení stavby scény od herní logiky, vlastní
    Bepu těleso držící kostku v jejím sloupci, text v prostoru obrazovky bez UI systému a výběr
    myší pomocí fyzikálního raycastu.
concepts:
  - Structuring a code-only game into setup, components and scripts
  - Constraining a Bepu body to one axis with a custom BodyComponent
  - Locking rotation by zeroing the whole inverse inertia tensor
  - Raising the solver substep count to settle a rotation-locked stack
  - Picking entities with a camera raycast from the mouse
  - "Drawing screen-space text without the UI system: EntityTextComponent"
  - Flood filling a grid to find connected same-coloured neighbours
  - "Using helpers: Add3DCamera, Add3DGround, AddGizmo, Create3DPrimitive"
tags:
  - 3D
  - Bepu
  - Game
  - Physics
  - Raycast
  - Mouse Picking
  - Rigid Body
  - Inertia
  - Text Rendering
  - Materials
  - Lighting
related:
  - E09_3D_SceneRenderer
  - E07_3D_SimpleGeometry
enabled: true
created: 2025-04-20
---
*/