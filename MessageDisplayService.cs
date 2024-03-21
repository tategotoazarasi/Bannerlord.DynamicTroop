#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Library;

#endregion

/// <summary>
///     消息显示服务，用于在游戏中显示消息。
/// </summary>
public static class MessageDisplayService {
	private static readonly ConcurrentQueue<InformationMessage> MessageQueue = new();
	private static readonly AutoResetEvent MessageAvailable = new(false);
	private static readonly CancellationTokenSource CancellationTokenSource = new();

	/// <summary>
	///     静态构造函数，初始化并启动消息处理任务。
	/// </summary>
	static MessageDisplayService() {
		Task.Factory.StartNew(ProcessMessages,
							  CancellationTokenSource.Token,
							  TaskCreationOptions.LongRunning,
							  TaskScheduler.Default);
	}

	/// <summary>
	///     将消息加入队列，并通知消息处理任务。
	/// </summary>
	/// <param name="message">要显示的消息。</param>
	public static void EnqueueMessage(InformationMessage message) {
		MessageQueue.Enqueue(message);
		MessageAvailable.Set(); // 通知有新消息
	}

	/// <summary>
	///     消息处理任务的主函数，循环检查消息队列并显示消息。
	/// </summary>
	private static void ProcessMessages() {
		while (!CancellationTokenSource.IsCancellationRequested) {
			MessageAvailable.WaitOne(); // 等待新消息通知
			while (MessageQueue.TryDequeue(out var message)) {
				try {
					// 显示消息
					InformationManager.DisplayMessage(message);
				}
				catch (Exception) {
					// 异常处理，可以根据需要记录日志或忽略
				}
			}
		}
	}

	/// <summary>
	///     停止消息处理任务。
	/// </summary>
	public static void StopService() {
		CancellationTokenSource.Cancel();
		MessageAvailable.Set(); // 确保如果任务正在等待，则能够退出等待状态
	}
}