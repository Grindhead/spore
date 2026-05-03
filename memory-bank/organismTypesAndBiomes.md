# Organism Types & Micro Biomes — Cell-Stage Design

Canonical design for **organism archetypes** (inspired by real microbes) and **environmental micro biomes**. Complements **cellStageOrganismDesign.md** (player organism, mutation, movement, combat) and **evolutionPrinciples.md**. Implementation should align with these archetypes and biomes where scope allows.

---

## 1. Amoeba-Type Organisms

**Inspired by:** [Amoeba](https://en.wikipedia.org/wiki/Amoeba)

### Behaviour

- shapeshifting body
- slow movement
- engulf prey

### Gameplay

Instead of biting: **touch prey → surround → digest**

### Evolution path

- larger body
- engulf multiple prey
- stronger membrane

These make great **slow tank** organisms.

---

## 2. Flagellate Swimmers

**Inspired by:** [Euglena](https://en.wikipedia.org/wiki/Euglena)

### Behaviour

- fast swimming using a flagellum
- attracted to light

### Gameplay

| Trait          | Effect                |
|----------------|------------------------|
| flagella       | speed                  |
| phototaxis     | move toward light      |
| photosynthesis | passive energy         |

These become **fast herbivores**.

---

## 3. Bacteria Swarms

**Inspired by:** [Escherichia coli](https://en.wikipedia.org/wiki/Escherichia_coli)

### Behaviour

- swarm behaviour
- chemical sensing

**Related concept:** [Chemotaxis](https://en.wikipedia.org/wiki/Chemotaxis)

### Gameplay

Bacteria detect **nutrient gradients**.

**Example rule:** move toward higher nutrient concentration

Large swarms create **dynamic food sources**.

---

## 4. Predator Cells

**Inspired by:** [Didinium](https://en.wikipedia.org/wiki/Didinium)

### Behaviour

- hunts other protozoa
- extremely fast attacks

### Gameplay

**Special abilities:**

- burst speed
- suction attack
- swallow prey whole

These are **top predators** in the ecosystem.

---

## 5. Photosynthetic Cells

**Inspired by:** [Chlorella](https://en.wikipedia.org/wiki/Chlorella)

### Behaviour

- produce energy from light
- slow movement

**Related concept:** [Photosynthesis](https://en.wikipedia.org/wiki/Photosynthesis)

### Gameplay

**Energy loop:** light exposure → energy gain

Weak in combat but **excellent long-term survival**.

---

## 6. Parasite Organisms

**Inspired by:** [Plasmodium](https://en.wikipedia.org/wiki/Plasmodium)

### Behaviour

- attach to host
- drain nutrients

### Gameplay

- attach to larger organism
- drain energy slowly

**Evolution path:**

- stealth parasites
- toxin parasites
- reproduction parasites

These create **unique gameplay strategies**.

---

## 7. Colonial Organisms

**Inspired by:** [Volvox](https://en.wikipedia.org/wiki/Volvox)

### Behaviour

- many cells form one organism
- cooperative movement

### Gameplay

**Multiple cells connect** → share energy → larger structure

**Possible builds:**

- chain creatures
- rotating colonies
- defensive clusters

---

## 8. Toxin Producers

**Inspired by:** [Dinoflagellate](https://en.wikipedia.org/wiki/Dinoflagellate)

### Behaviour

- release toxins
- cause harmful algal blooms

### Gameplay

**Toxin cloud** damages nearby cells.

Great for **area control** strategies.

---

## 9. Filter Feeders

**Inspired by:** [Paramecium](https://en.wikipedia.org/wiki/Paramecium)

### Behaviour

- cilia pull food particles inward

**Related concept:** [Filter Feeding](https://en.wikipedia.org/wiki/Filter_feeder)

### Gameplay

**Food particles automatically absorbed**

**Advantages:**

- efficient feeding
- safe playstyle

---

## 10. Endosymbiotic Organisms

**Related concept:** [Endosymbiosis](https://en.wikipedia.org/wiki/Endosymbiosis) — one of the most important events in evolution (e.g. how mitochondria evolved).

### Gameplay mechanic

Player can **absorb smaller organisms** and turn them into **organelles**.

| Absorbed Microbe    | New Ability      |
|---------------------|------------------|
| photosynthetic algae| passive energy   |
| bacteria            | digestion boost  |
| toxin bacteria      | poison attack    |

This would be a **fantastic evolution system**.

---

## 11. Environmental Micro Biomes

Real microscopic environments vary greatly.

**Inspired by:** [Microbial Ecology](https://en.wikipedia.org/wiki/Microbial_ecology)

| Biome         | Gameplay                    |
|---------------|-----------------------------|
| algae forest  | herbivore paradise          |
| bacterial mat | swarm predators             |
| thermal vent  | heat resistant organisms    |
| toxic pool    | rare mutations              |

Each biome **shapes evolution differently**.

---

## 12. Evolutionary Ecosystem Replay (Standout Feature)

Players can watch the **entire evolutionary history** of the world.

**Inspired by:** [Phylogenetic Tree](https://en.wikipedia.org/wiki/Phylogenetic_tree)

### Example timeline

| Generation | State                      |
|------------|----------------------------|
| 0          | algae dominant             |
| 200        | predators emerge           |
| 500        | armoured prey evolve       |
| 900        | predator extinction        |

This turns the game into a **mini artificial evolution simulator**.

*(Aligns with **cellStageOrganismDesign.md** §11 Microscopic Documentary Mode and §10 Evolution Timeline.)*

---

## Summary

| #  | Type / Feature       | Key idea                                              |
|----|----------------------|--------------------------------------------------------|
| 1  | Amoeba-type          | Shapeshift, engulf prey; slow tank; larger body, multi-prey, membrane |
| 2  | Flagellate swimmers  | Flagella + phototaxis + photosynthesis → fast herbivores |
| 3  | Bacteria swarms      | Chemotaxis; move toward nutrients; dynamic food sources |
| 4  | Predator cells       | Burst speed, suction, swallow whole; top predators      |
| 5  | Photosynthetic       | Light → energy; weak combat, strong long-term survival  |
| 6  | Parasites            | Attach, drain; stealth / toxin / reproduction paths    |
| 7  | Colonial             | Multi-cell, share energy; chain, rotating colony, clusters |
| 8  | Toxin producers      | Toxin cloud; area control                              |
| 9  | Filter feeders       | Cilia, auto-absorb particles; efficient, safe          |
| 10 | Endosymbiotic        | Absorb microbes → organelles (algae, bacteria, toxin)   |
| 11 | Micro biomes         | Algae forest, bacterial mat, thermal vent, toxic pool   |
| 12 | Ecosystem replay     | Watch full evolutionary history; phylogenetic timeline |
