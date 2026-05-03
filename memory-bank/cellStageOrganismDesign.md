# Cell-Stage Organism Design — Detailed Specification

Canonical design for the player organism, evolution, movement, combat, food, world feel, and standout features. Implementation should align with this document; see **projectbrief.md**, **evolutionPrinciples.md**, and **evolutionSimulationDesign.md** for principles and simulation architecture.

---

## 1. Player Organism Design

The player should **not** "buy upgrades" directly. Evolution happens through **mutation during reproduction**, based on principles like Natural Selection.

### Core stats

```csharp
public class PlayerOrganism
{
    public Genome genome;
    public float energy;
    public float age;
}
```

### Player actions → evolution pressure

| Player Action           | Evolution Pressure    |
|-------------------------|------------------------|
| hunt frequently         | attack traits increase |
| run from predators      | speed traits increase  |
| eat plants              | digestion traits increase |

This mimics **adaptive evolution**: behaviour influences which traits are favoured when mutations appear.

---

## 2. Mutation Choice System (Player-Friendly)

Real mutation is random, but players should still feel **agency**.

### Flow when reproduction happens

```
Parent genome
      ↓
3 mutated variants generated
      ↓
player chooses one
```

### Example mutation choices

| Mutation        | Effect                    |
|-----------------|---------------------------|
| longer flagella | +speed, +energy cost       |
| toxin gland     | poison attack              |
| thick membrane  | +defence, −speed           |

This keeps evolution **strategic but believable**.

---

## 3. Player Movement Model

Cells should move like **microorganisms**.

### Real microbes often use

- **flagella**
- **cilia**
- **amoeboid movement**

### Movement rule

```
thrust = flagellaCount * genome.speed
```

### Unity example

```csharp
velocity += direction * genome.speed * Time.deltaTime;
position += velocity * Time.deltaTime;
```

Add **drag** for fluid feel.

---

## 4. Physical Combat (Important)

Instead of a simple attack button, combat should be **contact-based**.

### Trait → combat behaviour

| Trait   | Combat Behaviour        |
|---------|--------------------------|
| spikes  | damage on collision      |
| jaws    | bite attack              |
| toxin   | poison damage over time  |

### Damage example

```
damage = attack - opponent.defence
```

This creates **shape-based combat**.

---

## 5. Food Sources

Use **several types** of food.

| Food       | Behaviour              |
|------------|------------------------|
| algae      | stationary energy       |
| bacteria   | small moving prey       |
| dead cells | scavenger food          |

Evolution emerges naturally:

- **algae eaters** → **predators** evolve → **scavengers** appear

This mirrors ecological systems studied in **Ecology**.

---

## 6. Beautiful Microscopic World

The world should feel like a **living drop of water**.

### Visual ideas

**Lighting**

- soft volumetric glow
- bioluminescent organisms

**Particles**

- floating organic debris
- tiny plankton

**Water movement**

- slow currents
- drifting particles

### Unity tools

- particle systems
- shader graph
- post processing

---

## 7. Real Microorganism Behaviours

Borrow behaviour from **real microbes**.

| Behaviour   | Real Example              |
|-------------|----------------------------|
| chemotaxis  | bacteria follow nutrients  |
| phototaxis  | organisms move toward light|
| swarming    | bacterial colonies         |

### Example rule

```
if nutrient gradient detected
  move toward stronger concentration
```

These behaviours add **realism**.

---

## 8. Evolutionary Arms Races

**Predator–prey dynamics** create endless evolution.

**Concept:** Coevolution.

### Example cycle

```
predators evolve speed
       ↓
prey evolve spikes
       ↓
predators evolve armour
       ↓
prey evolve poison
```

This keeps **gameplay evolving**.

---

## 9. Environmental Evolution Pressure

**Different zones** encourage different builds.

| Zone          | Evolution Advantage   |
|---------------|------------------------|
| algae forests | herbivores             |
| open water    | fast swimmers          |
| toxic pools   | resistant organisms    |
| deep areas    | low metabolism         |

Players can **migrate between zones** to evolve differently.

---

## 10. Evolution Timeline

Track evolution over time using a **phylogenetic tree** (concept from Phylogenetic Tree).

### Example display

```
Ancestor Cell
 ├ Fast Herbivore
 │   └ Armoured Herbivore
 └ Predator
    └ Venom Predator
```

Players can view:

- **extinct species**
- **current dominant species**
- **evolutionary branches**

---

## 11. Microscopic Documentary Mode (Standout Feature)

At any time players can **zoom out** and watch:

- population growth
- species evolution
- extinctions

Over **hundreds of generations**.

It turns the simulation into a **living artificial ecosystem**, similar to research in **Artificial Life**.

---

## 12. Major Evolutionary Events

Introduce **major evolutionary events** for dramatic ecosystem shifts.

| Event            | Effect                    |
|------------------|---------------------------|
| oxygen increase   | new metabolism types      |
| virus outbreak    | massive extinction        |
| nutrient bloom    | population explosion      |

These create **dramatic ecosystem shifts**.

---

## Summary

| # | Topic                        | Key idea                                              |
|---|------------------------------|--------------------------------------------------------|
| 1 | Player Organism              | Genome + energy + age; no direct upgrades; behaviour → evolution pressure |
| 2 | Mutation Choice              | 3 variants at reproduction; player picks one          |
| 3 | Movement                     | thrust = flagellaCount × genome.speed; drag           |
| 4 | Physical Combat              | Contact-based; spikes/jaws/toxin; damage = attack − defence |
| 5 | Food Sources                 | Algae, bacteria, dead cells; emergent niches          |
| 6 | Microscopic World            | Volumetric glow, bioluminescence, particles, currents  |
| 7 | Microbe Behaviours           | Chemotaxis, phototaxis, swarming                      |
| 8 | Arms Races                   | Coevolution cycle (speed ↔ spikes ↔ armour ↔ poison)  |
| 9 | Environmental Zones          | Algae forests, open water, toxic pools, deep           |
| 10| Evolution Timeline           | Phylogenetic tree UI (extinct/dominant/branches)       |
| 11| Documentary Mode             | Zoom out; watch populations/species over generations   |
| 12| Major Events                 | Oxygen, virus, nutrient bloom → ecosystem shifts      |
