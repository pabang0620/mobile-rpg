# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향). 상세 근거는 `docs/DECISIONS.md` 참고, 여기는 "지금 코드가 실제로 어떤 상태인가"만 요약한다.

## 2026-09-14 (최신): UI 에셋 전면 교체 + HP 바 신규 추가 - 현재 상태

사용자 지시("지금 있는 UI는 다 버려야해")로 신규 생성된 UI 에셋 4종을 전부 배선하고 기존 UI를 완전히 교체했다.

1. **원형 스킬 버튼**: `VillageHubUiBuilder.BuildRadialSkillMenu`가 쓰던 Unity 빌트인 `UI/Skin/Knob.psd`를 `SkillButtonFrame.png`(1536x1024, 2셀)로 교체했다. 좌측 셀(`SkillButtonFrame_Skill`)은 5개 스킬 버튼 전부에, 우측 셀(`SkillButtonFrame_BasicAttack`, 더 큰 원)은 중앙 기본공격 버튼에 쓴다. 두 원의 알파 내용을 컬럼 카운트 프로파일로 실측한 결과 정확히 절반(768/768)으로 나누면 우측 원의 내용(컬럼 738-1488)을 침범하므로, 둘 사이 공백 구간(632-737)의 중점(684)을 셀 경계로 잡았다 - `ArtImportConfigurator.ConfigureSkillButtonFrame` 참고.
2. **스킬 아이콘**: `SkillIconsSet.png`(1536x1024, 3x2=6칸)를 알파 컬럼/로우 카운트 프로파일로 실측해 정확한 슬라이스 좌표를 구했다(균등 512x512 그리드가 아니라 컬럼 519/1000, 로우 496 경계 - 갭 구간의 중점). 순서는 기본공격/비전탄/서리파동(위 행), 점멸/보호막/질주(아래 행)로, 각 아이콘 그림(지팡이+섬광/날아가는 파편/눈꽃/속도 화살표/방패/날개+번개)을 직접 눈으로 봐서 확인했다. `VillageHubUiBuilder.LoadSkillIcon`을 `SkillIconsSet.png` 단일 참조로 단순화했고(기존 `SkillIcons.png`+`SkillIconsExtra.png` 2-시트 폴백 로직 제거), 스프라이트 이름(`SkillIcons_ArcaneBolt` 등)은 그대로 유지해 `SkillCatalog.cs`는 코드 변경이 필요 없었다. 기존 두 시트 파일은 `git rm`으로 삭제했다.
3. **메시지 패널**: `FantasyPanelBorder.png`(48x48)를 `MessagePanelFrame.png`(1649x954, 9-slice)로 교체했다. 9-slice border는 골드 프레임과 남색 내부 채움 사이의 알파/색상 전환 지점을 이미지 가장자리(x=0/y=0 기준)에서 여러 지점(코너·중앙부 별 장식을 피해서) 실측해 구했다: 좌우 116px, 상단 181px, 하단 244-252px(하단이 상단보다 실제로 더 두껍다 - 7개 x축 지점에서 상단은 전부 정확히 181로 일관됐고 하단은 244-252 범위의 내부 텍스처 노이즈만 있어 측정 오차가 아니라 실제 비대칭으로 판단) - `ArtImportConfigurator.ConfigureUiFrames`의 `MessagePanelFrame` 항목 참고. 기존 48x48 border=10 값은 재사용하지 않았다(이미지 크기·프레임 두께가 전혀 다름).
4. **HP바 신규 추가**: 화면 좌측 상단에 `HealthBarFrame.png`(1774x887, 2셀 세로)를 이용한 새 HP 바를 만들었다. 위 셀(테두리+트랙)이 배경, 아래 셀(진홍색 필)이 `Image.Type.Filled`(Horizontal, Left origin) 채움 게이지다. 두 셀도 균등 반분이 아니라 알파 로우 카운트 프로파일로 실측한 갭(452-508)의 중점(480)을 경계로 썼다. 신규 파일 `Presentation/UI/HealthBarView.cs`(관심사 분리, God 클래스 아님)가 `fillImage.fillAmount`만 노출하며, 이번 슬라이스엔 전투/데미지 시스템이 없으므로 `Awake()`에서 100% 고정으로 표시한다 - 향후 전투 시스템이 생기면 `SetFillAmount(currentHp/maxHp)`를 호출하도록 설계했다. `VillageHubUiBuilder.BuildHealthBar`가 배선한다.
5. **기존 파일 정리**: 새 배선 이후에도 어디에서도 참조되지 않는 것을 grep으로 확인 후 `git rm`으로 삭제했다 - `UiChrome.png`, `MainMenuIcons.png`, `MenuPanel.png`, `HudControls.png`(이번 작업 전부터 이미 코드에서 미참조 상태였음), `FantasyPanelBorder.png`(MessagePanelFrame.png로 대체되며 참조 소멸), `SkillIcons.png`/`SkillIconsExtra.png`(SkillIconsSet.png로 통합되며 참조 소멸). `WideButton.png`(닫기 버튼)과 `InventoryShopIcons.png`은 계속 참조되므로 유지했다.

