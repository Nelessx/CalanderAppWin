using System;
using System.IO;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class AtomicFileTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "NCAtomicTest_" + Guid.NewGuid().ToString("N"));

        private string Path_(string name) => Path.Combine(_dir, name);

        [Fact]
        public void WriteAllText_CreatesFile_AndCreatesMissingDirectory()
        {
            string path = Path_("sub/data.json");
            AtomicFile.WriteAllText(path, "hello");

            Assert.True(File.Exists(path));
            Assert.Equal("hello", File.ReadAllText(path));
        }

        [Fact]
        public void SecondWrite_PreservesPreviousContentInBak()
        {
            string path = Path_("data.json");
            AtomicFile.WriteAllText(path, "first");
            AtomicFile.WriteAllText(path, "second");

            Assert.Equal("second", File.ReadAllText(path));
            Assert.True(File.Exists(path + ".bak"));
            Assert.Equal("first", File.ReadAllText(path + ".bak"));
        }

        [Fact]
        public void ReadWithRecovery_ReadsPrimary_WhenValid()
        {
            string path = Path_("data.json");
            AtomicFile.WriteAllText(path, "primary");

            Assert.Equal("primary", AtomicFile.ReadAllTextWithRecovery(path));
        }

        [Fact]
        public void ReadWithRecovery_FallsBackToBak_WhenPrimaryDeleted()
        {
            string path = Path_("data.json");
            AtomicFile.WriteAllText(path, "good");   // no bak yet
            AtomicFile.WriteAllText(path, "newer");  // now bak = "good"

            File.Delete(path); // simulate a lost/torn primary

            Assert.Equal("good", AtomicFile.ReadAllTextWithRecovery(path));
        }

        [Fact]
        public void ReadWithRecovery_ReturnsNull_WhenNothingExists()
        {
            Assert.Null(AtomicFile.ReadAllTextWithRecovery(Path_("nope.json")));
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }
    }
}
