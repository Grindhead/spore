# Balance & Performance (Phase 8)

Cell-stage tuning levers and performance notes. Use during playtests and the Phase 8 balance pass.

After changing any `BalanceConstants` value, run **Window → General → Test Runner → Run All** (EditMode) to confirm `BalanceConstantsTests` still pass (they enforce sanity constraints such as safe radius > threat radius and cost ≤ threshold).

---

## Balance — single source (BalanceConstants.cs)

All in **`Assets/Scripts/Core/BalanceConstants.cs`**. Rebuild after changes.

### Progression

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellEvolutionMassThresholdBase` | Mass required to trigger first evolution tier (scaled by `EvolutionThresholdMult` per difficulty) | **10** |
| `CellPlayerReproductionMassThreshold` | Player mass to trigger reproduction / mutation choice (set lower than evolution so reproduction comes first) | **6** |
| `CellPlayerReproductionMassCost` | Mass deducted from player after reproducing | **3** |
| `CellFoodNutritionMultiplier` | Global multiplier on food nutrition when consumed by player | **1.0** |
| `CellMassToScaleFactor` | Visual scale per mass unit | **0.05** |

### Rewards

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellDNAPointsPerFood` | DNA per food item consumed | **1** |
| `CellDNAPointsPerCellConsume` | DNA per NPC cell consumed | **5** |
| `CellDNAPointsPerEvolutionChoice` | DNA awarded when player picks a part at evolution | **5** |
| `CellDNAPointsPerGoalComplete` | DNA per micro-goal complete (scaled by `DNARewardMultiplier` per difficulty) | **3** |

### AI behaviour (NPCs)

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellBehaviourEnergyLowThreshold` | Below this energy, AI seeks food | **30** |
| `CellReproductionEnergyThreshold` | Energy required for NPC reproduction | **60** |
| `CellReproductionEnergyCost` | Energy cost per NPC reproduction | **40** |
| `CellOffspringInitialEnergy` | Energy given to newborn offspring | **30** |

### Species & evolution

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellSpeciationGeneticDistanceThreshold` | Sum of trait differences to branch a new species (higher = rarer, more meaningful) | **8** |

