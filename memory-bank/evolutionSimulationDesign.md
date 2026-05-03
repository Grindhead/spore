# Evolution Simulation Design — Digital Evolution / Alife Approach

Implementation-oriented design for simulating **population dynamics, mutation, and selection** in a computationally efficient way. Aligns with **evolutionPrinciples.md** and makes the cell-stage ecosystem reflect simplified systems used in digital evolution research (artificial life simulations).

---

## High-Level Simulation Architecture

Think of the ecosystem as **separate simulation layers**. Only the rendering layer needs GameObjects; everything else uses plain data structures. This approach is common in simulations inspired by Artificial Life research.

```
Simulation Layer
│
├── Population Simulation
│   ├── Organism data
│   ├── Genome
│   ├── Energy / metabolism
│
├── Behaviour Simulation
│   ├── food seeking
│   ├── predator avoidance
│   ├── reproduction
│
├── Environment Simulation
│   ├── currents
│   ├── food growth
│   ├── toxins
│
└── Rendering Layer
    ├── cell visuals
    ├── particles
    └── UI
```

**Important principle:** Only the rendering layer needs GameObjects. Everything else should be plain data structures.

---

## Data-Oriented Organism Structure

Instead of 1000 GameObjects with scripts, use **1000 organism structs in arrays**. This structure is CPU-cache friendly and massively improves performance.

Example organism data:

```csharp
public struct Organism
{
    public Vector2 position;
    public Vector2 velocity;

    public float energy;
    public float age;

    public Genome genome;

    public int speciesId;
}
```

**Player organism (see memory-bank/cellStageOrganismDesign.md):** The player is represented as a `PlayerOrganism` with `Genome`, `energy`, and `age`. No direct upgrades — evolution happens through mutation during reproduction; player actions (hunt / flee / eat plants) create evolution pressure (attack / speed / digestion traits).

Rendering layer reads this data and updates GameObjects/visuals; simulation layers never touch Unity objects.

---

## 1. Core Organism Architecture

Each organism has three main components: **Genome (traits)**, **Energy + metabolism**, and **Behaviour AI**. Evolution logic stays separate from behaviour.

```
Organism
 ├ Genome (traits)
 ├ Energy + metabolism
 └ Behaviour AI
```

### Organism class example (Unity)

```csharp
public class Organism : MonoBehaviour
{
    public Genome genome;
    public float energy;
    public float age;
    public int speciesId;

    private Movement movement;
    private Behaviour behaviour;

    void Update()
    {
        behaviour.Decide();
        movement.Move();

        energy -= genome.metabolismCost * Time.deltaTime;

        if (energy <= 0)
            Die();
    }
}
```

This keeps evolution logic separate from behaviour.

---

## 2. Genome System

**Related concept:** Population Genetics

The genome holds **evolvable traits**. Use a small list of genes so organisms are data-driven.

### Genome (serializable for save/debug)

```csharp
[System.Serializable]
public class Genome
{
    public float speed;
    public float size;
    public float attack;
    public float defence;
    public float digestion;
    public float metabolismCost;
}
```

### Gene → gameplay mapping

Traits directly affect gameplay. Genes can influence multiple biological functions (Evolutionary Biology–inspired):

| Gene | Gameplay |
|------|----------|
| speed | movement |
| size | health & food need |
| attack | damage |
| defence | damage reduction |
| digestion | energy from food |
| metabolismCost | ongoing energy drain (e.g. per second) |

### Derived properties (trade-offs)

Derived stats create trade-offs, essential for realistic evolution:

- **maxHealth** = size × defence  
- **movementCost** = speed × metabolism  
- **foodEfficiency** = digestion  

This prevents strictly dominant builds and encourages niche specialisation.

### Mutation choice (player-facing)

Per **cellStageOrganismDesign.md**: when the player reproduces, generate **3 mutated variants** from the parent genome; the player **chooses one**. Example choices: longer flagella (+speed, +energy cost), toxin gland (poison attack), thick membrane (+defence, −speed). Keeps evolution strategic but believable.

---

## 3. Mutation System

**Related concept:** Genetic Mutation

When an organism **reproduces**, genes mutate. Most mutations are small, which mirrors real evolution. For the **player**, use the **mutation choice system** (3 variants, player picks one) — see **cellStageOrganismDesign.md** §2.

