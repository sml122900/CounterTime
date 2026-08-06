# 컨트롤러가 미커밋 에셋의 guid 를 참조하던 문제 (+ .gitignore 규칙 순서)

작성 2026-08-07 · **상태: 해결**

문제 두 개다. 둘 다 "Unity 에셋 중 무엇을 저장소에 넣을 것인가"를 정하다 나왔다.

---

## 문제 1 — 컨트롤러가 저장소에 없는 파일을 가리키고 있었다

### 문제 상황

AI 클립 4종을 커밋하기 직전 상태가 이랬다.

```
 M Assets/_Project/Art/Animations/AC_Fighter_A.controller   ← 커밋됨 (직전 커밋)
 M Assets/_Project/Art/Animations/AC_Fighter_B.controller   ← 커밋됨 (직전 커밋)
?? Assets/_Project/Animations/                              ← 미추적
```

컨트롤러에는 이미 `ai_seoinage_entry` 등 4개 State 가 들어가 있었다. Unity 는 에셋을 **guid 로** 참조한다.

```yaml
# AC_Fighter_A.controller
m_Motion: {fileID: 7400000, guid: b3b766996f0981b4eb1c72c72374abc6, type: 2}
```

그리고 그 guid 는 **미추적 파일의 `.meta` 안에만** 있었다.

```yaml
# Assets/_Project/Animations/AI/ai_seoinage_entry.anim.meta  ← 저장소에 없음
guid: b3b766996f0981b4eb1c72c72374abc6
```

내 작업 PC 에서는 멀쩡히 돌아간다. 파일이 디스크에 있으니까. **클론한 사람에게는 4개 State 가 전부 빈 모션이다.** 컴파일 에러도, 콘솔 경고도 안 난다 — Unity 는 없는 guid 참조를 조용히 `None` 으로 두고 그 State 는 아무것도 재생하지 않는다.

증상이 나타나는 시점이 나쁘다. 커밋할 때도, 푸시할 때도, 클론할 때도 아니고 **Play 를 눌러 그 노드에 도달했을 때** 캐릭터가 가만히 서 있는 걸로 처음 드러난다.

### 왜 생겼나

에셋 생성 도구가 만든 파일이 **두 곳에 흩어졌다.**

- 컨트롤러 수정 → `Assets/_Project/Art/Animations/` (기존 경로, 이미 추적 중이라 `git add -u` 에 걸림)
- 클립 파일 → `Assets/_Project/Animations/AI/` (**새 경로**, 미추적이라 `git add -u` 에 안 걸림)

`git add -u` 는 추적 중인 파일의 변경만 스테이징한다. 새 폴더는 건드리지 않는다. 참조하는 쪽만 커밋되고 참조되는 쪽이 빠지는 정확한 조건이었다.

### 최종 해결법

커밋 전에 **guid 가 실제로 해소되는지** 확인했다.

```bash
for f in Assets/_Project/Animations/AI/*.anim.meta; do
  g=$(grep -o 'guid: [a-f0-9]*' "$f" | head -1 | cut -d' ' -f2)
  echo "$(basename $f .anim.meta) $g -> $(grep -c "$g" .../AC_Fighter_A.controller)"
done
```

```
ai_ground_pin       f98a27d9…  -> A:1 B:1
ai_osotogari_entry  ea9e7f97…  -> A:1 B:1
ai_seoinage_entry   b3b76699…  -> A:1 B:1
ai_ukemi_back       1e3d5d7d…  -> A:1 B:1
```

4개 전부 양쪽 컨트롤러에서 참조 중 = 4개 전부 커밋 대상이다. 폴더를 명시해 스테이징했다.

```bash
git add Assets/_Project/Animations.meta Assets/_Project/Animations/
```

`Animations.meta`(폴더 자체의 meta)도 같이 넣어야 한다. 빠뜨리면 Unity 가 폴더 meta 를 새로 만들고, 다른 사람 PC 에서 다른 guid 가 생겨 매번 diff 가 뜬다.

### 재발 방지

**참조하는 쪽과 참조되는 쪽이 다른 폴더에 있으면 `git add -u` 를 믿지 않는다.**

에셋을 만드는 도구(AI 생성, 임포터, 패키지)를 쓴 뒤에는 커밋 전에 이것부터 본다.

