# 시스템 계약 v1

## 구조와 의존성

Unity 6000.5.9f1이 설치되어 있음을 확인했다. 이 버전에 고정해 최초 컴파일부터 확인한다. C#은 Unity 게임 규칙과 런타임 구현에 사용한다. 패키지 버전은 실제 설치 호환성 검사 후 manifest/lock에 고정하고 추정 버전을 적지 않는다.

`Domain ← Application ← Presentation/UnityAdapters`, `Infrastructure → Application 인터페이스`. Domain은 UnityEngine, GameObject, MonoBehaviour, 파일 I/O를 참조하지 않는다. 월드 좌표용 Vec2, seeded RNG, tick clock은 순수 C# 값/서비스로 정의한다. ScriptableObject는 제작용이며 로딩 때 불변 런타임 정의로 변환한다. UI/애니메이션은 규칙 결과를 표시하고 피해 시점을 결정하지 않는다.

권장 디렉터리: `Assets/Sapphire/{Domain,Application,Infrastructure,Presentation,Content,Art,Audio,Scenes,Editor,Tests}`. 각 레이어 asmdef를 분리한다. 초기에는 직접 참조한 콘텐츠 카탈로그를 사용한다. Addressables/DI 프레임워크/ECS/네트워크 추상화를 미리 도입하지 않는다.

## 상태 소유 및 명령

| 소유자 | 소유 상태 | 입력 | 결과 |
|---|---|---|---|
| GameSession | 모드, 구역, runId, 정지 여부 | Start/Continue/Travel/Pause | SceneTransitionRequested |
| WorldSimulation | 위치, 충돌, 적 AI, 투사체, tick | Move/Attack/Cast/Dodge | DamageApplied/EntityDied |
| ProgressionService | XP, 레벨, 계산 능력치 | GrantXp/Equip/Upgrade | StatsChanged |
| InventoryService | 골드, 스택, 장비 인스턴스 | Grant/Consume/Buy/Equip/Upgrade | TransactionCommitted |
| QuestService | 수락/목표/보상 여부 | Accept/RecordKill/TurnIn | QuestChanged |
| DungeonRun | 방, 웨이브, 보상 원장, 완료 상태 | Enter/RecordKill/Finish/Fail | RunSettled |
| SaveRepository | 저장 파일·백업·마이그레이션 | Save(snapshot)/Load | 성공 또는 구조화된 오류 |
| Presenter | 선택/표시/포인터 상태 | 사용자 이벤트 | 애플리케이션 명령만 호출 |

명령 결과는 `Accepted` 또는 거절 코드(`Dead`, `Paused`, `Cooldown`, `InsufficientResource`, `Locked`, `InvalidTarget`, `InventoryFull`, `AlreadyCommitted`)로 통일한다. 거절 명령은 비용·쿨다운·보상을 바꾸지 않는다. 이벤트에는 sessionId, tick, eventId, entityId를 포함한다. 이벤트를 UI에서 재수신해도 보상을 다시 지급하지 않는다.

## 시뮬레이션 계약

고정 60Hz. 렌더는 이전/현재 위치 보간. 한 렌더 프레임 최대 5 tick을 수행하고 초과 누적은 버려 나선형 지연을 방지한다. 일시정지 시 누적을 초기화한다. 쿨다운/무적/상태 지속은 tick 단위, 벽시계 미사용.

처리 순서: 입력 검증 → 이동/충돌 → 공격 및 적 예고 진행 → 히트 후보 수집 → 안정된 entityId 순서로 피해 적용 → 사망/드롭 → 퀘스트 → 모드 전환. 동일 tick 플레이어와 보스 동시 사망은 실패가 우선이며 던전 성공 보상을 지급하지 않는다.

캐릭터는 반경 0.22 월드 단위 원, 장애물은 AABB/원으로 제작한다. 이동은 축별 충돌 해소, 점멸은 경로를 검사하여 첫 장애물 앞에서 정지한다. 투사체는 이전→현재 위치 구간 검사로 관통 누락을 방지한다. 월드 좌표를 화면 픽셀로 저장하지 않는다. 아트 크기와 충돌 크기는 독립이다.

공격 상태: Idle → Startup → Active → Recovery → Idle. 사망은 모든 상태에서 전이. 회피는 Idle/Recovery에서만 허용, Startup/Active에서는 거절. 기본공격 버퍼는 1개, 유효기간 0.18초. 연계는 이전 Recovery 종료 후 0.6초 이내 수락 시 다음 단계, 이외 1단계. 공격 시작 시 방향 고정. 스킬은 연계 초기화. 한 attackId는 같은 entityId에 최대 한 번 피해를 준다.

## 초기 밸런스 표 — 플레이테스트용

| 항목 | 값/규칙 |
|---|---|
| 플레이어 | HP 120, MP 100, 공격력 18, 방어력 4, 이동 3.6u/s |
| 레벨 | 최대 5, 다음 레벨 필요 XP: 80/120/180/260, 레벨마다 HP +12/공격 +3 |
| 마나 | 전투 중 초당 2, 마을 전부 회복 |
| 기본 1/2/3 | 공격 배율 1.0/1.1/1.5; Startup .12/.12/.20, Active .05, Recovery .20/.20/.30초 |
| 기본 마법탄 | 속도 8u/s, 최대 거리 3u, 1대상 후 소멸 |
| 비전탄 | MP12, CD3초, 배율2.0, 사거리5u, Startup .18초 |
| 서리 파동 | MP24, CD7초, 배율1.4, 반경2u, 2초간 이동 -40%, Startup .25초 |
| 점멸 | MP18, CD5초, 거리2u, 무적 .25초, 벽 통과 불가 |
| 보호막 | MP22, CD10초, 흡수량40, 지속4초, 중첩 불가 |
| 회피 | 거리1.2u/.22초, 무적 .18초, CD1.2초 |
| 물약 | HP60 또는 MP45, 종류 간 공유 CD3초, 최대치에서 사용 거절 |
| 피해 | max(1, floor(공격력×배율)-방어력), 치명타/명중 난수는 MVP 제외 |
| 강화 | +1/+2/+3 비용 30/60/100골드, 공격 무기 +2/단계, 방어구 방어 +1/단계 |

