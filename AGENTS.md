# Sapphire RPG development

User authorized parallel development on 2026-09-13. Read docs/planning/01_PRODUCT.md and docs/planning/02_SYSTEM_CONTRACTS.md. Implement top-down XY action RPG, never sideways gravity/platform movement. Original project is reference only. New implementation lives in this directory. Use apply_patch for source edits.

Ownership: combat agent Domain/Combat and its tests; campaign agent Domain/Campaign, Infrastructure and its tests; presentation agent Presentation and Editor build pipeline; root Application facade, shared integration, assets and documents. Do not modify another owner's files without coordinating. Namespace Sapphire. Domain cannot depend on UnityEngine. Root controls Unity build execution to avoid simultaneous project locks.

Baseline public contracts are docs/PARALLEL_CONTRACT.md. Each agent writes a handoff with tests and limitations. Assets copied from original are reusable references, not automatically approved final art. No unverified completion claims.
