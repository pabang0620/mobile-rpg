# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향). 상세 근거는 `docs/DECISIONS.md` 참고, 여기는 "지금 코드가 실제로 어떤 상태인가"만 요약한다.

## 2026-09-16 (최신): 워리어 공격모션 시트 최종본 교체 + 스킬 VFX 중심 보정

사용자 지적 2건: (1) 전사 기본공격 Right 방향 포즈가 삐뚤어져 보임. (2) 스킬 이펙트가 캐릭터 중심에서 왼쪽아래로 살짝 치우쳐 보임.

**구현**: `Art/WarriorAttackGridSheet.png`를 새 시트로 교체 - 최종본은 Left 방향의 점 결함을 제거하고 Right는 더 이상 개별 AI 생성이 아니라 정리된 Left를 좌우반전해서 만듦(Down/Up 행 무변경). `SkillVfxPlayer.cs`/`WarriorSkillVfxPlayer.cs`의 공유 `ActorCenter()`에 월드 오프셋 `VfxCenteringOffset=(0.05, 0.075, 0)`(우측 4px/상단 6px)을 추가해 모든 스킬/기본공격 VFX 앵커를 한 곳에서 보정. 상세 alpha bbox 실측치·알려진 오차는 `docs/ASSET_STATUS.md` 해당 날짜 절 참고.

**검증**: 컴파일 0에러, `BuildEverything`(SceneOnly, 배치당 1회) -> `BuildWindows`(배치당 1회) 재빌드 성공. 사용자가 "왜 자꾸 켰다껐다하냐, 내가 확인할테니 수정만 하면 된다"고 지적해, 화면 캡처 반복 루프를 중단하고 이후 근거는 PIL alpha bbox 수치 측정으로 대체했다(공격시트 vs 이동시트의 방향별 footY 비교, `docs/ASSET_STATUS.md` 참고) - 실제 화면 판단은 사용자 손테스트 몫. 검증에 썼던 임시 디버그 훅(`RadialSkillMenu`의 `-sapphire-debug-face=`/`-sapphire-debug-cast=`)은 `git checkout`으로 완전히 원복, `git diff` 잔여 0건 확인 후 최종 재빌드까지 재확인했다. 최종적으로 빌드된 `SapphireRPG.exe`를 개발 인자 없이 1회만 실행해 로그인 화면(1280x720 창모드)이 뜬 상태로 유지.

## 2026-09-16: 기본공격/스킬 캐스트에 실제 공격 모션 추가 (Windup/Apex/Recovery)

사용자 요청: 기본공격이나 스킬을 쓸 때 캐릭터가 그 자리에 가만히 서 있던 것을, 전사는 칼을 휘두르는 식으로 실제 모션이 나오게 개선.

**구현**: 이미 도착해 있던 `Art/MageAttackGridSheet.png`/`Art/WarriorAttackGridSheet.png`(둘 다 걷기 시트와 동일한 1086x1448, 3열x4행, 362px 셀 - 컬럼만 Idle/WalkA/WalkB 대신 Windup/Apex/Recovery)를 배선했다. `CharacterGridSheetImporter.BuildGridSlices`에 `colNames` 옵션 파라미터를 추가(기본값은 기존 Idle/WalkA/WalkB라 걷기 시트 호출부는 완전히 무변경)해 `ArtImportConfigurator.ConfigureMageAttackSheet`/`WarriorArtImportConfigurator.ConfigureWarriorAttackSheet`에서 재사용 - 걷기 시트와 동일한 프레임별 alpha 기반 foot pivot(`CharacterFootPivotCalculator`)이 그대로 적용된다. 신규 컴포넌트 `Presentation/Movement/SkillMotionPlayer.cs`가 캐스트 시 `SpriteRenderer`를 현재 Facing 방향의 Windup->Apex->Recovery 3프레임으로 0.35초(워리어 기본공격 VFX와 동일 지속시간) 재생한 뒤 `DirectionalSpriteAnimator.ForceRefresh()`(신규 공개 메서드, 그냥 `ApplyFrame()` 재호출)로 idle/walk 프레임으로 복귀시킨다. `SapphireSceneBuilder.BuildPlayer`가 마법사/전사 두 리그 모두에 이 컴포넌트를 추가하고 스프라이트 12개(4방향 x 3프레임)를 배선하며, `RadialSkillMenu`는 `Awake`에서 `player.GetComponent<SkillMotionPlayer>()`로 참조를 얻어 `CastBasicAttack()`과 `CastSkill()` 양쪽에서 호출한다(기본공격뿐 아니라 스킬 캐스트에도 동일 모션이 재생됨). 이동 입력은 잠그지 않았다(요청에 없었고, 기본공격은 어차피 이동 중엔 캐스트 자체가 막혀 있어 경합 여지가 거의 없음).

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러(첫 시도에서 `RootArtDir` 네임스페이스 누락 CS0103 1건 발생, `SapphireSceneBuilder.RootArtDir`로 즉시 수정). 씬 도메인 로직 변경이 아니라 애니메이션 재생 컴포넌트라 EditMode 테스트는 추가하지 않았다(AGENTS.md 검증 관행 - 로직/상태 배선 버그가 아닌 신규 프레젠테이션 기능). `SapphireSceneBuilder.BuildEverything()`(4개 씬) -> `SapphireBuildPlayer.BuildWindows` 재빌드 성공. 씬 YAML을 직접 파싱해 마법사/전사 두 `SkillMotionPlayer` 컴포넌트의 windup/apex/recovery 12개 필드 전부가 null이 아닌 실제 스프라이트 guid를 가리키는지 확인했다. 임시 디버그 훅(`RadialSkillMenu`에 `-sapphire-repeat-cast-basic` - 기본공격을 0.3초 간격으로 재발동해 화면 캡처가 스윙 프레임을 확실히 잡도록 함, `capture_burst_vfxfix.ps1`과 동일한 SetForegroundWindow+CopyFromScreen 캡처 방식 재사용)으로 캡처한 스크린샷을 직접 확인 - 워리어(`generated-images/diagnostics/attackmotion_warrior.png`)는 검을 앞으로 내지르는 Apex 포즈와 슬래시 VFX가 함께 렌더됨을 확인, 마법사(`attackmotion_mage.png` + 4연속 버스트 크롭)는 idle과 거의 같아 보이는 Recovery 포즈와 수정이 발광하는 Apex 포즈가 실제로 프레임마다 교차 전환되는 것을 연속 캡처 비교로 확인(단일 스크린샷만으로는 mage 쪽이 육안 구분이 어려워 추가로 4연속 캡처했다). 디버그 훅은 검증 직후 완전히 제거하고 `grep`으로 코드베이스에 잔여 0건 확인한 뒤 최종 재빌드까지 재확인했다(영구 훅인 `-sapphire-class=`/`-sapphire-scene=`/`-sapphire-account=`/`-sapphire-open-menu`는 그대로 유지). 최종적으로 빌드된 `SapphireRPG.exe`를 개발 인자 없이 실행해 로그인 화면(1280x720 창모드)이 뜬 상태로 유지.

## 2026-09-16: 그리드 이동 - 방향 전환 시 첫 스텝은 회전만(포켓몬류 표준 동작)

사용자 요청: 방향키를 누르면 즉시 그 방향으로 "회전+1칸 이동"이 한번에 처리되던 것을, 눌린 방향이 현재 Facing과 같으면 그대로 이동하되 **다르면 이번 스텝은 회전만 하고 이동은 하지 않도록** 변경 - 방향을 바꾼 첫 입력은 제자리 회전으로 소비되고, 그 다음(같은 방향이 계속 눌려 있거나 다시 눌렸을 때, 이제 Facing과 일치하므로) 스텝부터 실제로 이동한다.

**구현**: 판정 자체는 순수 Domain 함수로 분리 - `GridMoveInputBuffer.ResolveStepAction(GridDirection inputDirection, GridDirection currentFacing) : StepAction`(신규 enum, `Move`/`TurnOnly`) - 입력 방향이 현재 Facing과 같으면 `Move`, 다르면 `TurnOnly`. `GridMover`에는 위치/`IsMoving`은 건드리지 않고 `Facing`만 바꾸는 `TurnToFace(GridDirection)`를 추가(이동 중에는 no-op, `TryBeginMove`의 가드와 동일 원칙) - 기존 `TryBeginMove`는 완전히 무변경이라 `GridMoverTests`의 기존 시나리오는 그대로 유지된다. `PlayerGridController.Update()`에서 우선순위 스택 최상위 방향을 뽑은 직후 `ResolveStepAction(direction, mover.Facing)`이 `TurnOnly`면 `mover.TurnToFace(direction)` + `spriteAnimator.SetFacing(...)`만 하고 즉시 `return`(이동 시퀀스·애니메이션 시작 안 함, 딜레이 없이 스프라이트만 즉시 전환) - `Move`일 때만 기존 `TryBeginMove` 경로로 진행한다. 직전 세션에 고정한 "눌린 순서 우선순위 스택"과 "키 떼면 즉시 정지" 동작은 이 판정 앞뒤로 무변경.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 87/87 PASS(순증 6 - `GridMoveInputBufferTests`에 `ResolveStepAction` 단위 테스트 2개 + 사용자 시나리오(탭 후 재입력, "아래 누른 채 오른쪽 탭 -> 회전만 -> 오른쪽 뗌 -> 아래로 회전만 -> 계속 누르면 이동") end-to-end 테스트 2개, `GridMoverTests`에 `TurnToFace` 단위 테스트 2개). `SapphireSceneBuilder.BuildEverything()`(4개 씬) -> `SapphireBuildPlayer.BuildWindows` 재빌드 성공(`SAPPHIRE_PLAYER_BUILD SUCCESS`). 판정 로직은 EditMode 테스트로 이미 검증됐으므로 화면은 "명백히 깨지지 않았는지"만 스크린샷 1장(`generated-images/diagnostics/turnfix_login.png`)으로 확인(로그인 화면 정상 렌더, 에러 배너 없음) - 반복 재조정은 하지 않음(AGENTS.md 작업 속도 규칙). 최종적으로 빌드된 `SapphireRPG.exe`를 개발 인자 없이 실행해 로그인 화면(1280x720 창모드)이 뜬 상태로 유지.

## 2026-09-16: 그리드 이동 - 방향 우선순위 고정 if/else 버그 수정 + 이동완료 시점 재판정으로 릴리즈 경합 완화

사용자 재보고 2건: (1) 아래를 누른 채 오른쪽을 탭하면 오른쪽 이동이 대부분 무시되고 가끔만 반영됨. (2) 꾹 눌렀다 떼면 멈춰야 하는데 가끔 1칸 더 감(직전 세션 `UpdateBuffer` 수정으로 대부분 해소됐으나 완전히는 아니었음).

