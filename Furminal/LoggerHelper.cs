using Microsoft.Extensions.Logging;
using System;

namespace Spakov.Furminal
{
    /// <summary>
    /// Logging-related helper methods.
    /// </summary>
    internal static class LoggerHelper
    {
        /// <summary>
        /// The application logging level to use.
        /// </summary>
        internal static readonly LogLevel s_logLevel = LogLevel.None;

        /// <summary>
        /// Creates a logger for <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>Generates output in <see cref="AppContext.BaseDirectory"/>
        /// in a file named <c>typeof(T).Name</c>.</remarks>
        /// <typeparam name="T">A class to associate with the
        /// logger.</typeparam>
        /// <returns>An <see cref="ILogger"/>, if this is a debug build, or
        /// <see langword="null"/> otherwise.</returns>
        internal static ILogger? CreateLogger<T>()
        {
#if DEBUG
            ILoggerFactory factory = LoggerFactory.Create(
                builder =>
                {
                    builder.AddFile(options =>
                    {
                        options.RootPath = AppContext.BaseDirectory;
                        options.BasePath = "Logs";
                        options.FileAccessMode = Karambolo.Extensions.Logging.File.LogFileAccessMode.KeepOpenAndAutoFlush;
                        options.Files =
                        [
                            new Karambolo.Extensions.Logging.File.LogFileOptions() { Path = $"{typeof(T).Name}.log" }
                        ];
                    });
                    builder.SetMinimumLevel(s_logLevel);
                }
            );

            return factory.CreateLogger<T>();
#else
            return null;
#endif
        }

        /// <summary>
        /// Attempts to invoke <paramref name="func"/>, logging <paramref
        /// name="exceptionLogMessage"/> if it throws any exception.
        /// </summary>
        /// <remarks>Simply invokes <paramref name="func"/> if <paramref
        /// name="logger"/> is <see langword="null"/>.</remarks>
        /// <typeparam name="T"><paramref name="func"/>'s return
        /// type.</typeparam>
        /// <param name="func">A function.</param>
        /// <param name="exceptionLogMessage">The message to log if an
        /// exception is caught.</param>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <returns>A <see cref="ValueTuple{T1, T2}"/> indicating whether an
        /// exception was thrown and <paramref name="func"/>'s return
        /// value.</returns>
        internal static (bool, T?) LogTry<T>(Func<T> func, string exceptionLogMessage, ILogger? logger = null)
        {
            if (logger != null)
            {
                try
                {
                    T? returnValue = func();
                    return (false, returnValue);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "{exceptionLogMessage}", exceptionLogMessage);

                    return (true, default!);
                }
            }
            else
            {
                return (false, func());
            }
        }
    }
}