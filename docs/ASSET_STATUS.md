# 에셋 배선 상태

## 캐릭터 불변 조건
은회색 장발, 청회색 눈, 큰 남색 후드, 해진 다층 로브, 고금 자수, 사파이어 보석, 검정/금 부츠, 푸른 결정 지팡이를 유지한다. 원본은 `generated-images/source-reference/mage_original_concept.png`다.

## 현재 연결됨
- `SapphireTown.png`: 로그인 배경과 실제 마을 허브.
- `SapphirePlatformMap.png`: 달빛 회랑 횡스크롤 전장. 지면 y=214, 발판 y=414/473/395.
- `MageIdle.png`, `MagePortrait.png`, `MagePose02/06/09/14.png`: 법사 표시와 공격 핵심 포즈.
- `Goblin.png`: 첫 전투 적.
- `MageSkills.png`, `MainMenuIcons.png`, `HudControls.png`: 스킬, 메뉴, 조작 글리프.
- `UiChrome.png`: 평면형 반투명 원형/사각/바/배지 프레임.
- `WideButton.png`: 로그인·서버·캐릭터·출정 공통 와이드 버튼.
- `NotoSansCJKkr-Regular.otf`: 모든 한글 UI 폰트. OFL 라이선스 동봉.
- `hud-redesign-v2.png`: 최신 전투 HUD 승인 기준 시안.

`MoonCourtyard.png`와 `MenuPanel.png`는 과거 시안 호환용 보관 에셋이며 현재 핵심 화면 외형에 사용하지 않는다. 크기, 형식, SHA-256은 `ASSET_MANIFEST.json` schema 2가 기준이다. 빌더는 16개 이미지를 임포트하고 실제 투명 에셋 11개를 검사한다.

## 생성 또는 보수 필요
1. 법사 idle/run/attack/hit/down/get-up/death 전체 프레임을 동일 발 기준선으로 완성한다.
2. 스킬 VFX를 캐릭터와 분리한 RGBA 시트로 제작한다.
3. 고블린 전체 행동과 추가 일반 적 2종을 만든다.
4. 마을 NPC, 상호작용 표식, 상점/강화/퀘스트 화면용 글리프와 카드 에셋을 만든다.
5. 배경을 전경, 전투 바닥, 중경, 원경 시차 레이어로 분해한다.

모든 결과는 외부 절대경로를 런타임에서 참조하지 않고 `client/Assets/Game` 안으로 복제한 뒤 `tools/update_asset_manifest.py`로 매니페스트를 갱신한다.

## 승인된 무료 상업용 에셋 묶음 (2026-09-13 추가)

4개 팩에서 실측·정제한 14개 파일을 `client/Assets/Game/Art/{Backgrounds,VFX,Enemies,UI}/`에 추가했다. 전부 RGBA 실측 알파(투명+불투명 픽셀 공존, RGB 체크무늬 아님)를 통과했고 `docs/ASSET_MANIFEST.json`(schema 3)에 source_url/license/alpha/used_by로 기록했다.

- **배경 시차 레이어** (`joe777.itch.io/free-parallax-backgrounds`, 상업이용 무료·재판매 금지): `WinterTreesFar/Mid/Near.png`(겨울 침엽수 3단 깊이 레이어), `CaveCrystalRidgeA/B.png`(사파이어 톤 수정 동굴 능선 실루엣). `WinterTreesFar`(원경)와 `CaveCrystalRidgeB`(근경)만 `BattleScreen.cs`에 배선했다 - 겨울 숲 톤은 달빛 회랑의 사파이어 분위기와 맞지 않아 기술 시연용 원경 레이어로만 쓰고, 수정 능선(크리스탈+어두운 실루엣)이 프로덕션에 더 맞는 근경으로 판단했다.
- **스킬 VFX**: `icemaan.itch.io/free-500-pixel-art-effects`(상업이용 무료·재판매 금지)의 Arcane/Magic Missile.png(96px 42프레임 스트립)을 `MagicMissile.png`로 복제, `BattleScreen.ImpactFlash`에서 적중 시(플레이어가 가한 피해에 한함) 프레임 14~26을 감쇠 재생하는 실제 스킬 이펙트로 배선했다.
- **몬스터 2종 (대조용, 기존 Goblin.png 옆 배치)**: OpenGameArt CC0 두 팩을 비교 관찰한 결과 - `goblin-free-pixelart`(`GoblinPixelArtIdle/Run/Attack/Death.png`, 19/8/3/19프레임)는 뾰족귀·녹색피부·창을 든 실루엣이 뚜렷하고 idle/run/attack/death 상태를 모두 갖춰 M1 로드맵이 요구하는 "고블린 전체 행동"에 더 근접한다. `goblin-monster`(`GoblinMonsterSpritesheet32.png`, 32px 8x8 그리드 8색 변형)는 실제로는 회색 갑주를 두른 고블린이 맞지만 32px 스프라이트라 종 식별력이 약하고 idle+walk만 제공한다. 원본 `Goblin.png`(1254x1254, 소프트 안티에일리어싱, 회화풍 음영)과 비교하면 둘 다 디테일 밀도·외곽선 두께가 크게 다른 레트로 픽셀아트라 톤이 맞지 않는다 - 이는 의도된 것으로, 전투 씬의 3번째 적 슬롯(x=800/950/1100)에 원본·픽셀아트·몬스터시트를 나란히 배치해(`BattleScreen.ResetRun`) 실제 대조가 가능하게 했다. 최종 채택은 픽셀아트 쪽을 우선 후보로 권장하되, 둘 다 매니페스트에 등록하고 씬에도 함께 배선했다(택1 강제 아님).
- **UI 아이콘/9-slice**: `kenney.nl/assets/ui-pack-rpg-expansion`(CC0)에서 실제 아이콘 소재(검/건틀릿/코인/체크/크로스 등)를 4x2 시트로 재조립한 `InventoryShopIcons.png`를 가방(슬롯0)·상점(슬롯6) 메뉴 아이콘 자리표시자로, `kenney-assets.itch.io/fantasy-ui-borders`(CC0)의 `panel-border-000.png`을 `FantasyPanelBorder.png`로 복제해 우측 메뉴 패널 위에 9-slice(border=16px) 오버레이로 배선했다.

미배선 채로 매니페스트에만 등록된 파일(향후 재사용 대기): `WinterTreesMid/Near.png`, `CaveCrystalRidgeA.png`, `GoblinPixelArtRun/Attack/Death.png`, `GoblinMonsterSpritesheet32.png`(전체 8색 시트 원본, 배선판은 `GoblinMonsterFrame.png` 1프레임만 사용).
