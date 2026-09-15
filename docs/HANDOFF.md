# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향). 상세 근거는 `docs/DECISIONS.md` 참고, 여기는 "지금 코드가 실제로 어떤 상태인가"만 요약한다.

## 2026-09-16 (최신): REMEDIATION_PLAN.md Phase 2(HUD 재배선) + Phase 3(오딘식 메뉴) 완료

Phase 1(가로 1280x720 기준 정정, 아래 절 참고) 이후 Phase 2·3을 한 세션에서 연속 구현했다.
측정은 전부 PIL/numpy로 알파/색상 전이 지점을 직접 스캔해 구했다(균등분할 가정 금지 원칙
재적용) - 아래 각 항목의 실측값 참고.

### Phase 2 - HUD 재배선

**1. HP바 개구부**: 이전 세션(2026-09-16 새벽, 아래 "메뉴 버튼 배경 소실..." 절)에서 이미
실측·수정된 상태였다 - Track 타이트크롭 1744x358, Fill 앵커 `(0.096,0.249)-(0.901,0.757)`.
이번 세션에서 독립적으로 재실측(navy 인테리어 색상 스캔)해 대체로 일치함을 확인했고 재수정
없이 유지했다(REMEDIATION_PLAN.md가 "고쳐야 한다"고 서술한 0.06-0.94/0.17-0.83 값은 이미
과거 값이었다).

**2. MP바 신규 추가**: `Presentation/UI/HealthBarView.cs` → `GaugeView.cs`로 일반화(HP/MP
공용, `SetFillAmount(float)` 공개 메서드 유지). MP는 HP와 동일 프레임(`HealthBarFrameGold`
Track) + Unity 내장 `UI/Skin/Background.psd`(흰 스프라이트)를 사파이어 블루
`(0.20,0.45,0.95,1)`로 틴트해 채움. HP바 바로 아래 6유닛 간격.

**3. 레벨 텍스트**: "Lv.1", HP/MP바 우측(x=20+260+12=292), `AddButtonLabel` 패턴과 동일하게
`Fonts/NotoSansCJKkr-Regular.otf` 사용.

**4. 지역명 배너**: `MenuSectionHeader.png`(아래 9-slice 실측값) 상단중앙(0.5,1) 배치, 폭
360, "사파이어 광장" 텍스트.

**5. 스킬 부채꼴 재배치 (핵심 수정)**: 반지름 200, 100°~190°(90° 폭, 4개 버튼, 칸당 30°) -
인접 중심간 거리 `2*200*sin(15°)=103.53` > 96(버튼 지름) 검증 통과(코드 내 assertion
`VerifyNoOverlap`/`VerifyOnScreen`로 빌드타임에도 재검증). 5번째 버튼("Dash" 슬롯, 부채꼴
밖 220°)은 아래 "콘텐츠 불일치" 항목 참고. 루트 앵커 우하단(1,0), `anchoredPosition=(-100,200)`.
전 버튼이 1280x720 캔버스 안에 있음(최소 여유 약 23유닛, 우측 버튼 우측edge 여유 30유닛).

