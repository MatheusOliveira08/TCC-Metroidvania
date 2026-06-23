using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerraSilente.Provenance
{
    [DisallowMultipleComponent]
    public class ProvenanceRewardShaper : MonoBehaviour
    {
        private const int DefaultSequenceLength = 3;

        [SerializeField] private float sequenceMatchReward = 2f;
        [SerializeField] private string winningSequencesFilePath;

        private readonly List<string[]> winningSequences = new();
        private readonly List<string> recentActions = new();
        private int sequenceLength = DefaultSequenceLength;

        public float SequenceMatchReward
        {
            get => sequenceMatchReward;
            set => sequenceMatchReward = value;
        }

        public int SequenceLength => sequenceLength;

        public int LoadedSequenceCount => winningSequences.Count;

        public int BufferedActionCount => recentActions.Count;

        private void OnEnable()
        {
            if (winningSequences.Count == 0)
            {
                TryLoadFromFile();
            }
        }

        public void LoadFromJson(string json)
        {
            winningSequences.Clear();
            recentActions.Clear();
            sequenceLength = DefaultSequenceLength;

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var document = JsonUtility.FromJson<WinningSequencesDocument>(json);
            if (document == null)
            {
                return;
            }

            sequenceLength = document.sequenceLength > 0
                ? document.sequenceLength
                : DefaultSequenceLength;

            if (document.sequences == null)
            {
                return;
            }

            for (var i = 0; i < document.sequences.Length; i++)
            {
                var sequence = document.sequences[i];
                if (sequence?.actions == null || sequence.actions.Length != sequenceLength)
                {
                    continue;
                }

                var copiedActions = new string[sequence.actions.Length];
                Array.Copy(sequence.actions, copiedActions, sequence.actions.Length);
                winningSequences.Add(copiedActions);
            }
        }

        public bool TryLoadFromFile(string filePath = null)
        {
            var resolvedFilePath = ResolveWinningSequencesFilePath(filePath);
            if (!File.Exists(resolvedFilePath))
            {
                return false;
            }

            LoadFromJson(File.ReadAllText(resolvedFilePath));
            return true;
        }

        public float RecordAction(string actionType)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                return 0f;
            }

            recentActions.Add(actionType);
            while (recentActions.Count > sequenceLength)
            {
                recentActions.RemoveAt(0);
            }

            if (recentActions.Count < sequenceLength)
            {
                return 0f;
            }

            return HasMatchingCurrentSequence() ? sequenceMatchReward : 0f;
        }

        public void ResetBuffer()
        {
            recentActions.Clear();
        }

        private bool HasMatchingCurrentSequence()
        {
            for (var sequenceIndex = 0; sequenceIndex < winningSequences.Count; sequenceIndex++)
            {
                var sequence = winningSequences[sequenceIndex];
                var isMatch = true;
                for (var actionIndex = 0; actionIndex < sequenceLength; actionIndex++)
                {
                    if (sequence[actionIndex] != recentActions[actionIndex])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        private string ResolveWinningSequencesFilePath(string explicitFilePath)
        {
            if (!string.IsNullOrWhiteSpace(explicitFilePath))
            {
                return explicitFilePath;
            }

            if (!string.IsNullOrWhiteSpace(winningSequencesFilePath))
            {
                return winningSequencesFilePath;
            }

            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repositoryRoot, "TreinamentoML", "winning_sequences.json");
        }

        [Serializable]
        private sealed class WinningSequencesDocument
        {
            public int sequenceLength = DefaultSequenceLength;
            public WinningSequence[] sequences = Array.Empty<WinningSequence>();
        }

        [Serializable]
        private sealed class WinningSequence
        {
            public string[] actions = Array.Empty<string>();
        }
    }
}
