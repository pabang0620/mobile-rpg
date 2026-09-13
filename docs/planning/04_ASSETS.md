# 에셋 제작 명세

## 출처와 현재 상태

원본 AGENTS/UI/타이포그래피/에셋 manifest를 확인했다. 별도의 사용자 에셋 생성 프롬프트 원문은 찾지 못했으므로 아래 프롬프트는 새 제안이다. 원문 발견 시 비교하여 캐릭터 정체성과 사용자의 직접 요구를 우선한다.

기존 MageIdle/초상/포즈는 정체성 참고. 기존 횡스크롤 맵은 새 월드 맵으로 사용할 수 없다. UiChrome/WideButton/HudControls/한글 폰트는 새 HUD에서 실제 잘림/라이선스/형식을 검사한 뒤 재사용 가능하다. 새로 생성한 `MoonshardRPG/client/Assets/Game/Art/MoonshardField.png`는 배경 시안이며 충돌·가림·출구를 제작한 런타임 맵이 아니다.

## 제작 목록과 수량 기준

| ID 계열 | 범위 | 제작/검수 기준 |
|---|---|---|
| chr.mage | 4방향 × idle4/run6/attack6/hurt2/death4/cast4/dodge4 | 총120프레임, down/left/right/up, 기본공격 3타는 같은 동작+별도 VFX |
| enemy.goblin/spirit/wolf | 각 4방향 × idle4/move6/attack4/hurt2/death4 | 각80, 총240프레임, 정예는 명시된 팔레트/VFX 변형 |
| boss.guardian | 4방향 × idle4/move6/attack6/hurt2/death6 | 총96프레임, 패턴 추가 효과는 VFX로 분리 |
| npc.guide/shop/smith | 각 정면 idle4+초상1 | 12프레임+초상3, 정면 상호작용 연출 |
| world | 마을1/필드2/던전방3 | 총6구역, 바닥·가림 전경·충돌 데이터 분리 |
| props | 나무/바위/벽/폐허/수정/상자/포털/간판 등 | 재사용 16종, 월드 피벗·충돌 프리셋 연결 |
| icons | 스킬4/물약2/장비9/재료3/메뉴4/기능6 | 28종, 동적 숫자·프레임 미포함 |
| ui | 원형/사각/와이드/패널/바/선택·잠금 상태 | 기존 아트 재사용 검수, 필요한 부분만 신규 생성 |
| vfx | 기본탄3/비전탄/서리/점멸/보호막/피격/드롭/레벨업/보스3 | 13종, 루프/길이/정렬을 각 정의에 기록 |
| audio | 공격/명중/피격/회피/스킬/보스/거래/UI 등 SFX12, BGM3 | 합법적 생성 또는 사용권 확보, 루프·볼륨 검수 |

프레임 총합은 캐릭터 468프레임이며 VFX는 제외다. 한 번의 이미지 생성으로 전량 완성된다고 가정하지 않는다. 4방향의 해부·장식 일관성을 검수하고 방향별로 생산한다. 좌우 반전은 지팡이/장식의 비대칭이 깨지지 않는 경우에만 명세에 허용한다.

## 파이프라인

정체성 기준 원본 보기 → 방향 시트 시안 승인 수준의 자체 검수 → idle/run → attack/hurt/death → cast/dodge → Unity 실제 렌더 검수. 아트가 판정이나 캐릭터 위치를 바꾸지 않아야 한다. 원본 경계 잘림 문제가 있었으므로 시트 숫자보다 개별 프레임의 잘림과 피벗을 우선 검사한다.

PNG RGBA, sRGB, mipmap OFF, 프레임 256×256 제작 기준, feet pivot=(0.5,0.18), 기준 발 위치 고정. 프레임 사이 여백 최소8px. 엔진 아틀라스는 padding4/extrude2 출발점으로 축소 번짐 검수. 최종 PPU/필터는 실제 화면 샘플로 확정하고 모든 캐릭터에 통일한다. 픽셀아트가 아닌 손그림 스타일이므로 Point 필터를 무조건 계승하지 않는다.

