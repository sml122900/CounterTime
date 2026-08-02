# 기술 결정: Git LFS 도입

작성 2026-08-03 · 상태: 설정 완료, 기존 히스토리 미정리

## Problem — 문제

Unity 프로젝트는 바이너리가 본체다. 텍스처(psd/png), 모캡 FBX, 오디오(wav), 영상. COUNTER TIME 은 격투 동작을 다루는 게임이라 모캡 데이터와 애니메이션이 계속 들어온다.

Git 은 바이너리 변경분을 델타로 못 줄인다. 100MB 짜리 FBX 를 10번 수정하면 히스토리에 1GB 가 그대로 쌓이고, **이건 되돌릴 수 없다** — 나중에 지우려면 히스토리 재작성이라 이미 클론한 사람 전부가 깨진다. 즉 이 결정은 프로젝트 첫날에 하지 않으면 비용이 계속 오르는 종류다.

추가로 Unity 특유의 문제: `.meta` 파일과 자동 생성물 때문에 기본 `.gitignore` 로는 저장소가 금방 오염된다.

## Action — 한 것

**1. Git LFS 설치·트래킹 설정**

```
git lfs install            # git-lfs 3.7.1
```

`.gitattributes` 에 패턴 등록:

```
*.fbx *.FBX *.obj *.blend        # 3D 모델 / 모캡
*.png *.jpg *.jpeg *.tga *.psd   # 텍스처
*.wav *.mp3 *.ogg                # 오디오
*.mp4 *.mov                      # 영상
    → filter=lfs diff=lfs merge=lfs -text
```

**2. 같은 파일에 텍스트 자산 개행 통일**

```
*.cs *.unity *.prefab *.asset *.meta *.mat *.anim *.controller *.json *.md
    → text eol=lf
```

Windows 에서 작업하지만 저장소는 LF 로 고정. `.unity` / `.prefab` 은 YAML 텍스트라 개행이 섞이면 diff 가 파일 전체로 번지고 병합 충돌이 무의미하게 커진다.

**3. `.gitignore` 를 Unity 기준으로 재작성 (349줄 → 필요한 것만)**

핵심은 한 줄이다:

```
!/[Aa]ssets/**/*.meta
```

`.meta` 는 Unity 가 에셋 GUID·임포트 설정을 담아두는 파일이다. 이게 빠지면 다른 머신에서 참조가 전부 끊긴다. 무시 규칙에 휩쓸리지 않도록 예외로 못 박았다.

## Result — 결과

- 앞으로 커밋되는 바이너리는 포인터(≈130바이트)로만 히스토리에 들어간다.
- `.unity` / `.prefab` diff 가 실제 변경분만 보여준다.
- `.meta` 누락으로 인한 참조 깨짐 가능성 차단.

**알려진 잔여 이슈(수용함)**: 초기 커밋(`204aefc`)이 LFS 설정보다 **먼저** 들어갔고, 이미 원격에 푸시된 상태다. 그 커밋 안의 `Assets/TutorialInfo/Icons/URP.png` 는 24KB raw blob 으로 히스토리에 남는다. 히스토리 재작성(`git lfs migrate import`)은 force push 를 요구하는데, 얻는 게 24KB짜리 튜토리얼 아이콘 하나라 하지 않기로 했다. **앞으로 들어올 바이너리가 LFS 를 타는 것은 `.gitattributes` 로 이미 보장된다.**
→ `docs/troubleshooting/lfs-after-first-commit.md`

## 이력서 한 줄

> Unity 프로젝트 첫 커밋 단계에서 Git LFS·`.gitattributes` 개행 정책·`.meta` 예외 규칙을 설계해, 바이너리 자산이 히스토리에 누적되는 문제와 YAML 자산의 병합 충돌을 사전에 차단했습니다.
