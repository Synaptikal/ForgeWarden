using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validates URLs against SSRF (Server-Side Request Forgery) attacks.
    /// Resolves hostnames to IPs before validation to prevent DNS rebinding attacks.
    /// </summary>
    internal static class RsvUrlValidator
    {
        // Blocked IP ranges (CIDR notation)
        private static readonly string[] BlockedCidrRanges = new[]
        {
            "10.0.0.0/8",      // Private
            "172.16.0.0/12",   // Private
            "192.168.0.0/16",  // Private
            "127.0.0.0/8",     // Loopback
            "169.254.0.0/16",  // Link-local
            "0.0.0.0/8",       // Current network
            "100.64.0.0/10",   // Carrier-grade NAT
            "192.0.0.0/24",    // IETF Protocol Assignments
            "192.0.2.0/24",    // TEST-NET-1
            "198.18.0.0/15",   // Network benchmark tests
            "198.51.100.0/24", // TEST-NET-2
            "203.0.113.0/24",  // TEST-NET-3
            "224.0.0.0/4",     // Multicast
            "240.0.0.0/4",     // Reserved
            "255.255.255.255/32" // Broadcast
        };

        // Blocked IPv6 ranges (CIDR notation)
        private static readonly string[] BlockedIpv6CidrRanges = new[]
        {
            "::1/128",         // Loopback
            "fe80::/10",       // Link-local
            "fc00::/7",        // Unique local (private)
            "ff00::/8"         // Multicast
        };

        // Cloud metadata endpoints
        private static readonly string[] CloudMetadataHosts = new[]
        {
            "169.254.169.254",
            "metadata.google.internal",
            "metadata.google.internal.",
            "169.254.170.2",
            "fd00:ec2::254"
        };

        // Suspicious URL patterns
        private static readonly string[] SuspiciousPatterns = new[]
        {
            @"^file://",
            @"^ftp://",
            @"^sftp://",
            @"^dict://",
            @"^ldap://",
            @"^gopher://",
            @"^telnet://",
            @"^ssh://",
            @":\d{5,}/"
        };

        private static readonly List<(uint start, uint end)> BlockedIpRanges = new();

        static RsvUrlValidator()
        {
            foreach (var cidr in BlockedCidrRanges)
            {
                try
                {
                    var (start, end) = ParseCidrRange(cidr);
                    BlockedIpRanges.Add((start, end));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RSV] Failed to parse CIDR range '{cidr}': {ex.Message}");
                }
            }
        }

        public static RsvEditorValidationResult<bool> ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return RsvEditorValidationResult<bool>.Failure("URL is empty", ValidationStatus.Error);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return RsvEditorValidationResult<bool>.Failure($"Invalid URL format: {url}", ValidationStatus.Error);

            if (uri.Scheme != "https")
                return RsvEditorValidationResult<bool>.Failure($"Only HTTPS URLs are allowed. Found: {uri.Scheme}", ValidationStatus.Critical);

            var suspiciousResult = CheckSuspiciousPatterns(url);
            if (suspiciousResult.IsFailure) return suspiciousResult;

            var whitelistResult = CheckWhitelist(uri);
            if (whitelistResult.IsFailure) return whitelistResult;

            var blacklistResult = CheckBlacklist(uri);
            if (blacklistResult.IsFailure) return blacklistResult;

            return ValidateDnsResolution(uri);
        }

        public static async Task<RsvEditorValidationResult<bool>> ValidateUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return RsvEditorValidationResult<bool>.Failure("URL is empty", ValidationStatus.Error);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return RsvEditorValidationResult<bool>.Failure($"Invalid URL format: {url}", ValidationStatus.Error);

            if (uri.Scheme != "https")
                return RsvEditorValidationResult<bool>.Failure($"Only HTTPS URLs are allowed. Found: {uri.Scheme}", ValidationStatus.Critical);

            var suspiciousResult = CheckSuspiciousPatterns(url);
            if (suspiciousResult.IsFailure) return suspiciousResult;

            var whitelistResult = CheckWhitelist(uri);
            if (whitelistResult.IsFailure) return whitelistResult;

            var blacklistResult = CheckBlacklist(uri);
            if (blacklistResult.IsFailure) return blacklistResult;

            return await ValidateDnsResolutionAsync(uri);
        }

        private static RsvEditorValidationResult<bool> ValidateDnsResolution(Uri uri)
        {
            try
            {
                var host = uri.Host;
                // WARNING: Task.Wait() can deadlock in Unity Editor context.
                // Use ValidateUrlAsync() instead for Editor operations to avoid blocking.
                var dnsTask = Task.Run(() => Dns.GetHostEntry(host));
                if (!dnsTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    return RsvEditorValidationResult<bool>.Failure("DNS resolution timed out", ValidationStatus.Error);
                }
                var hostEntry = dnsTask.Result;
                foreach (var ip in hostEntry.AddressList)
                {
                    if (IsIpBlocked(ip))
                        return RsvEditorValidationResult<bool>.Failure($"Host resolves to blocked IP: {ip}", ValidationStatus.Critical);
                    if (IsCloudMetadataEndpoint(ip.ToString(), uri.Host))
                        return RsvEditorValidationResult<bool>.Failure($"Cloud metadata endpoint blocked: {ip}", ValidationStatus.Critical);
                }
                return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
            }
            catch (Exception ex)
            {
                return RsvEditorValidationResult<bool>.Failure($"DNS resolution failed: {ex.Message}", ValidationStatus.Error);
            }
        }

        private static async Task<RsvEditorValidationResult<bool>> ValidateDnsResolutionAsync(Uri uri)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                IPHostEntry hostEntry;
                try
                {
                    hostEntry = await Task.Run(() => Dns.GetHostEntry(uri.Host), cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return RsvEditorValidationResult<bool>.Failure("DNS resolution timed out", ValidationStatus.Error);
                }
                foreach (var ip in hostEntry.AddressList)
                {
                    if (IsIpBlocked(ip))
                        return RsvEditorValidationResult<bool>.Failure($"Host resolves to blocked IP: {ip}", ValidationStatus.Critical);
                    if (IsCloudMetadataEndpoint(ip.ToString(), uri.Host))
                        return RsvEditorValidationResult<bool>.Failure($"Cloud metadata endpoint blocked: {ip}", ValidationStatus.Critical);
                }
                return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
            }
            catch (Exception ex)
            {
                return RsvEditorValidationResult<bool>.Failure($"DNS resolution failed: {ex.Message}", ValidationStatus.Error);
            }
        }

        private static bool IsIpBlocked(IPAddress ip)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var ipValue = IpToUint(ip);
                foreach (var (start, end) in BlockedIpRanges)
                {
                    if (ipValue >= start && ipValue <= end)
                        return true;
                }
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return IsIpv6Blocked(ip);
            }
            return false;
        }

        private static bool IsIpv6Blocked(IPAddress ip)
        {
            var ipBytes = ip.GetAddressBytes();
            foreach (var cidr in BlockedIpv6CidrRanges)
            {
                if (IsIpv6InRange(ipBytes, cidr))
                    return true;
            }
            return false;
        }

        private static bool IsIpv6InRange(byte[] ipBytes, string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2) return false;

            var networkBytes = IPAddress.Parse(parts[0]).GetAddressBytes();
            var prefixLength = int.Parse(parts[1]);

            int bytesToCheck = prefixLength / 8;
            int bitsToCheck = prefixLength % 8;

            for (int i = 0; i < bytesToCheck; i++)
            {
                if (ipBytes[i] != networkBytes[i]) return false;
            }

            if (bitsToCheck > 0 && bytesToCheck < 16)
            {
                byte mask = (byte)(0xFF << (8 - bitsToCheck));
                if ((ipBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                    return false;
            }

            return true;
        }

        private static bool IsCloudMetadataEndpoint(string ip, string host)
        {
            if (CloudMetadataHosts.Contains(ip)) return true;
            foreach (var metadataHost in CloudMetadataHosts)
                if (host.Equals(metadataHost, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static uint IpToUint(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        private static (uint start, uint end) ParseCidrRange(string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid CIDR: {cidr}");

            var ip = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            if (prefixLength < 0 || prefixLength > 32)
                throw new ArgumentException($"Invalid prefix: {prefixLength}");

            var ipValue = IpToUint(ip);
            var mask = uint.MaxValue << (32 - prefixLength);
            var start = ipValue & mask;
            var end = start | ~mask;
            return (start, end);
        }

        private static RsvEditorValidationResult<bool> CheckSuspiciousPatterns(string url)
        {
            string lowerUrl = url.ToLowerInvariant();
            foreach (var pattern in SuspiciousPatterns)
            {
                if (Regex.IsMatch(lowerUrl, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2)))
                    return RsvEditorValidationResult<bool>.Failure("URL matches suspicious pattern", ValidationStatus.Warning);
            }
            return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
        }

        private static RsvEditorValidationResult<bool> CheckWhitelist(Uri uri)
        {
            var whitelist = RsvConfiguration.UrlWhitelist;
            if (whitelist == null || whitelist.Length == 0)
                return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);

            string host = uri.Host.ToLowerInvariant();
            foreach (var pattern in whitelist)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                string lowerPattern = pattern.ToLowerInvariant();

                if (host == lowerPattern) return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
                if (lowerPattern.StartsWith("*."))
                {
                    string domain = lowerPattern.Substring(2);
                    if (host == domain || host.EndsWith("." + domain))
                        return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
                }
                if (lowerPattern.StartsWith("regex:"))
                {
                    string regexPattern = lowerPattern.Substring(6);
                    if (Regex.IsMatch(host, regexPattern, RegexOptions.None, TimeSpan.FromSeconds(2)))
                        return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
                }
            }
            return RsvEditorValidationResult<bool>.Failure("URL not in whitelist", ValidationStatus.Critical);
        }

        private static RsvEditorValidationResult<bool> CheckBlacklist(Uri uri)
        {
            var blacklist = RsvConfiguration.UrlBlacklist;
            if (blacklist == null || blacklist.Length == 0)
                return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);

            string host = uri.Host.ToLowerInvariant();
            foreach (var pattern in blacklist)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                string lowerPattern = pattern.ToLowerInvariant();

                if (host == lowerPattern)
                    return RsvEditorValidationResult<bool>.Failure("URL is blacklisted", ValidationStatus.Critical);
                if (lowerPattern.StartsWith("*."))
                {
                    string domain = lowerPattern.Substring(2);
                    if (host == domain || host.EndsWith("." + domain))
                        return RsvEditorValidationResult<bool>.Failure("URL matches blacklisted pattern", ValidationStatus.Critical);
                }
                if (lowerPattern.StartsWith("regex:"))
                {
                    string regexPattern = lowerPattern.Substring(6);
                    if (Regex.IsMatch(host, regexPattern, RegexOptions.None, TimeSpan.FromSeconds(2)))
                        return RsvEditorValidationResult<bool>.Failure("URL matches blacklisted regex", ValidationStatus.Critical);
                }
            }
            return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
        }
    }
}