Unity CLI로 컴파일 확인(경고 1건만 - `TextureImporter.spritesheet` obsolete API, 이번 변경과 무관한 기존 경고), EditMode 테스트 30/30 재통과, `SapphireSceneBuilder.BuildAll` 재실행 후 씬 파일(`VillageHub.unity`)을 YAML 파싱해 확인 - `HealthBar`/`Fill`/`MessagePanel`/`AttackButton`/`SkillButton_0~4`/6개 `Icon`/`CloseButton` GameObject의 Image 컴포넌트 `m_Sprite` 필드가 전부 null(`fileID: 0`)이 아니고 각각 올바른 소스 텍스처 guid(SkillButtonFrame/SkillIconsSet/HealthBarFrame/MessagePanelFrame/WideButton)를 가리키는지, `HealthBarView`의 `fillImage` 필드가 실제 Fill Image 컴포넌트를 참조하는지, Fill Image의 `m_Type=3`(Filled)/`m_FillMethod=0`(Horizontal)/`m_FillAmount=1`/`m_FillOrigin=0`(Left)인지까지 전부 직접 파싱해 확인했다(육안 판단 없음). `SapphireBuildPlayer.BuildWindows`로 재빌드(빌드 산출물의 `Assembly-CSharp.dll` 타임스탬프가 이번 빌드 시각과 일치함을 확인) 후 기존 실행 중이던 프로세스가 없음을 확인하고 새 빌드를 실행해 35초 이상 프로세스가 살아있음을 `tasklist`로 확인했다. 화면 렌더링·미학 판단은 하지 않았다(AGENTS.md 검증 관행 및 사용자 지시에 따름) - 사용자 몫이다.

## 2026-09-14: RadialSkillMenu 우측 배치 버그 수정 - 이전 상태

사용자가 실제 빌드를 플레이해 원형 스킬메뉴가 우측이 아니라 화면 정중앙에 떠 있다고 보고했다(스킬 아이콘이 안 보인다던 최초 지적과 같은 세션에서 재확인됨). 근본 원인과 수정 내용은 `docs/DECISIONS.md`의 같은 날짜 "RadialSkillMenu가 실제로는 화면 중앙에 렌더된 버그 수정" 항목 참고 - 요약하면 `VillageHubUiBuilder.BuildRadialSkillMenu`의 루트 앵커를 좌하단(0,0)에서 우하단(1,0)으로 바꾸고 `anchoredPosition`을 우측 모서리 기준 오프셋(`x=-130`)으로 재정의했다. 이전 빌드타임 assertion(`leftmostEdge >= 360`)은 캔버스 폭이 항상 720이라는 잘못된 가정을 재검증하고 있어서 실제 레이아웃 오류를 잡아내지 못했다 - 이번에 그 assertion도 우측 모서리 기준 거리로 다시 정의했다.

