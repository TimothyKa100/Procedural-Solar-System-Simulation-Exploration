# Procedural Solar System Simulation & Exploration (Unity, C#)

This project implements a procedurally generated solar system with real-time simulation and interactive exploration. It combines numerical modelling, procedural terrain generation, and system-level optimisation to simulate large-scale environments within Unity.

---

## Demo

<!-- Replace with your best video(s) -->

[![Simulation Demo](https://img.youtube.com/vi/YOUR_VIDEO_ID/0.jpg)]([https://www.youtube.com/watch?v=YOUR_VIDEO_ID](https://img.youtube.com/vi/YOUR_VIDEO_ID/0.jpg))

<!-- Optional second video -->
<!--
[![Exploration Demo](https://img.youtube.com/vi/YOUR_VIDEO_ID_2/0.jpg)](https://www.youtube.com/watch?v=YOUR_VIDEO_ID_2)
-->

---

## Key Features

- **N-body simulation** using Newtonian gravity with Runge–Kutta (RK4) integration  
- **Procedural planet generation** using simplex noise and fractal Brownian motion (fBm)  
- **Dynamic reference frame (“floating origin”)** to maintain numerical precision at large spatial scales  
- **Adaptive rescaling** to balance global system size with high-resolution local detail  
- **Quadtree-based level of detail (LOD)** for efficient terrain rendering on spherical surfaces  
- **Real-time exploration**, allowing navigation and landing on procedurally generated planets  
- **Multithreaded optimisation** using Unity Jobs and Burst compiler  

---

## Technical Overview

### Physical Simulation
The system models gravitational interactions between bodies using Newton’s law of gravitation. Planetary motion is integrated numerically using a fourth-order Runge–Kutta (RK4) method, providing improved stability and accuracy over simpler methods such as Euler integration when simulating over extended time periods.

### Procedural Terrain Generation
Planetary surfaces are generated using coherent noise functions (simplex noise) combined with fractal Brownian motion (fBm) to create multi-scale terrain features. Height-based mapping and gradient sampling are used to produce visually distinct planetary surfaces.

### Large-Scale System Design
To address floating-point precision limitations in Unity, a dynamic reference frame (“floating origin”) is implemented, where the simulation recentres around the player. This enables stable representation of large spatial scales.

An adaptive rescaling approach allows the system to maintain a “to-scale” structure while increasing local resolution when approaching planetary surfaces.

### Rendering and Optimisation
Planets are represented using a sphere constructed from a cube-based mesh, with each face managed by a quadtree. This enables dynamic level of detail (LOD), refining mesh resolution near the viewer while reducing unnecessary computation elsewhere.

Performance is further improved through multithreaded computation using Unity’s Job System and Burst compiler.

---

## Repository Structure

- `Assets/` – Core simulation, procedural generation, and system logic  
- `ProjectSettings/` – Unity project configuration  
- `docs/` – Full technical documentation  

---

## Documentation

A detailed technical report covering design decisions, algorithms, and implementation is available here:

**→ `/technical-report.pdf`**

---

## Notes

This project was developed as an independent coursework project, with a focus on combining physical modelling, procedural systems, and scalable simulation design.