**(1) 방향 우선순위 - 원인**: `PlayerInputReader.TryGetHeldDirection`이 4방향을 매 프레임 **고정된 if/else 순서**(항상 Up -> Down -> Left -> Right)로 검사해 첫 번째로 걸리는 방향 하나만 반환하고 있었다. Down을 누르고 있으면 그 아래 순번인 Right는 Down이 떨어지기 전까지 검사조차 되지 않으므로, "가장 최근에 눌린 키"가 아니라 "if/else에서 먼저 오는 키"가 항상 이겼다 - 이것이 "오른쪽 탭이 대부분 무시되고 가끔만 반영"의 정확한 메커니즘(가끔 반영되는 건 Down이 그 폴링 프레임에 우연히 안 잡힌 입력 타이밍 우연일 뿐, 우선순위 로직과는 무관). **수정**: `PlayerInputReader.TryGetHeldDirection(out GridDirection)`을 제거하고 4방향 각각의 raw held 상태를 그대로 노출하는 `GetHeldDirections() : HeldDirections`(신규 구조체, Domain)로 교체. `GridMoveInputBuffer`(Domain)를 단일 버퍼(`GridDirection?`)에서 **눌린 순서 우선순위 스택**(`List<GridDirection>`, index 0=최우선)으로 재설계 - 매 프레임 (a) 더 이상 눌려있지 않은 방향은 스택 어디에 있든 제거하고 (b) 이번 프레임에 새로 눌린 방향(직전 스택에 없던 것)은 무조건 맨 앞에 삽입한다. `PlayerGridController`는 이 스택의 맨 앞(`TopDirection`)을 그대로 이동 방향으로 쓴다 - 어느 물리 키든 가장 최근에 눌린 것이 항상 최우선.

**(2) 릴리즈 후 가끔 1칸 더 감 - 원인**: `isContinuousHold`(연속 홀드 여부 - true면 스텝 사이 정지 간격 `stepPause` 생략)가 이동 **시작 시점**에 캡처된 `bool` 값으로 코루틴에 그대로 박혀 있었다. 즉 이동이 시작될 때 키가 눌려 있었다는 사실 하나만으로 그 이동이 끝날 때까지 "연속 홀드"로 취급됐고, 이동 애니메이션 도중(0.343s 사이) 키를 놓아도 이 값은 갱신되지 않아 `stepPause`(0.04s) 없이 곧바로 `mover.CompleteMove()`가 불렸다 - 신선한 탭이나 릴리즈 직후에는 원래 있어야 할 40ms 안전 여유가 사라진 채 mover가 즉시 풀리는 구조였다. 순수 Update()/코루틴 실행 순서만으로는 매 프레임 입력이 항상 그 프레임 기준으로 정확하므로 이 자체가 결정론적 버그를 일으키진 않지만, 입력 폴링에 실제로 있는 미세한 프레임 단위 지연(가끔만 재현되는 것과 부합)이 끼어들 여유(cushion)를 이 생략이 없애버린 것으로 판단. **수정**: `GridMoveAnimator.PlayMove`의 `isContinuousHold` 파라미터를 `bool`에서 `Func<bool>`로 바꿔, 이동 **완료 시점**(코루틴이 stepPause 여부를 판단하는 바로 그 순간)에 `PlayerInputReader.GetHeldDirections()`를 다시 호출해 "그 방향키가 지금도 눌려있는가"를 재확인하도록 변경 - 시작 시점에 눌려 있었는지(`wasAlreadyHeldLastFrame`)와 완료 시점에도 여전히 눌려 있는지를 모두 만족해야 `stepPause`를 생략한다. 키가 도중에 떨어지면 자동으로 40ms 안전 여유가 다시 적용된다.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 81/81 PASS(`GridMoveInputBufferTests` 12개로 전면 재작성 - 우선순위 스택 단위 테스트 + 버그 리포트 시나리오 end-to-end 테스트 포함, 기존 7개 대비 순증 5). `SapphireSceneBuilder.BuildEverything()`(4개 씬) -> `SapphireBuildPlayer.BuildWindows` 재빌드 성공. 로그인 화면 스크린샷 1장(`generated-images/diagnostics/smoke_20260916_login.png`)으로 정상 렌더 확인(에러 배너 없음) - 실제 키 입력 우선순위 반응은 EditMode 테스트로 검증했고 반복 스크린샷 재조정은 하지 않음(AGENTS.md 작업 속도 규칙). 최종적으로 빌드된 `SapphireRPG.exe`를 개발 인자 없이 실행해 로그인 화면(1280x720 창모드)이 뜬 상태로 유지.

## 2026-09-16: 마을 메뉴 헤더/그리드가 패널 밖으로 나오던 근본 원인 수정

사용자가 "성장/모험/시스템 글자가 메뉴판 밖에 나와있다"고 두 번째로 지적. 이전 수정(`HeaderWidthReduction` -20px씩)은 헤더에만 적용된 땜질이었고 근본 원인이 아니었다.

**원인**: `VillageHubMenuBuilder.OdinContentMargin`이 `MenuPanelOdin.png`(v3 베이지 에셋, border 52px @ ppu 300 -> 캔버스 17.33유닛)의 테두리를 기준으로 계산돼 있었는데, 실제로 패널에 그려지는 스프라이트는 이번 세션 밖에서 들어온 커밋(`ab85e1c`)이 바꿔치기한 `MenuPanelDark.png`(border 52px @ ppu **100** -> 캔버스 **52**유닛, 3배)였다. 헤더뿐 아니라 이 마진을 공유하는 아이콘 그리드 전체가 실제 테두리보다 훨씬 안쪽 여백만 확보한 채 배치되고 있었다.

**수정**: `MenuPanelOdinLeftRightBorderPx`/`MenuPanelOdinSpritePixelsPerUnit`를 `MenuPanelDark.png`의 실제 임포트값(52px, ppu 100)으로 교체 - `OdinContentMargin` 11.33 -> 46으로 정정. 헤더 전용 `HeaderWidthReduction`(-40) 땜질은 제거 - 이제 헤더가 그리드와 같은 `contentWidth`를 그대로 쓴다.

**검증**: 컴파일 성공(`SapphireSceneBuilder.BuildEverything`), 플레이어 빌드 성공(`SapphireBuildPlayer.BuildWindows`). 라이브 스크린샷 픽셀 실측: 수정 전 헤더 텍스트 첫 픽셀 x=798(패널 왼쪽 테두리 x=833보다 35px 왼쪽, 게임 배경 위에 렌더) -> 수정 후 x=835(테두리 안쪽, 정상). 단순 마진 상수 조정이라 EditMode 테스트는 재실행하지 않았다(`AGENTS.md` 작업 속도 규칙 4번). 오케스트레이터가 직접 Edit로 수정하고 직접 빌드/커밋했다(에이전트 미사용).

## 2026-09-16: 그리드 이동 - 키 뗀 뒤 추가 이동 버그 수정 + 이동속도 70%로 하향

**(A) 방향키를 떼도 ~0.5초(1~2칸) 더 가던 버그 - 원인은 직전 세션에 도입한 입력 버퍼**: `GridMoveInputBuffer.UpdateBuffer`가 `isDirectionHeld ? heldDirection : currentBuffer`로 구현돼 있어, 키가 올라간 프레임에도 이전에 버퍼된 방향을 그대로 유지하고 있었다. 문제는 이 버퍼가 "이동이 막힌 창 안에서 탭했다가 놓은 것"과 "계속 누르고 있다가 애니메이션 도중 그냥 놓은 것"을 구분하지 못한다는 점 - 후자(사용자가 실제로 겪은 케이스: 꾹 눌러 이동하다 떼는 순간)에도 버퍼에 마지막 방향이 그대로 남아 있다가, `mover.IsMoving`이 풀리는 순간 `ResolveMoveDirection`이 그 스테일 값을 채택해 원치 않는 추가 이동 1회를 시작시켰다(이동시간+`stepPause`만큼, 사용자가 느낀 "~0.5초 더 감"과 일치). **수정**: `UpdateBuffer`를 `isDirectionHeld ? heldDirection : null`로 변경 - 버퍼는 이제 "이번 프레임에 실제로 눌려 있는 방향"만 반영하고 키가 올라간 즉시(그 프레임에) 비워진다. 트레이드오프: 이동 애니메이션 도중 탭했다가 창이 끝나기 전에 놓은 입력을 창이 끝나는 순간 자동으로 이어서 처리해주던 반응성 기능은 이번 수정으로 사실상 무력화됨(놓은 키는 더 이상 다음 이동을 예약하지 않음) - "떼면 즉시 멈춘다"는 사용자 요구가 그 반응성 기능과 근본적으로 충돌해 우선순위를 명시적으로 정지 쪽에 둔 것(둘 다 프레임 단위 상태만으로는 구분 불가능한 동일 패턴이라 절충 불가). `ResolveMoveDirection` 자체(현재 눌림 우선, 아니면 버퍼 fallback)는 변경하지 않음 - 순수 함수 규칙은 여전히 유효하고, 실제로 스테일 버퍼가 발생하지 않게 된 것은 `UpdateBuffer` 쪽 수정 때문. **회귀 테스트**: 기존 6개 중 버그를 그대로 고정하던 `UpdateBuffer_NotHeld_KeepsPreviousBufferedValue`를 `UpdateBuffer_NotHeld_ClearsBufferImmediately`(반환값 null 검증)로 교체, 사용자가 겪은 정확한 시나리오(누름→이동 도중 뗌→몇 프레임 뒤 mover 해제→추가 이동 없음)를 end-to-end로 검증하는 `PressedThenReleasedBeforeMoveEnds_NoExtraMoveWhenMoverFrees` 신규 추가(총 7개).

**(B) 이동속도가 너무 빠름 - 현재 속도의 70%로 하향**: `GridMoveAnimator.moveDuration`(직전 세션에 0.32s→0.24s로 단축된 값)을 실제 코드에서 재확인 - 0.24s였다. 속도는 duration에 반비례하므로 70% 속도 = duration을 0.7로 나눈 값: `0.24 / 0.7 = 0.342857... ≈ 0.343s`로 변경. `stepPause`(0.04s)는 사용자가 언급하지 않아 무변경.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 76/76 PASS(75 기존 - 버그를 고정하던 테스트 1개 교체 + 신규 시나리오 테스트 1개 추가 = 순증 1). `SapphireSceneBuilder.BuildEverything()`(4개 씬) → `SapphireBuildPlayer.BuildWindows` 재빌드 성공(`SAPPHIRE_PLAYER_BUILD SUCCESS`, batchmode 정상 종료). (A)는 코드 레벨 근거(원인 추적 서술 + `PressedThenReleasedBeforeMoveEnds_NoExtraMoveWhenMoverFrees` 테스트로 "떼면 추가 이동 없음"을 직접 검증)로 확인, 반복 스크린샷 재조정은 하지 않음(AGENTS.md 2026-09-16 검증 관행 기준). (B)는 빌드 성공까지만 확인, 실제 체감 속도 판단은 사용자 손테스트 몫. 최종적으로 빌드된 `SapphireRPG.exe`를 개발 인자 없이 실행해 로그인 화면(1280x720 창모드)이 뜬 상태로 유지.

## 2026-09-16 (최신): 메뉴 헤더 폭 초과 수정 + 그리드 이동 입력버퍼링 + 캐릭터 선택 클릭 안 되던 버그 수정

