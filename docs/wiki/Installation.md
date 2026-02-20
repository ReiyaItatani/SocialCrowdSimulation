# Installation

## Prerequisites

- **Unity 2021.2 or newer** (compatibility is not guaranteed on older versions)
- **Universal Render Pipeline (URP)** is used in all sample scenes. If you use a different pipeline, you may need to convert materials.

## Install via Unity Package Manager

1. Open Unity Editor (version 2021.2 or newer)
2. Go to **Window > Package Manager**
3. Click the **Add (+)** button, then select **Add package by git URL...**
4. Enter:
   ```
   https://github.com/ReiyaItatani/SocialCrowdSimulation.git?path=Assets/com.reiya.socialcrowdsimulation
   ```
5. Click **Add** to begin the installation

The package depends on:
- `com.unity.collections` 1.4.0
- `com.unity.burst` 1.6.4
- `com.unity.inputsystem` 1.5.1

These will be installed automatically.

## Dependencies

This project integrates with [Motion Matching](https://github.com/JLPM22/MotionMatching) for natural animation. The motion matching system is included as part of the project.

---

Next: [Quick Start](Quick-Start.md)
