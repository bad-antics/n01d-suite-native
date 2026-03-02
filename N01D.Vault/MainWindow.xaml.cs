using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace N01D.Vault
{
    public class VaultEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Modified { get; set; } = DateTime.Now;
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<VaultEntry> _entries = new();
        private ObservableCollection<VaultEntry> _filteredEntries = new();
        private VaultEntry? _currentEntry;
        private byte[]? _masterKey;
        private bool _isUnlocked;
        private bool _showPassword;
        private string _passwordPlain = "";
        private DispatcherTimer? _clipboardTimer;
        private readonly string _vaultDir;
        private readonly string _vaultFile;

        public MainWindow()
        {
            InitializeComponent();
            _vaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "N01D", "Vault");
            _vaultFile = Path.Combine(_vaultDir, "vault.dat");
            Directory.CreateDirectory(_vaultDir);

            lstEntries.ItemsSource = _filteredEntries;
            PromptUnlock();
        }

        private void PromptUnlock()
        {
            var dlg = new UnlockDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                var pw = dlg.MasterPassword;
                _masterKey = DeriveKey(pw);

                if (File.Exists(_vaultFile))
                {
                    try
                    {
                        var data = File.ReadAllBytes(_vaultFile);
                        var json = Decrypt(data, _masterKey);
                        var list = JsonSerializer.Deserialize<VaultEntry[]>(json);
                        if (list != null)
                            foreach (var e in list) _entries.Add(e);
                    }
                    catch
                    {
                        MessageBox.Show("Wrong master password or corrupted vault.", "N01D Vault",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        _masterKey = null;
                        PromptUnlock();
                        return;
                    }
                }

                _isUnlocked = true;
                RefreshList();
                lblStatus.Text = $"[ UNLOCKED — {_entries.Count} entries ]";
                lblStatus.Foreground = FindResource("N01DPrimaryBrush") as System.Windows.Media.Brush;
            }
            else
            {
                lblStatus.Text = "[ LOCKED ]";
            }
        }

        private void RefreshList()
        {
            var filter = txtSearch.Text.Trim().ToLowerInvariant();
            _filteredEntries.Clear();
            foreach (var e in _entries.Where(e =>
                string.IsNullOrEmpty(filter) ||
                e.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            {
                _filteredEntries.Add(e);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();

        private void LstEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEntries.SelectedItem is VaultEntry entry)
            {
                _currentEntry = entry;
                txtTitle.Text = entry.Title;
                txtUsername.Text = entry.Username;
                _passwordPlain = entry.Password;
                txtPassword.Password = entry.Password;
                txtNotes.Text = entry.Notes;
                pnlDetails.Visibility = Visibility.Visible;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!_isUnlocked) return;
            var entry = new VaultEntry { Title = "New Entry" };
            _entries.Add(entry);
            RefreshList();
            lstEntries.SelectedItem = entry;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null) return;
            if (MessageBox.Show($"Delete \"{_currentEntry.Title}\"?", "N01D Vault",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _entries.Remove(_currentEntry);
                _currentEntry = null;
                pnlDetails.Visibility = Visibility.Collapsed;
                RefreshList();
                SaveVault();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null) return;
            _currentEntry.Title = txtTitle.Text;
            _currentEntry.Username = txtUsername.Text;
            _currentEntry.Password = _showPassword ? _passwordPlain : txtPassword.Password;
            _currentEntry.Notes = txtNotes.Text;
            _currentEntry.Modified = DateTime.Now;
            SaveVault();
            RefreshList();
            lblStatus.Text = $"[ SAVED — {DateTime.Now:HH:mm:ss} ]";
        }

        private void BtnCopyUser_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtUsername.Text);
            lblStatus.Text = "[ USERNAME COPIED ]";
        }

        private void BtnCopyPass_Click(object sender, RoutedEventArgs e)
        {
            var pw = _showPassword ? _passwordPlain : txtPassword.Password;
            Clipboard.SetText(pw);
            lblStatus.Text = "[ PASSWORD COPIED — auto-clear in 15s ]";

            _clipboardTimer?.Stop();
            _clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _clipboardTimer.Tick += (_, _) =>
            {
                Clipboard.Clear();
                lblStatus.Text = "[ CLIPBOARD CLEARED ]";
                _clipboardTimer.Stop();
            };
            _clipboardTimer.Start();
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _showPassword = !_showPassword;
            // WPF PasswordBox can't show plain text natively, 
            // so we store plain and toggle visibility hint in status
            if (_showPassword)
            {
                _passwordPlain = txtPassword.Password;
                lblStatus.Text = $"[ PASSWORD: {_passwordPlain} ]";
            }
            else
            {
                lblStatus.Text = "[ PASSWORD HIDDEN ]";
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            var pw = GeneratePassword(24);
            txtPassword.Password = pw;
            _passwordPlain = pw;
            lblStatus.Text = "[ PASSWORD GENERATED — 24 chars ]";
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            SaveVault();
            _entries.Clear();
            _filteredEntries.Clear();
            _masterKey = null;
            _isUnlocked = false;
            _currentEntry = null;
            pnlDetails.Visibility = Visibility.Collapsed;
            lblStatus.Text = "[ LOCKED ]";
            lblStatus.Foreground = FindResource("N01DWarningBrush") as System.Windows.Media.Brush;
            PromptUnlock();
        }

        private void SaveVault()
        {
            if (_masterKey == null) return;
            var json = JsonSerializer.Serialize(_entries.ToArray());
            var data = Encrypt(json, _masterKey);
            File.WriteAllBytes(_vaultFile, data);
        }

        // ── Crypto helpers ──

        private static byte[] DeriveKey(string password)
        {
            var salt = Encoding.UTF8.GetBytes("N01D-VAULT-SALT-2026");
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        }

        private static byte[] Encrypt(string plaintext, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using var enc = aes.CreateEncryptor();
            var plain = Encoding.UTF8.GetBytes(plaintext);
            var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
            var result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return result;
        }

        private static string Decrypt(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            var iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            var cipher = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 16, cipher, 0, cipher.Length);
            var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }

        private static string GeneratePassword(int length)
        {
            const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{}|;:,.<>?";
            var result = new char[length];
            var bytes = RandomNumberGenerator.GetBytes(length);
            for (int i = 0; i < length; i++)
                result[i] = chars[bytes[i] % chars.Length];
            return new string(result);
        }
    }
}
