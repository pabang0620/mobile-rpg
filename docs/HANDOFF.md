# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향, 이 문서와 같은 날 갱신). 2026-09-14 기준 현재 코드 상태 재정리.

이 문서는 예전 `docs/PARALLEL_CONTRACT.md`(Codex 병렬 에이전트 모듈 계약)와 `docs/COMBAT_HANDOFF.md`(전투 모듈 구현 인계)의 내용 중 지금도 유효한 부분을 흡수해 대체한다. 두 파일은 삭제했다 - Codex 병렬 에이전트 개발 방식 자체가 폐기되어 원래 목적(에이전트 간 API 계약 조율)이 없어졌기 때문이다.

## 확정된 기술 방향과 코드의 격차 (가장 중요)

아래 코드는 격자 스냅 이동/Pixel Perfect Camera/에셋 하이브리드 전략이 확정(2026-09-14, `docs/DECISIONS.md`)되기 **이전**인 2026-09-13 세션의 산출물이다. 방향 확정 문서 작업만으로 코드가 바뀐 것은 아니다.

| 영역 | 확정된 방향 | 현재 코드 상태 |
|---|---|---|
| 필드/던전 전투 중 이동 | 격자 1칸 스냅 + 짧은 tween | `Domain/Combat/CombatWorld`: 연속 XY 자유 이동(8방향 아날로그, `Vec2` 기반) - **미마이그레이션** |
| 마을/탐험 이동 | 격자 1칸 스냅 + 짧은 tween | `Domain/World/TileWorld`: 이미 타일 좌표(`TileCoord`) 기반. 다만 즉시 스냅이라 tween 없음 - **부분 일치** |
| 화면 스케일링 | Unity 공식 `com.unity.2d.pixel-perfect`, 타일당 약 20px | 자체 제작 `Presentation/Exploration/PixelCameraFollow.cs`(`pixelsPerUnit=16`) - **미마이그레이션**, 패키지 자체가 manifest에 없음 |
| 에셋 전략 | 무료 CC0/CC-BY 팩 + AI 생성 하이브리드 | 전량 AI 생성/원본 참고, `CREDITS.md` 없음 - **미마이그레이션** (`docs/ASSET_STATUS.md` 참고) |

## 현재 코드 실측 상태 (2026-09-13 세션 산출물)

### Domain/Combat

60Hz 고정 tick, `Vec2` 이동, 경계/장애물 충돌, 대각 이동 정규화, `.25`단위 격자 A* 경로탐색, 기본 3연계 마법탄, 투사체(스윕 판정, 벽 차단), 스킬 4종(비전탄/서리 파동/점멸/보호막), 회피/무적, 근접(고블린)/원거리(정령)/돌진(늑대) 3종 일반 AI, 정예 변형, 2페이즈 보스(범위 공격/돌진/소환), 단일 처치 이벤트(`Killed`는 고유 `Id`로 한 번만), 필드 한정 AUTO.

`Attack`/`Cast(0..3)`/`Dodge` 명령은 거절 시 자원을 소비하지 않는다. 동일 tick에 플레이어와 보스가 함께 사망하면 플레이어 사망이 우선(던전 실패 처리). `SetStats`는 최대 HP 증감분만큼 현재 HP를 보존하거나, 요청 시 완전 회복한다. `EnterZone`은 적/투사체/이벤트 큐/공격 상태/AUTO를 초기화하되 쿨다운은 이동 구간을 넘어 유지한다.

알려진 한계(2026-09-13 시점, 아직 해소 안 됨): 액터 간 분리(separation) 스티어링이 없어 적 스프라이트가 겹칠 수 있음, 투사체는 고정 원형 충돌만 지원, 대시 텔레그래프는 원형만으로는 방향을 정확히 표현하지 못해 방향/거리 기반 렌더링이 필요, 애니메이션 보간은 Presentation 책임, 모바일 밸런스 미검증.

### Domain/World (TileWorld)

마을/집/숲길 진입로를 `TileCoord` 격자로 관리한다. `Move(Direction)`은 벽/NPC 충돌을 검사하고 워프(맵 전환) 지점을 지나면 자동 전환한다. 즉시 스냅이며 애니메이션 보간은 없다. 격자 스냅 이동 방향과 개념적으로는 가장 가깝지만, 확정된 짧은 tween(0.05~0.1초)은 아직 적용되지 않았다.

### Domain/Campaign + Infrastructure

프로필, 장비/스탯/강화/판매, 물약 구매/소비, 퀘스트 6개, XP/레벨, 필드 보상 즉시 확정, 던전 보상은 원장에 임시 기록 후 성공 시에만 확정, 체크섬 저장/백업/중복 거래 방지(`transactionId` 재시도 시 동일 결과 반환).

### Application (`GameApp` 파사드)

