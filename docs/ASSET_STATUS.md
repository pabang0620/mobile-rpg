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

**하이브리드 전략 적용 전이다.** `CREDITS.md`가 아직 없다. 무료 커뮤니티 팩은 아직 도입되지 않았다.

### 2026-09-14 정리: 비AI·비UI 플레이스홀더 제거

사용자 지시로 AI 생성물이 아니고 UI도 아닌 에셋을 제거했다. 제거한 것:

- `Art/Enemies/`의 고블린 커뮤니티 스프라이트 6개(`GoblinPixelArtIdle/Run/Attack/Death.png`, `GoblinMonsterSpritesheet32.png`, `GoblinMonsterFrame.png` - CC0 무료팩, 2026-09-13에 받아온 것) 및 폴더 자체
- `Art/VFX/MagicMissile.png`(무료팩) 및 이제 빈 폴더가 된 `Art/VFX/`
- `Art/Goblin.png`(플레이스홀더 fallback, AI 생성 여부 불명확)
- `Art-Backgrounds-Unused-Sidescroll/`(이미 미사용으로 분리해뒀던 폴더 전체 5파일)
- 시안 단계 배경 4개: `TopdownTown.png`, `MoonshardField.png`, `SapphirePlatformMap.png`, `SapphireTown.png`

**판단 보류(임의로 지우지 않음)**: `MoonCourtyard.png`, `TopdownDungeon.png`, `WorldTiles6x4-v2.png` - 문서에 AI 생성 여부나 폐기 여부가 명시돼 있지 않아 판단이 서지 않았다. 정확한 출처 확인이 필요하면 `lh2d-asset-specialist`에게 위임한다.

유지한 것: Mage 관련 AI 생성 캐릭터 아트 전부(`MageIdle.png`, `MagePortrait.png`, `MagePose*.png`, `MageDirectional.png`, `MageSDDirectional*.png`, `MageSkills.png` - 목록은 이번 정리 시점 기준, 아래 2026-09-14 갱신 참고), `Art/UI/` 폴더 전체(`FantasyPanelBorder.png`, `InventoryShopIcons.png`)와 그 외 UI chrome/버튼 이미지(`HudControls.png`, `MainMenuIcons.png`, `MenuPanel.png`, `UiChrome.png`, `WideButton.png`), `Fonts/`.

### 2026-09-14 갱신: 캐릭터 시트를 단일 통합 시트로 교체

`MageIdleDirectional.png`(2열x4행 유휴 시트)와 `MageWalk4x3-v2.png`(3열x4행 이동 시트) 2개를 폐기하고, `MageTopdownGridSheet.png`(1086x1448, 3열(idle/walkA/walkB) x 4행(Down/Left/Right/Up), 셀 362x362) 단일 시트로 교체했다. 실제 사용 중인 캐릭터 시트는 이제 이 파일 하나뿐이다 - `MageIdle.png`, `MageDirectional.png`, `MageSDDirectional*.png` 등 위 문단이 나열하는 나머지 Mage 원화/시안 파일들은 게임플레이 코드가 참조하지 않는 참고용 원본으로 그대로 둔다(이번 정리에서 손대지 않음). Pivot은 `ArtImportConfigurator.ConfigureCharacterSheets()`가 그룹 단위(전면계열/후면 x idle·walkA/walkB, 4가지 조합)로 적용한다 - 자세한 실측 근거는 `docs/DECISIONS.md`의 "캐릭터 스프라이트 시트를 단일 통합 시트로 교체" 항목.

### 폴더 실측 (참고, 개별 파일 상태는 미실사 - TBD)

각 파일이 실제로 게임에서 쓰이는지, 방향/투명도 검사를 통과했는지는 이번 정리 범위에서 다시 실사하지 않았다 - 지어내지 않고 **TBD로 남긴다.** 정확한 인벤토리가 필요하면 별도 작업으로 `lh2d-asset-specialist`에게 위임해 파일 존재/해시/치수/알파 실측부터 다시 만든다.

