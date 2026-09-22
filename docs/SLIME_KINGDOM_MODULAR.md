# 슬라임 왕국 재구성

2026-09-23. 기존 SlimeKingdom 씬을 새 빈 씬에서 다시 생성하는 5단계 작업.

## 지역 구성

- 40×30칸, 한 칸 64px/1 Unity unit. 시작점 (19,2), 마을 복귀 (19,0).
- 남쪽 완충 지대 → 굽은 흙길 → 중앙 공터와 사냥터.
- 불규칙한 강을 건너는 서쪽 다리 (12,13~15), 동쪽 다리 (27,14~16).
  각 다리 양끝은 육지에 연결하고, 물 위의 다리 칸만 통행 가능하다.
- 북서쪽 고목과 상자 샛길, 북쪽 보스 공터, 북동쪽 계단 위 유적.
- 유적은 (31,19~21) 계단을 통해 진입한다. 나머지 고지대 테두리는 충돌로 막는다.
- 일반 슬라임 10마리와 보스 1마리. 상자/성소는 현재 안내 상호작용이며 신규 보상 시스템은 포함하지 않는다.

## 제작과 실행 연결

`SapphireSceneBuilder.RebuildSlimeKingdom`으로 해당 플레이 씬만 재생성한다.
일반 `BuildEverything`에서도 같은 `SlimeKingdomTerrainBuilder.Build`를 사용한다.
씬 이름은 `SlimeKingdom`을 유지하므로 기존 VillageHub 입구가 그대로 연결된다.

지형은 Modular64의 G/PA/PV/ET/C/SH/S/W/F/B/D 에셋을 사용한다.
Water, Foam, Ground, Path, Shadow, Elevated Top, Cliff, Stairs, Bridges,
Decorations, Collision을 분리한다. 기존 전체 배경이나 SeamlessV4 타일은 사용하지 않는다.
슬라임 캐릭터 스프라이트만 기존 몬스터 에셋을 재사용한다.

Foam은 4프레임/0.2초로 재생하며 통행 판정에 관여하지 않는다.
플레이어와 몬스터는 같은 런타임 GridMap을 공유한다. 이동/죽음은 충돌 Tilemap과
이 GridMap을 함께 갱신하여 죽은 몬스터 자리에 보이지 않는 장애물이 남지 않게 한다.
몬스터 초기 스탯과 위치는 직렬화하고 씬 로드 시 Awake에서 복원한다.

## 검증 산출물

- `verification/slime-kingdom-layout-validation.txt`: 스폰, 다리, 계단, 상호작용 접근 BFS.
- `verification/slime-kingdom-saved-scene.txt`: 저장 후 재로드된 씬의 참조 검사.
- `verification/slime-kingdom-overview.png`: 실제 씬 렌더러로 캡처한 전체 배치.
- `verification/slime-kingdom-entrance.png`: 1280×720 입구 렌더링.
- `verification/slime-kingdom-play-smoke.txt`: PlayMode 초기화·점유 갱신 확인.

생성된 검증 보고서가 존재하고 해당 실행 로그가 성공한 경우에만 검사 통과로 판단한다.

2026-09-23 검증 완료: 독립 리뷰 LGTM, Unity 재생성 종료 코드 0, 867칸 연결,
저장 씬의 몬스터 11개/정적 상호작용 4개와 Modular64 참조 검사 통과.
PlayMode 초기화·포말 재생·이동 거절/허용·사망 후 점유 해제 PASS.
전체와 입구 실제 렌더링에서 바닥 연결과 물 위 다리 배치를 확인했다.
나무/유적은 현재 모듈 라이브러리의 단순한 표현을 유지한다. 아트 최종 완성도를 의미하지 않는다.
실행 파일은 이번 작업에서 재빌드/실행하지 않았으므로 기존 EXE에는 새 맵이 아직 반영되지 않았다.
