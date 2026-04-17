using System.Collections;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Handle (tay cầm) đại diện cho một âm thanh đang bám theo một Transform.
    /// Trả về từ AudioManager.PlaySFXFollowing().
    ///
    /// Dùng để:
    ///   - Kiểm tra trạng thái: followingSound.IsPlaying
    ///   - Dừng âm thanh:       followingSound.Stop()
    ///   - Tự động dừng khi NPC bị hủy hoặc disable (qua SoundEmitter)
    ///
    /// Ví dụ:
    ///   // Trong script NPC
    ///   _footstepHandle = AudioManager.Instance.PlaySFXFollowing(footstepClip, transform, loop: true);
    ///   // Khi NPC chết:
    ///   _footstepHandle?.Stop();
    /// </summary>
    public class FollowingSound
{
    private readonly AudioSource   _source;
    private readonly Transform     _target;
    private readonly AudioManager  _manager;
    private readonly float         _minDist;
    private readonly float         _maxDist;

    private bool _stopped;

    public bool IsPlaying => !_stopped && _source != null && _source.isPlaying;
    public bool IsStopped => _stopped;

    public FollowingSound(
        AudioSource  source,
        Transform    target,
        AudioManager manager,
        float        minDist,
        float        maxDist)
    {
        _source  = source;
        _target  = target;
        _manager = manager;
        _minDist = minDist;
        _maxDist = maxDist;
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dừng âm thanh và trả AudioSource về pool ngay lập tức.
    /// An toàn khi gọi nhiều lần.
    /// </summary>
    public void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        _manager.ReturnToPool(_source);
    }

    // ─────────────────────────────────────────────
    //  Internal Coroutine (chạy bởi AudioManager)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Coroutine cập nhật vị trí và âm lượng mỗi frame.
    /// Tự kết thúc khi: đã Stop(), target bị hủy, hoặc âm thanh kết thúc (non-loop).
    /// </summary>
    public IEnumerator UpdateRoutine()
    {
        while (!_stopped)
        {
            // Target bị hủy (NPC chết, bị Destroy) → dừng luôn
            if (_target == null)
            {
                Stop();
                yield break;
            }

            // Cập nhật vị trí bám theo target
            _source.transform.position = _target.position;

            // Cập nhật âm lượng theo khoảng cách tới Player
            _source.volume = _manager.CalculateDistanceVolume(
                _target.position, _minDist, _maxDist
            );

            // Âm thanh non-loop đã phát xong → trả về pool
            if (!_source.loop && !_source.isPlaying)
            {
                Stop();
                yield break;
            }

            yield return null; // Chờ frame kế tiếp
        }
    }
}
}
