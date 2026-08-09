# 4단계 (a) — 결과 노드 덤프

수집일 2026-08-10 · 브랜치 `feat/match-engine` · 코드 수정 없음, 사실 수집만.

대상 데이터: `Assets/_Project/Scenario/counter-time-match-scenario-v2.json`
(nodes 42, edges 54, `exported: 2026-08-09T00:00:00.000Z`).

본문·제목은 전부 JSON `content` / `title` 원문 그대로다. 요약·교정·정규화를 하지 않았다.
수치나 문구가 이상해 보여도 손대지 않았다.

---

## 0. 판정식 보유 노드 — 실측

`content` 에 `[판정식` 이 들어 있는 노드는 정확히 9개다. 지시받은 목록과 일치한다.

    m10 m11 m14 m17 m18 m27 m33 m34 m36

`[STATE]` 가 들어 있는 노드는 14개로, 판정식 집합과 겹치는 것은 **m27 · m34 둘뿐**이다.

    m0 m1 m4 m5 m7 m9 m12 m15 m21 m24 m27 m28 m34 m40

---

## 1. 판정식 보유 노드 9개 — 전문 덤프

색 표기는 JSON `border` 값이며, 괄호 안 역할은 `CounterTime.Core.Scenario.NodeKinds.FromBorder`
가 그 색에 매기는 `NodeKind` 다 (m0 범례의 색 정의와 같다).

---

### m10

- **id / 색(역할)**: `m10` / 빨강 `#c9402f` (ActionB — B의 행동·위기)
- **제목 원문**: `B: 전환의 틈에 업어치기 진입`
- **[STATE] 원문**: 없음 (이 노드에 `[STATE]` 줄이 없다)
- **[판정식] 원문**:

      [판정식/B] 잡기-1 +수싸움+2(틈 읽음) +체력0 +기술+1 = 2 → 유효권.

- **본문 전문**:

      오른손이 소매→뒷깃으로 이동하는 0.5초. B가 그 틈을 노려 회전 진입.
      [판정식/B] 잡기-1 +수싸움+2(틈 읽음) +체력0 +기술+1 = 2 → 유효권.
      A의 하체가 '카운터 준비'였는지가 운명을 가른다.

**부모 노드 — m8** / 파랑 `#2f6bad` (ChoiceA) / `A: 오른손 틀어잡기 전환 / 코어 공격 / 하체 중립`

    왼손 소매를 축으로, 오른손이 뒷깃을 감아쥔다.
    연계 규칙: 한 손이 소매·깃을 잡고 있어야 틀어잡기·등싸잡기가 열린다.
    [A 심리] 여기가 내 유도의 시작점이다.

**자식 노드 — m11** / 파랑 `#2f6bad` (ChoiceA) / `A: 하체가 카운터 준비였다 — 빗당겨치기 되치기`

    들어오는 회전을 그대로 당겨 눕힌다. 되치기 체력 -2 (상대 힘 이용).
    [판정식/A] 잡기+1 +수싸움+2 +체력0 +기술+1 = 4 → 유효! 소매를 쥔 채라 곁누르기 연계 가능.
    [대파열: 예고 0.8초 정적 → 정지 0.25초 + 흑백반전 + 굉음]

**자식 노드 — m12** / 초록 `#2e8b57` (Judgement) / `A 무방비 — B 유효 선취`

    B의 업어치기가 반쯤 들어갔다. 잡기 열세라 회전이 얕아 유효.
    [STATE] 잡기 0(리셋) | A92 B96 | B 유효1 | 3:30
    유효는 몇 개를 쌓아도 절반이 되지 않는다 — 그러나 골든스코어에선 이것만으로 이긴다.

**서술 문구의 위치**: 양쪽에 나뉘어 있다. 진입 국면의 서술(`오른손이 소매→뒷깃으로 …`,
`A의 하체가 '카운터 준비'였는지가 운명을 가른다.`)은 **m10 자기 본문**에 있고, 그 판정식이
실제로 어떻게 끝났는지의 서술(`B의 업어치기가 반쯤 들어갔다. 잡기 열세라 회전이 얕아 유효.`
및 규칙 해설 줄)은 **자식 m12(초록)** 에 있다. m10 자기 본문에는 결과 서술이 없다.

---

### m11

- **id / 색(역할)**: `m11` / 파랑 `#2f6bad` (ChoiceA — A의 선택)
- **제목 원문**: `A: 하체가 카운터 준비였다 — 빗당겨치기 되치기`
- **[STATE] 원문**: 없음
- **[판정식] 원문**:

      [판정식/A] 잡기+1 +수싸움+2 +체력0 +기술+1 = 4 → 유효! 소매를 쥔 채라 곁누르기 연계 가능.

- **본문 전문**:

      들어오는 회전을 그대로 당겨 눕힌다. 되치기 체력 -2 (상대 힘 이용).
      [판정식/A] 잡기+1 +수싸움+2 +체력0 +기술+1 = 4 → 유효! 소매를 쥔 채라 곁누르기 연계 가능.
      [대파열: 예고 0.8초 정적 → 정지 0.25초 + 흑백반전 + 굉음]

