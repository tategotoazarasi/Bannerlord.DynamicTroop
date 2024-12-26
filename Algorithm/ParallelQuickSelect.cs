#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
#endregion

namespace DTES2.Algorithm;

public class ParallelQuickSelect {
	/// <summary>
	///     并行 QuickSelect 算法，返回数组中按比较器比较的 Top-K 元素。
	/// </summary>
	/// <typeparam name="T">数组元素类型。</typeparam>
	/// <param name="array">输入数组。</param>
	/// <param name="k">要选择的 Top-K 元素数量。</param>
	/// <param name="comparer">比较器。</param>
	/// <returns>Top-K 元素，无特定顺序。</returns>
	public static T[] FindTopK<T>(T[] array, int k, IComparer<T> comparer) {
		if (array == null) {
			throw new ArgumentNullException(nameof(array));
		}

		if (comparer == null) {
			throw new ArgumentNullException(nameof(comparer));
		}

		if (k <= 0) {
			return new T[0];
		}

		if (k >= array.Length) {
			return array;
		}

		// 找到第 K 大的元素
		T kthElement = QuickSelect(array, 0, array.Length - 1, k, comparer);

		// 并行提取 Top-K 元素
		List<T> topK = new List<T>();
		Parallel.ForEach(array,
						 item => {
							 if (comparer.Compare(item, kthElement) >= 0) {
								 lock (topK) {
									 topK.Add(item);
								 }
							 }
						 });

		return topK.ToArray();
	}

	/// <summary>
	///     并行 QuickSelect 算法的核心实现。
	/// </summary>
	private static T QuickSelect<T>(T[] array, int left, int right, int k, IComparer<T> comparer) {
		while (true) {
			if (left == right) {
				return array[left];
			}

			int pivotIndex = Partition(array, left, right, comparer);
			int rank       = pivotIndex - left + 1;

			if (k == rank) {
				return array[pivotIndex];
			}
			if (k < rank) {
				right = pivotIndex - 1;
			}
			else {
				// 并行化：在右侧子数组中查找
				if (right - pivotIndex > 1000) // 设置一个阈值，当子数组长度大于该值时才进行并行化
				{
					Task<T> rightTask = Task.Run(() => QuickSelect(array, pivotIndex + 1, right, k - rank, comparer));
					right = pivotIndex - 1;
					T result = QuickSelect(array, left, right, k, comparer);
					if (rightTask.Wait(TimeSpan.FromMilliseconds(100))) {
						return rightTask.Result;
					}
					return result;
				}
				left =  pivotIndex + 1;
				k    -= rank;
			}
		}
	}

	/// <summary>
	///     分区操作。
	/// </summary>
	private static int Partition<T>(T[] array, int left, int right, IComparer<T> comparer) {
		T   pivot = array[right];
		int i     = left;

		for (int j = left; j < right; j++) {
			if (comparer.Compare(array[j], pivot) >= 0) {
				Swap(array, i, j);
				i++;
			}
		}

		Swap(array, i, right);
		return i;
	}

	/// <summary>
	///     交换数组中两个元素的位置。
	/// </summary>
	private static void Swap<T>(T[] array, int i, int j) => (array[j], array[i]) = (array[i], array[j]);
}
[TestFixture]
public class ParallelQuickSelectTests {
	[Test]
	public void TestFindTopK() {
		// 生成随机数组
		Random random    = new Random();
		int    arraySize = 10000;
		int    k         = 100;
		int[]  array     = Enumerable.Range(0, arraySize).Select(_ => random.Next(1, 100000)).ToArray();

		// 使用并行 QuickSelect 算法获取 Top-K
		int[] topKParallel = ParallelQuickSelect.FindTopK(array.ToArray(), k, Comparer<int>.Default);

		// 使用 LINQ 获取 Top-K
		int[] topKLinq = array.OrderByDescending(x => x).Take(k).ToArray();

		// 验证结果
		ClassicAssert.AreEqual(topKLinq.Length, topKParallel.Length);
		foreach (int item in topKLinq) {
			ClassicAssert.True(topKParallel.Contains(item));
		}
	}
	[Test]
	public void TestFindTopK_KGreaterThanArrayLength() {
		// 生成随机数组
		Random random    = new Random();
		int    arraySize = 100;
		int    k         = 200;
		int[]  array     = Enumerable.Range(0, arraySize).Select(_ => random.Next(1, 1000)).ToArray();

		// 使用并行 QuickSelect 算法获取 Top-K
		int[] topKParallel = ParallelQuickSelect.FindTopK(array.ToArray(), k, Comparer<int>.Default);

		// 使用 LINQ 获取 Top-K
		int[] topKLinq = array.OrderByDescending(x => x).Take(k).ToArray();

		// 验证结果
		ClassicAssert.AreEqual(topKLinq.Length, topKParallel.Length);
		foreach (int item in topKLinq) {
			ClassicAssert.True(topKParallel.Contains(item));
		}
	}
	[Test]
	public void TestFindTopK_KEqualsZero() {
		// 生成随机数组
		Random random    = new Random();
		int    arraySize = 100;
		int    k         = 0;
		int[]  array     = Enumerable.Range(0, arraySize).Select(_ => random.Next(1, 1000)).ToArray();

		// 使用并行 QuickSelect 算法获取 Top-K
		int[] topKParallel = ParallelQuickSelect.FindTopK(array.ToArray(), k, Comparer<int>.Default);

		// 验证结果
		ClassicAssert.AreEqual(0, topKParallel.Length);
	}

	[Test]
	public void TestFindTopK_NullArray_ThrowsArgumentNullException() {
		int k = 10;
		Assert.Throws<ArgumentNullException>(() => ParallelQuickSelect.FindTopK(null, k, Comparer<int>.Default));
	}

	[Test]
	public void TestFindTopK_NullComparer_ThrowsArgumentNullException() {
		int[] array = new[] { 1, 2, 3 };
		int   k     = 2;
		Assert.Throws<ArgumentNullException>(() => ParallelQuickSelect.FindTopK(array, k, null));
	}
}