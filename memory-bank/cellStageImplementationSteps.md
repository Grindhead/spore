# Cell-Stage Implementation Steps — 10-Step Roadmap

Sequential implementation roadmap for the evolving cell game. Each step has a goal, concrete tasks, and test criteria. Aligns with **cellStageUnityLayout.md**, **evolutionSimulationDesign.md**, **cellStageOrganismDesign.md**, and **evolutionPrinciples.md**.

---

## Step 1: Core Organism & Genome System

**Goal:** Have a working organism with traits and energy.

### Tasks

- **Create Genome.cs**
  - Traits: speed, size, attack, defence, digestion, metabolism
  - Visual genes: spikes, flagella, color
- **Create Organism.cs**
  - Holds Genome, energy, age, position, speciesId
  - Methods: Die(), Move(), Eat()
- **Test** a single organism in the scene moving around.

**Pro tip:** Use a simple sphere with scale and color to visualize size and color traits initially.

---

## Step 2: Basic AI & Movement

**Goal:** Organisms move and respond to the environment.

### Tasks

- **Create Movement.cs**
  - Apply speed from genome
  - Add small wobble/pulsation
- **Create Behaviour.cs**
  - Decision-making logic (SeekFood, Flee, Attack, Wander)
  - Use thresholds for energy levels
- **Test** multiple organisms moving and reacting in a small test environment.

---

## Step 3: Food & Energy System

**Goal:** Organisms can eat and gain/lose energy.

### Tasks

- **Create FoodManager.cs**
  - Spawn algae, bacteria, and dead cells
  - Keep track of positions for AI
- **Update Organism.cs**
  - Eating increases energy
  - Movement/metabolism decreases energy
- **Test** energy depletion, feeding, and death.

---

## Step 4: Reproduction & Mutation

**Goal:** Organisms reproduce with slight mutations.

### Tasks

- **Create ReproductionSystem.cs**
  - Spawn new organism when energy ≥ threshold
  - Reduce parent energy
- **Create MutationSystem.cs**
  - Small random changes to genome (traits + visuals)
  - Optional: player chooses between a few mutation variants
- **Test** population growth and trait variation over a few generations.

---

## Step 5: Species Tracking & Speciation

**Goal:** Track evolution and create new species.

### Tasks

- **Create Species.cs**
  - speciesId, parentSpeciesId, baseGenome, population
- **Create SpeciationSystem.cs**
  - Compare genomes
  - If genetic distance > threshold → new species
- **Test** lineage tracking and verify new species emerge naturally.

---

## Step 6: Environment & Biomes

**Goal:** Add environmental pressure that affects evolution.

### Tasks

- **Create Biome.cs**
  - Define zones: algae forest, open water, toxic zone, deep zone
  - Apply bonuses/penalties to traits
- **Add environmental events:**
  - Nutrient bloom
  - Virus outbreak
  - Temperature changes
- **Test** migration between zones and adaptive trait changes.

---

## Step 7: Procedural Visuals ✅ Done

**Goal:** Organisms visually represent their genome.

### Tasks

- **Create ProceduralCellGenerator.cs** ✅
  - Map size, spikes, flagella, color → visuals (body + spike children + flagella children)
  - Optional: add soft-body shader or particle effects for flagella/spikes (deferred)
- **Test** visually distinct organisms for different mutations. ✅ ProceduralCellGeneratorTests (EditMode)

---

## Step 8: Phylogenetic Tree & UI ✅ Done

**Goal:** Track evolution and display it to the player.

### Tasks

- **Create PhylogeneticTreeUI.cs** ✅
  - Show species branching (indent by depth, sorted)
  - Distinguish extinct vs active species (extinct suffix, population)
- **Create EvolutionStatsUI.cs** ✅
  - Show total organisms, species count, active vs extinct
  - FormatTraits(Genome) for optional selected-organism display (Step 10)