스킬 아이콘 스프라이트 배선 자체(`SkillIcons.png`/`SkillIconsExtra.png` 6개 스프라이트, `LoadSkillIcon` 2-시트 폴백, `ArtImportConfigurator`의 슬라이스 좌표)는 이번 재조사에서 전부 정상으로 재확인했다 - 이미지 알파 채널 실측(코너 픽셀 alpha=0), 임포터 메타 파일의 스프라이트 시트 좌표, 씬 파일에 직접 파싱해 확인한 6개 `m_Sprite` 참조(전부 null 아님) 전부 일치했다. 즉 아이콘이 안 보인 것은 스프라이트 배선 결함이 아니라 위 앵커 버그로 메뉴 전체가 엉뚱한 위치(및 엉뚱한 화면 비율 스케일)에 렌더된 결과로 보인다.

Unity CLI로 컴파일 + EditMode 테스트 30/30 재통과, 씬 재빌드 후 새 앵커값(`m_AnchorMin/Max={x:1,y:0}`, `m_AnchoredPosition={x:-130,y:150}`)이 실제로 반영됐음을 씬 파일 직접 파싱으로 확인, Windows 플레이어 재빌드 후 새 실행 파일을 60초 이상 실행해 크래시 없음을 확인했다. 로컬 환경에 남아있던 레지스트리상의 창 크기 캐시(`Screenmanager Resolution Width/Height`, 1201x700으로 오염되어 있었음 - 이 값이 곧 실측된 버그 재현 조건이었다)도 초기화해 다음 실행이 참고 해상도(720x1280) 기준 기본값으로 뜨도록 정리했다. 화면 픽셀 단위의 최종 육안 확인은 여전히 사용자 몫이다.

## 2026-09-14: 이동속도 원복 + 질주 스킬 + UI 개편(좌측 가상패드/우측 원형 스킬메뉴) - 이전 상태

아래 "격자 이동 구현 + 캐릭터 시트 통합 완료" 절 이후, 같은 날 후속 세션에서 다음 3가지를 추가로 반영했다(세부 근거는 `docs/DECISIONS.md`):

1. **연속이동(꾹 누름) 속도 원복**: `GridMoveAnimator`가 `isContinuousHold`일 때 더 짧은 `continuousMoveDuration`(0.22s)을 쓰던 로직을 제거했다. 탭 이동과 연속 이동 모두 다시 기본 `moveDuration=0.32s`를 쓴다(구분 없음). 그 0.22s 값 자체는 버리지 않고 아래 질주 스킬의 부스트 속도(`boostedMoveDuration`)로 재활용했다.
2. **"질주"(Haste) 공용 스킬 추가**: `SkillCatalog.All`에 5번째 항목(`skill.haste`, 표시명 "질주")을 추가했다. 클릭(또는 5번 키)하면 `GridMoveAnimator.ActivateSpeedBoost(10f)`가 호출되어 10초간 이동 속도가 `boostedMoveDuration=0.22s`로 상승하고, 10초 후 자동으로 `moveDuration=0.32s`로 복귀한다(탭/연속 이동 둘 다 적용).
3. **UI 개편: 하단 가로 스킬바 제거 → 좌측 가상 이동패드 + 우측 원형 스킬메뉴**: 기존 `SkillBarController`(하단 가로 4버튼 바)를 `RadialSkillMenu`로 rename·재구성했다(`Presentation/Skills/`). 화면 좌하단에는 신규 `VirtualMovementPad`(`Presentation/Movement/`, 터치/마우스 드래그 기반 가상 스틱)를 배치해 `PlayerInputReader`가 읽는 것과 동일한 `GridDirection` 입력을 만든다(키보드 WASD/방향키와 병행 지원 - 가상패드가 방향을 보고할 때만 우선하고, 안 쓰면 키보드로 자연스럽게 폴백). 화면 우측 절반 안에는 큰 중앙 "기본공격" 버튼(신규 - 데미지 로직 없이 캐스트 피드백만) + 기존 스킬 4개 + 질주 1개, 총 6개의 원형 버튼을 부채꼴(100°~190°)로 배치했다. 모든 버튼은 Unity 빌트인 "Knob" UI 스프라이트를 재사용한 원형이다.
4. **신규 스킬 아이콘 2개 생성(기본공격/질주)**: 기존 `Art/UI/SkillIcons.png`(비전탄/서리파동/점멸/보호막 4개뿐)와 그 외 UI 아이콘류(`MageSkills.png` - 알파 없어 재사용 불가·팔레트만 참고, `InventoryShopIcons.png`·`MainMenuIcons.png` - 스타일/팔레트가 확연히 달라 재사용 불가)를 먼저 확인했으나 재사용 가능한 게 없어 gpt-image 스킬로 `SkillIcons.png`를 스타일 레퍼런스로 넘겨 신규 생성했다. 자세한 근거는 `docs/ASSET_STATUS.md`.

