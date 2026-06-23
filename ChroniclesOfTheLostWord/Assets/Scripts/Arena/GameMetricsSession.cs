using System;

namespace TerraSilente.Arena
{
    [Serializable]
    public sealed class GameMetricsSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string BossType { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public float StartTimeSeconds { get; set; }
        public float EndTimeSeconds { get; set; }
        public float DurationSeconds => Math.Max(0f, EndTimeSeconds - StartTimeSeconds);
        public int EpisodeSteps { get; set; }
        public float BossDamageTaken { get; set; }
        public float PlayerDamageTaken { get; set; }
        public int PlayerJumpCount { get; set; }
        public int PlayerAttackCount { get; set; }
        public int PlayerDashCount { get; set; }
        public int BossAttackCount { get; set; }
        public int PpoIdleCount { get; set; }
        public int PpoMoveLeftCount { get; set; }
        public int PpoMoveRightCount { get; set; }
        public int PpoJumpCount { get; set; }
        public int PpoAttackCount { get; set; }
        public int PpoDashCount { get; set; }
    }
}
