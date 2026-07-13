using System.Collections.Concurrent;
using TaleWorlds.Library;

public static class MessageDisplayService {

	private static readonly ConcurrentQueue<InformationMessage> MessageQueue = new();
	private static float _timeSinceLastMessage;

	public static void EnqueueMessage(InformationMessage message) {
		MessageQueue.Enqueue(message);
	}

	public static void Tick(float dt) {
		_timeSinceLastMessage += dt;
		if (_timeSinceLastMessage < 0.05f)
			return;

		_timeSinceLastMessage = 0f;
		if (MessageQueue.TryDequeue(out var message))
			InformationManager.DisplayMessage(message);
	}

	public static void StopService() {
		while (MessageQueue.TryDequeue(out _)) { }
		_timeSinceLastMessage = 0f;
	}
}