### 과거 실패 사례 (참고, 알파 검수 근거)

`MageDirectional.png`는 4×4 방향 시트로 생성됐으나 배경 체크무늬가 알파가 아니라 RGB 픽셀로 구워져 있어 런타임에서 제외됐고, 기존 투명 `MageIdle.png`을 계속 쓰고 있다. 배경 추출 재시도로도 진짜 알파를 만들지 못했다. **AI 생성 에셋은 알파 채널을 실측해서 검수해야 한다** - `Read` 도구로 눈으로 보는 것만으로는 RGB 배경과 투명 배경을 구분하지 못할 수 있다.

## 2026-09-14 갱신: 스킬 아이콘 2개 신규 생성 (기본공격/질주)

UI 개편(좌측 가상패드 + 우측 원형 스킬메뉴, `docs/DECISIONS.md` 참고)으로 기본공격 버튼과 5번째 스킬 "질주"가 추가됐는데, 기존 `Art/UI/SkillIcons.png`(2x2, 비전탄/서리파동/점멸/보호막 4개뿐)에는 둘 다 아이콘이 없었다.

**재사용 후보 확인(채택 안 함)**: `MageSkills.png`(루트 `Art/`, painterly 픽셀아트 스타일) - 알파 채널이 없어(배경이 RGB 검정으로 구워짐) UI 아이콘으로 바로 못 쓴다. `Art/UI/InventoryShopIcons.png`, `Art/MainMenuIcons.png` - 스타일(플랫 아이콘 / 보석형 프레임 아이콘)이 `SkillIcons.png`의 "저폴리 각진 크리스탈, 진한 블루+가끔 골드 베벨" 스타일과 확연히 달라 같은 메뉴에 섞으면 어색하다.

**채택**: gpt-image 스킬(ChatGPT 구독 브릿지, `scripts/gpt_image.mjs generate`)로 `SkillIcons.png`를 `--reference`로 첨부하고 `--background transparent`로 신규 생성했다. 결과는 `Art/UI/SkillIconsExtra.png`(1287x611, 1행 2열 - 기존 시트의 643/644x611 셀 폭과 동일하게 맞춤): 왼쪽 `SkillIcons_BasicAttack`(저폴리 크리스탈 스타버스트), 오른쪽 `SkillIcons_Haste`(저폴리 크리스탈 날개 한 쌍). `ArtImportConfigurator.ConfigureSkillIconsExtra()`가 슬라이스한다.

**알파 채널 실측 검증** (`docs/DECISIONS.md`의 과거 `MageDirectional.png` 알파 사고 재발 방지 원칙 적용): PIL로 원본 생성 이미지와 최종 크롭·리사이즈 후 시트 양쪽 모두 확인 - 네 모서리 픽셀이 `(0,0,0,0)`(완전 투명), 아이콘 내부 픽셀이 alpha 0~255 범위로 분포(안티에일리어싱 경계 포함)하는 실제 알파 채널임을 확인했다(`Read` 도구가 보여주는 흰 배경은 렌더링 관례일 뿐 실제 알파와 무관 - 눈으로만 보고 판단하지 않았다).

## 2026-09-15 갱신: 골드 등급 UI 에셋 6종 배선 (2026-09-14 UI를 재교체)

2026-09-14에 배선했던 UI 에셋(`SkillButtonFrame.png`/`HealthBarFrame.png`/`SkillIconsSet.png`/
`MessagePanelFrame.png`/`WideButton.png`, 아래 절)을 신규 고퀄리티 골드 에셋 6종으로 전부
교체했다 - `Art/UI/SkillButtonFrameGold.png`, `Art/UI/HealthBarFrameGold.png`,
`Art/UI/SkillIconsSetGold.png`, `Art/UI/MessagePanelFrameGold.png`, `Art/UI/MenuButtonGold.png`
(신규 - 닫기/메뉴 버튼 전용, `WideButton.png` 대체), `Art/UI/MenuPanelFrameGold.png`(신규 -
메인 메뉴 패널 배경 전용, 기존에 `MessagePanelFrame.png`를 재사용하던 것을 분리). 배선 세부
(재실측한 슬라이스 좌표·9-slice border·`pixelsPerUnit` 보정·원형 스킬버튼 정사각형 크롭 수정·
반응형 anchor 수정)는 `docs/HANDOFF.md`의 같은 날짜 항목 참고.

