using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Interaction logic for EventListWindow.xaml.
    /// Lists every saved event and lets the user add, edit, or delete them.
    /// </summary>
    public partial class EventListWindow : Window
    {
        private readonly EventStore _eventStore = new();
        private readonly BsDateConverter _converter = new();

        public EventListWindow()
        {
            InitializeComponent();
            LoadEvents();
        }

        private void LoadEvents()
        {
            var items = _eventStore.GetAll()
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

            EventsItemsControl.ItemsSource = items;
            EventCountText.Text = items.Count.ToString();
            EmptyState.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

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
                $"Delete \"{item.Title}\"? This cannot be undone.",
                "Delete event",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _eventStore.Delete(item.Source.Id);
            LoadEvents();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
