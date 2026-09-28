# 구현 인계 (2026-09-18)

## 최신 상태 — 2026-09-28 품질 개선 1단계

계획과 원인 분석은 `docs/QUALITY_PLAN.md` 참고. 이 세션에는 Unity Editor가 없어서 Domain(mono 컴파일과 테스트)까지만 검증했다.
Presentation/Editor 변경은 **Unity에서 컴파일, `BuildEverything`, `ApprovedEnvironmentBuild.Build` 재실행과 손테스트가 필요하다.**
- 스킬 VFX 시트 5장 재배치(`tools/art_qa/clean_vfx_frames.py`): 칸 경계 침범과 헤이즈 제거.
- 전투 규칙 Domain화: 스킬 마나·쿨다운, 거절 코드, 치명타 난수 주입.
- 캐릭터와 몬스터의 발 기준선, 이펙트 기준점 통일.
- 캐릭터 생성 "돌아가기"와 메뉴 배경 닫기 버튼을 Awake에서 등록.

## 최신 상태 — 2026-09-28 수변 소재 V3

잔디·물·암벽을 새 소재로 재생성했다. 잔디 내부 변형 범위를 넓히고 물 변형 12개를 사용한다.
SB001 토양 레이어로 지면 아래 물 틈을 차단하고, 22–30px 암벽 하단에 포말을 연결했다.
독립 검토 LGTM, 전체 타일/씬 생성 종료 0, RGBA 접합 및 867칸 연결 검사 통과.
실제 화면은 verification/slime-kingdom-shoreline.png, 소재/프롬프트는 SHORELINE_V3.md 참고.
일부 소재 반복감은 남는다. Windows 실행 파일은 아직 V2이므로 다음 실행 요청 시 1회 빌드한다.
이번 요청에서 게임 실행/커밋/푸시는 하지 않았다.

## 최신 상태 — 2026-09-26 승인 시안 타일 V2

승인 시안 기반 신규 소재/오브젝트를 Modular64 타일로 변환하고 슬라임 왕국에 적용했다.
TS08 수변벽 BK001~BK047을 추가했으며 물 점유 47마스크로 모서리/연결부를 처리한다.
다리 중앙과 양끝은 분리된 원본 스트립, 나무/유적/꽃/바위는 알파 성분 추출을 사용한다.
`ApprovedEnvironmentBuild.Build`가 타일 전체와 실제 씬을 의존 순서대로 재생성한다.
코드 리뷰와 Unity 생성/저장 씬 검사 통과. 실제 렌더는 verification/slime-kingdom-overview.png.
원본/프롬프트/재생성/표현 한계는 SLIME_ART_V2.md 참고.
Windows 빌드도 2026-09-26 14:07 성공(종료 0). 9월 28일 재개 시 로그와 데이터 갱신을 확인했다.
현재 builds/Windows/SapphireRPG.exe로 실행하면 V2 맵을 사용한다. 실행/커밋/푸시는 미수행.

## 최신 상태 — 2026-09-23 슬라임 왕국 재구성

5단계 실제 맵 통합 완료. SlimeKingdom을 40×30 Modular64 타일맵으로 새로 생성했다.
물 위 다리 2개, 계단 전용 고지대 진입, 북서 상자 샛길, 북쪽 보스 공터를 구성했다.
일반 슬라임 10마리와 보스 1마리는 MonsterController로 초기화하며 정적 대화 목록에 등록하지 않는다.
플레이어/몬스터가 같은 GridMap을 사용하고 이동·사망 시 점유를 갱신한다.
독립 코드 검토 LGTM, Unity 씬 생성 성공, 867칸 연결 검사 및 저장 씬 재로드 검사 통과.
PlayMode에서 몬스터 초기화, 포말 애니메이션, 이동 거절/허용과 사망 후 충돌 해제를 확인했다.
실제 전체/입구 렌더링은 verification/slime-kingdom-*.png, 상세는 SLIME_KINGDOM_MODULAR.md.
상자와 성소는 안내 상호작용만 제공한다. 플레이어 EXE는 이번에 빌드/실행하지 않았다.
아래 이전 날짜의 '실제 맵 미적용/슬라임은 대화 오브젝트' 내용은 이 씬에 대해서는 대체된다.

## 2026-09-22 추가: Modular64 지형 1차

