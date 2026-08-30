# The Project — Features Added This Session

A running log of everything added, fixed, or changed in this conversation.

## Screen wipe transition

Original-work replacement for the old Animator-driven screen wipe (`ScreenWipeTransition.cs`, `Scripts/Entities/ScreenWipeTransition.cs`) — a plain `MonoBehaviour` that slides a solid panel across the screen via coroutine, no baked Animation clip or Animator Controller.

Debugged and fixed three separate, stacked bugs that kept it from working:
- `ScreenWipeTransition._Panel` was wired to the wrong child object (`Banana_Death_Loading`, a leftover loading-icon sprite) instead of the actual `Panel` object.
- `Panel`'s `Image` color alpha was 0.
- A dead leftover component (`ScreenWipe.cs`, the old pre-transition system) was still attached to the same Canvas GameObject and its `Awake()` was silently zeroing the Panel's alpha on every scene load, regardless of its own enabled/disabled state (`Awake()` always runs). Removed the component from the prefab.
- `LevelLoader.Start()` now calls `Transition.PlayOut()` — previously nothing ever revealed a freshly-loaded level, so it stayed hidden behind the covering panel forever.

## Level Editor UI

- **Return-to-editor keybind**: changed from F1 to Escape (`LevelInstantiator._ReturnToEditorKey`).
- **Removed the "Delete Screen" toolbar button** — `Delete`/`Backspace` (already context-sensitive between entity/screen deletion) is now the only way to delete a screen.
- **"Open Script" button**: appears in the bottom-left status panel whenever the selected entity is a MiniScript (`ScriptBehavior`) entity with a script assigned; opens its `.ms` file with the OS default app.
- **Entity palette highlighting**: the currently active entity type (the one that will be placed next) is highlighted green in the entity palette, mirroring the existing Background/Foreground layer-toggle highlight in Paint mode.
- **"Open Resources Folder" / "Open Scripts Folder" toolbar buttons**: open, respectively, the player-facing mod-content folder (`persistentDataPath/Resources`) and the folder holding runtime-created MiniScript entity scripts (`persistentDataPath/Entities`) in the OS file browser.
- **Live property reload**: editing a selected MiniScript entity's `.ms` file externally now refreshes its `expose()`'d properties in the inspector immediately — previously this only updated on the next `EntityCatalog` load, i.e. an app restart. A cheap per-frame timestamp check (`RuntimeEntityIO.ReloadIfChanged`) on whatever entity is currently inspected swaps in a freshly-read `TextAsset` and re-renders the property list when the file actually changes.
- **Removed the "Category" field from entity creation** — dropped from the "New Script Entity" dialog and from `EntityDefinition`/`RuntimeEntityIO.Record` entirely, since it was never read anywhere.

## Modding: loose-file content loading

New `RuntimeResourceLoader.cs` — `setSprite`/`setAudioClip`/`playAudioOneShot`/entity-icon lookups now check `persistentDataPath/Resources/<path>` first (a real folder a player can drop files into, even in a shipped build) before falling back to the game's built-in `Resources.Load`. Previously every such lookup went straight to `Resources.Load`, which only serves content baked into the build at compile time — unreachable by a player.

- Sprites: any format `Texture2D.LoadImage` decodes (PNG/JPG). Built with the project's pixel-art conventions enforced — Pixels Per Unit 16 (matches `CameraFollow`'s own `ppu` math), `FilterMode.Point` (crisp, no blur), `SpriteMeshType.FullRect` — rather than `Sprite.Create`'s defaults (100 PPU, `Tight` mesh) or the texture's default `Bilinear` filtering. Since entity-type icons load through this same function (`RuntimeEntityIO.ToEntityDefinition`), a runtime-created entity's palette/marker icon gets the same treatment automatically.
- Audio: WAV only, via a hand-rolled RIFF/WAVE parser (`ParseWav`) — Unity has no synchronous decoder for compressed formats, and this covers what every built-in audio asset in the project already is.
- Both cache by path (hits and misses) to avoid re-hitting disk every frame.
- Wired into `SpriteRendererBinder`, `AudioSourceBinder`, `ScriptEntityRunner` (`playAudioOneShot`), and `RuntimeEntityIO` (entity-type icons).

## Pause menu — removed entirely

Per request, deleted the pause menu feature completely:
- Deleted `Entities/Abstract/Pause_Menu.prefab` and the nested instance embedded in `Entities/LevelLoader.prefab`.
- Removed `LevelManager.PauseMenuUI`, `Pause()`, and `HandlePausing()` (the Escape-key toggle). `Resume()`/`PauseNoUI()` were kept — they're also used for screen-transition freezing, unrelated to the menu UI itself.
- Cleaned the stale `PauseMenuUI` field reference from every scene/prefab that had it (6 scenes, `LevelManager.prefab`).

## Codebase-wide comment cleanup

- Removed every `/// <summary>` XML doc comment block project-wide (mechanical pass, ~890 lines across 55 files).
- Removed comments that narrated a past code change ("previously only done in...", "unlike the old ComponentSpec...") rather than explaining current behavior.
- Trimmed the remaining comments to the shortest form that still carries real information; deleted ones that only restated the line right below them.
- Scoped to this project's own code — vendored/third-party folders (`Plugins/`, `Tarodev 2D Controller/`, `TextMesh Pro/`, `Scripts/MiniScript/`) were left untouched.

## Bugfixes

- **`CollectableBanana`**: `_Light`, `_AudioSource`, `_SpriteRenderer`, and `_Animator` were only cached in `OnValidate()`, which never runs in a build or for a runtime-instantiated banana — `Start()` would immediately null-reference on `_AudioSource`. Extracted the caching into `CacheReferences()`, now also called from `Awake()`.
- **`Spikes`**: its rotation (from `Direction`) was likewise only applied in `OnValidate()`. Extracted into a public `ApplyDirection()`, called from both `OnValidate()` and a new `Awake()`.
- **`ScriptEntityRunner` file-watch crash**: `HasScriptChanged()`'s `File.ReadAllText` on the watched `.ms` file wasn't wrapped in a try/catch, so an external editor holding a transient lock (e.g. mid-save) threw an unhandled `IOException` every frame until the lock cleared — and since the last-write timestamp was recorded *before* the read, a failed read also got silently marked "already seen," permanently skipping that reload. Now catches `IOException` and only advances the timestamp after a successful read, so it just retries next frame.

## Tile system

- **Removed the "Collision Type" tile attribute**: deleted `TileDef.CollisionType` and the `TileCollisionType` enum entirely (previously `None`/`Solid`/`OneWayPlatform`/`Hazard`/`Ladder`, though only `Solid` was ever actually wired up). Collision is now purely a function of layer — every tile painted on the Foreground layer generates a collider; Background tiles never do.
- **Spikes prefab adapter** (`SpikesAdapter.cs`, registered in `NativePrefabAdapterRegistry`): exposes `Direction` in the level editor's entity inspector and live-previews the rotation on the entity marker, mirroring the existing `SpringAdapter`.
- **Tilemap rendering & lighting**, set up at instantiation time in `LevelInstantiator.BuildTiles()`:
  - Background tilemap: sorting layer `"Background"` (plus a now-largely-redundant `sortingOrder = -100` safety margin from before the sorting-layer fix).
  - Foreground tilemap: sorting layer `"Solid"` (sits behind the player/entities in the project's existing sorting-layer order, but in front of Background).
  - Foreground tilemap: `ShadowCaster2D` + `CompositeShadowCaster2D` added, so adjacent solid tiles don't self-shadow at their seams under 2D lights.
