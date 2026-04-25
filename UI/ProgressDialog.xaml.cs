using System;
using System.ComponentModel;
using System.Windows;

namespace StoryManager.UI
{
    /// <summary>Lightweight progress dialog used during startup story-load and shutdown story-save. The owner can call
    /// <see cref="Report"/> to update progress (any thread) and <see cref="MarkComplete"/> when done; if AutoCloseOnComplete is
    /// true the dialog dismisses itself, otherwise it shows an OK button so the user can acknowledge the completion.</summary>
    public partial class ProgressDialog : Window
    {
        private bool _IsComplete;
        private bool _AllowClose;
        private readonly bool _AutoCloseOnComplete;

        public ProgressDialog(string Header, bool AutoCloseOnComplete)
        {
            InitializeComponent();
            HeaderText.Text = Header;
            _AutoCloseOnComplete = AutoCloseOnComplete;
            //  Indeterminate while we don't yet know the total.
            Bar.IsIndeterminate = true;
            StatusText.Text = "Preparing...";
        }

        /// <summary>Updates the progress display. Safe to call from any thread.</summary>
        public void Report(int Current, int Total, string Status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => Report(Current, Total, Status)));
                return;
            }

            if (Total <= 0)
            {
                Bar.IsIndeterminate = true;
            }
            else
            {
                Bar.IsIndeterminate = false;
                Bar.Maximum = Total;
                Bar.Value = Math.Max(0, Math.Min(Current, Total));
            }
            StatusText.Text = Status ?? string.Empty;
        }

        /// <summary>Marks the operation finished. If <c>AutoCloseOnComplete</c> was true, the dialog closes itself;
        /// otherwise the OK button becomes visible so the user can dismiss it. Pass <paramref name="FinalHeader"/> to
        /// replace the in-progress header (e.g. "Loading stories...") with a completion message such as "Done".</summary>
        public void MarkComplete(string FinalStatus, string FinalHeader = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => MarkComplete(FinalStatus, FinalHeader)));
                return;
            }

            _IsComplete = true;
            Bar.IsIndeterminate = false;
            if (Bar.Maximum > 0)
                Bar.Value = Bar.Maximum;
            if (!string.IsNullOrEmpty(FinalStatus))
                StatusText.Text = FinalStatus;
            if (!string.IsNullOrEmpty(FinalHeader))
                HeaderText.Text = FinalHeader;

            if (_AutoCloseOnComplete)
            {
                _AllowClose = true;
                Close();
            }
            else
            {
                OkButton.Visibility = Visibility.Visible;
                OkButton.Focus();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _AllowClose = true;
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //  Block the close (alt-F4 / window X) until the operation is complete; otherwise progress reports could
            //  arrive after the dialog is gone, causing dispatch on a closed window.
            if (!_IsComplete && !_AllowClose)
                e.Cancel = true;
        }
    }
}
