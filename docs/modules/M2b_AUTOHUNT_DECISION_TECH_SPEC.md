# M2b 자동사냥 의사결정 기술 명세

상태: 구현 전 설계 고정안(부모 결정 반영판). 현재 `BattleSession.Tick`에 내장된 "단순 추적형"(가까운 적 방향 이동 → 사거리 진입 시 스킬/기본공격 고정 순서)을 대체한다. 이 모듈은 새 에셋을 요구하지 않는 순수 로직이며 M1(전투감) 에셋 도착과 무관하게 착수할 수 있다.

## 명명 확정

`docs/modules/M2_COMBAT_HUD_REDESIGN.md`가 이미 "M2" 접두를 다른 범위(HUD 개편)에 쓰고 있어, `MODULE_ROADMAP.md`의 "M2 자동사냥 의사결정"을 다루는 이 문서는 접미 `b`를 붙여 `M2b_AUTOHUNT_DECISION_TECH_SPEC.md`로 확정했다(부모 결정 1). `MODULE_ROADMAP.md`의 M2 절에 이 파일을 가리키는 링크를 추가했다. 기존에는 이 파일을 참조하는 다른 문서가 없었다(신규 작성 직후였음, 확인 완료).

## 플레이 결과 (UX)

AUTO가 켜져 있으면 플레이어 개입 없이 적을 찾아 접근하고, 사거리 안에 들어오면 즉시 교전하며, 위험 예고 중에는 피하고, MP를 아껴 쓰고, 체력이 위험 수준으로 떨어지면 물약으로 자가 회복하며 전투를 지속한다. 수동 이동/공격/스킬/구르기/점프/물약 입력이 들어오는 즉시 AUTO는 그 입력 결과에 개입하지 않고 3초간 완전히 물러난다(기존 `Manual()`/`ManualUntil` 규칙, 변경 없음). 사거리 안에 목표가 없고 2초 넘게 상황이 나아지지 않으면 다른 대상을 다시 찾고, 5초 넘게 나아지지 않으면 안전 지점으로 돌아간다 — 단, 돌아가는 도중에도 사거리 안에 적이 들어오면 즉시 교전한다(무조건 안전지점까지 무시하고 걷지 않음). 화면·연출·수치는 이전과 동일하게 보인다.

## 범위

포함: AUTO 의사결정(대상·이동 방향·스킬/기본공격·구르기·체력물약 의도), 우선순위 규칙, 짧은 상태 머신, 0.2초 판단 주기, 2초 재탐색, 5초 복귀, 수동 유예 3초(기존 규칙 재확인), 위험 회피, 저체력 자동 물약(기존 물약 시스템 그대로 재사용), MP 절약 설명 가능성, 결정 사유 로그, 타겟 선택 우선순위.

제외: 실제 피해 적용·MP 소비·쿨다운/수량 갱신(기존 `BattleSession`이 계속 소유), MP 물약 자동 사용(부모 결정 4는 HP 물약만 명시 — 범위 확장 아님, 아래 참조), 점프를 이용한 발판 간 추적(현재 단순 추적형도 하지 않음, 범위 확장 아님), 애니메이션·판정·보상·화면(M1/M0 소유 유지).

## 소유 경계

- **AutoHuntDirector(신규, Domain)** 소유: 판단 주기 누적기, 상태 머신, 무진행 타이머, 대상 락온/블랙리스트, 안전 지점 좌표, 결정 사유 문자열. **UnityEngine.UI 참조 금지**, `BattleSession`의 필드를 직접 대입하지 않는다.
- **BattleSession(기존)** 소유 유지: 위치, 이동 적용, HP/MP, 쿨다운, 비용, 명중 판정, 보상(Kills/Xp/Gold/Level), `Auto`/`ManualUntil` 값 자체, 입력 게이트(`Attacking`, `Grounded`, `DodgeCooldown` 등), 물약 수량/회복량/쿨다운 규칙. Director는 이 값들을 **읽기만** 하고, 실제 적용은 반드시 `BattleSession`의 기존 공개 메서드(`Attack`, `Dodge`, `UseHpPotion`, 이동)를 통해서만 요청한다.
- **BattleScreen(기존)** 변경 없음: AUTO 램프 버튼과 `T` 키는 그대로 `battle.ToggleAuto()`를 호출한다. 새 UI 배선이 없다.