1. `git status --short` 에서 `??` 로 뜬 것 중 `Assets/` 아래에 있는 게 있나
2. 있으면 그 `.meta` 의 guid 를 이미 커밋된 씬/프리팹/컨트롤러가 참조하고 있나
3. 참조하고 있으면 **반드시 같은 커밋에** 들어가야 한다

이번엔 `.controller` 였지만 `.unity`(씬), `.prefab`, `.asset` 전부 같은 구조다.

---

## 문제 2 — .gitignore 규칙을 뒤에 붙여야 했다

### 문제 상황

무시할 대상이 둘이었다.

- `Assets/AI Toolkit/` — 패키지 캐시
- `Assets/AI Toolkit.meta` — **폴더 밖에 있는 폴더용 meta 파일**
- `GeneratedAssets/` — 생성 중간산물

그런데 이 프로젝트의 `.gitignore` 15번 줄에 이게 있다.

```gitignore
# .meta 는 원본 에셋이 무시될 때만 무시 (원본이 살아있으면 .meta 는 반드시 커밋)
!/[Aa]ssets/**/*.meta
```

Unity 프로젝트에 필요한 규칙이다. `.meta` 를 빠뜨리면 안 되니까 전부 재포함시킨다.

**이 부정 규칙이 `Assets/AI Toolkit.meta` 도 재포함시킨다.** 무시 규칙을 15번 줄보다 **앞에** 넣으면 무시가 안 된다.

### 왜 그런가

gitignore 는 **마지막에 매칭된 규칙이 이긴다.** 앞에서 무시하고 뒤에서 부정하면 부정이 이긴다.

폴더(`Assets/AI Toolkit/`)는 상관없다. 디렉터리가 무시되면 git 이 아예 안 들어가서 내부 파일은 부정 규칙으로도 되살릴 수 없다. 문제는 **폴더 밖에 따로 있는 `Assets/AI Toolkit.meta` 파일 하나**다.

### 최종 해결법

규칙을 **파일 맨 끝**에 붙였다.

```gitignore
# AI Toolkit — 패키지 캐시 (Unity 가 재생성)
/[Aa]ssets/AI Toolkit/
/[Aa]ssets/AI Toolkit.meta

# AI 에셋 생성 중간산물 (클립 guid 별 작업 폴더)
/GeneratedAssets/
```

경로에 공백이 있어도 escape 안 해도 된다. gitignore 에서 escape 가 필요한 건 **줄 끝 공백**뿐이다.

### 검증

추측하지 말고 `git check-ignore -v` 로 어느 규칙이 이겼는지 직접 본다.

```
$ git check-ignore -v "Assets/AI Toolkit/" "Assets/AI Toolkit.meta" \
                      "GeneratedAssets/" ".../AI/ai_ground_pin.anim.meta"

.gitignore:58:/[Aa]ssets/AI Toolkit/       Assets/AI Toolkit/          ← 무시됨
.gitignore:59:/[Aa]ssets/AI Toolkit.meta   Assets/AI Toolkit.meta      ← 무시됨
.gitignore:62:/GeneratedAssets/            GeneratedAssets/            ← 무시됨
.gitignore:15:!/[Aa]ssets/**/*.meta        .../ai_ground_pin.anim.meta ← 부정 규칙이 이김 = 무시 안 됨
```

마지막 줄이 핵심이다. **새 무시 규칙이 AI 클립의 `.meta` 까지 잡아가지 않았음**을 같이 확인해야 한다. 무시하려던 것만 보고 넘어가면, 커밋해야 할 `.meta` 를 같이 날린 걸 나중에 안다.

### 재발 방지

- Unity 프로젝트의 `.gitignore` 에 규칙을 추가할 때는 **`.meta` 부정 규칙보다 뒤에** 넣는다. 파일 끝에 붙이는 게 안전하다
- 추가 후 `git check-ignore -v` 를 **양방향으로** 돌린다 — 무시하려던 것이 무시되는지, 무시하면 안 되는 것이 살아있는지

---

## 이력서 한 줄

> Unity 에셋의 guid 참조 무결성을 커밋 전에 검증하는 절차를 세워, 애니메이터 컨트롤러가 미추적 클립을 참조한 채 커밋되어 클론 환경에서만 무음 실패하는 결함을 사전에 차단했습니다. 동시에 `.meta` 재포함 부정 규칙과 신규 무시 규칙의 우선순위 충돌을 `git check-ignore` 로 양방향 검증했습니다.
