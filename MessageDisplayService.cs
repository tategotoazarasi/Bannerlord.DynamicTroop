using System;
using System.Collections.Concurrent;
using System.Threading;
using TaleWorlds.Library;

/// <summary>
///     消息显示服务，用于在游戏中显示消息。
/// </summary>
public static class MessageDisplayService {
	/// <summary>
	///     消息队列，线程安全。
	/// </summary>
	private static readonly ConcurrentQueue<InformationMessage> MessageQueue = new();

	/// <summary>
	///     事件通知，用于指示消息队列中是否有新消息。
	/// </summary>
	private static readonly AutoResetEvent MessageAvailable = new(false);

	/// <summary>
	///     显示消息时使用的锁对象。
	/// </summary>
	private static readonly object DisplayMessageLock = new();

	/// <summary>
	///     控制消息处理线程运行状态的标志。
	/// </summary>
	private static bool _running = true;

	/// <summary>
	///     静态构造函数，初始化并启动消息处理线程。
	/// </summary>
	static MessageDisplayService() {
		var displayThread = new Thread(ProcessMessages) { IsBackground = true };
		displayThread.Start();
	}

	/// <summary>
	///     将消息加入队列，并通知消息处理线程。
	/// </summary>
	/// <param name="message"> 要显示的消息。 </param>
	public static void EnqueueMessage(InformationMessage message) {
		MessageQueue.Enqueue(message);
		_ = MessageAvailable.Set(); // 通知有新消息
	}

	/// <summary>
	///     消息处理线程的主函数，循环检查消息队列并显示消息。
	/// </summary>
	private static void ProcessMessages() {
		while (_running) {
			_ = MessageAvailable.WaitOne(); // 等待新消息通知
			while (MessageQueue.TryDequeue(out var message)) {
				try {
					// 锁定并显示消息
					lock (DisplayMessageLock) { InformationManager.DisplayMessage(message); }
				}
				catch (Exception) { /* 异常处理，可以根据需要记录日志或忽略 */
				}
			}
		}
	}

	/// <summary>
	///     停止消息处理线程。
	/// </summary>
	public static void StopService() {
		_running = false;
		_        = MessageAvailable.Set(); // 通知线程退出
	}
}