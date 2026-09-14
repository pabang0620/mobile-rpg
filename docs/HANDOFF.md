# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향). 상세 근거는 `docs/DECISIONS.md` 참고, 여기는 "지금 코드가 실제로 어떤 상태인가"만 요약한다.

## 2026-09-14 (최신): 이동속도 원복 + 질주 스킬 + UI 개편(좌측 가상패드/우측 원형 스킬메뉴) - 현재 상태

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
