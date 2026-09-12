# M1 전투감·캐릭터 표현 기술 명세

상태: 구현 전 설계 고정안. M0 전투 규칙과 실제 배선은 완료됐고, M1은 임시 4포즈 전환을 일관된 애니메이션·판정·피드백 구조로 교체한다.

## 플레이 결과
이동 중 법사의 발과 지면이 미끄러지지 않고, 기본 3연계와 네 스킬의 준비/발동/명중/복귀가 눈으로 구분된다. 공격을 맞으면 적이 멈칫하고 밀리며, 빗나감·무적·슈퍼아머가 같은 규칙으로 설명된다. AUTO와 수동은 동일한 전투 명령을 사용하므로 피해와 캔슬 규칙이 달라지지 않는다.

## 범위
포함: locomotion, facing, action timeline, command buffer, hitbox/hurtbox, 1회 명중 집합, hit-stop, 경직/밀림, 무적/슈퍼아머, 애니메이션 presenter, 분리 VFX, 카메라 충격, 디버그 표시.

제외: 장비 능력치, 정식 스킬40종, 영구 성장, 던전 방 전환, 레이드 phase, 네트워크 권한. 수치는 전투감 검증 fixture로 표시한다.

## 데이터 계약

`CombatCommand`
- `sequence`: 세션 내 단조 증가 ID.
- `actorId`, `kind`, `slot`, `move`, `issuedAt`.
- `source`: Manual/Auto/Replay. 판정에는 사용하지 않고 관측용으로만 남긴다.

`ActionDefinition`
- `id`, `duration`, `startupEnd`, `activeEnd`, `recoveryEnd`.
- `manaCost`, `cooldown`, `movementCurveId`, `facingLockAt`.
- `cancelWindows[]`: 시작/끝/허용 command tag.
- `hitTracks[]`, `vfxEvents[]`, `cameraEvents[]`, `audioEvents[]`.

`HitTrack`
- 초 단위 `start/end`, actor-local 중심과 크기, shape(Box/Capsule/Arc), damage fixture, hitStop, stagger, knockback, targetMask.
- `hitPolicy`: OncePerAction/OncePerTrack/Interval. 기본은 OncePerAction.
- 동일 action에서 `HashSet<targetId>`로 이미 맞은 대상을 보관하고 action 종료 때 폐기한다.

`CombatSnapshot`
- 위치, facing, locomotion, actionId/actionTime, HP/MP, invulnerable/superArmor, velocity, current target.
- presenter가 읽기만 한다. 이미지 프레임과 UI가 snapshot을 다시 쓰지 않는다.

## 시간과 상태
규칙 시뮬레이션은 누적 시간을 1/60초 step으로 소비하고 한 화면 frame에서 최대 5step까지만 수행한다. 초과 시간은 진단값으로 기록해 spiral을 막는다. 렌더는 이전/현재 snapshot을 보간한다. 일시정지 중 규칙 시간과 쿨다운은 진행하지 않는다.

상태 우선순위: Dead > Down > HitStun > Dodge > Skill > BasicAttack > Move > Idle. 상태 전환은 `TryStartAction` 한 곳에서 비용, 쿨다운, 취소 창, 생존 상태를 검사하고 수락된 경우에만 MP를 한 번 소비한다.

명령 버퍼는 최근 0.15초 입력 1개를 보관한다. 연계 가능 창에 들어오면 소비하고, 사망·다운·세션 종료·재도전 시 즉시 비운다. AUTO 명령도 같은 버퍼를 사용하지만 대기 중인 수동 명령이 있으면 버린다.

## 기본공격 3연계 fixture

| 단계 | Startup | Active | Recovery | 피해 | 이동 | 다음 연계 입력 창 |
|---|---:|---:|---:|---:|---:|---:|
| A1 지팡이 찌르기 | 0.14s | 0.08s | 0.25s | 24 | +34px | 0.24~0.42s |
| A2 역방향 휘두르기 | 0.12s | 0.10s | 0.28s | 27 | +42px | 0.24~0.45s |
| A3 사파이어 폭발 | 0.20s | 0.12s | 0.42s | 42 | +18px | 없음 |

일반 적 hit-stop은 공격자/피격자 각각 A1 45ms, A2 55ms, A3 80ms다. 보스는 위치 밀림을 무시할 수 있지만 hit-stop과 피해 피드백은 유지한다. 이 값은 손맛 검증 fixture이며 정식 밸런스 표가 아니다.

