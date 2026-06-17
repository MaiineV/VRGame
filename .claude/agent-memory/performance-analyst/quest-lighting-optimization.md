---
name: quest-lighting-optimization
description: QuestLightingOptimize.cs — shadow/lightmap settings chosen for Quest 2 GPU budget, rationale, and baseline values found in the project
metadata:
  type: project
---

One-shot editor script created at Assets/Editor/QuestLightingOptimize.cs.

## Baseline (what was in the project before)
- URP Mobile_RPAsset: shadow distance 50 m, cascade count 1 (already correct), shadowmap 1024, MSAA 4, HDR off
- Bar.unity directional light: m_Lightmapping: 4 (Realtime), soft shadows (m_Type: 2)
- LightmapSettings in Bar.unity: bake resolution 40, atlas 1024, NonDirectional (mode 1), compression on, no LightingSettings asset assigned

## Chosen values and rationale

| Setting | Old | New | Why |
|---|---|---|---|
| Shadow distance | 50 m | 15 m | Bar interior <10 m; large shadow-pass fill savings |
| Cascade count | 1 | 1 | Already correct; confirmed and logged |
| Shadowmap resolution | 1024 | 512 | 3 cm/texel at 15 m — imperceptible on Quest; 75% memory/cost saving |
| MSAA | 4 | 4 (kept) | Tiled GPU resolves on-chip at near-zero cost |
| HDR | off | off (kept) | Already correct for Quest |
| Light bake type | Realtime | Mixed | Dynamic objects keep realtime direct; statics bake indirect |
| Lightmap resolution | 40 tx/u | 30 tx/u | Reduces atlas count and bake time; still within 20-40 mobile range |
| Atlas size | 1024 | 1024 | Kept; fits small interior |
| Lightmap mode | NonDirectional | NonDirectional | Saves 50% lightmap memory vs Directional; standard Quest recommendation |
| Texture compression | on | on | ETC2 on Android; no visible quality loss |

**Why:** Quest 2 GPU is fillrate- and bandwidth-limited. The biggest realtime wins are shadow distance reduction (less shadow pass area) and shadowmap size (memory + fill). Baked lightmaps eliminate all indirect lighting cost at runtime. Mixed mode is the right balance for a scene with dynamic interactive objects (bottles, hands).

**How to apply:** When asked about lighting performance regressions or GPU bottlenecks on Quest, reference these baselines as the "before" state and these values as the first-pass optimization target.
