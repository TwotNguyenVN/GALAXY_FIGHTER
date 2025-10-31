using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            this.Load += AboutForm_Load;
            lnkProject.LinkClicked += LnkProject_LinkClicked;
            btnOk.Click += (_, __) => this.Close();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            lblVersion.Text = $"Version: {ver?.ToString() ?? "1.0.0.0"}";
            
        }

        private void LnkProject_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // Đổi URL này thành repo/tài liệu của bạn
                var url = "https://example.com/your-project";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }

        private void lnkProject_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }
    }
}
