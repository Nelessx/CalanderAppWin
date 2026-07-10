using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Interaction logic for EventListWindow.xaml.
    /// Lists every saved event and lets the user add, edit, delete, search, import, and undo.
    /// </summary>
    public partial class EventListWindow : Window
    {
        private readonly EventStore _eventStore = new();
        private readonly BsDateConverter _converter = new();
        private readonly CalendarImportService _importService = new();

        private List<EventListItem> _allItems = new();

        public EventListWindow()
        {
            InitializeComponent();
            LoadEvents();
        }

        private void LoadEvents()
        {
            _allItems = _eventStore.GetAll()
                .OrderBy(e => e.AdDate)
                .Select(e => new EventListItem
                {
                    Source = e,
                    Title = e.Title,
                    DateText = $"{_converter.GetNepaliMonthName(e.BsMonth)} {e.BsDay}, {e.BsYear}  ·  {e.AdDate:MMM d, yyyy}",
                    TimeText = e.IsAllDay
                        ? "All Day"
                        : (string.IsNullOrWhiteSpace(e.TimeText) ? "No time set" : e.TimeText!),
                    BadgeText = e.BadgeText,
                    ShowBadge = !string.IsNullOrWhiteSpace(e.BadgeText)
                })
                .ToList();

            ApplyFilter();
            UndoButton.IsEnabled = _eventStore.CanUndoDelete;
        }

        private void ApplyFilter()
        {
            string query = SearchBox.Text.Trim();
            SearchHint.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

            var visible = string.IsNullOrEmpty(query)
                ? _allItems
                : _allItems.Where(i => Matches(i, query)).ToList();

            EventsItemsControl.ItemsSource = visible;
            EventCountText.Text = visible.Count.ToString();
            EmptyState.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Text = _allItems.Count == 0
                ? "No events yet. Click \"+ Add Event\" to create one."
                : "No events match your search.";
        }

        private static bool Matches(EventListItem item, string query)
        {
            bool Has(string? s) => s?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
            return Has(item.Title) || Has(item.BadgeText) || Has(item.DateText)
                || Has(item.Source.EventType) || Has(item.Source.Location) || Has(item.Source.NepaliTitle);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEventWindow { Owner = this };

            if (dialog.ShowDialog() == true)
                LoadEvents();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not EventListItem item)
                return;

            var dialog = new AddEventWindow(item.Source) { Owner = this };

            if (dialog.ShowDialog() == true)
                LoadEvents();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not EventListItem item)
                return;

            var result = MessageBox.Show(
                $"Delete \"{item.Title}\"?",
                "Delete event",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _eventStore.Delete(item.Source.Id);
            LoadEvents();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            _eventStore.RestoreLastDeleted();
            LoadEvents();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import events",
                Filter = "Calendar files (*.ics;*.csv)|*.ics;*.csv|iCalendar (*.ics)|*.ics|CSV (*.csv)|*.csv"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                string content = File.ReadAllText(dialog.FileName);
                bool isCsv = Path.GetExtension(dialog.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

                var imported = _importService.Parse(content, isCsv);
                if (imported.Count == 0)
                {
                    MessageBox.Show(
                        "No importable events were found (they may fall outside the supported date range).",
                        "Import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                int added = _eventStore.AddRange(imported);
                LoadEvents();

                MessageBox.Show(
                    $"Imported {added} event(s).",
                    "Import complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not import that file: " + ex.Message,
                    "Import failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class EventListItem
        {
            public CalendarEvent Source { get; init; } = new();
            public string Title { get; init; } = string.Empty;
            public string DateText { get; init; } = string.Empty;
            public string TimeText { get; init; } = string.Empty;
            public string? BadgeText { get; init; }
            public bool ShowBadge { get; init; }
        }
    }
}
