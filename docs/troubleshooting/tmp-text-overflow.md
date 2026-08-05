# TMP 텍스트가 패널을 넘치는데 눈에 안 띄던 문제

작성 2026-08-05 · **상태: 해결**

## 문제 상황

시나리오 UI 를 붙이고 Play 로 8개 노드를 캡처해 눈으로 확인했다. **화면상으로는 멀쩡해 보였다.** 한글도 잘 나오고 버튼도 정상이고 잘린 글자도 없었다.

그런데 수치로 재보니 두 군데가 rect 를 넘고 있었다.

```
n30 | title 41/34  OVERFLOW | content 107.5/97  OVERFLOW @22.4pt
n1  | title 41/34  OVERFLOW | content 86/97  ok
n7  | title 41/34  OVERFLOW | content 86/97  ok
...   (title 은 전 노드에서 넘침)
```

- **Title**: rect 높이 34px, 34pt 폰트가 필요로 하는 높이 41px. **모든 노드에서** 7px 초과
- **Content**: 본문 최장 노드 n30 이 rect 97px 에 107.5px 로 렌더. 10.5px 초과

## 왜 눈에 안 띄었나

TMP 의 `overflowMode` 기본값이 **`Overflow`** 다. 이름 그대로 rect 를 넘겨도 **자르지 않고 그냥 그린다.**

즉 "글자가 잘리는" 증상이 안 나온다. 대신 이렇게 샌다.

- Title 은 rect 위로 삐져나가는데, 마침 위에 TextPanel 여백이 있어서 배경 안에 들어갔다. **제목이 두 줄이 되는 순간 패널 밖으로 샌다**
- Content 는 rect 아래로 삐져나가고, Content rect 바닥이 TextPanel 바닥에서 10px 위였다. 10.5px 초과 → **마지막 줄이 어두운 패널을 벗어나 3D 씬 위에 그려진다**. 캡처에서는 경계선에 딱 붙어 있어 "빠듯하네" 정도로 보였다

캡처만 봤으면 그냥 넘어갔을 문제다. 실제 rect 높이와 `GetPreferredValues` / `textBounds.size.y` 를 비교해서 잡았다.

## 시도한 것들

### 오토사이즈가 구해줄 거라 기대했다 — 아니었다

Content 는 `enableAutoSizing = true`, `fontSizeMin = 14`, `fontSizeMax = 24` 로 설정돼 있었다. 넘치면 알아서 줄어들 줄 알았다.

n30 에서 24 → **22.4pt 까지만 줄고 멈췄다.** 그 상태로도 107.5 > 97 로 여전히 넘친다.

"프레임이 모자라 아직 수렴 중인가" 싶어 강제로 6번 돌려봤다.

```
pass 0: fontSize=22.40  renderedH=107.5  rectH=97.1  preferredH=115.2
pass 1: fontSize=22.40  renderedH=107.5  rectH=97.1  preferredH=115.2
...
pass 5: fontSize=22.40  renderedH=107.5  rectH=97.1  preferredH=115.2
```

**수렴이 아니라 정착이다.** 22.40 에서 완전히 멈춰 있고 더 줄일 생각이 없다. 하한 14pt 는 닿지도 않았다.

`isTextOverflowing` 도 `False` 를 반환했다 — `overflowMode = Overflow` 에서는 "넘칠 수 없다"고 보기 때문인 듯하다. **이 플래그를 믿으면 안 된다.**

### 폰트 크기를 줄이는 건 선택지에서 뺐다

가장 쉬운 해결은 타이틀 34pt / 본문 24pt 를 낮추는 것이다. 하지만 그건 **가독성 취향 문제**고, 결함 수정으로 슬쩍 끼워 넣을 변경이 아니다. 사용자가 정할 몫이라 판단해 손대지 않았다.

## 최종 해결법

**폰트는 그대로 두고 rect 를 키웠다.** 하단 1/3(`ScenarioPanel`)이라는 제약은 유지하고, 그 안에서 버튼 행의 몫을 텍스트 패널로 옮겼다.

| | before | after |
|---|---|---|
| ChoiceRow | `0.04..0.42` (111px) | `0.03..0.31` (82px) |
| TextPanel | `0.45..0.97` (152px) | `0.34..0.99` (190px) |
| Title | `0.72..1.0` (34px) | `0.72..1.0` (50px) |
| Content | `0..0.72` (97px) | `0..0.72` (128px) |

결과 (1918×879 기준):

```
n30 | title 41/50  ok | content 115/128 @24.0pt  ok | btns [1L 22.0pt r=26/68]
n1  | title 41/50  ok | content 86/128  @24.0pt  ok
... 8개 노드 전부 ok
```

본문이 **오토사이즈를 안 쓰고도 24pt 로 들어간다.** 22.4pt 로 눌려 있던 것도 원래 크기를 회복했다.

**부수 효과: 버튼 높이가 111 → 82px 로 줄었다.** 라벨(26px)에는 충분하지만 이건 크기 취향이라 자의로 확정하지 않고 사용자에게 보고했다.

## 재발 방지

**TMP 텍스트를 배치할 때 캡처만 보고 넘기지 않는다.** 최소한 이 두 값을 비교한다.

```csharp
float rectH = /* GetWorldCorners 로 계산한 rect 높이 */;
float needH = text.GetPreferredValues(text.text, rectW, 0).y;   // 필요 높이
float drawH = text.textBounds.size.y;                            // 실제 그려진 높이
// needH > rectH  또는  drawH > rectH  이면 넘치는 것
```

- `isTextOverflowing` 은 `overflowMode = Overflow` 에서 신뢰할 수 없다
- **오토사이즈는 안전망이 아니다.** 하한 근처까지 줄어들 거라 가정하지 말 것
- 검증은 **최장 콘텐츠 노드**로 한다. 평균 길이로 재면 통과한다

## 이력서 한 줄

> TextMeshPro 의 Overflow 모드 특성상 화면상 잘림 없이 컨테이너를 벗어나던 텍스트 오버플로 2건을, rect 높이와 요구 높이를 수치 비교하는 방식으로 검출하고, 오토사이즈가 하한까지 수렴하지 않는 동작을 확인한 뒤 레이아웃 재배분으로 해결했습니다.
