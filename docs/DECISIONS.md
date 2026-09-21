# 결정 기록

## 2026-09-21: 64px 모듈형 지형 제작 — TS01부터 독립 검수

사용자가 제공한 6시트 타일셋 설계를 채택한다. 첫 단계는 AI 생성 잔디/흙 소재를 입력으로 하는
64px 연결 타일, 47방향 Blob 규칙, 재생성 도구, 독립적인 30×30 테스트 씬이다.
큰 맵 이미지를 잘라 배치하거나 타일을 겹쳐 경계를 가리는 방식은 사용하지 않는다.
호환되는 접점과 변형 타일 외곽을 공유하고 정수 좌표/Point 샘플링으로 배치한다.
명명된 P001~P024 프리셋은 유지하고, 완전한 자동 연결에는 PA001~PA047을 별도로 추가한다.
기존 플레이 씬의 전환은 전체 지형과 충돌 통합 단계에서 진행한다. 이번 테스트 씬은 자동으로
플레이어 빌드에 추가하지 않는다. 상세 규격과 생성 프롬프트는 `MODULAR_TILESET_64.md`,
`MODULAR_TILESET_PROMPTS.md`를 참조한다. 원본 기획 문서는 변경하지 않는다.

기술 방향 결정을 날짜순으로 남긴다(최신이 위). 기획 자체(무엇을 만들지)는 `docs/planning/*.md`가 SSOT이고 여기서는 다루지 않는다 - 여기는 "어떻게 구현할지"에 대한 결정만 남긴다.

## 2026-09-16: 메뉴판 확장 - 패널 자체를 줄이지 않고 내부 패딩만 대칭화하기로 결정 (스킬부채꼴 가림 회귀 발견 후 되돌림)

**작업**: 마을 Odin 메뉴판 폭 30% 확장(400->520) + 시스템 섹션에 "캐릭터 선택"/"게임 종료" 추가 + "상단여백=하단여백" 요구.

**시도 1 (기각)**: `OdinPanelTopMargin`/`OdinPanelBottomMargin`을 `(720 - 콘텐츠높이)/2`로 계산해 패널 자체를 콘텐츠 높이(444+padding)에 맞춰 줄이고 화면 중앙에 배치. 코드상으로는 top==bottom이 정확히 성립했지만, 빌드 후 스크린샷(`generated-images/diagnostics/v3_menu.png`)에서 `VillageHubSkillMenuBuilder`의 전사/마법사 스킬부채꼴(같은 우하단 코너, root anchoredPosition (-112,118), radius 172, arc 80-200deg)이 패널 바닥 마진이 늘어난 만큼(20->98.5) 아래쪽에서 비쳐 보이는 회귀를 발견했다 - 특히 arc 200도 버튼(캔버스 좌표 약 (1006,59))이 새 마진 구간(20~98.5) 안에 들어와 버렸다.

**시도 2 (더 나쁨, 기각)**: 반대로 `OdinPanelBottomMargin`을 20->100으로 올려서 "더 안전하게" 가리려 했으나, 마진을 올리면 패널이 오히려 더 짧아져서(바닥 모서리가 더 위로 올라감) 스킬부채꼴 전체(기본공격 버튼 포함)가 완전히 노출되는 더 심한 회귀가 나왔다. 마진↑=커버리지↓라는 기초적인 방향을 착각했던 시행착오 - 재검증 스크린샷으로 바로 확인·롤백.

**최종 결정**: `OdinPanelTopMargin=0`/`OdinPanelBottomMargin=20`을 원래 값 그대로 둔다(패널의 바깥 크기·위치 자체는 안 건드림 - 이러면 스킬부채꼴 커버리지는 기존과 동일하게 유지된다). "상단여백=하단여백" 요구는 대신 패널 **안쪽**의 콘텐츠 패딩(`OdinContentPadding`, 첫 섹션 헤더 위 공백과 마지막 섹션 아래 공백)을 동일하게 만드는 것으로 재해석해 만족시켰다 - `(패널높이 700 - 콘텐츠높이 444)/2 = 128px`씩 동일하게 계산됨(`ComputeSectionsHeight()`가 MenuCatalog 데이터로 동적 계산, 항목이 늘어나면 자동 재계산). 사용자가 실제로 보고 있던 문제("아래쪽에 빈 공간 몰려있음", 스크린샷 `final_village_warrior_menu.png`)는 콘텐츠가 위쪽에 쏠려 있고 여백이 전부 바닥에 몰려 있던 게 핵심이었으므로, 이 재해석이 실제 불만을 정확히 해소한다.

**남은 잠재적 결함(이번 범위 밖으로 명시적으로 남김)**: 패널 우하단 모서리 장식(`MenuPanelDark.png`의 곡선 컷아웃 - 4개 모서리 전부에 있는 의도된 디자인)이 딱 스킬부채꼴의 arc≈200도 버튼 위치와 겹쳐서, 메뉴가 열려 있을 때 그 버튼이 옅게 비쳐 보이는 현상이 여전히 남아있다. 이건 이번 작업(패널 폭 확장)이 만든 회귀가 아니라 - 폭을 넓히기 전 원래 마진(20)에서도 기하학적으로 이미 존재하던 현상이며, 지금까지 화면에 안 보였던 이유는 실제로는 죽어있던(Build()에서 호출 안 됨) `CharacterSelectButton` 풋터 버튼이 마침 그 자리를 시각적으로 덮고 있었기 때문이다(그 풋터를 이번 작업 B에서 정식으로 제거). 클릭은 백드롭이 전체화면 raycast를 막고 있어 기능적 버그는 아니고 순수 시각적 문제다. 패널 바깥 마진을 조정해 고치는 두 방향(시도 1/2) 모두 실패했으므로, 스킬부채꼴 위치 자체를 조정하거나 모서리 컷아웃 아트를 바꾸는 더 큰 작업이 필요 - 이번 세션 범위(메뉴판 확장/시스템 섹션/로그인 에셋) 밖이라 손대지 않았다.

## 2026-09-16: 프리미엄 캐릭터선택/생성/로그인 UI - 9-slice 비대칭 border, 카드 레이아웃, 텍스트 truncate 버그

**결정 1 - CharacterSlotFrameV2 9-slice border를 비대칭으로 확정**: 작업 지시서가 "select 카드 340/create 카드 250, 실측 후 스트레치 vs Simple+고정폭 중 판단"을 요구했다. `tools/ui_kit/build_title_kit_v2.py`의 `build_slot_frame` 소스를 직접 읽어 배지 노치(`BADGE_NOTCH_CY=34`+`FRAME_MARGIN=4`=38 target 중심, `BADGE_NOTCH_R=30` target 반경 → 바닥 엣지 ~69.5 target)와 구분선(`divider_y=FRAME_H(400)-NAMEPLATE_BAND_H(76)`=324 target, 바닥 기준 76 target)의 정확한 좌표를 계산했다 - "실측"을 스크린샷 눈대중이 아니라 생성 스크립트의 소스 상수를 직접 읽어 계산했다. Top border 72 target(216 native), Bottom border 90 target(270 native), Left/Right 16 target(48 native, 생성기 자체의 `FRAME_BORDER`)로 확정 - Unity Sliced Image는 native border를 통해서만 "고정 vs 스트레치" 경계를 정의하므로, 이 비대칭 값 하나로 카드 높이가 340이든 250이든 배지·구분선이 항상 같은 절대 거리(72/90 canvas 유닛, PPU=300=100×SCALE_V2(3) 덕분에 1 target px = 1 canvas 유닛)를 유지한다. **기각한 대안**: 카드마다 별도 9-slice 값을 쓰는 방안 - 관리 부담만 늘고 위 비대칭 값이 두 카드 높이 모두에서 이미 충분히 여유(select 340 middle-zone 178유닛, create 250 middle-zone 88유닛)를 확보해 불필요.

