# Lighthaven 2D Codex 에이전트 운영

이 폴더는 Codex와 다른 코딩 에이전트가 같은 품질 기준으로 작업하도록 만든 역할 프롬프트 모음이다. 계정 설정이나 플러그인 캐시는 포함하지 않는다.

## 기본 실행 순서

1. `module-planner.md`: 현재 모듈의 계약, 실패 조건, 배선, 검증법을 확정한다.
2. `design-reviewer.md`: 구현 전에 모순과 빠진 결정을 최대 6개로 압축한다.
3. `implementer.md` 또는 `asset-specialist.md`: 지정된 범위만 구현하거나 제작한다.
4. `qa-verifier.md`: 문서의 완료 조건과 실제 결과를 독립적으로 대조한다.
5. `integration-custodian.md`: 문서, 코드, 에셋 경로와 다음 작업의 인계를 갱신한다.

동일 파일을 여러 에이전트가 동시에 수정하지 않는다. 각 단계의 산출물이 끝난 뒤 다음 역할을 시작한다. 총괄 에이전트는 `AGENTS.md`, `docs/HANDOFF.md`, 해당 모듈 문서를 먼저 읽고 작업 범위를 역할 프롬프트에 명시한다.

## 공통 입력 양식

```text
역할: .codex/agents/<role>.md
현재 모듈: <module id and name>
허용 경로: <editable paths>
참조 문서: <spec and handoff paths>
완료 조건: <observable acceptance criteria>
금지 범위: <adjacent modules or files>
```

에이전트는 결과에 변경 파일, 실행한 검사, 실제 결과, 미검증 항목, 다음 역할에 필요한 입력을 남긴다.