## 입력 (AutoHuntSnapshot, 매 판단 tick마다 BattleSession이 구성해 넘김)

- `HeroPosition`, `HeroFacing`, `Grounded`
- `Hp`, `MaxHp`, `Mp`, `HpPotions`, `Invulnerable`(현재 시각과 비교), `ShieldUntil`, `DodgeCooldown`
- `Enemies[]`: `Id`, `Position`, `Hp`, `MaxHp`, `Dead`, `Windup`(위험 예고), `Facing`
- `Cooldowns[4]`, `Costs[4]`, `MaxCooldowns[4]`
- `Time`(도메인 시뮬레이션 시계, `Unity Time.deltaTime` 아님 — 결정성 확보)
- `Auto`, `ManualUntil`
- `MovableRangeMinX/MaxX` = 기존 `ClampHorizontal` 상수(75~1205) 그대로 참조, 별도 계산 없음
- `ProgressSincePreviousDecision`: BattleSession이 계산해 넘기는 bool. 직전 판단 시각 이후 (a) `Impacts` 리스트에 `Hero==false`(적이 맞음) 항목이 새로 추가됐거나 (b) `HeroPosition.x`가 4px 이상 변했거나 (c) `SafeAnchor` 반경에 도달했으면 true.

Director는 이 snapshot 밖의 어떤 `BattleSession` 필드도 참조하지 않는다.

## 타겟 선택 우선순위 (부모 결정 2 — 확정)

매 판단 tick마다 다음 순서로 정확히 하나의 `TargetId`를 고른다. "사거리"는 `BattleSession.Attack()`이 이미 쓰는 390px 게이트(스킬 -1/0/1 공통, skill<2 조건) 값을 그대로 재사용한다 — 새 수치를 만들지 않는다.

1. **락온 유지**: Director가 직전 tick에 고른 `TargetId`가 아직 살아있고 390px 사거리 안이면 그대로 유지한다.
2. **킬 확정 우선**: 1이 성립하지 않으면, 사거리 390px 안의 살아있는 적 중 HP가 가장 낮은 적을 고른다. 동률이면 그중 가장 가까운 적을 고른다(동률 처리 규칙 — 로드맵에 없는 내 판단, fixture로 표시).
3. **최근접 대체**: 사거리 안에 살아있는 적이 하나도 없으면(2가 성립 안 함), 블랙리스트를 제외한 살아있는 적 중 가장 가까운 적을 고른다. Approach/Reposition의 이동 목표로 쓴다.
4. 살아있는 적이 전혀 없으면 `TargetId = null` (Searching).

이 우선순위는 Director 내부 계산만으로 끝난다 — `BattleSession.Enemies`가 이미 공개돼 있어 새 snapshot 필드가 필요 없다. 실제 공격 시에는 이 `TargetId`를 `Attack(skill, manual:false, targetId)`로 넘겨 **실제로 그 대상이 맞는지**를 `BattleSession`이 보장한다(아래 계약 변경 참조 — 이전 설계의 "항상 Nearest()로 재선정되는 공백"은 이 변경으로 해소됨).

## 출력 (AutoIntent, 0.2초마다 갱신되고 그 사이 캐시된 값이 그대로 적용됨)

- `MoveDirection`: -1 / 0 / +1 (수평만, 기존과 동일한 축)
- `WantsDodge`: bool, `DodgeAwayFrom`: 위협 위치(있으면)
- `Attack`: `None` / `Basic` / `Skill(index)`
- `TargetId`: 위 우선순위로 고른 값. `Attack()` 호출 시 실제로 이 대상을 공격 대상으로 강제한다.
- `UseHpPotion`: bool — 아래 "저체력 자동 물약" 규칙으로 매 tick 독립 평가(이동/공격 상태 머신과 배타적이지 않음, 기존 `UseHpPotion()`도 `Attacking` 여부와 무관하게 호출 가능한 것과 동일).
- `Reason`: 사람이 읽는 한글 사유 문자열(기존 `LastAction` 패턴 재사용, 새 텍스트 컴포넌트 불필요)