- **Test** UI updates in real-time as the ecosystem evolves. ✅ PhylogeneticTreeAndEvolutionStatsUITests (EditMode): FormatTreeLines, FormatSummary, FormatTraits.

---

## Step 9: Scaling & Performance ✅ Done (spatial grid + AI tick)

**Goal:** Run hundreds to thousands of organisms smoothly.

### Tasks

- Implement **spatial grid / partitioning** for AI checks. ✅ CellSpatialGrid.cs (Register, Unregister, UpdatePosition, GetInRadius, GetNearestInRadius by category Food/Organism); FoodManager.SetSpatialGrid, register on add/unregister on remove/consume; GetNearestFood uses grid when set; CellStage creates grid and passes to FoodManager.
- Use **structs** for organism data instead of MonoBehaviours for simulation. ⬜ Deferred (optional ECS later).
- **Pool GameObjects** for visual representation. ⬜ Deferred (requires Organism prefab; PoolManager ready).
- Update AI **less frequently than rendering** (e.g., every 0.2–0.3 s). ✅ BalanceConstants.CellAITickIntervalSeconds = 0.2f; CellOrganismAI.TickInterval defaults to it.
- Optional: move to **Unity ECS/DOTS** for 10k+ organisms. ⬜ Not done.

---

## Step 10: Player Features & Advanced Mechanics ✅ Done

**Goal:** Make the simulation interactive and fun.

### Tasks

- **Player-controlled organism:** ✅
  - Moves, eats, reproduces (mass ≥ CellPlayerReproductionMassThreshold)
  - Chooses mutations: MutationChoiceUI with 3 MutationSystem.Mutate variants; player genome updates, offspring spawned and registered
- **Environmental manipulation mode:** ✅
  - FoodManager.AddFoodAt(position, type, nutrition); CellStage.SpawnFoodAtPosition, SpawnToxinAtPosition, SpawnCurrentAtPosition
  - Alt+F / Alt+T / Alt+C spawn at player position
- **Evolution Documentary Mode:** ✅
  - Time scale 1× / 2× / 4× (RightBracket to cycle); PhylogeneticTreeUI + EvolutionStatsUI populated by Organism NPCs
- **Optional: Multicellular evolution:** ⬜ Deferred

---

## Summary Table

| Step | Focus | Key deliverables |
|------|--------|-------------------|
| 1 | Core organism & genome | Genome.cs, Organism.cs, single organism in scene |
| 2 | AI & movement | Movement.cs, Behaviour.cs, multiple organisms reacting |
| 3 | Food & energy | FoodManager.cs, energy gain/loss, death |
| 4 | Reproduction & mutation | ReproductionSystem.cs, MutationSystem.cs, population growth |
| 5 | Species & speciation | Species.cs, SpeciationSystem.cs, lineage tracking |
| 6 | Environment & biomes | Biome.cs, zones, events (bloom, virus, temperature) |
| 7 | Procedural visuals | ProceduralCellGenerator.cs, genome→visual mapping |
| 8 | Phylogenetic tree & UI | PhylogeneticTreeUI.cs, EvolutionStatsUI.cs ✅ |
| 9 | Scaling & performance | CellSpatialGrid, FoodManager integration, CellAITickIntervalSeconds; structs/pooling/ECS deferred ✅ |
| 10 | Player & advanced | PlayerCell genome + reproduction, MutationChoiceUI, AddFoodAt, SpawnToxin/CurrentAtPosition, time scale ✅ |

---

## References

- **cellStageUnityLayout.md** — Folder structure, script responsibilities, prefabs.
- **evolutionSimulationDesign.md** — Simulation layers, data-oriented design, behaviour loop.
- **cellStageOrganismDesign.md** — Player organism, mutation choice, movement, combat, food.
- **evolutionPrinciples.md** — Natural selection, mutation, speciation, environmental pressure.
- **implementation-plan.md** — Phase 0–10 project-wide plan; cell stage is Phase 3 + polish.
