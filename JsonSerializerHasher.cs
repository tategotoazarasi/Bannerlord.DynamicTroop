using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public static class JsonSerializerHasher {
	public static string Serialize(WeaponComponentData obj) {
		// 默认序列化设置，处理循环引用，忽略错误
		JsonSerializerSettings settings = new() {
													ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // 忽略循环引用
													Error = (sender, args) => {
																args.ErrorContext.Handled = true; // 忽略错误
															}
												};
		var json = JsonConvert.SerializeObject(obj, settings);

		// 计算 SHA512 哈希
		using var sha512     = SHA512.Create();
		var       hashBytes  = sha512.ComputeHash(Encoding.UTF8.GetBytes(json));
		var       base64Hash = Convert.ToBase64String(hashBytes);
		return base64Hash;
	}
}