### Example

```csharp
public Genome Mutate(Genome parent)
{
    Genome g = new Genome();

    g.speed = MutateGene(parent.speed);
    g.size = MutateGene(parent.size);
    g.attack = MutateGene(parent.attack);
    g.defence = MutateGene(parent.defence);
    g.digestion = MutateGene(parent.digestion);
    g.metabolismCost = MutateGene(parent.metabolismCost);

    return g;
}

float MutateGene(float value)
{
    float mutation = Random.Range(-0.1f, 0.1f);
    return Mathf.Clamp(value + mutation, 0f, 10f);
}
```

Alternative: **rare, small, sometimes harmful** (Genetic Mutation–inspired):

```csharp
float MutateGene(float gene)
{
    if (Random.value < 0.05f)
        gene += Random.Range(-0.2f, 0.2f);
    return Mathf.Clamp(gene, 0f, 10f);
}
```

- **Mutation rate** per gene (e.g. 0.05) so not every gene mutates every reproduction.
- Mutations occur during reproduction. Avoid huge jumps; small deltas create gradual, believable evolution.

---

## 4. Energy Economy

**Related concept:** Metabolism

Energy drives everything. Everything revolves around **energy flow** so evolution optimises efficiency.

### Example rules

| Action | Energy |
|--------|--------|
| move | -0.2 |
| attack | -1 |
| eat food | +10 |
| reproduce | -40 |

**Energy loop:** eat → gain energy → reproduce → mutation → evolution.

(Values are tuning parameters; use **BalanceConstants** or ScriptableObject config in implementation.)

---

## 5. Natural Selection

**Related concept:** Natural Selection

Evolution should **emerge through survival**. You don't need to code evolution directly—it emerges.

Organisms that:

- collect food efficiently
- avoid predators
- survive longer

will reproduce more. Traits that help survival and reproduction become more common over generations.

---

## 6. Reproduction

When organisms reach **enough energy**, they reproduce.

### Rules

- **Condition:** `energy > reproductionThreshold` (threshold can come from genome or config).
- **Effect:** Spawn offspring with **mutated** copy of parent genome (see §3).
- **Important:** Parent energy is **split** between parent and offspring (or parent pays a fixed cost, e.g. -40). This creates population growth and competition.

---

## 7. Efficient World Simulation

To simulate **hundreds of organisms**, avoid expensive checks. Use **spatial partitioning**.

### Simple grid example

```
World
 ├ Grid cell
 │   ├ organisms
 │   └ food
```

Each organism only checks **nearby grid cells** for food, predators, and prey.

### Grid lookup example (critical for scale)

Organisms only interact with nearby cells. This reduces complexity from **O(n²) → O(n)**.

```csharp
Dictionary<Vector2Int, List<int>> grid;  // cell → organism indices
```

Each grid cell contains organism indices (and optionally food). Query neighbours by cell only.

### Unity alternatives

| Option | Use case |
|--------|----------|
| **Physics2D.OverlapCircle** | Simple radius checks |
| **Custom spatial grid** | Full control, no physics dependency |
| **Unity ECS** | If scaling to huge numbers (thousands+) |

See **techContext.md** (cell stage scale) and **systemPatterns.md** (object pooling).

---

## 7a. Player Movement Model (Microorganism-like)

**See cellStageOrganismDesign.md §3.** Cells move like microorganisms (flagella, cilia, amoeboid). Rule: **thrust = flagellaCount × genome.speed**. Example:

```csharp
velocity += direction * genome.speed * Time.deltaTime;
position += velocity * Time.deltaTime;
```

Add **drag** for fluid feel.

---

## 7b. Physical Combat (Contact-Based)

**See cellStageOrganismDesign.md §4.** No simple attack button — combat is **contact-based**. Traits define behaviour: **spikes** → damage on collision, **jaws** → bite attack, **toxin** → poison damage over time. **Damage:** `damage = attack - opponent.defence`. Shape-based combat.

---

## 7c. Food Source Types

**See cellStageOrganismDesign.md §5.** Use several types: **algae** (stationary energy), **bacteria** (small moving prey), **dead cells** (scavenger food). Emergent evolution: algae eaters → predators evolve → scavengers appear.