**부모 노드 — m10** / 빨강 `#c9402f` (ActionB) / `B: 전환의 틈에 업어치기 진입`

    오른손이 소매→뒷깃으로 이동하는 0.5초. B가 그 틈을 노려 회전 진입.
    [판정식/B] 잡기-1 +수싸움+2(틈 읽음) +체력0 +기술+1 = 2 → 유효권.
    A의 하체가 '카운터 준비'였는지가 운명을 가른다.

**자식 노드 — m38** / 파랑 `#2f6bad` (ChoiceA) / `A: 곁누르기 — 굳히기`

    절반(또는 되치기 유효) 직후 그대로 상체를 눌러 고정.
    2025 룰: 5초 유효 / 10초 절반 / 20초 한판. 절반 보유 시 10초에 합산 한판.

**서술 문구의 위치**: **자기 본문**이다. 동작 서술(`들어오는 회전을 그대로 당겨 눕힌다.`),
판정 결과(`= 4 → 유효!`), 연출 지시(`[대파열: …]`)가 전부 m11 안에 있다. 이 판정에 대응하는
초록 결과 노드가 없다 — 유일한 자식 m38 은 다음 국면(굳히기)의 파랑 선택 노드이고 `[STATE]` 가 없다.

---

### m14

- **id / 색(역할)**: `m14` / 파랑 `#2f6bad` (ChoiceA)
- **제목 원문**: `A: 공격 카운트 쌓기 — 밭다리·발목받치기·밭다리·허리후리기`
- **[STATE] 원문**: 없음
- **[판정식] 원문**:

      [판정식] 잡기+2 +수싸움0 +체력0 +기술+1 +상대수비-2 = 1 → 노포인트.

- **본문 전문**:

      진심 반, 어필 반. 체력 -10 (잡기+2 계수 0.6: 3+1+3+3).
      [판정식] 잡기+2 +수싸움0 +체력0 +기술+1 +상대수비-2 = 1 → 노포인트.
      굳힌 상대에겐 점수가 안 난다 — 대신 지도가 난다.

**부모 노드 — m13** / 빨강 `#c9402f` (ActionB) / `B: 극단적 방어 — 상체·코어·하체 수비 올인`

    웅크린 B. 기술이 안 통하는 대신 B도 아무것도 못 한다.
    [심판] 극단 방어 = 턴당 소극 +3 (비공개). 체력도 턴당 -1.
    지도 2 상태에서 이걸 또 고르면 → 반칙패 라인.

**자식 노드 — m15** / 초록 `#2e8b57` (Judgement) / `그쳐 — B 소극 지도 1`

    B가 배를 깔고 매달리자 심판이 끊는다. "지도!"
    [심판 내부] 소극 12 도달(표준 임계 12) → 지도. 카운터 0 리셋, 임계 14로 상승.
    [STATE] 잡기 0 | A90 B97 | 지도 A0 B1 | 3:10 → 중앙 재개(회귀)

**서술 문구의 위치**: 나뉘어 있다. 판정식 자체의 결과 서술(`노포인트`, `굳힌 상대에겐 점수가
안 난다 — 대신 지도가 난다.`)은 **자기 본문**에 있고, 그 "대신 나는 지도"의 실제 발생과 상태
갱신은 **자식 m15(초록)** 에 있다.

---

### m17

- **id / 색(역할)**: `m17` / 초록 `#2e8b57` (Judgement — 판정·결과)
- **제목 원문**: `판정: 밭다리 절반 → 곁누르기`
- **[STATE] 원문**: 없음 (초록 노드지만 `[STATE]` 줄이 없다)
- **[판정식] 원문**:

      [판정식] 잡기+2 +수싸움+2(푸는 타이밍 읽음) +체력0 +시그니처+2 = 6 → 절반. 그대로 굳히기 진입.

- **본문 전문**:

      자세를 풀던 B가 반 박자 늦었다. 회전 90도.
      [판정식] 잡기+2 +수싸움+2(푸는 타이밍 읽음) +체력0 +시그니처+2 = 6 → 절반. 그대로 굳히기 진입.

**부모 노드 — m16** / 파랑 `#2f6bad` (ChoiceA) / `A: 진짜 승부 — 지도 압박에 B가 자세를 푸는 찰나`

    지도를 안은 B가 웅크림을 푸는 순간을 노렸다. 어필용이 아니라 넘기러 간다.
    시그니처 틀어잡고 밭다리, 체력 -3.
    [A 심리] 지도 따위 필요 없다. 여기서 끝낸다.

**자식 노드 — m38** / 파랑 `#2f6bad` (ChoiceA) / `A: 곁누르기 — 굳히기`

    절반(또는 되치기 유효) 직후 그대로 상체를 눌러 고정.
    2025 룰: 5초 유효 / 10초 절반 / 20초 한판. 절반 보유 시 10초에 합산 한판.

**서술 문구의 위치**: **자기 본문**이다. 결과 서술(`자세를 풀던 B가 반 박자 늦었다. 회전 90도.`)이
판정식 줄 위에 놓여 있다. 부모 m16 에는 그 판정으로 가는 의도·비용 서술이 있고, 자식 m38 은 다음
국면이다.

