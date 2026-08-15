# Subway Surfers — Rebuilt

A complete, production-quality endless runner built procedurally in Unity 6.
No art assets. No prefabs. Everything is code.

## What Changed

This project was rebuilt from a single 1,074-line monolith into a clean,
subsystem-based architecture. The old `EndlessSubwayRunner.cs` handled input,
physics, UI, spawning, scoring, and settings in one class. That is gone.

### Before → After

| Before | After |
|--------|-------|
| 1 monolithic script (1,074 lines) | 18 focused scripts across 8 namespaces |
| Raw Instantiate/Destroy every frame | Pooled chunks, obstacles, coins, powerups |
| No jump/roll feedback | Snappy jump, roll, lane lean, camera FOV kick |
| No obstacles variety | Trains, barriers, overheads, side buildings |
| No scoring depth | Distance + coin combo multiplier + 2x powerup |
| No audio | Procedural SFX + generative music loop |
| No effects | Pooled particle bursts for every interaction |
| Best score lost on kill | SaveSystem flushes on pause/quit |
| Duplicate pause buttons | Single UI state machine |
| Ground detection by name | Trigger-based collision on tagged layers |

## Architecture

```
Assets/Scripts/
├── Core/
│   ├── Game.cs              ← bootstrap + state machine + service locator
│   ├── GameConfig.cs        ← all tuning constants & palette
│   ├── GameObjectPool.cs    ← root-level pool (survives restarts)
│   ├── InputReader.cs       ← keyboard + swipe, one update loop
│   ├── SaveSystem.cs        ← persistence, flushes on pause/quit
│   ├── ScoreSystem.cs       ← distance + coin combo
│   └── PowerupSystem.cs     ← magnet, shield, 2x score timers
├── Player/
│   └── PlayerController.cs  ← lanes, jump, roll, lean, death
├── Track/
│   ├── TrackSystem.cs       ← endless spawner / recycler
│   ├── TrackChunk.cs        ← one 18m chunk: floor, rails, obstacles, coins
│   ├── Obstacle.cs          ← trains, barriers, overheads
│   ├── Coin.cs              ← magnet attraction + spin
│   └── PowerupPickup.cs     ← bobbing, grants powerups
├── Presentation/
│   └── CameraController.cs  ← chase cam, FOV kick, shake
├── Effects/
│   └── EffectsSystem.cs     ← pooled particle bursts
├── Audio/
│   └── AudioSystem.cs       ← procedural SFX + generative music
├── UI/
│   └── UISystem.cs          ← HUD, panels, settings, mobile controls
└── Editor/
    └── CompileCheck.cs      ← one-click compile verification
```

## How to Run

1. Open the project in Unity 6000.5.4f1.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.

The game bootstraps itself via `[RuntimeInitializeOnLoadMethod]` — no scene
setup needed.

## Controls

| Input | Action |
|-------|--------|
| A / ← | Move left |
| D / → | Move right |
| Space / W / ↑ | Jump |
| S / ↓ | Roll |
| Esc | Pause |
| R | Restart (game over) |
| Swipe ← → | Change lane |
| Swipe ↑ | Jump |
| Swipe ↓ | Roll |

## Verification

All 18 C# files compile cleanly against Unity 6000.5.4f1 managed assemblies:

```
EXIT: 0
TOTAL: 0 errors
```

To verify in-editor: **Tools → Compile Check**.