**(A) 메뉴판 섹션 헤더 폭 초과**: `VillageHubMenuBuilder.OdinContentMargin`(헤더가 배치되는 안쪽 여백)을 실측하니 `52/(300/100) + 14 - 20 = 11.33px`로, 패널의 실제 시각적 테두리 두께(`52/(300/100) = 17.33px`)보다 6px 더 안쪽(패널 중심 쪽이 아니라 테두리 쪽으로)에서 시작하고 있었다 - 직전 세션의 `OdinHorizontalPaddingReduction=20` 도입이 테두리 두께 자체보다 크게 당겨서 헤더(구분선+텍스트)가 테두리를 6px씩(좌우 총 12px) 침범해 그려지고 있었다. 태스크 지정대로 헤더 폭 자체를 좌우 20px씩(총 40px) 줄이고 같은 중심을 유지하도록 재배치(`VillageHubMenuHeaderBuilder.HeaderWidthReduction=40`, `anchoredPosition.x`에 `+20` 보정) - 결과적으로 헤더 좌우 여백이 테두리 안쪽으로 14px(=`OdinContentMarginClearance`, 아이콘 그리드 원래 여백값)까지 벌어져 침범이 완전히 해소됐다(아이콘 그리드 자체의 `contentWidth`/`cellWidth`는 이번 요청 범위가 아니라 무변경). 콘텐츠(헤더+아이콘 그리드) 전체를 패널 안에서 위로 15px 이동(`VillageHubMenuBuilder.ContentUpShift=15`) - `OdinContentPadding`(상하 여백을 균등하게 유지하던 기존 계산, 각 128px)을 상단만 113px로 줄임(하단은 128px 그대로, 사용자가 "위쪽에 여유공간이 있다"고 지적한 부분만 조정). `VerifyPanelLayout`에 상단 여백이 `MinTopMarginAfterShift=8px` 이상 유지되는지 빌드 타임 어서션 추가(현재 113px로 충분히 여유, MenuCatalog가 더 커져도 0 이하로 붕괴하면 즉시 예외).

**(B) 그리드 이동 반응성 - 입력 버퍼링 부재**: `PlayerGridController.Update()`를 실측한 결과, `mover.IsMoving`이 true인 동안(이동 애니메이션 0.32s + 논컨티뉴어스 스텝의 `stepPause` 0.04s) 매 프레임 입력을 읽기는 하지만 그 값을 그냥 버리고 있었다 - 새 방향키를 탭했다가 그 이동 창(최대 ~0.36s) 안에서 놓아버리면, `mover.IsMoving`이 풀려서 다시 입력을 읽는 시점엔 이미 키가 올라가 있어 그 탭 자체가 통째로 유실됐다. "옆 아래 옆 아래" 연타가 굼뜨게 느껴진 정확한 원인. 방향 전환(제자리 회전, 벽 쪽으로 눌러 막히는 경우)은 `GridMover.TryBeginMove`가 이동 성공 여부와 무관하게 `Facing`을 먼저 갱신하므로 원래부터 이동보다 빨랐다(수정 불필요, 확인만 함). **수정**: 순수 로직 `Sapphire.Domain.Grid.GridMoveInputBuffer`(신규, `UpdateBuffer`/`ResolveMoveDirection` 2개 정적 메서드)를 도입해 매 프레임(이동 중 포함) 마지막으로 눌린 방향을 기억해뒀다가, `mover.IsMoving`이 풀리는 즉시(그 시점에 키가 이미 떼어져 있어도) 그 버퍼된 방향으로 바로 다음 이동을 시작하도록 `PlayerGridController.Update()`를 변경. 한 타일 이동 시간(`GridMoveAnimator.moveDuration`)도 과거 이력(0.08→0.16→0.4→0.32, "너무 빠름"/"너무 느림" 왕복)을 참고해 완전 원복은 피하면서 0.32→0.24로 완만히 추가 단축(태스크 지정 기준선 0.25s 이하로). `CharacterFootPivotCalculator`(발 pivot 계산)는 스프라이트 임포트 시점 로직이라 이동 타이밍/버퍼링과 완전히 무관 - 영향 없음을 코드 확인. 회귀 테스트: `GridMoveInputBufferTests`(6개, `Assets/Sapphire/Tests/Domain/`) 신규 추가 - `PlayerGridController` 자체는 `Keyboard.current` 의존 MonoBehaviour라 EditMode 유닛 테스트로 격리 불가하므로 생략, 대신 버퍼링 판단 로직만 순수 정적 클래스로 뽑아 테스트했다.

**(버그) "캐릭터 선택" 메뉴 클릭이 안 되던 문제 (오케스트레이터 추가 리포트)**: 직전 커밋(`d916e2c`)의 자체 보고("MainMenuPanel의 런타임 id 기반 dispatch로 재구현했다")는 배선 자체는 맞았다 - `MainMenuPanel.Awake()`가 매 씬 로드마다 실제 `Button.onClick.AddListener`를 실행하고(Editor 빌드 스크립트의 비영속 리스너 문제 없음), id 매칭도 정확했다. **진짜 원인은 한 단계 아래**: `VillageHubMenuBuilder.BuildOdinMenuItem`이 만드는 메뉴 아이템 루트 GameObject가 `Button`만 있고 자기 자신의 `Graphic`이 없었고, 그 자식인 Icon/Label 둘 다 `raycastTarget=false`로 명시돼 있었다 - `UnityEngine.UI.GraphicRaycaster`는 `raycastTarget=true`인 `Graphic`만 히트테스트 후보로 삼기 때문에, 이 아이템 계층 전체에 클릭 가능한 대상이 하나도 없어서 클릭이 항상 패널 배경 이미지(클릭 핸들러 없음)로 그냥 통과해버렸다 - **"캐릭터 선택"/"게임 종료"뿐 아니라 그리드의 9개 항목 전부**가 마우스로는 눌리지 않는 상태였다(이번 세션 직전에 새로 추가된 두 항목만 실제로 눌러봐서 발견된 것뿐). **수정**: `BuildOdinMenuItem`의 아이템 루트에 `Image`(alpha 0, `raycastTarget=true`)를 추가해 셀 전체가 클릭 히트테스트 대상이 되도록 함 - `MainMenuPanel` 쪽 코드는 무변경. **검증**: PowerShell로 실제 OS 마우스 클릭(`SetCursorPos`+`mouse_event`, 좌표는 `RectTransformUtility.WorldToScreenPoint`로 실측)을 빌드된 `SapphireRPG.exe`의 "캐릭터 선택" 버튼 위치에 직접 발생시켜, 클릭 전/후 스크린샷(`generated-images/diagnostics/click_before.png`/`click_after.png` - gitignore 대상)으로 VillageHub -> CharacterSelect 씬 전환이 실제로 일어남을 확인했다(코드 내부 시뮬레이션 raycast는 이 환경에서 이유 불명으로 0건만 나와 폐기 - 대신 실제 OS 클릭으로 검증). "게임 종료"도 동일한 `BuildOdinMenuItem` 경로를 쓰므로 같은 수정으로 함께 해결됨(별도 확인 불필요).

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 75/75 PASS(69 기존 + `GridMoveInputBufferTests` 6개 신규, 회귀 없음). `SapphireSceneBuilder.BuildEverything()`(4개 씬) → `SapphireBuildPlayer.BuildWindows` 재빌드 성공. (A) `generated-images/diagnostics/v4_menu.png`(+확대 크롭 `v4_menu_zoom_top.png`)를 직접 Read로 확인 - 헤더 구분선이 패널 테두리 안쪽에 여유 있게 들어옴, 상단 여백 자연스러움(0 아님). (B) 사용자가 직접 손테스트하기로 해 반복 캡처 검증은 하지 않았음 - 코드 근거(원인·수정 내용)와 EditMode 회귀 테스트로 근거를 남김. (버그) 위 캐릭터선택 클릭 검증 스크린샷으로 대체.

## 2026-09-16 (최신): 마을 메뉴판 확장 + 시스템 섹션(캐릭터선택/게임종료) 추가 + 로그인 입력칸/버튼 재배선

**(A) VillageHubMenuBuilder 메뉴판 레이아웃**: 패널 너비 400->520(1.3배, `OdinPanelWidth`), 4열이 아니라 기존부터 3열(`OdinColumns=3`)이던 그리드 그대로 폭만 넓어져 셀 너비 125.78->165.78, 컬럼 피치 94.33->124.33(같은 0.75 압축비 유지, 아이콘 크기 54는 무변경 - 스크린샷상 균형 양호해 추가 조정 안 함). 우측 정렬(anchorMin/Max=(1,y))이라 패널이 넓어져도 오른쪽 화면 밖으로 안 나가고 왼쪽으로만 확장됨(우측 640px 여백 유지 확인).

패널 "높이 위아래 여백 안 맞음" 문제(스크린샷 `final_village_warrior_menu.png`의 하단 큰 빈 공간)는 처음엔 패널 자체 높이를 콘텐츠 기준으로 줄이고 외곽 top/bottom 마진을 동일하게 계산하는 방식으로 시도했으나(마진 각 98.5px), 이 방식이 그 자리에 함께 있는 `VillageHubSkillMenuBuilder`의 전사/마법사 스킬부채꼴(같은 우하단 코너에 앵커)을 가리지 못해 메뉴 열림 상태에서 스킬버튼이 비쳐 보이는 회귀를 일으켰다(스크린샷으로 발견, 사유는 `docs/DECISIONS.md` 참고). **최종안**: 패널 자체의 바깥 top/bottom 마진(`OdinPanelTopMargin=0`/`OdinPanelBottomMargin=20`)은 원래 값 그대로 유지하고, 대신 패널 "안쪽" 첫 섹션 위 공백과 마지막 섹션 아래 공백(`OdinContentPadding`, 신설)을 `(패널높이 - 콘텐츠높이)/2`로 계산해 서로 같아지도록 했다 - 콘텐츠높이는 `ComputeSectionsHeight()`가 MenuCatalog 데이터로 동적 계산(현재 444px), 패딩은 각 128px로 나옴. 기존에 있던 "시스템 섹션을 패널 바닥에 강제로 붙이는" 특수분기(풋터 버튼 자리 확보용)는 제거 - 이제 시스템도 다른 섹션과 똑같이 `OdinSectionGap=4`로 자연스럽게 흘러간다(모험-시스템 간격이 기존 ~180px에서 4px로 줄어 사용자가 요청한 "20px 위로"보다 훨씬 크게 줄었으므로 별도 -20 추가 적용은 안 함, 사유는 코드 주석+DECISIONS.md).