---

### m18

- **id / 색(역할)**: `m18` / 빨강 `#c9402f` (ActionB)
- **제목 원문**: `B: 패턴 읽힘 — 안뒤축 되치기`
- **[STATE] 원문**: 없음
- **[판정식] 원문**:

      [판정식/B] 잡기-2 +수싸움+3(완전간파) +체력0 +기술+1 = 2 → B 유효.

- **본문 전문**:

      같은 밭다리, 세 번째. B가 축발을 걸어 되받는다.
      [판정식/B] 잡기-2 +수싸움+3(완전간파) +체력0 +기술+1 = 2 → B 유효.
      반복은 읽힌다. 습관은 상대에게도 생긴다. → 중앙 재개

**부모 노드 — m16** / 파랑 `#2f6bad` (ChoiceA) / `A: 진짜 승부 — 지도 압박에 B가 자세를 푸는 찰나`

    지도를 안은 B가 웅크림을 푸는 순간을 노렸다. 어필용이 아니라 넘기러 간다.
    시그니처 틀어잡고 밭다리, 체력 -3.
    [A 심리] 지도 따위 필요 없다. 여기서 끝낸다.

**자식 노드 — m2** / 검정 `#000000` (Narration) / `국면1·2 — 오른소매 공방 (중앙 재개 허브)`

    손끝 싸움. 잡고, 털리고, 다시 잡는다. 모든 '그쳐' 후 재개는 여기로 회귀.
    한 턴 = 손 하나(상대의 오른소매/왼소매/왼가슴/오른가슴/회피) + 코어(공/중/수) + 하체(공/중/수/카운터). 발기술은 잡으면서도 가능.
    양손 동시 진입은 선택지에 없다 — '나 넘겨주세요'다.
    [A 심리] 서두를 것 없다. 리치는 내가 길다.
    [B 심리] 오른소매를 내주는 순간 업어치기가 봉인된다.

**서술 문구의 위치**: **자기 본문**이다. 동작 서술과 결과 해설(`반복은 읽힌다. 습관은 상대에게도
생긴다.`)이 모두 m18 안에 있다. 자식 m2 는 중앙 재개 허브이고 이 판정과 무관한 상설 서술이다.
m16 은 m17 과 m18 양쪽의 부모다.

---

### m27

- **id / 색(역할)**: `m27` / 초록 `#2e8b57` (Judgement)
- **제목 원문**: `판정: 유효! + 굳히기로 10초 소모`
- **[STATE] 원문**:

      [STATE] A 유효1 | A93 B90 | 2:50 → 종반으로

- **[판정식] 원문**:

      [판정식] 잡기-1 +수싸움+2 +체력0 +기술+1 = 2 → 유효.

- **본문 전문**:

      [판정식] 잡기-1 +수싸움+2 +체력0 +기술+1 = 2 → 유효.
      도복을 놓쳐 본격 굳히기는 무리 — 유리한 쪽 특권으로 10초만 태운다(체력 +2 회복).
      [STATE] A 유효1 | A93 B90 | 2:50 → 종반으로

**부모 노드 — m26** / 파랑 `#2f6bad` (ChoiceA) / `② 선제 빗당겨치기 — 달려드는 B를 마중`

    지도 하나를 진 B는 만회하러 온다. 그 전진을 그대로 당겨 눕힌다.
    [A 심리] 수싸움이다. '온다'에 건다.

**자식 노드 — m31** / 검정 `#000000` (Narration) / `종반 — 잔여 1:00`

    A 유효 리드. B 지도 2, 조급하다. 체력 A70 / B45.
    B가 25~49 구간 — A의 모든 판정식에 +2가 붙기 시작한다.
    [B 심리] 이대로면 진다. '그 수'를 꺼낼 때다.

**서술 문구의 위치**: **자기 본문**이다. 다만 이 노드는 판정식 줄이 첫 줄이고 그 아래가 서술
(`도복을 놓쳐 본격 굳히기는 무리 — …`)이다. 부모 m26 에 진입 의도 서술이 있다.

---

### m33

- **id / 색(역할)**: `m33` / 파랑 `#2f6bad` (ChoiceA)
- **제목 원문**: `A: 간파 — 수 노트가 가리킨 답 → 한판`
- **[STATE] 원문**: 없음
- **[판정식] 원문**:

      [판정식] 잡기+1 +완전간파+3 +상대체력+2 +기술+1 = 7 → 한판! 경기 종료.

- **본문 전문**:

      "초동이 같아도 말아쥔 손목은 숨길 수 없다." 회전을 흘리고 등 뒤를 취한다.
      [판정식] 잡기+1 +완전간파+3 +상대체력+2 +기술+1 = 7 → 한판! 경기 종료.
      [한판 연출: 붉은 실루엣 → 슬로우 + 무음 → 「一本」]

**부모 노드 — m32** / 빨강 `#c9402f` (ActionB) / `B: 깜짝 변칙 — 말아업어치기 (1회 한정)`

    2025 개정으로 합법화된 그 기술. B 체력 30↓ 1회 한정 발동.
    소매를 말아쥐고 몸째 구른다 — 정석 업어치기와 초동이 같다.
    [예고 연출: 0.8초 정적 — 읽을 수 있는 마지막 창]

