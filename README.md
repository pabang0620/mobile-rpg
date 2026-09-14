# Sapphire: 달빛의 균열 (레포명 mobile-rpg)

Unity 6000.5.9f1 기반 탑다운 2D 액션 RPG 세로 슬라이스. 사파이어 메이지가 마을에서 의뢰를 받고 숲을 탐험해 폐허의 수호자를 처치하는 첫 장(20~30분 목표)을 만든다.

기획 확정: [`PLANNING_START_HERE.md`](PLANNING_START_HERE.md) + [`docs/planning/`](docs/planning/) (읽기 전용, 이 문서들은 수정하지 않는다). 기술 방향 확정 이력은 [`docs/DECISIONS.md`](docs/DECISIONS.md), 현재 구현 상태는 [`docs/HANDOFF.md`](docs/HANDOFF.md)를 본다.

## 확정된 기술 방향 (2026-09-14)

별도 실험 프로젝트(`../topdown-asset-mvp/`)로 이동 방식과 에셋 전략을 검증해 아래로 확정했다. 상세 근거는 `docs/DECISIONS.md` 참고.

- **엔진/시점**: Unity 6000.5.9f1, 탑다운 2D.
- **이동 방식**: 격자(타일) 기반 스냅 이동. 방향키 입력 1회(또는 꾹 눌렀을 때 반복 틱)마다 정확히 타일 1칸만 이동하고, 짧은 tween(약 0.05~0.1초, 최종 값은 폴리싱 단계에서 조정)으로 부드럽게 스냅한다. 이동 중 새 입력은 무시해 여러 칸이 겹쳐 밀리지 않는다. 목표 조작감은 바람의나라/제노니아/포켓몬 골드류 클래식 탑다운.
  **주의**: 이는 문서상 확정 방향이고, 실제 `Domain/Combat`의 전투 중 이동은 아직 이 방식으로 마이그레이션되지 않았다 (아래 "현재 구현 상태" 참고).
- **화면 스케일링**: Unity 2D Pixel Perfect Camera(`com.unity.2d.pixel-perfect`), 타일 1칸이 화면상 약 20픽셀 정도가 되도록. 참고 해상도 720x1280(모바일 세로 + PC 겸용)으로 테스트했고 최종 값은 폴리싱 단계에서 조정한다.
- **에셋 전략**: 하이브리드. 지형/배경/환경 오브젝트는 무료 + 상업적 이용 가능한 커뮤니티 팩(CC0/CC-BY, 라이선스 원문 확인 필수)을 우선 쓰고, 주인공/주요 몬스터처럼 게임 정체성을 좌우하는 요소는 AI 이미지 생성으로 직접 제작해 섞는다. 무료 팩 요소의 퀄리티가 부족하면 그 요소만 AI로 재생성해 교체한다.

## 현재 구현 상태 (요약)

`client/Assets/Sapphire/`에 이미 상당 분량이 구현되어 있다 (2026-09-13 세션 산출물). 자세한 현황과 확정 방향과의 격차는 [`docs/HANDOFF.md`](docs/HANDOFF.md)를 본다. 핵심만 요약하면:

- 마을/집 탐험(`Domain/World/TileWorld`)은 이미 타일 격자 기반으로 동작한다. 다만 이동이 즉시 스냅이라 확정 방향의 짧은 tween은 아직 없다.
- 필드/던전 전투(`Domain/Combat/CombatWorld`)는 아직 연속 XY 자유 이동(8방향 아날로그)이다. 확정된 격자 스냅 방향으로 옮기는 작업은 별도 후속 구현이다.
- 화면 스케일링은 자체 제작 `PixelCameraFollow`(pixelsPerUnit=16)를 쓰고 있고, Unity 공식 `com.unity.2d.pixel-perfect` 패키지는 아직 도입되지 않았다.
- 에셋은 하이브리드 전략 적용 전이다. 현재 아트는 전부 이전 세션의 AI 생성/원본 참고 이미지다. 자세한 내용은 [`docs/ASSET_STATUS.md`](docs/ASSET_STATUS.md).

## 열기와 빌드

Unity Hub에서 `client` 폴더를 Unity 6000.5.9f1로 연다. 에디터 라이선스를 활성화한 다음 메뉴 또는 batchmode에서 `Sapphire.Editor.BuildGame.BuildAndTest`를 실행한다. 결과는 `client/builds/Windows/SapphireRPG.exe`다.

조작(현재 코드 기준): WASD/방향키 이동, J 공격, 1~4 스킬, Shift 회피, Q/F 물약, T AUTO, E 상호작용, Esc 메뉴. 화면 버튼도 같은 명령을 호출한다.

## 개발 프로세스

이 프로젝트는 Claude Code 세션에서 `lh2d-*` 계열 전용 에이전트(module-planner → combat-implementer/asset-specialist → qa-verifier)로 작업한다. 예전에 쓰던 Codex CLI 기반 병렬 에이전트(combat/campaign/presentation 동시 진행, `apply_patch`) 방식은 더 이상 쓰지 않는다. 자세한 내용은 [`AGENTS.md`](AGENTS.md).

검증은 컴파일과 도메인 규칙 테스트까지만 자동으로 하고, 실제 플레이 확인과 화면 판단은 사용자가 직접 한다.

## 문서 안내

| 문서 | 성격 | 용도 |
|---|---|---|
| `PLANNING_START_HERE.md`, `docs/planning/*.md` | 기획 (고정, 수정 금지) | 무엇을 만들지, 상태 계약, 실행 백로그, 에셋 명세, 후속 모델 지침 |
| `docs/DECISIONS.md` | 기술 결정 로그 | 왜 이렇게 정했는지, 언제 정했는지 |
| `docs/HANDOFF.md` | 구현 인계 | 실제 코드가 지금 어디까지 왔는지, 다음에 뭘 해야 하는지 |
| `docs/ASSET_STATUS.md` | 에셋 현황/전략 | 어떤 에셋을 무료 팩으로 쓰고 어떤 걸 AI로 만들지, 현재 상태 |
| `AGENTS.md` | 개발 프로세스 | 어떤 에이전트로 어떤 순서로 작업하는지 |
