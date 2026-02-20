# Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior (MIG2024, C&G2025)

Welcome to the **Social Crowd Simulation** repository. This system simulates dynamic pedestrian movement by integrating social forces, group behavior, gaze control, and motion matching animation.

- Steer toward goals with social force model
- Avoid collisions (urgent avoidance + anticipated collision)
- Form natural groups (cohesion, repulsion, alignment)
- Realistic attention model with gaze behavior
- Natural animation by [motion matching](https://github.com/JLPM22/MotionMatching)

For a detailed overview and demo, visit the [Project Page](https://reiyaitatani.github.io/SocialCrowdSimulation/).
For the paper: [MIG2024](https://dl.acm.org/doi/10.1145/3677388.3696337) | [Computers & Graphics 2025](https://www.sciencedirect.com/science/article/pii/S009784932500127X)

<img src=".github/media/collision_avoidance_system.png" alt="Collision Avoidance System" width="300"/>

![Collision Avoidance Demo](.github/media/collision_avoidance.gif)

---

## Documentation

Full documentation is available in the [Wiki](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki).

| Page | Description |
|------|-------------|
| [Installation](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Installation) | Prerequisites and Unity Package Manager setup |
| [Quick Start](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Quick-Start) | Step-by-step demo scene setup |
| [First-Person Camera](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/First-Person-Camera) | Adding a controllable player |
| [Environment Setup](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Environment-Setup) | Walls and obstacles |
| [Agent Manager](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Agent-Manager) | Force weights, motion matching parameters, debug gizmos |
| [Architecture Overview](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Architecture-Overview) | 5-layer pipeline design and data flow |
| [Pipeline Layers](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Pipeline-Layers) | Detailed L1-2, L3, L4, L5 documentation |
| [Data Contracts](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Data-Contracts) | All `readonly struct` types |
| [Group System](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Group-System) | Group management, shared FOV, group collider |
| [Animation and Gaze](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Animation-and-Gaze) | Social behavior and gaze controller |
| [Customization](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Customization) | How to swap pipeline layers |

---

## Quick Install

1. Open Unity (2021.2+)
2. **Window > Package Manager > Add (+) > Add package by git URL...**
3. Enter:
   ```
   https://github.com/ReiyaItatani/SocialCrowdSimulation.git?path=Assets/com.reiya.socialcrowdsimulation
   ```

> All sample scenes use **URP**. Convert materials if using a different pipeline.

For detailed setup, see [Installation](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Installation) and [Quick Start](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Quick-Start).

---

## Architecture

The steering system uses a **5-layer pipeline** architecture with immutable data contracts:

```mermaid
flowchart LR
    subgraph "Per-Agent Pipeline"
        L12["L1-2<br/>Perception + Attention"]
        L3["L3<br/>Prediction"]
        L4["L4<br/>Decision"]
        L5["L5<br/>Motor"]
    end

    Sensor["SensorInput"] --> L12
    L12 -->|AttentionOutput| L3
    L3 -->|PredictionOutput| L4
    L4 -->|DecisionOutput| L5
    L5 -->|MotorOutput| MM["Motion Matching"]

    style L12 fill:#4a90d9,color:#fff
    style L3 fill:#6c8ebf,color:#fff
    style L4 fill:#e8a838,color:#fff
    style L5 fill:#7bc67e,color:#fff
```

| Layer | Default Implementation | Role |
|-------|----------------------|------|
| **L1-2** | `DefaultPerceptionAttentionLayer` | Resolves GameObjects to `PerceivedAgent` structs (only `GetComponent` boundary) |
| **L3** | `DefaultPredictionLayer` | Linear extrapolation of neighbor positions |
| **L4** | `DefaultDecisionLayer` | Social Force Model: 6 weighted forces |
| **L5** | `DefaultMotorLayer` | Speed management, goal slowing |

Each layer is interface-based and swappable. See [Architecture Overview](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Architecture-Overview) and [Customization](https://github.com/ReiyaItatani/SocialCrowdSimulation/wiki/Customization) for details.

---

## Contributions

We encourage contributions and inquiries — please **open an issue** or submit a **pull request** if you wish to collaborate or have questions.

---

## Citation

If you find this work beneficial, kindly cite the following:

```bibtex
@article{Reiya2025,
title = {Social crowd simulation: Improving realism with social rules and gaze behavior},
journal = {Computers & Graphics},
volume = {131},
pages = {104286},
year = {2025},
issn = {0097-8493},
doi = {https://doi.org/10.1016/j.cag.2025.104286},
url = {https://www.sciencedirect.com/science/article/pii/S009784932500127X},
author = {Reiya Itatani and Nuria Pelechano},
keywords = {Crowd simulation, Collision avoidance, Gaze behavior, Social rules}
}
}
```
