using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace HostDetector
{
    public partial class FormMain : Form
    {
        SoundPlayer soundPlayer;
        HostDetectorSettings settings;
        delegate void NotifyPresentCallback();
        public FormMain(HostDetectorSettings settings, HostDetector detector)
        {
            InitializeComponent();
            this.settings = settings;
            

            detector.NotifyPresent += InvokeNotifyPresent;
            Monitor.Exit(detector); // Allow detector thread to proceed.
            UnmanagedMemoryStream audioStream;
            switch (settings.soundId)
            {
                case 1:
                    audioStream = Properties.Resources.MainSound1;
                    break;
                case 2:
                    audioStream = Properties.Resources.MainSound2;
                    break;
                default:
                    audioStream = Properties.Resources.MainSound1;
                    break;
            }
            soundPlayer = new SoundPlayer(audioStream);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Visible = false;
                soundPlayer.Stop();
            }
        }
        private void InvokeNotifyPresent(object sender, EventArgs e)
        {
            this.Invoke(new NotifyPresentCallback(NotifyPresent));
        }
        private void NotifyPresent()
        {
            WindowState = FormWindowState.Normal;
            this.Visible = true;
            this.Focus();
            soundPlayer.Play();
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowState = FormWindowState.Normal;
                this.Visible = true;
                this.Focus();
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void setStartupMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("HostDetector", '"'+Application.ExecutablePath.ToString()+'"');
        }

        private void unsetStartupMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.DeleteValue("HostDetector",false);
        }

        private void FormMain_Load(object sender, EventArgs e)
        {

        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}
