using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace Bannerlord.DynamicTroop;

public static class ItemBlackList {
	private static readonly HashSet<string> stringIds = new();

	private static readonly HashSet<string> patterns = new();

	static ItemBlackList() {
		Global.Debug("initializing black list");
		var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var blackListFile = Path.Combine(modDirectory, "../../blacklist.json");
		EnsureBlackListExists(blackListFile, modDirectory);
		LoadBlackList(blackListFile);
	}

	private static void EnsureBlackListExists(string filePath, string modDirectory) {
		if (!File.Exists(filePath)) {
			Global.Warn("blacklist.json not found");
			var exampleFile = Path.Combine(modDirectory, "../../blacklist-example.json");
			if (File.Exists(exampleFile))
				File.Copy(exampleFile, filePath);
			else
				Global.Error("blacklist-example.json not found");
		}
	}

	private static void LoadBlackList(string filePath) {
		var content = File.ReadAllText(filePath);
		Global.Debug($"read black list file:{content}");
		var blackList = JsonConvert.DeserializeObject<BlackList>(content);

		if (blackList != null) {
			if (blackList.string_id != null)
				foreach (var id in blackList.string_id) {
					_ = stringIds.Add(id);
					Global.Debug($"string id {id} added to blacklist");
				}

			if (blackList.regex != null)
				foreach (var pattern in blackList.regex) {
					_ = patterns.Add(pattern);
					Global.Debug($"regex pattern {pattern} added to blacklist");
				}
		}
	}

	public static bool Test(string str) {
		return !stringIds.Contains(str) && !patterns.Any(pattern => Regex.IsMatch(str, pattern));
	}

	private sealed class BlackList {
		public List<string>? regex { get; }

		public List<string>? string_id { get; }
	}
}