---

## 7d. Real Microorganism Behaviours

**See cellStageOrganismDesign.md §7.** Chemotaxis (follow nutrients), phototaxis (move toward light), swarming (colony behaviour). Example rule: if nutrient gradient detected → move toward stronger concentration.

---

## 7e. Environmental Zones (Evolution Pressure)

**See cellStageOrganismDesign.md §9.** Different zones favour different builds: **algae forests** → herbivores, **open water** → fast swimmers, **toxic pools** → resistant organisms, **deep areas** → low metabolism. Players migrate between zones to evolve differently.

---

## 7f. Major Evolutionary Events

**See cellStageOrganismDesign.md §12.** Introduce dramatic events: **oxygen increase** (new metabolism types), **virus outbreak** (massive extinction), **nutrient bloom** (population explosion). Creates ecosystem shifts.

---

## 8. Behaviour System

**Related concept:** Artificial Life

Use a **simple decision loop**. Even simple logic produces complex ecosystems.

### Example

```csharp
public class Behaviour
{
    public void Decide()
    {
        if (IsHungry())
            SeekFood();
        else if (PredatorNearby())
            Flee();
        else if (PreyNearby())
            Attack();
        else if (EnergyHigh())
            Reproduce();
        else
            Wander();
    }
}
```

Implement via behaviour tree or utility AI; keep rules minimal so emergence comes from genome + environment.

---

## 9. Emergent Ecosystems

Do **not** hardcode predators or herbivores. **Behaviour emerges from traits.**

### Trait combo → role (examples)

| Trait combo | Result |
|-------------|--------|
| high speed + attack | predator |
| high digestion | herbivore / filter feeder |
| high defence | tank |

Ecological roles (Grazer, Hunter, Parasite, Scavenger) can be **derived** from genome + simple rules rather than fixed tags.

---

## 10. Speciation Detection

**Related concept:** Speciation

When organisms become **genetically different enough**, classify them as a new species.

### Example

```csharp
float GeneticDistance(Genome a, Genome b)
{
    return Mathf.Abs(a.speed - b.speed) +
           Mathf.Abs(a.attack - b.attack) +
           Mathf.Abs(a.defence - b.defence);
    // optionally include size, digestion, metabolismCost
}
```

If **distance > threshold** → new species. Track species statistics (count, lineage, time of first appearance) for UI and balancing.

---

## 11. Evolution Tree (Phylogenetic Tree)

**Related concept:** Phylogenetic Tree

Track **ancestry**. Each species stores:

- **speciesId**
- **parentSpeciesId**
- **generation**

You can show players a **tree of life** that evolved in the simulation.

### Example

```
Ancestor Cell
 ├ Fast Grazer
 │   └ Armoured Grazer
 └ Predator Cell
     └ Venom Predator
```

Players love seeing **how life evolved**; supports the procedural species tree in **projectbrief.md** and **evolutionPrinciples.md**.

---

## 12. Procedural Cell Generator (Gene → Visual)

Cells should **visually represent genes** so organisms are recognisable by appearance.

| Gene | Visual |
|------|--------|
| speed | flagella length / count |
| attack | spikes |
| defence | thicker membrane |
| digestion | colour |

Example:

- `flagellaCount = speed * 2`
- `spikeCount = attack`
- `cellSize = size`

Procedural generation at runtime keeps visuals aligned with simulation data.

---

## 12a. Unity Visual Tricks for "Living Cells"

Small visual touches make organisms feel **alive**:

| Area | Ideas |
|------|--------|
| **Movement** | Perlin noise wiggle, soft turning, slight scale pulsation |
| **Rendering** | Translucent shaders, glow effects, particle trails |
| **Physics** | Soft collisions, slight bounce |

This makes cells feel organic and squishy (aligns with **projectbrief.md** § Visual Polish).

---

## 13. Population Control

Without limits the simulation **explodes**. Use constraints to stabilise ecosystems.

| Mechanism | Effect |
|-----------|--------|
| Limited food | Starvation |
| Predators | Population control |
| Disease events | Die-offs |

Tune so that population stays in a target range (e.g. hundreds to low thousands) for performance and gameplay.

---

