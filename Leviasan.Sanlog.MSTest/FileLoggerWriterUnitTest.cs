using System.Text;
using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog.MSTest
{
    /*
    [TestClass]
    public sealed class FileLoggerWriterUnitTest
    {
        private readonly string FilePath = "./";
        private static readonly Action<ILogger, string, string, Exception?> UserLogged = LoggerMessage.Define<string, string>(LogLevel.Information, default, "User {UserName} logged in from {MachineName}.");
        private static readonly SanlogLoggerOptions Options = new SanlogLoggerOptions { AppId = Guid.NewGuid() };

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
            await using (var writer = new FileLoggerWriter(directory: FilePath, filePrefix: "DropWrite", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileFullMode.DropWrite, encoding: Encoding.Unicode, allowSynchronousContinuations: false))
            {
                var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, () => Options);
                UserLoggedInvoke(logger, null, 3);
                searchPattern = writer.SearchPattern;
            }
            var files = Directory.GetFiles(FilePath, searchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropWrite{DateTime.Now:yyyyMMdd}_0.log", files[0]);
            Assert.AreEqual($"{FilePath}DropWrite{DateTime.Now:yyyyMMdd}_1.log", files[1]);
        }
        [TestMethod]
        public void FileCountLimitDropNewest()
        {
            string? searchPattern = null;
            using (var writer = new FileLoggerWriter(FilePath, filePrefix: "DropNewest", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileFullMode.DropNewest, encoding: Encoding.UTF8, allowSynchronousContinuations: false))
            {
                var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, () => Options);
                UserLoggedInvoke(logger, null, 50);
                searchPattern = writer.SearchPattern;
            }
            var files = Directory.GetFiles(FilePath, searchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropNewest{DateTime.Now:yyyyMMdd}_0.log", files[0]);
            Assert.AreEqual($"{FilePath}DropNewest{DateTime.Now:yyyyMMdd}_24.log", files[1]);
        }
        [TestMethod]
        public void FileCountLimitDropOldest()
        {
            var writer = new FileLoggerWriter(FilePath, filePrefix: "DropOldest", fileSizeLimit: 1024, fileCountLimit: 2, strategy: FileFullMode.DropOldest, encoding: Encoding.UTF8, allowSynchronousContinuations: false);
            var logger = new SanlogLogger("Leviasan.Sanlog.MSTest", writer, () => Options);
            try
            {
                UserLoggedInvoke(logger, null, 50);
            }
            finally
            {
                //await Task.Delay(2000);
                writer.Dispose();
            }
            var files = Directory.GetFiles(FilePath, writer.SearchPattern, SearchOption.TopDirectoryOnly);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual($"{FilePath}DropOldest{DateTime.Now:yyyyMMdd}_10.log", files[0]);
            Assert.AreEqual($"{FilePath}DropOldest{DateTime.Now:yyyyMMdd}_9.log", files[1]);
        }
        private static void UserLoggedInvoke(ILogger logger, Exception? exception, int count)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
            for (var index = 0; index < count; index++)
                UserLogged.Invoke(logger, Environment.UserName, Environment.MachineName, exception);
        }
    }
    */
}