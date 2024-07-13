using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FileLoggingWriterUnitTest
    {
        private readonly string FilePath = "./";
        private static readonly Action<ILogger, string, string, Exception?> UserLogged = LoggerMessage.Define<string, string>(LogLevel.Information, default, "User {UserName} logged in from {MachineName}.");

        [TestCleanup]
        public void TestCleanup()
        {
            var directory = new DirectoryInfo(FilePath);
            foreach (var file in directory.GetFiles("*.log"))
                file.Delete();
        }
        [TestMethod]
        public async Task FileCountLimitDropWrite()
        {
            string? searchPattern = null;
            await using (var writer = new FileLoggingWriter(FilePath, filePrefix: "DropWrite", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileLoggingFullMode.DropWrite))
            {
                var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, new SanlogLoggerOptions { AppId = Guid.NewGuid() });
                UserLoggedInvoke(logger, null, 3);
                searchPattern = writer.SearchPattern;
            }
            var files = Directory.GetFiles(FilePath, searchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropWrite{DateTime.Now:yyyyMMdd}_0.log", files[0]);
            Assert.AreEqual($"{FilePath}DropWrite{DateTime.Now:yyyyMMdd}_1.log", files[1]);
        }
        /*
        [TestMethod]
        public void FileCountLimitDropNewest()
        {
            var writer = new FileLoggingWriter(directory: FilePath, filePrefix: "DropNewest", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileLoggingFullMode.DropNewest);
            var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, new SanlogLoggerOptions { AppId = Guid.NewGuid() });
            UserLoggedInvoke(logger, null, 5);

            var files = Directory.GetFiles(FilePath, writer.SearchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropNewest{DateTime.Now:yyyyMMdd}_0.log", files[0]);
            Assert.AreEqual($"{FilePath}DropNewest{DateTime.Now:yyyyMMdd}_4.log", files[1]);
        }
        [TestMethod]
        public void FileCountLimitDropOldest()
        {
            var writer = new FileLoggingWriter(directory: FilePath, filePrefix: "DropOldest", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileLoggingFullMode.DropOldest);
            var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, new SanlogLoggerOptions { AppId = Guid.NewGuid() });
            UserLoggedInvoke(logger, null, 5);

            var files = Directory.GetFiles(FilePath, writer.SearchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropOldest{DateTime.Now:yyyyMMdd}_3.log", files[0]);
            Assert.AreEqual($"{FilePath}DropOldest{DateTime.Now:yyyyMMdd}_4.log", files[1]);
        }
        */
        private static void UserLoggedInvoke(ILogger logger, Exception? exception, int count)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
            for (var index = 0; index < count; index++)
                UserLogged.Invoke(logger, Environment.UserName, Environment.MachineName, exception);
        }
    }
}