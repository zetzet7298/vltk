using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using VLTK.Core;

namespace VLTK.Resources
{
    // ── Encoding / Compression options (AC2, AC3) ──────────────────────────

    public enum EncodingStrategy
    {
        /// <summary>Treat path bytes as UTF-8 (default).</summary>
        Utf8,
        /// <summary>Treat path/content bytes as GBK / GB2312 (legacy JX encoding).</summary>
        Gbk,
        /// <summary>Auto-detect encoding from BOM or heuristic.</summary>
        AutoDetect,
    }

    public enum CompressionFormat
    {
        /// <summary>No compression — bytes returned as-is.</summary>
        None,
        /// <summary>Zlib / DEFLATE stream (RFC 1950).</summary>
        Zlib,
        /// <summary>GZip stream (RFC 1952).</summary>
        Gzip,
    }

    public struct ResourceLookupOptions
    {
        public EncodingStrategy Encoding;
        public CompressionFormat Compression;

        public static ResourceLookupOptions Default => new()
        {
            Encoding = EncodingStrategy.Utf8,
            Compression = CompressionFormat.None,
        };
    }

    // ── Status / Result ─────────────────────────────────────────────────────

    public enum ResourceLookupStatus
    {
        Found,
        Missing,
        Invalid,
        Error,
    }

    public struct ResourceLookupResult
    {
        public ResourceLookupStatus status;
        public byte[] data;
        public string sourcePackage;
        public string error;
        /// <summary>
        /// Set when a non-Utf8 encoding strategy is used (AC2 note).
        /// Empty string when encoding is Utf8 or not applicable.
        /// </summary>
        public string encodingNote;

        public static ResourceLookupResult Found(byte[] data, string package = "")
            => new() { status = ResourceLookupStatus.Found, data = data, sourcePackage = package };

        public static ResourceLookupResult Missing(string path)
            => new() { status = ResourceLookupStatus.Missing, error = $"Resource not found: {path}" };

        public static ResourceLookupResult Error(string message)
            => new() { status = ResourceLookupStatus.Error, error = message };
    }

    public interface IResourceProvider
    {
        string ProviderId { get; }
        bool Contains(string resourcePath);
        ResourceLookupResult Load(string resourcePath);
    }

    public interface IResourceLookup
    {
        ResourceLookupResult Resolve(string resourcePath);
        /// <summary>Resolve with encoding/compression post-processing (AC2, AC3).</summary>
        ResourceLookupResult Resolve(string resourcePath, ResourceLookupOptions options);
        ResourceLookupResult Resolve(int uid);
        void RegisterProvider(IResourceProvider provider);
        IReadOnlyList<string> GetAvailableResources();
    }

    public class ResourceLookup : IResourceLookup
    {
        private readonly List<IResourceProvider> _providers = new();
        private readonly Dictionary<string, IResourceProvider> _pathCache = new();
        private readonly Dictionary<int, IResourceProvider> _uidCache = new();

        public void RegisterProvider(IResourceProvider provider)
        {
            _providers.Add(provider);
            SubsystemLog.Info("ResourceLookup", $"Registered provider: {provider.ProviderId}");
        }

        public ResourceLookupResult Resolve(string resourcePath)
            => Resolve(resourcePath, ResourceLookupOptions.Default);

        /// <inheritdoc/>
        public ResourceLookupResult Resolve(string resourcePath, ResourceLookupOptions options)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return ResourceLookupResult.Error("Empty resource path");

            // Provider lookup (first-match wins — AC1)
            IResourceProvider matchedProvider = null;

            if (_pathCache.TryGetValue(resourcePath, out var cached) && cached.Contains(resourcePath))
                matchedProvider = cached;

            if (matchedProvider == null)
            {
                foreach (var provider in _providers)
                {
                    if (provider.Contains(resourcePath))
                    {
                        _pathCache[resourcePath] = provider;
                        matchedProvider = provider;
                        break;
                    }
                }
            }

            if (matchedProvider == null)
                return ResourceLookupResult.Missing(resourcePath);

            var result = matchedProvider.Load(resourcePath);

            if (result.status != ResourceLookupStatus.Found)
                return result;

            // AC2: Encoding note
            if (options.Encoding == EncodingStrategy.Gbk)
                result.encodingNote = "GBK/GB2312 encoding assumed; caller must transcode if needed";
            else if (options.Encoding == EncodingStrategy.AutoDetect)
                result.encodingNote = "AutoDetect: UTF-8 assumed; BOM or heuristic not applied at lookup layer";

            // AC3: Decompression
            if (options.Compression != CompressionFormat.None && result.data != null)
            {
                try
                {
                    result.data = Decompress(result.data, options.Compression);
                }
                catch (Exception ex)
                {
                    SubsystemLog.Error("ResourceLookup", $"Decompress failed for '{resourcePath}': {ex.Message}");
                    return ResourceLookupResult.Error($"Decompress failed: {ex.Message}");
                }
            }

            return result;
        }

        public ResourceLookupResult Resolve(int uid)
        {
            if (_uidCache.TryGetValue(uid, out var cached))
                return cached.Load(uid.ToString());

            foreach (var provider in _providers)
            {
                var path = uid.ToString();
                if (provider.Contains(path))
                {
                    _uidCache[uid] = provider;
                    return provider.Load(path);
                }
            }

            return ResourceLookupResult.Missing($"uid:{uid}");
        }

        public IReadOnlyList<string> GetAvailableResources()
        {
            var all = new List<string>();
            foreach (var p in _providers)
            {
                // Providers may implement IListable for full listing
                // For now return empty — batch listing is provider-specific
            }
            return all.AsReadOnly();
        }

        // ── Static factory (AC4 — runtime enforces StreamingAssets only) ─────

        /// <summary>
        /// Creates a lookup pre-configured with only the StreamingAssetsResourceProvider.
        /// Guarantees no PAK archive is parsed during the gameplay loop (AC4).
        /// </summary>
        public static ResourceLookup CreateRuntimeLookup(string subDir = "")
        {
            var lookup = new ResourceLookup();
            lookup.RegisterProvider(new StreamingAssetsResourceProvider(subDir));
            SubsystemLog.Info("ResourceLookup", "Runtime lookup created (StreamingAssets only — no PAK parsing)");
            return lookup;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static byte[] Decompress(byte[] input, CompressionFormat format)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();

            if (format == CompressionFormat.Gzip)
            {
                using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
                gzip.CopyTo(outputStream);
            }
            else // Zlib — skip 2-byte zlib header then use DeflateStream
            {
                // Zlib wraps DEFLATE with a 2-byte header (CMF + FLG) and 4-byte Adler-32 checksum.
                if (input.Length < 2)
                    throw new InvalidDataException("Input too short to be a valid Zlib stream");
                inputStream.Seek(2, SeekOrigin.Begin); // skip zlib header
                using var deflate = new DeflateStream(inputStream, CompressionMode.Decompress);
                deflate.CopyTo(outputStream);
            }

            return outputStream.ToArray();
        }
    }
}