**결정 2 - Select/Delete 버튼은 자산의 authored target 폭(96)을 그대로 쓰지 않고 기존 70 유지**: `ButtonSelectV2`/`ButtonDeleteV2`의 자체 authored 셀 크기는 96x40 target이지만, 카드 폭 260에 `CardButtonEdgeInset=48`(D3 스펙 잔존)을 적용하면 두 버튼(96 폭 + 16 간격)이 안전지대 밖으로 밀려난다 - 실측 계산으로 확인(선택 버튼 xMin=-104가 안전지대 xMin=-82를 벗어남) 후, 자산의 9-slice border(native 40, canvas 13.3 유닛)가 훨씬 작아 어떤 on-screen 폭에서도 안전하게 렌더된다는 점을 근거로 기존 D3 폭(70)을 그대로 유지했다. "자산의 authored 크기를 그대로 써야 진짜"라는 가정은 9-slice 자산에는 적용되지 않는다는 걸 재확인.

**결정 3 - VerifyButtonsInsideCard의 Y축 안전지대를 대칭 48px inset에서 하단 고정 존(90유닛) 기준으로 재정의**: 첫 빌드가 `Layout containment violated`로 실패했다 - 원인은 D3 시절 체크가 "카드 4면 모두 48px 인셋"을 가정했는데, 새 레이아웃은 버튼을 의도적으로 카드 바닥 엣지에 딱 붙인다(9-slice 하단 고정 존 안이라 시각적으로 안전). X축은 기존 48px 인셋 의미(테두리 회피)가 여전히 유효해 유지하고, Y축만 "하단 고정 존 높이(90)" 기준으로 바꿨다 - 체크의 의도(9-slice 아트가 스트레치 없이 안전하게 보이는 영역인지 검증)에 맞게 재정의한 것이지 체크를 약화시킨 게 아니다.

**결정 4 - 텍스트 truncate 버그: "폰트 크기" 이론이 아니라 "박스 높이" 실측으로 확정**: 스크린샷에서 이름/레벨/클래스 라벨이 완전히 안 보이는 걸 발견하고, 처음엔 2026-09-15 D3 fix가 남긴 "새 폰트 크기의 첫 렌더링 버그"를 의심했으나, 같은 화면에 이미 동일 크기(22/16/26)로 렌더 중인 다른 Text(선택/삭제/+생성 버튼 라벨)가 정상 표시되는 걸 보고 그 가설을 기각했다. 실제 원인은 `Text.verticalOverflow`의 기본값(Truncate)이 `sizeDelta.height`가 fontSize의 필요 줄높이(~1.2×fontSize)보다 작을 때 글자를 완전히 잘라내는 것 - 좁은 이름표 안에 이름+클래스Lv 두 줄을 욱여넣으려고 박스 높이를 20/16으로 줄인 게 원인이었다. 박스 높이를 fontSize 대비 안전하게 키우고(22→26, 16→20, 캐릭터생성 라벨 26→32) `verticalOverflow=Overflow`를 명시하는 것으로 해결 - 재캡처로 실제 렌더링 확인(이론만으로 판정하지 않음).

## 2026-09-15: UI 킷 배선 최종 라운드 - 버튼 적용 범위, PPU 통일, 전사 기본공격 사거리, VFX 좌표계 결정

**결정 1 - PPU를 자산별 개별 공식에서 공용 상수(300)로 통일**: `tools/ui_kit/build_ui_kit.py`가 만드는 모든 9-slice 자산은 `SCALE=3`(native = target * 3) 고정이라, 어느 자산이든 `pixelsPerUnit = 100 * native/target = 300`이 항상 성립한다. 기존 관례(`ArtImportConfigurator.ConfigureUiFrames` 등)는 자산마다 `100*nativeWidth/targetWidth` 공식을 개별 계산했는데, v3 자산군에는 이 계산이 전부 300으로 수렴하므로 `ArtImportConfigurator.UiKitV3PixelsPerUnit` 상수 하나로 대체했다. `CharacterFlowArtImportConfigurator`의 두 자산(CharacterSlotFrame/InputFieldFrame)은 기존 방식(`GetSourceTextureWidthAndHeight` 기반 동적 계산)을 그대로 둬도 결과가 자동으로 300이 되므로 손대지 않았다(코드 변경 없이 값만 바뀜).

**결정 2 - ButtonPrimary/Secondary 적용 범위: "캐릭터 선택으로" 2곳 + 로그인/생성 주요 버튼 2곳만**: 작업 지시서가 명시한 대상(시작하기/생성=Primary, 캐릭터 선택으로=Secondary)을 문자 그대로 좁게 적용했다 - "캐릭터 선택으로" 텍스트를 가진 버튼이 `CharacterCreateSceneBuilder`(뒤로가기)와 `VillageHubMenuBuilder`(메뉴 풋터) 2곳에 존재해 둘 다 Secondary로 교체했지만, 그 외 버튼(CharacterSelect의 선택/삭제/+생성, 메시지·확인 다이얼로그의 닫기/확인/취소, Odin 메뉴 그리드 항목)은 지시서에 없어 `MenuButtonGold.png`를 그대로 유지했다. **기각한 대안**: 신규 UI 스타일 일관성을 위해 전체 버튼을 Primary/Secondary로 확대 교체 - 스코프 크립 금지 원칙(`feedback_dont_expand_given_scope`)에 따라 기각, 필요하면 후속 작업으로 사용자가 명시적으로 요청.

**결정 3 - 전사 기본공격("대검베기") 사거리 = 2칸, Line 형태**: 지시서가 "1~2칸으로 신규 정의"만 요구하고 정확한 값·형태는 위임했다. 대검(양손검)의 리치를 표현하기 위해 상한값인 2칸을 택했고, 형태는 `Cone`(폭 넓은 슬램)이 아니라 `Line`(정면 직선)을 택했다 - 기존 `GroundSlam`이 이미 "폭 넓은 지면 슬램" 이미지를 쓰고 있어 같은 시각 언어(BasicAttackSlash 스프라이트, 좌측edge pivot으로 늘어나는 직선 베기)와 형태 모두 구분하기 위함. `Domain/Skills/WarriorCombatConstants.BasicAttackRangeTiles=2`로 새 Domain 상수를 신설했다(SkillCatalog에는 항목이 없음 - 기본공격은 SkillCatalog 밖의 별도 버튼이라는 기존 설계를 그대로 유지, `SkillCatalog` 클래스 doc 참고).

**결정 4 - GroundSlam VFX 좌표계: pivot 중앙 + 위치=1타일 전방 tile 중심**: 지시서 원문("위치=actorPos+facing*1타일")을 문자 그대로 "1타일 뒤(actor 좌표)에서 시작해 1타일 전진하는 좌측-edge pivot" 방식으로 구현하면, 전사 원점(actor 위치)에서 시작해 딱 1타일만 앞으로 나가는 스프라이트가 origin 타일과 destination 타일에 절반씩 걸쳐, 실제 `TilesInFrontCone(origin,facing,1)`이 가리키는 "전방 1칸(타일 경계 0.5~1.5)"과 어긋난다. 대신 pivot을 중앙(0.5,0.5)으로 하고 위치를 "전방 1칸의 중심"(`Point(origin+facing.ToOffset())`)으로 잡아, 스프라이트의 깊이축(가로, ppu=프레임폭이라 1로컬유닛=1타일)이 정확히 그 칸의 앞뒤 경계(0.5~1.5)를 덮도록 했다 - `TilesInFrontCone`의 실제 판정 범위와 시각 효과가 일치하는 것을 좌표 계산으로 확인(코드 주석에 수록).

**결정 5 - 스크린샷 검증용 임시 디버그 코드는 커밋 전 완전 제거**: 전사 스킬 VFX를 스크린샷으로 확인하기 위해 `RadialSkillMenu`에 `-sapphire-warrior-skill=<name>` 커맨드라인 훅(0.3초 간격 반복 캐스트)을 임시로 추가했었다. Dash는 실제로 캐릭터를 이동시켜(TryBlink) 반복 호출 시 ~1.2초 만에 맵 경계에 막혀 이후 전부 무효 캐스트가 되는 문제가 있어, 가짜 origin(현재 위치에서 facing 반대 방향으로 1칸)을 사용해 실제 이동 없이 VFX만 재생하도록 우회했다(reflection으로 GridMover 내부 위치를 강제 리셋하는 방식도 시도했으나 효과가 없어 폐기 - 정확한 원인은 특정하지 못함, `docs/HANDOFF.md` 참고). 스크린샷 12장을 전부 확보한 뒤 이 훅 전체를 삭제하고 컴파일·EditMode 60/60·씬 재빌드·플레이어 재빌드를 다시 통과시켰다 - 최종 커밋에는 디버그 코드가 전혀 포함되지 않는다.

