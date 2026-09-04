# RailCraft v0.4 Art Alpha

`v0.4.0-art-alpha.5` is the current controlled visual-production pass after the
ThirdPerson whitebox. It keeps the complete RC-EMU-01 training loop and replaces
the most visible temporary presentation layers. `FLOW-002` intentionally changes
the player-facing knowledge, pickup and install granularity to material packages;
domain PartIds, assembly anchors and the compatible snapshot schema remain stable.

## First-pass visual scope

- The capsule player is replaced by the curated URP Handyman worker prefab.
- A project-owned three-clip Animator blend drives Idle, Walk and Run from the
  existing `ThirdPersonMotor`; root motion stays disabled.
- A PBR overhead crane is normalized from its source axes to a 43 m factory span
  and placed over the north inspection bay.
- Five Kenney rail tiles and one maintenance flatbed form a visual-only west-side
  service display.
- Two dimension-normalized containers form west-wall storage and use simple box
  collision; a low-cost electrical substation fills the north-east service bay.
- Four `floor-large` panels cover the hall surface with the project floor material;
  an existing ArtStationIndustrial dressing layer adds forklift, equipment, shelving
  and barrier silhouettes to the background.
- Twelve UI sounds are staged; button, correct/success, warning/failure and repair
  clips are wired through one non-spatial `WhiteboxAudioPresenter`.
- Five package knowledge workbenches, four package assembly tables, the composite
  table and commissioning consoles use modular Kenney Factory Kit visual assemblies.
- The explicit `QIANYINLAGAN` traction-rod group from the CW-200K reference is
  available through the part-visual pipeline; unresolved sensor, positioning and
  height-control parts continue to use safe fallbacks.
- The player-facing flow now treats the five material packages as the unit of
  knowledge, pickup and installation. The fourteen child PartIds remain visible
  only in the assembly recipe and optional visual breakdown.
- A curated free ArtStation industrial layer adds a forklift, workshop equipment,
  concrete barrier and warehouse shelving as visual-only background dressing.
- Factory clear height increases from 6.4 m to 10 m so the crane and raised work
  lighting remain inside the authored hall envelope.
- `ART-001` adds curated free assets through `FreeAssetExpansionVisualFactory`:
  generic spring, sensor-support, positioning-support and stop-valve/pipe visuals;
  a tool bench, safety barriers, windows, industrial exterior silhouettes and
  distant trains. These are teaching or presentation assets and do not claim
  vehicle-specific geometry or dimensions.
- `ART-002` adds project-authored primitive geometry for a secondary air spring,
  scissor lift table, four-point lifting spreader, HMI cabinet and cable run.
  These objects close presentation gaps while remaining explicitly generic,
  dimension-neutral teaching or display assets.

All imported art is attached through deterministic factories called by
`WhiteboxSceneBuilder`. Rebuilding the scene therefore preserves this pass.

## Runtime contracts

- Player movement and collision stay on the existing 1.8 m `CharacterController`.
- Imported character colliders, rigidbodies, cameras, lights and audio sources are
  removed from the generated instance.
- Environment models are static visual objects with no collider, rigidbody,
  Animator, camera, light or audio source. Side-bay props use three project-owned
  `BoxCollider` volumes; imported mesh colliders are removed.
- The crane and Kenney assets use project-owned URP/Lit materials.
- Imported animation is Humanoid, uses a project-owned blend tree, and cannot move
  the authoritative player root.
- The smoke runner opts into background execution only when `-whitebox-smoke` is
  present, allowing licensed graphical-editor builds to be validated in a hidden
  Windows Player.

## Rebuild and validation

Use Unity `6000.3.21f1`:

1. `RailCraft > Third Person Whitebox > Rebuild Scene`
2. Run the complete EditMode suite.
3. `RailCraft > Third Person Whitebox > Build Windows x86_64`
4. Run `RailCraftWhitebox.exe -whitebox-smoke` with the documented screenshot
   arguments.

The tracked evidence for this batch is under `Artifacts/Whitebox/ArtAlpha/`.

Current `v0.4.0-art-alpha.5` evidence: 200/200 EditMode tests passed; the
Windows x86_64 build completed with 0 warnings and 0 errors; both the build
directory and independently staged package reported
`RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED`. The player-facing progress is 14 steps
and the material gate uses ten core questions across five packages.

Those results belong to `FLOW-002`. The subsequent `ART-001` asset expansion
has a rebuilt serialized scene and static source/license audit, but its EditMode,
Windows build and Player smoke results remain pending because the local Unity
license currently exits with code 198. See
`Artifacts/Whitebox/ArtAlpha/art-001-asset-expansion.md`.

## Known limits

- The factory architecture, stations and semantic part visuals still contain
  whitebox geometry and require subsequent art passes.
- The ArtStation industrial layer is presentation-only: it has no gameplay
  colliders, animation or engineering meaning. Its three product pages and
  Extended Commercial License record are kept in `ArtStationIndustrial/SOURCE.md`.
- Imported worker, UI SFX and overhead-crane licenses still require release-owner
  verification; they are acceptable only for local integration and internal review
  until that evidence is recorded.
- Electrical Substation has the same pending-license boundary. Shipping Containers
  retains its author ReadMe and credit requirement. The Simple Factory source is
  isolated outside `Assets` and Git because of missing license evidence, weak UV
  coverage and a door/light-heavy 269,600-triangle mesh.
- Character source textures and FBX files are large; Git LFS covers new PNG, JPG,
  JPEG, WAV and FBX additions, while the Windows build remains larger than v0.3.
- A new formal 1920×1080 target-machine performance capture remains required after
  the full environment and lighting pass.
- `ART-001` component meshes are generic teaching stand-ins. Sensor brackets,
  positioning elements and height-control equipment still require verified
  engineering sources before they can be described as vehicle-specific parts.
- `ART-002` authored equipment does not represent certified lifting capacity,
  torque, measurement accuracy, electrical design or a vehicle-specific interface.