잔디/흙길 TS01과 142개 Tile 에셋, 독립 30×30 `ModularGroundTest` 씬을 추가했다.
`Sapphire.EditorTools.ModularTiles.ModularGroundBuilder.Build`로 재생성한다.
Unity 6000.5.9f1 배치 종료 0, 262,144개 RGBA 경계 비교, 24개 변형 외곽,
Sprite 재로드 검사 통과. 직선 경계에 반복되던 홈과 대칭 소재 반복을 보완했다.
실제 마을/슬라임 킹덤 씬은 아직 이 타일셋으로 교체하지 않았다. 플레이어 빌드/실행도 하지 않았다.
다음 단계는 고지대/절벽/그림자·계단, 물/물결, 장식·다리 제작 후 맵 통합이다.
정확한 범위·파일·사용법·프롬프트는 `MODULAR_TILESET_64.md`, `MODULAR_TILESET_PROMPTS.md` 참조.

같은 날 후속으로 TS02 고지대/남쪽 절벽과 TS04 그림자를 추가했다. `ModularElevationBuilder.Build`로
59개 ET/C Tile과 8개 SH Tile, `ModularElevationTest` 씬을 재생성한다. 1단과 실제 2칸 낙차를
검수하며 Unity 배치 생성·직렬화 재로드·연결 검사를 통과했다. 계단/물/물결/장식/실제 맵 적용은 후속이다.

이후 Modular64 후속 1~4단계도 완료했다. 남향 계단 `S001-S012`, 물 `W001-W012`, 해안 포말
`F001-F188`(47형×4프레임, 0.2초), 소형 장식 `D001-D032`, 대형 장식 `D101-D114`, 다리
`B001-B012`과 각 독립 검증 씬을 생성했다. `ModularWorldConnectionTest`는 모든 지형 계층의
순서, 동일 레이어 중복 금지, 물 위 다리와 마른 육지 양 끝 접점을 함께 검증한다. 포말이 사각
격자처럼 보이던 내부 육지 판정도 수정했다. 실제 게임 시작 씬/슬라임 맵 적용과 충돌은 아직 후속이다.

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향).
이 문서는 과거 이력이 쌓여 너무 길어졌던 기존 문서를 아카이브(`HANDOFF_archive_20260916.md`)하고, 현재 코드가 실제로 어떤 상태인지 요약합니다.

## 현재 아키텍처 및 구현 상태 (완료된 부분)

기존 아날로그 자유이동 로직을 전면 롤백한 뒤(2026-09-14), **이동 조작감 및 시각적 프레젠테이션 기반**이 완성되었습니다.
- **코드 기반 씬 생성**: 유니티 에디터 편집 없이 `SapphireSceneBuilder.BuildEverything()`로 5개 씬(로그인, 캐릭터선택, 생성, 마을, 슬라임킹덤) 전체를 절차적으로 생성합니다.
- **그리드 스냅 이동 (포켓몬 스타일)**: 1칸 이동 0.343s, 스텝 간 일시정지 0.04s. 방향 전환 시 첫 입력은 제자리 회전만 처리(`GridMoveInputBuffer`). 최신 입력 우선순위 스택 적용.
- **UI 및 캐릭터 플로우**: 오딘 스타일 우측 서랍식 메뉴, 하단 부채꼴 스킬 휠 메뉴, 비대칭 9-slice 프리미엄 로그인/캐릭터 생성 UI 연동 완료. JSON 로컬 기반 계정/캐릭터 세션 관리.
- **비주얼 연출**: 알파 바운딩 박스 실측을 통한 정확한 발 피벗(`CharacterFootPivotCalculator`), 스킬 사용 시 3프레임(Windup-Apex-Recovery) 액터 모션(`SkillMotionPlayer`), 스킬 VFX 프레임별 피벗 보정 렌더링.

## 게임플레이 구현 상태 (2026-09-28 정정)

이전 문서의 "전투 로직이 한 줄도 없다"는 서술은 9월 중순 이후 사실이 아니다. 현재 있는 것과 없는 것:

- **있음**: HP/MP/EXP/레벨업(`Domain/Combat`), 몬스터 AI와 보스, 피격과 사망, 아이템 드롭 연출, 게임오버 후 마을 복귀.
- **2026-09-28 추가**: 스킬별 마나·쿨다운·배율 `Domain/Skills/SkillCombatCatalog`, 시전 판정 `SkillCastRules`(Accepted/거절 코드, 거절 시 자원 불변), 치명타 규칙 `CombatEngine.ResolveHit`(난수 주입), 경험치 보상 `CombatRewards`.
- **없음**: 퀘스트 시스템(P08), 인벤토리와 장비 효과, Application 계층(`GameApp`), 몬스터 규칙의 Domain화(몬스터 치명타 15%는 아직 `MonsterController`에 있음), 스킬 버튼 쿨다운 표시.

## 다음 작업 목표

`docs/QUALITY_PLAN.md`의 2단계(이펙트 연출: 가산 블렌딩, 스킬별 타이밍, 쿨다운 UI)부터 진행한다. 이후 3단계는 마을 Modular64 전환이다.
