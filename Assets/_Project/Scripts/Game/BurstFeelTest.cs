// BurstFeelTest.cs — 대파열 연출 무게 확인용 임시 스크립트
//
// 목적: 되치기가 경기당 한 번뿐이어도 기둥이 되는지를 눈과 귀로 판정한다.
//       재는 것은 동작이 아니라 시간 구조와 대비다. 되치기 애니메이션은 필요 없다.
//
// 위치: Assets/_Project/Game/Scripts/ (CounterTime.Game 쪽. Core는 건드리지 않는다)
// 임시 파일이므로 판정이 끝나면 지운다.
//
// 키:
//   1  성공판 — 예고 → 정지 → 흑백반전 → 복귀
//   2  근접 실패판 — 짧은 감속만. 반전·굉음 없음
//   Q  예고 0.5초   W  예고 0.8초   E  예고 1.2초   (플레이 중 즉석 비교)
//   R  비상 복구 (timeScale 원복. 코루틴이 중간에 죽었을 때)
//
// 주의 3가지는 파일 하단 SETUP 주석 참고.
 
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
 
public class BurstFeelTest : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("전체화면 UI Image. 평소 alpha 0")]
    public Image flashOverlay;
    [Tooltip("없어도 됨. 3번 판정(소리 끄고 보기)은 오히려 없는 편이 정확")]
    public AudioSource impactSfx;
 
    [Header("예고 (telegraph)")]
    public float telegraphSeconds = 0.8f;
    [Range(0.05f, 1f)] public float telegraphTimeScale = 0.25f;
 
    [Header("정지 (hitstop)")]
    public float freezeSeconds = 0.25f;
 
    [Header("흑백반전 (임시 대역)")]
    [Tooltip("진짜 화면 반전은 포스트 이펙트가 필요하다. 여기서는 흑/백 프레임 교대로 대신한다")]
    public float invertFrameSeconds = 0.05f;
    [Range(1, 8)] public int invertFrames = 3;
 
    [Header("근접 실패 (near miss)")]
    public float nearMissSeconds = 0.08f;
    [Range(0.05f, 1f)] public float nearMissTimeScale = 0.5f;
 
    bool running;
 
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { Recover(); return; }
        if (running) return;
 
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartCoroutine(Burst());
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartCoroutine(NearMiss());
 
        if (Input.GetKeyDown(KeyCode.Q)) telegraphSeconds = 0.5f;
        if (Input.GetKeyDown(KeyCode.W)) telegraphSeconds = 0.8f;
        if (Input.GetKeyDown(KeyCode.E)) telegraphSeconds = 1.2f;
    }
 
    IEnumerator Burst()
    {
        running = true;
 
        // 예고 — 느려지면서 "온다"를 만든다
        Time.timeScale = telegraphTimeScale;
        yield return new WaitForSecondsRealtime(telegraphSeconds);
 
        // 정지 — 완전 정지. timeScale 0이므로 이후 대기는 전부 Realtime
        Time.timeScale = 0f;
        if (impactSfx) impactSfx.Play();
 
        // 반전 — 흑/백 교대
        for (int i = 0; i < invertFrames; i++)
        {
            SetFlash(i % 2 == 0 ? Color.white : Color.black);
            yield return new WaitForSecondsRealtime(invertFrameSeconds);
        }
        SetFlash(Color.clear);
 
        float rest = freezeSeconds - invertFrames * invertFrameSeconds;
        if (rest > 0f) yield return new WaitForSecondsRealtime(rest);
 
        Time.timeScale = 1f;
        running = false;
    }
 
    IEnumerator NearMiss()
    {
        running = true;
        Time.timeScale = nearMissTimeScale;
        yield return new WaitForSecondsRealtime(nearMissSeconds);
        Time.timeScale = 1f;
        running = false;
    }
 
    void SetFlash(Color c) { if (flashOverlay) flashOverlay.color = c; }
 
    void Recover()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        SetFlash(Color.clear);
        running = false;
    }
 
    void OnDisable() { Time.timeScale = 1f; }
}
 