# 에셋 배선 상태

## 캐릭터 불변 조건
은회색 장발, 청회색 눈, 큰 남색 후드, 해진 다층 로브, 고금 자수, 사파이어 보석, 검정/금 부츠, 푸른 결정 지팡이를 유지한다. 원본은 `generated-images/source-reference/mage_original_concept.png`다.

## 현재 연결됨
- `SapphireTown.png`: 로그인 배경과 실제 마을 허브.
- `SapphirePlatformMap.png`: 달빛 회랑 횡스크롤 전장. 지면 y=214, 발판 y=414/473/395.
- `MageIdle.png`, `MagePortrait.png`, `MagePose02/06/09/14.png`: 법사 표시와 공격 핵심 포즈.
- `Goblin.png`: 첫 전투 적.
- `MageSkills.png`, `MainMenuIcons.png`, `HudControls.png`: 스킬, 메뉴, 조작 글리프.
- `UiChrome.png`: 평면형 반투명 원형/사각/바/배지 프레임.
- `WideButton.png`: 로그인·서버·캐릭터·출정 공통 와이드 버튼.
- `NotoSansCJKkr-Regular.otf`: 모든 한글 UI 폰트. OFL 라이선스 동봉.
- `hud-redesign-v2.png`: 최신 전투 HUD 승인 기준 시안.

`MoonCourtyard.png`와 `MenuPanel.png`는 과거 시안 호환용 보관 에셋이며 현재 핵심 화면 외형에 사용하지 않는다. 크기, 형식, SHA-256은 `ASSET_MANIFEST.json` schema 2가 기준이다. 빌더는 16개 이미지를 임포트하고 실제 투명 에셋 11개를 검사한다.

## 생성 또는 보수 필요
1. 법사 idle/run/attack/hit/down/get-up/death 전체 프레임을 동일 발 기준선으로 완성한다.
2. 스킬 VFX를 캐릭터와 분리한 RGBA 시트로 제작한다.
3. 고블린 전체 행동과 추가 일반 적 2종을 만든다.
4. 마을 NPC, 상호작용 표식, 상점/강화/퀘스트 화면용 글리프와 카드 에셋을 만든다.
5. 배경을 전경, 전투 바닥, 중경, 원경 시차 레이어로 분해한다.

모든 결과는 외부 절대경로를 런타임에서 참조하지 않고 `client/Assets/Game` 안으로 복제한 뒤 `tools/update_asset_manifest.py`로 매니페스트를 갱신한다.