**(B) 시스템 섹션에 캐릭터선택/게임종료 추가**: `MenuCatalog.cs`에 두 항목 추가(`IsAvailable=true`, 잠금뱃지 없음). 아이콘은 신규 `MenuIconsSetExtra.png`(980x460, 2셀 460x460, PIL 알파 실측으로 셀 경계가 나비스펙 그대로 정확함을 확인)에서 `HudArtImportConfigurator.ConfigureMenuIconsSetExtra`로 슬라이스. 클릭 동작은 `MainMenuPanel`이 id 기반으로 분기(`character_select`->`SceneManager.LoadScene("CharacterSelect")`, `quit`->기존 `ConfirmDialog` 재사용해 확인창 후 `Application.Quit()`/에디터에서는 `EditorApplication.isPlaying=false`) - 기존에 있던 "캐릭터 선택으로" 별도 footer 버튼 코드(`BuildCharacterSelectButton`)는 실제로는 Build()에서 호출되지 않는 죽은 코드였다(Editor 빌드 스크립트에서 직접 AddListener한 리스너는 비영속이라 씬 저장/재로드에서 사라지는 구조적 버그 - 그래서 죽어 있었음) - 삭제하고 동일 기능을 `MainMenuPanel.Awake()`의 런타임 리스너로 재구현(이건 씬 재로드/플레이어 빌드에서도 정상 동작).

**(C) 로그인 입력칸/버튼 신규 에셋**: `NicknameInputFieldV2.png`(2172x408, border 130 사방, PIL 실측 - 발광 테두리+라운드코너 전체가 border 안에 들어옴), `LoginStartButtonV2.png`(1580x250, Normal/Pressed 2셀 750x250, border left/right 120·top/bottom 20 - 핵사곤 뾰족한 끝점이 750셀 기준 x=75~114에서 시작하는 걸 PIL로 실측해 120으로 확정)를 `Art/UI/Title/`에 추가하고 `CharacterFlowArtImportConfigurator`에 등록. `LoginSceneBuilder`가 기존 `InputFieldFrame.png`/`ButtonPrimary.png` 참조를 이 두 파일로 교체(레이아웃 크기·위치는 무변경, 두 파일 다 다른 화면에서도 쓰이는지 grep 확인 후 무영향 확인 - `InputFieldFrame`은 CharacterCreate가 계속 씀, `ButtonPrimary`는 로그인 전용이었음). 버튼은 `Selectable.Transition.SpriteSwap`으로 전환해 Pressed 스프라이트가 실제로 배선됨(기존엔 ColorTint만 있었음).

**파일 분할**: `VillageHubMenuBuilder.cs`가 555줄로 500줄 규칙을 넘어서 `VillageHubMenuHeaderBuilder.cs`(섹션 헤더/디바이더 + 게임종료 확인 다이얼로그, 108줄)로 분리 - 472줄로 복귀.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 69/69 PASS(회귀 없음, 신규 도메인 로직 없어 테스트 추가 없음). `SapphireSceneBuilder.BuildEverything()`(4개 씬) -> `SapphireBuildPlayer.BuildWindows` 재빌드 성공. 스크린샷(`generated-images/diagnostics/v3_menu.png`/`v3_menu_quit_confirm.png`/`v3_login.png`) 직접 확인 - 패널 화면 밖으로 안 나감, 시스템 섹션 3항목(설정/캐릭터 선택/게임 종료) 겹침 없이 정렬, 게임종료 확인창 정상 표시, 로그인 화면 신규 입력칸/버튼 정상 렌더, "게임 시작"/"아이디 입력" 텍스트 전부 프레임 경계 안에 들어옴(줌 크롭으로 픽셀 단위 확인). 스크린샷 검증용 임시 디버그 훅(`MainMenuPanel`의 `-sapphire-open-quit-confirm`)은 검증 후 완전히 제거하고 재빌드로 재확인(영구 훅 `-sapphire-open-menu`/`-sapphire-class=`/`-sapphire-scene=`은 유지). 패널 우하단 코너 장식 컷아웃 뒤로 스킬부채꼴 버튼 일부가 살짝 비치는 현상은 기존(풋터 버튼이 죽은 코드가 되기 전)부터 잠재해 있던 것으로 이번 작업 범위 밖 - `docs/DECISIONS.md` 참고.

## 2026-09-16: 법사 방향성 VFX 알파 배선 + 로그인/캐릭터선택/캐릭터생성 프리미엄 UI 전면 재배선

두 묶음을 한 세션에서 끝까지 배선했다. 실측값·좌표계 결정 근거는 `docs/DECISIONS.md` 같은 날짜 항목, 에셋 목록은 `docs/ASSET_STATUS.md` 같은 날짜 항목 참고 - 여기는 "지금 코드가 실제로 어떤 상태인가"만 요약한다.

**(A) 법사 방향성 VFX 알파 배선**: `MageDirectionalPadded.png`(텔레포트/고드름/번개창 3스킬 담당, colortype=2 RGB·체커보드 배경 결함)를 `generated-images/mage-vfx-fix/MageDirectionalPadded_v2_noref.png`(2048x768, RGBA)로 교체했다. 기존 파일은 `generated-images/mage-vfx-fix-backup/MageDirectionalPadded_orig_backup.png`에 백업. 치수(2048x768)가 완전히 동일해 `SkillVfxImporter.ConfigureDirectionalTexture`의 그리드 상수(width/height 기반 슬라이스 계산)는 무변경 - Unity가 파일 내용 변경을 감지해 자동 재수입, `VfxFramePivotCalculator.ComputeContentPivot`이 이제 실제 alpha bbox로 pivot을 재계산한다(기존엔 알파가 없어 셀 전체를 콘텐츠로 오인).

**(B) 로그인/캐릭터선택/캐릭터생성 프리미엄 UI**: `generated-images/title-kit-v2/`(`tools/ui_kit/build_title_kit_v2.py` 산출물) 11개를 `Art/UI/Title/`에 배선, 기존 베이지 `CharacterSlotFrame.png`(+.meta)는 참조 0건 확인 후 `git rm`.

- `CharacterFlowArtImportConfigurator.cs`: 신규 자산 전부 `ArtImportConfigurator.UiKitV3PixelsPerUnit`(300, SCALE_V2=3 native=target*3 컨벤션) 적용. `CharacterSlotFrameV2`/`EmptyV2` border는 생성 스크립트의 badge-notch(top)·divider(bottom) 위치를 실측해 비대칭(`Vector4(48,270,48,216)` = left,bottom,right,top) 확정 - 카드 실제 높이(select 340/create 250)가 달라도 뱃지·구분선이 고정 위치를 유지한다. `ApplySingleSliceBorder`를 `HudArtImportConfigurator`에서 `ArtImportConfigurator`로 이동(공용 헬퍼, 신규 버튼 3종도 재사용).
- `CharacterSelectSceneBuilder.cs`: 카드 배경이 `CharacterSlotFrameV2`(채움)/`CharacterSlotFrameEmptyV2`(빈 슬롯)로 상태별 스프라이트 교체(`CharacterSlotCardView.cardBackground` 신설), 클래스 배지(`ClassBadgeMage/Warrior`)를 노치에, `CharacterPedestal`을 발밑에, `NameplateBar` 위에 이름(큰 흰 글씨)+"클래스 · Lv.N"(밝은 사파이어톤, 기존 어두운 갈색 대비값 폐기), 선택/삭제는 `ButtonSelectV2`/`ButtonDeleteV2`, 빈 슬롯은 `ButtonCreateV2`(다크 브라운 텍스트로 대비 확보). `VerifyButtonsInsideCard`를 새 하단 고정 영역(90유닛) 기준으로 재정의(기존 48px 전방향 inset 가정이 새 레이아웃과 충돌해 최초 빌드가 예외로 실패 - 실측 후 고쳤다).
- `CharacterCreateSceneBuilder.cs`: 클래스 카드도 동일 `CharacterSlotFrameV2` + 배지로 교체, 선택 글로우를 단색 사각형에서 `CharacterCreateSpotlight` 방사형 이미지로, "생성" 버튼을 `ButtonPrimary`에서 `ButtonCreateV2`로 교체. 카드 내부 레이아웃(포트레이트/라벨/설명)을 배지 존(72유닛)을 피해 재조정.
- `LoginSceneBuilder.cs`: 아이디 입력+게임시작 버튼을 감싸는 `LoginPortalFrame` 배경 패널 신규 추가(레이아웃 자체는 무변경, 프레임만 씌움).
- **실측으로 발견한 진짜 버그(이론이 아니라 스크린샷 확인)**: 캐릭터 선택 카드의 이름/클래스 텍스트, 캐릭터 생성 카드의 클래스 라벨이 전부 완전히 안 보였다 - 원인은 폰트 크기 문제가 아니라 Text의 `sizeDelta.height`가 fontSize가 필요로 하는 줄 높이보다 작아 기본 `verticalOverflow=Truncate`가 글자를 통째로 잘라낸 것(예: fontSize22에 height20). 해당 Text들의 box height를 fontSize의 약 1.2배 이상으로 키우고 `verticalOverflow=Overflow`로 명시 - 재캡처로 실제 렌더 확인.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 69/69 PASS(기존 69 - 신규 테스트 추가 없음, 이번 작업은 UI 배선·에셋 교체라 도메인 테스트 대상 로직 없음). `SapphireSceneBuilder.BuildEverything`(4개 씬 전부) -> `SapphireBuildPlayer.BuildWindows` 재빌드 성공. 스크린샷(`generated-images/diagnostics/v2_*.png`, gitignore 대상) 전부 직접 확인 - 로그인 프레임 정상, 캐릭터선택 카드(채움 2+빈 슬롯 2) 이름/레벨 텍스트 정상 렌더, 캐릭터생성 마법사/전사 카드 선택 전환(밝기+스케일+글로우) 정상, 마법사 텔레포트/고드름/번개창 VFX 체커보드 완전 해소(투명 배경 확인). 스크린샷 검증에 쓴 임시 디버그 훅(`RadialSkillMenu`의 `-sapphire-cast-skill=<index>`, `CharacterCreateController`의 `-sapphire-create-select=warrior`)은 검증 후 완전히 제거하고 `git diff` 0 확인, 최종 재빌드까지 재확인했다(영구 훅인 `-sapphire-scene=`/`-sapphire-account=`/`-sapphire-class=`는 그대로 유지).

**참고 (게임 코드 아님)**: 이번 세션에서 `SapphireSceneBuilder.BuildEverything()`이 VillageHub/SlimeKingdom 씬도 함께 재조립하면서 두 씬의 `.unity` 파일과 일부 미관련 텍스처 `.meta`(플랫폼별 오버라이드 스텁 자동 추가)가 큰 폭으로 diff에 포함됐다 - 이 프로젝트의 기존 관행(매 세션 전체 씬 재조립+커밋)과 일치하는 정상적인 부산물이다(과거 커밋 `ab85e1c`/`cc4b06d` 등도 동일 패턴).

## 2026-09-16: 캐릭터 접지 실측 재보정 + 스킬 이펙트 클리핑/알파결함 실측 + 스킬명 팝업 텍스트 제거

사용자 리포트 3건: (A) "캐릭터가 타일에 붙어있지 않고 공중에 떠 보인다 -
특히 오른쪽을 볼 때 심하다", (B) "스킬들이 뭔가 잘린다" + "스킬 배경에
눈금판(체커보드)이 같이 나온다", (C) "스킬 쓸 때 캐릭터 머리 위에 텍스트
나오는 것도 없애줘". 전부 PIL/scipy 실측(`tools/art_qa/`) 기반으로 조사·
수정했다. 근거·산출물 상세는 `docs/ASSET_STATUS.md` 2026-09-16 항목.