**현재 실제로 동작하는 것 (이번 갱신분, Unity CLI로 컴파일·EditMode 테스트 30개·씬 재빌드·플레이어 빌드+30초 이상 실행까지 확인, 화면 판단은 사용자 몫):**

- `GridMoveAnimator`: `moveDuration=0.32f`(기본, 탭/연속 공통), `boostedMoveDuration=0.22f`(질주 중), `IsSpeedBoosted` 프로퍼티·`ActivateSpeedBoost(float durationSeconds)` 공개 메서드.
- `SkillCatalog.All`이 5개 항목(비전탄/서리파동/점멸/보호막/질주)을 갖는다. `RadialSkillMenu.CastSkill`이 `SkillCatalog.HasteSkillId`를 특별 처리해 부스트를 트리거한다.
- `VirtualMovementPad`(신규): `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler` 기반, 드래그 벡터의 우세 축(상하좌우 중 하나, 대각선 없음 - `GridDirection`이 4방향뿐이라 그대로 반영)으로 판정하며 데드존은 반지름의 25%. `PlayerInputReader.TryGetHeldDirection`이 가상패드를 키보드보다 먼저 확인한다.
- `RadialSkillMenu`(구 `SkillBarController` rename): 기본공격(J 키 또는 클릭, 캐스트 피드백만) + 스킬 1-5(키 1-5 또는 클릭) 입력 처리, 로직 자체(범위 표시·질주 트리거·캐스트 피드백 호출)는 이전 `SkillBarController`와 동일하게 유지.
- `VillageHubUiBuilder`: 하단 가로 스킬바 빌드 코드(`BuildSkillBarContainer`/`BuildSkillButtons`/`BuildSkillButton`)를 제거하고 `BuildVirtualMovementPad`/`BuildRadialSkillMenu`/`BuildRadialButton`으로 교체. 좌측 패드는 앵커 (150,150)·반지름 100(캔버스 기준 해상도 720 폭 기준 완전히 좌측 절반 안), 우측 메뉴는 앵커 (590,150)·기본공격 크기 140(중앙)·스킬 5개는 반지름 130·크기 96·100°~190° 부채꼴. `BuildRadialSkillMenu`가 메뉴의 가장 왼쪽으로 벌어지는 지점(`anchoredPosition.x - skillRadius - skillButtonSize/2`)이 화면 절반(x=360) 아래로 내려가면 즉시 예외를 던지도록 방어 코드를 넣어, 이후 반지름/각도를 조정하다 실수로 좌측 절반을 침범하는 회귀를 배치 모드 빌드 단계에서 바로 잡아낸다.
- `Art/UI/SkillIconsExtra.png`(신규, 1287x611, 1행 2열) + `ArtImportConfigurator.ConfigureSkillIconsExtra()`: `SkillIcons_BasicAttack`(좌)·`SkillIcons_Haste`(우) 명명된 스프라이트 2개. `VillageHubUiBuilder.LoadSkillIcon`이 `SkillIcons.png`를 먼저 찾고 없으면 `SkillIconsExtra.png`에서 찾는다.

**아직 안 된 것 / TBD (이번 갱신 기준)**: 기본공격은 캐스트 피드백만 있고 실제 데미지·`docs/planning/01_PRODUCT.md`가 서술하는 3연타 콤보 로직은 없다(범위 밖). 질주 외 스킬(비전탄 등)에는 여전히 쿨다운/MP 자원이 없다(이전부터 알려진 TBD, 이번 갱신으로 바뀐 것 없음). Unity Editor 라이선스 환경이라 PLAYMODE/PLAYER 자동 실행 검증(빌드+30초 이상 생존 확인)까지만 했고, 실기기 터치 조작감·화면 배치 판단은 여전히 사용자 몫이다.

