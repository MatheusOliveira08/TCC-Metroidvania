using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TerraSilente.Arena
{
    public static class GameMetricsExporter
    {
        public const string DefaultFileName = "boss_evaluation_metrics.csv";

        public const string Header =
            "session_id,boss_type,result,start_time_seconds,end_time_seconds,duration_seconds,episode_steps," +
            "boss_damage_taken,player_damage_taken,player_jump_count,player_attack_count,player_dash_count," +
            "boss_attack_count,ppo_idle_count,ppo_move_left_count,ppo_move_right_count,ppo_jump_count," +
            "ppo_attack_count,ppo_dash_count";

        public static string AppendSession(
            GameMetricsSession session,
            string outputDirectory,
            string fileName = DefaultFileName)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var resolvedDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? BuildDefaultOutputDirectory()
                : outputDirectory;
            var resolvedFileName = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName;

            Directory.CreateDirectory(resolvedDirectory);
            var csvPath = Path.Combine(resolvedDirectory, resolvedFileName);
            var shouldWriteHeader = !File.Exists(csvPath) || new FileInfo(csvPath).Length == 0;

            using var writer = new StreamWriter(csvPath, append: true, new UTF8Encoding(false));
            if (shouldWriteHeader)
            {
                writer.WriteLine(Header);
            }

            writer.WriteLine(ToCsvLine(session));
            return csvPath;
        }

        public static string BuildDefaultOutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "TreinamentoML", "evaluation_data"));
        }

        private static string ToCsvLine(GameMetricsSession session)
        {
            return string.Join(",", new[]
            {
                Escape(session.SessionId),
                Escape(session.BossType),
                Escape(session.Result),
                FormatFloat(session.StartTimeSeconds),
                FormatFloat(session.EndTimeSeconds),
                FormatFloat(session.DurationSeconds),
                session.EpisodeSteps.ToString(CultureInfo.InvariantCulture),
                FormatFloat(session.BossDamageTaken),
                FormatFloat(session.PlayerDamageTaken),
                session.PlayerJumpCount.ToString(CultureInfo.InvariantCulture),
                session.PlayerAttackCount.ToString(CultureInfo.InvariantCulture),
                session.PlayerDashCount.ToString(CultureInfo.InvariantCulture),
                session.BossAttackCount.ToString(CultureInfo.InvariantCulture),
                session.PpoIdleCount.ToString(CultureInfo.InvariantCulture),
                session.PpoMoveLeftCount.ToString(CultureInfo.InvariantCulture),
                session.PpoMoveRightCount.ToString(CultureInfo.InvariantCulture),
                session.PpoJumpCount.ToString(CultureInfo.InvariantCulture),
                session.PpoAttackCount.ToString(CultureInfo.InvariantCulture),
                session.PpoDashCount.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            var safeValue = value ?? string.Empty;
            if (!safeValue.Contains(",") && !safeValue.Contains("\"") && !safeValue.Contains("\n") && !safeValue.Contains("\r"))
            {
                return safeValue;
            }

            return "\"" + safeValue.Replace("\"", "\"\"") + "\"";
        }
    }
}