## 2026-09-15: 전사 pivot은 좌/우를 강제로 맞추지 않음 (Mage 관례와의 의도적 차이)

`ArtImportConfigurator.BuildMageGridSlices`는 Left/Right 행에 동일한 pivot
`(0.59, 0.08)`을 강제한다("Side-view feet must share the same baseline"
주석). `WarriorArtImportConfigurator`의 pivot을 재측정하면서 같은 관례를
그대로 적용할지 검토했으나, 실측 결과 워리어 원화 자체가 Mage와 달리
좌우 비대칭이었다(Left 컨텐츠 bbox height 351-354px, 셀 상단에 닿음; Right는
height 328px에 상단 여백 33px). Mage의 강제 통일은 "원화가 원래 대칭인데
좌표 평균이 우연히 갈라진 경우"를 보정하려는 조치였지, 원화가 실제로
비대칭인 경우까지 인위적으로 맞추라는 뜻은 아니라고 판단해 - 워리어는 각 행
(Down/Left/Right/Up) 실측값을 그대로 사용한다: Down(0.56,0.00)
Left(0.55,0.02) Right(0.48,0.00) Up(0.51,0.19). 스크린샷(`flow_village_
warrior.png`, `flow_village_mage.png`)으로 두 클래스 다 발이 바닥 타일에
정확히 붙어 서있는 것을 육안 확인해 이 판단이 실제로 맞았음을 검증했다.

## 2026-09-15: MenuSectionHeader crop/border 재실측 - 기존 값이 자기 자신의 측정법과 불일치

2026-09-16 항목(F6.4 fix, 아래)이 도입한 crop `(17,306,2138,145)` / border
`(232,23,231,26)`을 그 항목이 명시한 측정법(행 alpha-pixel-count가 크롭된
너비의 90%를 넘는 구간) 그대로 PIL로 재실행한 결과, 실제 dense band는
rows 287-412(height 126)로 나왔다 - 기존 145는 위 14px/아래 6px의
"90% 미달" 여백을 포함하고 있었다(직접 검증: row 280은 밀도 6.9%, row 287에서
급격히 94%로 뛴다 - 미세한 오차가 아니라 명백히 다른 구간). x 방향도
재측정한 실제 alpha bbox는 [9,2161](width 2153)로, 기존 [17,2154]는 좌우
각 ~8px/~5px를 잘라내고 있었다(이 세션의 작업 지시서가 사전에 예상한
"좌우 끝 약 9px 잘림"과 정확히 일치). Border(9-slice 좌우 골드 장식 두께)도
재측정한 결과 40%-70% 구간 11샘플 median `(53,23,52,23)`로 나왔다 - 기존
232/231은 실제 골드 장식이 x=53-56 부근에서 이미 flat navy로 안정됨에도
그보다 4배 이상 큰 값이었다(400폭 호출부 기준 43%를 근거 없이 고정 영역으로
낭비하고 있었던 셈). **판단**: 기존 값을 만든 세션의 "재측정" 서술 자체가
실제 실행 결과와 다르다는 근거(위 수치 재현 결과)가 명확하므로, 새로 실측한
값으로 교체한다(기존 값을 신뢰해 유지하는 대신). MenuButtonGold.png의 border
`(114,71,116,77)`도 같은 방식으로 재검증했으나, 캡슐형 아이콘의 라운드캡이
실제로 안정되는 지점(x≈90-106)보다 다소 크지만 안전한 방향(과대, 시각적
왜곡 없음)이라 결함으로 보지 않고 변경하지 않았다 - 방향이 다르면(과소,
캡을 잘라 늘어뜨림) 결함이지만 과대는 9-slice에서 corner를 더 많이
고정시킬 뿐이다.

## 2026-09-16: REMEDIATION_PLAN.md Phase 2(HUD) + Phase 3(오딘식 메뉴) 구현 결정

**결정 1 - 스킬 부채꼴 5번째 슬롯("Dash") 콘텐츠 불일치 해소**: REMEDIATION_PLAN.md와 이번
작업 지시서는 "비전탄/서리파동/점멸/보호막 + 질주(이동속도 버프)"라는 옛 5스킬 모델을
전제하지만, 실제 `SkillCatalog.cs`는 이미 같은 날 더 이른 커밋(`9d2a71d`)에서
마력쉴드/텔레포트/낙뢰/고드름/번개창 5개의 실제 스펠로 전면 교체되어 있었고 이동속도 버프
스킬(`skill.haste`)은 존재하지 않는다(`GridMoveAnimator.ActivateSpeedBoost`/
`IsSpeedBoosted`는 코드에 남아있지만 그 커밋 이후 아무도 호출하지 않는 고아 코드).
`docs/HANDOFF.md`가 그 커밋의 스킬 교체를 문서에 반영하지 않아 생긴 문서 drift다.

**선택**: 5개 실스펠을 전부 그대로 유지(키 1-5, 클릭 전부 이전과 동일하게 동작)하면서
지시서의 레이아웃 요구(부채꼴 4개+부채꼴 밖 1개)만 만족시킨다. `SkillCatalog.All[0..3]`이
부채꼴 4개 슬롯, `All[4]`(번개창)가 "Dash" 자리(부채꼴 밖, 같은 반지름 200에서 30° 더 나간
220°)를 차지한다. **기각한 대안**: (a) 고아 상태인 `ActivateSpeedBoost`를 되살려 5번째
슬롯에서 호출 - 현재 게임 디자인에 더 이상 없는 능력을 되살리는 콘텐츠 결정이라 이번
레이아웃 작업 범위를 벗어난다고 판단. (b) 실스펠 5개 중 1개를 UI에서 빼서 "진짜 5개"
모델에 억지로 맞춤 - 이미 배선되어 작동하는 콘텐츠를 이유 없이 제거하는 것이라 기각.

**결정 2 - 아이콘 크기: "프레임 전체의 62%"가 아니라 "개구부의 62%"**: 기존 코드는
`inset=size*0.2`(버튼 전체 지름의 60%를 아이콘이 차지)였는데, 실측한 프레임 개구부(내부
남색 영역, 셀 경계가 아님)는 버튼 지름의 62.7%(스킬)/65.7%(기본공격)뿐이었다 - 아이콘이
개구부보다 커서 프레임 테두리를 침범하고 있었다. 지시서의 "개구부 지름의 62%"를 그대로
적용해 스킬 아이콘 ~37.3px, 기본공격 아이콘 ~57.0px로 축소했다.

