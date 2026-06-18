using System;
using System.IO;
using UnityEngine;

namespace TerraSilente.Provenance
{
    public static class ProvenanceExporter
    {
        public static string Export(ProvenanceGraph graph, string outputDirectory = null, string sessionId = null)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var targetDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? GetDefaultOutputDirectory()
                : outputDirectory;

            Directory.CreateDirectory(targetDirectory);

            var fileName = BuildFileName(sessionId);
            var filePath = Path.Combine(targetDirectory, fileName);

            graph.ExportToJson(filePath);

            return filePath;
        }

        private static string GetDefaultOutputDirectory()
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repositoryRoot, "TreinamentoML", "provenance_data");
        }

        private static string BuildFileName(string sessionId)
        {
            var safeSessionId = string.IsNullOrWhiteSpace(sessionId)
                ? DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")
                : SanitizeFileName(sessionId);

            return safeSessionId + ".json";
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}
