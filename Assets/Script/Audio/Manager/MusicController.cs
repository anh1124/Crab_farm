using System.Collections;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Điều khiển nhạc nền với hiệu ứng crossfade mượt mà.
    /// Dùng 2 AudioSource (A và B) xen kẽ nhau để tạo transition không bị ngắt tiếng.
    ///
    /// Ví dụ dùng (từ AudioManager):
    ///   _music.TransitionTo(dayClip, 2f);   // chuyển sang nhạc ban ngày trong 2 giây
    ///   _music.TransitionTo(nightClip, 3f); // crossfade sang nhạc ban đêm
    /// </summary>
    public class MusicController
{
    private readonly AudioSource    _sourceA;
    private readonly AudioSource    _sourceB;
    private readonly MonoBehaviour  _coroutineRunner; // AudioManager chạy coroutine thay

    private AudioSource _activeSource;   // Source đang phát hiện tại
    private Coroutine   _activeTransition;

    public AudioClip CurrentClip => _activeSource?.clip;
    public bool      IsPlaying   => _activeSource != null && _activeSource.isPlaying;

    public MusicController(AudioSource sourceA, AudioSource sourceB, MonoBehaviour runner)
    {
        _sourceA         = sourceA;
        _sourceB         = sourceB;
        _coroutineRunner = runner;
        _activeSource    = sourceA;

        // Khởi tạo cả hai về trạng thái sạch
        Reset(_sourceA);
        Reset(_sourceB);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Chuyển sang bài nhạc mới với hiệu ứng crossfade.
    /// Nếu đang có transition dở, hủy ngay và bắt đầu transition mới.
    /// </summary>
    public void TransitionTo(AudioClip newClip, float duration = 1.5f)
    {
        if (newClip == null) return;

        // Nếu đang phát đúng bài này rồi thì bỏ qua
        if (_activeSource.isPlaying && _activeSource.clip == newClip) return;

        // Hủy transition cũ nếu có
        if (_activeTransition != null)
            _coroutineRunner.StopCoroutine(_activeTransition);

        _activeTransition = _coroutineRunner.StartCoroutine(
            CrossfadeRoutine(newClip, duration)
        );
    }

    /// <summary>Dừng toàn bộ nhạc với fade out.</summary>
    public void StopAll(float duration = 1.0f)
    {
        if (_activeTransition != null)
            _coroutineRunner.StopCoroutine(_activeTransition);

        _activeTransition = _coroutineRunner.StartCoroutine(
            FadeOutRoutine(_activeSource, duration)
        );
    }

    /// <summary>Dừng ngay lập tức, không fade.</summary>
    public void StopImmediate()
    {
        if (_activeTransition != null)
            _coroutineRunner.StopCoroutine(_activeTransition);

        _sourceA.Stop();
        _sourceB.Stop();
    }

    // ─────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────

    /// <summary>
    /// Fade out source đang phát (inActive) đồng thời
    /// fade in source mới (outgoing) với clip mới.
    /// </summary>
    private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        AudioSource outgoing = _activeSource;
        AudioSource incoming = (outgoing == _sourceA) ? _sourceB : _sourceA;

        // Chuẩn bị incoming
        incoming.clip   = newClip;
        incoming.volume = 0f;
        incoming.loop   = true;
        incoming.Play();

        _activeSource = incoming;

        float elapsed = 0f;
        float startVolume = outgoing.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            incoming.volume = Mathf.Lerp(0f, 1f, t);
            outgoing.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        outgoing.Stop();
        outgoing.clip   = null;
        outgoing.volume = 1f; // Reset để lần sau dùng được ngay
        incoming.volume = 1f;

        _activeTransition = null;
    }

    private IEnumerator FadeOutRoutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            source.volume  = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume     = 1f;
        _activeTransition = null;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private static void Reset(AudioSource source)
    {
        source.Stop();
        source.clip        = null;
        source.volume      = 1f;
        source.loop        = true;
        source.spatialBlend = 0f; // BGM luôn là 2D
        source.playOnAwake  = false;
    }
}
}
