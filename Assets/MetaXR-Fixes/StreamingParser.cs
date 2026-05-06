// Shim around Meta XR SDK bug: Library/PackageCache/com.meta.xr.sdk.core@.../Scripts/BuildingBlocks/AIBlocks/Transport/StreamingParser.cs
// ships with a placeholder GUID (a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6) and Unity drops the file from compilation,
// so HttpTransport/OpenAIProvider/HuggingFaceProvider/OllamaProvider/LlamaApiProvider fail with CS0103 on StreamingParser.
// The asmref next to this file injects this copy into Meta.XR.BuildingBlocks.AIBlocks so the consumers find the symbol.
// If Meta ships a fix or a clean reimport resolves the original, delete the entire Assets/MetaXR-Fixes folder.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.AIBlocks
{
    public static class StreamingParser
    {
        private static readonly object _sseLock = new object();
        private static readonly object _jsonLock = new object();
        private static readonly Dictionary<int, string> SseBuffers = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> JsonBuffers = new Dictionary<int, string>();

        public static List<string> ParseSse(string chunk, Func<string, string> extractor, int streamId = 0, bool isFinalChunk = false)
        {
            lock (_sseLock)
            {
                var tokens = new List<string>();
                if (string.IsNullOrEmpty(chunk) && !isFinalChunk) return tokens;

                if (!SseBuffers.TryGetValue(streamId, out var buffer))
                {
                    buffer = string.Empty;
                }

                buffer += chunk ?? string.Empty;

                var lines = buffer.Split(new[] { '\n' }, StringSplitOptions.None);
                var completeLineCount = isFinalChunk ? lines.Length : lines.Length - 1;

                for (var i = 0; i < completeLineCount; i++)
                {
                    var line = lines[i].TrimEnd('\r');
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("event:"))
                    {
                        continue;
                    }

                    if (!trimmedLine.StartsWith("data: ")) continue;
                    var jsonData = trimmedLine.Substring(6).Trim();
                    if (jsonData == "[DONE]") continue;

                    try
                    {
                        var text = extractor(jsonData);
                        if (!string.IsNullOrEmpty(text))
                        {
                            tokens.Add(text);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[StreamingParser] Failed to parse SSE chunk: {ex.Message}. Data: {jsonData}");
                    }
                }

                if (isFinalChunk)
                {
                    SseBuffers.Remove(streamId);
                }
                else
                {
                    SseBuffers[streamId] = completeLineCount < lines.Length ? lines[lines.Length - 1] : string.Empty;
                }

                return tokens;
            }
        }

        public static List<string> ParseNewlineJson(string chunk, Func<string, string> extractor, int streamId = 0, bool isFinalChunk = false)
        {
            lock (_jsonLock)
            {
                var tokens = new List<string>();
                if (string.IsNullOrEmpty(chunk) && !isFinalChunk) return tokens;

                if (!JsonBuffers.TryGetValue(streamId, out var buffer))
                {
                    buffer = string.Empty;
                }

                buffer += chunk ?? string.Empty;

                var lines = buffer.Split(new[] { '\n' }, StringSplitOptions.None);
                var completeLineCount = isFinalChunk ? lines.Length : lines.Length - 1;

                for (var i = 0; i < completeLineCount; i++)
                {
                    var line = lines[i].TrimEnd('\r').Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var text = extractor(line);
                        if (!string.IsNullOrEmpty(text))
                        {
                            tokens.Add(text);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[StreamingParser] Failed to parse newline-delimited JSON: {ex.Message}. Line: {line}");
                    }
                }

                if (isFinalChunk)
                {
                    JsonBuffers.Remove(streamId);
                }
                else
                {
                    JsonBuffers[streamId] = completeLineCount < lines.Length ? lines[lines.Length - 1] : string.Empty;
                }

                return tokens;
            }
        }

        public static void ClearBuffers(int streamId = 0)
        {
            lock (_sseLock)
            {
                SseBuffers.Remove(streamId);
            }
            lock (_jsonLock)
            {
                JsonBuffers.Remove(streamId);
            }
        }

        public static T ParseJson<T>(string json) where T : class
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StreamingParser] Failed to parse JSON: {ex.Message}");
                return null;
            }
        }

        public static bool TryParseJson<T>(string json, out T result) where T : class
        {
            result = null;
            try
            {
                result = JsonUtility.FromJson<T>(json);
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StreamingParser] Failed to parse JSON: {ex.Message}");
                return false;
            }
        }
    }
}