적용 규칙: `AutoActing`(= `Auto && Time >= ManualUntil && !Dead`, 기존과 동일)이 아니면 캐시된 intent를 전부 버리고 아무것도 적용하지 않는다. `AutoActing`이면 `BattleSession.Tick`이 매 프레임 캐시된 intent를 기존 이동 코드와 `Attack`/`Dodge`/`UseHpPotion` 호출로 그대로 적용한다 — director는 절대 좌표나 수치를 직접 쓰지 않는다.

## 상태 머신과 우선순위

**레이어 A(항상 최우선, 매 tick 재평가)**
1. **Suspended** — `!AutoActing`(Auto OFF, 수동 유예 3초 이내, 사망). 의도 없음(물약 자동사용 포함 전부 정지). *확정 규칙, 변경 없음.*
2. **Avoid** — 어떤 적의 `Windup > 0`이고 거리 ≤ 130px(기존 피해판정 110px + 20px 여유 fixture)이고 hero가 무적/보호막 상태가 아니면: `DodgeCooldown <= 0`이면 위협 반대 방향 `WantsDodge`, 아니면 같은 방향으로 `MoveDirection`만 반대로 내어 후퇴(구르기 불가 시 걷기로 대체).

**레이어 B(전투/이동 행동 선택 — Avoid가 아니면 이 순서로 하나만 선택)**
3. **Engage** — 위 타겟 선택 결과가 사거리(390px) 안의 대상이면(우선순위 1 또는 2로 뽑힌 경우) 즉시 교전한다: 반경 410 내 살아있는 적 ≥2이고 스킬1 `Cooldowns[1]<=0 && Mp>=Costs[1]`이면 스킬1(AoE), 아니면 스킬0 가용하면 스킬0, 아니면 `Basic`. **부모 결정 6에 따라 이 규칙은 무진행 타이머 상태(Return/Reposition 여부)와 무관하게 항상 최우선으로 성립한다 — "복귀 중이라도 사거리 안에 적이 있으면 즉시 교전"이 기본값이다.**
4. **Return** — 3이 성립하지 않고(사거리 안에 대상 없음) 무진행 누적 5.0초 이상이면: `MoveDirection`은 `SafeAnchor`(fixture, 아래) 방향, `Attack = None`. `SafeAnchor` 반경 24px 이내 도달 시 무진행 타이머를 0으로 리셋한다.
5. **Reposition(재탐색)** — 3, 4가 성립하지 않고 무진행 누적 2.0초 이상(5초 미만)이면: 현재 Approach 중이던 대상을 4.0초(fixture) 블랙리스트에 넣고 타겟 선택을 다시 실행(우선순위 3, 최근접 대체)한다. 무진행 타이머는 리셋하지 않음(5초 도달 시 4번으로 승격되도록).
6. **Approach** — 3, 4, 5가 성립하지 않고 타겟 선택 결과(우선순위 3, 최근접)가 있으면 대상 방향 `MoveDirection`.
7. **Searching** — 살아있는 적이 하나도 없으면 `MoveDirection = 0`, `Attack = None`.

**저체력 자동 물약(레이어와 독립, 매 tick 병렬 평가)** — 부모 결정 4:
`AutoActing`이고 `Hp / MaxHp < 0.35`이고 `HpPotions > 0`이면 위 레이어 A/B의 어떤 상태여도 관계없이 `UseHpPotion(manual:false)`을 호출한다. 기존 `UseHpPotion()`의 쿨다운·수량·회복량 제약을 그대로 따르며(60 HP 고정 회복, 수량 차감), 새 아이템이나 AUTO 전용 보너스는 만들지 않는다. `UseHpPotion()`이 현재 내부에서 무조건 `Manual()`을 호출하는 자기잠금 버그가 있어(아래 계약 변경 참조) `manual` 파라미터를 추가해야 AUTO가 물약을 쓸 때마다 스스로 3초 정지되는 일을 막을 수 있다. MP 물약 자동 사용은 부모 결정 4의 범위 밖이라 포함하지 않았다(HP만 명시됨).

## 시간과 취소 규칙