**(A) 원인**: `ArtImportConfigurator.BuildMageGridSlices`/
`WarriorArtImportConfigurator.BuildTopdownGridSlices`가 방향(행)마다 하나의
손으로 고른 pivot 상수를 12개 셀(4방향x3프레임) 전체에 똑같이 적용하고
있었다. 그 상수 자체가 예전에 한 번 측정한 naive alpha bbox 기반이었는데,
Right(오른쪽) 행의 bbox가 바로 아래 Up(위쪽) 행의 후드/머리카락 아트가 셀
경계를 살짝 넘어 그려진 파편(연결요소 분석으로 확인 - mage 229-334px,
warrior 175-203px, 본체 38,000-44,000px에 비하면 아주 작음)까지 포함해
측정됐던 것 - 그래서 "발 위치"가 실제보다 셀 맨 아래(0.00)로 잘못 계산돼
있었다(실제 발 위치는 mage 0.17, warrior 0.11 정도 위). 캐릭터는 항상 타일
CENTER에 배치되므로(`GridWorldConversion.GridToWorld`), pivot이 실제 발
위치보다 아래로 잡혀 있으면 렌더링된 발이 타일 중심보다 그만큼 위에
떠 보인다 - 사용자가 "오른쪽 볼 때 특히 심하다"고 한 것과 정확히 일치.

**(A) 수정**: ① 원본 PNG 정리 - `tools/art_qa/clean_character_sheets.py`로
각 셀의 가장 큰 alpha 연결요소만 남기고 나머지(인접 셀 침범 파편)를
투명 처리(mage 911px, warrior 1879px 제거, 본체 픽셀은 무변경). ②
`Domain/Character/CharacterFootPivotCalculator.cs`(신설, `VfxFramePivotCalculator`
패턴 참고하되 "발 위치"(alpha bbox 최하단) 기준으로 다르게 계산, 순수 C#이라
단위테스트 가능) + `Editor/CharacterGridSheetImporter.cs`(신설, mage/warrior
공용 - 이전엔 Mage `BuildMageGridSlices`와 Warrior `BuildTopdownGridSlices`가
거의 동일한 로직을 각자 유지하고 있던 것을 하나로 통합) - 12셀 전부 **프레임별
개별** pivot을 실제 정리된 PNG에서 직접 계산해 대입(행 단위 평균이 아니라
idle/walkA/walkB 각자 - 걷기 프레임끼리 발 위치가 흔들리면 애니메이션 중
들썩임으로 보일 수 있어서). 실측 결과 프레임 간 spread는 최대 0.83
percentage point로 무시할 수준(들썩임 없음 확인).

**(B) VFX 클리핑 실측**: `MageSkillVfxAtlas.png`(8x5, 5개 행 전부) +
`Warrior/WarriorSkillVfxAtlas.png`(8x5, 4개 행) - alpha>120(명백한 콘텐츠)
기준으로도 각각 40/40, 14/40 프레임이 셀 경계에 닿거나 넘어감. 확대
크롭으로 직접 확인한 결과 얼음창·번개 문양이 수평선으로 뭉텅 잘리는 등
하드 컷(옅은 잔광 번짐이 아님) - 소스에 남은 픽셀이 없어 패딩으로 복구 불가,
**재생성 필요** (이번엔 건드리지 않음, 상세는 ASSET_STATUS.md). 반면
`ManaShieldPadded.png`/`ThunderFieldPadded.png`/`Warrior/WarriorGroundSlamPadded.png`는
같은 기준 0-2/8 프레임만 1-2px 수준 - 크롭 확인 결과 이펙트 본체는 완전한
형태(옅은 글로우 번짐만 경계에 닿음)라 방치.

**(B) 알파채널 결함 (클리핑보다 심각, 확정)**: `MageDirectionalPadded.png`는
PNG 자체가 colortype=2(RGB, 알파 없음)이고 배경이 회색/흰색 체커보드
무늬로 그대로 구워져 있다(전체 배경 96/96 코너 샘플이 alpha=255 - 나머지
5개 VFX 텍스처는 전부 colortype=6(RGBA)에 배경 알파도 0에 가까워 정상).
실제 게임 스크린샷(`vfx_mage_icespike.png`, `vfx_mage_lightningspear.png`)으로
이 체커보드 사각형이 스킬 이펙트 배경에 그대로 렌더링되는 것을 확인 -
사용자가 말한 "스킬 배경에 눈금판이 같이 나온다"가 바로 이것이다. 이
파일이 텔레포트/블링크·고드름·번개창 3개 스킬의 VFX를 담당한다
(`SkillVfxImporter.ConfigureLibrary`의 frames[8..15]/[24..31]/[32..39]).
알파 데이터 자체가 없어 크롭/패딩으로 복구 불가 - **재생성 필요, 최우선**.
`wrapMode`(Clamp 여부)는 6개 텍스처 전부 강제 재수입으로 직접 확인한 결과
이미 기존 .meta에 Clamp로 저장돼 있어 클리핑의 원인이 아니었다 - 그래도
`SkillVfxImporter.cs`/`WarriorSkillVfxImporter.cs`에 `importer.wrapMode =
TextureWrapMode.Clamp`를 명시적으로 추가는 해뒀다(기존 동작 무변경, 방어적
개선).

**(C) 수정**: `SkillCastFeedback.cs`의 `SpawnLabel`/`RiseAndFade`(TextMesh로
캐릭터 머리 위에 스킬 이름을 띄우고 위로 상승·페이드시키던 로직) 완전
제거. 스프라이트 색상 플래시 피드백은 그대로 유지(사용자는 텍스트만
지적함). `PlayCast(skillName)`의 `skillName` 매개변수는 호출부
(`RadialSkillMenu`) 수정을 피하려 시그니처만 유지하고 내부에서 미사용.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 69/69 PASS(기존 64 +
`CharacterFootPivotCalculatorTests` 신규 5건). `SapphireSceneBuilder.BuildEverything`
-> `SapphireBuildPlayer.BuildWindows` 재빌드 성공. 임시 디버그 훅
(`PlayerGridController.DebugForceFacing/DebugForceWalking`,
`SceneComposer.ApplyDebugScreenshotArgs`가 파싱하는 `-sapphire-face=<dir>`/
`-sapphire-walk=<dir>`/`-sapphire-cast-skill=<index>`/`-sapphire-cast-basic`
커맨드라인 인자 - SendKeys 없이 스크린샷 검증용, LoginScreenController의
기존 `-sapphire-class=` 컨벤션과 동일한 패턴)으로 아래 스크린샷을 찍은 뒤
`git diff` 0 확인하고 완전히 제거(`SceneComposer.cs`/`PlayerGridController.cs`는
최종적으로 이 세션 시작 시점과 동일):
- `generated-images/diagnostics/ground_{mage,warrior}_{down,left,right,up}.png`
  (8장) - 4방향 전부 발이 타일 중앙(길 텍스처 중심)에 안정적으로 붙어있음을
  직접 눈으로 확인, 특히 right 방향이 다른 방향과 동일한 높이로 개선됨.
- `walk_mage_right_2.png`, `walk_warrior_right_1.png` - 걷기 애니메이션 중
  들썩임 없음 확인(PIL 프레임별 spread 실측과 일치).
- `vfx_warrior_whirlwind*.png`/`vfx_warrior_shieldblock.png`/
  `vfx_warrior_groundslam*.png`/`vfx_mage_icespike.png`/
  `vfx_mage_lightningspear.png` - warrior 3종은 클리핑이 뚜렷이 보이지
  않는 축소 렌더 크기에서도 정상 재생 확인(원본 픽셀 결함은 실측 수치가
  근거), mage IceSpike/LightningSpear 2종은 체커보드 배경 결함이 실제
  게임 화면에 그대로 나타나는 것을 확인(재생성 전까지는 계속 보임).
- `vfx_warrior_groundslam_notext.png`, `vfx_mage_shield_notext.png` -
  텍스트 팝업 제거 후 재확인, 스킬 이름 텍스트 더 이상 나타나지 않음.

## 2026-09-15: 스킬 캐스트 중 "캐릭터가 흔들린다" 버그 - 원인은 VFX 아틀라스 프레임별 알파 중심 불일치

사용자 리포트: "스킬들이 법사도 그렇고 전사도 그렇고 캐릭터가 정가운데 있어야하는데 엄청 흔들린다".

**원인 확정**: 캐릭터 자신의 `transform.position`은 스킬 캐스트 중 어떤 코드 경로도 건드리지
않는다(`SkillVfxPlayer`/`WarriorSkillVfxPlayer.Play`는 항상 새로 생성한 VFX GameObject의
transform만 움직이고, `GridMoveAnimator`/`PlayerGridController`는 이동 중에만 동작 - 코드
추적으로 확인, 이동 스킬(블링크/대시)의 1회 순간이동은 의도된 동작이지 버그 아님). 대신
`MageSkillVfxAtlas.png`/`WarriorSkillVfxAtlas.png`/`ManaShieldPadded.png`/
`ThunderFieldPadded.png`/`WarriorGroundSlamPadded.png` 5개 VFX 시트가 AI 생성 특성상
같은 행(row)의 8프레임끼리 그려진 내용(알파 bbox)이 프레임마다 셀 안에서 최대 47.5px(셀
폭 198px 대비 약 24%)까지 어긋나 있었다(PIL로 프레임별 alpha bbox center 실측,
`SkillVfxImporter`/`WarriorSkillVfxImporter`가 그동안 모든 프레임에 고정 pivot(0.5,0.5 또는
방향성 행은 0,0.5)을 부여해 슬라이스하고 있었던 것과 충돌). 캐스터에 고정된
transform.position에 고정 pivot으로 매 프레임을 렌더하니, 실제 그림 내용이 셀 안에서
제자리가 아닌 프레임이 나올 때마다 화면상 그 프레임만 위치가 튀어 보였다 - 이게 8~12fps로
반복되며 "캐릭터가 흔들린다"로 지각된 것. VFX가 캐릭터와 거의 같은 크기로 겹쳐 그려지는
지속형 자기 버프(마력쉴드/방패막기)에서 특히 두드러졌다.

