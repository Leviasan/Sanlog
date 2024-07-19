namespace Leviasan.Sanlog
{
    /// <summary>
    /// Specifies the behavior to use when writing to a log file that is already full.
    /// </summary>
    public enum FileLoggerWriterMode
    {
        /// <summary>
        /// Drops the item being written.
        /// </summary>
        DropWrite = 0,
        /// <summary>
        /// Removes the newest log file to make room for the item being written.
        /// </summary>
        DropNewest = 1,
        /// <summary>
        /// Removes the oldest log file to make room for the item being written.
        /// </summary>
        DropOldest = 2
    }
}