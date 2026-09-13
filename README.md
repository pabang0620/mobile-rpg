# Sapphire: 달빛의 균열

Unity 6000.5.9f1용 탑다운 2D 액션 RPG 세로 슬라이스다. 원본 Lighthaven 기획의 사파이어 메이지와 UI 방향을 계승하고, 이동과 월드를 초기 휴대용 액션 RPG 감각의 XY 자유 이동으로 새로 설계했다.

## 현재 구현

- 타이틀, 새 게임/이어하기, 마을 허브, 필드 2개, 3방 던전과 2페이즈 보스
- 8방향 이동, 3연계 마법탄, 스킬 4개, 회피, HP/MP 물약, 필드 AUTO
- 근접/원거리/돌진 적, 정예, 보스, 고정 60Hz 규칙과 A* 우회
- 퀘스트 6개, 레벨 1~5, 장비 9종/3슬롯, 구매·강화·판매
- 체크섬 JSON 저장, 백업 복구, revision/idempotency 거래, 던전 임시 원장
- PC 키보드와 모바일 가상 입력이 동일한 GameApp 명령 사용

## 열기와 빌드

Unity Hub에서 `client` 폴더를 Unity 6000.5.9f1로 연다. 에디터 라이선스를 활성화한 다음 메뉴 또는 batchmode에서 `Sapphire.Editor.BuildGame.BuildAndTest`를 실행한다. 결과는 `client/builds/Windows/SapphireRPG.exe`다. 이 컴퓨터는 작성 시 Unity 라이선스가 활성화되지 않아 실제 Player 빌드는 아직 생성되지 않았다.

조작: WASD/방향키 이동, J 공격, 1~4 스킬, Shift 회피, Q/F 물약, T AUTO, E 상호작용, Esc 메뉴. 화면 버튼도 같은 명령을 호출한다.

## 검증

순수 규칙 검사는 `client/Assets/Sapphire/Tests`에 있다. 전투와 캠페인 검사는 Mono 실행으로 통과했다. `verification/unity-test-escalated.log`는 코드 실패가 아니라 `No valid Unity Editor license found`로 중단된 기록이다. `docs/HANDOFF.md`에서 완료/미검증 범위를 확인한다.

기획과 후속 모델 계약은 상위 `../PLANNING_START_HERE.md`와 `../planning/`에 있다.