회복은 최대치로 clamp. 쿨다운은 수락 tick부터. 스킬 취소/사망으로 비용을 환불하지 않는다. 레벨업은 HP를 증가한 최대치만큼 올리고 최대치로 제한, XP 초과분은 다음 레벨로 넘긴다. 레벨 5의 XP는 0으로 고정한다. 장비 변경에 따른 최대 HP 감소도 현재 HP를 clamp한다.

적 기본값: 고블린 HP50/공격10/XP15, 정령 HP40/공격12/XP18, 늑대 HP60/공격14/XP20, 정예 HP180/공격18/XP50, 보스 HP650/공격22/XP150. 일반 골드 4/5/6, 정예20, 보스60. 드롭 확률/전리품 테이블은 Content로 소유한다. 밸런스 조정은 테스트 목표에 근거해 표와 데이터를 함께 변경한다.

적 상태: Idle → Chase → Telegraph → Attack → Recover; Hurt는 보스 외 공격 취소, Dead는 종결. 초기 추적은 0.25u 격자 A*와 경로 재계산 0.2초, 시야 차단 검사. 타격 예고는 판정과 같은 ShapeDefinition을 렌더링한다. 일반 예고 최소 .45초, 보스 최소 .70초. 벽에 막힌 원거리 적은 위치를 바꾸고 무조건 발사하지 않는다.

## 데이터 계약

정의 ID는 `skill.arcane_bolt`, `enemy.goblin`, `item.staff_01`, `quest.q01`처럼 안정된 문자열. 저장 파일은 Unity instance ID, 배열 순서, 표시 이름을 참조하지 않는다.

필수 정의:

- SkillDefinition: id, unlockLevel, costMp, cooldownTicks, startupTicks, recoveryTicks, effectId, range, multiplier, artId.
- EnemyDefinition: id, stats, aiType, attackId, lootTableId, artId.
- ItemDefinition: id, category, equipSlot, maxStack, modifiers, price, iconId. 장비는 instanceId/definitionId/upgradeLevel을 별도 저장.
- QuestDefinition: id, prerequisiteIds, objectives(type/targetId/count), rewards, nextQuestIds.
- ZoneDefinition: id, mapArtId, bounds, collisionShapes, spawns, exits, checkpoints. 출구는 destinationZoneId/spawnId.
- DungeonDefinition: id, rooms, bossId, rewardTableId.

시작 시 중복 ID/없는 참조/음수 비용/잘못된 출구/퀘스트 순환 의존/빈 드롭 테이블/누락 아트를 검사한다. 오류는 에셋 경로와 ID를 포함하고 빌드를 실패시킨다.

## 저장과 거래

SaveData: schemaVersion=1, profileId, revision, level/xp, gold, inventory, rewardInbox, equippedInstanceIds, questStates, unlockedZoneIds, checkpoint(zoneId/spawnId), settings, lastSettledRunId, recentTransactions. recentTransactions는 최근256개 transactionId/결과를 저장한다. 각 명령은 expectedRevision을 포함하며, 원장에 없는 과거 revision 명령은 거절하여 원장 축소 후 재지급을 방지한다. 현재 전투 위치/투사체/적 HP는 저장하지 않는다. 로드 시 안전 체크포인트, HP/MP 회복, AUTO OFF.

필드 처치/구매/강화/퀘스트 완료/던전 정산마다 변경 전체를 단일 스냅샷으로 만든다. SaveRepository는 임시 파일 작성 → 검증 → 기존 파일 백업 → 원자적 교체. 최초 파일 생성도 처리한다. 스냅샷 저장 실패 시 영구 거래는 적용하지 않고 오류/재시도를 표시한다. 필드 보상 실패는 보류 상태로 두고 구역 이탈을 막아 재시도하게 한다. 같은 transactionId를 재시도해도 같은 결과를 반환한다.

던전 안에서는 물약 소비와 드롭/XP/골드를 RunLedger에 기록한다. 실패/앱 종료 시 드롭/XP/골드 폐기, 실제 소비한 물약은 체크포인트 저장으로 유지한다. 성공 시 inventory+xp+quest+lastSettledRunId를 하나로 저장하고 저장 성공 뒤 결과 확인을 허용한다. 던전 재개는 MVP 미지원, 앱 종료 후 마을로 복귀한다.

배낭: 장비 24칸, 재료/물약 스택 각각 최대99. 필드 장비 드롭은 월드에 남으며 공간 부족 표시, 구역 이탈 전 미획득 경고. 퀘스트/던전 확정 보상은 별도 수령함 24칸에 대기시켜 손실을 방지한다. 수령함도 가득 차면 정산을 보류하고 정리 UI 제공. 거래는 사전 검증 후 전체 적용 또는 전체 거절.

손상된 주 저장은 백업 로드 후 경고. 둘 다 손상되면 새 게임으로 자동 덮어쓰지 않고 복구 실패 안내. 미래 schemaVersion은 로드 거절. 마이그레이션은 명시적 vN→vN+1 함수와 fixture 테스트. 설정도 로컬 저장에 포함하며 음량/화면 외 게임 경제를 PlayerPrefs에 저장하지 않는다.
