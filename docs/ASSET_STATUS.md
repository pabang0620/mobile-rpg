# 에셋 현황과 전략

## 확정된 전략 (2026-09-14, `docs/DECISIONS.md` 참고)

하이브리드 전략:

- **지형/배경/환경 오브젝트**(바닥 타일, 나무, 울타리, 건물 등)는 무료 + 상업적 이용 가능한 커뮤니티 에셋팩을 우선 쓴다. CC0(귀속 불필요) 또는 CC-BY(귀속 필요) 라이선스만 쓰고, 채택 전에 반드시 라이선스 원문을 확인한다.
- **게임 정체성을 좌우하는 요소**(주인공 캐릭터, 주요 몬스터, 시각적으로 두드러지는 오브젝트)는 무료 팩으로 품질이 부족할 수 있으므로 AI 이미지 생성(gpt-image 스킬 / `game-asset-artist` 에이전트)으로 직접 제작한다.
- **부분 교체 원칙**: 무료 팩 전체를 버리는 게 아니라, 실제로 퀄리티가 부족하다고 판단된 개별 요소만 AI로 재생성해 교체한다.
- CC-BY 라이선스 팩을 쓰면 `CREDITS.md`(또는 동등 문서)에 출처·라이선스를 반드시 기록한다.

### 검증 사례 (참고, 이 레포 소속 아님)

별도 실험 프로젝트 `../topdown-asset-mvp/`에서 실제로 이 전략을 적용해 확인했다:

- LimeZu **Serene Village - RPG Tileset [16x16]** (CC BY 4.0, 귀속 필요) - 바닥(잔디/길), 나무, 울타리, 집.
- Kenney **Tiny Town** (CC0, 귀속 불필요) - 덤불/나무, 울타리.
- `Assets/Art/Ground/ai_grass.png`, `ai_dirt_path.png`, `ai_stone.png` - 무료 팩 바닥 타일의 퀄리티가 부족하다고 판단해 AI로 재생성해 교체한 사례. 플레이어는 아직 placeholder(`Assets/Art/Player/player_placeholder.png`).
- 출처는 `../topdown-asset-mvp/CREDITS.md`에 기록됨.

## 이 프로젝트(lighthaven-2d)의 현재 상태

**하이브리드 전략 적용 전이다.** `CREDITS.md`가 아직 없고, `client/Assets/Sapphire/Art/`의 모든 파일은 2026-09-13 세션에서 AI 생성 또는 원본(Lighthaven 기획) 참고로 만든 것들이다. 무료 커뮤니티 팩은 아직 도입되지 않았다.

### 폴더 실측 (참고, 개별 파일 상태는 미실사 - TBD)

이전 `ASSET_STATUS.md`는 파일별 치수/해시/상태 표를 유지했으나, 이번 재작성 시점에 실제 `Art/` 폴더를 확인한 결과 그 표에 없던 파일이 다수 추가돼 있었다(`Enemies/GoblinMonsterFrame.png`, `Enemies/GoblinPixelArt*.png`, `MageSDDirectional*.png`, `MageWalk4x3-v2.png`, `WorldTiles6x4-v2.png`, `VFX/MagicMissile.png`, `UI/FantasyPanelBorder.png`, `UI/InventoryShopIcons.png` 등). 각 파일이 실제로 게임에서 쓰이는지, 방향/투명도 검사를 통과했는지는 이번 기술 문서 재작성 범위에서 다시 실사하지 않았다 - 지어내지 않고 **TBD로 남긴다.** 정확한 인벤토리가 필요하면 별도 작업으로 `lh2d-asset-specialist`에게 위임해 파일 존재/해시/치수/알파 실측부터 다시 만든다.

### 과거 실패 사례 (참고, 알파 검수 근거)

`MageDirectional.png`는 4×4 방향 시트로 생성됐으나 배경 체크무늬가 알파가 아니라 RGB 픽셀로 구워져 있어 런타임에서 제외됐고, 기존 투명 `MageIdle.png`을 계속 쓰고 있다. 배경 추출 재시도로도 진짜 알파를 만들지 못했다. **AI 생성 에셋은 알파 채널을 실측해서 검수해야 한다** - `Read` 도구로 눈으로 보는 것만으로는 RGB 배경과 투명 배경을 구분하지 못할 수 있다.

## 다음 작업 (TBD/후속)

- 지형/배경용 무료 팩 선정(라이선스 확인 포함) 및 이 프로젝트에 도입.
- 현재 AI 생성 캐릭터/몬스터 아트가 하이브리드 전략의 "정체성 요소"로 유지할 수준인지, 재생성이 필요한지 판단.
- 이 레포에 `CREDITS.md` 신설(CC-BY 팩을 채택하는 즉시).
- `Art/` 폴더 전체 실사(파일 존재/해시/치수/알파/사용 여부) 및 상태 인벤토리 재구축.
- 카메라 마이그레이션(`docs/HANDOFF.md` 참고)에 맞춰 타일 크기/PPU 기준으로 에셋 치수 재검토.
