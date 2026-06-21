import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class FilterWinningSequencesCliTests(unittest.TestCase):
    def test_cli_generates_unique_sequences_from_victory_sessions(self):
        repo_root = Path(__file__).resolve().parents[2]
        script_path = repo_root / "TreinamentoML" / "scripts" / "filter_winning_sequences.py"
        self.assertTrue(script_path.exists(), f"Script not found: {script_path}")

        with tempfile.TemporaryDirectory() as temp_dir:
            input_dir = Path(temp_dir) / "provenance_data"
            input_dir.mkdir()
            output_path = Path(temp_dir) / "winning_sequences.json"

            self.write_session(
                input_dir / "victory.json",
                "victory",
                [
                    "SessionStart",
                    "PlayerJump",
                    "BossAttack",
                    "PlayerDash",
                    "PlayerDamageDealt",
                    "PlayerAttack",
                    "BossDamageTaken",
                    "PlayerJump",
                    "PlayerDash",
                    "BossAttack",
                    "PlayerAttack",
                    "BossDamageTaken",
                    "BossDeath",
                    "SessionEnd",
                ],
            )
            self.write_session(
                input_dir / "defeat.json",
                "defeat",
                [
                    "PlayerJump",
                    "PlayerDash",
                    "PlayerAttack",
                    "BossDamageTaken",
                ],
            )

            result = subprocess.run(
                [
                    sys.executable,
                    str(script_path),
                    "--input",
                    str(input_dir),
                    "--output",
                    str(output_path),
                ],
                check=True,
                capture_output=True,
                text=True,
            )

            self.assertIn("winning_sequences.json", result.stdout)
            output = json.loads(output_path.read_text(encoding="utf-8"))

        self.assertEqual(3, output["sequenceLength"])
        self.assertEqual(["PlayerJump", "PlayerAttack", "PlayerDash"], output["allowedActionTypes"])
        self.assertEqual(
            {
                "sourceFiles": 2,
                "victorySessions": 1,
                "bossDamageEvents": 2,
                "extractedSequences": 2,
                "uniqueSequences": 1,
            },
            output["summary"],
        )
        self.assertEqual(
            [
                {
                    "actions": ["PlayerJump", "PlayerDash", "PlayerAttack"],
                    "frequency": 2,
                }
            ],
            output["sequences"],
        )

    @staticmethod
    def write_session(path, result, action_types):
        events = [
            {
                "eventId": f"event-{index}",
                "timestamp": float(index),
                "actorId": "Player" if action_type.startswith("Player") else "Boss",
                "actionType": action_type,
                "parentEventId": None if index == 0 else f"event-{index - 1}",
            }
            for index, action_type in enumerate(action_types)
        ]
        path.write_text(
            json.dumps(
                {
                    "sessionId": path.stem,
                    "result": result,
                    "events": events,
                },
                indent=2,
            ),
            encoding="utf-8",
        )


if __name__ == "__main__":
    unittest.main()
