using System.Windows;
using System.Windows.Input;

namespace N01D.Vault
{
    public partial class UnlockDialog : Window
    {
        public string MasterPassword { get; private set; } = "";

        public UnlockDialog()
        {
            InitializeComponent();
            txtMasterPw.Focus();
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMasterPw.Password))
            {
                MessageBox.Show("Master password cannot be empty.", "N01D Vault",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MasterPassword = txtMasterPw.Password;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void TxtMasterPw_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnUnlock_Click(sender, e);
        }
    }
}