**수정**: `Domain/Vfx/VfxFramePivotCalculator.cs`(신설, 엔진 비의존 순수 C#) -
프레임의 alpha bbox를 받아 그 bbox 자체를 pivot으로 계산(중심형 행은 bbox 중심, 방향성
행(창/슬래시/대시)은 bbox 좌측 끝 - 타일 수만큼 늘리는 localScale 확장이 그림이 실제로
시작하는 지점에서부터 자연스럽게 벌어지도록). `SkillVfxImporter.cs`/
`WarriorSkillVfxImporter.cs`가 `OnPreprocessTexture`에서 PNG 원본 바이트를 직접 읽어
(`ReadAlphaBytes`, 임포트 파이프라인이 아직 텍스처를 못 읽는 시점이라 별도 Texture2D로 디코드)
프레임마다 이 계산기로 pivot을 구해 `SpriteMetaData.pivot`에 대입 - 5개 텍스처 임포터 전부
동일 적용(Custom alignment로 통일, 이전엔 strip 텍스처 2종이 Center alignment였음). 기존
`.meta`가 이미 슬라이스 40/8개를 캐싱하고 있어 `ConfigureLibrary()`가 자동 재수입하지
않으므로, 임시 Editor 스크립트로 5개 텍스처를 `ImportAssetOptions.ForceUpdate`로 강제
재수입해 새 pivot을 `.meta`에 반영한 뒤 삭제했다(git diff로 pivot 값만 바뀌고 rect는 무변경
확인 - 예: ManaShieldPadded 프레임들 pivot이 (0.5,0.5) 고정에서 (0.498~0.524, 0.498~0.506)
범위로, WarriorSkillVfxAtlas BasicAttackSlash 행은 (0,0.5) 고정에서 프레임별 (0~0.27, 0.44~
0.45)로 변경).

**회귀 테스트**: `Tests/Domain/VfxFramePivotCalculatorTests.cs`(신설, 6건) - 순수 알고리즘
단위 테스트. `SkillVfxPlayer`/`WarriorSkillVfxPlayer`는 MonoBehaviour+코루틴이라 EditMode
`[Test]`에서 직접 호출 불가(Play 모드 밖 StartCoroutine은 예외)라 캐릭터 transform 불변을
직접 도는 테스트 대신, 실제 버그를 만든 계산 자체(프레임마다 pivot이 달라져야 함/빈 프레임
폴백/방향성 좌측단 앵커/텍스처 오프셋 무관)를 검증한다.

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 64/64 PASS(기존 58 + 신규 6). 임시
디버그 훅(`RadialSkillMenu`에 `-sapphire-cast-skill=<index>` 커맨드라인 인자로 스킬 자동
캐스트 - SendKeys 없이 스크린샷 검증용, 검증 후 완전히 제거해 git diff 0 확인)으로
`-sapphire-class=mage -sapphire-cast-skill=0`(마력쉴드)·`-sapphire-class=warrior
-sapphire-cast-skill=2`(방패막기) 각각 연속 캡처(`generated-images/diagnostics/
shake_mage_{1,2,3}.png`, `shake_warrior_{1..5}.png`) - 눈으로도 VFX가 캐릭터 중앙에
원형으로 고정돼 보이고, PIL로 두 프레임씩(0.25~0.3초 간격) ±6px 탐색범위 교차상관 분석한
결과 최적 정렬 오프셋이 둘 다 (dx=0, dy=0) - 프레임 간 차이는 전부 이펙트 자체의 반짝임
애니메이션이지 위치 이동이 아님을 정량 확인. `SapphireSceneBuilder.BuildEverything` ->
`SapphireBuildPlayer.BuildWindows` 재빌드 성공(디버그 훅 제거 후 최종 재검증까지 포함).

## 2026-09-15: 미사용 에셋/죽은 코드 정리

`client/Assets/Sapphire/` 전체를 대상으로 미사용 PNG 45개 전수 grep 감사 +
죽은 코드 감사를 수행했다. 상세 근거는 `docs/ASSET_STATUS.md` 같은 날짜
항목 참고.

**삭제**: `Art/VFX/ManaShieldTransparent-v2.png`, `Art/WorldRegionVillageGrassland-v1.png`
(각 +.meta) - 코드/씬/프리팹 참조 0건 확인 후 `git rm`.
`Domain/Skills/SkillRangeCalculator.IsWithinRange`(프로덕션 호출부 없음,
자기 테스트만 존재) + 전용 테스트 2건 제거.

**삭제하지 않음**(문서화된 기존 "참고용/향후 대비 보존" 결정 존중):
`Art/MageIdle.png` 등 Mage 원화 7종, `Art/UI/InventoryShopIcons.png` - 전부
grep상 미사용이지만 이전 세션이 이미 명시적으로 보존을 결정한 파일이라
이번 청소에서 임의로 뒤집지 않았다.

**검증**: 컴파일 0에러, EditMode 58/58(60에서 -2, IsWithinRange 테스트
제거분만큼 정확히 감소 - 회귀 아님), `SapphireSceneBuilder.BuildEverything`
-> `SapphireBuildPlayer.BuildWindows` 재빌드 성공, 스크린샷 4장
(`generated-images/diagnostics/cleanup_{login,select,village_mage,
village_warrior}.png`) 직접 확인 - 핑크 텍스처/빈 아이콘/깨진 캐릭터 없음.

**빌드 인프라 관찰 (게임 코드 아님, 참고)**: 같은 프로젝트에 대해 EditMode
테스트 실행 직후 곧바로(수 초 내) `-executeMethod`로 두 번째 batchmode
인스턴스를 띄우면, 첫 인스턴스 종료 직후의 ArtifactDB 상태 때문인지
Packages 전체(수천 개 파일)를 처음부터 다시 임포트하며 그 뒤로 아무 로그도
남기지 않고 무한정 CPU를 소모하며 멈추는 현상이 1회 관찰됐다(약 15분
대기 후 강제 종료). 동일 커맨드를 `-quit` 플래그를 명시하고 재시도하니
정상적으로 1분 이내 완료됐다 - 원인은 특정하지 못했으나(우연히 재현 안 될
수도 있음), batchmode 연속 실행 사이에 진행이 몇 분 이상 멈추면 프로세스를
강제 종료하고 `-quit`를 명시해 재시도하는 편이 안전하다.

## 2026-09-15: 젬리스 메이플스토리M풍 UI 킷 배선 + 전사 VFX 실제 스프라이트 배선

두 묶음을 한 세션에서 끝까지 배선했다. 상세 실측값·좌표계 결정 근거는
`docs/DECISIONS.md` 같은 날짜 항목, 에셋 목록·교체/신규/폐기 파일은
`docs/ASSET_STATUS.md` 같은 날짜 항목 참고 - 여기는 "지금 코드가 실제로
어떤 상태인가"만 요약한다.

**UI 킷 (Part A)**: `tools/ui_kit/build_ui_kit.py`가 생성한 15개 최종 PNG를
`client/Assets/Sapphire/Art/UI/`에 배선했다 - 9개는 기존 파일 교체
(MenuPanelOdin/MessagePanelFrameGold/CharacterSlotFrame/InputFieldFrame/
HealthBarFrameGold/GaugeFillMana/SkillButtonFrameGold/MovementStickGold/
TitleLogo), 6개는 신규 추가(MenuSectionDivider/RegionNameplate/
ButtonPrimary/ButtonSecondary/MenuHamburgerIcon/LevelBadgeHex). 관련 Editor
임포터: `ArtImportConfigurator.cs`(MessagePanelFrameGold/SkillButtonFrame/
HealthBarFrame 갱신 + `UiKitV3PixelsPerUnit` 상수 신설),
`HudArtImportConfigurator.cs`(MovementStick/MenuPanelOdin 갱신,
`ConfigureMenuSectionHeader` 제거하고 `ConfigureMenuSectionDivider`/
`ConfigureRegionNameplate`/`ConfigureButtons`/`ConfigureMenuHamburgerIcon`/
`ConfigureLevelBadge` 5개 신설), `CharacterFlowArtImportConfigurator.cs`
(border 상수 2개 갱신). `MenuSectionHeader.png`(+.meta)는 두 신규 자산이
기존 두 용도를 모두 대체해 참조 0건이 되어 `git rm`으로 삭제했다.

씬 빌더 변경: `VillageHubMenuBuilder.cs`(우상단 메뉴 버튼을 배경 없는
아이콘 단독으로, "캐릭터 선택으로" 풋터 버튼을 ButtonSecondary로,
섹션 헤더를 배너 대신 좌우 페이딩 디바이더+텍스트로 전면 재작성),
`VillageHubUiBuilder.cs`(HP/MP 게이지 barHeight 53.4->16 + Fill 앵커
재계산, 레벨 배지 신설, 지역명 배너를 RegionNameplate로 교체),
`LoginSceneBuilder.cs`/`CharacterCreateSceneBuilder.cs`(시작하기/생성
버튼을 ButtonPrimary로, "캐릭터 선택으로" 뒤로가기 버튼을
ButtonSecondary로).

**전사 VFX (Part B)**: `WarriorSkillVfxImporter.cs`(신설) +
`WarriorSkillVfxLibrary.cs`(신설, ScriptableObject)가
`WarriorSkillVfxAtlas.png`(8x5)와 `WarriorGroundSlamPadded.png`(8프레임
스트립)를 이름 기준으로 매핑해 슬라이스한다(아틀라스 행 순서와
`WarriorSkillVfxPlayer.Play(row,...)`의 파라미터 순서가 서로 달라 인덱스
매핑이 아니라 이름 매핑을 씀 - 두 클래스의 doc comment 참고).
`WarriorSkillVfxPlayer.cs`를 프로시저럴 프리미티브 도형에서 실제 아틀라스
프레임 재생(mage의 `SkillVfxPlayer`와 동일한 Create/Animate 패턴)으로
전면 재작성 - Dash/GroundSlam은 `Mathf.Atan2` 기반 facing 회전을 신규로
추가했다(GroundSlam은 이전엔 회전 없이 타일마다 마커를 찍는 방식이었음).
`Domain/Skills/WarriorCombatConstants.cs`(신설)가 전사 기본공격 사거리(2칸,
Line)를 담고, `WarriorSkillVfxPlayer.PlayBasicAttack`(신규 public 메서드,
SkillCatalog 밖의 별도 버튼이라 `Play(row,...)`가 아님)이 이를 재생한다.
`RadialSkillMenu.CastBasicAttack()`이 전사일 때만
`(skillVfx as WarriorSkillVfxPlayer)?.PlayBasicAttack(...)`을 호출하도록
배선 - 법사는 해당 캐스트가 자동으로 null이 되어 동작 무변경.
`SapphireSceneBuilder.BuildAll()`에 `WarriorSkillVfxImporter.ConfigureLibrary()`
호출을 추가했다(mage의 `SkillVfxImporter.ConfigureLibrary()`와 동일한
자리, 동일한 컨벤션).

**검증**: Unity CLI(6000.5.9f1) 컴파일 0에러, EditMode 60/60 PASS(신규
`WarriorBasicAttackRangeTests` 1건 - 사거리 상수값 + `TilesInLine` 결과
동시 검증), `SapphireSceneBuilder.BuildEverything` -> `SapphireBuildPlayer.
BuildWindows` 재빌드 성공. 스크린샷 12장
(`generated-images/diagnostics/final_login.png`, `final_select.png`,
`final_create.png`, `final_village_{mage,warrior}.png`,
`final_village_{mage,warrior}_menu.png`, `final_warrior_{basicattack,dash,
whirlwind,shield,warcry,groundslam}.png`)을 오케스트레이터가 직접 PNG로
열어 확인 - 보석/금테 잔존 없음, 조이스틱 노브 완전한 원, 메뉴 구분선에
꺾쇠 없음(페이딩 라인만), 6개 전사 스킬 VFX 전부 절차적 도형이 아닌 실제
스프라이트 프레임으로 렌더(회오리는 3x3 스월, 대지강타는 전방 3폭x1깊이
띠, 기본공격/돌진은 방향성 슬래시). 캐릭터 선택 화면의 클래스+레벨 텍스트
(`Lv.1` 회색조 저대비)는 이번 작업 범위 밖의 기존 결함으로 남아있음(수정
안 함, 후속 과제로 아래 표기).

**스크린샷 캡처 중 알아낸 사실 (검증 절차, 게임 코드 아님)**:
`logs/tools/capture.ps1`로 연속 캡처 시 첫 실행이 이전에 실행 흔적이 남은
상태에서 아주 드물게 엉뚱한(초소형, 160x28) 창을 잡는 현상이 관찰됐다 -
직전 `Get-Process -Name SapphireRPG | Stop-Process -Force`로 잔류
프로세스를 정리하고 재시도하면 항상 정상 크기(1296x759)로 캡처됐다(원인
특정은 못 함, 재현 스크립트 자체는 변경 안 함).

**다음**: `docs/DECISIONS.md`의 같은 날짜 항목(결정 2)에 따라
ButtonPrimary/Secondary는 이번에 지정된 4개 버튼에만 적용됐다 - 나머지
버튼(선택/삭제/+생성/닫기/확인/취소/메뉴 그리드 항목)을 신규 스타일로
통일할지는 후속 결정 필요. CharacterSelect 카드의 `Lv.1` 텍스트 저대비
문제는 2026-09-15 후속 세션에서 해결(`classLevelText`/`descriptionText`
색을 `#4a4038`로 변경, 아래 별도 항목 참고).

## 2026-09-15: 캐릭터 생성/선택 + 마을 메뉴 레이아웃 결함 4건 수정

오케스트레이터가 스크린샷 6장을 직접 보고 판정한 결함 4건을 수정했다.

- **D1 (VillageHubMenuBuilder.cs)**: "캐릭터 선택으로" footer 버튼이 시스템
  섹션의 "설정" 아이콘/라벨과 겹치던 문제. `OdinItemRowHeight`(104→78),
  `OdinSectionGap`(22→8) 등 그리드 상수를 실제 콘텐츠 높이에 맞게 축소하고,
  footer 버튼과 그리드 콘텐츠 사이 최소 12px 간격을 빌드타임에 검증하는
  `VerifyFooterClearance`를 추가했다(현재 수치로 약 21px 여유).
- **D2 (CharacterCreateSceneBuilder.cs / CharacterCreateController.cs)**: 클래스
  카드를 `CharacterSlotFrame.png`(캐릭터 선택 화면과 동일 프레임)로 교체하고,
  선택 상태를 굵은 테두리 색 대신 밝기(흰색/0.6 다크 틴트) + 1.04배 스케일 +
  은은한 골드 글로우로 표현했다(보석 장식 없음). 클래스 한 줄 설명을 카드에
  추가했고, 제목-카드-이름입력-생성버튼 스택을 16px 이상 간격으로 재배치,
  좌상단에 "캐릭터 선택으로" 뒤로가기 버튼을 추가했다.
- **D3 (CharacterSelectSceneBuilder.cs / CharacterSelectController.cs)**: 카드
  너비를 220→260(아트 임포트 시 계산된 실제 캘리브레이션 폭과 일치)으로
  넓혀 선택/삭제 버튼이 카드 테두리 안쪽(48px 인셋)에 들어가도록 했고, 이름을
  큰 글씨로 위에, "클래스 · Lv.N"을 작은 글씨로 아래에 두 줄로 표시하도록
  변경했다. 초상화도 120→150으로 확대했다. **부수 발견**: 이름 텍스트가 화면에
  전혀 안 보이던 원인은 레이아웃이 아니라 `fontSize=24`가 이 프로젝트
  전체에서 유일하게 그 크기를 쓰는 Text였던 것 - 새로 도입되는(첫 요청되는)
  폰트 크기의 동적 폰트 글리프가 legacy uGUI Text에서 렌더링되지 않는 버그를
  스크린샷 픽셀 비교로 실측 확인했다. 이미 여러 곳에서 쓰는 크기(22)로
  바꿔서 해결 - 근본 엔진 버그 자체는 별도 조사 과제로 남는다(TextMeshPro
  전환이 근본 해결책일 가능성).
- **공통 (LayoutOverlapGuard.cs, 신규)**: CharacterCreate/CharacterSelect의
  주요 요소(제목/카드/입력칸/버튼)가 서로 겹치면 빌드가 실패하도록 하는
  `VerifyNoOverlap`/`VerifyContained` 헬퍼. VillageHub 메뉴 패널은 좌표계가
  달라(패널 기준 vs 캔버스 기준) 별도의 인라인 산술 검증(`VerifyFooterClearance`)을
  그대로 둔다(지역성 우선 컨벤션).

컴파일 0 에러, EditMode 59/59 통과, `SapphireSceneBuilder.BuildEverything` +
`SapphireBuildPlayer.BuildWindows` 재실행, 새 계정으로 캡처한
`generated-images/diagnostics/flow2_{select,create,village_warrior_menu}.png`
3장으로 결함 해소를 직접 확인했다. 캡처엔 기존 `sapphiretest` 테스트
계정(테스트법사/테스트전사, 실사용자 데이터 아님)을 재사용했다.

## 2026-09-15: 캐릭터 플로우 실측 확정 + 씬 빌드 + 플레이어 빌드 + 스크린샷 검증

이전 세션이 코드만 구현해두고(컴파일/EditMode만 확인) 아트가 없어 미룬 부분 -
아트 도착 후 실측·빌드·스크린샷 검증까지 마무리했다.

**실측 확정값** (전부 PIL/numpy, `Read`로 눈으로 보고 판단하지 않음 - 이 문서 상시 원칙):

- `CharacterFlowArtImportConfigurator.cs`: `CharacterSlotFrame.png`(793x1983)
  border 40%-70% 구간 mode 샘플링으로 `Vector4(36,31,36,32)`(9개 샘플 전부 1px
  이내 안정). `InputFieldFrame.png`(2170x725) border `Vector4(36,60,35,54)`
  (top은 21개 샘플 전부 54로 완전 안정, left/right는 코너 장식 오염 일부 샘플
  제외 후 mode).
- `WarriorArtImportConfigurator.cs` 행별 pivot: Mage와 동일한 방법(3포즈
  alpha bbox 평균)으로 재측정 - Down(0.56,0.00) Left(0.55,0.02)
  Right(0.48,0.00) Up(0.51,0.19). Mage와 달리 Left/Right를 강제로 같은 값으로
  묶지 않음 - 워리어 원화 자체가 비대칭(Left 컨텐츠 h=351-354px로 셀 상단까지
  닿음, Right는 h=328px+상단 여백 33px)이라 각 행 실측값을 그대로 쓰는 게 발
  위치를 더 정확히 맞춘다.
- `WarriorSkillIconsSetGold.png`(1536x1024, 3x2 그리드) 아이콘 6개를 등분할
  512x512 셀에서 각 셀 자체 alpha bbox로 재크롭(예: BasicAttack
  Rect(43,513,461,467), GroundSlam Rect(1033,53,476,459) 등, 상세는 코드
  주석). 시각 확인 결과 대검베기=검베기 이펙트, 돌진=화살표, 회오리베기=쌍날,
  방패막기=방패, 전쟁함성=포효하는 사자머리, 대지강타=바위 뚫는 건틀릿 - 라벨과
  아트가 정확히 일치. 전부 게임 전체와 통일된 블루+골드 크리스탈 톤이라
  스크린샷에서 "전사인데 얼음같다"로 오인하기 쉬우나 의도된 통일 아트 스타일이지
  결함이 아니다.
- `HudArtImportConfigurator.ConfigureMenuSectionHeader`: 기존 crop
  `(17,306,2138,145)` / border `(232,23,231,26)`를 이 파일 자신이 명시한
  측정법(행 alpha 밀도 90% 초과 구간)으로 재실측한 결과 실제와 불일치함을
  발견 - 진짜 dense band는 rows 287-412(height 126, 기존은 273-418로 위 14px/
  아래 6px 여백 포함), 진짜 x bbox는 [9,2161](width 2153, 기존 17-2154는 좌우
  각 ~8px/~5px 잘림 - 작업 지시서가 예상한 "좌우 끝 9px 잘림"과 일치). Border는
  40%-70% 구간 11샘플 median으로 `(53,23,52,23)` - 기존 232/231은 실제 골드
  장식이 x=53-56에서 이미 flat navy로 안정되는데도 그 4배 이상 큰 값이었다
  (400폭 호출부 기준 43%를 불필요하게 고정 영역으로 낭비). `MenuButtonGold.png`
  border는 재실측(캡슐 양끝 라운드캡이 안정되는 x≈90-106) 결과 기존
  `(114,71,116,77)`이 다소 보수적이지만 시각적 왜곡을 일으키는 방향(과소)이
  아니라 안전한 방향(과대)이라 결함 아님 - 변경 안 함.
- `VillageHubUiBuilder.BuildRegionNameBanner`의 `bannerHeight=47.3` 주석이
  참조하던 crop(2138x281)은 실제로 존재한 적 없는 stale 값이었다 - 값 자체는
  안 바꿈(Image.Type.Sliced라 종횡비 불일치가 찌그러짐을 유발하지 않음),
  주석만 정정.

**개발 인자 추가** (`LoginScreenController.cs`, Login이 항상 씬 인덱스 0이라
플레이어 빌드의 커맨드라인 가로채기 지점): `-sapphire-scene=<SceneName>`
(임의 씬으로 직행), `-sapphire-account=<id>`(세션에 계정 로그인 - 두 인자는
조합 가능, 계정을 먼저 로그인시킨 뒤 씬 이동). 기존 `-sapphire-class`는
그대로 유지.

**씬 빌드 통합**: `SapphireSceneBuilder.BuildEverything()` 신설 - `BuildAll()`
(VillageHub) 후 `CharacterFlowSceneBuilder.BuildAll()`(Login/CharacterSelect/
CharacterCreate)을 이어 호출하고, `BuildSettingsSceneRegistrar.ReorderScenes`
(신설)로 Build Settings 순서를 Login/CharacterSelect/CharacterCreate/
VillageHub로 강제한다 - 기존 `Register`/`RegisterFirst`는 이미 리스트에 있는
항목을 재정렬하지 않아 두 독립 빌더를 어떤 순서로 실행해도 이 정확한 순서가
보장되지 않았다.

**검증**: Unity 6000.5.9f1 batchmode, 컴파일 0에러, EditMode 59/59 PASS
(`WarriorSkillRangeTests` 5건 포함 - Whirlwind의 `TilesInRing`/GroundSlam의
`TilesInFrontCone`/Dash의 `GridMover.TryBlink` 전부 실제 도메인 함수를
호출하는 것을 코드 추적으로 확인: `RadialSkillMenu.CastSkill` ->
`WarriorSkillVfxPlayer.Play(row,...)` -> `PlayWhirlwind`/`PlayGroundSlam`가
`SkillRangeCalculator`를 직접 호출 - 이 슬라이스엔 쿨다운 시스템 자체가 없어
"쿨다운 호출" 검증은 해당 없음). `SapphireSceneBuilder.BuildEverything` ->
`SapphireBuildPlayer.BuildWindows` 순으로 재빌드 성공.

스크린샷 6장(`generated-images/diagnostics/flow_*.png`) 전부 오케스트레이터가
PNG를 직접 열어 확인 - 핑크 텍스처·빈 화면·겹침·잘림·한글 깨짐 없음. 로그인
배경/로고/입력창/버튼, 캐릭터선택 4슬롯(법사+전사 채움, 빈 슬롯 2개, "선택"/
"삭제"/"+생성" 라벨 정상), 캐릭터생성(클래스 카드+이름입력+생성 버튼),
마을(전사/법사 둘 다 발이 바닥 타일에 정확히 붙어 서있음 - 워리어 pivot
재측정이 실제로 통함), 메뉴 패널(성장/모험/시스템 섹션 헤더 텍스트가 헤더
띠 중앙에 위치 - MenuSectionHeader crop 수정이 실제로 통함, 4열 그리드에서
"캐릭터정보" 라벨 안 잘림) 전부 육안 결함 없음.

**스크린샷 캡처 도구 버그 수정** (`logs/tools/capture.ps1`, 이 레포 도구
스크립트, 게임 코드 아님): 두 가지 결함을 고쳤다 -
(1) `-ExeArgs ""` 빈 문자열을 그대로 `Start-Process -ArgumentList`에 넘기면
PowerShell이 예외를 던지던 것을 빈 값이면 `-ArgumentList` 자체를 생략하도록
가드. (2) 게임 창이 이전 세션에서 기억한 화면 밖(예: L=-1927, 주 모니터
음수 좌표) 위치로 뜨는 경우 `CopyFromScreen`이 그 좌표의 실제 데스크톱 다른
창(사용자가 쓰던 브라우저/메신저 등, 진단 스크린샷으로 확인)을 캡처해버리는
결함 발견 - `SetForegroundWindow`가 Windows 포커스 도용 방지로 조용히
실패(반환값 False)하는 것도 원인 중 하나였다. `SetWindowPos`로 z-order를
`HWND_TOPMOST`로 강제한 뒤 캡처하고, 캡처 직후 `HWND_NOTOPMOST`로 되돌리는
방식으로 해결(신설 `logs/tools/diag_capture.ps1`로 먼저 원인 진단).

**테스트 계정 시드**: `%LocalAppData%Low\Sapphire Studio\Sapphire RPG\
characters_sapphiretest.json`에 법사+전사 1개씩 수동 시드(실제 유저 데이터
없음 확인 후 별도 id 사용) - `CharacterRosterFileRepository`가 읽는 그대로의
포맷.

## 2026-09-15: 잔여 2건 수정 - 바닥 타일 seam, 메뉴 그리드 여백/라벨 잘림

`generated-images/diagnostics/final2_1280_*.png`를 오케스트레이터가 직접 검수해 남은 2건만 지정.

1. **바닥 타일 경계 1px 어두운 선 (S1)**: 원인은 6개 지면 셀이 `GroundTiles.png` 한 장의
   아틀라스에서 bilinear + Max Size 256 다운스케일로 슬라이스되어, 셀 가장자리 샘플이
   아틀라스 안의 인접 셀 텍스셀을 끌어오던 것(atlas bleed). PIL로 `GroundTiles.png`(1536x1024,
   3x2)를 개별 512x512 텍스처 6장(`Art/World/Ground/{Grass,Dirt}_{0,1,2}.png`)으로 분리하고
   각각 Sprite/Single + wrapMode Clamp(아틀라스가 없으므로 번질 인접 셀 자체가 없음) +
   mipmap 끔 + Max Size 256 + FullRect 메시로 재수입, PPU를 512가 아니라 508로 설정해 타일이
   1.008 unit로 살짝 커지게 해 타일 사이 미세 서브픽셀 틈도 함께 덮었다
   (`ArtImportConfigurator.ConfigureGroundAtlas`/`ConfigureGroundTileSprite`).
   `VillageHubTerrainBuilder.CreateGroundTile`은 이제 아틀라스 내 이름 검색 대신 표준
   `AssetDatabase.LoadAssetAtPath<Sprite>`로 직접 로드한다. 기존 `GroundTiles.png`(+.meta)는
   참조 0건 확인 후 `git rm`.
2. **메뉴 그리드 여백/라벨 잘림 (S2)**: `VillageHubMenuBuilder`의 `OdinContentMargin`이 고정
   24유닛이라 `MenuPanelOdin.png`의 실제 9-slice 좌우 보더(84px, PPU 100*793/400 환산 시 캔버스
   ~42.37유닛)보다 좁아 1열 아이콘이 금테에 붙고 "캐릭터정보"가 4열에서 잘렸다.
   `OdinContentMargin`을 상수가 아니라 `보더px / (spritePPU/100) + 14` 계산값(≈56.37)으로
   변경 - 좌우 보더를 그대로 참조하되 14유닛 여유를 더함(HudArtImportConfigurator의 실제 import
   값과 이름 상수로 동기화). 셀 폭은 (400-2*56.37)/4 ≈ 71.81로 재계산. 라벨 Text는
   `horizontalOverflow=Wrap`(기존 기본값 Overflow였던 게 BestFit이 가로로 전혀 안 줄어들던
   원인), `verticalOverflow=Truncate`, `resizeTextForBestFit`(9~13), rect 폭 cellWidth-6(≈65.81)/
   높이 18로 재설정.

전부 Unity CLI 컴파일 0에러, EditMode 테스트 33/33 PASS, `SapphireSceneBuilder.BuildAll` 재실행 후
씬 YAML 직접 파싱(6개 Tile 에셋이 새 텍스처 GUID 6종만 참조, 라벨 Text의
`m_HorizontalOverflow=0`/`m_VerticalOverflow=0`/`m_BestFit=1`/`m_MinSize=9`/`m_MaxSize=13`/
`m_SizeDelta={65.81463, 18}` 확인, 1열 아이템 `m_AnchoredPosition.x=92.278`로 좌측 보더에서
충분히 이격), `SapphireBuildPlayer.BuildWindows` 재빌드 후 스크린샷 재검증
(`generated-images/diagnostics/final3_1280_{default,menu}.png`)까지 확인.

## 2026-09-16: 오케스트레이터 시각 QA 불합격 항목 7건 수정

`generated-images/diagnostics/final_1280_*.png` 스크린샷을 오케스트레이터(Opus)가 직접 보고
불합격 판정한 항목을 전부 수정했다.

1. **바닥 타일 구멍 (F1, 원인 확정)**: `VillageHubTerrainBuilder.GetFlipMatrix`가 스케일
   -1 뒤집기에 `Matrix4x4.TRS(translate=(flipX?1:0,...), ...)`로 +1칸 이동을 더하고 있었다 -
   지면 타일 스프라이트 pivot이 중앙(0.5,0.5)이라 이 이동이 불필요했고, 뒤집힌 타일이 옆
   칸으로 밀려나면서 원래 칸이 비어(스카이박스 노출) 구멍처럼 보이고 흙길이 지그재그로
   어긋났다. `Matrix4x4.Scale(scale)`로 이동 항을 제거해 제자리 뒤집기로 수정.
2. **카메라 배경**: Main Camera에 `ClearFlags.SolidColor` + `(0.06,0.07,0.10)` 적용 -
   스카이박스가 향후 유사한 구멍을 가리지 못하게.
3. **스킬 부채꼴 (F3)**: `VillageHubSkillMenuBuilder`를 "4개 부채꼴 + 5번째 분리 배치"에서
   "5개 전부 한 부채꼴"로 재작성 - 반지름 172, 각도 80°~200°(4×30° 간격), 기본공격 중심을
   화면 우하단 코너에서 (-112,+118)로 재배치. 인접 버튼 중심거리 88.98 > 80(지름) 유지,
   화면 이탈 없음(우측 여백 42.1, 하단 여백 19.2) - `VerifyNoOverlap`/`VerifyOnScreen`이
   빌드 시점에 이 값을 재검증한다.
4. **스킬 아이콘 매핑 (F4)**: `SkillCatalog.cs`에서 낙뢰=눈꽃, 고드름=크리스탈로 잘못
   매핑돼 있던 것을 낙뢰→`SkillIcons_Haste`(번개), 고드름→`SkillIcons_FrostWave`(눈꽃),
   번개창→`SkillIcons_ArcaneBolt`(크리스탈)로 정정. 스프라이트 이름 자체는 변경 없음.
5. **죽은 코드 삭제 (F5)**: 카탈로그에서 이미 빠진 "질주" 스킬의 잔재 -
   `GridMoveAnimator.ActivateSpeedBoost`/`IsSpeedBoosted`/`boostedMoveDuration`/관련 코루틴,
   `SkillCatalog.HasteSkillId` 상수 제거 (grep으로 참조 0건 확인 후 삭제, 테스트 영향 없음).
6. **메뉴 패널 (F6)**: (a) 메뉴 버튼이 열린 패널에 가려지던 문제 - 패널 top 오프셋을
   24→104(버튼 하단 92 + 여유 12)로, bottom 여백을 24→20으로 조정. (b) 패널 뒤에 전체화면
   반투명(alpha 0.45) 백드롭을 추가해 HUD 비침·클릭 새어나감을 차단, 백드롭 클릭 시 메뉴
   닫힘, 패널과 하나의 `MenuOverlay` 루트로 함께 토글. (c) `MenuPanelOdin.png` 인테리어
   알파를 200→238로 상향(원본은
   `generated-images/menu-odin/MenuPanelOdin_alpha200_backup.png`에 백업). (d)
   `MenuSectionHeader.png`의 스프라이트 rect가 장식 끝단의 전체 높이(197~477, 281px)에
   맞춰져 있어 중앙부에서는 실제 밴드 앞뒤로 투명 여백이 76px/60px씩 남아 텍스트가 밴드
   상단에 걸렸던 문제 - 폭 전체의 90% 이상이 불투명한 행 구간(273~417, 145px)으로 재크롭,
   9-slice 보더도 그 안에서 재실측(232,23,231,26), 헤더 높이 36→40. (e) 메뉴 항목 라벨에
   `resizeTextForBestFit`(10~14), 폭을 셀폭-4로 설정해 "캐릭터정보" 잘림 방지.
7. **MP 게이지 (F7)**: 내장 흰 스프라이트 틴트 대신, `HealthBarFrameGold.png`의 HP 필 셀을
   PIL로 잘라 색상(hue)만 빨강→사파이어 블루(215°)로 회전(명도·채도·알파 유지)한
   `Art/UI/GaugeFillMana.png`를 생성해 MP 필로 교체 (`Image.color`는 흰색).

전부 Unity CLI 컴파일 0에러 + EditMode 테스트 전체 PASS + `SapphireSceneBuilder.BuildAll`
재실행(씬 YAML로 좌표 재검증) + `SapphireBuildPlayer.BuildWindows` 확인 후 커밋.

## 2026-09-15: REMEDIATION_PLAN.md Phase 2(HUD 재배선) + Phase 3(오딘식 메뉴) 완료

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
