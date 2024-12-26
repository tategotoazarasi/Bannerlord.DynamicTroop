using System;
using System.Collections.Generic;

namespace DTES2.Algorithm;

public static class QuickSelect {
	/// <summary>
	///     使用 QuickSelect 算法查找数组中按指定比较器比较的 Top-K 元素。
	/// </summary>
	/// <typeparam name="T"> 数组元素的类型。 </typeparam>
	/// <param name="array">    输入数组。 </param>
	/// <param name="k">        要选择的 Top-K 元素的数量。 </param>
	/// <param name="comparer"> 用于比较元素的比较器。 </param>
	/// <returns> 按指定比较器比较的 Top-K 元素列表。 </returns>
	/// <exception cref="ArgumentNullException"> 如果 array 或 comparer 为 null，则抛出此异常。 </exception>
	/// <exception cref="ArgumentOutOfRangeException"> 如果 k 小于 0，则抛出此异常。 </exception>
	public static List<T> Select<T>(T[] array, int k, IComparer<T> comparer) {
		// 参数校验
		if (array == null) {
			throw new ArgumentNullException(nameof(array), "Array cannot be null.");
		}

		if (comparer == null) {
			throw new ArgumentNullException(nameof(comparer), "Comparer cannot be null.");
		}

		if (k < 0) {
			throw new ArgumentOutOfRangeException(nameof(k), "k cannot be negative.");
		}

		// 如果 k 大于或等于数组长度，则返回整个数组
		if (k >= array.Length) {
			return new List<T>(array);
		}

		// 复制数组以避免修改原始数组
		T[] arrCopy = (T[])array.Clone();

		// 使用 QuickSelect 算法查找第 k 大的元素
		int kthLargestIndex = FindKthLargestIndex(
			arrCopy,
			0,
			arrCopy.Length - 1,
			k,
			comparer
		);

		// 提取 Top-K 元素
		List<T> topK = [];
		for (int i = kthLargestIndex; i < arrCopy.Length; ++i) {
			topK.Add(arrCopy[i]);
		}

		return topK;
	}

	/// <summary>
	///     使用 QuickSelect 算法的递归部分，查找第 k 大的元素的索引。
	/// </summary>
	/// <typeparam name="T"> 数组元素的类型。 </typeparam>
	/// <param name="arr">      输入数组。 </param>
	/// <param name="left">     当前子数组的左边界。 </param>
	/// <param name="right">    当前子数组的右边界。 </param>
	/// <param name="k">        要查找的第 k 大的元素。 </param>
	/// <param name="comparer"> 用于比较元素的比较器。 </param>
	/// <returns> 第 k 大的元素的索引。 </returns>
	private static int FindKthLargestIndex<T>(
		T[]          arr,
		int          left,
		int          right,
		int          k,
		IComparer<T> comparer
	) {
		if (left == right) {
			return left;
		}

		// 选择一个随机的枢轴元素
		Random random     = new();
		int    pivotIndex = random.Next(left, right + 1);

		// 将数组围绕枢轴元素进行分区
		pivotIndex = Partition(
			arr,
			left,
			right,
			pivotIndex,
			comparer
		);

		// 计算第 k 大的元素在分区后的位置
		int kthLargestIndexInPartition = right - k + 1;

		// 根据分区结果递归查找
		return kthLargestIndexInPartition == pivotIndex ? pivotIndex :
			   kthLargestIndexInPartition < pivotIndex  ? FindKthLargestIndex(
															  arr,
															  left,
															  pivotIndex - 1,
															  k          - (right - pivotIndex + 1),
															  comparer
														  ) : FindKthLargestIndex(
															  arr,
															  pivotIndex + 1,
															  right,
															  k,
															  comparer
														  );
	}

	/// <summary>
	///     将数组围绕枢轴元素进行分区。
	/// </summary>
	/// <typeparam name="T"> 数组元素的类型。 </typeparam>
	/// <param name="arr">        输入数组。 </param>
	/// <param name="left">       当前子数组的左边界。 </param>
	/// <param name="right">      当前子数组的右边界。 </param>
	/// <param name="pivotIndex"> 枢轴元素的索引。 </param>
	/// <param name="comparer">   用于比较元素的比较器。 </param>
	/// <returns> 分区后枢轴元素的新索引。 </returns>
	private static int Partition<T>(
		T[]          arr,
		int          left,
		int          right,
		int          pivotIndex,
		IComparer<T> comparer
	) {
		T pivotValue = arr[pivotIndex];
		Swap(arr, pivotIndex, right); // 将枢轴元素移动到末尾
		int storeIndex = left;

		for (int i = left; i < right; i++) {
			if (comparer.Compare(arr[i], pivotValue) < 0) {
				Swap(arr, storeIndex, i);
				storeIndex++;
			}
		}

		Swap(arr, right, storeIndex); // 将枢轴元素移动到其最终位置
		return storeIndex;
	}

	/// <summary>
	///     交换数组中的两个元素。
	/// </summary>
	/// <typeparam name="T"> 数组元素的类型。 </typeparam>
	/// <param name="arr"> 输入数组。 </param>
	/// <param name="i">   第一个元素的索引。 </param>
	/// <param name="j">   第二个元素的索引。 </param>
	private static void Swap<T>(T[] arr, int i, int j) => (arr[i], arr[j]) = (arr[j], arr[i]);
}