## Organism Archetypes & Micro Biomes

**See memory-bank/organismTypesAndBiomes.md** for the full spec. Summary: organism types (amoeba engulf, flagellate/phototaxis/photosynthesis, bacteria swarms/chemotaxis, predator burst/suction/swallow, photosynthetic, parasite attach/drain, colonial multi-cell, toxin cloud, filter feeder cilia, endosymbiotic absorb→organelles), environmental micro biomes (algae forest, bacterial mat, thermal vent, toxic pool), and **Evolutionary Ecosystem Replay** (watch full evolutionary history). These archetypes and biomes shape niche filling and emergence.

---

## Emergent Evolution Example (Coevolution)

**Related concept:** Coevolution

1. **Start simulation:** 100 algae eaters.
2. **Mutation produces:** fast swimmer.
3. **Fast swimmers catch others** → predator species emerges.
4. **Prey evolve spikes** → predator evolves armour.

This is **coevolution**. You don't need to code it directly—it emerges from genome + energy + behaviour rules.

---

## Environment Simulation (Pressure → Evolution)

Environmental pressure drives evolution. World systems create selection pressure (Ecology-inspired):

| Feature | Evolution result |
|--------|-------------------|
| strong currents | streamlined cells |
| toxic zones | toxin resistance |
| dark regions | sensory organs |
| algae blooms | herbivores |

Tune environment (currents, food growth, toxins) so traits that counter or exploit them become favourable.

---

## Scaling to Huge Populations (Unity DOTS / ECS)

For **very large simulations** (10k+ organisms), use **Unity DOTS / ECS**.

Benefits: multithreading, extremely fast updates.

Typical update pipeline:

1. **MovementSystem**
2. **EnergySystem**
3. **BehaviourSystem**
4. **ReproductionSystem**
5. **MutationSystem**
6. **DeathSystem**

Data stays in components; rendering layer syncs visuals from component data.

---

## Player Roles

Possible player roles in the simulation:

| Role | Description |
|------|--------------|
| **Single Organism** | Control one evolving cell. |
| **Ecosystem Controller** | Change environment variables (currents, food, toxins). |
| **Evolution Observer** | Watch evolution unfold (tree, populations, events). |

Combining **Single Organism + Ecosystem Controller** often creates the best gameplay.

---

## Performance Tips (Important for Games)

For **hundreds to thousands** of organisms:

| Technique | Use |
|-----------|-----|
| **Spatial hashing** | Neighbour search (predators, prey, food) |
| **Fixed update loop** | Deterministic, predictable cost per tick |
| **Object pooling** | Spawn/despawn without GC spikes |
| **Simple physics** | Avoid full rigidbody per cell if possible |

Target: **500–2000 organisms** easily achievable with these practices. See **techContext.md** (cell stage scale) and **systemPatterns.md** (object pooling).

---

## Simple Organism Data (Example)

```csharp
struct Organism
{
    int id;
    Genome genome;
    float energy;
    float age;
    int species;   // or speciesId
}
```

### Update loop (per organism, per tick)

1. **Sense** environment (nearby food, threats, prey).
2. **Choose action** (from behaviour rules).
3. **Spend energy** (move, attack, etc.).
4. **Possibly reproduce** (if energy > threshold → spawn offspring with mutated genome; split or deduct energy).
5. **Possibly die** (energy ≤ 0, age, or killed).

---

## Standout Feature: Microscopic Documentary Mode

**See cellStageOrganismDesign.md §11.** **Microscopic Documentary Mode** — At any time the player can **zoom out** and watch:

- **population growth**, **species evolution**, **extinctions**
- Over **hundreds of generations**

It turns the simulation into a **living artificial ecosystem** (Artificial Life–style). Implementation: record key events (birth, death, speciation) with timestamps and genome snapshots; replay by stepping through timeline and visualising phylogenetic tree + population graph.

---

## References

- **evolutionPrinciples.md** — high-level principles (natural selection, mutation, niches, coevolution, etc.).
- **projectbrief.md** — Core Gameplay Design, Cell-Stage Design Ideas, killer features (procedural species tree).
- **systemPatterns.md** — § 9a (cell-stage design targets), § 9b (evolution principles mapping), object pooling, spatial hashing.
