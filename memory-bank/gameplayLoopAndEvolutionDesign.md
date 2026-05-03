# Full Gameplay Loop and Evolution System Design

Design for a **living microscopic ecosystem** in the cell stage, inspired by **Evolutionary Biology** and **Microbial Ecology**. Focus: fun, playable game in Unity with real biological concepts, not a pure simulation.

Complements **cellStageOrganismDesign.md** (organism mechanics), **organismTypesAndBiomes.md** (archetypes and biomes), **evolutionPrinciples.md** (design framework), and **evolutionSimulationDesign.md** (simulation architecture).

---

## 1. Core Gameplay Loop

A simple loop that allows **deep outcomes** and naturally produces **selection pressure** (Natural Selection).

```
Explore
   ↓
Find food
   ↓
Avoid predators
   ↓
Gain energy
   ↓
Reproduce
   ↓
Mutation occurs
   ↓
Evolution changes gameplay
```

This loop drives **Natural Selection**: successful behaviours lead to reproduction and heritable change.

---

## 2. Moment-to-Moment Gameplay

The player controls a **single organism** in a microscopic world.

| Activity   | Gameplay |
|-----------|----------|
| **hunt**  | Chase smaller organisms |
| **graze** | Eat algae |
| **escape**| Evade predators |
| **scavenge** | Eat dead cells |
| **explore** | Find better environments |

**Design principle:** Constant **risk vs reward**.

| Situation        | Risk vs reward |
|------------------|----------------|
| Algae field      | Lots of food, but many predators |
| Deep zone        | Safer, but less food |

---

## 3. Reproduction and Evolution

When the organism has enough energy:

```
energy ≥ reproduction threshold
   → reproduce
   → mutation occurs
```

Based on **Genetic Mutation**: mutations are heritable changes that affect survival and reproduction.

### Example mutation system

| Mutation        | Effect |
|-----------------|--------|
| extra flagella  | Faster movement |
| spikes          | Defensive damage |
| toxin gland     | Poison attack |
| thicker membrane| Higher defence |

**Player agency:** Players select **1 of several mutations** at reproduction (e.g. 3 variants; see **cellStageOrganismDesign.md**), keeping evolution **strategic**.

---

## 4. Evolution Through Behaviour

The player’s **playstyle** influences evolution (adaptive pressure).

| Behaviour         | Mutation bias |
|-------------------|----------------|
| Frequent hunting  | Attack mutations |
| Frequent escaping| Speed mutations |
| Plant eating      | Digestion mutations |

This simulates **adaptive pressure**: behaviour shapes which traits are favoured when mutations appear.

---

## 5. Food Web System

The ecosystem should form a **natural food web**.

```
Algae
   ↓
Herbivore cells
   ↓
Predator cells
   ↓
Scavengers
```

**Coevolution:** If predators dominate → prey evolve defence (spikes, speed, poison). Predator–prey arms races keep the world dynamic (see **evolutionPrinciples.md** — Coevolution).

---

## 6. Environmental Evolution Pressure

Different **regions** create different evolutionary outcomes.

| Zone        | Advantage |
|------------|-----------|
| Algae forest | Herbivores |
| Open water   | Fast swimmers |
| Toxic zone   | Poison resistance |
| Deep zone    | Low metabolism |

Players can **move between zones** to shape their evolution.

---

## 7. Rare Evolution Events

Occasional **random events** create dramatic change.

| Event         | Effect |
|---------------|--------|
| Nutrient bloom| Huge population growth |
| Virus outbreak| Species die-off |
| Toxic spill   | New resistance mutations |
| Oxygen surge  | New metabolism types |

These events **reshape ecosystems** and create memorable moments.

---

## 8. Intelligent Predator Behaviour

Predators should not be simple enemies. Use **simple rules** for emergent behaviour (Complex Systems).

| Behaviour     | Description |
|---------------|-------------|
| **Ambush**    | Hide, then strike |
| **Pack hunting** | Multiple predators chase together |
| **Territorial**  | Guard food-rich areas |

Even these simple rules create **emergent ecosystems**.

---

## 9. Evolution Timeline System

Track species history using a **phylogenetic tree** (Phylogenetic Tree concept).

### Example timeline

```
Ancestor Cell
 ├ Fast Herbivore
 │   └ Armoured Herbivore
 └ Predator Cell
     └ Venom Predator
```

Players can explore:

- **Extinct species**
- **Dominant species**
- **Evolutionary branches**

---

## 10. Long-Term Player Progression

Even though evolution resets each generation, players unlock **knowledge and discoveries** across runs (meta-progression).

| Discovery      | Unlock |
|----------------|--------|
| Photosynthesis | Plant-like builds |
| Venom glands   | Poison builds |
| Symbiosis      | Attach helpers |
| Electric organs| Stun attacks |

Inspired by concepts like **Endosymbiosis**. See **organismTypesAndBiomes.md** for endosymbiotic and other archetypes.

---

## 11. Making the World Feel Alive

Microscopic environments should feel **dynamic**.

- Drifting organic particles
- Slow fluid currents
- Glowing organisms
- Plankton swarms

These **visuals and motion** help the world resemble real microscope environments (see **cellStageOrganismDesign.md** §6).

---

## 12. Evolution Documentary Mode (Standout Feature)

At any time players can **zoom out** and watch:

- **Generation 0 → generation 1000**
- Species appearing
- Predator–prey cycles
- Extinctions

This turns the game into a **living artificial ecosystem** and could make the game **special**. See **cellStageOrganismDesign.md** §11 and **evolutionSimulationDesign.md** (evolution time-lapse).

---

## Advanced: Multi-Cellular Evolution

Introduce **multi-cellular evolution** late in the game, inspired by the transition to multicellularity (Evolutionary Developmental Biology).

### Example progression

```
single cell
   ↓
cell colony
   ↓
multi-cell organism
```

This would give the game a strong sense of **evolutionary progress**. See **organismTypesAndBiomes.md** for colonial organisms and ecosystem replay.

---

## Summary Table

| #  | Topic              | Key idea |
|----|--------------------|----------|
| 1  | Core loop          | Explore → Find food → Avoid predators → Gain energy → Reproduce → Mutation → Evolution |
| 2  | Moment-to-moment   | Hunt, graze, escape, scavenge, explore; risk vs reward (algae field vs deep zone) |
| 3  | Reproduction       | energy ≥ threshold → reproduce → mutation; player picks 1 of several (e.g. flagella, spikes, toxin, membrane) |
| 4  | Behaviour bias     | Hunting → attack; escaping → speed; plant eating → digestion |
| 5  | Food web           | Algae → Herbivore → Predator → Scavengers; coevolution when predators dominate |
| 6  | Environmental zones| Algae forest, open water, toxic zone, deep zone — each favours different builds |
| 7  | Rare events        | Nutrient bloom, virus outbreak, toxic spill, oxygen surge |
| 8  | Predator AI        | Ambush, pack hunting, territorial — simple rules, emergent ecosystems |
| 9  | Phylogenetic tree  | Timeline UI: extinct/dominant species, evolutionary branches |
| 10 | Meta-progression   | Discoveries: photosynthesis, venom, symbiosis, electric — unlock new builds |
| 11 | World feel         | Drifting particles, currents, glow, plankton swarms |
| 12 | Documentary mode   | Zoom out; watch gen 0 → 1000; living artificial ecosystem |
| —  | Multicellular      | Late-game: single cell → colony → multi-cell organism |