## 2026-09-14: 격자 이동 구현 + 캐릭터 시트 통합 완료

아래 "이전 구현 전체 제거" 절(같은 날 더 이른 시점 기록)이 서술하는 "완전히 빈 상태"는 더 이상 유효하지 않다. 그 이후 같은 세션에서 격자 스냅 이동 기반 VillageHub 씬을 처음부터 다시 구현했고, 오늘 안에서만 다음 순서로 진행됐다(세부 근거는 각각 `docs/DECISIONS.md`):

1. 격자 이동/전투/UI 슬라이스 구현 (`Domain/Grid`, `Presentation/Movement|Skills|UI|World`, `Editor/SapphireSceneBuilder` 등 신규).
2. `GridWorldConversion`의 코너 vs 중앙 좌표 버그 발견·수정 + 이동속도(`moveDuration=0.4s`, `stepPause=0.04s`)·카메라(`assetsPPU=72`) 튜닝 + `GroundTiles.png` 내부 균일성 버그 재생성.
3. 캐릭터 스프라이트를 `MageIdleDirectional.png`+`MageWalk4x3-v2.png` 2시트 체제에서 `MageTopdownGridSheet.png` 단일 시트(3열 idle/walkA/walkB x 4행 Down/Left/Right/Up, 그룹 pivot 4종)로 교체.

**현재 실제로 동작하는 것** (Unity CLI로 컴파일·EditMode 테스트·씬 재빌드·플레이어 빌드까지 확인, 화면 판단은 사용자 몫):

- `Assets/Sapphire/Scenes/VillageHub.unity` - `Sapphire.EditorTools.SapphireSceneBuilder.BuildAll`(배치모드 전용, `-executeMethod`로 재실행 가능)이 매번 처음부터 새로 조립한다: 24x18 마을 맵(잔디/흙 바닥 타일맵 + 경계 콜리전 타일맵 + 테두리 펜스 + 사인포스트), 플레이어(격자 스냅 이동 + 방향별 스프라이트), 카메라(Pixel Perfect Camera 팔로우), 메시지 패널 + 스킬바 UI.
- 이동: 방향 입력 1회 = 정확히 1칸, `GridMover.TryBeginMove`가 이동 중 새 입력을 거절(겹쳐 밀리지 않음), `GridMoveAnimator`가 0.4s tween + 0.04s 정지로 스냅.
- 상호작용: `InteractionTrigger` + `InteractableZone`(사인포스트)이 동작, 스킬 캐스트 시 `SkillCastFeedback`이 스프라이트에 피드백을 준다.
- 캐릭터 애니메이션: 정지 중엔 idle 열 1프레임 고정(루프 없음), 이동 중엔 idle→walkA→idle→walkB 4프레임 순환.
- 테스트: `client/Assets/Sapphire/Tests/Domain/*`의 EditMode 테스트 30개 전부 통과(`GridDirectionExtensionsTests`·`GridMoverTests`·`GridWorldConversionTests`·`InteractableIdTests`·`InteractionMapTests`·`SkillRangeCalculatorTests`).
- 빌드: `Sapphire.EditorTools.SapphireBuildPlayer.BuildWindows`가 `builds/Windows/SapphireRPG.exe`(Windows Standalone, Development 빌드)를 생성한다. `Run-SapphireRPG.cmd`로 실행 가능.

**아직 안 된 것 / TBD**: 실제 플레이 조작감·비주얼 판단은 사용자가 직접 확인해야 한다(`docs/DECISIONS.md`의 검증 관행 결정 참고). 몬스터/전투 콘텐츠는 아직 없다(`docs/ASSET_STATUS.md` 참고). `AGENTS.md`가 언급하는 `lh2d-*` 에이전트 4종은 여전히 파일·심볼릭 링크가 없는 상태다(이 문서 재작성 범위 밖).

## 2026-09-14 (초반): 이전 구현 전체 제거 (당시 상태 - 이후 위 절에서 재구현됨)

