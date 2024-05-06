namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides a mechanism for writing log entries to the storage.
    /// </summary>
    public interface ILoggingWriter
    {
        /// <summary>
        /// Writes a log entry to the storage.
        /// </summary>
        /// <param name="item">The value to write to the storage.</param>
        void Write(LoggingEntry item);
    }
}