**자식 노드**: 없음 (종단). `edges` 에 `from: "m33"` 인 항목이 하나도 없다.

**서술 문구의 위치**: **자기 본문**이다. 대사·동작 서술, 판정식, 연출 지시가 전부 m33 안에 있다.
자식이 없으므로 결과를 받아 적을 초록 노드도 없다.

---

### m34

- **id / 색(역할)**: `m34` / 초록 `#2e8b57` (Judgement)
- **제목 원문**: `오독 — B 말아업어치기 절반`
- **[STATE] 원문**:

      [STATE] 0:40 → 중앙 재개

- **[판정식] 원문**:

      [판정식/B] 잡기0 +수싸움+3(완전히 속음) +A체력0 +기술급+2(B의 숨겨둔 시그니처) = 5 → B 절반.

- **본문 전문**:

      [판정식/B] 잡기0 +수싸움+3(완전히 속음) +A체력0 +기술급+2(B의 숨겨둔 시그니처) = 5 → B 절반.
      B 절반1·유효1 vs A 유효1 — 유효는 승계되지 않는다. A는 이제 절반 이상이 필요하다.
      [STATE] 0:40 → 중앙 재개

**부모 노드 — m32** / 빨강 `#c9402f` (ActionB) / `B: 깜짝 변칙 — 말아업어치기 (1회 한정)`

    2025 개정으로 합법화된 그 기술. B 체력 30↓ 1회 한정 발동.
    소매를 말아쥐고 몸째 구른다 — 정석 업어치기와 초동이 같다.
    [예고 연출: 0.8초 정적 — 읽을 수 있는 마지막 창]

**자식 노드 — m2** / 검정 `#000000` (Narration) / `국면1·2 — 오른소매 공방 (중앙 재개 허브)`

    손끝 싸움. 잡고, 털리고, 다시 잡는다. 모든 '그쳐' 후 재개는 여기로 회귀.
    한 턴 = 손 하나(상대의 오른소매/왼소매/왼가슴/오른가슴/회피) + 코어(공/중/수) + 하체(공/중/수/카운터). 발기술은 잡으면서도 가능.
    양손 동시 진입은 선택지에 없다 — '나 넘겨주세요'다.
    [A 심리] 서두를 것 없다. 리치는 내가 길다.
    [B 심리] 오른소매를 내주는 순간 업어치기가 봉인된다.

**서술 문구의 위치**: **자기 본문**이다. 다만 동작 서술은 부모 m32 에만 있고(`소매를 말아쥐고
몸째 구른다 — 정석 업어치기와 초동이 같다.`), m34 자기 본문의 서술 줄은 결과 해설
(`유효는 승계되지 않는다 …`)이다. m33 과 m34 는 같은 부모 m32 의 두 갈래다.

---

### m36

- **id / 색(역할)**: `m36` / 초록 `#2e8b57` (Judgement)
- **제목 원문**: `판정: 한판 — 경기 종료`
- **[STATE] 원문**: 없음
- **[판정식] 원문**:

      [판정식] 잡기+2 +완전간파+3(잡으러 오는 중 = 무방비) +체력0 +시그니처+2 = 7 → 한판.

- **본문 전문**:

      [판정식] 잡기+2 +완전간파+3(잡으러 오는 중 = 무방비) +체력0 +시그니처+2 = 7 → 한판.
      체력 보정 없이도 한판은 난다 — 한판은 지치게 해서가 아니라 '읽어서' 만드는 것.
      [「一本」]

**부모 노드 — m35** / 파랑 `#2f6bad` (ChoiceA) / `A: 시그니처 — 거리 좁혀 틀어잡고 밭다리`

    종반. B가 A의 가슴깃을 잡으러 손을 뻗는 중 — 방어가 완전히 비었다.
    [A 심리] 지금이다. 4년을 반복한 그 각도.

**자식 노드**: 없음 (종단). `edges` 에 `from: "m36"` 인 항목이 하나도 없다.

**서술 문구의 위치**: 나뉘어 있다. 상황 서술은 **부모 m35** 에 있고(`종반. B가 A의 가슴깃을
잡으러 손을 뻗는 중 — 방어가 완전히 비었다.`), m36 자기 본문의 서술 줄은 규칙 해설
(`체력 보정 없이도 한판은 난다 …`)과 연출 표기(`[「一本」]`)다.

---

### 1 요약표 — 서술 문구의 소재

