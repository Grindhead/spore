# Cell-Stage Unity Project Layout — Evolving Cell Game

Concrete Unity project layout for the evolving cell game: scalable, modular, and easy to expand. Combines simulation, procedural generation, AI, evolution tracking, and visuals. Use under **RoobsWorld/Assets/**; align with **evolutionSimulationDesign.md**, **cellStageOrganismDesign.md**, and **evolutionPrinciples.md**.

---

## 1. Folder / File Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Organism.cs           # Stores organism data and energy
│   │   ├── Genome.cs             # Traits & visual genes
│   │   ├── Species.cs            # Tracks species and phylogeny
│   │   └── EcosystemManager.cs   # Manages world updates & population
│   │
│   ├── AI/
│   │   ├── Behaviour.cs         # Decision-making logic
│   │   └── Movement.cs          # Movement physics & speed
│   │
│   ├── Evolution/
│   │   ├── MutationSystem.cs    # Handles mutations on reproduction
│   │   ├── ReproductionSystem.cs # Handles spawning new organisms
│   │   └── SpeciationSystem.cs  # Tracks species divergence
│   │
│   ├── Environment/
│   │   ├── Biome.cs             # Defines zones & environmental effects
│   │   └── FoodManager.cs       # Spawns and manages food items
│   │
│   ├── Rendering/
│   │   ├── OrganismView.cs      # Links organism data to visuals
│   │   ├── ProceduralCellGenerator.cs # Generates cell body mesh/particles
│   │   └── ParticleEffects.cs  # Optional glow, spikes, flagella
│   │
│   └── UI/
│       ├── PhylogeneticTreeUI.cs # Tracks species evolution
│       └── EvolutionStatsUI.cs  # Shows energy, traits, population
│
├── Prefabs/
│   ├── Organism.prefab
│   ├── Food.prefab
│   └── BiomeZones.prefab
│
├── Materials/
│   ├── CellBody.mat
│   └── Spike.mat
│
├── Scenes/
│   └── Main.unity
│
└── Shaders/
    └── SoftBodyShader.shader
```

**Note:** In this repo the Unity project lives under **RoobsWorld/**; the above paths are relative to `RoobsWorld/Assets/`. Existing Scripts folders (Core, Creature, Stages, World, UI, Utils) remain; cell-stage evolution systems can live under **Stages/Cell/** or be split into Core/AI/Evolution/Environment/Rendering as above for clarity.

---

## 2. Core Scripts Overview

| Script | Responsibility |
|--------|----------------|
| **Organism.cs** | Holds organism state: position, energy, age, Genome, speciesId. Handles death conditions. |
| **Genome.cs** | Defines traits: speed, size, attack, defence, digestion, metabolism. Visual genes: spikes, flagella, color. |
| **Species.cs** | Tracks speciesId, parentSpeciesId, baseGenome, and population. Used for phylogenetic tree. |
| **EcosystemManager.cs** | Central update loop: moves organisms, updates energy, spawns food, handles reproduction. Uses spatial partitioning grid for efficiency. |

---

## 3. AI & Behaviour

**Behaviour.cs** — Decision-making:

```csharp
public void Decide(Organism org)
{
    if (org.energy < hungerThreshold)
        SeekFood();
    else if (PredatorNearby(org))
        Flee();
    else if (PreyNearby(org))
        Attack();
    else if (org.energy > reproduceThreshold)
        Reproduce();
    else
        Wander();
}
```

**Movement.cs** — Converts genome speed & flagella into actual movement; adds small wobble/pulsation for realism.

---

## 4. Evolution Systems

| System | Responsibility |
|--------|----------------|
| **MutationSystem.cs** | Small random changes per gene on reproduction. Visual genes mutate too (spikes, color, flagella). |
| **ReproductionSystem.cs** | Spawns new organism with mutated genome. Reduces parent energy. |
| **SpeciationSystem.cs** | Measures genetic distance between populations. If distance > threshold → new species. |

---

## 5. Environment Systems

| System | Responsibility |
|--------|----------------|
| **Biome.cs** | Defines zones: algae forest, toxic pool, open water. Each zone gives trait bonuses/penalties. Can include dynamic events (nutrient bloom, virus outbreak). |
| **FoodManager.cs** | Spawns algae, bacteria, or dead cells. Can move small particles for realism. |

---

## 6. Rendering & Visuals

| Component | Responsibility |
|-----------|----------------|
| **ProceduralCellGenerator.cs** | Generates mesh/particles from genome. Examples: size → scale; spikes → spike objects; flagella → particle tails; color → body material. |
| **OrganismView.cs** | Links organism data to visuals (read from simulation layer; update GameObjects). |
| **ParticleEffects.cs** | Optional glow, spikes, flagella effects. |
| **SoftBodyShader.shader** | Adds slight pulsation. Optional glow based on traits (e.g. poison, photosynthesis). |

---

## 7. UI Systems

| UI | Responsibility |
|----|----------------|
| **PhylogeneticTreeUI.cs** | Tracks species branching. Shows extinct vs current species. Optional time-lapse replay. |
| **EvolutionStatsUI.cs** | Displays organism energy, traits, mutation options. Shows player choices during reproduction. |

---

## 8. Performance Tips

- Use **structs** for Organism data to reduce GC.
- **Pool GameObjects** instead of creating/destroying.
- Update AI every **0.2–0.3 s** for large populations.
- **Only render nearby organisms** (culling / LOD).
- Optionally switch to **Unity ECS/DOTS** for 10k+ organisms.

---

## 9. Optional Advanced Features

- **Player-controlled organism** with mutation choice (choose 1 of 3 variants at reproduction).
- **Environmental manipulation mode** — player adds food, toxins, currents.
- **Evolution Documentary Mode** — fast-forward and replay generations.
- **Multicellular evolution** — small colonies merge into larger organisms.

---

## 10. Design Summary (Quick Reference)

| Area | Key scripts / responsibilities |
|------|--------------------------------|
| **Core** | Organism (data, energy, age, Genome, speciesId), Genome (traits + visual genes), Species (phylogeny), EcosystemManager (update loop, spatial grid) |
| **AI** | Behaviour (Decide: SeekFood / Flee / Attack / Reproduce / Wander), Movement (genome speed & flagella → movement, wobble) |
| **Evolution** | MutationSystem (gene + visual mutations on reproduce), ReproductionSystem (spawn mutated offspring), SpeciationSystem (genetic distance → new species) |
| **Environment** | Biome (zones, bonuses/penalties, events), FoodManager (algae, bacteria, dead cells) |
| **Rendering** | OrganismView (data → visuals), ProceduralCellGenerator (genome → mesh/particles), ParticleEffects; SoftBodyShader (pulsation, trait glow) |
| **UI** | PhylogeneticTreeUI (species tree, extinct/current, time-lapse), EvolutionStatsUI (energy, traits, mutation choices) |
| **Prefabs** | Organism, Food, BiomeZones |
| **Performance** | Structs for Organism, object pooling, AI tick 0.2–0.3 s, render nearby only; optional ECS/DOTS for 10k+ |

---

## References

- **cellStageImplementationSteps.md** — 10-step implementation roadmap (Core Organism & Genome → AI & Movement → Food & Energy → Reproduction & Mutation → Species & Speciation → Biomes → Procedural Visuals → Phylogenetic Tree & UI → Scaling → Player & Advanced).
- **evolutionSimulationDesign.md** — Simulation layers, data-oriented organism structs, behaviour loop, speciation, evolution tree.
- **cellStageOrganismDesign.md** — Player organism, mutation choice, movement, combat, food, zones, phylogenetic tree, documentary mode.
- **evolutionPrinciples.md** — Natural selection, mutation, trade-offs, speciation, environmental pressure.
- **organismTypesAndBiomes.md** — Organism archetypes and micro biomes.
- **systemPatterns.md** — Overall folder structure and RoobsWorld layout.