`client/Assets/Sapphire/`의 아날로그 자유이동 기반 게임플레이 C# 코드 전체와, AI 생성물이 아니고 UI도 아닌 플레이스홀더/커뮤니티 에셋을 사용자 지시로 전부 제거했다. 롤백 지점은 git 태그 `pre-cleanup-2026-09-14`(origin에도 푸시됨).

**현재 코드는 완전히 빈 상태다.** 이전 세션(2026-09-13)에서 구현했던 아래 내용은 전부 삭제되었고, 격자 스냅 이동 기반(`docs/DECISIONS.md` 2026-09-14 결정)으로 처음부터 다시 구현해야 한다:

- `Domain/Combat`(연속 XY 자유 이동 전투), `Domain/Campaign`(프로필/장비/퀘스트), `Domain/World`(TileWorld)
- `Application/GameApp`(파사드), `Infrastructure/FileProfileStore`(세이브 - `Domain/Campaign`에 의존해 함께 제거)
- `Presentation/`(WorldRenderer, GamePresenter, GameInput, Exploration/* 전부)
- `Editor/BuildGame.cs`(빌드 파이프라인), `Tests/`(CampaignChecks·CombatChecks·WorldChecks)

남은 것은 `client/Assets/Sapphire/Scenes/Boot.unity`(빈 씬 1개만 유지 - Unity가 프로젝트를 열 때 씬 파일 자체가 있어야 하므로 최소한으로 남김), `Fonts/`, 정리된 `Art/`(아래 참고)뿐이다.

**주의**: `Boot.unity`는 이전 `GameApp`/`GamePresenter` 컴포넌트를 참조하던 GameObject를 그대로 담고 있을 수 있다. 해당 스크립트가 삭제되었으므로 Unity 에디터에서 열면 "Missing Script" 경고가 뜰 수 있다 - 이는 예상된 상태이며, 새 구현에서 씬을 다시 구성하면서 정리하면 된다.

### 삭제된 빌드 파이프라인(`Editor/BuildGame.cs`)의 재사용 가치 있는 로직 (참고용 메모)

새 구현에서 텍스처 임포트 설정을 다시 자동화할 때 참고할 수 있는 원본 로직(옛 `ConfigureArt()` 메서드):

```csharp
// Art/ 하위 모든 Texture2D에 대해:
// - textureType = Default, mipmapEnabled = false, wrapMode = Clamp, filterMode = Bilinear
// - textureCompression = CompressedHQ
// - alphaIsTransparency = importer.DoesSourceTextureHaveAlpha()  // 알파 채널 실측 검사
importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
```

`DoesSourceTextureHaveAlpha()`가 실제 알파 채널 유무를 판정하는 API 호출이다 - `docs/ASSET_STATUS.md`에 남긴 "AI 생성 에셋은 알파 채널을 실측해서 검수해야 한다"는 교훈(`MageDirectional.png` 사례)과 연결되는 도구다. 새 임포트 파이프라인을 다시 만들 때 이 API를 그대로 재사용하면 된다.

## 에셋 현황

`docs/ASSET_STATUS.md` 참고. 이번 정리로 커뮤니티/무료팩 플레이스홀더(고블린 스프라이트, MagicMissile 등)와 시안 단계 배경 다수를 제거했고, AI 생성 캐릭터 아트(Mage 관련)와 UI 에셋은 그대로 유지했다.

## 다음 실행

위 1~2번(격자 스냅 이동 재구현, Pixel Perfect Camera 도입)은 2026-09-14 안에 완료됐다 - 맨 위 "현재 상태" 절 참고. 남은 것:

1. 신규 구현(전투/몬스터/퀘스트 등) 착수 전 에셋 하이브리드 전략(`docs/ASSET_STATUS.md`) 적용 여부 판단 - 아직 미착수.
2. Player 빌드는 Unity CLI로 자동 확인했다(컴파일+테스트+씬 재빌드+`SapphireBuildPlayer.BuildWindows`). 실제 플레이·조작감·비주얼 판단은 여전히 사용자가 직접 수행.
3. `AGENTS.md`가 전제하는 `lh2d-*` 에이전트 4종의 파일·심볼릭 링크 생성(아직 안 됨, 다음에 그 에이전트들을 쓰려면 먼저 필요).
