# Audio Attributions — Pour Decisions

All clips in this folder are **CC0 (public domain dedication)** or **Public Domain**. None require
attribution, but sources are credited here for traceability and good practice. Files are mapped to
their `SfxId` (see `Assets/2. Scripts/Data/Enums/SfxId.cs`).

Each `SfxId` resolves to the file whose name (without extension) matches it, via
`Pour Decisions ▸ Audio ▸ Rebuild SFX Database` (see `Editor/AudioLibrarySetup.cs`). To swap any
sound, drop a new file with the same base name into this folder and re-run that menu item.

## Natural recordings — Wikimedia Commons (Public Domain)

These are Wikimedia's official **MP3 transcodes** of the source files. The original `.ogg` uploads carry
an Ogg *Skeleton* bitstream that Unity's audio importer rejects, so the equivalent MP3 transcode is used
(identical recording, Unity-importable).

| File | SfxId | Source | Author | License |
|------|-------|--------|--------|---------|
| `PourLoop.mp3` | PourLoop | [Pouring wine.ogg](https://commons.wikimedia.org/wiki/File:Pouring_wine.ogg) | cori | Public Domain |
| `DrinkSip.mp3` | DrinkSip | [Swallowing gulp.ogg](https://commons.wikimedia.org/wiki/File:Swallowing_gulp.ogg) | gregoryweir | Public Domain |
| `CashSale.mp3` | CashSale | [Coins dropped in wooden moneybox.ogg](https://commons.wikimedia.org/wiki/File:Coins_dropped_in_wooden_moneybox.ogg) | ezwa | Public Domain |
| `CashExpense.mp3` | CashExpense | [Coins dropped in metallic moneybox.ogg](https://commons.wikimedia.org/wiki/File:Coins_dropped_in_metallic_moneybox.ogg) | ezwa | Public Domain |
| `BottlePlace.mp3` | BottlePlace | [Dull thud.ogg](https://commons.wikimedia.org/wiki/File:Dull_thud.ogg) | gregoryweir | Public Domain |

## Foley — OpenGameArt "100 CC0 SFX" + "#2" (CC0)

| File | SfxId | Source | License |
|------|-------|--------|---------|
| `GlassBreak.ogg` | GlassBreak | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) (`glass_01`) | CC0 |
| `BottleBreak.ogg` | BottleBreak | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) (`glass_02`) | CC0 |
| `GlassPlace.ogg` | GlassPlace | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) (`dishes_01`) | CC0 |
| `NightStart.ogg` | NightStart | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) (`gong_01`) | CC0 |
| `NightEnd.ogg` | NightEnd | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) (`gong_02`) | CC0 |
| `Footstep.ogg` | Footstep | [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) (`footstep_wood_01`) | CC0 |
| `BarAmbience.ogg` | BarAmbience | [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) (`loop_ambient_01`) | CC0 |

## UI / feedback — Kenney "Interface Sounds" (CC0)

| File | SfxId | Source | License |
|------|-------|--------|---------|
| `ButtonPress.ogg` | ButtonPress | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`click_001`) | CC0 |
| `CustomerServed.ogg` | CustomerServed | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`confirmation_001`) | CC0 |
| `CustomerLeft.ogg` | CustomerLeft | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`error_002`) | CC0 |
| `GlassFull.ogg` | GlassFull | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`glass_001`) | CC0 |
| `GrabObject.ogg` | GrabObject | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`select_001`) | CC0 |
| `ReleaseObject.ogg` | ReleaseObject | [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) (`drop_001`) | CC0 |

## Music — OpenGameArt (CC0)

| File | SfxId | Source | License |
|------|-------|--------|---------|
| `MusicIdle.ogg` | MusicIdle | [Tavern](https://opengameart.org/content/tavern-0) | CC0 |
| `MusicNight.mp3` | MusicNight | [Happy Adventure (Loop)](https://opengameart.org/content/happy-adventure-loop) | CC0 |

## Notes / swap candidates

These picks were chosen by filename without auditioning each clip. If any feels off in playtest,
drop a replacement with the same base name and re-run *Rebuild SFX Database*:

- **GlassBreak / BottleBreak / GlassPlace** — `glass_0X` / `dishes_0X` from the OGA pack are
  interchangeable; swap among them if the break vs. clink character is wrong.
- **BarAmbience** — `loop_ambient_01` is a neutral room tone. For a louder pub murmur, a richer
  crowd recording exists on Wikimedia (e.g. *Ronacher Wien … Stimmengewirr*) but it is **CC BY-SA 4.0**
  (attribution + share-alike), so it was not used by default to keep this folder CC0/PD-only.
