import argparse
import json
from collections import Counter
from pathlib import Path


ALLOWED_ACTION_TYPES = ["PlayerJump", "PlayerAttack", "PlayerDash"]
DEFAULT_SEQUENCE_LENGTH = 3


def build_winning_sequences(input_dir, sequence_length=DEFAULT_SEQUENCE_LENGTH):
    source_files = sorted(Path(input_dir).glob("*.json"))
    sequence_counts = Counter()
    victory_sessions = 0
    boss_damage_events = 0
    extracted_sequences = 0

    for source_file in source_files:
        session = load_json(source_file)
        if session.get("result") != "victory":
            continue

        victory_sessions += 1
        sequences, damage_events = extract_sequences(
            session.get("events", []),
            sequence_length,
            ALLOWED_ACTION_TYPES,
        )
        boss_damage_events += damage_events
        extracted_sequences += len(sequences)
        sequence_counts.update(sequences)

    return {
        "sequenceLength": sequence_length,
        "allowedActionTypes": ALLOWED_ACTION_TYPES,
        "summary": {
            "sourceFiles": len(source_files),
            "victorySessions": victory_sessions,
            "bossDamageEvents": boss_damage_events,
            "extractedSequences": extracted_sequences,
            "uniqueSequences": len(sequence_counts),
        },
        "sequences": [
            {
                "actions": list(actions),
                "frequency": frequency,
            }
            for actions, frequency in sorted(
                sequence_counts.items(),
                key=lambda item: (-item[1], item[0]),
            )
        ],
    }


def extract_sequences(events, sequence_length, allowed_action_types):
    allowed_actions = set(allowed_action_types)
    recent_player_actions = []
    sequences = []
    boss_damage_events = 0

    for event in events:
        action_type = event.get("actionType")
        if action_type == "BossDamageTaken":
            boss_damage_events += 1
            if len(recent_player_actions) >= sequence_length:
                sequences.append(tuple(recent_player_actions[-sequence_length:]))

        if action_type in allowed_actions:
            recent_player_actions.append(action_type)

    return sequences, boss_damage_events


def load_json(path):
    with Path(path).open("r", encoding="utf-8") as file:
        return json.load(file)


def save_json(data, output_path):
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(data, indent=2), encoding="utf-8")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Generate winning player action sequences from provenance JSON files."
    )
    parser.add_argument(
        "--input",
        default=str(Path("TreinamentoML") / "provenance_data"),
        help="Directory containing provenance JSON files.",
    )
    parser.add_argument(
        "--output",
        default=str(Path("TreinamentoML") / "winning_sequences.json"),
        help="Path to the generated winning_sequences.json file.",
    )
    parser.add_argument(
        "--sequence-length",
        type=int,
        default=DEFAULT_SEQUENCE_LENGTH,
        help="Number of player actions to extract before each BossDamageTaken event.",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    output = build_winning_sequences(args.input, args.sequence_length)
    save_json(output, args.output)
    print(f"Generated {args.output}")


if __name__ == "__main__":
    main()
