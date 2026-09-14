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

**2026-09-14 정리로 이전 구현(2026-09-13 세션 산출물)은 전부 제거됐다.** 아날로그 자유이동 기반 게임플레이 C# 코드 전체(`Domain/`, `Application/`, `Presentation/`, `Editor/BuildGame.cs`, `Tests/`)와 AI 생성물이 아니고 UI도 아닌 플레이스홀더/커뮤니티 에셋을 삭제했다. 롤백 지점은 git 태그 `pre-cleanup-2026-09-14`. 자세한 내용은 [`docs/HANDOFF.md`](docs/HANDOFF.md)를 본다.

`client/Assets/Sapphire/`는 현재 빈 씬(`Scenes/Boot.unity`) 1개, 폰트, 정리된 `Art/`(AI 생성 Mage 아트 + UI 에셋만)만 남은 상태다. **격자 스냅 이동 기반 재구현이 필요하다** - 위 "확정된 기술 방향"에 맞춰 처음부터 새로 만든다. 에셋 현황은 [`docs/ASSET_STATUS.md`](docs/ASSET_STATUS.md).

## 열기와 빌드

Unity Hub에서 `client` 폴더를 Unity 6000.5.9f1로 연다. 빌드 파이프라인(`Editor/BuildGame.cs`)은 이전 코드 구조에 종속적이라 함께 삭제됐으므로, 재구현 시 새로 만들어야 한다(참고할 만한 텍스처 임포트 로직은 `docs/HANDOFF.md`에 메모로 남겨뒀다).

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