최종 월드 맵은 장식이 이동 가능 면으로 오해되지 않도록 바닥과 장애물을 구분한다. 넓은 단일 배경을 사용해도 충돌/가림 레이어를 별도로 제작하고 포털 좌표를 검수한다. 자동 컬러 추출로 충돌을 추정하지 않는다. 캐릭터 정렬은 발의 worldY 기준, 가림 전경은 별도 sorting layer.

## 생성 프롬프트 템플릿

### 캐릭터 방향/동작

Use case: stylized-concept. Asset type: production 2D top-down action RPG sprite frames.
Reference: supplied original sapphire mage concept, identity reference only.
Subject: preserve the exact face, hairstyle, robe silhouette, staff proportions and sapphire/gold color relationships from the reference.
Camera: orthographic three-quarter top-down, direction {down|left|right|up}, identical camera and scale in every frame.
Action: {idle|run|attack|hurt|death|cast|dodge}; {frameCount} sequential distinct animation poses.
Composition: equal cells, whole body and staff inside each cell, constant foot anchor, consistent head/body size, generous transparent margin.
Style: polished hand-painted chibi fantasy game art with clean readable silhouette, matching the approved reference.
Background: genuinely transparent alpha. No text, watermark, frame numbers, UI, baked ground shadow, checkerboard, extra limbs, cell contamination.

실제 생성 전에 중괄호 항목을 구체적인 값으로 치환한다. 시트 하나에는 캐릭터 1종·방향 1개·동작 1개만 넣는다.

### 구역 맵

Use case: stylized-concept. Asset type: 2D top-down RPG environment, {zoneId}.
Scene: {zoneDescription}; walkable paths connecting {entry} to {exit}; visually distinct blocked perimeter and obstacles.
Camera: orthographic three-quarter top-down, no horizon, consistent scale with approved mage.
Style/palette: hand-painted fantasy, sapphire blue, muted teal, charcoal; {daylight|moonlight}.
Composition: {requiredLandmarks}; broad readable combat areas; no hidden decorative pits in walkable regions.
Constraints: no characters, UI, text, logos, watermark. Produce {ground|foreground props} layer; foreground props require transparent alpha.

### 아이콘

Use case: stylized-concept. Asset type: standalone RPG UI icon, {iconId}.
Subject: {skill/item meaning}, readable at 48 logical pixels, matching sapphire/charcoal UI.
Composition: centered single glyph, transparent background, internal padding 12%, no button border, labels, numbers or watermark.

## 자동 검사와 사람 눈 검수

자동: 파일 존재/해시/치수/알파 극값/빈 프레임/셀 경계 접촉/기준 발 위치 편차/누락 ID. 자동 알파 검사는 체크무늬가 그림으로 포함됐는지 완전히 판별하지 못하므로 어두운·밝은 배경 합성에서 육안 확인한다.

육안: 방향 정합, 발 미끄러짐, 지팡이 길이, 캐릭터 정체성, 실루엣, 잘림, 루프 점프, 실제 모바일 크기 가독성. 실패는 재생성/수정 후 재검수. 단순 알파 존재만으로 PASS 금지.

manifest 필드: assetId, sourcePath, finalPath, promptPath, referencePaths, generator, createdAt, sha256, dimensions, frames, direction, pivot, licenseOrTermsNote, qaStatus, usedBy. 생성은 프로젝트 내부 최종 사본을 사용하고 기본 생성 폴더만 참조하지 않는다. 이미지 생성 스킬의 기본 내장 도구를 사용한다. 오디오 생성 도구는 아직 확인하지 않았으므로 제공 가능성을 구현 단계에서 확인하고, 확보 전에는 AUDIO_PENDING 상태로 남긴다.