## 기존 네 스킬의 M1 표현
- 비전 화살: 0.18초 발사, 별도 projectile이 충돌을 소유. 캐릭터 시트에 투사체를 그리지 않는다.
- 수정 파동: 0.28초 준비 뒤 전방 부채꼴 1회 판정. 바닥 결정 VFX의 성장 시점과 active 시작을 맞춘다.
- 점멸: 0.22초 이동, 시작 후 0.42초 무적. 출발/도착 잔상을 별도 VFX로 둔다.
- 마나 보호막: 0.25초 시전 뒤 3.5초 상태. 보호막 sprite가 피해 감소를 결정하지 않는다.

## 스프라이트 제작 계약
캐릭터 셀은 RGBA 512×512, 발 기준선 y=448, 기준 중심 x=256으로 통일한다. 캐릭터 본체/지팡이는 셀 경계에서 최소 12px 떨어뜨린다. 공격 궤적·마법은 별도 시트라서 본체 실루엣과 알파 경계를 오염시키지 않는다.

1차 세트: idle 8, run 8, A1/A2/A3 각 10/12/16, hit 4, down 6, get-up 6, death 8, cast 공통 8. 총 78프레임. 오른쪽 기준을 제작하고 왼쪽은 임포트 단계 mirror로 제공하되, 후드 자수·허리 보석·지팡이 손잡이의 비대칭이 어색한지 별도 캡처한다. 어색하면 왼쪽 세트를 따로 제작한다.

각 시트에는 텍스트·격자·배경·워터마크가 없어야 한다. 프레임별 실제 자세가 달라야 하며 정지 그림의 이동/회전/확대만으로 동작을 만들지 않는다. 머리 크기, 로브 길이, 지팡이 길이, 보석 위치, 팔레트 관계가 전 프레임에서 유지돼야 한다.

## Unity 구조
- `Domain/Combat`: 명령, action runner, hit resolution, 상태와 snapshot. Unity UI 참조 금지.
- `Content/Combat`: ActionDefinition ScriptableObject와 fixture 데이터. 코드 상수로 timing을 중복하지 않는다.
- `Runtime/Presentation`: snapshot→Animator/Sprite/VFX/Camera 변환. 애니메이션 이벤트가 피해 함수를 직접 호출하지 않는다.
- `Runtime/Input`: keyboard/touch/auto를 `CombatCommand`로 변환.
- `Editor/Validation`: 시트 셀, alpha, 기준선, 참조 누락, timeline 정렬 검사.

M0의 `BattleSession`은 한 번에 갈아엎지 않는다. 먼저 새 action runner를 기본 공격에 연결하고 기존 검사와 화면을 유지한다. A1이 통과하면 A2/A3, 피격, 스킬 순으로 옮긴다. 마이그레이션 중 옛 공격 경로와 새 공격 경로가 동시에 피해를 적용하지 않도록 feature flag는 한 곳에만 둔다.

## 자동 검사
1. 같은 action/target은 1회만 명중한다.
2. active 시작 전과 종료 후에는 피해가 없다.
3. 거절된 action은 MP/쿨다운을 바꾸지 않는다.
4. 0.15초 버퍼 안의 연계 입력만 소비된다.
5. dodge 무적 경계 직전/직후가 명세와 일치한다.
6. hit-stop 중 action 시간과 이동이 멈춘다.
7. 사망/재도전/Close가 예약 hit와 명령을 제거한다.
8. 30/60/120fps 렌더 조건에서 60Hz 규칙 결과 hash가 같다.
9. 모든 sprite가 실제 투명/보이는 픽셀을 포함하고 셀 경계 12px를 지킨다.
10. 누락 frame·중복 frame·완전 동일 frame 비율을 검사한다.

## 실제 검토 장면
`CombatLab` 씬에 정지 표적, 일반 고블린, 슈퍼아머 표적을 둔다. action time, 현재 상태, hitbox/hurtbox, 이미 맞은 target ID, command buffer, hit-stop 시간을 토글로 표시한다. 일반 캡처에는 디버그 표시를 끄고, 판정 증거 캡처에는 켠다.

완료 증거는 도메인 검사 결과, 에셋 검사 JSON, 1280×720과 2400×1080 플레이 캡처, 60초 기본 연계/스킬 영상, 로그 오류 0개다. 모바일 입력 지연은 M6 전까지 미검증으로 남기되 터치 버튼이 동일 command 경로를 쓰는지는 확인한다.
