# AR LabFactory — Interactive Instructions

A research-oriented augmented-reality prototype developed to explore immersive instructional interfaces for technical procedures.

## Overview

This project adapts written instructions to an augmented-reality experience. Spatial anchors help place the experience in the physical environment, while interactive widgets guide the user through the procedure in context.

The repository presents the non-confidential laboratory prototype and selected source code. It is intended to document the interaction design and implementation approach rather than provide a turnkey production build.

## Main features

- Spatial-anchor-based placement of the AR experience
- Interactive instructional widgets and contextual panels
- Tutorial calibration and instruction-flow management
- Support for XR interaction with Meta Quest devices
- Optional computer-vision-assisted component detection workflow

## Technology

- Unity 2022.3 LTS
- C#
- Meta Quest / Oculus XR tooling
- Unity Sentis for the computer-vision workflow

## Repository structure

- `Scripts/` — selected C# scripts for anchors, calibration, widgets, instruction management, and detection
- `README.md` — project overview and setup notes

## Setup notes

1. Install Unity 2022.3 LTS with the XR tooling required by your target headset.
2. Create or open a Unity project and copy the contents of `Scripts/` into an appropriate `Assets/Scripts/` directory.
3. Configure the XR platform, scene permissions, input bindings, and any required model assets for the target device.
4. Build the scene and test the anchor placement and instructional flow on the headset.

The complete Unity project, binary builds, proprietary assets, industrial datasets, and confidential client-specific materials are intentionally not included.

## Scope

This repository is a sanitized technical record of an academic/laboratory prototype. Please treat the source as an educational reference and verify dependencies and permissions before reuse.