| 노드 | 색 | [STATE] | 그 국면의 서술이 있는 곳 |
|---|---|---|---|
| m10 | 빨강 | 없음 | 진입 서술 = 자기 본문 / 결과 서술 = 자식 m12(초록) |
| m11 | 파랑 | 없음 | 전부 자기 본문 (대응하는 초록 결과 노드 없음) |
| m14 | 파랑 | 없음 | 판정 결과 서술 = 자기 본문 / 지도 발생·상태 = 자식 m15(초록) |
| m17 | 초록 | 없음 | 전부 자기 본문 (부모 m16 에 진입 의도 서술) |
| m18 | 빨강 | 없음 | 전부 자기 본문 |
| m27 | 초록 | 있음 | 전부 자기 본문 (부모 m26 에 진입 의도 서술) |
| m33 | 파랑 | 없음 | 전부 자기 본문 (종단, 자식 없음) |
| m34 | 초록 | 있음 | 결과 해설 = 자기 본문 / 동작 서술 = 부모 m32 |
| m36 | 초록 | 없음 | 규칙 해설·연출 = 자기 본문 / 상황 서술 = 부모 m35 |

---

## 2. 기술 카탈로그 실측

### 2.1 `TechniqueSpec` 의 실제 필드 전부

`Assets/_Project/Scripts/Core/TechniqueSpec.cs` 전문:

```csharp
namespace CounterTime.Core
{
    /// Plain technique data. The ScriptableObject wrapper lives in CounterTime.Game.
    /// MasteryLevel rides on the spec because §5.4 makes mastery per-technique and
    /// StaminaSystem.PayAttack takes no separate mastery parameter.
    public sealed class TechniqueSpec
    {
        public readonly string Id;
        public readonly ActionCost Cost;
        public readonly TechniqueClass Class;
        public readonly int MasteryLevel;   // §5.4, 0..5

        public TechniqueSpec(string id, ActionCost cost, TechniqueClass techClass, int masteryLevel = 0)
        {
            Id = id;
            Cost = cost;
            Class = techClass;
            MasteryLevel = masteryLevel;
        }
    }
}
```

필드는 4개다: `Id` (string) / `Cost` (`ActionCost`) / `Class` (`TechniqueClass`) / `MasteryLevel` (int).
**표시명 필드는 없다.** 주석이 말하는 "ScriptableObject wrapper lives in CounterTime.Game" 은
아직 존재하지 않는다 — `Assets/_Project/Scripts` 전체에서 `ScriptableObject` 를 상속한 기술
래퍼 클래스가 검색되지 않는다.

### 2.2 안정적 식별자가 있는가

**필드 이름은 `Id`, 타입은 `string`.** 다만 이 문자열을 생성하는 카탈로그·레지스트리·enum 이
리포에 없다. 값은 전부 호출 지점에서 리터럴 또는 문자열 결합으로 만들어진다. `new TechniqueSpec(...)`
전체 검색 결과 (테스트 제외):

| 위치 | Id 인자 |
|---|---|
| `Core/MatchEngine.cs:140` | `"tear"` (뜯기 비용 청구용, §3.2) |
| `Brain/HabitCatalogue.cs:31` | `"업어치기"` |
| `Brain/HabitCatalogue.cs:46` | `"안뒤축감아치기"` |
| `Brain/HabitBrainCommitSource.cs:174` | `"견제 발기술"` |
| `Game/Match/ScriptedAssembler.cs:93` | `node.id + ":발기술"` |
| `Game/Match/ScriptedAssembler.cs:104` | 아래 2.3 참조 — 노드 제목에서 파생 |

즉 대조 모드에서 나오는 Id 는 **노드에서 파생된 문자열**이고, 브레인 경로에서 나오는 Id 는
**소스에 박힌 한글 기술명**이다. 두 계열이 같은 기술을 가리켜도 문자열이 일치한다는 보장은 없다.

### 2.3 `ScriptedAssembler` 가 만드는 Id 규칙 (실측)

`ScriptedAssembler.TechniqueSequence(title)` 은 제목의 **마지막 `—` 뒤**를 `·` 로 끊고,
**2개 이상일 때만** 기술 나열로 본다. 1개면 기술명으로 보지 않고 `node.id` 를 그대로 Id 로 쓴다
(`ScriptedAssembler.cs:67`).

판정식 보유 9개 노드에 이 규칙을 그대로 적용한 실측 결과:

| 노드 | 공격자 | 분해된 이름 수 | 실제 생성되는 `TechniqueSpec.Id` |
|---|---|---|---|
| m10 | B | 1 | `m10` |
| m11 | A | 1 (`빗당겨치기 되치기`) | `m11` |
| m14 | A | 4 | `밭다리` · `발목받치기` · `밭다리` · `허리후리기` |
| m17 | A | 1 | `m17` |
| m18 | B | 1 (`안뒤축 되치기`) | `m18` |
| m27 | A | 1 | `m27` |
| m33 | A | 1 | `m33` |
| m34 | B | 1 (`B 말아업어치기 절반`) | `m34` |
| m36 | A | 1 (`경기 종료`) | `m36` |

판정식이 없고 하체 공격을 선언한 노드는 `GripTurn` 이 견제기를 만든다. 42노드 중 해당 노드는
**m3 하나**이고 Id 는 `m3:발기술` 이다.

### 2.4 현재 정의된 기술 전부 — 식별자 / 표시명 / TechniqueClass

카탈로그가 없으므로 "정의된 기술"은 **코드에서 실제로 생성되는 `TechniqueSpec` 인스턴스의 전집합**
이다. 프로덕션 경로(테스트 제외) 기준:

