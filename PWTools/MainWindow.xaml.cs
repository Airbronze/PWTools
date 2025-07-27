using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Win32;

namespace HostsEditor
{
    public partial class MainWindow : Window
    {
        string hostsPath = @"C:\Windows\System32\drivers\etc\hosts";

        string[] awsRedirectLines =
        {
            "127.0.0.1 lambda.us-east-1.amazonaws.com",
            "127.0.0.1 ec2-34-237-73-93.compute-1.amazonaws.com",
            "127.0.0.1 cognito-identity.us-east-1.amazonaws.com"
        };

        string prodHost = "prod.gamev92.portalworldsgame.com";

        public MainWindow()
        {
            InitializeComponent();
            CreateHostsBackupIfMissing();
            CheckHostsFile();
        }

        private void CreateHostsBackupIfMissing()
        {
            string backupPath = hostsPath + "_BACKUP";
            if (!File.Exists(backupPath))
            {
                try
                {
                    File.Copy(hostsPath, backupPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create backup: " + ex.Message);
                }
            }
        }

        void CheckHostsFile()
        {
            if (!File.Exists(hostsPath))
            {
                MessageBox.Show("Hosts file not found.");
                return;
            }

            var lines = File.ReadAllLines(hostsPath);
            bool prodRedirectExists = lines.Any(l => l.Contains(prodHost) && !l.StartsWith("127.0.0.1"));
            bool localhostRedirectExists = lines.Contains($"127.0.0.1 {prodHost}");
            bool awsRedirectsExist = awsRedirectLines.All(line => lines.Contains(line));

            btnConnect.Visibility = prodRedirectExists ? Visibility.Collapsed : Visibility.Visible;
            btnLocalhost.Visibility = localhostRedirectExists ? Visibility.Collapsed : Visibility.Visible;
            btnAWS.Visibility = awsRedirectsExist ? Visibility.Collapsed : Visibility.Visible;
            btnDisconnect.Visibility = (prodRedirectExists || localhostRedirectExists || awsRedirectsExist) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIP.Text.Trim();
            if (!Regex.IsMatch(ip, @"^\d{1,3}(\.\d{1,3}){3}$"))
            {
                MessageBox.Show("Please enter a valid IPv4 address.");
                return;
            }

            var lines = File.ReadAllLines(hostsPath).ToList();

            lines.RemoveAll(line => line.Contains(prodHost));
            lines.Add($"{ip} {prodHost}");

            File.WriteAllLines(hostsPath, lines);
            MessageBox.Show($"Connected to {ip}");
            RefreshUI();
        }

        private void BtnLocalhost_Click(object sender, RoutedEventArgs e)
        {
            var lines = File.ReadAllLines(hostsPath).ToList();

            lines.RemoveAll(l => l.Contains(prodHost));
            lines.Add($"127.0.0.1 {prodHost}");

            File.WriteAllLines(hostsPath, lines);
            MessageBox.Show("Set to localhost.");
            RefreshUI();
        }

        private void BtnAWS_Click(object sender, RoutedEventArgs e)
        {
            var lines = File.ReadAllLines(hostsPath).ToList();
            foreach (var line in awsRedirectLines)
            {
                if (!lines.Contains(line))
                    lines.Add(line);
            }

            File.WriteAllLines(hostsPath, lines);
            MessageBox.Show("AWS redirects added.");
            RefreshUI();
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var lines = File.ReadAllLines(hostsPath).ToList();

            lines.RemoveAll(l => l.Contains(prodHost));

            foreach (var aws in awsRedirectLines)
                lines.RemoveAll(l => l.Trim() == aws);

            File.WriteAllLines(hostsPath, lines);
            MessageBox.Show("Redirects removed.");
            RefreshUI();
        }

        private void RefreshUI()
        {
            btnConnect.Visibility = Visibility.Collapsed;
            btnDisconnect.Visibility = Visibility.Collapsed;
            btnLocalhost.Visibility = Visibility.Collapsed;
            btnAWS.Visibility = Visibility.Collapsed;
            CheckHostsFile();
        }

        private void TxtIP_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtIP.Text == "Enter IP address...")
            {
                txtIP.Text = string.Empty;
            }
        }

        private void BtnModifyRegistry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Kukouri\PixelWorlds", writable: true))
                {
                    if (key == null)
                    {
                        MessageBox.Show("Registry key not found.");
                        return;
                    }

                    string valueName = key.GetValueNames()
                        .FirstOrDefault(name => name.Contains("CognitoIdentity:IdentityId"));

                    if (valueName != null)
                    {
                        string newId = GenerateRandomCognitoId();
                        key.SetValue(valueName, newId);
                        MessageBox.Show($"CognitoIdentity updated:\n{newId}");
                    }
                    else
                    {
                        MessageBox.Show("CognitoIdentity:IdentityId not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registry error: " + ex.Message);
            }
        }

        private string GenerateRandomCognitoId()
        {
            string RandomString(int len)
            {
                const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                var rand = new Random();
                return new string(Enumerable.Repeat(chars, len).Select(s => s[rand.Next(s.Length)]).ToArray());
            }

            return $"us-east-1:reboot{RandomString(2).ToUpper()}-2025-{RandomString(4)}-{RandomString(4)}-airbronze{RandomString(3)}";
        }
    }
}