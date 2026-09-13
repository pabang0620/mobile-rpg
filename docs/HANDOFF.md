# 구현 인계

기준: planning v1 + `docs/PARALLEL_CONTRACT.md`. 2026-09-13 병렬 구현 통합 상태.

## 구현됨

- `Domain/Combat`: 60Hz XY 이동, 경계/장애물, 정규화 대각선, A*, 기본 3연계, 투사체 벽 차단, 네 스킬, 회피/무적, 보호막, 세 일반 AI, 정예, 2페이즈 보스/소환, 단일 처치 이벤트, AUTO.
- `Domain/Campaign` + `Infrastructure`: 프로필, 장비/스탯/강화/판매, 물약 구매/소비, 퀘스트 6개, XP/레벨, 필드 보상, 던전 원장/성공·실패, 체크섬 저장/백업/중복 거래 방지.
- `Application/GameApp`: 전투/캠페인 결합, 타이틀→마을→필드 또는 던전 3방→결과, 좌표 변환, UI snapshot/명령 facade.
- `Presentation`: 카메라 월드 렌더, HP/MP/XP, 보스/텔레그래프/투사체, 터치/키보드, 마을 서비스, 인벤토리/장비/퀘스트/상점/설정/결과 UI, safe area.
- `Editor/BuildGame`: 에셋 임포트, 규칙 검사, Boot 씬 생성, Windows 빌드.

## 검증 결과

- PASS: `CombatChecks.RunAll` — 대각 이동, 경계, 점멸 벽, 자원 거절, 보호막, 단일 처치, 벽 차단, 회피 경계, 예고, 보스 2페이즈, A*.
- PASS: `CampaignChecks.RunAll` — 저장 실패 롤백, idempotency, 퀘스트/장비, 던전 성공·실패·재로드, 레벨 상한, 파일 roundtrip, 손상 백업, schema 거절.
- PASS: GameApp+도메인 소스는 최소 Unity 코어 stub을 사용한 C# 컴파일. 경고 없이 정리됨.
- PASS: GameApp smoke — 타이틀→새 게임/마을→필드→귀환→던전 포기→귀환→저장.
- BLOCKED: Unity Editor/Player 컴파일과 실행. 외부 실행 로그에 `No valid Unity Editor license found`, return code 198. 이 환경은 WindowsStandaloneSupport만 설치됨.

## 에셋

신규 사용 배경: `TopdownTown.png`, `MoonshardField.png`, `TopdownDungeon.png`. 기존 투명 MageIdle/Goblin과 UI 아트는 런타임 참고 에셋이다. MageDirectional은 알파 실패로 제외. 정령/늑대/정예/보스 전용 캐릭터 아트와 오디오가 없어 현재 렌더 fallback은 Goblin이다. 따라서 시스템 MVP 코드는 갖춰졌지만 최종 아트 완료 상태가 아니다.

## 다음 실행

1. Unity 라이선스 활성화 후 `Sapphire.Editor.BuildGame.BuildAndTest` 실행.
2. 실제 컴파일 오류가 있으면 계약을 유지하며 수정하고, Boot 씬과 Windows Player에서 새 게임/필드/던전 완주.
3. 스크린샷으로 16:9/20:9/4:3 UI 검사.
4. P11 에셋 목록의 방향별 전용 적/보스/VFX/오디오를 생성·검수하고 fallback 제거.
5. Android 모듈과 실기기를 준비한 뒤 성능·멀티터치 검증.