**삭제한 파일** (grep으로 새 배선 이후 참조가 전혀 없음을 확인 후 `git rm`): `Art/UI/SkillButtonFrame.png`,
`Art/UI/HealthBarFrame.png`, `Art/UI/SkillIconsSet.png`, `Art/UI/MessagePanelFrame.png`,
`Art/WideButton.png`.

**MenuButtonGold.png/MenuPanelFrameGold.png 크롭 재실측**: 두 파일 모두 "20px 패딩으로 크롭됨"
이라는 보고를 받았으나 그 수치를 신뢰하지 않고 PIL로 알파 바운딩박스를 다시 측정했다 -
`MenuButtonGold`는 실제 알파 콘텐츠가 x[20,972] y[20,250](993x251 캔버스 기준)에서 시작하지만,
9-slice border(테두리에서 남색 내부 채움까지의 거리)는 이와 별개로 색상 전이 지점에서 재실측해
left 114/right 116/top 77/bottom 71px로 구했다(20px는 "그림이 시작하는 지점"이고 114px 등은
"안쪽 채움이 시작하는 지점"이라 서로 다른 값인 게 정상 - 114 > 20이 이를 뒷받침한다).
`MenuPanelFrameGold`도 동일한 방식으로 border left 87/right 88/top 92/bottom 90px을 구했다.

## 2026-09-15 갱신: HUD 신규 에셋 5종 배선 + MenuPanelFrameGold 폐기 (REMEDIATION_PLAN.md Phase 2/3)

`Art/UI/MovementStickGold.png`(1438x902, 베이스 링+노브 2셀), `Art/UI/MenuPanelOdin.png`
(793x1983, 오딘식 메뉴 세로 패널), `Art/UI/MenuSectionHeader.png`(2172x724, 가로 배너 -
지역명 상단 배너와 메뉴 섹션 헤더 양쪽에 재사용), `Art/UI/MenuIconsSet.png`(1774x887,
4x2=8칸 메뉴 아이콘), `Art/UI/MenuLockBadge.png`(1278x1230, 자물쇠 아이콘) 5종을 신규
배선했다(이미 생성되어 있던 파일을 이번 세션에서 배선). 전부 PIL/numpy로 알파bbox·색상
전이 지점을 직접 실측(균등분할 가정 금지 원칙 재적용) - 실측값은 `docs/HANDOFF.md`
2026-09-15 항목과 `Editor/ArtImportConfigurator.cs`의 각 `Configure*` 메서드 주석 참고.

**폐기**: `Art/UI/MenuPanelFrameGold.png`(세로 텍스트 목록 메뉴 배경, 7항목 divider 6개
포함) - `MenuPanelOdin.png`로 완전히 대체되어 새 배선 이후 참조가 전혀 없음을 grep으로
확인 후 `git rm`.

## 2026-09-14 갱신: UI 에셋 전면 교체 (기존 UI 전부 폐기) + HP 바 신규 추가

사용자 지시("지금 있는 UI는 다 버려야해")로 신규 생성된 UI 에셋 4종(`Art/UI/SkillButtonFrame.png`, `Art/UI/HealthBarFrame.png`, `Art/UI/SkillIconsSet.png`, `Art/UI/MessagePanelFrame.png`)을 전부 배선하고 이전 UI 텍스처를 대체했다. 배선 세부는 `docs/HANDOFF.md`의 같은 날짜 "UI 에셋 전면 교체 + HP 바 신규 추가" 항목 참고.

