# RunCommand 스크립트가 이른 단계 NullReferenceException으로 죽는 문제 — 진짜 원인, 지난 세션 진단 정정

작성 2026-08-13 · **상태: 해결(원인 확정), 지난 세션 진단 오류 발견**

## 문제 상황

Unity MCP `RunCommand`로 짠 스크립트가 IL 초반 오프셋(`[0x0000b]`~`[0x0000c]`)에서
`NullReferenceException`을 내며 죽었다. 에러 메시지는 항상 같은 패턴:

```
[Error] System.NullReferenceException: Object reference not set to an instance of an object
  at Unity.AI.Assistant.Agent.Dynamic.Extension.Editor.CommandScript.Execute (...) [0x0000b/c] in <...>:0
  ...
Consider reverting changes that may have happened if you retry.
```

이번 세션(§H 3.5b-로스터)에서 6상대 스모크 테스트 스크립트를 짤 때 이 에러가
두 번 재현됐다 — 한 번은 `CommandScript`에 헬퍼 메서드(`RunRound`)를 둔
버전에서, 한 번은 그 헬퍼를 지우고 전부 `Execute()` 안에 인라인한 버전에서.

## 왜 생겼나

두 시도 다 스크립트 첫머리에 `GameObject.Find("LiveMatch")`(또는
`liveGo.SetActive(true)`)가 있었다. `LiveMatch` GameObject는 대회 흐름의
설계상 대진표에서 "경기 시작"을 누르기 전까지 **비활성 상태**로 지어진다
(`docs/decisions/livematch-gameobject-reuse-awake-timing.md` 참조). `GameObject.Find`는
씬 안의 **활성 오브젝트만** 검색 대상으로 삼는다 — Unity 공식 동작이다.
비활성인 `LiveMatch`를 찾으면 `null`이 돌아오고, 바로 다음 줄에서
`.GetComponent<...>()`나 `.SetActive(true)`를 호출하면 그 자리에서 NRE가
난다. IL 오프셋이 매번 똑같이 작았던 이유도 이거다 — 스크립트의 **첫
번째나 두 번째 문장**에서 죽었으니까.

지난 세션(§H 3.5b-뼈대)에서도 같은 증상을 두 번 겪었는데, 그때는 "직전에
헬퍼 메서드를 추가했다"는 시점 상관관계만 보고 "`CommandScript`에 `Execute()`
외 별도 메서드를 두면 NRE가 난다"고 결론 내렸다(원인 불명, Unity AI
Assistant 패키지 쪽 문제로 추정). 이번 세션엔 **헬퍼 메서드를 완전히 지운
버전에서도** 똑같은 에러가 재현돼서 그 결론이 틀렸다는 게 드러났다.

## 시도한 것들

1. (지난 세션) 헬퍼 메서드를 지우고 전부 인라인 — 에러가 사라진 것처럼
   보여서 "헬퍼 메서드가 원인"으로 잠정 결론.
2. (이번 세션) 같은 패턴(6상대 스모크 스크립트)에서 헬퍼 메서드 버전이
   또 죽음 → 지난 결론이 재현 가능한지 의심.
3. 헬퍼 메서드를 지우고 인라인만 남긴 두 번째 버전도 **똑같이** 죽음 —
   헬퍼 메서드 가설을 반증.
4. 에러 직전 상태를 별도 진단 스크립트(`SceneManager.GetActiveScene().
   GetRootGameObjects()`로 씬 전체 순회, `activeSelf` 출력)로 찍어보니
   `LiveMatch=False`(비활성)로 나옴 — 죽은 스크립트가 그 오브젝트를
   `GameObject.Find`로 찾으려 했다는 사실과 맞아떨어짐.

## 최종 해결법

`GameObject.Find` 대신 `SceneManager.GetActiveScene().GetRootGameObjects()`로
씬 루트를 순회해 이름으로 찾는다 — 이 방법은 비활성 오브젝트도 찾는다.

```csharp
GameObject liveGo = null;
foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
    if (go.name == "LiveMatch") liveGo = go;
liveGo.SetActive(true);
```

이후 6상대 스모크 스크립트, 진도윤/임준철 장기 매치 스크립트 등 여러
후속 스크립트에서 이 패턴으로 안정적으로 동작했다.

## 재발 방지

- **`GameObject.Find`는 비활성 오브젝트를 못 찾는다.** RunCommand
  스크립트가 대상 오브젝트의 활성 상태를 확신할 수 없으면(특히 이
  프로젝트처럼 "매치 시작 전까지 비활성"이 정상 상태인 설계에서는)
  `GetRootGameObjects()` 순회나 `Resources.FindObjectsOfTypeAll<T>()`로
  대체한다.
- **시점 상관관계만으로 원인을 확정하지 않는다.** 지난 세션은 "헬퍼
  메서드를 지웠더니 에러가 없어졌다"만 보고 결론 내렸는데, 실제로는
  그 우연한 실행에서 대상 오브젝트가 마침 활성 상태였을 가능성이 높다
  (재현 조건이 세션마다 씬 상태에 따라 달라진다). 반증 시도(같은
  변경을 다시 걸어보기, 다른 변수를 통제하고 재현하기) 없이 첫 번째
  그럴듯한 가설을 확정 짓지 않는다.
- 이 문서 자체가 지난 세션 STATUS 기록의 정정이다 — 잘못된 진단이
  기록에 남아 있으면 다음 세션이 그걸 참고해 또 잘못된 우회를 시도할
  수 있다. 정정도 원래 기록만큼 기록해야 한다.

## 이력서 한 줄

> Unity 에디터 자동화 스크립트가 재현 가능한 `NullReferenceException`으로
> 반복 실패하는 것을, 지난 세션이 남긴 "헬퍼 메서드가 원인"이라는 진단을
> 그대로 믿지 않고 같은 변경을 다른 조건에서 재현·반증해 진짜 원인
> (`GameObject.Find`가 비활성 오브젝트를 못 찾는 Unity API 동작)을
> 찾아냈습니다. 잘못된 이전 진단을 문서에 명시적으로 정정해, 같은 실수가
> 반복되지 않게 남겼습니다.