- 판단 주기: 도메인 `Time` 기준 0.2초마다 재계산. 같은 0.2초 구간 안의 여러 `Tick` 호출은 캐시된 intent를 그대로 적용한다(60Hz 적용 vs 0.2초 판단 분리, 확정 규칙). 저체력 물약 체크는 매 판단 tick마다 함께 재평가된다(별도 캐시 없음 — 물약 사용은 즉시성이 중요하므로 판단 주기 안에서 매번 최신 HP로 재확인).
- 재탐색 2.0초 / 복귀 5.0초 / 수동 유예 3.0초는 로드맵 확정 수치이며 fixture가 아니다(그대로 고정). 저체력 물약 임계 35%는 부모 지정 수치이며 fixture가 아니다(그대로 고정).
- 취소 계약: M1의 `CombatCommand`/캔슬 창은 아직 구현되지 않았다. 현재 등가 계약은 `BattleSession`의 기존 게이트뿐이다 — `Attacking`, `Time < dodgeUntil`, `!Grounded` 등. Director는 이 게이트를 우회하지 않고, 매 0.2초 재요청해도 게이트가 이미 막고 있으면 그냥 실패 반환을 받는다(부작용 없음). **M1이 실제 캔슬 창을 도입하면 이 절을 재검토해야 한다.**
- AUTO 결정 함수가 예외를 던지면 해당 tick은 직전 intent를 유지하고 로그만 남긴다(전투가 멈추지 않는다) — 방어적 설계로 이 문서에서 제안.

## 필요한 BattleSession 계약 변경 (승인됨 — 부모 결정 2, 3, 4)

```csharp
public bool Attack(int skill = -1, bool manual = true, int targetId = -1)
public bool Dodge(Vector2 input, bool manual = true)
public bool Jump(bool manual = true)
public bool UseHpPotion(bool manual = true)
```

- `Attack`: `targetId >= 0`이면 `Enemies.Find(x => x.Id == targetId && !x.Dead)`로 그 대상을 우선 사용하고, 못 찾으면(사망/제거됨) 기존 `Nearest()`로 안전하게 대체한다. `targetId`를 생략(-1)하면 완전히 기존 동작(항상 `Nearest()`) — 수동 호출부(`BattleScreen`의 버튼/키 입력)는 코드 변경 없이 그대로 동작한다.
- `Dodge`/`Jump`/`UseHpPotion`: `manual=false`일 때만 내부 `Manual()` 호출을 건너뛴다. 기본값이 `true`이므로 기존 수동 호출부는 무변경으로 동작한다.
- 이 네 메서드 모두 이미 "완료"로 표시된 M0 슬라이스의 공개 API를 건드리지만 부모 승인이 났으므로 그대로 반영한다.

## 저장 경계 (부모 결정 5 — 승인됨, 그대로 유지)

전투 세션 로컬 상태만 다룬다(무진행 타이머, 락온/블랙리스트, 캐시 intent). `Close()`/`ResetRun()` 시 director 내부 상태를 전부 초기화해야 한다 — 현재 `Close()`는 `Impacts.Clear()`만 하므로 director의 `Reset()` 호출을 한 줄 추가해야 한다.

AUTO ON/OFF는 영구 저장하지 않는다. 매 새 `BattleSession` 생성 시 기존 기본값(`Auto = true`)으로 시작한다. 저장 시스템 자체가 프로젝트에 아직 없으므로(M0 문서가 저장을 후속 모듈로 명시) 이 정책이 현재 완료 기준의 "저장 정책 일치"를 충족하는 최종 해석이다.

## 실패와 복구

- 판단 함수 예외 → 직전 intent 유지, 로그만 기록(위 참조).
- 대상 소실(사망/범위 이탈) 중 진행 중이던 캐스팅 → 기존 `Attack`/`Tick`의 사망 처리 그대로(변경 없음), director는 다음 판단에서 새 대상을 고른다.
- 물약 소진(`HpPotions == 0`) 상태에서 저체력이 지속돼도 실패로 취급하지 않는다 — AUTO는 물약 없이 Avoid 위주로 계속 버틴다. `UseHpPotion()`의 기존 수량 게이트가 그대로 실패를 반환하며 director는 이를 그냥 무시하고 다음 tick을 진행한다.
- Return 상태에서 도중에 위험(Windup)에 걸리면 Avoid가 여전히 최우선이므로 회피 후 Return을 계속한다.
- ~~Return 중에는 사거리 안에 새 대상이 들어와도 재교전하지 않는다~~ — **부모 결정 6에 따라 이 규칙을 제거했다.** 기본 설계는 사거리 안에 대상이 있으면 Return/Reposition 여부와 무관하게 항상 Engage가 먼저 성립한다(위 상태 머신 3번 참조). 안정성 우려(사거리 경계에서 Engage↔Return이 짧게 오가는 flicker)가 실제로 관찰되면, **선택적으로 고려할 수 있는 옵션**으로 사거리 경계에 소폭의 히스테리시스(예: 진입 390px / 이탈 420px처럼 진입·이탈 임계값을 다르게 둠)를 추가하는 방법이 있다 — 기본 설계에는 포함하지 않는다.

