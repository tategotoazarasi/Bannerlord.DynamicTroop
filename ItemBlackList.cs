using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

/// <summary>
///     Provides functionality to test if an item should be blacklisted.
/// </summary>
public static class ItemBlackList {
	/// <summary>
	///     Represents a collection of string IDs used for blacklisting items.
	/// </summary>
	/// <remarks>
	///     This set is populated by reading and parsing a blacklist file.
	/// </remarks>
	private static readonly HashSet<string> StringIds = new();

	/// <summary>
	///     Represents a collection of string names used for blacklisting items.
	/// </summary>
	/// <remarks>
	///     This set is populated by reading and parsing a blacklist file.
	/// </remarks>
	private static readonly HashSet<string> Names = new();

	/// <summary>
	///     The set of string ID patterns used for blacklisting items.
	/// </summary>
	/// <remarks>
	///     This set is populated by reading and parsing a blacklist file.
	/// </remarks>
	private static readonly HashSet<string> StringIdPatterns = new();

	/// <summary>
	///     Contains the patterns used for filtering item names.
	/// </summary>
	/// <remarks>
	///     This set is populated by reading and parsing a blacklist file.
	/// </remarks>
	private static readonly HashSet<string> NamePatterns = new();

	/// <summary>
	///     Initializes the ItemBlackList.
	/// </summary>
	static ItemBlackList() {
		Global.Debug("initializing black list");
		var modDirectory  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var blackListFile = Path.Combine(modDirectory, "../../blacklist.json");
		EnsureBlackListExists(blackListFile, modDirectory);
		LoadBlackList(blackListFile);
	}

	/// <summary>
	///     Ensures that the black list exists by checking if the black list file exists on the given file path.
	///     If the file does not exist, it will be created using a provided example file.
	/// </summary>
	/// <param name="filePath">The file path of the black list file.</param>
	/// <param name="modDirectory">The directory where the mod is located.</param>
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

	/// <summary>
	///     Loads the blacklist from a JSON file and populates the internal blacklists.
	/// </summary>
	/// <param name="filePath">The path to the JSON file containing the blacklist.</param>
	private static void LoadBlackList(string filePath) {
		var content = File.ReadAllText(filePath);
		Global.Debug($"read black list file: {content}");
		var blackList = JsonConvert.DeserializeObject<BlackList>(content);

		if (blackList != null) {
			StringIds.UnionWith(blackList.string_id              ?? Enumerable.Empty<string>());
			Names.UnionWith(blackList.name                       ?? Enumerable.Empty<string>());
			StringIdPatterns.UnionWith(blackList.string_id_regex ?? Enumerable.Empty<string>());
			NamePatterns.UnionWith(blackList.name_regex          ?? Enumerable.Empty<string>());

			foreach (var id in StringIds) { Global.Debug($"string id {id} added to blacklist"); }

			foreach (var name in Names) { Global.Debug($"name {name} added to blacklist"); }

			foreach (var pattern in StringIdPatterns) { Global.Debug($"string id regex pattern {pattern} added to blacklist"); }

			foreach (var pattern in NamePatterns) { Global.Debug($"name regex pattern {pattern} added to blacklist"); }
		}
	}

	/// <summary>
	///     Determines if an item passes the blacklisting conditions.
	/// </summary>
	/// <param name="item">The item to test.</param>
	/// <returns>True if the item passes the blacklisting conditions, false otherwise.</returns>
	public static bool Test(ItemObject item) {
		if (item is not { StringId: not null, Name: not null } || item.StringId.IsEmpty() || item.Name.ToString().IsEmpty()) return true;
		var stringId = item.StringId;
		var name     = item.Name.ToString();

		return !StringIds.Contains(stringId) && !Names.Contains(name) && !StringIdPatterns.Any(pattern => Regex.IsMatch(stringId, pattern)) && !NamePatterns.Any(pattern => Regex.IsMatch(name, pattern));
	}

	/// <summary>
	///     Utility class for managing item blacklist for dynamic troops.
	/// </summary>
	private sealed class BlackList {
		/// <summary>
		///     Represents a property or attribute name used in the ItemBlackList class.
		/// </summary>
		public List<string>? name;

		/// <summary>
		///     Represents a regular expression used to match the name of an item for blacklisting purposes.
		/// </summary>
		public List<string>? name_regex;

		/// <summary>
		///     Represents a utility class for managing an item blacklist.
		/// </summary>
		public List<string>? string_id;

		/// <summary>
		///     Represents the regular expression patterns used for blacklisting string IDs.
		/// </summary>
		public List<string>? string_id_regex;
	}
}