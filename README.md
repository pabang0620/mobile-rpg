# Lighthaven 2D

사파이어 메이지를 주인공으로 하는 Unity 2D 횡스크롤 액션 RPG 프로토타입이다. 로그인, 서버 선택, 캐릭터 선택, 마을, 자동사냥 전투 흐름이 실제로 연결돼 있다.

## 실행

Unity 6000.5.9f1로 `client`를 열거나 `Run-Lighthaven2D.cmd`를 실행한다. Windows 빌드는 저장소에 포함하지 않으며 `Lighthaven2D.Editor.BuildGame.BuildAndTest`로 생성한다.

## 현재 조작

- A/D 또는 방향키: 좌우 이동
- Space: 점프
- Shift: 구르기
- J: 기본공격
- 1~4: 스킬
- T: 자동사냥
- R: 사망 후 재도전

## 문서 진입점

- `AGENTS.md`: 모든 구현자가 지켜야 할 현재 방향
- `docs/HANDOFF.md`: 구현 및 검증 상태
- `docs/MODULE_ROADMAP.md`: 모듈 순서
- `docs/UI_LAYOUT_SPEC.md`: HUD 좌표와 레이어
- `docs/UI_TYPOGRAPHY_SPEC.md`: 폰트와 패딩
- `docs/modules/`: 모듈별 기술 설계

생성 에셋은 `client/Assets/Game/Art`에 실제 런타임 사본을 두고 `generated-images`에 제작 결과와 기준 시안을 보관한다.
