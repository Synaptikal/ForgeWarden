using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor.CLI
{
    /// <summary>
    /// Command-line interface for RSV validation.
    /// Enables headless validation for CI/CD pipelines.
    ///
    /// Note: HTTP operations use RsvAsyncHttpFetcher which maintains a static
    /// HttpClient instance for connection pooling. No explicit cleanup is required.
    ///
    /// Output formatters are in RsvCommandLine.Formatters.cs (partial class).
    /// </summary>
    public static partial class RsvCommandLine
    {
        private static bool _hasValidationErrors = false;
        private static readonly string[] HelpText = new[]
        {
            "RSV Command-Line Interface",
            "===========================",
            "",
            "Usage:",
            "  Unity.exe -quit -batchmode -executeMethod LiveGameDev.RSV.Editor.CLI.RsvCommandLine.Run [options]",
            "",
            "Options:",
            "  --validate-all              Validate all bindings in the project",
            "  --validate <binding>        Validate specific binding by name",
            "  --schema <schemaId>         Validate against specific schema",
            "  --json <file>               Validate specific JSON file",
            "  --output <file>             Output results to file (JSON or XML)",
            "  --format <format>           Output format: console, json, xml, junit",
            "  --verbose                   Enable verbose logging",
            "  --fail-on-error             Exit with error code on validation failure",
            "  --help                      Show this help message",
            "",
            "Exit Codes:",
            "  0 - Success, 1 - Validation failed, 2 - Argument error, 3 - Runtime error"
        };

        /// <summary>Entry point for Unity's -executeMethod flag.</summary>
        public static void Run()
        {
            try
            {
                _hasValidationErrors = false;
                var args    = Environment.GetCommandLineArgs();
                var options = ParseArguments(args);

                if (options.ShowHelp)            { PrintHelp(); EditorApplication.Exit(0); return; }
                if (options.ValidateAll)           ValidateAllBindings(options);
                else if (!string.IsNullOrEmpty(options.BindingName)) ValidateBinding(options);
                else if (!string.IsNullOrEmpty(options.JsonFile))    ValidateJsonFile(options);
                else                             { PrintHelp(); EditorApplication.Exit(2); return; }

                EditorApplication.Exit(options.FailOnError && _hasValidationErrors ? 1 : 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RSV CLI] Runtime error: {ex.Message}");
                Debug.LogException(ex);
                EditorApplication.Exit(3);
            }
        }

        private static CommandLineOptions ParseArguments(string[] args)
        {
            var options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--validate-all":  options.ValidateAll = true;  break;
                    case "--verbose":       options.Verbose = true;      break;
                    case "--fail-on-error": options.FailOnError = true;  break;
                    case "--help":          options.ShowHelp = true;     break;

                    case "--validate":
                        if (!TryConsumeArg(args, ref i, out var bindingName, "--validate")) return options;
                        options.BindingName = bindingName;
                        break;

                    case "--schema":
                        if (!TryConsumeArg(args, ref i, out var schemaId, "--schema")) return options;
                        options.SchemaId = schemaId;
                        break;

                    case "--json":
                        if (!TryConsumeArg(args, ref i, out var jsonFile, "--json")) return options;
                        options.JsonFile = jsonFile;
                        break;

                    case "--output":
                        if (!TryConsumeArg(args, ref i, out var outputFile, "--output")) return options;
                        options.OutputFile = outputFile;
                        break;

                    case "--format":
                        if (!TryConsumeArg(args, ref i, out var format, "--format")) return options;
                        options.OutputFormat = ParseOutputFormat(format);
                        break;
                }
            }

            return options;
        }

        private static bool TryConsumeArg(string[] args, ref int i, out string value, string flag)
        {
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                value = args[++i];
                return true;
            }
            Debug.LogError($"[RSV CLI] Error: {flag} requires an argument.");
            EditorApplication.Exit(2);
            value = null;
            return false;
        }

        private static OutputFormat ParseOutputFormat(string format) => format.ToLower() switch
        {
            "console" => OutputFormat.Console,
            "json"    => OutputFormat.Json,
            "xml"     => OutputFormat.Xml,
            "junit"   => OutputFormat.JUnit,
            _         => OutputFormat.Console
        };

        private static void ValidateAllBindings(CommandLineOptions options)
        {
            Debug.Log("[RSV CLI] Validating all bindings...");

            var bindings = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>();
            var results  = new List<LGD_ValidationReport>();

            foreach (var binding in bindings)
            {
                var report = RsvValidator.ValidateBinding(binding);
                results.Add(report);

                if (report.HasErrors || report.HasCritical)
                    _hasValidationErrors = true;

                if (options.Verbose)
                    Debug.Log($"[RSV CLI] Binding '{binding.name}': {report.OverallStatus}");
            }

            OutputResults(results, options);
        }

        private static void ValidateBinding(CommandLineOptions options)
        {
            Debug.Log($"[RSV CLI] Validating binding: {options.BindingName}");

            var binding = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>()
                .FirstOrDefault(b => b.name == options.BindingName);

            if (binding == null)
            {
                Debug.LogError($"[RSV CLI] Binding not found: {options.BindingName}");
                EditorApplication.Exit(2);
                return;
            }

            var report = RsvValidator.ValidateBinding(binding);
            if (report.HasErrors || report.HasCritical) _hasValidationErrors = true;
            OutputResults(new[] { report }, options);
        }

        private static void ValidateJsonFile(CommandLineOptions options)
        {
            Debug.Log($"[RSV CLI] Validating JSON file: {options.JsonFile}");

            if (!File.Exists(options.JsonFile))
            {
                Debug.LogError($"[RSV CLI] File not found: {options.JsonFile}");
                EditorApplication.Exit(2);
                return;
            }

            var fullPath = Path.GetFullPath(options.JsonFile);
            if (!fullPath.StartsWith(Application.dataPath) && !Path.IsPathRooted(options.JsonFile))
            {
                Debug.LogError($"[RSV CLI] Invalid file path (traversal detected): {options.JsonFile}");
                EditorApplication.Exit(2);
                return;
            }

            if (string.IsNullOrEmpty(options.SchemaId))
            {
                Debug.LogError("[RSV CLI] --schema <schemaId> required for JSON file validation.");
                EditorApplication.Exit(2);
                return;
            }

            var schema = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>()
                .FirstOrDefault(s => s.SchemaId == options.SchemaId);

            if (schema == null)
            {
                Debug.LogError($"[RSV CLI] Schema not found: {options.SchemaId}");
                EditorApplication.Exit(2);
                return;
            }

            var json   = File.ReadAllText(options.JsonFile);
            var report = RsvValidator.Validate(schema, json);
            if (report.HasErrors || report.HasCritical) _hasValidationErrors = true;
            OutputResults(new[] { report }, options);
        }

        private static void OutputResults(IEnumerable<LGD_ValidationReport> reports, CommandLineOptions options)
        {
            var list = reports as List<LGD_ValidationReport> ?? new List<LGD_ValidationReport>(reports);
            switch (options.OutputFormat)
            {
                case OutputFormat.Console: OutputConsole(list, options); break;
                case OutputFormat.Json:    OutputJson(list, options);    break;
                case OutputFormat.Xml:     OutputXml(list, options);     break;
                case OutputFormat.JUnit:   OutputJUnit(list, options);   break;
            }
        }

        private static void PrintHelp()
        {
            foreach (var line in HelpText) Debug.Log(line);
        }

        // ── Types ────────────────────────────────────────────────
        private class CommandLineOptions
        {
            public bool ValidateAll  { get; set; }
            public string BindingName { get; set; }
            public string SchemaId   { get; set; }
            public string JsonFile   { get; set; }
            public string OutputFile { get; set; }
            public OutputFormat OutputFormat { get; set; } = OutputFormat.Console;
            public bool Verbose      { get; set; }
            public bool FailOnError  { get; set; }
            public bool ShowHelp     { get; set; }
        }

        private enum OutputFormat { Console, Json, Xml, JUnit }
    }
}
