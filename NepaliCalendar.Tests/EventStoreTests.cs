using System;
using System.IO;
using System.Linq;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class EventStoreTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "NCStoreTest_" + Guid.NewGuid().ToString("N"));

        private EventStore NewStore() => new(_dir);

        private static CalendarEvent Sample(string title, DateTime ad, int y, int m, int d, bool allDay = false, string? time = null) =>
            new() { Title = title, AdDate = ad, BsYear = y, BsMonth = m, BsDay = d, IsAllDay = allDay, TimeText = time };

        [Fact]
        public void Add_AssignsId_SetsTimestamps_Persists()
        {
            var store = NewStore();
            var e = store.Add(Sample("A", new DateTime(2026, 5, 10), 2083, 1, 27));

            Assert.NotEqual(Guid.Empty, e.Id);
            Assert.NotEqual(default, e.CreatedUtc);
            Assert.Equal(e.CreatedUtc, e.ModifiedUtc);
            Assert.Single(NewStore().GetAll());
        }

        [Fact]
        public void Update_PreservesCreatedUtc_NoDuplicate()
        {
            var store = NewStore();
            var e = store.Add(Sample("A", new DateTime(2026, 5, 10), 2083, 1, 27));
            System.Threading.Thread.Sleep(5);

            var edited = Sample("A edited", e.AdDate, e.BsYear, e.BsMonth, e.BsDay);
            edited.Id = e.Id;
            Assert.True(store.Update(edited));

            var all = NewStore().GetAll();
            Assert.Single(all);
            Assert.Equal("A edited", all[0].Title);
            Assert.Equal(e.CreatedUtc, all[0].CreatedUtc);
            Assert.True(all[0].ModifiedUtc > e.ModifiedUtc);
        }

        [Fact]
        public void Update_UnknownId_ReturnsFalse()
        {
            Assert.False(NewStore().Update(Sample("ghost", DateTime.Today, 2083, 1, 1)));
        }

        [Fact]
        public void Delete_RemovesById()
        {
            var store = NewStore();
            var a = store.Add(Sample("A", new DateTime(2026, 5, 10), 2083, 1, 27));
            store.Add(Sample("B", new DateTime(2026, 5, 11), 2083, 1, 28));

            store.Delete(a.Id);

            var all = NewStore().GetAll();
            Assert.Single(all);
            Assert.DoesNotContain(all, x => x.Id == a.Id);
        }

        [Fact]
        public void GetForBsDate_OrdersAllDayFirstThenByTime()
        {
            var store = NewStore();
            store.Add(Sample("Ten", new DateTime(2026, 5, 10), 2083, 1, 27, time: "10:00 AM"));
            store.Add(Sample("Nine", new DateTime(2026, 5, 10), 2083, 1, 27, time: "9:00 AM"));
            store.Add(Sample("AllDay", new DateTime(2026, 5, 10), 2083, 1, 27, allDay: true));

            var day = store.GetForBsDate(2083, 1, 27);
            Assert.Equal(new[] { "AllDay", "Nine", "Ten" }, day.Select(e => e.Title).ToArray());
        }

        [Fact]
        public void GetUpcoming_ExcludesPast_AndHonorsLimit()
        {
            var store = NewStore();
            store.Add(Sample("Past", new DateTime(2026, 5, 1), 2083, 1, 18));
            store.Add(Sample("Now", new DateTime(2026, 5, 10), 2083, 1, 27));
            store.Add(Sample("Later", new DateTime(2026, 6, 1), 2083, 2, 18));

            Assert.Equal(2, store.GetUpcoming(new DateTime(2026, 5, 10), 10).Count);
            Assert.Single(store.GetUpcoming(new DateTime(2026, 5, 10), 1));
        }

        [Fact]
        public void CorruptFile_YieldsEmpty()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, "events.json"), "{ not valid json ]");
            Assert.Empty(NewStore().GetAll());
        }

        [Fact]
        public void CorruptPrimary_RecoversFromBackup()
        {
            var store = NewStore();
            store.Add(Sample("Keeper", new DateTime(2026, 5, 10), 2083, 1, 27)); // writes events.json
            store.Add(Sample("Second", new DateTime(2026, 5, 11), 2083, 1, 28)); // rolls prior copy into .bak

            // Simulate a torn primary write; the .bak still holds the prior good copy.
            File.WriteAllText(Path.Combine(_dir, "events.json"), "corrupted <<<");

            var recovered = NewStore().GetAll();
            Assert.Single(recovered);
            Assert.Equal("Keeper", recovered[0].Title);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }
    }
}
