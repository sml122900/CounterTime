# 초기 커밋이 Git LFS 설정보다 먼저 들어간 문제

작성 2026-08-03 · **상태: 미해결 (조치 방법 확정, 실행 대기)**

## 문제 상황

작업 순서가 이렇게 됐다.

```
23:46  git init → 초기 체크인 (204aefc)
02:01  .gitattributes 재작성 + git lfs install
```

즉 **LFS 트래킹이 붙기 전에 바이너리가 이미 커밋됐다.**

증상:

```
$ git status --short
 M Assets/TutorialInfo/Icons/URP.png

$ git diff --stat
 Assets/TutorialInfo/Icons/URP.png | Bin 24069 -> 130 bytes

$ git lfs ls-files
(빈 출력)
```

**만지지도 않은 파일이 modified 로 뜬다.** 그리고 24069 → 130 bytes 라는 diff 는 이미지가 손상된 게 아니다.

## 원인

LFS 는 clean/smudge 필터로 동작한다.

- 워킹 트리의 `URP.png` 는 여전히 정상 이미지 24KB (`Get-Content` 로 JFIF 헤더 확인함)
- `.gitattributes` 가 `*.png` 를 LFS 로 잡으면서, git 이 이 파일을 볼 때 clean 필터를 태워 **130바이트 포인터**로 환산한다
- HEAD 에 들어 있는 건 raw blob 24069바이트
- 130 ≠ 24069 → git 은 "변경됨"이라고 판단

파일은 멀쩡하다. 표현 형식이 커밋 시점과 달라진 것뿐이다.

## 시도한 것들 / 검토한 선택지

**A. 그냥 커밋해서 포인터로 덮는다**
- 앞으로는 LFS 로 간다. 다만 초기 커밋 안의 24KB blob 은 히스토리에 영구 잔류.
- 비용: 지금은 24KB. 무시 가능한 크기.

**B. `git lfs migrate import` 로 히스토리 재작성**
```
git lfs migrate import --include="*.png,*.jpg,*.fbx,*.psd,*.wav" --everything
```
- 과거 커밋의 바이너리까지 전부 포인터로 바꾼다. 히스토리가 깨끗해짐.
- 비용: 커밋 해시가 전부 바뀐다. **그리고 원격(`github.com/sml122900/CounterTime`)에 초기 커밋이 이미 푸시돼 있어 `git push --force` 가 필요하다.**

**C. 초기 커밋을 버리고 다시 커밋**
```
git reset --soft $(git rev-list --max-parents=0 HEAD)  # 또는 브랜치 재생성
```
- 가장 단순하지만 "초기 체크인" 시점 기록이 사라지고, 역시 force push 가 필요하다.

## 최종 해결법

**A 를 택했다 — 앞으로만 LFS 로 가고, 히스토리의 24KB blob 은 남긴다.**

당초 판단은 B(히스토리 재작성)였다. "원격도 협업자도 없으니 재작성 비용이 0"이라는 전제였는데, **이 전제가 틀렸다.** `git ls-remote origin` 확인 결과 초기 커밋 `204aefc` 가 이미 GitHub 에 올라가 있다. B 를 하려면 force push 가 필요하고, 이는 되돌리기 어려운 원격 조작이다.

바뀐 저울:

- **이득**: 히스토리에서 제거되는 건 `URP.png` **24KB 하나**. 그나마 Unity 튜토리얼 아이콘이라 프로젝트에 불필요한 파일이다.
- **비용**: 원격 히스토리 강제 재작성.

24KB를 위해 force push 할 이유가 없다. **중요한 건 "지금부터 들어올 모캡 FBX·텍스처가 LFS 를 타는 것"이고, 그건 이미 `.gitattributes` 로 보장된다.**

향후 B 가 필요해지는 조건: 초기 커밋에 실수로 대용량 바이너리(수십 MB 이상)가 들어갔다는 게 나중에 발견될 경우. 그때는 협업자가 생기기 전에 처리한다.

## 재발 방지

Unity 프로젝트 신규 세팅 순서를 고정한다:

```
1. .gitignore / .gitattributes 배치
2. git lfs install
3. git init
4. 첫 커밋          ← 반드시 마지막
```

`.gitattributes` 는 **첫 커밋에 포함되어야 한다.** LFS 는 소급 적용되지 않는다.

## 이력서 한 줄

> Git LFS 도입 시점이 초기 커밋보다 늦어 바이너리가 히스토리에 raw blob 으로 남는 문제를 clean/smudge 필터 동작 원리에서 원인 규명하고, 협업자·원격이 없는 시점에 `git lfs migrate import` 로 처리하도록 판단해 향후 저장소 비대화를 차단했습니다.
