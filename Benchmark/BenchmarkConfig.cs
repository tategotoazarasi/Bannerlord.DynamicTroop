using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;

namespace Bannerlord.DynamicTroop.Benchmark {
	public class BenchmarkConfig : ManualConfig {
		public BenchmarkConfig() {
			AddLogger(ConsoleLogger.Default);
			AddExporter(CsvExporter.Default);
			AddDiagnoser(MemoryDiagnoser.Default);
			WithOptions(ConfigOptions.DisableOptimizationsValidator);
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
				var assemblyName = new AssemblyName(args.Name).Name;
				var assemblyPath1 = Path.Combine(@"C:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client", $"{assemblyName}.dll");
				if (File.Exists(assemblyPath1))
				{
					return Assembly.LoadFrom(assemblyPath1);
				}
				var assemblyPath2 = Path.Combine(@"C:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\Bannerlord.DynamicTroop\bin\Win64_Shipping_Client", $"{assemblyName}.dll");
				if (File.Exists(assemblyPath2))
				{
					return Assembly.LoadFrom(assemblyPath2);
				}
				return null;
			};
		}
	}
}