### Goals

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellGoalPredatorThreatRadius` | Distance at which player is considered "threatened" (EscapePredator goal starts) | **18** |
| `CellGoalPredatorSafeRadius` | Distance at which player counts as "escaped" (must be > ThreatRadius for hysteresis) | **28** |

### Biomes

Radius fractions are proportions of arena radius (0–1), measured from centre.

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `CellBiomeDeepZoneMaxRadiusFraction` | Inner edge of Open Water zone | **0.2** |
| `CellBiomeOpenWaterMaxRadiusFraction` | Inner edge of Algae Forest zone | **0.6** |
| `CellBiomeAlgaeForestMaxRadiusFraction` | Outer edge of Algae Forest zone | **0.9** |
| `CellBiomeToxicZoneCenterOffsetFraction` | Toxic zone centre offset from arena centre (X and Z) | **0.4** |
| `CellBiomeToxicZoneRadiusFraction` | Toxic zone radius fraction | **0.2** |
| `CellBiomeMetabolismMultiplier*` | Deep **0.7**, OpenWater **1.0**, AlgaeForest **1.0**, Toxic **1.5** | — |
| `CellBiomeSpeedMultiplier*` | Deep **0.8**, OpenWater **1.0**, AlgaeForest **1.0**, Toxic **0.85** | — |
| `CellBiomeFoodEfficiencyMultiplierToxic` | Food nutrition penalty in toxic zone | **0.7** |

### Genetic traits

| Constant | Purpose | Current value |
|----------|---------|---------------|
| `GeneticTraitPhotosyntheticEnergyRate` | Energy/s gained in light biomes (Photosynthetic) | **3** |
| `GeneticTraitToxicContactDrain` | Energy/s drained from target by Toxic contact | **4** |
| `GeneticTraitArmouredDamageReduction` | Fraction of incoming damage absorbed (Armoured) | **0.35** |
| `GeneticTraitRegenerativeRate` | Energy/s regenerated when energy > 10 (Regenerative) | **1.5** |
| `GeneticTraitElectricPulseRadius` | Stun pulse radius (Electric) | **4** |
| `GeneticTraitElectricPulseInterval` | Seconds between Electric pulses | **3** |
| `GeneticTraitElectricSlowDuration` | Seconds shocked organisms are slowed | **1.2** |

---

## Balance — Inspector (CellStage, FoodManager)

Tunable per-scene without code changes.

| Component | Field | Purpose | Typical |
|-----------|-------|---------|---------|
| **CellStage** | `_arenaRadius` | Arena radius in world units | **50** |
| | `_initialOrganismCount` | Evolvable NPC organisms | **15** |
| | `_initialNPCCount` | Legacy NPC cells | **20** |
| | `_initialPartPickupCount` | Part pickups | **8** |
| | `_predatorCount` | PredatorCell hazards | **3** |
| | `_toxinCloudCount` | ToxinCloud hazards | **5** |
| | `_currentZoneCount` | ArenaCurrent zones | **3** |
| **FoodManager** | `_algaeCount` / `_bacteriaCount` / `_deadCellCount` | Food spawn counts | **60 / 30 / 10** |
| | `_algaeNutrition` / `_bacteriaNutrition` / `_deadCellNutrition` | Nutrition per type | **5 / 8 / 10** |

Lower organism/food counts = easier on CPU and simpler arena. Higher = more crowded and more evolution activity.

---

## Difficulty multipliers (DifficultyModifiers)

Chosen at New Game via `NewGameOptionsUI`. Applied at runtime; do not change `BalanceConstants` to tune difficulty — use these multipliers instead.

| Multiplier | Easy | Normal | Hard |
|------------|------|--------|------|
| `FoodCountMultiplier` | 1.5× | 1.0× | 0.6× |
| `PredatorCountMultiplier` | 0.5× | 1.0× | 1.5× |
| `NPCCountMultiplier` | 1.2× | 1.0× | 0.8× |
| `MetabolismMultiplier` | 0.7× | 1.0× | 1.3× |
| `EvolutionThresholdMult` | 0.8× | 1.0× | 1.2× |
| `DNARewardMultiplier` | 1.2× | 1.0× | 0.8× |

---

## Performance levers (BalanceConstants)

| Constant | Effect | Current value |
|----------|--------|---------------|
| `CellAITickIntervalSeconds` | Seconds between AI decisions per organism. **Larger = fewer updates, better FPS.** | **0.2** |
| `CellSpatialGridCellSize` | Grid bucket size for food/organism lookup. **Larger = fewer buckets, faster queries, less precision.** | **5** |

**Benchmark baseline** (`CellSpatialGridPerformanceTests`, EditMode):
- Bulk register 700 entries: < 50 ms
- 500-organism AI tick (each `GetNearestInRadius`): < 50 ms
- 500 position updates: < 30 ms

These are CI-safe upper bounds, not targets. Real profiling target is < 1 ms per AI tick frame at 60 FPS (Unity Profiler, deep profile, target hardware).

**LOD:** `OrganismLOD` disables spike/flagella GameObjects beyond 30 units from camera (0.25 s staggered check). Adjust `LodDistance` in the component if the visual pop-in is noticeable.

**Pooling:** Organisms use `SetPoolCallback` / `Reinitialize`; the pool sits in `CellStage`. Verify pool size ≥ peak live organism count to avoid instantiate spikes.

---

## Recommended playtest tuning order

1. **Progression feel** — evolution threshold, reproduction threshold/cost, speciation distance.
2. **Reward loop** — DNA per food/cell/goal; confirm DNA accumulates at a satisfying rate.
3. **Difficulty** — predator threat/safe radii; hazard counts in Inspector; difficulty multipliers.
4. **Ecosystem health** — organism/food counts; NPC energy thresholds; reproduction energy.
5. **Performance** — AI tick interval, grid cell size, spawn counts if frame time spikes.

---

## Reference

- `Assets/Scripts/Core/BalanceConstants.cs` — all compile-time constants
- `Assets/Tests/EditMode/BalanceConstantsTests.cs` — sanity guards; run after each change
- `docs/PLAYTEST_LOG_TEMPLATE.md` — session recording template
- `docs/PLAYTEST_CHECKLIST.md` — pre-playtest checklist
- `memory-bank/techContext.md` — frame budget, memory targets, Burst notes
