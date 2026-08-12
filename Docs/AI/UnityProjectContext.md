# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `titansoul`
- Last analyzed: 2026-08-12
- Last analyzed commit: `b2c9c720da8cc907da8a45faad9e4ae3773fd925`
- 2D single-player boss arena project centered on the EyeCube encounter.

## Confirmed Environment

- Unity version: 6000.3.10f1
- Render pipeline: Universal Render Pipeline, using the 2D Renderer
- Input system: Unity Input System package with direct keyboard/mouse reads
- Target platforms: not explicitly documented

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.3 with 2D packages | Confirmed | `Packages/manifest.json`, `Assets/Settings/Renderer2D.asset` |
| Input | Input System 1.18 | Confirmed | `Packages/manifest.json`, `PlayerController.cs` |
| Tests | Unity Test Framework 1.6 installed; no first-party tests found | Confirmed | `Packages/manifest.json` |
| Networking | No first-party multiplayer implementation found | Confirmed | representative scripts and package manifest |
| Editor automation | MCP for Unity (CoplayDev) v10.0.0 | Confirmed | `Packages/manifest.json`, `Packages/packages-lock.json` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scripts/Player` | Player movement, aiming, dash, projectile attack | Confirmed | `PlayerController.cs` |
| `Assets/Scripts/Bosses/EyeCube` | EyeCube state loop and attacks | Confirmed | boss scripts |
| `Assets/Scripts/Combat` | Shared health and damage contracts | Confirmed | combat scripts |
| `Assets/Editor` | Generated animation, prefab, and arena assembly | Confirmed | editor builder scripts |
| `Assets/Art`, `Assets/Animations`, `Assets/Prefabs` | Source and generated content | Confirmed | asset tree |

## Assembly Boundaries

- No custom assembly definitions; runtime code compiles into `Assembly-CSharp` and editor code into `Assembly-CSharp-Editor`.

## Scenes And Startup Flow

- Build scene: `Assets/Scenes/SampleScene.unity`
- Startup: the single enabled scene starts directly in the EyeCube arena.
- Editor builders keep `SampleScene` configured with the player and arena only; the boss prefab is placed later through an explicit menu command.

## Architecture

- MonoBehaviour-centric composition with small combat interfaces and coroutine-driven boss states.
- Inspector references are preferred, with narrow fallback discovery for the player tag.
- Shared damage authority lives in `Health` through `IDamageable`.

## Coding Conventions

- File-scoped feature namespaces under `TitanSoul`.
- Private `[SerializeField]` configuration with PascalCase public APIs.
- Coroutines for timed gameplay; conventional Unity lifecycle methods.

## Testing And Validation

- Unity Test Framework is installed but no EditMode or PlayMode tests were found.
- Generated solution/project files support external C# compilation checks.

## Available Unity Tooling

- MCP for Unity is installed and configured for Codex at `http://127.0.0.1:8080/mcp`.
- The local server is configured to start automatically with the Unity Editor.
- Codex must be restarted after initial configuration before Unity MCP tools appear in a task.

## Important Constraints

- Preserve generated asset GUIDs and avoid manual edits to generated animation assets.
- User-owned changes already exist under `ProjectSettings` and must not be overwritten.

## Unknowns And Confidence

- Target build platform and performance budget are undocumented.
- Runtime visual verification requires opening the project in Unity Editor.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- representative player, combat, boss, world, and editor scripts
- `Assets/Scenes/SampleScene.unity`

<!-- unity-onboarding:generated:end -->