## 에셋 목록

없음. 이 모듈은 신규 스프라이트/사운드/폰트를 요구하지 않는다. 기존 `LastAction` 텍스트, AUTO 램프, 룬 회전 연출, 기존 물약 슬롯 UI를 그대로 재사용한다.

## 실제 씬 배선

신규 GameObject/Canvas 없음. `BattleSession` 생성자 내부에서 `AutoHuntDirector` 인스턴스 하나를 비공개로 생성해 보관하고, `Tick(float delta, Vector2 input)` 시그니처는 바꾸지 않는다. `BattleScreen`의 AUTO 램프 클릭(`battle.ToggleAuto()`), `T` 키, 물약 버튼(`battle.UseHpPotion()`) 입력은 무변경.

## 디버그 관측값

새 UI 에셋 없이 다음을 노출한다:
- 기존 `LastAction` 문자열에 AUTO 사유를 이어 씀(예: "AUTO · 재탐색(무진행 2.1s)", "AUTO · 마나 절약 · 기본공격", "AUTO · 체력 물약(35% 미만)").
- `--auto-debug` 실행 인자(기존 `--smoke`, `--capture-dir` 패턴과 동일)가 있으면 각 판단 tick마다 상태 이름, TargetId, 무진행 누적 초, 발동 규칙 번호, 물약 사용 여부를 `Debug.Log`로 남긴다.
- 순수 데이터 구조체(`AutoDebugState`: 상태 enum, TargetId, SecondsSinceProgress, LastHpPotionAt)를 `BattleSession`에서 읽기 전용으로 노출해 향후 편집기 오버레이가 쓸 수 있게 한다.

## 자동 검사

1. 판단 주기: 같은 0.2초 구간 내 여러 `Tick` 호출에서 intent가 바뀌지 않는다.
2. Suspended: `Time < ManualUntil`(또는 Auto OFF, 사망) 구간에는 어떤 intent도 적용되지 않는다(물약 자동사용 포함).
3. 타겟 우선순위: 락온 대상이 사거리 안에 살아있는 동안은 다른 후보가 더 낮은 HP여도 교체되지 않는다. 락온 대상이 죽거나 사거리를 벗어나면 사거리 내 최저 HP 대상으로 즉시 교체된다. 사거리 내 대상이 전혀 없으면 최근접(블랙리스트 제외)으로 전환된다.
4. Engage 우선: Return 또는 Reposition 상태 중에도 사거리 안에 살아있는 적이 있으면 그 tick에는 이동 대신 공격 의도가 나온다.
5. 재탐색: 사거리 밖 특정 대상을 2.0초 연속 추적해도 무진행이면 그 대상이 블랙리스트에 들어가고 다른 대상(또는 없음)으로 전환된다.
6. 복귀: 사거리 안에 대상이 없는 채로 무진행 5.0초 이상이면 이동 의도만 SafeAnchor 방향으로 나오고 공격 의도가 없다. 반경 도달 시 무진행 타이머가 리셋된다.
7. 위험회피 우선순위: Windup 임박 상황에서는 Engage/Approach 의도가 나오지 않고 Dodge(가능 시) 또는 회피 이동(불가 시)만 나온다.
8. 무보너스 검사: 동일 fixture에서 AUTO가 유발한 `Attack` 결과(피해량/MP소비/쿨다운 변화)와 동일 파라미터·동일 대상의 수동 `Attack` 결과가 완전히 같다.
9. 저체력 물약: `Hp/MaxHp<0.35`이고 `HpPotions>0`이면 다음 판단 tick 이내에 `UseHpPotion(false)`이 호출되고, `ManualUntil`이 바뀌지 않으며(자기잠금 없음), 회복량·수량 차감이 수동 사용과 동일하다. `HpPotions==0`이면 호출되지 않고 예외 없이 다음 로직을 계속한다.
10. 자기잠금 회귀: AUTO가 호출한 `Dodge(_, false)`/`Jump(false)`/`UseHpPotion(false)` 이후 `ManualUntil`이 변경되지 않는다.
11. 중복보상 검사: soak 동안 Kills/Xp/Gold 증가량이 실제 처치 수와 항상 1:1이다(기존 1회 보상 규칙 회귀 없음 재확인).
12. 벽끼임/정지 검사: soak 동안 `Hero.x`가 5초 넘게 동일 좌표(오차 1px 이내)로 머무르는 구간이 없다.