| 식별자 (`Id`) | 표시명 | `TechniqueClass` | `ActionCost` | 출처 |
|---|---|---|---|---|
| `업어치기` | (없음 — Id 가 곧 한글명) | `Standard` | `BigThrow` | `HabitCatalogue.cs:31` (H1) |
| `안뒤축감아치기` | (없음 — Id 가 곧 한글명) | `Standard` | `BigThrow` | `HabitCatalogue.cs:46` (H2) |
| `견제 발기술` | (없음 — Id 가 곧 한글명) | `Sloppy` | `Poke` | `HabitBrainCommitSource.cs:174` |
| `tear` | (없음) | `Sloppy` | `Tear` | `MatchEngine.cs:140` |
| `m10` | (없음 — 노드 id) | 노드의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `m11` | (없음 — 노드 id) | 노드의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `밭다리` | (없음) | 노드 m14 의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `발목받치기` | (없음) | 노드 m14 의 `기술+1` → `Standard` | `Poke` (`Pokes` 배열에 등재) | 조립기 |
| `허리후리기` | (없음) | 노드 m14 의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `m17` | (없음 — 노드 id) | 노드의 `시그니처+2` → `Signature` | `BigThrow` | 조립기 |
| `m18` | (없음 — 노드 id) | 노드의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `m27` | (없음 — 노드 id) | 노드의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `m33` | (없음 — 노드 id) | 노드의 `기술+1` → `Standard` | `BigThrow` | 조립기 |
| `m34` | (없음 — 노드 id) | 노드의 `기술급+2` → `Signature` | `BigThrow` | 조립기 |
| `m36` | (없음 — 노드 id) | 노드의 `시그니처+2` → `Signature` | `BigThrow` | 조립기 |
| `m3:발기술` | (없음) | `Sloppy` (고정) | `Poke` | `GripTurn` |

`TechniqueClass` 는 `Sloppy = 0 / Standard = 1 / Signature = 2` (`Core/Enums.cs`).
조립기 쪽 `Class` 는 노드 판정식의 기술 항 숫자를 그대로 캐스팅한 값이다
(`ScriptedAssembler.ClassOf`). `MasteryLevel` 은 위 모든 생성 지점에서 인자를 주지 않아 기본값 `0` 이다.

---

## 3. 이벤트 실측

### 3.1 `JudgementScored` 선언부 원문

`Assets/_Project/Scripts/Core/MatchEvent.cs:48-63`:

```csharp
    public sealed class JudgementScored : MatchEvent
    {
        public readonly FighterId ScoredBy;
        public readonly Judgement Judgement;
        public readonly int Sum;
        public readonly bool IsCounter;
        /// The five §4 terms in order, exposed so the coach panel can explain a result.
        public readonly int[] Terms;

        public JudgementScored(FighterId scoredBy, Judgement judgement, int sum,
                               bool isCounter, int[] terms)
        {
            ScoredBy = scoredBy; Judgement = judgement; Sum = sum;
            IsCounter = isCounter; Terms = terms;
        }
    }
```

필드 5개: `ScoredBy` / `Judgement` / `Sum` / `IsCounter` / `Terms` (int[5]).

`Terms` 에 담기는 값은 `MatchEngine.cs:198-200` 기준으로 순서대로
`{ grip, (int)read, stamTerm, (int)script.Technique.Class, defensive ? DefensivePosturePenalty : 0 }` 다.
네 번째 항이 `TechniqueClass` 의 정수값이지만 **기술 식별자는 아니다**.

### 3.2 이 이벤트만으로 알 수 있는가

| 알고 싶은 값 | 이 이벤트만으로 가능? | 근거 |
|---|---|---|
| 기술 식별자 | **아니오** | `JudgementScored` 에 `TechniqueSpec` 도 Id 문자열도 없다 |
| 되치기 여부 | **예** | `IsCounter` |
| 득점자 | **예** | `ScoredBy` |

기술 식별자가 현재 있는 곳: `ExchangeScript.Technique` (`Core/MatchEngine.cs:1-27` 의 `ExchangeScript`)
이며, 그 인스턴스는 `MatchEngine.ResolveExchange(a, b, script)` 의 매개변수로만 들어왔다가
`ResolveTechnique` 안에서 소비되고 끝난다. 엔진은 그것을 어디에도 저장하지 않고 이벤트에도 싣지 않는다.
호출자 쪽에서는 `MatchRunner.EnterNode` 의 지역 변수 `scripts` / 루프 변수 `s` 가 그 시점의 유일한
보유자다 (`Game/Match/MatchRunner.cs:104-140`).

---

## 4. 표기 실측

### 4.1 42노드의 색별 개수

JSON `border` 값 실측 (m0 범례 포함, 전체 42):

| 색 | 값 | `NodeKind` | 개수 |
|---|---|---|---|
| 검정 | `#000000` | Narration | 5 |
| 파랑 | `#2f6bad` | ChoiceA | 14 |
| 빨강 | `#c9402f` | ActionB | 9 |
| 초록 | `#2e8b57` | Judgement | 14 |

합 42. 노드별 배정:

