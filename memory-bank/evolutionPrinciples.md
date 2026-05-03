# Core Evolution Principles — Project Spore (Cell Stage)

Design framework for making evolution feel **authentic and dynamic**. Implementation should align with these principles; see `projectbrief.md` and `systemPatterns.md` § 9a for gameplay targets.

**Detailed cell-stage spec:** **memory-bank/cellStageOrganismDesign.md** — player organism (Genome/energy/age), mutation choice (3 variants), movement, combat, food, world, behaviours, arms races, zones, phylogenetic tree, documentary mode, major events.

**Implementation design:** For a concrete, computationally efficient simulation (genome system, mutation, energy economy, speciation, evolution tree, performance), see **memory-bank/evolutionSimulationDesign.md** (digital evolution / artificial life approach).

---

## 1. Natural Selection

**Related concept:** Natural Selection

**Rule:** Traits that increase survival or reproduction become more common.

**In the game:** Instead of "buying upgrades", mutations appear randomly.

**Example:** Some cells mutate faster flagella → if speed helps survival → those cells reproduce more → the trait spreads through the population.

Evolution emerges from: **mutation → survival → reproduction → population change**. This makes the ecosystem feel authentic and dynamic.

---

## 2. Mutation

All evolution begins with genetic changes.

**Related concept:** Genetic Mutation

**Mutations should be:**
- rare
- mostly neutral
- sometimes harmful
- occasionally beneficial

**Gameplay implementation examples:**

| Mutation           | Result              |
|--------------------|---------------------|
| longer flagella    | faster swimming     |
| extra membrane     | slower but tougher   |
| digestive enzyme   | new food source      |
| toxin gland        | defense              |

Players guide evolution **indirectly**, not directly.

---

## 3. Genetic Drift

**Related concept:** Genetic Drift

Random changes in populations. Even useless traits can spread by chance, especially in small populations.

**Gameplay example:** A small isolated group might evolve strange colours, unusual body shapes, or inefficient structures. This makes evolution messy and realistic.

---

## 4. Ecological Niches

Organisms evolve to fill roles in ecosystems.

**Related concept:** Ecological Niche

**Examples of niches:**

| Niche        | Behaviour           |
|-------------|---------------------|
| filter feeder | consumes plankton |
| predator    | hunts other cells   |
| scavenger   | eats dead cells     |
| parasite    | attaches to hosts   |

The player must adapt to a niche to survive.

---

## 5. Arms Races (Co-evolution)

**Related concept:** Coevolution

Species constantly adapt against each other.

**Example:** Prey evolve speed → predators evolve speed → prey evolve spikes → predators evolve armour. This is an evolutionary arms race. In gameplay this keeps evolution constantly changing.

---

## 6. Energy Economy

Real organisms evolve under energy constraints. Every trait has a metabolic cost.

**Related concept:** Metabolism

**Example:**

| Trait      | Cost                |
|-----------|---------------------|
| spikes    | maintenance energy  |
| large body| higher food demand  |
| speed     | constant energy drain|

This prevents overpowered creatures.

---

## 7. Trade-offs

Evolution always involves compromises.

**Related concept:** Evolutionary Trade-off

**Examples:**

| Advantage   | Disadvantage    |
|------------|-----------------|
| armour     | slower movement |
| speed      | fragile body    |
| large jaws | higher energy cost |

The player must balance survival strategies.

---

## 8. Speciation

**Related concept:** Speciation

Populations eventually split into new species.

**Example:** Population splits into two groups → different environments → different mutations → eventually become separate species.

**Gameplay result:** Biodiversity emerges naturally.

---

## 9. Environmental Pressure

Environmental factors drive evolution.

**Examples:**

| Pressure     | Evolution Result   |
|-------------|--------------------|
| predators   | defensive traits   |
| scarce food | efficient digestion|
| strong currents | streamlined bodies |
| toxins      | resistance mutations |

Evolution becomes reactionary.

---

## 10. Emergent Behaviour

**Related field:** Complex Systems

Real ecosystems emerge from simple rules. If you simulate reproduction, energy, mutation, and predation, you get surprisingly complex ecosystems automatically.

---

## Example Gameplay Loop (Realistic Evolution)

```
player cell survives
       ↓
random mutation occurs
       ↓
trait affects survival
       ↓
successful cells reproduce
       ↓
population evolves
```

The player influences evolution through **behaviour**, not menus.

---

## What Makes Evolution Games Fun

The best evolution games don't let players directly design creatures. Instead they let players:

- influence selection pressure
- guide survival strategies
- explore outcomes

Evolution should feel **unpredictable**.

---

## Advanced Realistic Ideas

### Horizontal Gene Transfer

**Related concept:** Horizontal Gene Transfer. Common in microbes.

Cells can absorb DNA from dead organisms. **Example:** Eat a toxic organism → gain toxin resistance gene.

### Endosymbiosis

**Related concept:** Endosymbiosis. One of the most important events in evolution.

**Example in game:** Engulf bacteria → bacteria becomes energy organelle. This mirrors how mitochondria evolved. **Full organism types (including endosymbiotic absorb→organelles):** see **memory-bank/organismTypesAndBiomes.md** (amoeba, flagellate, bacteria swarms, predator, photosynthetic, parasite, colonial, toxin, filter feeder, endosymbiotic, micro biomes, ecosystem replay).

---

## Unique Feature: Evolutionary Tree

Show players an **evolutionary tree** of all species that emerged.

**Related concept:** Phylogenetic Tree

**Example:**

```
Ancestor Cell
 ├ Fast Herbivore
 │  └ Armoured Herbivore
 └ Predator Cell
    └ Venom Predator
```

Players see the history of evolution in their world.
