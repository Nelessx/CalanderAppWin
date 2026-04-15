using NepaliCalendar.App.Services;
using System;
using System.Windows;

namespace NepaliCalendar.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var converter = new BsDateConverter();
            var todayBs = converter.ConvertFromAd(DateTime.Today);

            BsDateText.Text = $"Today's BS Date: {todayBs}";
            DayNameText.Text = $"Day: {todayBs.DayName}";
        }
    }
}