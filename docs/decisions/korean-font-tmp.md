# 기술 결정: TextMeshPro 한글 폰트 (NotoSansKR 동적 폰트 에셋)

작성 2026-08-05 · 상태: **적용 완료, Play 검증 완료**

## Problem — 문제

시나리오 UI 를 TextMeshPro 로 붙이고 Play 를 눌렀더니 한글이 전부 두부(□)로 나온다.

원인은 단순하다. TMP Essential Resources 가 기본으로 딸려 오는 폰트는 **LiberationSans** 하나고, 여기엔 한글 글리프가 없다. 이 게임의 텍스트는 전부 한글이다 — 시나리오 노드 43개의 title/content 가 통째로 안 보인다는 뜻이다.

선택해야 할 게 세 가지였다.

1. **어떤 폰트를 쓸 것인가** — 라이선스가 걸린다. 이 저장소는 GitHub 에 올라간다
2. **정적 아틀라스 vs 동적 아틀라스** — 한글은 완성형만 11,172자다
3. **폰트 파일을 저장소에 커밋할 것인가** — 한글 TTF 는 5~13MB

## Action — 한 것

### 1. 폰트: NotoSansKR-VF (SIL Open Font License)

Windows 에 동봉된 후보 두 개를 비교했다.

| 폰트 | 크기 | 라이선스 | 판단 |
|---|---|---|---|
| `malgun.ttf` (맑은 고딕) | 13.4MB | Microsoft 독점 | **탈락** — 저장소 재배포가 라이선스 위반 소지 |
| `NotoSansKR-VF.ttf` | 10.4MB | SIL OFL | **채택** — 재배포·임베딩 자유 |

맑은 고딕이 한국어 UI 의 기본값이고 파일도 익숙하지만, **공개 저장소에 커밋하는 순간 재배포다.** OFL 폰트를 쓰면 이 고민 자체가 사라진다.

Noto Sans KR 은 가변 폰트(VF)라 TMP 가 못 다룰 위험이 있었는데, 실제로 만들어 보니 동적 아틀라스가 정상 생성됐고 렌더링도 문제없었다.

### 2. 아틀라스: 동적(Dynamic)

```csharp
TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
    1024, 1024, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
```

정적 아틀라스로 한글을 굽는 건 현실적이지 않다. 완성형 11,172자를 SDF 로 미리 구우면 아틀라스가 수십 MB가 되고, 대부분은 안 쓰는 글자다.

동적 모드는 **실제로 화면에 나온 글자만** 런타임에 아틀라스로 굽는다. 1024×1024 한 장으로 시작해 모자라면 늘린다(`enableMultiAtlasSupport`). 이 게임처럼 텍스트 총량이 고정(시나리오 JSON 안의 글자만)이고 그 안에서도 실제 쓰는 글자는 수백 자 수준이면 정확히 맞는 선택이다.

### 3. 커밋 정책: TTF 는 LFS, 폰트 에셋은 그냥 커밋

- `.gitattributes` 에 `*.ttf` / `*.otf` LFS 규칙 추가 — 10MB 바이너리가 히스토리에 raw blob 으로 쌓이면 안 된다 (이미 `docs/troubleshooting/lfs-after-first-commit.md` 에서 배운 것)
- `NotoSansKR SDF.asset` (TMP 폰트 에셋)은 일반 커밋

**폰트 에셋은 런타임에 커진다.** 동적 아틀라스가 새로 구운 글리프를 에셋에 써 넣기 때문에, Play 한 번 돌리면 `+6,700줄` 짜리 diff 가 뜬다. 이걸 커밋하면 이미 쓴 글자는 미리 구워진 상태로 배포되고, 안 하면 매번 런타임에 굽는다. **커밋하는 쪽을 택했다** — diff 소음보다 첫 표시 프레임의 안정성이 낫다.

대신 **새 글자가 나올 때마다 이 에셋이 계속 변경된다**는 점은 알고 있어야 한다. 시나리오 텍스트를 고칠 때마다 따라붙는다.

### 검토했다가 안 택한 것

- **TMP_Settings 의 기본 폰트를 교체** — 프로젝트 전역 설정을 건드리는 것. 지금 텍스트 3곳(제목·본문·버튼 라벨)에 명시 할당하는 게 더 명확하고 되돌리기 쉽다
- **웹폰트 다운로드(Pretendard 등)** — 더 예쁜 후보가 있지만 네트워크 의존이 생기고, 지금 필요한 건 "한글이 보이는 것"이지 타이포그래피가 아니다

## Result — 결과

- 한글 글리프 미해결 **0개** (`TMP_FontAsset.HasCharacter` 로 확인)
- 8개 노드 실제 캡처에서 깨짐 0
- 폰트 파일 10.4MB, LFS 포인터로 커밋됨

### 되돌리는 법

폰트가 마음에 안 들면 `Assets/_Project/Art/Fonts/` 에 새 TTF 를 넣고 같은 방식으로 폰트 에셋을 만든 뒤, 씬의 TMP 컴포넌트 3곳(Title / Content / ChoiceButton 프리팹의 Label)에서 Font Asset 만 갈아끼우면 된다. 코드 변경 없음.

## 이력서 한 줄

> TextMeshPro 기본 폰트에 한글 글리프가 없어 UI 전체가 표시되지 않는 문제를, OFL 라이선스 폰트 선정 + 동적 SDF 아틀라스 구성으로 해결하고, 폰트 바이너리를 Git LFS 로 분리해 저장소 비대화를 함께 차단했습니다.
