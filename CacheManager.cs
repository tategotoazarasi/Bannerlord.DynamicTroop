using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;

namespace DTES2;

/// <summary>
///     提供了一个通用的缓存机制，可以自动生成基于调用方法全名的缓存键。
/// </summary>
public static class CacheManager {
	private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

	private static readonly MemoryCacheEntryOptions _cacheEntryOptions =
		new() { SlidingExpiration = TimeSpan.FromMinutes(5) };

	/// <summary>
	///     获取或添加缓存项。如果缓存中已存在具有相同键的项，则返回该项；否则，使用指定的工厂函数创建新项，将其添加到缓存中，并返回该项。
	/// </summary>
	/// <typeparam name="T"> 缓存项的类型。 </typeparam>
	/// <param name="valueFactory">     用于创建新缓存项的工厂函数。 </param>
	/// <param name="keySuffix">        可选的键后缀，用于区分具有相同方法签名的不同缓存项。 </param>
	/// <param name="callerFilePath">   调用方法的源文件路径（由编译器自动填充）。 </param>
	/// <param name="callerLineNumber"> 调用方法在源文件中的行号（由编译器自动填充）。 </param>
	/// <param name="callerMemberName"> 调用方法的名称（由编译器自动填充）。 </param>
	/// <returns> 缓存项。 </returns>
	public static T GetOrAdd<T>(
		Func<T>                   valueFactory,
		string?                   keySuffix        = null,
		[CallerFilePath]   string callerFilePath   = "",
		[CallerLineNumber] int    callerLineNumber = 0,
		[CallerMemberName] string callerMemberName = ""
	) {
		string cacheKey = GenerateCacheKey(
			callerFilePath,
			callerLineNumber,
			callerMemberName,
			keySuffix
		);

		if (_cache.TryGetValue(cacheKey, out object? cachedValue)) {
			return (T)cachedValue!;
		}

		T value = valueFactory();
		_ = _cache.Set(cacheKey, value, _cacheEntryOptions);
		return value;
	}

	/// <summary>
	///     根据调用方法的信息生成缓存键。
	/// </summary>
	/// <param name="callerFilePath">   调用方法的源文件路径。 </param>
	/// <param name="callerLineNumber"> 调用方法在源文件中的行号。 </param>
	/// <param name="callerMemberName"> 调用方法的名称。 </param>
	/// <param name="keySuffix">        可选的键后缀。 </param>
	/// <returns> 生成的缓存键。 </returns>
	private static string GenerateCacheKey(
		string  callerFilePath,
		int     callerLineNumber,
		string  callerMemberName,
		string? keySuffix
	) {
		// 使用调用方法的文件路径、行号和方法名生成唯一的缓存键。 可以根据需要自定义键的生成逻辑。
		string key = $"{callerFilePath}:{callerLineNumber}:{callerMemberName}";
		if (!string.IsNullOrEmpty(keySuffix)) {
			key += $":{keySuffix}";
		}

		return key;
	}
}