**삭제한 파일** (grep으로 새 배선 이후 참조가 전혀 없음을 확인 후 `git rm`):

- `Art/UI/FantasyPanelBorder.png` - `MessagePanelFrame.png`로 대체.
- `Art/UI/SkillIcons.png`, `Art/UI/SkillIconsExtra.png` - `SkillIconsSet.png` 1개로 통합.
- `Art/UiChrome.png`, `Art/MainMenuIcons.png`, `Art/MenuPanel.png`, `Art/HudControls.png` - 이번 작업 전부터 이미 코드 어디에서도 참조되지 않던 미사용 UI chrome 에셋(위 "2026-09-14 정리" 문단이 "그 외 UI chrome/버튼 이미지"로 유지 대상 나열했던 것과 달리, 실제 grep 재확인 결과 참조가 전무해 이번에 함께 정리했다).

**유지한 파일**: `Art/WideButton.png`(메시지 패널 닫기 버튼, 계속 참조됨), `Art/UI/InventoryShopIcons.png`(별도 용도, 이번 작업과 무관).

**신규 파일**: 위 4개 신규 텍스처 + `Presentation/UI/HealthBarView.cs`(HP 바 채움 게이지 컴포넌트, 현재는 전투 시스템이 없어 100% 고정 표시).

알파 채널·9-slice border·그리드 셀 경계는 전부 `Read`로 눈으로 보지 않고 PIL/numpy로 알파 컬럼/로우 카운트 프로파일을 직접 실측해 구했다(이 문서가 반복 강조하는 "측정하지 않고 균등분할을 가정하지 말 것" 원칙 재적용 - SkillButtonFrame/HealthBarFrame/SkillIconsSet 셋 다 실제로는 정확히 절반/균등 그리드가 아니었다). 컴파일·EditMode 테스트 30/30·씬 파일 파싱(스프라이트 참조 null 아님 확인)·플레이어 재빌드·35초 이상 프로세스 생존까지 확인했고, 화면 렌더링·미학 판단은 하지 않았다(사용자 몫).

## 2026-09-15 갱신: 캐릭터 플로우 아트(Login/CharacterSelect/CharacterCreate) + 워리어 클래스 아트 실측 배선 완료

이전 세션이 코드 골격만 만들어두고 미뤘던 부분 - 아트가 도착해 전부 배선하고
실측을 확정했다.

- `Art/UI/Title/TitleBackground.png`(1672x941), `TitleLogo.png`(1942x809,
  Simple), `PortraitMage.png`/`PortraitWarrior.png`(1024x1536) - Simple
  스프라이트, border 불필요.
- `Art/UI/Title/CharacterSlotFrame.png`(793x1983, 9-slice) 실측 border
  `(36,31,36,32)`, `InputFieldFrame.png`(2170x725, 9-slice) 실측 border
  `(36,60,35,54)` - 둘 다 PIL로 골드프레임<->navy 내부색 전이 지점을
  40%-70% 구간 다중 샘플 mode로 확정(`CharacterFlowArtImportConfigurator.cs`).
- `Art/WarriorTopdownGridSheet.png`(1086x1448, 3x4 격자, Mage와 동일 셀크기/
  PPU=302)의 행별 pivot을 Mage와 같은 방법(3포즈 alpha bbox 평균)으로 실측:
  Down(0.56,0.00) Left(0.55,0.02) Right(0.48,0.00) Up(0.51,0.19) - Mage와
  달리 Left/Right를 강제로 같은 값으로 묶지 않음(원화 자체가 비대칭, 근거는
  `docs/DECISIONS.md` 2026-09-15 항목).