- 검정 5: m0 m1 m2 m19 m31
- 파랑 14: m3 m6 m8 m11 m14 m16 m20 m23 m26 m29 m30 m33 m35 m38
- 빨강 9: m5 m7 m10 m13 m18 m22 m28 m32 m40
- 초록 14: m4 m9 m12 m15 m17 m21 m24 m25 m27 m34 m36 m37 m39 m41

### 4.2 결과 노드를 데이터만으로 가려낼 수 있는 표기가 존재하는가

노드가 가진 필드는 `id` / `title` / `content` / `contentHtml` / `x` / `y` / `border` 가 전부다
(`Core/Scenario/ScenarioData.cs` 의 `ScenarioNode`, JSON 키와 1:1). 종류를 나타내는 별도 필드
(`kind`, `type` 등)는 **없다**.

존재하는 표기는 `border` 하나다. m0 범례가 `초록=판정·결과` 로 정의하고, 코드에서는
`NodeKinds.FromBorder` 가 `#2e8b57` → `NodeKind.Judgement` 로 매핑한다. 즉 **"초록 테두리" 는
데이터만으로 읽을 수 있는 표기이며, 이것이 유일한 표기다.**

다만 실측된 사실 두 가지를 그대로 적어둔다 (대체 규칙을 만들지 않는다):

- 초록 14개와 판정식 보유 9개는 서로 다른 집합이다. 교집합은 4개(m17 m27 m34 m36)뿐이고,
  판정식을 가진 나머지 5개는 파랑 3개(m11 m14 m33)·빨강 2개(m10 m18)다.
- `[STATE]` 를 가진 14개도 초록 14개와 다른 집합이다. 초록이면서 `[STATE]` 가 없는 노드가
  6개(m17 m25 m36 m37 m39 m41), 초록이 아니면서 `[STATE]` 가 있는 노드가 6개(m0 m1 m5 m7 m28 m40)다.
- 제목이 `판정:` 으로 시작하는 노드는 4개뿐이다 (m17 m27 m36 m39).

---

## 5. m11 — 카운터 준비 게이트와 부딪히는 지점 (사실만)

### 5.1 원문

**제목 원문**

    A: 하체가 카운터 준비였다 — 빗당겨치기 되치기

**[판정식/A] 줄 원문**

    [판정식/A] 잡기+1 +수싸움+2 +체력0 +기술+1 = 4 → 유효! 소매를 쥔 채라 곁누르기 연계 가능.

**m11 본문 전문**

    들어오는 회전을 그대로 당겨 눕힌다. 되치기 체력 -2 (상대 힘 이용).
    [판정식/A] 잡기+1 +수싸움+2 +체력0 +기술+1 = 4 → 유효! 소매를 쥔 채라 곁누르기 연계 가능.
    [대파열: 예고 0.8초 정적 → 정지 0.25초 + 흑백반전 + 굉음]

**부모 m10** / 빨강 `#c9402f` / `B: 전환의 틈에 업어치기 진입`

    오른손이 소매→뒷깃으로 이동하는 0.5초. B가 그 틈을 노려 회전 진입.
    [판정식/B] 잡기-1 +수싸움+2(틈 읽음) +체력0 +기술+1 = 2 → 유효권.
    A의 하체가 '카운터 준비'였는지가 운명을 가른다.

**자식 m38** / 파랑 `#2f6bad` / `A: 곁누르기 — 굳히기`

    절반(또는 되치기 유효) 직후 그대로 상체를 눌러 고정.
    2025 룰: 5초 유효 / 10초 절반 / 20초 한판. 절반 보유 시 10초에 합산 한판.

### 5.2 규칙 원문

`md/MATCH_RULES.md` §2 (50–56행):

> **카운터 준비 (counter-ready)** — legs slot only, and the point where the player
> *decides* to counter rather than receiving one by accident. Gate: that fighter
> cannot enter a technique this turn — no 기술 진입, no judgment fired by them, and
> no technique of their own resolved this exchange. Costs no stamina. …

`md/MATCH_RULES.md` §4.2 (171–177행):

> - Legs slot `카운터 준비` is declared like any other action and resolves before
>   the judgment. The fighter who declared it cannot have a technique of their own
>   resolved this exchange (§2).
> - If the opponent commits a **big throw** this turn, **the attacker's** ReadLevel drops
>   **one step** (FullRead→Read→Neutral→GotRead, floored at GotRead). …

### 5.3 부딪히는 지점 — 코드 경로 순서대로

**① 파싱 — m11 은 A 의 커밋으로 `카운터 준비` 를 선언한다.**
`ScenarioTurnChoices.TryParse` 는 제목이 `A:` 로 시작하므로 `who = FighterId.A` 로 잡고
(`ScenarioTurnChoices.cs:24`), `LegsOf(title)` 이 제목의 `카운터 준비` 문자열에 걸려
`LegAction.CounterReady` 를 낸다 (`ScenarioTurnChoices.cs:80`). 손 목표는 제목에 해당 키워드가
없어 `HandTarget.None`, 코어는 `CoreStance.Neutral` 이다. 파서에는 시제 개념이 없다 — 제목이
과거형(`카운터 준비였다`)이어도 이 노드의 교환에서 다시 선언된다.