Presentation은 `GameApp.Instance` 하나만 참조하고 Domain을 직접 참조하지 않는다. 실제 코드(`client/Assets/Sapphire/Application/GameApp.cs`) 기준 공개 표면 요약:

- 상태 필드: `Mode`("Title"/"Town"/"Field"/"Explore"/"Dungeon"/"Result"), `ZoneName`, `Objective`, `Notice`, `SaveStatus`, `Level`/`Hp`/`MaxHp`/`Mp`/`MaxMp`/`Gold`/`Xp`/`NextXp`/`HpPotions`/`MpPotions`, `Paused`/`Auto`/`Dead`/`CanContinue`, `PlayerX`/`PlayerY`/`FacingX`/`FacingY`, `Cooldowns`/`MaxCooldowns`/`SkillUnlocked`, `Actors`/`Projectiles`/`Effects`/`Inventory`/`Quests` 리스트, `Exploration`(`TileWorld`, 탐험 모드일 때만).
- 명령 메서드: `NewGame()`, `ContinueGame()`, `Move(float x, float y)`, `Attack()`, `Cast(int slot)`, `Dodge()`, `UsePotion(bool hp)`, `ToggleAuto()`, `Interact()`, `Travel(int destination)`, `ReturnTown()`, `Respawn()`, `SetPaused(bool)`, `Equip`/`Upgrade`/`Sell(string instanceId)`, `BuyPotion(bool hp)`, `AcceptQuest()`, `TurnInQuest()`, `SaveNow()`, `SetVolumes(float, float)`.
- `Move(float x, float y)`는 전투 중엔 `CombatWorld.Step(Vec2)`로 연속 이동에, 탐험 중엔 내부에서 4방향으로 양자화해 `TileWorld.Move(Direction)`로 라우팅된다. **격자 스냅 이동으로 전환하면 이 시그니처와 라우팅 방식을 다시 검토해야 한다** - 특히 전투 중 이동을 `Move(float x,float y)` 그대로 둘지, `Move(Direction)` 계열로 통일할지가 마이그레이션의 핵심 결정 포인트다.

### Presentation

카메라(월드 렌더용 + `PixelCameraFollow` 탐험용), HP/MP/XP, 보스/텔레그래프/투사체 표시, 터치/키보드 입력, 마을 서비스 UI, 인벤토리/장비/퀘스트/상점/설정/결과 UI, safe area 대응.

### Editor/BuildGame

에셋 임포트, 규칙 검사, Boot 씬 생성, Windows 빌드(`Sapphire.Editor.BuildGame.BuildAndTest` → `client/builds/Windows/SapphireRPG.exe`).

## 검증 결과 (2026-09-13 세션, 아직 유효)

- PASS: 순수 C# 도메인 규칙 테스트(`CombatChecks.RunAll`, `CampaignChecks.RunAll`) - 대각 이동 정규화, 경계, 점멸 벽 차단, 자원/쿨다운 거절, 보호막, 단일 처치, 투사체 벽 차단, 회피 경계, 예고, 보스 2페이즈, A*, 저장 실패 롤백, idempotency, 던전 성공/실패/재로드, 손상 백업, schema 거절 등.
- PASS: Mono/.NET Framework 컴파일러로 Domain+GameApp 컴파일, GameApp smoke(타이틀→새 게임/마을→필드→귀환→던전 포기→귀환→저장).
- BLOCKED: Unity Editor/Player 실행. `No valid Unity Editor license found`, 종료코드 198 - 이 환경은 라이선스가 활성화되지 않았다. PLAYMODE/PLAYER/DEVICE 검증은 사용자가 라이선스 활성화 후 직접 수행해야 한다.

## 다음 실행

1. **전투 이동 마이그레이션**: `Domain/Combat/CombatWorld`의 연속 XY 이동을 격자 스냅 이동으로 교체. `GameApp.Move` 시그니처/라우팅 재검토가 선행 과제.
2. **카메라 마이그레이션**: `com.unity.2d.pixel-perfect` 패키지 도입, `assetsPPU`≈20/참고 해상도 720x1280 기준으로 설정, 자체 제작 `PixelCameraFollow`는 대체 또는 정리.
3. **탐험 이동 다듬기**: `TileWorld.Move`의 즉시 스냅에 짧은 tween(0.05~0.1초) 추가.
4. **에셋 하이브리드 전략 적용**: `docs/ASSET_STATUS.md`의 다음 작업 참고.
5. Unity 라이선스 활성화 후 실제 Player 빌드/플레이 검증(사용자 직접) - 16:9/20:9/4:3 UI, 모바일 멀티터치, AUTO 장시간 실행, 대상 Android 기기 성능.
6. `lh2d-*` 에이전트 파일/심볼릭 링크 정비(`AGENTS.md`에 현황 기록, 이번 재작성 범위 밖).

각 항목은 `lh2d-module-planner`로 설계를 먼저 고정한 뒤 진행한다(`AGENTS.md` 참고).
