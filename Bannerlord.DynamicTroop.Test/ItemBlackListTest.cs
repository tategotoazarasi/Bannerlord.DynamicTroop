#region

using System.Text.RegularExpressions;
using Newtonsoft.Json;

#endregion

namespace Bannerlord.DynamicTroop.Test;

public class ItemBlackListTest {
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

	[Fact]
	public void Test1() {
		LoadBlackList(AppDomain.CurrentDomain.BaseDirectory + "../../../../_Module/blacklist-example.json");
		Assert.False(TestStringId("crown"));
		Assert.False(TestStringId("dp_anything"));
		Assert.False(TestName("女士裙"));
		Assert.True(TestStringId("女士裙"));
		Assert.False(TestName("Golden Crown"));
	}

	public static bool TestStringId(string stringId) {
		return !StringIds.Contains(stringId) &&
			   !StringIdPatterns.Any(pattern => Regex.IsMatch(stringId, pattern));
	}

	public static bool TestName(string name) {
		return !Names.Contains(name) &&
			   !NamePatterns.Any(pattern => Regex.IsMatch(name, pattern));
	}

	/// <summary>
	///     Loads the blacklist from a JSON file and populates the internal blacklists.
	/// </summary>
	/// <param name="filePath">The path to the JSON file containing the blacklist.</param>
	private static void LoadBlackList(string filePath) {
		var content = File.ReadAllText(filePath);
		//Global.Debug($"read black list file: {content}");
		var blackList = JsonConvert.DeserializeObject<BlackList>(content);

		if (blackList != null) {
			StringIds.UnionWith(blackList.string_id              ?? Enumerable.Empty<string>());
			Names.UnionWith(blackList.name                       ?? Enumerable.Empty<string>());
			StringIdPatterns.UnionWith(blackList.string_id_regex ?? Enumerable.Empty<string>());
			NamePatterns.UnionWith(blackList.name_regex          ?? Enumerable.Empty<string>());

			//foreach (var id in StringIds) { Global.Debug($"string id {id} added to blacklist"); }

			//foreach (var name in Names) { Global.Debug($"name {name} added to blacklist"); }

			//foreach (var pattern in StringIdPatterns) { Global.Debug($"string id regex pattern {pattern} added to blacklist"); }

			//foreach (var pattern in NamePatterns) { Global.Debug($"name regex pattern {pattern} added to blacklist"); }
		}
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