- `Art/UI/WarriorSkillIconsSetGold.png`(1536x1024, 3x2)의 6개 아이콘을
  등분할 512x512 셀 각각의 alpha bbox로 재크롭 - 라벨(대검베기/돌진/
  회오리베기/방패막기/전쟁함성/대지강타)과 실제 그림(검베기 이펙트/화살표/
  쌍날/방패/포효하는 사자머리/바위 뚫는 건틀릿)이 정확히 일치함을 육안
  확인. 게임 전체와 통일된 블루+골드 크리스탈 톤(의도된 스타일, 결함 아님).

**실사용 확인**: `SapphireSceneBuilder.BuildEverything()`(신설, VillageHub +
Login/CharacterSelect/CharacterCreate 4개 씬 전부 재빌드) ->
`SapphireBuildPlayer.BuildWindows` 재빌드 후 스크린샷 6장
(`generated-images/diagnostics/flow_*.png`)을 오케스트레이터가 직접 열어
확인 - 핑크 텍스처/빈 화면/겹침/잘림/한글 깨짐 없음, 워리어·법사 둘 다 발이
바닥 타일에 정확히 붙어 서있음.

## 2026-09-15 갱신: 젬리스(gemless) 메이플스토리M풍 UI 킷 배선 + 전사 VFX 실제 스프라이트 배선 완료

`tools/ui_kit/build_ui_kit.py`(procedural, PIL 기반, SCALE=3 supersample - AI
이미지 아님)로 생성한 15개 최종 에셋을 실제 게임 코드에 배선했다. 베이지
양피지 패널 + 반투명 다크 HUD + 육각컷 버튼 스타일로, 금색/보석 장식을
전부 제거했다(사용자 스타일 확정 지시).

**교체(기존 파일명 유지, 내용 전면 교체)**:
- `Art/UI/MenuPanelOdin.png`(1200x3000), `MessagePanelFrameGold.png`(1680x960),
  `Art/UI/Title/CharacterSlotFrame.png`(780x1950) - 전부 `build_beige_panel`
  계열, border 실측(PIL inward-scan) 32px 4면 동일(생성 스크립트 자체
  파라미터 - margin 6 + radius 4 + 이중선 두께, SCALE 3 배 - 와도 정확히
  일치).
- `Art/UI/Title/InputFieldFrame.png`(1080x180, 단일 외곽선 사각형, border
  22px 4면), `Art/UI/Title/TitleLogo.png`(1942x809, Simple, 무변경 로직).
- `Art/UI/HealthBarFrameGold.png`(780x120, Track/Fill 각 780x48 캡슐, 상단
  cell=Track 하단 cell=Fill), `Art/UI/GaugeFillMana.png`(780x48) -
  `HealthBarFrameGold`의 HP Fill을 hue-rotate(215°)한 것, 절차 동일 유지.
  barHeight를 53.4(구 자산 보정값)에서 16(신규 자산 native aspect
  780:48=16.25:1과 target 260:16=16.25:1이 정확히 일치 - 별도 보정 불필요)
  으로 축소.
- `Art/UI/SkillButtonFrameGold.png`(756x396, Skill/BasicAttack 원형 2개),
  `Art/UI/MovementStickGold.png`(792x480, Base/Knob 원형 2개) - 전부 PIL
  alpha bbox로 재실측(포뮬러 예측값과 1px 이내로 일치).

**신규 추가**:
- `Art/UI/MenuSectionDivider.png`(1200x60, 좌/우 600x60 페이딩 라인 2셀) -
  Odin 메뉴 섹션 헤더의 배경 배너(`MenuSectionHeader.png`)를 완전히
  대체(배너 자체를 없애고 타이틀 좌우에 얇은 선만 남김).
  `VillageHubMenuBuilder.BuildOdinSectionHeader` 전면 재작성.
- `Art/UI/RegionNameplate.png`(540x96, 핀 아이콘 내장 캡슐, border
  152/0/54/0) - 상단 지역명 배너(`MenuSectionHeader.png` 재사용분)를 대체.
  `VillageHubUiBuilder.BuildRegionNameBanner` 재작성(텍스트를 핀 오른쪽에
  좌측정렬).
