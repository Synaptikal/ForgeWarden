using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor.CLI
{
    /// <summary>
    /// Output formatters for RSV CLI results.
    /// Partial class of RsvCommandLine — see RsvCommandLine.cs for the core.
    /// </summary>
    public static partial class RsvCommandLine
    {
        private static void OutputConsole(List<LGD_ValidationReport> reports, CommandLineOptions options)
        {
            Debug.Log("[RSV CLI] Validation Results:");
            Debug.Log("===========================");

            int totalErrors = 0, totalWarnings = 0, totalCritical = 0;

            foreach (var report in reports)
            {
                Debug.Log($"Status: {report.OverallStatus} | Entries: {report.Entries.Count}");

                foreach (var entry in report.Entries)
                {
                    var label = entry.Status switch
                    {
                        ValidationStatus.Critical => "CRITICAL",
                        ValidationStatus.Error    => "ERROR",
                        ValidationStatus.Warning  => "WARNING",
                        ValidationStatus.Info     => "INFO",
                        _                         => "PASS"
                    };

                    Debug.Log($"  [{label}] {entry.Category}: {entry.Message}");

                    if (entry.Status == ValidationStatus.Critical)     totalCritical++;
                    else if (entry.Status == ValidationStatus.Error)   totalErrors++;
                    else if (entry.Status == ValidationStatus.Warning) totalWarnings++;
                }
            }

            Debug.Log("===========================");
            Debug.Log($"Summary: {totalCritical} Critical, {totalErrors} Errors, {totalWarnings} Warnings");

            if (options.FailOnError && (totalErrors > 0 || totalCritical > 0))
                Debug.LogError("[RSV CLI] Validation failed!");
            else
                Debug.Log("[RSV CLI] Validation passed!");
        }

        private static void OutputJson(List<LGD_ValidationReport> reports, CommandLineOptions options)
        {
            var output = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                totalReports = reports.Count,
                reports = reports.Select(r => new
                {
                    overallStatus = r.OverallStatus.ToString(),
                    entries = r.Entries.Select(e => new
                    {
                        status       = e.Status.ToString(),
                        category     = e.Category,
                        message      = e.Message,
                        suggestedFix = e.SuggestedFix,
                        assetPath    = e.AssetPath
                    })
                })
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(output, Newtonsoft.Json.Formatting.Indented);
            WriteOrLog(json, options.OutputFile, "Results");
        }

        private static void OutputXml(List<LGD_ValidationReport> reports, CommandLineOptions options)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<rsvValidationResults>");
            sb.AppendLine($"  <timestamp>{DateTime.UtcNow:O}</timestamp>");
            sb.AppendLine($"  <totalReports>{reports.Count}</totalReports>");
            sb.AppendLine("  <reports>");

            foreach (var report in reports)
            {
                sb.AppendLine("    <report>");
                sb.AppendLine($"      <overallStatus>{report.OverallStatus}</overallStatus>");
                sb.AppendLine("      <entries>");

                foreach (var entry in report.Entries)
                {
                    sb.AppendLine("        <entry>");
                    sb.AppendLine($"          <status>{entry.Status}</status>");
                    sb.AppendLine($"          <category>{entry.Category}</category>");
                    sb.AppendLine($"          <message>{Escape(entry.Message)}</message>");
                    if (!string.IsNullOrEmpty(entry.SuggestedFix))
                        sb.AppendLine($"          <suggestedFix>{Escape(entry.SuggestedFix)}</suggestedFix>");
                    if (!string.IsNullOrEmpty(entry.AssetPath))
                        sb.AppendLine($"          <assetPath>{Escape(entry.AssetPath)}</assetPath>");
                    sb.AppendLine("        </entry>");
                }

                sb.AppendLine("      </entries>");
                sb.AppendLine("    </report>");
            }

            sb.AppendLine("  </reports>");
            sb.Append("</rsvValidationResults>");
            WriteOrLog(sb.ToString(), options.OutputFile, "Results");
        }

        private static void OutputJUnit(List<LGD_ValidationReport> reports, CommandLineOptions options)
        {
            int totalTests = 0, totalFailures = 0, totalErrors = 0;

            foreach (var report in reports)
            {
                totalTests += report.Entries.Count;
                foreach (var entry in report.Entries)
                {
                    if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                        totalErrors++;
                    else if (entry.Status == ValidationStatus.Warning)
                        totalFailures++;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<testsuites>");
            sb.AppendLine($"  <testsuite name=\"RSV Validation\" tests=\"{totalTests}\" " +
                          $"failures=\"{totalFailures}\" errors=\"{totalErrors}\">");

            foreach (var report in reports)
            {
                foreach (var entry in report.Entries)
                {
                    sb.Append($"    <testcase name=\"{entry.Category}\" classname=\"RSV.{report.ToolId}\"");

                    if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                    {
                        sb.AppendLine(">");
                        sb.AppendLine($"      <error type=\"{entry.Status}\" message=\"{Escape(entry.Message)}\" />");
                        sb.AppendLine("    </testcase>");
                    }
                    else if (entry.Status == ValidationStatus.Warning)
                    {
                        sb.AppendLine(">");
                        sb.AppendLine($"      <failure type=\"{entry.Status}\" message=\"{Escape(entry.Message)}\" />");
                        sb.AppendLine("    </testcase>");
                    }
                    else
                    {
                        sb.AppendLine(" />");
                    }
                }
            }

            sb.AppendLine("  </testsuite>");
            sb.Append("</testsuites>");
            WriteOrLog(sb.ToString(), options.OutputFile, "Results");
        }

        // ── Shared helpers ────────────────────────────────────────
        private static string Escape(string s) =>
            System.Security.SecurityElement.Escape(s);

        private static void WriteOrLog(string content, string outputFile, string label)
        {
            if (!string.IsNullOrEmpty(outputFile))
            {
                File.WriteAllText(outputFile, content);
                Debug.Log($"[RSV CLI] {label} written to: {outputFile}");
            }
            else
            {
                Debug.Log(content);
            }
        }
    }
}
