# RSV Security Best Practices

## Overview

The Runtime Schema Validator (RSV) includes comprehensive security features to protect against common vulnerabilities identified in the security audit. This document outlines security best practices for using RSV in production environments.

## Security Audit Response

This document addresses the critical security issues identified in the [AUDIT_REPORT.md](AUDIT_REPORT.md):

### ✅ Resolved Critical Issues

1. **Remote URL Fetching Without Validation** → Implemented `RsvUrlValidator` and `RsvAsyncHttpFetcher`
2. **Code Injection via Migration Scripts** → Implemented `RsvMigrationScriptValidator`
3. **Path Traversal Vulnerability** → Implemented `RsvPathValidator`
4. **No Input Size Limits** → Implemented streaming with size limits
5. **Regex DoS Vulnerability** → Implemented regex timeout protection

### Security Features Summary

| Feature | Implementation | Protection Level |
|---------|---------------|------------------|
| Path Traversal Protection | `RsvPathValidator` | 🔴 Critical |
| URL Whitelist/Blacklist | `RsvUrlValidator` | 🔴 Critical |
| Streaming Size Limits | `RsvAsyncHttpFetcher` | 🔴 Critical |
| Regex Timeout | `RsvRuntimeValidator` | 🔴 Critical |
| Migration Script Validation | `RsvMigrationScriptValidator` | 🔴 Critical |
| Error Message Sanitization | `RsvErrorSanitizer` | 🟡 High |

---

## Security Features

### 1. Path Traversal Protection

**Feature:** `RsvPathValidator` prevents access to files outside the project directory.

**Protection:**
- Blocks `../` and `..\\` patterns
- Blocks URL-encoded traversal patterns (`%2e%2e%2f`, `%2e%2e%5c`, etc.)
- Restricts file access to project directory
- Validates all file paths before reading
- Sanitizes dangerous characters and control characters
- Enforces allowed file extensions (.json, .jsonc)

**Blocked Patterns:**
- `../` and `..\\` (standard traversal)
- `%2e%2e%2f` and `%2e%2e%5c` (URL-encoded traversal)
- `..%2f` and `..%5c` (mixed encoding)
- `%2e%2e/` and `%2e%2e\\` (partial encoding)
- Null bytes and control characters (0x00-0x1F, 0x7F)

**Allowed Extensions:**
- `.json` - Standard JSON files
- `.jsonc` - JSON with comments

**Best Practices:**
- Always use relative paths within the project
- Avoid absolute paths in bindings
- Use `Assets/`, `StreamingAssets/`, or `Resources/` prefixes
- Use `ValidateAssetsPath()` for Assets folder paths
- Use `ValidateStreamingAssetsPath()` for StreamingAssets paths
- Use `SanitizePath()` to clean user-provided paths
- Use `GetSafeAbsolutePath()` to get validated absolute paths

**Example:**
```csharp
// ✅ Good - Relative path within project
binding.SourcePathOrUrl = "Assets/Data/items.json";

// ❌ Bad - Absolute path outside project
binding.SourcePathOrUrl = "C:/Users/John/Desktop/data.json";

// ❌ Bad - Path traversal attempt
binding.SourcePathOrUrl = "../../../etc/passwd";
```

---

### 2. URL Whitelist/Blacklist

**Feature:** `RsvUrlValidator` validates URLs against whitelist and blacklist patterns.

