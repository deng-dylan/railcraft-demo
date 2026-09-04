# Third-party art staging

This directory contains the curated runtime subset used by the `v0.4 Art Alpha`
development branch. Original ZIP and UnityPackage files remain local staging inputs
at the repository root and are ignored by Git.

## Included groups

- `Character/`: one URP worker appearance, its minimum material/texture dependency
  closure, three locomotion clips, and three optional Humanoid comparison clips.
- `Environment/`: one PBR overhead crane, a small CC0 Kenney Train Kit subset,
  two shipping containers, one electrical substation, a curated ArtStation
  industrial dressing set and one quarantined factory-shell review asset.
- `Audio/UI/`: selected button, success, failure and repair feedback clips.

Each group contains an asset manifest or source note. Kenney Train Kit includes its
CC0 license text. The worker, UI SFX and overhead-crane source archives did not carry
a license document in their payloads; account purchase records and redistribution
terms must be verified before a public release.

Third-party source files are treated as immutable inputs. Project-owned Animator
controllers, materials, layout rules, colliders and gameplay bindings live outside
this directory.
