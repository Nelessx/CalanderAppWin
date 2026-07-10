using System;
using System.Collections.Generic;
using System.Windows;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml.
    /// Reads and writes app settings (language, default widget size) plus the
    /// Windows startup registration. DialogResult is true when settings were saved.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService = new();
        private readonly StartupService _startupService = new();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = _settingsService.Load();

            LanguageComboBox.ItemsSource = new List<Option>
            {
                new(AppLanguage.English, "English"),
                new(AppLanguage.Nepali, "नेपाली (Nepali)")
            };
            LanguageComboBox.DisplayMemberPath = nameof(Option.Text);
            LanguageComboBox.SelectedValuePath = nameof(Option.Value);
            LanguageComboBox.SelectedValue = settings.Language;

            ThemeComboBox.ItemsSource = new List<Option>
            {
                new(AppTheme.Light, "Light"),
                new(AppTheme.Dark, "Dark"),
                new(AppTheme.System, "System (follow Windows)")
            };
            ThemeComboBox.DisplayMemberPath = nameof(Option.Text);
            ThemeComboBox.SelectedValuePath = nameof(Option.Value);
            ThemeComboBox.SelectedValue = settings.Theme;

            WidgetSizeComboBox.ItemsSource = new List<Option>
            {
                new(WidgetSize.Small, "Small"),
                new(WidgetSize.Medium, "Medium"),
                new(WidgetSize.Large, "Large")
            };
            WidgetSizeComboBox.DisplayMemberPath = nameof(Option.Text);
            WidgetSizeComboBox.SelectedValuePath = nameof(Option.Value);
            WidgetSizeComboBox.SelectedValue = settings.SelectedWidgetSize;

            RunAtStartupCheckBox.IsChecked = _startupService.IsEnabled();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsService.Load();

            if (LanguageComboBox.SelectedValue is AppLanguage language)
                settings.Language = language;

            if (WidgetSizeComboBox.SelectedValue is WidgetSize widgetSize)
                settings.SelectedWidgetSize = widgetSize;

            if (ThemeComboBox.SelectedValue is AppTheme theme)
                settings.Theme = theme;

            _settingsService.Save(settings);

            App.ApplyTheme(settings.Theme);

            try
            {
                _startupService.SetEnabled(RunAtStartupCheckBox.IsChecked == true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Settings were saved, but the 'Start with Windows' option could not be updated:\n\n" + ex.Message,
                    "Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>A value + its display label, used for the settings dropdowns.</summary>
        private sealed record Option(object Value, string Text);
    }
}