- `Art/UI/ButtonPrimary.png`/`ButtonSecondary.png`(각 1032x456, 육각컷
  Normal/Pressed 2셀, border 53px 4면 - corner cut 50.4px 실측 확인 후
  버퍼) - 로그인/생성 화면 주요 버튼(시작하기/생성)과 "캐릭터 선택으로"
  버튼(백/풋터 2곳)에 적용, `MenuButtonGold.png`는 그 외 버튼(선택/삭제/
  +생성/닫기/확인/취소/메뉴 그리드 항목)에 그대로 유지.
- `Art/UI/MenuHamburgerIcon.png`(240x216, 배경 없는 라인 아이콘+그림자) -
  우상단 "메뉴" 버튼의 배경 pill을 제거하고 아이콘 단독으로 교체
  (150x72->56x56).
- `Art/UI/LevelBadgeHex.png`(192x192, 육각 배지) - "Lv.1" 텍스트 배경으로
  신규 추가.

**폐기**: `Art/UI/MenuSectionHeader.png`(+.meta) - 위 두 신규 자산(Divider/
Nameplate)이 기존 두 용도(섹션 헤더/지역명 배너)를 모두 대체해 참조 0건
확인(grep) 후 `git rm`.

**전사 VFX**: `WarriorSkillVfxAtlas.png`(1586x992, 8x5, 셀 198.25x198.4)와
`WarriorGroundSlamPadded.png`(2079x756, 8프레임 스트립, 프레임
259.875x756)를 `WarriorSkillVfxImporter`(신설, 이름 기준 행 매핑)로
슬라이스해 `WarriorSkillVfxLibrary`(신설 ScriptableObject, Frames[48])로
빌드, `WarriorSkillVfxPlayer`를 프로시저럴 프리미티브에서 실제 아틀라스
프레임 재생으로 전면 재작성. 상세 근거·기본공격 신규 사거리는
`docs/DECISIONS.md`/`docs/HANDOFF.md`의 같은 날짜 항목 참고.

**검증**: Unity CLI 컴파일 0에러, EditMode 60/60(전사 기본공격 range 테스트
1건 신규), `SapphireSceneBuilder.BuildEverything` -> `SapphireBuildPlayer.
BuildWindows` 재빌드, 스크린샷 12장(`generated-images/diagnostics/final_*.png`)
전부 오케스트레이터가 직접 확인 - 보석/금테 잔존 없음, 조이스틱 노브
완전한 원, 메뉴 구분선 꺾쇠 없이 페이딩 라인만, 6개 전사 스킬 VFX 전부
실제 스프라이트로 렌더(placeholder 도형 아님).

## 다음 작업 (TBD/후속)

- 지형/배경용 무료 팩 선정(라이선스 확인 포함) 및 이 프로젝트에 도입.
- 현재 AI 생성 캐릭터(Mage) 아트가 하이브리드 전략의 "정체성 요소"로 유지할 수준인지, 재생성이 필요한지 판단. 몬스터 아트는 2026-09-14 정리로 커뮤니티 플레이스홀더가 제거되어 현재 전무하므로 신규 구현 시 처음부터 다시 만들어야 한다.
- 이 레포에 `CREDITS.md` 신설(CC-BY 팩을 채택하는 즉시).
- `Art/` 폴더 전체 실사(파일 존재/해시/치수/알파/사용 여부) 및 상태 인벤토리 재구축.
- ~~카메라 마이그레이션에 맞춰 타일 크기/PPU 기준으로 에셋 치수 재검토~~ - 2026-09-14 완료: 카메라 `assetsPPU=72`, 캐릭터 시트 `ppu=302`, 지형 아틀라스 `ppu=512`로 전부 확정(`docs/DECISIONS.md` 참고).
- 몬스터 아트는 여전히 전무하다 - 신규 전투 콘텐츠 착수 시 하이브리드 전략에 따라 처음부터 제작 필요.