**결정 1-1 - 메뉴 그리드 열 개수: 4열 (REMEDIATION_PLAN.md와의 실제 충돌)**:
`docs/REMEDIATION_PLAN.md` 61행은 "5열 아이콘 그리드"라고 서술하지만, 이번 세션의 작업
지시서는 명시적으로 "a 4-column grid of items (cell width = (400 - 2*margin) / 4 ...)"라고
못박았다. 지시서 자체의 충돌 해소 규칙("문서와 지시서가 사실관계에서 충돌하면 더 최신·구체
적인 지시서를 따르되 충돌을 보고할 것")에 따라 4열로 구현했다. 현재 카탈로그의 모든 섹션이
4개 이하 항목이라 4열/5열 어느 쪽이든 화면상 줄바꿈이 발생하지 않아 시각적으로는 차이가
드러나지 않지만, 향후 항목이 5개 이상인 섹션이 추가되면 줄바꿈 위치가 달라진다 - 그 시점에
`REMEDIATION_PLAN.md`를 4로 정정할지, 코드를 5로 바꿀지 재확인이 필요하다(현재는 손대지
않음, 코드 주석에도 동일 근거 기록).

**결정 3 - 메뉴 구조를 데이터(MenuCatalog) + 빌더(VillageHubMenuBuilder) + 컨트롤러
(MainMenuPanel)로 3분리**: 이전 `MainMenuPanel`은 UI 생성(레이아웃)과 클릭 이벤트 바인딩을
한 클래스에 같이 두고 있었다. Phase 3 요구(항목 추가 시 빌더 코드 무변경)를 만족시키려면
데이터를 별도 정적 클래스(`MenuCatalog`)로 빼고, 빌더는 그 데이터를 순회만 하고,
`MainMenuPanel`은 완성된 버튼 배열을 받아 클릭 이벤트만 연결하는 얇은 컨트롤러로 남겨야
한다고 판단해 3분리했다.

**결정 4 - `VillageHubUiBuilder.cs` 파일 분할**: Phase 2+3을 모두 얹은 뒤 이 파일이
800줄을 넘어섰다. 이 코드베이스가 이미 `SapphireSceneBuilder`에서 `ArtImportConfigurator`·
`VillageHubTerrainBuilder`를 관심사별로 분리해둔 전례가 있어, 같은 원칙으로
`VillageHubSkillMenuBuilder.cs`(스킬 부채꼴, Phase 2)와 `VillageHubMenuBuilder.cs`(오딘식
메뉴, Phase 3)를 분리했다. 결과: `VillageHubUiBuilder.cs` 455줄, 나머지 두 파일 각각
250·230줄 안팎.

**결정 5 - 한글 폰트 버그 발견 시 수정 범위**: `VillageHubUiBuilder.cs`의 모든 Text가
Unity 내장 `LegacyRuntime.ttf`(한글 미지원)를 쓰고 있어서 이 파일이 만드는 모든 한글
라벨이 실제로는 글리프 누락으로 렌더될 것이라는 실제 결함을 발견했다. 이 작업이 만든
결함은 아니지만(이전 세션부터 있었음) 이번에 손대는 모든 라벨(레벨 텍스트·지역명 배너·
메뉴 섹션/항목·서브패널 제목 등)에 직접 영향을 주므로, 별도 작업으로 미루지 않고 같은
diff 안에서 `LoadKoreanFont()` 헬퍼(`Fonts/NotoSansCJKkr-Regular.otf` 로드) 하나로
일괄 수정했다 - `feedback_fix_defects_dont_ask` 원칙(발견한 결함은 물어보지 않고 고친다)
적용.

**검증**: Unity CLI(6000.5.9f1) 컴파일, EditMode 테스트 33/33,
`SapphireSceneBuilder.BuildAll` 재실행(신규 빌드타임 assertion `VerifyNoOverlap`/
`VerifyOnScreen` 통과 포함) 후 `VillageHub.unity`를 Python으로 직접 파싱해 스킬버튼 5개
좌표·상호거리, 메뉴 항목 8개의 `m_Interactable` 값, Image 컴포넌트 38개의 `m_Sprite`
non-null, `KeyHint` GameObject 0개를 재확인. code-reviewer 스킬로 Phase 2/3 각각 리뷰 -
Phase 2에서 `AssignField` 리플렉션 헬퍼 중복 + null-target 가드 누락을 지적받아 즉시
`VillageHubUiBuilder.AssignField`로 단일화하고 null 가드 추가.

## 2026-09-15: 기준해상도 세로 720x1280은 오류(SSOT 위반) → 가로 1280x720으로 정정, Pixel Perfect Camera 제거, 세로 9타일 고정 + 맵경계 클램프 (REMEDIATION_PLAN.md Phase 1)

**버그 수정 기록**: `docs/planning/01_PRODUCT.md` 9행·52행은 "탑다운 2D, **가로 화면**", "**1280x720 기준** HUD 배치"를 명시하는데, 실제 구현(`VillageHubUiBuilder.BuildCanvas`의 `CanvasScaler.referenceResolution`, `SapphireSceneBuilder.BuildCamera`의 `PixelPerfectCamera.refResolutionX/Y`, `SapphireBuildPlayer`의 `PlayerSettings.defaultScreenWidth/Height`)는 전부 세로 720x1280이었다. 아래 "2026-09-14: 화면 스케일링은 Unity 2D Pixel Perfect Camera로 확정"과 "2026-09-14: 이동 속도·정지 간격, 카메라 PPU 최종값 확정" 두 항목이 이 세로값을 확정처럼 기록하고 있었으나, 이는 폐기된 별도 실험 프로젝트(`../topdown-asset-mvp/`)의 값을 그대로 가져온 것이었고 SSOT와 정면으로 어긋난다 - **아래 두 항목은 이 항목으로 대체되어 폐기됨(삭제하지 않고 보존, 각 항목에 폐기 표시함)**.

**결정 D1(b) 채택** (`docs/REMEDIATION_PLAN.md` 2절 - 사용자 확정): `com.unity.2d.pixel-perfect`의 `PixelPerfectCamera`를 완전히 제거하고 일반 직교 카메라로 교체한다. 그 컴포넌트는 정수 줌만 허용해 창 크기에 따라 화면에 보이는 타일 수가 24/16/10개로 널뛰었고(구 세로 기준에 가로 창을 대면 정수 줌이 1배로 고정), Development 빌드에서 화면 좌상단에 "Rendering at an odd-numbered resolution", "Screen resolution is smaller than the reference resolution" 온스크린 경고까지 그렸다(이 페인터리 AI 아트는 애초에 `pixelSnapping=false`로 픽셀 스냅 이점을 안 쓰고 있었으므로 잃는 것이 없다).

**수정 내용**:
- `VillageHubUiBuilder.BuildCanvas`: `CanvasScaler.referenceResolution` (720,1280) → **(1280,720)**, `matchWidthOrHeight=0.5`는 유지.
- `SapphireSceneBuilder.BuildCamera`: `PixelPerfectCamera` 컴포넌트 추가 제거. 카메라는 `orthographic=true`, **`orthographicSize=4.5`**(=`VerticalTilesVisible(9) * 0.5 * GridWorldConversion.CellSize(1)` - 화면 세로에 정확히 9타일이 보이도록 고정, 어떤 창 크기·비율이든 동일).
- `Presentation/Camera/CameraFollowRig.cs`: 맵 경계 클램프 신규 추가. `SetGroundTilemap(Tilemap)`으로 Ground 타일맵 참조를 받아 런타임에 `cellBounds`(→`CellToWorld`로 월드 좌표 변환)를 읽고, 카메라의 가시 사각형(반높이=orthographicSize, 반너비=orthographicSize*aspect)이 맵 밖으로 못 나가게 최종 위치를 클램프한다. 화면이 맵보다 넓은/높은 축은 맵 중앙에 고정(Mathf.Clamp의 min>max 방지). 하드코딩된 맵 크기(24x18)를 쓰지 않으므로 맵이 다시 리사이즈돼도 그대로 반영된다. `VillageHubTerrainBuilder`의 `TerrainBuildResult`에 `GroundTilemap` 필드를 추가해 배선했다.
- `Packages/manifest.json`에서 `com.unity.2d.pixel-perfect` 의존성 제거(grep으로 다른 참조 없음을 확인 후 제거).
- `SapphireBuildPlayer`: 기존 `BuildWindows()`가 항상 `BuildOptions.Development`로 빌드하던 것을 `BuildOptions.None`으로 바꾸고(플레이테스트/배포 빌드는 비-개발 빌드), 개발 빌드가 필요할 때 쓸 별도 진입점 `BuildWindowsDevelopment()`를 신설했다. `PlayerSettings.defaultScreenWidth/Height`도 720x1280 → 1280x720으로 맞춤(위 세로 기준과 동일한 잔재값이었음).

**추가 수정 (같은 날 후속 - 사용자 피드백 "켜주는 게임 창이 너무 크다")**: `ProjectSettings.asset`의 `defaultIsNativeResolution`이 Unity 템플릿 기본값(`1`, 활성)으로 남아있었다. Windowed 모드에서 이 값이 켜져 있으면 저장된 레지스트리 값이 없는 최초 실행 시 `defaultScreenWidth/Height`를 무시하고 창을 데스크톱 네이티브 해상도로 띄운다 - "창이 너무 크다" 증상의 실제 메커니즘. `SapphireBuildPlayer.Build`에 `PlayerSettings.defaultIsNativeResolution = false`를 추가해 껐다(재빌드로 `ProjectSettings.asset`에 `defaultIsNativeResolution: 0`으로 반영·확인). 로컬 환경에 남아있던 이전 세션의 레지스트리 창 크기/위치 캐시(`HKCU\Software\Sapphire Studio\Sapphire RPG`의 `Screenmanager Resolution Width/Height`, `...Window Width/Height`, `...Use Native`, `...Fullscreen mode`, `...Window Position X/Y` - 해시 접미사 있는 override 키, `...Default` 접미사 키는 보존)도 삭제해 다음 실행이 새 기본값(1280x720, Windowed)으로 뜨도록 정리했다. 인자 없는 실행(`Start-Process`로 검증)으로 창 크기가 콘텐츠 기준 1280x720(윈도우 전체 크기 1296x759, 테두리 포함)임을 직접 확인했다.

**검증 (이번 라운드)**: Unity CLI(6000.5.9f1) 컴파일 확인, EditMode 테스트 33/33 통과, `SapphireSceneBuilder.BuildAll` 재실행 후 `VillageHub.unity`를 직접 파싱해 확인 - Main Camera GameObject 컴포넌트가 Transform/Camera/CameraFollowRig 3개뿐(PixelPerfectCamera 없음), `orthographic size: 4.5`, `CanvasScaler.m_ReferenceResolution: {x: 1280, y: 720}`, `CameraFollowRig.groundTilemap` 필드가 실제 Ground Tilemap을 참조(fileID 비어있지 않음). `ProjectSettings.asset` 직접 확인 - `defaultScreenWidth: 1280`/`defaultScreenHeight: 720`/`fullscreenMode: 3`(Windowed)/`defaultIsNativeResolution: 0`. **플레이어 빌드(`SapphireBuildPlayer.BuildWindows`)와 3개 해상도(1280x720/1920x1080/1936x1048) 스크린샷 검증은 사용자 결정으로 이번 라운드 범위에서 제외했다** - Phase 2·3까지 마친 뒤 마지막에 한 번만 실행 파일을 빌드해 검수한다. 이 세션 중 일부 진단용 스크린샷(`generated-images/diagnostics/phase1_*.png`, gitignore 대상)이 중간 산출물로 남아있으나 최종 검수가 아니므로 그대로 두었을 뿐 이번 완료 판정의 근거로 쓰지 않는다. 화면의 미학적 배치 판단(HUD 요소 배치 등, Phase 2 범위)도 하지 않았다 - 전부 사용자 몫이다.

## 2026-09-14 (후속): RadialSkillMenu가 실제로는 화면 중앙에 렌더된 버그 수정 (아래 "결정 3-1 - 우측 배치 검증"을 대체)

**버그 수정 기록**: 사용자가 실제 빌드를 플레이해 "원형 스킬메뉴가 우측이 아니라 화면 정중앙"이라고 보고했다. 아래 "결정 3-1"이 서술한 빌드타임 assertion(`leftmostEdge >= 360`)은 통과하고 있었는데도 실제 렌더링은 틀렸다 - assertion 자체가 캔버스 폭이 항상 참고 해상도(720)와 같다고 가정한 채로 같은 720 가정을 재검증하고 있었을 뿐, 실제 런타임 캔버스 폭을 전혀 보지 않았기 때문이다.

**근본 원인**: `BuildCanvas`의 `CanvasScaler`는 `ScaleWithScreenSize`다. 이 모드에서 캔버스의 실제 단위 폭은 `스크린폭/scaleFactor`이고, 런타임 화면 비율이 참고 해상도(720x1280, 9:16)와 다르면 720이 아니게 된다. `RadialSkillMenu` 루트는 `anchorMin/Max=(0,0)`(좌하단 앵커) + `anchoredPosition.x=590`(고정값)으로 배치되어 있었는데, 이는 "캔버스 폭이 정확히 720일 때만" 우측(82% 지점)에 온다. 로컬 환경에서 실측한 원인: Windows 빌드가 저장하는 `Screenmanager Resolution Width/Height` 레지스트리 값(`HKCU\Software\Sapphire Studio\Sapphire RPG`)이 이전 실행에서 1201x700(가로형)으로 남아있었고, 이 경우 CanvasScaler가 계산하는 실제 캔버스 폭은 약 1257 유닛 - x=590은 그 폭의 약 47%로 정중앙 근처에 온다. 좌하단 앵커+고정 오프셋 방식 자체가 참고 해상도와 다른 화면 비율(리사이즈된 창, 다른 기기 화면비 등) 어디서나 재현 가능한 구조적 결함이었다.

**수정**: `VillageHubUiBuilder.BuildRadialSkillMenu`의 루트 `RectTransform`을 캔버스의 **우하단 코너**(`anchorMin/Max=(1,0)`)로 앵커를 바꾸고, `anchoredPosition`을 그 우측 모서리 기준 오프셋(`x=-130`)으로 재정의했다. 코너 앵커는 실제 캔버스 폭이 얼마든 항상 그 모서리를 기준으로 위치가 정의되므로, 참고 해상도와 다른 화면비에서도 구조적으로 우측에 고정된다(좌측 `VirtualMovementPad`는 원래부터 좌하단 코너 앵커라 이 버그가 없었다 - 우측 메뉴만 코너 앵커를 안 쓴 것이 이번 결함의 직접 원인). 빌드타임 assertion도 "우측 모서리로부터의 거리"로 재정의해 실제 앵커링 방식과 일치시켰다(`docs/DECISIONS.md` 결정 3-1의 leftmostEdge 계산은 더 이상 유효하지 않음 - 아래 "결정 3-1"은 당시 기록으로 보존하되 이 항목이 대체한다).

**검증**: Unity CLI로 컴파일 + EditMode 테스트 30/30 재통과, `SapphireSceneBuilder.BuildAll` 재실행 후 씬 파일을 직접 파싱해 `RadialSkillMenu` RectTransform의 `m_AnchorMin/Max={x:1,y:0}`, `m_AnchoredPosition={x:-130,y:150}`이 실제로 반영됐음을 확인, 6개 스킬 아이콘 스프라이트 참조(`m_Sprite`)가 전부 null이 아님을 재확인, `SapphireBuildPlayer.BuildWindows` 재빌드 후 새 실행 파일을 재실행해 60초 이상 크래시 없이 생존 확인(런타임 로그에 예외 없음). 화면상 실제 우측 배치 여부(창 크기·비율에 따른 최종 픽셀 위치)는 여전히 사용자가 직접 확인해야 한다 - 이번 수정은 "런타임 화면비와 무관하게 구조적으로 우측 코너 기준"으로 고쳤다는 것이지, 특정 창 크기에서의 픽셀 좌표를 육안 검증한 것은 아니다.

## 2026-09-14: 연속이동 속도 원복 + 질주 스킬 + UI 개편(좌측 가상패드/우측 원형 스킬메뉴)

**결정 1 - 연속이동 속도 원복**: `GridMoveAnimator`가 `isContinuousHold`(방향키 꾹 누름)일 때 더 짧은 `continuousMoveDuration=0.22s`를 쓰던 로직을 제거했다. 탭 이동과 연속 이동 모두 다시 공통 `moveDuration=0.32s`를 쓴다. **근거**: 사용자가 이 구분을 되돌려달라고 명시적으로 요청했다(연속 이동이 유지되어야 할 이유가 사라짐 - 아래 질주 스킬이 대신 그 역할을 맡음). 0.22s 값 자체는 폐기하지 않고 필드명을 `boostedMoveDuration`으로 바꿔 질주 스킬 전용 부스트 속도로 재활용했다 - 같은 상수를 용도만 바꿔 재사용하는 편이 "0.22s가 왜 좋은 속도였는지"에 대한 기존 튜닝 근거를 그대로 이어받을 수 있어 새 값을 다시 튜닝하는 것보다 낫다고 판단했다.

**결정 2 - "질주"(Haste) 공용 스킬 추가**: 클릭 또는 5번 키로 발동하는 5번째 공용 스킬을 추가했다. 발동 시 10초간 이동 애니메이션 속도가 `boostedMoveDuration=0.22s`로 상승하고(`GridMoveAnimator.IsSpeedBoosted`/`ActivateSpeedBoost(float)`), 시간이 지나면 자동으로 `moveDuration=0.32s`로 복귀한다. 이 부스트는 `isContinuousHold` 여부와 무관하게 적용된다 - "이동속도 자체"를 10초간 바꾸는 효과이지 연속 이동 전용 효과가 아니기 때문이다. 구현은 코루틴 기반 단순 타이머(`SpeedBoostRoutine`)로, 별도 상태머신이나 이벤트버스 없이 최소 슬라이스로 유지했다. 재시전 시 타이머가 처음부터 다시 시작된다(스택 없음, 갱신만).

**결정 3 - UI 개편: 하단 가로 스킬바 → 좌측 가상 이동패드 + 우측 원형 스킬메뉴**: 사용자가 "게임콘솔로 움직이고" 우측에 원형 배치 스킬 버튼을 요구했다. `docs/planning/01_PRODUCT.md`의 "입력은 WASD/방향키 또는 좌측 가상 스틱... 우하단 공격 중심 스킬 4개" 서술과 방향은 일치하지만, 그 문서는 "원형/부채꼴 배치"까지는 명시하지 않는다 - 이번 결정으로 그 배치 형태를 구체화했다. 기존 `SkillBarController`(하단 가로 4버튼)를 `RadialSkillMenu`로 rename하고, 신규 `VirtualMovementPad`(좌하단, 드래그 기반 4방향 가상 스틱)와 `RadialSkillMenu`가 관리하는 원형 버튼 6개(기본공격 1 + 스킬 5)로 교체했다. 기본공격은 이번에 처음 추가된 액션이지만 데미지 계산 등 전투 로직은 만들지 않고(범위 밖) 기존 스킬과 동일 수준의 캐스트 피드백만 붙였다. 모든 버튼은 원(circle) 모양이어야 한다는 요구사항에 따라 Unity 빌트인 `UI/Skin/Knob.psd` 스프라이트를 재사용했다(신규 아트 없이 원형 프레임 확보).

**결정 3-1 - 우측 배치 검증**: "원형 스킬메뉴는 반드시 화면 우측"이라는 요구를 좌표 계산으로만 만족시키지 않고, `VillageHubUiBuilder.BuildRadialSkillMenu`에 방어적 체크를 넣었다 - 메뉴가 가장 왼쪽으로 벌어지는 지점이 캔버스 절반(720 기준 x=360) 밑으로 내려가면 씬 빌드 자체가 예외로 실패한다. 실제 배치는 루트 앵커 x=590, 스킬 반지름 130, 버튼 반지름 48(크기 96/2) → 최소 x=590-130-48=412 > 360으로 여유 있게 우측 절반 안에 있다.

**결정 4 - 기본공격/질주 아이콘 신규 생성**: 기존 `Art/UI/SkillIcons.png`(비전탄/서리파동/점멸/보호막 4개)에는 기본공격·질주 아이콘이 없다. 재사용 후보를 먼저 확인했다 - `MageSkills.png`(painterly 픽셀아트, 알파 채널 없음 - RGB로 구워진 배경이라 그대로 못 씀, 팔레트 참고용으로만 검토), `Art/UI/InventoryShopIcons.png`·`Art/MainMenuIcons.png`(플랫/보석형 다른 스타일, `SkillIcons.png`의 "저폴리 각진 크리스탈 + 진한 블루+골드 베벨" 스타일과 확연히 달라 섞으면 시각적으로 어긋남) - 전부 부적합 판정. gpt-image 스킬(ChatGPT 구독 브릿지)로 `SkillIcons.png`를 스타일 레퍼런스로 첨부해 신규 생성했다. **알파 채널 실측**(PIL로 코너 픽셀 alpha=0, 아이콘 내부 alpha>128 분포 확인 - `docs/ASSET_STATUS.md`가 반복 지적하는 "Read 도구는 알파를 무시하고 흰 배경으로 보여준다" 함정을 피하기 위함)으로 진짜 투명 배경임을 확인한 뒤에만 채택했다. 최종 파일은 `Art/UI/SkillIconsExtra.png`(1287x611, 1행 2열 - 기존 `SkillIcons.png`의 셀 크기(643/644 x 611)와 동일하게 맞춰 시각적 크기가 어긋나지 않게 함).

**영향**: `GridMoveAnimator`, `PlayerGridController`(신규 `MoveAnimator` getter), `SkillCatalog`(5번째 항목 + `HasteSkillId` 상수), `RadialSkillMenu`(rename + `CastBasicAttack`), `VirtualMovementPad`(신규), `PlayerInputReader`(가상패드 우선 폴백), `VillageHubUiBuilder`(레이아웃 전면 교체 + `LoadSkillIcon` 2-시트 폴백 + 우측 배치 방어 체크), `ArtImportConfigurator`(`ConfigureSkillIconsExtra` 추가), `SapphireSceneBuilder`(UI 빌더 호출 시그니처에 `playerInputReader` 추가) - 전부 코드/에셋 변경만이고 `docs/planning/*.md`는 손대지 않았다.

## 2026-09-14: 캐릭터 스프라이트 시트를 단일 통합 시트(MageTopdownGridSheet.png)로 교체

**결정**: 방향별 유휴(`MageIdleDirectional.png`, 2열x4행)와 이동(`MageWalk4x3-v2.png`, 3열x4행) 두 시트로 나눠 관리하던 방식을 버리고, 하나의 시트(`MageTopdownGridSheet.png`, 1086x1448, 3열(idle/walkA/walkB) x 4행(Down/Left/Right/Up), 셀 362x362)로 통합했다. pivot도 12셀 전부를 개별 하드코딩하지 않고, 실측 결과가 2개 축(정면계열 Down/Left/Right vs 후면 Up, idle·walkA열 vs walkB열)으로만 갈리는 것을 확인해 그룹 단위 pivot 4종(front x normal, front x walkB, back x normal, back x walkB)으로 정리했다.

**근거**: 셀별 알파 바운딩박스 실측(`Read`로 보는 게 아니라 PIL로 알파>128 픽셀의 bbox를 직접 측정) - Down/Left/Right 발위치는 셀 하단 기준 약 97~100% 지점에서 방향 간 편차가 1%p 안팎이고, Up만 약 90% 지점으로 확연히 다르다(후면 로브가 프레임 아래로 더 내려와서). 가로 중심은 idle/walkA가 방향 불문 대체로 50% 근처인 반면 walkB만 방향 불문 좌측으로 5~12%p 치우쳐 있다. 이 패턴이 12셀 전부에 걸쳐 재현되므로 셀마다 다른 pivot을 따로 구하지 않고 그룹화하는 것이 타당하다고 판단했다. 최종 채택값: front pivotY=0.01, back(Up) pivotY=0.10, normal(idle/walkA) pivotX=0.50, walkB pivotX=0.44.

**영향**: `ArtImportConfigurator.ConfigureCharacterSheets()`가 `BuildMageGridSlices` 헬퍼(그룹 pivot 로직) 하나로 단순화됐고, 이전의 `BuildDirectionalGridSlices`(호출자가 12개 pivot을 전부 넘겨야 했던 범용 헬퍼, 사용처가 이제 없음)는 제거했다. `DirectionalSpriteAnimator`는 방향당 Sprite[] 2~3개 배열 대신 idle/walkA/walkB 각 1장씩 단일 Sprite 필드로 재설계했고, 이동 애니메이션은 idle→walkA→idle→walkB 4프레임 순환으로 바뀌었다(정지 시엔 여전히 idle 열 1프레임 고정, 애니메이션 루프 없음 - 이 요구사항은 유지). `SapphireSceneBuilder.BuildPlayer()`도 새 시트/필드명에 맞춰 스프라이트 로딩 코드를 갱신했다. 구 시트 2개(`MageIdleDirectional.png`, `MageWalk4x3-v2.png`)와 각 `.meta`는 참조하는 코드가 더 없음을 grep으로 확인한 뒤 `git rm`으로 삭제했다.

## 2026-09-14: GridWorldConversion 코너 vs 중앙 버그 수정

**버그 수정 기록** (설계 결정이 아니라 실측으로 확인한 결함 수정): `GridWorldConversion.GridToWorld`가 `coord * CellSize`(셀의 좌하단 코너)를 반환하던 것을 `(coord + 0.5) * CellSize`(셀 중앙)로 고쳤다. 이전 공식대로면 플레이어·스킬 범위 마커 등 격자 기반 위치가 항상 타일 4개가 만나는 코너 위에 그려져 "타일 경계에 걸쳐 서 있는 것처럼 보인다"는 증상이 났다. `VillageHubTerrainBuilder`의 펜스/사인포스트 배치도 같은 코너 좌표를 직접 재계산하던 중복 코드였던 것을 걷어내고 `GridWorldConversion` 하나로 위임하도록 정리했다(같은 버그가 여러 곳에 중복 존재하는 것을 막기 위함).

**영향**: `GridWorldConversionTests`도 코너가 아니라 중앙 좌표를 기대하도록 케이스를 갱신했다. Unity `Tilemap.CellToWorld`는 코너를 반환하므로 `GridToWorld`가 그 값을 그대로 재사용하지 않는다는 점을 클래스 doc comment에 남겼다.

## 2026-09-14: 이동 속도·정지 간격, 카메라 PPU 최종값 확정 (카메라 부분 폐기됨 - 위 2026-09-15 항목 참조)

> **폐기됨 (2026-09-15)**: 이 항목의 카메라 관련 서술(`assetsPPU=72`, 참고 해상도 720x1280 세로 기준)은 SSOT(`docs/planning/01_PRODUCT.md`, 가로 1280x720)와 반대 방향이었던 오류로, 위 "2026-09-15: 기준해상도 세로 720x1280은 오류..." 항목으로 대체됐다. 이동 속도·정지 간격(`moveDuration`/`stepPause`) 부분은 카메라와 무관하므로 계속 유효하다. 아래 원문은 당시 기록 그대로 보존한다.

**결정**: 격자 스냅 이동의 tween 지속시간을 초안값(0.08s → 0.16s)에서 재상향해 `moveDuration=0.4s`로 확정했다(한 칸 이동이 자유이동처럼 보이지 않고 눈에 확실히 보이도록). 한 칸 이동 완료 후 `stepPause=0.04s`를 추가로 둬서 "이동 → 살짝 멈춤 → 이동"의 칸 단위 리듬을 만들었다. 카메라는 `PixelPerfectCamera.assetsPPU`를 20 → 100 → 72 순으로 재조정해 최종 72로 확정했다(참고 해상도 720x1280에서 가로 10칸이 보이는 밀도 - 바람의나라/포켓몬 골드 스타일 목표치에 부합).

**근거**: 사용자 피드백을 거쳐 반복 조정했다 - 0.08s/0.16s는 여전히 자유이동처럼 보인다는 피드백, assetsPPU=20은 타일/캐릭터가 너무 작다는 피드백, assetsPPU=100은 한 칸 이동이 화면을 과하게 잠식해 보인다는 피드백. 스프라이트 임포트 PPU(구 320/302, 이제 통합 시트의 302 하나)는 캐릭터의 월드 공간 크기만 결정하고 화면 스케일과는 독립적이라는 점을 `SapphireSceneBuilder.BuildCamera()` 주석에 남겼다.

**현재 코드 상태**: `GridMoveAnimator.moveDuration=0.4f`, `stepPause=0.04f`(둘 다 인스펙터 노출 필드, 폴리싱 시 조정 가능). 카메라는 아래 2026-09-14 "화면 스케일링" 결정대로 `com.unity.2d.pixel-perfect`의 `PixelPerfectCamera`를 실제로 쓰고 있고(자체 제작 코드가 아님 - 아래 항목의 "현재 코드 상태" 서술은 이 값으로 갱신됨), `assetsPPU=72`, `refResolutionX/Y=720/1280`, `pixelSnapping=false`(페인터리 AI 아트라 스냅 지터 방지 목적).

## 2026-09-14: GroundTiles 아틀라스 재생성 (내부 균일성 버그)

**버그 수정 기록**: 이전 "hq" 1024x1024 셀 아틀라스가 셀 내부가 4개의 서로 다른 512x512 서브이미지로 구성돼 있던 것(솔기만의 문제가 아니라 실제 내용 차이 - 사분면 간 평균 절대 RGB 차이 실측 약 39-49)을 발견해, 1536x1024 3x2 그리드(잔디 3종 + 흙 3종, 셀당 진짜 균일한 512x512 텍스처)로 아틀라스 전체를 재생성했다. 재생성 후 사분면 간 차이는 약 17-25로 정상 범위(기존 정상 참고 에셋 ~25와 동급)로 떨어졌고, 타일링 시 이음매도 랩어라운드 비율 약 1.0-1.1x(재생성 전 1.8-2.4x)로 해소를 확인했다.

**영향**: 셀 전체를 그대로 슬라이스하는 방식(ppu=512, 셀 높이/너비와 동일해 1타일=1월드유닛)으로 되돌렸다 - 이전에 512x512 서브영역만 잘라 쓰던 임시방편은 더 이상 필요 없다.

## 2026-09-14: 이동 방식을 격자(타일) 스냅 이동으로 확정

**결정**: 캐릭터 이동은 자유로운 아날로그 XY 이동이 아니라, 방향 입력 1회(또는 꾹 눌렀을 때 반복 틱 1회)마다 정확히 타일 1칸만 이동하는 격자 스냅 방식으로 한다. 짧은 tween(약 0.05~0.1초, 정확한 값은 폴리싱 단계에서 재조정)으로 부드럽게 스냅하고, 이동 중에는 새 입력을 무시해 여러 칸이 겹쳐 밀리지 않게 한다. 목표 조작감은 바람의나라/제노니아/포켓몬 골드 스타일의 클래식 탑다운.

**근거**: 별도 실험 프로젝트 `../topdown-asset-mvp/`(`client/Assets/Scripts/PlayerMovement.cs`)에서 실제로 구현·테스트해 확정. 그 구현은 half-tile 단위 스텝(`stepDuration=0.082s`, `repeatDelay=0.034s`)으로 이동 중 애니메이션이 더 매끄럽게 보이도록 했다 - 실제 타일 그리드 크기 자체는 그대로 두고 스텝 거리만 절반으로 나눈 방식이다. 이 세부 파라미터는 참고값이고 본 프로젝트에 그대로 이식할지는 마이그레이션 시점에 다시 정한다.

**현재 코드 상태 (2026-09-14 갱신, 구현 완료)**: 이 단락이 서술하던 `Domain/Combat/CombatWorld`·`Domain/World/TileWorld`(연속 XY 자유 이동 기반)는 이후 `docs/HANDOFF.md`의 "이전 구현 전체 제거" 정리로 코드베이스에서 완전히 삭제되고, `Domain/Grid`(`GridCoord`/`GridDirection`/`GridMover`/`GridWorldConversion`) 네임스페이스로 처음부터 다시 구현됐다. 방향 입력 1회 = 정확히 1칸 이동, `GridMoveAnimator`가 `moveDuration=0.4s` tween + `stepPause=0.04s`로 스냅을 부드럽게 만든다(값 확정 근거는 아래 "이동 속도·정지 간격, 카메라 PPU 최종값 확정" 항목). 이동 중 새 입력은 무시된다(`GridMover`가 `IsMoving` 동안 `TryBeginMove` 거절). VillageHub 씬에서 실제로 동작하며 `GridMoverTests` 등 EditMode 테스트로 회귀 검증된다.

**초래하는 영향**: `docs/planning/02_SYSTEM_CONTRACTS.md`가 명시한 "월드는 XY 평면, 중력/점프 없음... 8방향 이동"이라는 표현은 여전히 유효하다(8방향이라는 것과 중력 없음은 격자 이동에서도 그대로 성립). 다만 그 문서가 전제하는 연속 이동/충돌 해소 방식(반경 0.22 원, 축별 충돌 해소 등)은 격자 스냅 이동에서는 그대로 쓰기 어렵다. 이 계약을 실제로 어떻게 재정의할지는 마이그레이션 착수 시점에 별도 결정으로 남긴다 - 기획 문서 자체는 고치지 않는다.

## 2026-09-14: 화면 스케일링은 Unity 2D Pixel Perfect Camera로 확정 (폐기됨 - 위 2026-09-15 항목 참조)

> **폐기됨 (2026-09-15)**: `PixelPerfectCamera`는 REMEDIATION_PLAN.md D1(b) 결정으로 완전히 제거됐다(정수 줌만 허용해 창 크기별로 보이는 타일 수가 널뛰는 문제 + Development 빌드 경고 오버레이 문제, 위 2026-09-15 항목 참조). 참고 해상도 720x1280(세로) 역시 SSOT(가로 1280x720)와 반대였던 오류. 아래 원문은 당시 기록 그대로 보존한다.

**결정**: 자체 제작 카메라 스냅 코드 대신 Unity 공식 `com.unity.2d.pixel-perfect` 패키지를 쓴다. 타일 1칸이 화면상 약 20픽셀 정도로 보이게 하고, 모바일 세로 화면과 PC 양쪽을 겸용한다.

**근거**: `../topdown-asset-mvp/client/Assets/Scenes/MainScene.unity`와 `Assets/Editor/BuildMvp.cs`에서 `assetsPPU=20`, `refResolutionX=720`, `refResolutionY=1280`으로 실제 구성해 확인. 참고 해상도이며 최종 값은 폴리싱 단계에서 조정한다.

**현재 코드 상태 (2026-09-14 갱신, 마이그레이션 완료)**: 자체 제작 `PixelCameraFollow`는 격자 이동 구현 과정에서 완전히 제거됐다. `SapphireSceneBuilder.BuildCamera()`가 공식 `com.unity.2d.pixel-perfect` 패키지의 `PixelPerfectCamera`를 직접 붙이고, `assetsPPU=72`(참고치였던 20에서 최종 확정), `refResolutionX/Y=720/1280`, `upscaleRT=false`, `pixelSnapping=false`, `cropFrameX/Y=false`, `stretchFill=false`로 설정한다. 최종 PPU 값과 근거는 바로 아래 2026-09-14 "이동 속도·정지 간격, 카메라 PPU 최종값 확정" 항목 참고.

## 2026-09-14: 에셋 전략을 하이브리드(무료 팩 + AI 생성)로 확정

**결정**: 지형/배경/환경 오브젝트(바닥 타일, 나무, 울타리, 건물 등)는 무료 + 상업적 이용 가능한(CC0 또는 CC-BY, 라이선스 원문 확인 필수) 커뮤니티 에셋팩을 우선 쓴다. 다만 주인공 캐릭터·주요 몬스터·시각적으로 두드러지는 오브젝트처럼 게임 정체성을 좌우하는 요소는 AI 이미지 생성(gpt-image 스킬/game-asset-artist 에이전트)으로 직접 만들어 섞는다. 무료 팩 요소의 퀄리티가 부족하다고 판단되면 그 요소만 AI로 재생성해 교체한다(전체 교체가 아니라 부분 교체).

**근거**: `../topdown-asset-mvp/`에서 LimeZu Serene Village(CC BY 4.0, 바닥/나무/울타리/집)와 Kenney Tiny Town(CC0, 덤불/울타리)을 조합해 확인. 실제로 무료 팩의 바닥 타일 퀄리티가 부족하다는 판단이 나와서 `Assets/Art/Ground/ai_grass.png`·`ai_dirt_path.png`·`ai_stone.png` 3종을 AI로 재생성해 교체한 사례가 있다 - "퀄리티 부족하면 그 요소만 AI로 대체"하는 원칙의 실제 근거다. CC-BY 팩은 `CREDITS.md`에 출처·라이선스를 기록해 크레딧 표기 의무를 지켰다.

**현재 코드 상태**: 이 프로젝트에는 아직 `CREDITS.md`가 없고, 하이브리드 전략이 적용되지 않았다. 현재 `client/Assets/Sapphire/Art/`의 모든 파일은 이전(2026-09-13) 세션에서 AI 생성 또는 원본 참고로 만든 것들이다. 자세한 현황은 `docs/ASSET_STATUS.md`.

## 2026-09-14: 개발 프로세스를 Codex 병렬 에이전트에서 Claude Code `lh2d-*` 에이전트로 전환

**결정**: `AGENTS.md`가 예전에 설명하던 "combat/campaign/presentation 에이전트가 각자 모듈을 동시에 `apply_patch`로 수정" 방식은 더 이상 쓰지 않는다. 대신 Claude Code 세션에서 `lh2d-module-planner`(설계) → `lh2d-combat-implementer`/`lh2d-asset-specialist`(구현) → `lh2d-qa-verifier`(독립 재검증) 순서로 모듈 1개씩 처리한다.

**근거**: 사용자 지시. 이전 병렬 방식은 이미 module ownership 충돌 방지 규칙이 필요했을 만큼 조율 비용이 컸고(`docs/PARALLEL_CONTRACT.md`, 이제 삭제됨), 지금은 Claude Code 세션이 기본 작업 환경이다.

**부수 확인 (범위 밖, 손대지 않음)**: `lh2d-*` 4개 에이전트가 `project/.claude/agents/`에 심볼릭 링크로 연결돼 있어야 한다고 알려져 있었으나, 이 문서 작성 시점에 실제로는 이 레포에 `.claude/agents/`도 없고 링크도 없는 상태를 확인했다. 연결 작업은 이 문서 재작성의 범위 밖이라 그대로 두었다.

## 2026-09-14: 검증 관행 - 컴파일/도메인 체크까지만 자동 검증

**결정**: 화면 캡처, 실제 플레이 판단, 조작감 평가는 Claude가 임의로 하지 않는다. 자동으로는 컴파일 성공과 도메인 규칙 테스트(정상/오류/취소 사례)까지만 확인하고, 실제 플레이 확인은 사용자가 직접 한다.

**근거**: 이 세션 동안 여러 차례 확립된 관행. 화면으로 보이는 판단(밸런스·조작감 제외)은 사용자가 직접 하는 편이 정확하고, Claude가 시각적으로 추정해 반복 시도하는 것은 비효율적이었다.

---

## 배경: 2026-09-13 구현 세션에서 확정되어 아직 유효한 결정 (참고)

아래는 격자 이동 방향이 확정되기 이전, 첫 구현 세션에서 정한 것 중 지금도 유효한 부분을 간단히 정리한 것이다(원문은 git 이력에 남아있던 이전 버전 `DECISIONS.md`에 있었고, 이 재작성으로 대체된다).

- 원본(횡스크롤) 이동 계약은 새 제품에 적용하지 않는다는 방향은 유지된다. 다만 "새 제품의 이동 방식"은 위 2026-09-14 결정으로 자유 XY에서 격자 스냅으로 다시 바뀌었다.
- 전투 좌표계는 `CombatWorld [0,20]×[0,11]` ↔ `Presentation [-10,10]×[-5.5,5.5]`, `GameApp`만 `(x-10, y-5.5)` 변환을 수행한다. 이 변환 경계는 격자 이동 마이그레이션에서도 유지할지 여부를 다시 검토해야 한다.
- 저장 파일은 체크섬 envelope, revision, 최근 transaction ID를 갖고, 파일 교체 실패 시 `.bak` 복사 후 임시 파일 이동으로 복구 가능한 경로를 쓴다.
- 생성 캐릭터 시트(`MageDirectional.png`)는 체크무늬가 알파가 아니라 RGB로 구워져 런타임에서 제외되고 기존 `MageIdle`을 대신 쓴다 - AI 생성 에셋 검수 시 알파 채널을 실측해야 한다는 근거 사례로 `docs/ASSET_STATUS.md`에도 남긴다.
