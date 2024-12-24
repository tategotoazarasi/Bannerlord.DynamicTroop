#region
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using ILogger = Microsoft.Extensions.Logging.ILogger;
#endregion

/// <summary>
///     日志记录器类，实现了`Microsoft.Extensions.Logging.ILogger`和`Serilog.ILogger`接口。 提供日志记录功能，包括文本和JSON格式的日志文件。
/// </summary>
public class Logger : StreamWriter, ILogger, Serilog.ILogger {
	/// <summary>
	///     存储所有命名日志记录器的字典。
	/// </summary>
	private static readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();

	/// <summary>
	///     单例实例。
	/// </summary>
	private static Logger _instance;

	/// <summary>
	///     Serilog的日志记录器实例。
	/// </summary>
	private readonly Serilog.ILogger _logger;

	/// <summary>
	///     初始化`Logger`类的新实例。
	/// </summary>
	/// <param name="filename"> 日志文件名。 </param>
	private Logger(string filename)
		: base(Stream.Null, new UTF8Encoding(false, true), 128, true) {
		_logger = new LoggerConfiguration()
				  .MinimumLevel
				  .Verbose()
				  .Enrich
				  .FromLogContext()
				  .Enrich
				  .With(new CallerInfoEnricher())
				  .WriteTo
				  .File(
					  Path.Combine(Assembly.GetExecutingAssembly().Location, "Log", filename + ".log"),
					  rollingInterval: RollingInterval.Day,
					  outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss:fff}] [{Level:u3}] [{Class}:{Method}] [{File}:{Line}] {Message:l}{NewLine}{Exception}"
				  )
				  .WriteTo
				  .File(
					  new JsonFormatter(",\n"),
					  Path.Combine(Assembly.GetExecutingAssembly().Location, "Log", filename + ".json"),
					  rollingInterval: RollingInterval.Day
				  )
				  .CreateLogger();
	}

	// Implementing StreamWriter interface methods
	public override Encoding Encoding
	{
		get { return Encoding.UTF8; }
	}

	/// <summary>
	///     获取单例实例。
	/// </summary>
	public static Logger Instance
	{
		get
		{
			if (_instance == null)
				return _instance = new Logger("misc");

			return _instance;
		}
	}

	/// <summary>
	///     Begins a logical operation scope.
	/// </summary>
	/// <param name="state"> The identifier for the scope. </param>
	/// <returns> An IDisposable that ends the logical operation scope on dispose. </returns>
	IDisposable ILogger.BeginScope<TState>(TState state) {
		return new DisposableScope();
	}

	/// <summary>
	///     Checks if the given <paramref name="logLevel" /> is enabled.
	/// </summary>
	/// <param name="logLevel"> level to be checked. </param>
	/// <returns> <c> true </c> if enabled. </returns>
	bool ILogger.IsEnabled(LogLevel logLevel) {
		return _logger.IsEnabled(ConvertLogLevel(logLevel));
	}

	/// <summary>
	///     Represents a type used to perform logging.
	/// </summary>
	/// <remarks> Aggregates most logging patterns to a single method. </remarks>
	void ILogger.Log<TState>(
		LogLevel                        logLevel,
		EventId                         eventId,
		TState                          state,
		Exception                       exception,
		Func<TState, Exception, string> formatter
	) {
		if (!((ILogger)this).IsEnabled(logLevel))
			return;

		var logEventLevel = ConvertLogLevel(logLevel);
		var message       = formatter(state, exception);
		var logEvent = new LogEvent(
			DateTimeOffset.Now,
			logEventLevel,
			exception,
			new MessageTemplateParser().Parse(message),
			new List<LogEventProperty>()
		);

		_logger.Write(logEvent);
	}

	/// <summary>
	/// Uses configured scalar conversion and destructuring rules to bind a set of properties to
	/// a message template. Returns false if the template or values are invalid ( <c> ILogger
	/// </c> methods never throw exceptions).
	/// </summary>
	/// <param name="messageTemplate"> Message template describing an event. </param>
	/// <param name="properties">      Objects positionally formatted into the message template. </param>
	/// <param name="parsedTemplate">
	/// The internal representation of the template, which may be used to render the
	/// <paramref name="boundProperties" /> as text.
	/// </param>
	/// <param name="boundProperties"> Captured properties from the template and <paramref name="propertyValues" />. </param>
	/// <example>
	/// <code>
	///MessageTemplate template;
	///IEnumerable&lt;LogEventProperty&gt; properties;
	///if (Log.BindMessageTemplate("Hello, {Name}!", new[] { "World" }, out template, out properties)
	///{
	///var propsByName = properties.ToDictionary(p =&gt; p.Name, p =&gt; p.Value);
	///Console.WriteLine(template.Render(propsByName, null));
	///// -&gt; "Hello, World!"
	/// }
	/// </code>
	/// </example>
	bool Serilog.ILogger.BindMessageTemplate(
		string                            messageTemplate,
		object[]                          properties,
		out MessageTemplate               parsedTemplate,
		out IEnumerable<LogEventProperty> boundProperties
	) {
		return _logger.BindMessageTemplate(
			messageTemplate,
			properties,
			out parsedTemplate,
			out boundProperties
		);
	}

	/// <summary>
	///     Uses configured scalar conversion and destructuring rules to bind a property value to
	///     its captured representation.
	/// </summary>
	/// <param name="propertyName">       The name of the property. Must be non-empty. </param>
	/// <param name="value">              The property value. </param>
	/// <param name="destructureObjects">
	///     If <see langword="true" />, the value will be serialized as a structured object if
	///     possible; if <see langword="false" />, the object will be recorded as a scalar or simple array.
	/// </param>
	/// <param name="property">           The resulting property. </param>
	/// <returns>
	///     True if the property could be bound, otherwise false (
	///     <summary>
	///         ILogger
	///     </summary>
	///     methods never throw exceptions).
	/// </returns>
	bool Serilog.ILogger.BindProperty(
		string               propertyName,
		object               value,
		bool                 destructureObjects,
		out LogEventProperty property
	) {
		return _logger.BindProperty(propertyName, value, destructureObjects, out property);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
	///  </code>
	/// </example>
	public void Debug(string message) {
		_logger.Debug(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
	///  </code>
	/// </example>
	public void Debug(string message, params object[] propertyValues) {
		_logger.Debug(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Debug(ex, "Swallowing a mundane exception.");
	///  </code>
	/// </example>
	public void Debug(Exception exception, string message) {
		_logger.Debug(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug(ex, "Swallowing a mundane exception.");
	///  </code>
	/// </example>
	public void Debug(Exception exception, string message, params object[] propertyValues) {
		_logger.Debug(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
	///  </code>
	/// </example>
	public void Debug<T>(string message, T propertyValue) {
		_logger.Debug(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug(ex, "Swallowing a mundane exception.");
	///  </code>
	/// </example>
	public void Debug<T>(Exception exception, string message, T propertyValue) {
		_logger.Debug(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
	///  </code>
	/// </example>
	public void Debug<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Debug(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug(ex, "Swallowing a mundane exception.");
	///  </code>
	/// </example>
	public void Debug<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Debug(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
	///  </code>
	/// </example>
	public void Debug<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Debug(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Debug(ex, "Swallowing a mundane exception.");
	///  </code>
	/// </example>
	public void Debug<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Debug(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error(string message) {
		_logger.Error(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error(string message, params object[] propertyValues) {
		_logger.Error(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error(Exception exception, string message) {
		_logger.Error(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error(Exception exception, string message, params object[] propertyValues) {
		_logger.Error(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T>(string message, T propertyValue) {
		_logger.Error(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T>(Exception exception, string message, T propertyValue) {
		_logger.Error(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Error(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Error(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Error(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Error<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Error(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Fatal("Process terminating.");
	///  </code>
	/// </example>
	public void Fatal(string message) {
		_logger.Fatal(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal("Process terminating.");
	///  </code>
	/// </example>
	public void Fatal(string message, params object[] propertyValues) {
		_logger.Fatal(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Fatal(ex, "Process terminating.");
	///  </code>
	/// </example>
	public void Fatal(Exception exception, string message) {
		_logger.Fatal(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal(ex, "Process terminating.");
	///  </code>
	/// </example>
	public void Fatal(Exception exception, string message, params object[] propertyValues) {
		_logger.Fatal(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal("Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T>(string message, T propertyValue) {
		_logger.Fatal(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal(ex, "Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T>(Exception exception, string message, T propertyValue) {
		_logger.Fatal(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal("Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Fatal(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal(ex, "Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Fatal(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal("Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Fatal(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Fatal" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Fatal(ex, "Process terminating.");
	///  </code>
	/// </example>
	public void Fatal<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Fatal(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Create a logger that enriches log events via the provided enrichers.
	/// </summary>
	/// <param name="enricher"> Enricher that applies in the context. </param>
	/// <returns> A logger that will enrich log events as specified. </returns>
	Serilog.ILogger Serilog.ILogger.ForContext(ILogEventEnricher enricher) {
		return _logger.ForContext(enricher);
	}

	/// <summary>
	///     Create a logger that enriches log events via the provided enrichers.
	/// </summary>
	/// <param name="enrichers"> Enrichers that apply in the context. </param>
	/// <returns> A logger that will enrich log events as specified. </returns>
	Serilog.ILogger Serilog.ILogger.ForContext(IEnumerable<ILogEventEnricher> enrichers) {
		return _logger.ForContext(enrichers);
	}

	/// <summary>
	///     Create a logger that enriches log events with the specified property.
	/// </summary>
	/// <param name="propertyName">       The name of the property. Must be non-empty. </param>
	/// <param name="value">              The property value. </param>
	/// <param name="destructureObjects">
	///     If <see langword="true" />, the value will be serialized as a structured object if
	///     possible; if <see langword="false" />, the object will be recorded as a scalar or simple array.
	/// </param>
	/// <returns> A logger that will enrich log events as specified. </returns>
	Serilog.ILogger Serilog.ILogger.ForContext(
		string propertyName,
		object value,
		bool   destructureObjects
	) {
		return _logger.ForContext(propertyName, value, destructureObjects);
	}

	/// <summary>
	///     Create a logger that marks log events as being from the specified source type.
	/// </summary>
	/// <param name="source"> Type generating log messages in the context. </param>
	/// <returns> A logger that will enrich log events as specified. </returns>
	Serilog.ILogger Serilog.ILogger.ForContext(Type source) {
		return _logger.ForContext(source);
	}

	/// <summary>
	///     Create a logger that marks log events as being from the specified source type.
	/// </summary>
	/// <typeparam name="TSource"> Type generating log messages in the context. </typeparam>
	/// <returns> A logger that will enrich log events as specified. </returns>
	Serilog.ILogger Serilog.ILogger.ForContext<TSource>() {
		return _logger.ForContext<TSource>();
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information(string message) {
		_logger.Information(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information(string message, params object[] propertyValues) {
		_logger.Information(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information(Exception exception, string message) {
		_logger.Information(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information(Exception exception, string message, params object[] propertyValues) {
		_logger.Information(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T>(string message, T propertyValue) {
		_logger.Information(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T>(Exception exception, string message, T propertyValue) {
		_logger.Information(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Information(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Information(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Information(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Information" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
	///  </code>
	/// </example>
	public void Information<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Information(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Determine if events at the specified level will be passed through to the log sinks.
	/// </summary>
	/// <param name="level"> Level to check. </param>
	/// <returns> <see langword="true" /> if the level is enabled; otherwise, <see langword="false" />. </returns>
	bool Serilog.ILogger.IsEnabled(LogEventLevel level) {
		return _logger.IsEnabled(level);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Verbose("Staring into space, wondering if we're alone.");
	///  </code>
	/// </example>
	public void Verbose(string message) {
		_logger.Verbose(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose("Staring into space, wondering if we're alone.");
	///  </code>
	/// </example>
	public void Verbose(string message, params object[] propertyValues) {
		_logger.Verbose(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
	///  </code>
	/// </example>
	public void Verbose(Exception exception, string message) {
		_logger.Verbose(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
	///  </code>
	/// </example>
	public void Verbose(Exception exception, string message, params object[] propertyValues) {
		_logger.Verbose(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose("Staring into space, wondering if we're alone.");
	///  </code>
	/// </example>
	public void Verbose<T>(string message, T propertyValue) {
		_logger.Verbose(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
	///  </code>
	/// </example>
	public void Verbose<T>(Exception exception, string message, T propertyValue) {
		_logger.Verbose(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose("Staring into space, wondering if we're alone.");
	///  </code>
	/// </example>
	public void Verbose<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Verbose(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
	///  </code>
	/// </example>
	public void Verbose<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Verbose(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose("Staring into space, wondering if we're alone.");
	///  </code>
	/// </example>
	public void Verbose<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Verbose(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Verbose" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
	///  </code>
	/// </example>
	public void Verbose<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Verbose(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level.
	/// </summary>
	/// <param name="message"> Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning(string message) {
		_logger.Warning(message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning(string message, params object[] propertyValues) {
		_logger.Warning(message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	/// <example>
	///     <code>
	/// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning(Exception exception, string message) {
		_logger.Warning(exception, message);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning(Exception exception, string message, params object[] propertyValues) {
		_logger.Warning(exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Error" /> level.
	/// </summary>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T>(string message, T propertyValue) {
		_logger.Warning(message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T>(Exception exception, string message, T propertyValue) {
		_logger.Warning(exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T0, T1, T2>(
		string message,
		T0     propertyValue0,
		T1     propertyValue1,
		T2     propertyValue2
	) {
		_logger.Warning(message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T0, T1, T2>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1,
		T2        propertyValue2
	) {
		_logger.Warning(exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level.
	/// </summary>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) {
		_logger.Warning(message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
	/// </summary>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <example>
	///     <code>
	/// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
	///  </code>
	/// </example>
	public void Warning<T0, T1>(
		Exception exception,
		string    message,
		T0        propertyValue0,
		T1        propertyValue1
	) {
		_logger.Warning(exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write an event to the log.
	/// </summary>
	/// <param name="logEvent"> The event to write. </param>
	void Serilog.ILogger.Write(LogEvent logEvent) {
		_logger.Write(logEvent);
	}

	/// <summary>
	///     Write a log event with the specified level.
	/// </summary>
	/// <param name="level">   The level of the event. </param>
	/// <param name="message"> Message template describing the event. </param>
	void Serilog.ILogger.Write(LogEventLevel level, string message) {
		_logger.Write(level, message);
	}

	/// <summary>
	///     Write a log event with the specified level.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="message">        </param>
	/// <param name="propertyValues"> </param>
	void Serilog.ILogger.Write(LogEventLevel level, string message, params object[] propertyValues) {
		_logger.Write(level, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the specified level and associated exception.
	/// </summary>
	/// <param name="level">     The level of the event. </param>
	/// <param name="exception"> Exception related to the event. </param>
	/// <param name="message">   Message template describing the event. </param>
	void Serilog.ILogger.Write(LogEventLevel level, Exception exception, string message) {
		_logger.Write(level, exception, message);
	}

	/// <summary>
	///     Write a log event with the specified level and associated exception.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValues"> Objects positionally formatted into the message template. </param>
	void Serilog.ILogger.Write(
		LogEventLevel   level,
		Exception       exception,
		string          message,
		params object[] propertyValues
	) {
		_logger.Write(level, exception, message, propertyValues);
	}

	/// <summary>
	///     Write a log event with the specified level.
	/// </summary>
	/// <param name="level">         The level of the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T>(LogEventLevel level, string message, T propertyValue) {
		_logger.Write(level, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the specified level and associated exception.
	/// </summary>
	/// <param name="level">         The level of the event. </param>
	/// <param name="exception">     Exception related to the event. </param>
	/// <param name="message">       Message template describing the event. </param>
	/// <param name="propertyValue"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T>(
		LogEventLevel level,
		Exception     exception,
		string        message,
		T             propertyValue
	) {
		_logger.Write(level, exception, message, propertyValue);
	}

	/// <summary>
	///     Write a log event with the specified level.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T0, T1, T2>(
		LogEventLevel level,
		string        message,
		T0            propertyValue0,
		T1            propertyValue1,
		T2            propertyValue2
	) {
		_logger.Write(level, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the specified level and associated exception.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue2"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T0, T1, T2>(
		LogEventLevel level,
		Exception     exception,
		string        message,
		T0            propertyValue0,
		T1            propertyValue1,
		T2            propertyValue2
	) {
		_logger.Write(level, exception, message, propertyValue0, propertyValue1, propertyValue2);
	}

	/// <summary>
	///     Write a log event with the specified level.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T0, T1>(
		LogEventLevel level,
		string        message,
		T0            propertyValue0,
		T1            propertyValue1
	) {
		_logger.Write(level, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     Write a log event with the specified level and associated exception.
	/// </summary>
	/// <param name="level">          The level of the event. </param>
	/// <param name="exception">      Exception related to the event. </param>
	/// <param name="message">        Message template describing the event. </param>
	/// <param name="propertyValue0"> Object positionally formatted into the message template. </param>
	/// <param name="propertyValue1"> Object positionally formatted into the message template. </param>
	void Serilog.ILogger.Write<T0, T1>(
		LogEventLevel level,
		Exception     exception,
		string        message,
		T0            propertyValue0,
		T1            propertyValue1
	) {
		_logger.Write(level, exception, message, propertyValue0, propertyValue1);
	}

	/// <summary>
	///     根据名称获取日志记录器实例。 如果实例不存在，则创建一个新的实例。
	/// </summary>
	/// <param name="name"> 日志记录器的名称。 </param>
	/// <returns> 指定名称的日志记录器实例。 </returns>
	public static Logger GetLogger(string name) {
		if (!_loggers.ContainsKey(name))
			_loggers.Add(name, new Logger(name));

		return _loggers[name];
	}

	/// <summary>
	///     将`Microsoft.Extensions.Logging.LogLevel`转换为`Serilog.Events.LogEventLevel`。
	/// </summary>
	/// <param name="logLevel"> `Microsoft.Extensions.Logging.LogLevel`级别。 </param>
	/// <returns> 对应的`Serilog.Events.LogEventLevel`级别。 </returns>
	private LogEventLevel ConvertLogLevel(LogLevel logLevel) {
		return logLevel switch {
				   LogLevel.Trace       => LogEventLevel.Verbose,
				   LogLevel.Debug       => LogEventLevel.Debug,
				   LogLevel.Information => LogEventLevel.Information,
				   LogLevel.Warning     => LogEventLevel.Warning,
				   LogLevel.Error       => LogEventLevel.Error,
				   LogLevel.Critical    => LogEventLevel.Fatal,
				   _                    => LogEventLevel.Information
			   };
	}

	public override void WriteLine(string value) {
		Debug(value);
	}

	/**
 * @class CallerInfoEnricher
 * @brief 提供日志事件丰富功能的类，添加调用者信息。
 */
	public class CallerInfoEnricher : ILogEventEnricher {
		private readonly string[] skipWords = {
												  "log",
												  "debug",
												  "postfix",
												  "prefix",
												  "harmony",
												  "error"
											  };

		/**
		 * @brief 丰富日志事件，添加调用者信息。
		 * @param logEvent 要丰富的日志事件。
		 * @param propertyFactory 用于创建日志事件属性的工厂。
		 */
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
			// Start looking from the third frame
			StackFrame frame = null;
			for (var i = 3; i < new StackTrace().FrameCount; i++) {
				var tempFrame = new StackFrame(i, true);
				var method    = tempFrame.GetMethod();

				if (method != null) {
					var className  = method.DeclaringType?.FullName ?? string.Empty;
					var methodName = method.Name;

					var flag = false;

					// Check if the class name or method name contains "log"
					foreach (var skipWord in skipWords) {
						if (
							!className.ToLower().Contains(skipWord) && !methodName.ToLower().Contains(skipWord)
							) {
							frame = tempFrame;
							flag  = true;
							break;
						}
					}

					if (flag)
						break;
				}
			}

			if (frame == null)
				return; // No suitable frame found

			var finalMethod = frame.GetMethod();

			// Create properties for the selected stack frame
			LogEventProperty[] properties = {
												propertyFactory.CreateProperty("Class",  finalMethod.DeclaringType?.FullName ?? ""),
												propertyFactory.CreateProperty("Method", finalMethod.Name),
												propertyFactory.CreateProperty("File",   Path.GetFileName(frame.GetFileName())),
												propertyFactory.CreateProperty("Line",   frame.GetFileLineNumber())
											};

			foreach (var property in properties)
				logEvent.AddPropertyIfAbsent(property); // 添加属性
		}
	}

	/// <summary>
	///     空实现的可释放作用域类。
	/// </summary>
	private class DisposableScope : IDisposable {
		/// <summary>
		///     释放资源。
		/// </summary>
		public void Dispose() { }
	}
}