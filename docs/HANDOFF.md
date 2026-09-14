# 구현 인계

기준: `docs/planning/*.md`(기획, 불변) + `docs/DECISIONS.md`(기술 방향). 2026-09-14 코드/에셋 정리 이후 상태로 재정리.

## 2026-09-14: 이전 구현 전체 제거 (현재 상태)

`client/Assets/Sapphire/`의 아날로그 자유이동 기반 게임플레이 C# 코드 전체와, AI 생성물이 아니고 UI도 아닌 플레이스홀더/커뮤니티 에셋을 사용자 지시로 전부 제거했다. 롤백 지점은 git 태그 `pre-cleanup-2026-09-14`(origin에도 푸시됨).

**현재 코드는 완전히 빈 상태다.** 이전 세션(2026-09-13)에서 구현했던 아래 내용은 전부 삭제되었고, 격자 스냅 이동 기반(`docs/DECISIONS.md` 2026-09-14 결정)으로 처음부터 다시 구현해야 한다:

- `Domain/Combat`(연속 XY 자유 이동 전투), `Domain/Campaign`(프로필/장비/퀘스트), `Domain/World`(TileWorld)
- `Application/GameApp`(파사드), `Infrastructure/FileProfileStore`(세이브 - `Domain/Campaign`에 의존해 함께 제거)
- `Presentation/`(WorldRenderer, GamePresenter, GameInput, Exploration/* 전부)
- `Editor/BuildGame.cs`(빌드 파이프라인), `Tests/`(CampaignChecks·CombatChecks·WorldChecks)

남은 것은 `client/Assets/Sapphire/Scenes/Boot.unity`(빈 씬 1개만 유지 - Unity가 프로젝트를 열 때 씬 파일 자체가 있어야 하므로 최소한으로 남김), `Fonts/`, 정리된 `Art/`(아래 참고)뿐이다.

**주의**: `Boot.unity`는 이전 `GameApp`/`GamePresenter` 컴포넌트를 참조하던 GameObject를 그대로 담고 있을 수 있다. 해당 스크립트가 삭제되었으므로 Unity 에디터에서 열면 "Missing Script" 경고가 뜰 수 있다 - 이는 예상된 상태이며, 새 구현에서 씬을 다시 구성하면서 정리하면 된다.

### 삭제된 빌드 파이프라인(`Editor/BuildGame.cs`)의 재사용 가치 있는 로직 (참고용 메모)

새 구현에서 텍스처 임포트 설정을 다시 자동화할 때 참고할 수 있는 원본 로직(옛 `ConfigureArt()` 메서드):

```csharp
// Art/ 하위 모든 Texture2D에 대해:
// - textureType = Default, mipmapEnabled = false, wrapMode = Clamp, filterMode = Bilinear
// - textureCompression = CompressedHQ
// - alphaIsTransparency = importer.DoesSourceTextureHaveAlpha()  // 알파 채널 실측 검사
importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
```

`DoesSourceTextureHaveAlpha()`가 실제 알파 채널 유무를 판정하는 API 호출이다 - `docs/ASSET_STATUS.md`에 남긴 "AI 생성 에셋은 알파 채널을 실측해서 검수해야 한다"는 교훈(`MageDirectional.png` 사례)과 연결되는 도구다. 새 임포트 파이프라인을 다시 만들 때 이 API를 그대로 재사용하면 된다.

## 에셋 현황

`docs/ASSET_STATUS.md` 참고. 이번 정리로 커뮤니티/무료팩 플레이스홀더(고블린 스프라이트, MagicMissile 등)와 시안 단계 배경 다수를 제거했고, AI 생성 캐릭터 아트(Mage 관련)와 UI 에셋은 그대로 유지했다.

## 다음 실행

1. 엔진/기획 방향(`docs/planning/*.md`, `docs/DECISIONS.md`)에 맞춰 격자 스냅 이동 기반으로 처음부터 재구현. 이전 구현의 아키텍처·코드 구조는 참고하지 않는다(기획·수치만 승계).
2. `com.unity.2d.pixel-perfect` 패키지 도입, 참고 해상도 720x1280 기준 설정.
3. 신규 구현 착수 전 에셋 하이브리드 전략(`docs/ASSET_STATUS.md`) 적용 여부 판단.
4. Unity 라이선스 활성화 후 Player 빌드/플레이 검증은 사용자가 직접 수행.