**Protection:**
- Enforces HTTPS-only URLs (HTTP is blocked)
- Blocks internal network ranges (SSRF protection)
- Blocks cloud metadata endpoints
- Supports custom whitelist/blacklist patterns
- Supports wildcard patterns (*.example.com)
- Supports regex patterns (regex:.*\\.trusted-domain\\.com)
- Blocks suspicious URL patterns (file://, ftp://, custom protocols)
- Blocks IP addresses in URLs (suspicious for public APIs)
- Blocks port scanning patterns (ports > 65535)

**Blocked Network Ranges:**
- IPv4 private: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`
- IPv4 loopback: `127.0.0.0/8`, `localhost`
- IPv4 link-local: `169.254.0.0/16`
- Cloud metadata: `169.254.169.254`, `metadata.google.internal`
- IPv6 loopback: `[::1]`, `[0:0:0:0:0:0:0:1]`

**Blocked Patterns:**
- `file://` - File protocol
- `ftp://` - FTP protocol
- Custom protocols (any non-http/https scheme)
- Port scanning (ports > 65535)
- IP addresses in URLs (e.g., `https://192.168.1.1/data`)

**Best Practices:**
- Configure URL whitelist for production
- Block internal network ranges
- Use HTTPS for all remote URLs
- Avoid IP addresses in URLs
- Use wildcard patterns for subdomains
- Use regex patterns for complex matching
- Regularly review and update whitelist/blacklist

**Configuration:**
```csharp
// Configure whitelist
RsvConfiguration.UrlWhitelist = new[]
{
    "api.example.com",
    "*.example.com",
    "regex:.*\\.trusted-domain\\.com"
};

// Configure blacklist
RsvConfiguration.UrlBlacklist = new[]
{
    "localhost",
    "*.internal",
    "169.254.169.254"  // AWS metadata endpoint
};

// Add to whitelist programmatically
RsvUrlValidator.AddToWhitelist("new-api.example.com");

// Remove from whitelist
RsvUrlValidator.RemoveFromWhitelist("old-api.example.com");

// Get current whitelist/blacklist
var whitelist = RsvUrlValidator.GetWhitelist();
var blacklist = RsvUrlValidator.GetBlacklist();
```

---

### 3. File Size Limits & Streaming

**Feature:** Configurable size limits prevent DoS attacks via large files. Streaming support for large responses.

**Protection:**
- Limits local file size (default: 100MB)
- Limits remote response size (default: 10MB)
- Streaming for large files (>10MB)
- Size enforcement during download
- Early rejection of oversized responses
- 8KB buffer for efficient streaming
- Byte estimation during streaming (UTF-8: 1-4 bytes per char)

**Streaming Implementation:**
- Uses `HttpCompletionOption.ResponseHeadersRead` for streaming
- Reads content in 8KB chunks
- Enforces size limit during streaming (not just at the end)
- Supports cancellation tokens for long-running operations
- Exponential backoff retry logic (1s, 2s, 4s, 8s...)

**Best Practices:**
- Set appropriate size limits for your use case
- Use streaming for large datasets
- Monitor file sizes in production
- Use cancellation tokens for long operations
- Implement retry logic for transient failures
- Check Content-Length header before reading

**Configuration:**
```csharp
RsvConfiguration.MaxLocalFileSizeBytes = 100 * 1024 * 1024;  // 100MB
RsvConfiguration.MaxRemoteResponseSizeBytes = 10 * 1024 * 1024;  // 10MB
RsvConfiguration.StreamingThresholdBytes = 10 * 1024 * 1024;  // 10MB
RsvConfiguration.HttpTimeout = TimeSpan.FromSeconds(30);  // 30 seconds
```

**Usage Example:**
```csharp
// Fetch with size limit and cancellation
var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
var content = await RsvAsyncHttpFetcher.FetchAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,  // 10MB limit
    cancellationToken
);

// Fetch with retry logic
var content = await RsvAsyncHttpFetcher.FetchWithRetryAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    maxRetries: 3,
    cancellationToken
);
```

---

### 4. Regex Timeout Protection

**Feature:** Regex patterns have timeout to prevent ReDoS (Regular Expression Denial of Service) attacks.

**Protection:**
- 5-second timeout for regex matching
- Catches catastrophic backtracking
- Prevents CPU exhaustion
- Uses `RegexOptions.Compiled` for performance
- Catches `RegexMatchTimeoutException` and reports as Critical security issue
- Validates regex patterns before use

**Implementation Details:**
```csharp
// Runtime validation with timeout
var regex = new Regex(node.Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
if (!regex.IsMatch(value))
{
    result.AddEntry(RsvValidationStatus.Error, "PatternViolation",
        $"String does not match pattern: {node.Pattern}", path);
}
```

**Best Practices:**
- Avoid complex regex patterns
- Test regex patterns for performance
- Use simple patterns when possible
- Avoid nested quantifiers (e.g., `(a+)+`)
- Avoid overlapping alternations (e.g., `(a|a)+`)
- Use possessive quantifiers when appropriate (e.g., `a++`)

**Configuration:**
```csharp
// The timeout is hardcoded to 5 seconds in RsvRuntimeValidator
// This is a security measure to prevent ReDoS attacks
// Do not increase this timeout without careful consideration
```

**Common ReDoS Patterns to Avoid:**
```csharp
// ❌ Bad - Catastrophic backtracking
"(a+)+"
"(a|a)+"
"(.*a)+"

// ✅ Good - Simple patterns
"a+"
"[a-z]+"
"\\d{3}-\\d{3}-\\d{4}"
```

---

### 5. Migration Script Security

**Feature:** `RsvMigrationScriptValidator` validates migration scripts before execution.

**Protection:**
- Namespace whitelist
- Blocked type detection
- Blocked method detection
- Suspicious pattern detection

**Best Practices:**
- Review migration scripts before use
- Use allowed namespaces only
- Avoid file system operations
- Avoid network operations

**Allowed Namespaces:**
- `LiveGameDev.RSV`
- `LiveGameDev`
- `System`
- `System.Collections`
- `System.Collections.Generic`
- `System.Linq`
- `System.Text`
- `Newtonsoft.Json`
- `Newtonsoft.Json.Linq`

**Blocked Types:**
- `System.IO.File`
- `System.IO.Directory`
- `System.Diagnostics.Process`
- `System.Net.WebClient`
- `System.Net.Http.HttpClient`
- `System.Reflection.Assembly`

**Blocked Methods:**
- `Execute`, `Start`, `Load`, `Save`, `Delete`
- `WriteAllText`, `ReadAllText`, `Open`, `Create`

---

### 6. Async HTTP Fetching with Retry Logic

**Feature:** `RsvAsyncHttpFetcher` provides async HTTP fetching with retry logic and cancellation support.

**Protection:**
- Async/await pattern prevents Editor thread blocking
- Cancellation token support for long-running operations
- Exponential backoff retry logic (1s, 2s, 4s, 8s...)
- Configurable timeout (default: 30 seconds)
- Size limit enforcement during streaming
- HTTPS enforcement
- URL validation before fetching
- Proper error handling and logging

**Retry Logic:**
- Default: 3 retry attempts
- Exponential backoff: 1s → 2s → 4s → 8s
- Retries on transient failures
- Logs retry attempts
- Returns null after all retries fail

**Best Practices:**
- Use async fetching for better Editor responsiveness
- Implement cancellation tokens for long operations
- Set appropriate timeout values
- Use retry logic for transient failures
- Monitor retry attempts for network issues
- Handle null responses appropriately

**Usage Example:**
```csharp
// Basic async fetch
var content = await RsvAsyncHttpFetcher.FetchAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    cancellationToken
);

// Fetch with retry logic
var content = await RsvAsyncHttpFetcher.FetchWithRetryAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    maxRetries: 3,
    cancellationToken
);

// With cancellation token
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var content = await RsvAsyncHttpFetcher.FetchAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    cts.Token
);
```

---

### 7. Error Message Sanitization

**Feature:** `RsvErrorSanitizer` removes sensitive information from error messages.

**Protection:**
- Redacts absolute file paths
- Redacts user-specific paths
- Redacts GUIDs
- Preserves relative project paths for debugging

**Best Practices:**
- Review error logs before sharing
- Use sanitized error messages in production
- Preserve relative paths for debugging

---

## Security Checklist

### Before Production Deployment

- [ ] Configure URL whitelist for allowed domains
- [ ] Configure URL blacklist for blocked domains
- [ ] Set appropriate file size limits
- [ ] Review and test migration scripts
- [ ] Enable HTTPS enforcement
- [ ] Test path traversal protection
- [ ] Test SSRF protection
- [ ] Review error message sanitization

### Ongoing Security

- [ ] Monitor validation logs for suspicious activity
- [ ] Update whitelist/blacklist as needed
- [ ] Review migration scripts regularly
- [ ] Keep dependencies updated
- [ ] Perform security audits periodically

---

## Common Security Pitfalls

### 1. Disabling Security Features

**❌ Don't:**
```csharp
// Never disable security checks
RsvConfiguration.UrlWhitelist = new string[0];  // Allows all URLs
```

**✅ Do:**
```csharp
// Configure proper whitelist
RsvConfiguration.UrlWhitelist = new[] { "api.example.com" };
```

---

### 2. Using Absolute Paths

**❌ Don't:**
```csharp
binding.SourcePathOrUrl = "C:/Users/John/Desktop/data.json";
```

**✅ Do:**
```csharp
binding.SourcePathOrUrl = "Assets/Data/items.json";
```

---

### 3. Ignoring Validation Errors

**❌ Don't:**
```csharp
var result = RsvValidator.Validate(schema, json);
// Ignoring errors
```

**✅ Do:**
```csharp
var result = RsvValidator.Validate(schema, json);
if (result.HasErrors || result.HasCritical)
{
    // Handle errors appropriately
    Debug.LogError($"Validation failed: {result}");
}
```

---

### 4. Using Untrusted Migration Scripts

**❌ Don't:**
```csharp
// Never run untrusted scripts
var script = LoadScriptFromUserInput();
RunMigrationScript(script);
```

**✅ Do:**
```csharp
// Always validate scripts
var validationResult = RsvMigrationScriptValidator.ValidateScript(scriptPath);
if (validationResult.IsFailure)
{
    Debug.LogError($"Script validation failed: {validationResult.ErrorMessage}");
    return;
}
```

---

## Reporting Security Issues

If you discover a security vulnerability in RSV, please report it responsibly:

1. Do not disclose the vulnerability publicly
2. Contact the security team through private channels
3. Provide details about the vulnerability
4. Allow time for a fix to be developed
5. Follow responsible disclosure practices

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [Unity Security Best Practices](https://docs.unity3d.com/Manual/BestPracticeUnderstandingSecurityInUnity.html)

---

**Last Updated:** March 12, 2026
**Version:** 1.0.0
**Security Audit:** All critical issues from AUDIT_REPORT.md have been resolved