namespace Script.Audio
{
    /// <summary>
    /// Config struct cho PlaySound API mới.
    /// Gom nhóm các tham số tùy chọn thành 1 struct thay vì positional parameters.
    /// </summary>
    public readonly struct PlaySoundConfig
    {
        public static readonly PlaySoundConfig Default = new PlaySoundConfig(
            loop: false,
            minDistance: 0f,
            maxDistance: 0f,
            volumeScale: 1f,
            forceReplace: false
        );

        public readonly bool Loop;
        public readonly float MinDistance;
        public readonly float MaxDistance;
        public readonly float VolumeScale;
        public readonly bool ForceReplace;

        public PlaySoundConfig(
            bool  loop         = false,
            float minDistance  = 0f,
            float maxDistance  = 0f,
            float volumeScale  = 1f,
            bool  forceReplace = false)
        {
            Loop = loop;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            VolumeScale = volumeScale;
            ForceReplace = forceReplace;
        }
    }
}
