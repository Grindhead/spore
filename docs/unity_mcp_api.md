# Unity MCP Server API (Cursor ↔ Unity)

The Unity MCP server runs inside the Unity Editor and listens on **http://localhost:7777/**. It allows Cursor (or any HTTP client) to drive the editor: create objects, generate prefabs, run builds, trigger Blender exports, and generate full playable levels with one command.

## Startup

- The server starts automatically when the Unity Editor loads (RoobsWorld project).
- Ensure tags **AI_Generated**, **AI_Enemy**, **AI_Loot** exist (run **RoobsWorld > Ensure Required Tags** once).

## Request Format

- **Method**: POST  
- **URL**: http://localhost:7777/  
- **Body**: JSON with `action` and `parameters` (see below).

## Actions

| Action | Description |
|--------|-------------|
| `create_object` | Create a GameObject with optional components and position. |
| `edit_object` | Move/find an existing object by name. |
| `generate_prefab` | Save a scene object as prefab; optional LOD levels. |
| `run_build` | Build the project (Windows/Mac/Linux/Android/iOS). |
| `trigger_blender_export` | Run Blender in background with a Python export script. |
| `scatter_objects` | Instantiate a prefab multiple times in a random area. |
| `generate_level` | Place prefabs in a chunked grid (optional LOD, auto-prefab). |
| `generate_full_level` | Like generate_level + clear AI_Generated, randomize rotation/scale, lighting, NavMesh. |
| `populate_level` | Spawn enemies and loot around every AI_Generated chunk; optional patrol routes. |
| `wire_events` | Add AISpawnTrigger components so enemies spawn when player enters zones. |
| **`generate_playable_dungeon`** | **Single command**: runs generate_full_level → populate_level → wire_events. |

## Single Command: generate_playable_dungeon

One JSON call builds a full playable level: terrain chunks, enemies, loot, spawn triggers, lighting.

### Example JSON

```json
{
  "action": "generate_playable_dungeon",
  "parameters": {
    "levelSize": { "x": 50, "y": 0, "z": 50 },
    "chunkSize": 10,
    "prefabs": ["Floor", "Wall", "PropTree"],
    "lodLevels": [0.7, 0.4, 0.1],
    "autoPrefab": false,
    "randomizeRotation": true,
    "randomizeScale": true,
    "scaleMin": 0.8,
    "scaleMax": 1.2,
    "setupLighting": true,
    "bakeNavMesh": false,
    "enemyPrefabs": ["Goblin", "Orc"],
    "enemyMinPerChunk": 1,
    "enemyMaxPerChunk": 3,
    "lootPrefabs": ["GoldCoin", "HealthPotion"],
    "lootMinPerChunk": 1,
    "lootMaxPerChunk": 2,
    "attachPatrol": true,
    "patrolRadius": 5,
    "patrolPoints": 3,
    "spawnOffset": { "x": 5, "y": 0, "z": 5 },
    "createEnemyTrigger": true,
    "createLootTrigger": true,
    "triggerRadius": 5
  }
}
```

### Requirements

- Prefabs must exist under **Assets/Prefabs/** (e.g. Floor.prefab, Goblin.prefab, GoldCoin.prefab).
- For `wire_events`, enemy prefabs are loaded and assigned to AISpawnTrigger components so they spawn at runtime when the player enters the trigger radius.

## Other Example Requests

### Create object with components

```json
{
  "action": "create_object",
  "parameters": {
    "name": "EnemySpawner",
    "componentTemplates": ["UnityEngine.BoxCollider", "UnityEngine.MeshFilter"],
    "position": { "x": 0, "y": 0, "z": 0 }
  }
}
```

### Generate prefab with LODs

```json
{
  "action": "generate_prefab",
  "parameters": {
    "name": "Tree",
    "lodLevels": [0.7, 0.4, 0.1]
  }
}
```

### Scatter prefabs

```json
{
  "action": "scatter_objects",
  "parameters": {
    "scatterPrefab": "Tree",
    "scatterCount": 50,
    "scatterArea": { "x": 50, "y": 0, "z": 50 }
  }
}
```

### Run build (e.g. Android)

```json
{
  "action": "run_build",
  "parameters": {
    "buildTarget": "Android",
    "buildPath": "Builds/Android/MyGame.apk"
  }
}
```

### Trigger Blender export

```json
{
  "action": "trigger_blender_export",
  "parameters": {
    "blenderPath": "C:/Program Files/Blender Foundation/Blender/blender.exe",
    "scriptPath": "tools/blender_scripts/export_for_unity.py"
  }
}
```

## Runtime Components (used by MCP)

- **PatrolRoute** (Scripts/World): waypoints for AI; populated by `populate_level` when `attachPatrol` is true.
- **AISpawnTrigger** (Scripts/World): when player enters radius, spawns assigned prefabs once; added by `wire_events`.
- **LootPickup** (Scripts/World): OnTriggerEnter with Player destroys object (pickup); added to loot by `populate_level`.

## Implementation

- **Server**: `RoobsWorld/Assets/Editor/UnityMCPServer.cs`
- **Tags**: `RoobsWorldProjectSetup` ensures Food, AI_Generated, AI_Enemy, AI_Loot.
- All Unity API calls are executed on the main thread via an editor update queue.
