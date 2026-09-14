# 결정 기록

기술 방향 결정을 날짜순으로 남긴다(최신이 위). 기획 자체(무엇을 만들지)는 `docs/planning/*.md`가 SSOT이고 여기서는 다루지 않는다 - 여기는 "어떻게 구현할지"에 대한 결정만 남긴다.

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

## 2026-09-14: 이동 속도·정지 간격, 카메라 PPU 최종값 확정

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

## 2026-09-14: 화면 스케일링은 Unity 2D Pixel Perfect Camera로 확정

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