**② 조립 — 같은 노드가 A 를 공격자로 지목한다.**
`ScriptedAssembler.FormulaLine` 이 `[판정식/A]` 를 잡아 `attacker = FighterId.A` 로 놓고
(`ScriptedAssembler.cs:60`), 제목 분해가 1개뿐이라 `techId = node.id = "m11"`,
`ActionCost.BigThrow`, 기술 항 `기술+1` → `TechniqueClass.Standard` 로 `ExchangeScript` 를 만든다
(`ScriptedAssembler.cs:67`, `104`).

**③ 러너 — 둘을 같은 교환에 넣는다.**
`MatchRunner.EnterNode` 는 ①의 커밋을 `_commit[A]` 에 넣고 (`MatchRunner.cs:106-107`),
②의 스크립트가 `Attacker` 를 가지므로 `hasTechnique = true` 가 되어 `return` 하지 않고
(`MatchRunner.cs:108`, `116`) 그대로 `Engine.ResolveExchange(_commit[0], _commit[1], s)` 를 부른다
(`MatchRunner.cs:134`). 즉 **A 의 `카운터 준비` 선언과 A 의 기술이 같은 `ResolveExchange` 호출에 들어간다.**

**④ 엔진 게이트 — 여기서 정확히 부딪힌다.**
`MatchEngine.ResolveExchange` (`MatchEngine.cs:62-69`):

```csharp
            bool crA = a.Legs == LegAction.CounterReady;
            bool crB = b.Legs == LegAction.CounterReady;
            FighterId? attacker = script.Attacker;
            if ((attacker == FighterId.A && crA) || (attacker == FighterId.B && crB))
                attacker = null;
```

m11 에서는 `crA == true` 이고 `attacker == FighterId.A` 이므로 `attacker` 가 `null` 이 된다.
그 결과 `MatchEngine.cs:80` 의 `if (attacker.HasValue && script.Technique != null)` 이 거짓이 되어
`ResolveTechnique` 이 호출되지 않는다. `JudgementScored` 는 `ResolveTechnique` 안(`MatchEngine.cs:204`)
에서만 생성되므로, **m11 본문이 적은 `= 4 → 유효` 는 엔진에서 어떤 이벤트도 만들지 않는다.**
같은 이유로 `entryA` 와 `realA` 도 `false` 가 되어(`MatchEngine.cs:70-73`) 심판은 A 의 기술 진입을
관찰하지 않는다.

**⑤ 표현 방식의 불일치.**
§4.2 는 되치기를 "같은 교환 안에서 **공격자의** ReadLevel 이 한 칸 내려가고, 합이
`CounterThreshold` 이하가 되면 방어자가 `|합|` 으로 득점" 하는 형태로 정의한다
(`JudgementCalculator.cs:47` 의 `if (sum <= cfg.CounterThreshold)`). 이 형태에서 나오는 이벤트는
공격자 = B, `ScoredBy` = A, `IsCounter = true` 인 `JudgementScored` **하나**다.
반면 시나리오는 m10(공격자 B, 합 2)과 m11(공격자 A, 합 4)을 **두 개의 노드 = 두 개의 교환**으로
적었고, m11 의 판정식은 A 자신의 공격(합 +4)으로 쓰여 있다. 되치기임을 나타내는 표기는
`제목`·`본문`의 한국어 단어(`되치기`)뿐이고 판정식 줄에는 없다.

**⑥ 숫자 관계 (노드가 쓴 항만으로).**
m10 이 B 에게 준 항은 `잡기-1 +수싸움+2 +체력0 +기술+1 = 2` 다. §4.2 의 한 칸 내림을 그 `수싸움+2`
(= `ReadLevel.Read`) 에 적용하면 `Neutral` (0) 이 되어 합은 0 이 된다. `MatchRulesConfig` 의
`CounterThreshold` 는 §4 되치기 발화 임계이며 `MATCH_RULES` 는 이를 −3 유지(option ⓐ)로 못 박고 있다.
(잡기·체력·수비 항은 실제로는 엔진 상태에서 나오므로, 위 계산은 **노드가 직접 쓴 항만** 을 대상으로 한 것이다.)

**⑦ 도달 여부.**
m10 과 m11 은 `MatchRunner.Mainline` 에 없다 (`MatchRunner.cs:71-76` 의 배열은
`m1 m2 m3 m4 m8 m9 m13 m14 m15 / m2 m3 m4 m8 m9 / m31 m35 m36`). 따라서 1–3단계에서 IDENTICAL
로 유지된 52줄 지문은 이 경로를 한 번도 걷지 않았다. m10 은 m8 의 두 자식 중 하나다
(`m8 → m9, m10`).

---

## 부록 — 이 문서를 만든 실측 절차

- JSON 파싱·집계는 `node -e` 일회성 스크립트로 수행했고 리포에 남기지 않았다.
- 인용한 코드는 전부 작업 트리의 커밋된 파일에서 그대로 옮겼다.
- 4단계 (a) 는 사실 수집이므로 코드·시나리오 JSON·테스트를 하나도 수정하지 않았다.
