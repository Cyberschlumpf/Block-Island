# The Voxel Box

The Voxel Box is a Unity voxel-game project with block-based world rendering, imported worlds, dynamic time-of-day, weather, water and configurable graphics.

## Current project snapshot

GitHub-ready snapshot based on **The Voxel Box 1.0.1 – Warm Graphics**.

## Unity project

Open this repository's root folder as a Unity project. The project requires the Unity version recorded in `ProjectSettings/ProjectVersion.txt`.

### Version-control settings

For reliable Git collaboration, keep these Unity settings enabled:

- **Version Control Mode:** Visible Meta Files
- **Asset Serialization Mode:** Force Text

## Git LFS

This repository uses Git LFS for large binary assets such as textures, models, audio and imported voxel-world data. Install Git LFS before the first push/clone workflow:

```bash
git lfs install
```

## Do not commit generated Unity folders

`Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs` and `UserSettings` are intentionally excluded by `.gitignore`. Unity recreates them locally.

## First publish

1. Create an empty repository on GitHub (no README, license or .gitignore during creation).
2. Add this project folder in GitHub Desktop.
3. Commit all files as `Initial The Voxel Box 1.0.1`.
4. Publish/push the repository to GitHub.

## Note

Before making the repository public, verify that every included third-party asset/package may legally be redistributed publicly. A private repository is the safest starting point while that review is pending.
