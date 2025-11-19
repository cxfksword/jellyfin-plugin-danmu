using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Core;

/// <summary>
/// Simple thread-safe cache that keeps data in-memory and persists to disk with a delayed flush.
/// </summary>
/// <typeparam name="TValue">Type of cached values.</typeparam>
public sealed class FileCache<TValue> : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _defaultTtl;
    private readonly TimeSpan _saveDelay;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly object _flushLock = new();
    private readonly string _filePath;
    private Timer? _flushTimer;
    private bool _disposed;
    private readonly ILogger<FileCache<TValue>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCache{TValue}"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides plugin specific storage locations.</param>
    /// <param name="defaultTtl">Default time-to-live for entries when not specified.</param>
    /// <param name="saveDelay">Delay before pending changes are flushed to disk.</param>
    public FileCache(IApplicationPaths applicationPaths, ILoggerFactory loggerFactory, TimeSpan? defaultTtl = null, TimeSpan? saveDelay = null)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        _logger = loggerFactory.CreateLogger<FileCache<TValue>>();
        _defaultTtl = defaultTtl ?? TimeSpan.FromHours(12);
        _saveDelay = saveDelay ?? TimeSpan.FromSeconds(10);
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
        };
        _filePath = Path.Join(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.Danmu.Cache.dat");
        LoadFromDisk();
    }

    /// <summary>
    /// Attempts to retrieve a cached value.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Resolved value when found.</param>
    /// <returns>True when the value exists and did not expire.</returns>
    public bool TryGetValue(string key, out TValue value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        value = default!;
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry.IsExpired)
        {
            _entries.TryRemove(key, out _);
            ScheduleFlush();
            return false;
        }

        value = entry.Value;
        return true;
    }

    /// <summary>
    /// Adds or updates a cache item.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="ttl">Optional per-entry expiration override.</param>
    public void Set(string key, TValue value, TimeSpan? ttl = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        var expiration = DateTimeOffset.UtcNow.Add(ttl ?? _defaultTtl);
        _entries[key] = new CacheEntry(value, expiration);
        ScheduleFlush();
    }

    /// <summary>
    /// Removes a cached entry.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>True when an entry was removed.</returns>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        var removed = _entries.TryRemove(key, out _);
        if (removed)
        {
            ScheduleFlush();
        }

        return removed;
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        ScheduleFlush();
    }

    /// <summary>
    /// Gets the number of cached entries currently in memory.
    /// </summary>
    public int Count => _entries.Count;

    private void LoadFromDisk()
    {
        var path = _filePath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            using var stream = File.OpenRead(path);
            var cache = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(stream, _serializerOptions);
            if (cache == null)
            {
                return;
            }

            foreach (var pair in cache)
            {
                if (pair.Value != null && !pair.Value.IsExpired)
                {
                    _entries.TryAdd(pair.Key, pair.Value);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "Failed to load cache from disk");
            // Ignore corrupted cache file to keep plugin functional.
            _ = ex;
        }
    }

    private void PersistToDisk()
    {
        if (_disposed || string.IsNullOrEmpty(_filePath))
        {
            return;
        }

        RemoveExpiredEntries();

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var snapshot = _entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, snapshot, _serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist cache to disk");
            // Ignore IO issues; data remains in memory and will retry on next flush.
            _ = ex;
        }
    }

    private void RemoveExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in _entries)
        {
            if (pair.Value.Expiration <= now)
            {
                _entries.TryRemove(pair.Key, out _);
            }
        }
    }

    private void ScheduleFlush()
    {
        if (_disposed || string.IsNullOrEmpty(_filePath))
        {
            return;
        }

        lock (_flushLock)
        {
            _flushTimer ??= new Timer(OnFlushTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _flushTimer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Flushes pending entries to disk and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_flushLock)
        {
            _flushTimer?.Dispose();
            _flushTimer = null;
        }

        PersistToDisk();
    }

    private void OnFlushTimer(object? state)
    {
        PersistToDisk();
    }

    private sealed class CacheEntry
    {
        public CacheEntry(TValue value, DateTimeOffset expiration)
        {
            Value = value;
            Expiration = expiration;
        }

        public TValue Value { get; }

        public DateTimeOffset Expiration { get; }

        [JsonIgnore]
        public bool IsExpired => DateTimeOffset.UtcNow >= Expiration;
    }
}
