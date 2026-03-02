using System;
using System.Windows;

namespace N01D.Cron
{
    public partial class JobDialog : Window
    {
        public CronJob Job { get; private set; } = new();

        public JobDialog()
        {
            InitializeComponent();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCommand.Text))
            {
                MessageBox.Show("Name and Command are required.", "N01D Cron",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Job.Name = txtName.Text.Trim();
            Job.Command = txtCommand.Text.Trim();
            Job.WorkingDirectory = txtWorkDir.Text.Trim();

            Job.ScheduleType = cmbSchedule.SelectedIndex switch
            {
                0 => JobScheduleType.Once,
                1 => JobScheduleType.Interval,
                2 => JobScheduleType.Hourly,
                3 => JobScheduleType.Daily,
                _ => JobScheduleType.Interval
            };

            if (int.TryParse(txtInterval.Text, out var interval))
                Job.IntervalSeconds = interval;

            if (TimeSpan.TryParse(txtDailyTime.Text, out var daily))
                Job.DailyTime = daily;

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
