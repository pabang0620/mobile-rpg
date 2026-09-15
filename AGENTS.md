# 에이전트 작업 지침

## 우선순위

사용자의 최신 지시 → 이 `AGENTS.md` → `docs/planning/*.md` + `PLANNING_START_HERE.md`(기획, 불변) → `docs/DECISIONS.md`/`docs/HANDOFF.md`(기술 방향과 현재 상태) → `docs/ASSET_STATUS.md`.

**`docs/planning/` 5개 파일과 `PLANNING_START_HERE.md`는 절대 수정하지 않는다.** 참고만 하고, 계획을 바꿔야 할 사유가 생기면 그 내용을 `docs/DECISIONS.md`에 새 결정으로 기록한다.

## 개발 프로세스 (2026-09-14부터)

이 프로젝트는 **Claude Code 세션 + `lh2d-*` 전용 에이전트**로 작업한다. 예전 `PARALLEL_CONTRACT.md`/`COMBAT_HANDOFF.md`가 설명하던 **Codex CLI 기반 병렬 에이전트 방식(combat/campaign/presentation 3개 에이전트가 `apply_patch`로 동시에 각자 모듈을 수정)은 더 이상 쓰지 않는다.** 그 두 문서는 내용이 `docs/HANDOFF.md`에 흡수되어 삭제되었다.

새 작업 순서 (모듈 1개당):

1. **lh2d-module-planner** - 모듈 설계·계약 고정 (상태 소유, 공개 시그니처, 입력/거절 조건)
2. **lh2d-combat-implementer** 또는 **lh2d-asset-specialist** - 승인된 설계를 구현 (전투/게임플레이 코드는 전자, 에셋 임포트·배선은 후자)
3. **lh2d-qa-verifier** - 독립 재검증 (구현자 본인이 아닌 별도 패스)

이 4개 에이전트는 `project/.claude/agents/`에 심볼릭 링크로 연결되어 있어야 한다(원본은 이 레포의 `.claude/agents/`). **이 문서를 재작성한 시점에 확인한 결과, 실제로는 이 레포에 `.claude/agents/` 디렉토리 자체가 없고 `project/.claude/agents/`에도 `lh2d-*` 링크가 없다.** 이 상태를 고치는 것은 이번 문서 재작성 범위 밖이라 손대지 않았다 - 다음에 이 에이전트들을 실제로 쓰려면 먼저 파일과 링크를 만들어야 한다.

## 확정된 기술 방향 (요약, 상세는 `docs/DECISIONS.md`)

- 이동: 격자(타일) 스냅 이동. 방향 입력 1회 = 정확히 1칸, 짧은 tween으로 부드럽게.
- 카메라: 일반 직교(orthographic) 카메라, 화면 세로 기준 9타일 고정(`orthographicSize=4.5`) + Ground 타일맵 기준 맵 경계 클램프. 참고 해상도 1280x720(가로) - `com.unity.2d.pixel-perfect`(PixelPerfectCamera)는 2026-09-15 제거됨(`docs/DECISIONS.md` 해당 날짜 항목 참고).
- 에셋: 무료 CC0/CC-BY 커뮤니티 팩(지형/배경) + AI 생성(핵심 캐릭터/몬스터/특징 오브젝트) 하이브리드.

**현재 코드는 이 방향으로 아직 마이그레이션되지 않았다.** `Domain/Combat`(전투 중 이동)은 연속 XY 자유 이동이고, 카메라는 자체 제작 `PixelCameraFollow`(공식 Pixel Perfect Camera 패키지 아님)를 쓴다. 마이그레이션은 별도 후속 작업으로 남겨둔다 - 이 문서 재작성만으로 코드가 바뀐 것처럼 보고하지 않는다. 자세한 현재 상태는 `docs/HANDOFF.md` 참고.

## 아키텍처 원칙 (유지, `docs/planning/02_SYSTEM_CONTRACTS.md`에서 계승)

- `Domain ← Application ← Presentation/UnityAdapters`, `Infrastructure → Application 인터페이스`. Domain은 UnityEngine/MonoBehaviour/파일 I/O를 참조하지 않는다.
- 정의 데이터(스킬/적/아이템/퀘스트/구역)는 안정된 문자열 ID로 참조하고, Unity instance ID나 배열 순서에 의존하지 않는다.
- 명령 결과는 `Accepted` 또는 명시적 거절 코드로 통일하고, 거절된 명령은 자원/쿨다운/보상을 바꾸지 않는다.
- 이 원칙들은 이동 방식이 격자 스냅으로 바뀌어도 그대로 유지한다 - 바뀌는 것은 이동 입력/판정 방식이고, 상태 소유 구조가 아니다.

## 검증 관행

- 자동 검증은 **컴파일 성공 + 도메인 규칙 테스트(정상/오류/취소 사례) 통과**까지만 한다.
- **실제 화면 렌더링, 플레이 느낌, 조작감 판단은 Claude가 임의로 하지 않는다.** 사용자가 직접 빌드를 실행해 확인한다.
- Unity Editor 라이선스가 없는 환경에서는 Editor/Player 실행 검증이 막힌다(`No valid Unity Editor license found`) - 이 경우 STATIC/EDITMODE 검증까지 하고 PLAYMODE/PLAYER/DEVICE 검증은 미완료로 명시한다.

## 파일 소유권

- Domain(Combat/Campaign/World) - 순수 C#, 게임플레이 규칙
- Infrastructure - 저장/파일 I/O
- Application(`GameApp`) - Domain↔Presentation 연결 파사드
- Presentation - uGUI, 입력, 카메라, 렌더링
- Editor - 빌드 파이프라인

여러 모듈에 걸친 변경은 module-planner 단계에서 영향 범위를 먼저 정리하고 진행한다.
