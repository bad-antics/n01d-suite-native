using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace N01D.Calc
{
    public partial class MainWindow : Window
    {
        private bool _updating;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Number Converter ──

        private void UpdateAll(long value, TextBox? source)
        {
            _updating = true;
            try
            {
                if (source != txtDec) txtDec.Text = value.ToString();
                if (source != txtHex) txtHex.Text = value.ToString("X");
                if (source != txtOct) txtOct.Text = Convert.ToString(value, 8);
                if (source != txtBin) txtBin.Text = Convert.ToString(value, 2);

                // ASCII representation
                if (value >= 0 && value <= 127)
                    txtAscii.Text = value >= 32 ? $"'{(char)value}'" : $"(control: 0x{value:X2})";
                else
                    txtAscii.Text = "(non-ASCII)";

                lblStatus.Text = $"[ VALUE: {value} | 0x{value:X} | {Convert.ToString(value, 2).Length} bits ]";
            }
            finally { _updating = false; }
        }

        private void TxtDec_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            if (long.TryParse(txtDec.Text.Trim(), out var val))
                UpdateAll(val, txtDec);
        }

        private void TxtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            if (long.TryParse(txtHex.Text.Trim(), NumberStyles.HexNumber, null, out var val))
                UpdateAll(val, txtHex);
        }

        private void TxtOct_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            try
            {
                var val = Convert.ToInt64(txtOct.Text.Trim(), 8);
                UpdateAll(val, txtOct);
            }
            catch { }
        }

        private void TxtBin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            try
            {
                var val = Convert.ToInt64(txtBin.Text.Trim(), 2);
                UpdateAll(val, txtBin);
            }
            catch { }
        }

        private void BtnNot_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(txtDec.Text.Trim(), out var val))
                UpdateAll(~val, null);
        }

        private void BtnShl_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(txtDec.Text.Trim(), out var val))
                UpdateAll(val << 1, null);
        }

        private void BtnShr_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(txtDec.Text.Trim(), out var val))
                UpdateAll(val >> 1, null);
        }

        private void BtnSwapEndian_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(txtDec.Text.Trim(), out var val))
            {
                var bytes = BitConverter.GetBytes(val);
                Array.Reverse(bytes);
                UpdateAll(BitConverter.ToInt64(bytes), null);
            }
        }

        // ── Hash Generator ──

        private void BtnHash_Click(object sender, RoutedEventArgs e)
        {
            var input = txtHashInput.Text;
            var bytes = Encoding.UTF8.GetBytes(input);
            var sb = new StringBuilder();

            sb.AppendLine($"MD5:      {Hash(MD5.Create(), bytes)}");
            sb.AppendLine($"SHA-1:    {Hash(SHA1.Create(), bytes)}");
            sb.AppendLine($"SHA-256:  {Hash(SHA256.Create(), bytes)}");
            sb.AppendLine($"SHA-384:  {Hash(SHA384.Create(), bytes)}");
            sb.AppendLine($"SHA-512:  {Hash(SHA512.Create(), bytes)}");
            sb.AppendLine();
            sb.AppendLine($"CRC32:    {Crc32(bytes):X8}");
            sb.AppendLine();
            sb.AppendLine($"Byte count: {bytes.Length}");

            txtHashOutput.Text = sb.ToString();
            lblStatus.Text = "[ HASHES GENERATED ]";
        }

        private static string Hash(HashAlgorithm alg, byte[] data)
        {
            using (alg) return BitConverter.ToString(alg.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        private static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (var b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            return ~crc;
        }

        // ── Encode / Decode ──

        private void BtnB64Enc_Click(object sender, RoutedEventArgs e)
        {
            txtEncodeOutput.Text = Convert.ToBase64String(Encoding.UTF8.GetBytes(txtEncodeInput.Text));
            lblStatus.Text = "[ BASE64 ENCODED ]";
        }

        private void BtnB64Dec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtEncodeOutput.Text = Encoding.UTF8.GetString(Convert.FromBase64String(txtEncodeInput.Text.Trim()));
                lblStatus.Text = "[ BASE64 DECODED ]";
            }
            catch (Exception ex) { txtEncodeOutput.Text = $"Error: {ex.Message}"; }
        }

        private void BtnUrlEnc_Click(object sender, RoutedEventArgs e)
        {
            txtEncodeOutput.Text = WebUtility.UrlEncode(txtEncodeInput.Text);
            lblStatus.Text = "[ URL ENCODED ]";
        }

        private void BtnUrlDec_Click(object sender, RoutedEventArgs e)
        {
            txtEncodeOutput.Text = WebUtility.UrlDecode(txtEncodeInput.Text);
            lblStatus.Text = "[ URL DECODED ]";
        }

        private void BtnHexEnc_Click(object sender, RoutedEventArgs e)
        {
            var bytes = Encoding.UTF8.GetBytes(txtEncodeInput.Text);
            txtEncodeOutput.Text = BitConverter.ToString(bytes).Replace("-", " ");
            lblStatus.Text = "[ HEX ENCODED ]";
        }

        private void BtnHexDec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hex = txtEncodeInput.Text.Replace(" ", "").Replace("-", "");
                var bytes = Enumerable.Range(0, hex.Length / 2)
                    .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
                txtEncodeOutput.Text = Encoding.UTF8.GetString(bytes);
                lblStatus.Text = "[ HEX DECODED ]";
            }
            catch (Exception ex) { txtEncodeOutput.Text = $"Error: {ex.Message}"; }
        }

        // ── Regex Tester ──

        private void TxtRegex_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunRegex();
        }

        private void RunRegex()
        {
            var pattern = txtRegexPattern.Text;
            var input = txtRegexInput.Text;
            if (string.IsNullOrEmpty(pattern)) { txtRegexOutput.Text = ""; return; }

            try
            {
                var regex = new Regex(pattern, RegexOptions.Multiline, TimeSpan.FromSeconds(2));
                var matches = regex.Matches(input);
                var sb = new StringBuilder();
                sb.AppendLine($"Found {matches.Count} match(es):\r\n");
                int idx = 0;
                foreach (Match m in matches)
                {
                    sb.AppendLine($"  [{idx++}] Index={m.Index} Length={m.Length} Value=\"{m.Value}\"");
                    for (int g = 1; g < m.Groups.Count; g++)
                        sb.AppendLine($"       Group[{g}]: \"{m.Groups[g].Value}\"");
                }
                txtRegexOutput.Text = sb.ToString();
                lblStatus.Text = $"[ {matches.Count} MATCHES ]";
            }
            catch (RegexParseException ex)
            {
                txtRegexOutput.Text = $"Regex error: {ex.Message}";
                lblStatus.Text = "[ REGEX ERROR ]";
            }
        }
    }
}