**콘텐츠 불일치 발견 및 해소**: REMEDIATION_PLAN.md와 이 작업 지시서가 서술하는
"비전탄/서리파동/점멸/보호막 + 질주(대시)" 5스킬 모델은 이미 커밋 `9d2a71d`("Implement
centered wizard skills and menu UI", 같은 날 더 이른 시점)에서 마력쉴드/텔레포트/낙뢰/고드름/
번개창 5개의 실제 스펠로 전면 교체돼 있었다 - `docs/HANDOFF.md`의 과거 기록이 갱신되지
않아서 생긴 문서 drift다. `GridMoveAnimator.ActivateSpeedBoost`/`IsSpeedBoosted`는 여전히
코드에 남아있지만 그 커밋 이후로 호출하는 곳이 전혀 없다(고아 코드, 이번 세션에서 손대지
않음). 스펠 5개를 전부 실제로 캐스트 가능하게 유지하되(1~5키, 클릭 전부 동일하게 동작)
레이아웃만 지시서대로 맞췄다 - `SkillCatalog.All[0..3]`이 부채꼴 4개, `All[4]`(번개창)가
부채꼴 밖 "Dash" 슬롯. 상세 근거는 `Editor/VillageHubSkillMenuBuilder.cs` 클래스 doc과
`docs/DECISIONS.md` 참고.

**6. 키 힌트 라벨 제거**: 스킬 버튼의 1~5/J 텍스트 GameObject 생성 코드 전체 삭제. 키보드
단축키(`RadialSkillMenu.Update`)는 무변경.

**7. 아이콘 중심/크기**: 프레임 "개구부"(내부 남색 영역, 셀 경계 아님)를 셀 중앙 부근 여러
행의 최장 non-border 연속구간으로 실측 - Skill 셀(659x665) 개구부 폭 median 413px(=0.627),
BasicAttack 셀(824x854) median 541px(=0.657). 아이콘 지름 = 개구부 지름의 62% →
Skill ~37.3px, BasicAttack ~57.0px(기존 inset=size*0.2 방식은 버튼 전체의 60%로 프레임
테두리를 침범하고 있었음). `SkillIconsSetGold.png`의 6개 아이콘 각각 알파 콘텐츠 중심을
재실측했으나 셀 중심과의 편차가 -7~+9.5px(셀 크기 대비 <2%)로 무시할 수준 - 추가 오프셋
없이 중앙정렬 유지.

**8. 이동 스틱**: `MovementStickGold.png`(1438x902) 베이스(알파bbox 90,97-812,805, 지름
~722) + 노브(1010,263-1391,639, 지름 ~381) - 둘 다 각자 셀 안에서 <1px 오차로 중앙정렬됨을
확인. Base 160 / Knob 64(40%). `VirtualMovementPad`의 드래그 로직은 무변경.

**9. 잔디/흙 반복 패턴 제거**: `(x+y)%3` 방식(주기 3의 대각선 줄무늬) → 셀 좌표 공간
해시(`x*374761393 + y*668265263` 후 xorshift) 1회로 변종(`hash%3`)과 뒤집기 상태
(`(hash/3)%4`) 독립 산출, `Tilemap.SetTransformMatrix`로 좌우/상하 뒤집기만 적용(90° 회전
금지 - 이 타일들은 좌우/상하 변만 seamless하게 설계돼 회전하면 이음매가 다시 보임). 흙길에도
동일 적용.

**부수 수정**: `VillageHubUiBuilder.cs`의 모든 Text가 Unity 내장 `LegacyRuntime.ttf`(한글
미지원)를 쓰고 있었다는 실제 결함을 발견해 `Fonts/NotoSansCJKkr-Regular.otf` 로더
(`LoadKoreanFont()`, internal)로 일괄 수정 - 이번 작업이 만든 결함이 아니라 기존에 있던
결함이지만, 이번에 손대는 모든 라벨에 직접 영향을 주므로 같은 파일 내에서 함께 고쳤다.

**파일 분할**: `VillageHubUiBuilder.cs`가 800줄을 넘어가 이 코드베이스의 기존 관례(파일당
~500줄, `ArtImportConfigurator`/`VillageHubTerrainBuilder`를 `SapphireSceneBuilder`에서
분리한 것과 동일한 원칙)에 따라 `VillageHubSkillMenuBuilder.cs`(스킬 부채꼴)와
`VillageHubMenuBuilder.cs`(Phase 3, 아래)로 분리했다.

### Phase 3 - 오딘식 메뉴 (D2(b) 확정안)

**1. 패널 배경**: `MenuPanelOdin.png`(793x1983) 9-slice, border 실측(색상 전이 지점,
30%-70% 구간 5개 행/열 전부 동일값) left 84 / right 84 / top 85 / bottom 93px.
우측(1,0)-(1,1) 세로 스트레치, 상하 margin 24, 폭 400. `pixelsPerUnit = 100*793/400`
(고정 치수인 폭 기준 보정 - 세로는 스트레치되므로 폭을 기준 치수로 삼음).

**2. `MenuCatalog.cs` 데이터소스 신설**: `MenuSectionDefinition{Title,Items}` +
`MenuItemDefinition{Id,Label,IconSpriteName,IsAvailable}` 정적 배열. 성장(장비✅/가방✅/
스킬북🔒/캐릭터정보🔒) · 모험(퀘스트✅/지도🔒/던전🔒) · 시스템(설정✅) - 실제 제공 4개
(장비/가방/퀘스트/설정)가 정확히 SSOT(01_PRODUCT.md 52행)와 일치.

**3. 레이아웃**: `MenuSectionHeader.png` 9-slice(border 실측 - 크롭된 2138x281 스프라이트
기준 left 232 / right 231 / top 99 / bottom 79px, `pixelsPerUnit=100*2138/360`) + 섹션
제목, 이어서 4열 그리드(cellWidth=(400-2*24)/4=88, 아이콘 56px, 라벨 14pt). 빌더
(`VillageHubMenuBuilder.BuildOdinMenu`)는 `MenuCatalog.Sections`를 순회만 하므로 항목
추가/삭제/재배열은 `MenuCatalog.cs`만 고치면 된다(빌더 코드 무변경).

**4. 잠긴 항목**: 아이콘 `Image.color=(0.45,0.45,0.5,1)` 그레이 틴트 + `MenuLockBadge.png`
(1278x1230, 알파bbox(239,52)-(1038,1230) 기준 799x1128 타이트크롭) 아이콘 우하단 오버레이
(아이콘의 40% 크기) + `Button.interactable=false`.

**5. 클릭 동작**: `MessagePanelFrameGold` 기반 서브패널을 재사용하되 새 `titleText` 필드
추가(`SimpleMessagePanel.SetTitleAndBody(title,body)`) - 제목=항목 라벨, 본문="이 기능은
다음 슬라이스에서 연결됩니다." 기존 토스트(제목 없이 라벨+문구 한 텍스트) 방식은 제거.
`MainMenuPanel.cs`는 얇은 컨트롤러로 재작성(83줄) - 클릭 이벤트 바인딩과 토글/ESC/디버그
훅만 담당, UI 생성 로직 없음.

**6. 메뉴 토글/ESC**: 기존 동작 무변경(`Toggle()`, ESC 키).

**7. 디버그 훅**: `-sapphire-open-menu` 커맨드라인 인자로 시작하면 `Awake()`에서 자동으로
메뉴가 열림(`Environment.GetCommandLineArgs()` 순회, 5줄 이내).

**폐기 자산**: `MenuPanelFrameGold.png`(세로 텍스트 목록용, 7개 항목 divider 6개) - 새 배선
이후 참조가 전혀 없음을 grep으로 확인 후 `git rm`.

**검증**: Unity CLI(6000.5.9f1) 컴파일, EditMode 테스트 33/33, `SapphireSceneBuilder.BuildAll`
재실행(빌드타임 assertion 통과 포함) 후 `VillageHub.unity`를 Python으로 직접 파싱 -
Image 컴포넌트 38개 전부 `m_Sprite` non-null, `MenuItem_*` GameObject 8개(장비/가방/퀘스트/
설정=`m_Interactable:1`, 스킬북/캐릭터정보/지도/던전=`m_Interactable:0`, SSOT와 정확히
일치), `KeyHint` GameObject 0개, 5개 스킬버튼 좌표·상호거리 표(전부 >96, 아래
`DECISIONS.md`에 재수록). code-reviewer 스킬로 두 차례(Phase2/Phase3 각각) 리뷰 - Phase2에서
`AssignField` 리플렉션 헬퍼 중복 + null-target 가드 누락 지적받아 즉시 수정 후 재검증,
Phase3에서는 `docs/REMEDIATION_PLAN.md`의 "5열" 서술과 코드의 4열 구현이 어긋난다는 점과
`MainMenuPanel.cs`의 디버그 훅 주석이 실재하지 않는 "Phase 3 item 7"을 인용한다는 점을
지적받아 각각 코드 주석·`DECISIONS.md` 기록·주석 정정으로 대응했다(`ArtImportConfigurator.cs`가
600줄을 넘어선 것도 함께 지적받아 `HudArtImportConfigurator.cs`로 분리).

**최종 검증 (Phase 2·3 전체 완료 후 1회, 사용자 지시대로 여기서만 수행)**: `SapphireBuildPlayer.
BuildWindows`로 비-개발 빌드 재생성(`builds/Windows/SapphireRPG.exe`, 빌드 로그에 에러 없음).
기존 실행 중이던 프로세스 없음을 확인 후 PowerShell(`AttachThreadInput`+`ShowWindow`+
`SetForegroundWindow`+`GetWindowRect`+`Graphics.CopyFromScreen` 절차)로 3개 시나리오를
각각 실행→6초 대기→캡처→종료했다:
- `final_1280_default.png`(1280x720 창모드, 기본 상태) - 상단 0-80px 밴드 빨간 픽셀 1305개.
  실제로는 에러 배너가 아니라 좌상단 HP 게이지(진홍색, 프레임 크기상 y=20~73.4가 이 밴드 안에
  들어옴) - 의도된 디자인 요소가 단순 색상 임계값 검사에 걸린 것으로 확인(육안 확인,
  "Rendering at an odd..." 류 텍스트 없음). 한글 폰트 수정도 이 스크린샷에서 실제로 확인됨
  ("사파이어 광장", "메뉴", "Lv.1" 전부 정상 렌더).
- `final_1280_menu.png`(동일 해상도 + `-sapphire-open-menu`) - 밴드 빨간 픽셀 1305개(동일한
  HP 게이지, 메뉴 패널 자체는 화면 우측이라 밴드에 영향 없음). 메뉴가 자동으로 열려
  성장/모험/시스템 3섹션과 8개 항목이 스크린샷에서 육안으로도 확인됨(디버그 훅 정상 동작).
- `final_1920_default.png`(1920x1080 창모드) - 밴드 빨간 픽셀 0개(HP 게이지가 이 해상도의
  스케일에서는 y=20~약 90 근처로 밀려 0-80px 밴드를 살짝 벗어남). 캡처된 이미지 크기가
  1936x1119로 창 크기보다 커 하단에 Windows 작업표시줄이 함께 잡혔다(로컬 디스플레이
  해상도가 1920x1080 요청보다 낮아 생긴 캡처 아티팩트로 추정 - 게임 자체 렌더링 문제 아님,
  이미지 상단 게임 영역은 정상 렌더됨).
- 마지막으로 1280x720 창모드로 재실행해 종료하지 않고 그대로 남겨둠(PID 확인, 사용자
  손테스트 대기 상태).
- 화면의 미학적 판단(배치가 보기 좋은지 등)은 하지 않았다 - 위 수치·객체 유무 확인만
  했고 이미지 파일 자체는 사용자가 직접 본다.

## 2026-09-15: REMEDIATION_PLAN.md Phase 1 완료 - 가로 1280x720 기준 정정 + Pixel Perfect Camera 제거 + 맵 경계 클램프

`docs/REMEDIATION_PLAN.md`(사용자가 실제 스크린샷을 보고 작성한 진단·계획 문서) Phase 1을 구현했다. 근본 원인은
기준 해상도 방향이 SSOT(`docs/planning/01_PRODUCT.md` - 가로 1280x720)와 반대(세로 720x1280)로 구현돼
있었던 것 - 상세 근거·수정 목록은 `docs/DECISIONS.md`의 같은 날짜 항목 참고. 요약:

- `VillageHubUiBuilder.BuildCanvas`: `CanvasScaler.referenceResolution` (720,1280) → (1280,720).
- `SapphireSceneBuilder.BuildCamera`: `PixelPerfectCamera` 제거, 일반 orthographic 카메라 +
  `orthographicSize=4.5`(세로 9타일 고정, 해상도 무관하게 항상 9타일).
- `Presentation/Camera/CameraFollowRig.cs`: Ground 타일맵의 `cellBounds`를 런타임에 읽어 카메라 가시
  사각형이 맵 밖으로 못 나가게 클램프하는 로직 신규 추가(`SetGroundTilemap`). 맵 크기가 다시 바뀌어도
  하드코딩 없이 자동 반영.
- `Packages/manifest.json`: `com.unity.2d.pixel-perfect` 의존성 제거.
- `SapphireBuildPlayer`: 기본 `BuildWindows()`가 이제 비-Development 빌드. Development가 필요하면
  별도 `BuildWindowsDevelopment()`를 쓴다. `PlayerSettings.defaultScreenWidth/Height`도 1280x720,
  `defaultIsNativeResolution=false`로(아래 "추가 수정" 참고).
- 함께 유지: 중단됐던 작업의 미커밋 변경(메뉴 버튼 배경 소실 수정 - `ArtImportConfigurator`의
  `pixelsPerUnit`에 `referencePixelsPerUnit(100)` 곱셈 누락, `VillageHubUiBuilder.cs` 일부, UI 텍스처
  meta 3개, `VillageHub.unity`)을 그대로 포함해 이번 커밋에 함께 반영했다(되돌리지 않음).

**추가 수정 (사용자 피드백 "켜주는 게임 창이 너무 크다")**: `ProjectSettings.asset`의
`defaultIsNativeResolution`이 켜져 있으면(Unity 템플릿 기본값) Windowed 모드에서도 저장된 창 크기가
없는 최초 실행이 데스크톱 네이티브 해상도로 뜬다 - `defaultScreenWidth/Height`가 무시되는 원인.
`false`로 껐고, 로컬 레지스트리(`HKCU\Software\Sapphire Studio\Sapphire RPG`)에 남아있던 이전 세션의
창 크기/위치 override 값도 삭제했다(`...Default` 접미사 키는 보존 - 그게 새 1280x720/Windowed
기본값을 담고 있다). 상세 근거는 `docs/DECISIONS.md` 2026-09-15 항목의 "추가 수정" 문단 참고.

**검증 (이번 라운드, 사용자 결정으로 범위 축소)**: Unity CLI(6000.5.9f1) 컴파일 확인, EditMode 테스트
33/33 통과, `SapphireSceneBuilder.BuildAll` 재실행 후 `VillageHub.unity`를 직접 파싱해 확인(Main
Camera 컴포넌트가 Transform/Camera/CameraFollowRig 3개뿐 - PixelPerfectCamera 없음, `orthographic
size: 4.5`, CanvasScaler `m_ReferenceResolution: {x: 1280, y: 720}`, `CameraFollowRig.groundTilemap`이
실제 Ground Tilemap을 참조), `ProjectSettings.asset` 직접 확인(`defaultScreenWidth: 1280`/
`defaultScreenHeight: 720`/`fullscreenMode: 3`/`defaultIsNativeResolution: 0`). **플레이어 빌드
실행+3개 해상도 스크린샷 검증은 이번 라운드에서 하지 않았다** - Phase 2·3까지 마친 뒤 마지막에 한 번만
실행 파일을 빌드해 검수하기로 사용자가 결정했다. 이 세션 도중 일부 진단용 스크린샷이
`generated-images/diagnostics/phase1_*.png`(gitignore 대상)에 남아있으나 중간 산출물일 뿐 최종 검수가
아니다. 화면 레이아웃의 미학적 판단(HUD 요소 배치·겹침 등, Phase 2 범위)도 하지 않았다 - 전부 사용자
몫이다.

**다음**: `docs/REMEDIATION_PLAN.md` Phase 2(HUD 재배선 - HP/MP바 개구부 실측, 스킬 버튼 부채꼴
간격, 지역명 슬롯) 이후 Phase 3(D2 확정 후 메뉴 오딘식 재설계) → Phase 4(이동 스틱·잔디 마감) 순.
플레이어 빌드+스크린샷 3종 검수는 Phase 2·3 완료 후 한 번만 수행한다.

## 2026-09-16: 메뉴 버튼 배경 소실 + HP바 프레임 불일치 + 스킬버튼 겹침 수정 - 이전 상태

사용자가 실제 스크린샷에서 지적한 4가지 중 3가지를 코드 근거로 원인을 확정하고 수정했다(4번째는 버그가
아님으로 판정). **화면 캡처·육안 판단은 하지 않았다** - 전부 씬 파일(.unity) 텍스트 파싱, 픽셀 알파
분석(PIL/numpy), Unity `Image.OnPopulateMesh` 리플렉션 호출, 좌표 계산으로만 검증했다.

**1. 메뉴 버튼 배경 완전 소실 (근본 원인, 진짜 버그)**: `VillageHubUiBuilder.BuildMainMenu`는 스프라이트를
정상적으로 할당하고 있었고 씬 파일의 `m_Sprite`도 null이 아니었다 - 처음 가정("스프라이트 미할당")은
틀렸다. 실제 원인은 `ArtImportConfigurator.ConfigureSingleSprite`에 넘긴 `pixelsPerUnit` 값이었다.
`Image.pixelsPerUnit`은 `sprite.pixelsPerUnit`이 아니라 `sprite.pixelsPerUnit / canvas.referencePixelsPerUnit`
(Unity uGUI `Image.cs`, 기본값 100)이라 실제로 쓰인 값이 의도한 값의 1/100이었고, 그 결과
`Image.Type.Sliced`의 `GetAdjustedBorders`가 border/padding을 ~100배 부풀려 계산해 슬라이스 9칸이 전부
0 또는 음수 폭이 되어 Unity 6의 새 가드(`UUM-71372`, 음수/0 크기 quad 스킵)에 걸려 **버텍스 0개**를
생성했다 - 씬 데이터·색상·enabled는 전부 정상인데 실제로는 아무것도 그려지지 않는 상태였다. 리플렉션으로
`Image.OnPopulateMesh`를 직접 호출해 수정 전 `currentVertCount=0`, 수정 후 `currentVertCount=36`(9칸 x
4버텍스, 정상)임을 확인했다. 수정: `ArtImportConfigurator.ConfigureUiFrames`의 세 `ConfigureSingleSprite`
호출(`MessagePanelFrameGold`/`MenuButtonGold`/`MenuPanelFrameGold`) 전부 `pixelsPerUnit`에 `100f *`를
곱함(예: `993f/280f` -> `100f * 993f/280f`). 이 세 에셋 전부 `Image.Type.Sliced`로 쓰이므로 동일하게
영향받고 있었다(닫기 버튼·메인메뉴 버튼·메뉴 목록 7개·메시지 패널·메뉴 패널 배경 전부) - 사용자가 직접
본 건 메인 메뉴 버튼뿐이지만 근본 원인은 공용이라 셋 다 같이 고쳤다.

**2. HP바가 프레임에 안 맞음 (진짜 버그)**: `ArtImportConfigurator.ConfigureHealthBarFrame`이
`HealthBarFrameGold.png`의 Track/Fill 셀을 "칸을 반으로 나눈 전체 영역"으로만 슬라이스하고 실제 그림
내용에 맞춰 크롭하지 않았다. PIL/numpy로 알파 채널을 직접 측정한 결과 Track 셀(1774x504) 안에 실제
그림은 알파 바운딩박스 기준 겨우 1744x358만 차지하고 나머지는 완전 투명 여백이었다(`Image.Type.Simple`이
이 여백까지 통째로 늘려 그리는 바람에 실제 캡슐 모양이 지정한 rect 안에서 작고 치우치게 그려짐).
수정: Track/Fill 슬라이스 Rect를 알파 바운딩박스로 타이트 크롭(Track: `(15,400,1744,358)`, Fill:
`(54,152,1665,213)`, 이전엔 `(0,383,1774,504)`/`(0,0,1774,383)`), `VillageHubUiBuilder.BuildHealthBar`의
`barHeight`를 새 종횡비(1744/358≈4.872)에 맞춰 74 -> 53.4로 재조정, Fill 앵커를 Track 내부의 실제
남색 창(별도로 색상 전이 지점을 스캔해 측정 - 골드/남색 경계를 픽셀 단위로 분류)에 맞춰
`(0.06,0.17)-(0.94,0.83)` -> `(0.096,0.249)-(0.901,0.757)`로 재조정.

**3. 원형 스킬 버튼 6개가 서로 겹침 (진짜 버그)**: `VillageHubUiBuilder.BuildRadialSkillMenu`가 5개
스킬 버튼을 반지름 130, 100~190도(90도 폭) 부채꼴에 배치했는데, 버튼 5개면 간격이 4칸뿐이라 칸당
각도가 22.5도 - 반지름 130에서 인접 버튼 중심 간 거리는 `2*130*sin(11.25도)≈50.7`유닛인데 버튼 지름
96(반지름 48+48)이 서로 안 겹치려면 최소 96유닛이 필요해 실제로는 필요 거리의 절반 정도(약 47%
부족)만 떨어져 있었다 - 좌표 계산으로 확정. 수정: 반지름 130->200, 각도 100~190도->95~223도(칸당
32도)로 넓히고, 빌드타임 화면 우측 경계 assertion(`ScreenHalfWidth=360`)을 다시 만족시키기 위해
`RadialSkillMenu` 루트의 `anchoredPosition.x`도 -130->-90으로 당겼다. 수정 후 인접 버튼 간 거리
110.25유닛(필요 96유닛 대비 +14.8% 여유), 기본공격 버튼과의 거리는 200유닛(필요 118유닛 대비 +69.5%
여유), `distanceFromRightEdge=338`(한도 360 대비 22유닛 여유) - 전부 좌표 계산으로 재확인.

**4. 화면 좌상단 빨간/초록 배너 (버그 아님, 오판정)**: Player.log(현재 실행분 + 직전 실행분
`Player-prev.log`) 전체를 처음부터 끝까지 읽었지만 `Debug.LogError`/`Debug.LogWarning` 계열 로그는
단 한 줄도 없었다. 배너 텍스트("Rendering at an odd-numbered resolution...", "Screen resolution is
smaller than the reference resolution...")를 grep으로 역추적한 결과 `com.unity.2d.pixel-perfect`
패키지의 `PixelPerfectCamera.OnGUI()`가 `#if DEVELOPMENT_BUILD || UNITY_EDITOR` 가드 안에서
`GUILayout.Box`로 직접 그리는 Unity 공식 온스크린 진단 오버레이였다(로그 파일에 안 남는 이유가
이것 - `Debug.Log`를 거치지 않는다). 실제 창 해상도(1920x1009, 세로가 홀수)가 참조 해상도(720x1280,
세로형)보다 작고 홀수라서 뜨는 정상 경고이며, Development Build에서만 보이고 코드 결함이 아니다 -
수정하지 않았다.

**검증**: Unity CLI(6000.5.9f1) 컴파일 확인, `SapphireSceneBuilder.BuildAll` 재실행(예외 없음 - 특히
2번 항목의 빌드타임 assertion이 새 반지름/각도로도 통과함을 확인), EditMode 테스트 33/33 통과,
재생성된 `VillageHub.unity`를 Python으로 직접 파싱해 위 수치(MainMenuButton 스프라이트 non-null,
HealthBar `sizeDelta=(260,53.4)`, Fill 앵커, 6개 버튼 좌표·상호거리) 전부 코드가 의도한 값과 일치함을
재확인. **`SapphireBuildPlayer`로 플레이어를 재빌드하거나 실행 파일을 다시 돌리는 것은 이번 단계
범위 밖(다음 단계의 30초 크래시 폴링 담당)이라 하지 않았다** - 다만 1번 항목(메뉴 버튼) 수정
직후에는 이 제약을 인지하기 전에 플레이어를 1회 재빌드해 실행하고 스크린샷으로 육안 확인한 이력이
있다(`screenshot_after_fix.png`, 골드 프레임이 정상적으로 보임을 확인) - 이후 경로는 전부 텍스트/픽셀
기반 검증으로 전환했다.

## 2026-09-15: 골드 등급 UI 에셋 6종 배선 + 반응형 근본 수정 - 이전 상태

2026-09-14에 배선한 UI(위 절)를 신규 고퀄리티 골드 에셋 6종으로 전면 교체하고, 사용자가 지적한
"칸 안 맞고 이상하고 반응형이 안 돼서 뭉개진다"는 문제를 3갈래로 나눠 근본 원인을 찾아 고쳤다.

**1. 에셋 교체** (`ArtImportConfigurator.cs`, `VillageHubUiBuilder.cs`): `SkillButtonFrame.png` →
`SkillButtonFrameGold.png`, `HealthBarFrame.png` → `HealthBarFrameGold.png`, `SkillIconsSet.png` →
`SkillIconsSetGold.png`, `MessagePanelFrame.png` → `MessagePanelFrameGold.png`, `WideButton.png`
(닫기 버튼·메뉴 버튼·메뉴 목록 항목 7개 전부) → `MenuButtonGold.png`, 메인 메뉴 패널 배경(기존
`MessagePanelFrame.png` 재사용) → 전용 `MenuPanelFrameGold.png`. 각 이미지는 PIL/numpy로 알파
컬럼/로우 프로파일을 다시 실측했다(균등분할 가정 금지 원칙 재적용, 구 에셋과 그리드 구성이
같아도 갭 위치·중점은 달랐다):
- `SkillButtonFrameGold`: 2셀 경계 갭이 672-710(구 632-737)으로 이동, 중점 691.
- `HealthBarFrameGold`: 2셀 경계 갭이 487-521(구 452-508)로 이동, 중점 504.
- `SkillIconsSetGold`: 3x2 경계가 완전한 제로-알파 갭이 아니라 1-13px 노이즈가 섞인 갭(골드
  연결 장식선 때문) - 갭 전체 구간의 중점(524, 1013)과 행 경계는 최소밀도 지점(502, 값 83 -
  0이 아님)으로 잡았다. 아이콘 배치·순서는 이미지로 직접 재확인해 기존과 동일함을 검증했다
  (기본공격/비전탄/서리파동 위 행, 점멸/보호막/질주 아래 행).
- `MessagePanelFrameGold`/`MenuButtonGold`/`MenuPanelFrameGold`: 9-slice border를 골드 프레임과
  남색 내부 채움 사이 색상 전이 지점으로 재측정(코너 곡선·중앙 다이아몬드 장식을 피한
  40%-60% 구간 중앙값) - `MenuButtonGold`는 "20px 패딩 크롭"이라는 보고를 신뢰하지 않고
  직접 재실측했다(border: left 114/right 116/top 77/bottom 71px).

**2. 원 스킬 버튼 정사각형 크롭 수정** (근본 원인 발견): 기존 코드는 스킬 버튼 프레임을 "컬럼만
자르고 전체 캔버스 높이(1024px) 그대로" 슬라이스했는데, 그 결과 셀 Rect가 정사각형이 아니었다
(684x1024, 852x1024 - 종횡비 약 1:1.5). 정사각형 버튼(`sizeDelta.x==sizeDelta.y`,
`Image.Type.Simple`, `preserveAspect` 미설정)에 이 비정사각형 스프라이트를 넣으면 원이 타원으로
찌그러져 보인다 - "안 맞고 뭉개진다"는 사용자 불만의 실제 원인 중 하나로 강하게 의심된다. 각
원의 알파 바운딩박스만 정확히 크롭해 거의 1:1 비율(Skill 659x665, BasicAttack 824x854)로
바꿔 해결했다.

**3. 9-slice `pixelsPerUnit` 보정**: 기존 코드는 단일 스프라이트(`ConfigureSingleSprite`)에
`spritePixelsPerUnit`를 전혀 지정하지 않아 Unity 기본값(100)이 그대로 쓰이고 있었다. 이 신규
골드 에셋들은 해상도가 매우 커서(1937x812, 993x251, 1007x1230) border 픽셀값도 크게 측정되는데,
ppu=100으로 나누면 캔버스 단위로는 2 미만까지 줄어들어 화면에 그려지는 골드 테두리가 거의
보이지 않을 정도로 얇아진다 - 이것이 "이상하다"는 불만의 또 다른 유력한 원인으로 판단했다.
`ConfigureSingleSprite`에 `pixelsPerUnit` 파라미터를 추가하고 각 에셋의 `nativeWidth(또는
Height)/sizeDelta` 비율로 명시 설정해(예: MessagePanelFrameGold 1937/560≈3.459) 테두리가
원본 이미지가 실제로 그려진 비례를 유지하도록 했다.

**4. 반응형/앵커 전수 점검**: `BuildCanvas`의 `CanvasScaler`(`ScaleWithScreenSize`,
reference 720x1280, `matchWidthOrHeight=0.5`)는 이미 적절했다(변경 없음). `BuildVirtualMovementPad`
(좌하단), `BuildRadialSkillMenu`(우하단, 2026-09-14에 이미 수정됨), `BuildHealthBar`(좌상단),
`BuildMainMenu`의 열기 버튼(우상단)은 전부 코너 anchor + 그 코너 기준 오프셋 패턴을 이미
올바르게 쓰고 있어 고정 720 폭을 가정하는 계산이 없었다. 단, `BuildMainMenu`의
`MainMenuPanel`(390x790 고정, 우상단 anchor)은 실제 버그를 하나 발견했다 - 7개 메뉴 항목(91px
간격) + 패널 상단 여백(102)까지 총 892 캔버스 유닛이 필요한데, 일반적인 16:9 가로(PC) 해상도의
캔버스 높이는 위 CanvasScaler 설정 기준 약 623-720 유닛뿐이라 패널이 화면 아래로 172-268 유닛
벗어난다(해상도 시뮬레이션으로 확인, 아래 참고). `anchorMin=(1,0)/anchorMax=(1,1)`로 세로
스트레치하도록 고쳐 패널 자체가 항상 화면 안에 들어오게 했다(항목이 91px 간격으로 패널 상단
기준 고정 배치라 아주 짧은 캔버스에서는 마지막 1-2개 항목이 패널 하단을 살짝 넘칠 수 있다는
잔여 제약은 남지만, 패널 자체가 화면 밖으로 나가던 것보다는 명확히 개선됐다 - 스크롤뷰 등 완전
해결은 이번 범위 밖).

**5. 해상도 시뮬레이션 검증** (좌표 계산, 육안 아님): 720x1280(기준 세로)/1920x1080/1280x720(가로
PC)/1080x2400(세로 폰)/2560x1080(울트라와이드) 5개 해상도에 대해 Python으로 CanvasScaler
스케일팩터와 각 UI 요소의 실제 캔버스 좌표를 계산 - VirtualMovementPad·RadialSkillMenu·
HealthBar·MainMenuButton 겹침 없음을 전 해상도에서 확인, MainMenuPanel은 스트레치 수정 후
5개 해상도 모두에서 패널 자체가 화면 안에 들어옴을 확인(가장 짧은 1280x720/1920x1080 캔버스
높이 720에서 패널 높이 598 - 화면 안, 단 7번째 항목 위치 -632가 패널 하단 -598보다 34유닛
더 내려가 항목 자체는 일부 겹칠 수 있음, 위 4번 잔여 제약과 동일).

**6. HP 바 종횡비 보정**: Track 셀 실측 종횡비가 구 자산(3.70)에서 3.52로 바뀌어
`barHeight`를 70→74로(barWidth=260 고정) 올려 `Image.Type.Simple`의 비균일 스트레치를
줄였다. Fill 안쪽 anchor y-span도 0.64→0.66으로 미세 조정.

**검증**: Unity CLI(6000.5.9f1)로 컴파일 확인(경고 1건만 - 기존과 동일한
`TextureImporter.spritesheet` obsolete 경고, 이번 변경과 무관), EditMode 테스트 33/33 통과,
`SapphireSceneBuilder.BuildAll` 재실행 후 `VillageHub.unity`를 직접 파싱해 신규 골드 텍스처 6종의
guid가 씬 전역에서 전부 참조되고(`SkillButtonFrameGold` 6회·`HealthBarFrameGold` 2회·
`SkillIconsSetGold` 6회·`MessagePanelFrameGold` 1회·`MenuButtonGold` 9회·`MenuPanelFrameGold`
1회) null(`fileID:0`) 스프라이트 참조가 전무함을 확인, `MainMenuPanel`/`HealthBar`/`Fill`의
RectTransform 필드(anchorMin/Max, sizeDelta)가 의도한 값대로 반영됐음을 확인,
`HealthBarView.fillImage`가 실제 Fill Image를 참조하고 `m_Type=3/m_FillMethod=0/m_FillAmount=1/
m_FillOrigin=0`을 유지함을 확인. `SapphireBuildPlayer.BuildWindows` 재빌드(`Assembly-CSharp.dll`·
`resources.assets` 타임스탬프가 빌드 시각과 일치) 후 새 실행 파일을 실행해 30초 이상 프로세스
생존을 `tasklist`로 확인했다. 화면 렌더링·미학 판단은 하지 않았다 - 사용자 몫이다.

**삭제한 파일** (grep으로 미참조 확인 후 `git rm`): `Art/UI/SkillButtonFrame.png`,
`Art/UI/HealthBarFrame.png`, `Art/UI/SkillIconsSet.png`, `Art/UI/MessagePanelFrame.png`,
`Art/WideButton.png` (각 `.meta` 포함).

## 2026-09-14: UI 에셋 전면 교체 + HP 바 신규 추가 - 이전 상태

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