## 수동 검토

- 실제 빌드에서 `T`키로 AUTO ON/OFF를 반복 토글할 때 램프/룬 표시가 기존과 동일하게 즉시 반응하는지.
- 고블린 3개체가 동시에 Windup 상태일 때 AUTO가 무한 회피 루프에 빠지지 않는지 60초 관찰.
- 수동으로 이동만 계속 입력했을 때 AUTO가 실제로 3초 동안 완전히 손을 떼는지(로그 문자열로 확인).
- 체력을 인위적으로 35% 아래로 떨어뜨렸을 때 물약을 실제로 쓰고, 물약이 다 떨어진 뒤에는 멈추지 않고 회피 위주로 버티는지.
- 복귀(Return) 이동 중 적이 사거리에 들어왔을 때 실제로 멈춰서 교전하는지, 사거리 경계에서 눈에 띄는 심한 flicker가 있는지.
- `LastAction`에 표시되는 AUTO 사유 문구가 실제 상황과 일치하는지.

## 완료 기준

- 10분 soak(실시간 또는 시간 가속)에서 멈춤·벽 끼임·중복 보상 없이 전투가 지속된다.
- AUTO ON/OFF 정책 = "영구 저장 없음, 매 세션 기본 ON"이 문서·코드 일치.
- 위 자동 검사 12개 전부 통과.
- 위 수동 검토 6개 항목 이상 없음.

## fixture 표

| 값 | 수치 | 근거 |
|---|---:|---|
| 판단 주기 | 0.2s | 로드맵 확정 |
| 재탐색 임계 | 2.0s | 로드맵 확정 |
| 복귀 임계 | 5.0s | 로드맵 확정 |
| 수동 유예 | 3.0s | 로드맵 확정, 기존 `ManualUntil` 값 재사용 |
| 저체력 물약 임계 | Hp/MaxHp < 0.35 | 부모 결정 4 확정 수치 |
| 사거리(타겟 선택·Engage) | 390px | 확정 — 기존 `Attack()` 내부 게이트 값 그대로 재사용, 새 수치 아님 |
| 위험회피 감지 반경 | 130px | fixture — 기존 피해판정 110px + 20px 여유 |
| 재탐색 블랙리스트 지속시간 | 4.0s | fixture |
| 무진행 판정 이동량 임계 | 4px | fixture |
| SafeAnchor 좌표 | (360, GroundTop+HeroHalfHeight) | fixture — 초기 Hero 스폰 좌표를 임시 채용, 실제 "안전지역" 아트/기획 기준 없음 |
| 동률 HP 킬확정 타이브레이크 | 가장 가까운 적 | fixture — 로드맵/부모 결정에 없는 내 판단 |

---

## 남은 확인 필요 항목 (참고용, 착수를 막지 않음)

- `SafeAnchor` 좌표는 여전히 임시값(초기 스폰 좌표)이다. 실제 "안전지역" 기준이 기획에서 나오면 교체가 필요하다.
- 동률 HP 킬확정 타이브레이크(가장 가까운 적)는 로드맵/부모 결정에 명시되지 않은 내 판단이다. 문제가 되면 쉽게 바꿀 수 있는 지점으로 격리해뒀다(타겟 선택 함수 한 곳).
- Engage↔Return 경계 flicker는 "실패와 복구" 절에 옵션으로만 남겨뒀다 — 기본 구현에는 없으므로 실제 플레이에서 체감되면 그때 히스테리시스를 추가하면 된다.
