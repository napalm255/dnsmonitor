using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;


namespace DNSMonitor
{
    public class DNSMonitor : System.Windows.Forms.Form
    {
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.ComponentModel.IContainer components;

        private Dictionary<string, string[]> dnsMatches = new Dictionary<string, string[]>();
        private List<string> dnsServers = new List<string>();
        private List<string> dnsDomains = new List<string>();

        [STAThread]
        static void Main()
        {
            Application.Run(new DNSMonitor());
        }

        public DNSMonitor()
        {
            initLayout();

            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
            this.Size = new Size(290, 275);
            this.ControlBox = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            //this.Text = "DNS Monitor v" + this.ProductVersion.ToString();
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width - 15;
            this.Top = Screen.PrimaryScreen.WorkingArea.Height - this.Height - 15;

            
            this.components = new System.ComponentModel.Container();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            
            // Initialize contextMenu1 
            this.contextMenu1.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { this.menuItem1, this.menuItem2 });

            // Initialize menuItem1 
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            // Initialize menuItem2 
            this.menuItem2.Index = 0;
            this.menuItem2.Text = "&Refresh";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);

            // Create the NotifyIcon. 
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);

            // The Icon property sets the icon that will appear 
            // in the systray for this application.
            notifyIcon1.Icon = new Icon(GetType(), "activity_monitor_homer.ico");

            // The ContextMenu property sets the menu that will 
            // appear when the systray icon is right clicked.
            notifyIcon1.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed, 
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon1.Text = "DNS Monitor v" + this.ProductVersion.ToString();
            notifyIcon1.Visible = true;

            // Handle the DoubleClick event to activate the form.
            notifyIcon1.MouseUp += new MouseEventHandler(this.notifyIcon1_MouseUp);

            queryDNS();
            this.Hide();
        }

        public void queryDNS()
        {
            // Refresh Config
            loadDNSConfig();
            
            // Clear Panels
            this.Controls["Main"].Controls.Clear();

            foreach (string dom in dnsDomains) {
                List<string> ipResults = new List<string>();
                foreach (string srv in dnsServers)
                {
                    var Options = new JHSoftware.DnsClient.RequestOptions();
                    Options.DnsServers = new System.Net.IPAddress[] { System.Net.IPAddress.Parse(srv) };
                    var IPs = JHSoftware.DnsClient.LookupHost(dom,
                                                          JHSoftware.DnsClient.IPVersion.IPv4,
                                                          Options);
                    foreach (var IP in IPs)
                    {
                        ipResults.Add(IP.ToString());
                    }
                }

                string matchedSide = "";
                string matchedIP = "";
                var uniqueIPs = new HashSet<string>(ipResults);
                foreach (string s in uniqueIPs)
                {
                    matchedIP = s;
                    foreach (KeyValuePair<string, string[]> match in dnsMatches)
                    {
                        if (match.Value.Contains(s))
                        {
                            matchedSide = match.Key;
                        }
                    }
                }

                addPanel(matchedSide, dom + "\n" + matchedIP);
            }

            
        }

        public bool loadDNSConfig()
        {
            dnsMatches.Clear();
            dnsMatches.Add("VA", new string[] { "64.124.172.65", "64.214.201.140" });
            dnsMatches.Add("NJ", new string[] { "64.124.8.65" });
            dnsMatches.Add("AWS", new string[] { "184.73.242.143" });

            dnsDomains.Clear();
            dnsDomains.Add("bankassetpoint.com");
            dnsDomains.Add("cdars.com");
            dnsDomains.Add("promnetwork.com");
            dnsDomains.Add("filesync.net");
            dnsDomains.Add("mentalminis.com");

            dnsServers.Clear();
            dnsServers.Add("8.8.8.8");
            dnsServers.Add("8.8.4.4");
            dnsServers.Add("4.2.2.2");

            return true;
        }

        private void initLayout()
        {
            Panel m = new Panel();
            m.Parent = this;
            m.Name = "Main";
            m.BackColor = Color.Black;
            m.Dock = DockStyle.Fill;
            m.Size = new Size(0, 50);
            Panel n = new Panel();
            n.Parent = this;
            n.Name = "Links";
            n.BackColor = Color.CadetBlue;
            n.Dock = DockStyle.Bottom;
            n.Size = new Size(0, 50);
        }

        private void addPanel(string Site, string Data)
        {
            // Create Panels
            Panel p = new Panel();
            p.Parent = this.Controls["Main"];
            p.Size = new Size(50,50);
            p.Name = Guid.NewGuid().ToString();
            p.Dock = DockStyle.Top;
            Panel l = new Panel();
            l.Parent = p;
            l.Size = new Size(50, 50);
            l.Name = Guid.NewGuid().ToString();
            l.Dock = DockStyle.Left;
            Panel r = new Panel();
            r.Parent = p;
            r.Size = new Size(50, 50);
            r.Name = Guid.NewGuid().ToString();
            r.Dock = DockStyle.Fill;

            // Create Labels
            Label lblL = new Label();
            lblL.Parent = l;
            lblL.Size = new Size(50, 50);
            lblL.Name = Guid.NewGuid().ToString();
            lblL.Dock = DockStyle.Fill;
            lblL.TextAlign = ContentAlignment.MiddleCenter;
            lblL.Text = Site.Trim();
            lblL.ForeColor = Color.White;
            lblL.BackColor = Color.DarkBlue;
            lblL.BorderStyle = BorderStyle.FixedSingle;
            Label lblR = new Label();
            lblR.Parent = r;
            lblR.Size = new Size(50, 50);
            lblR.Name = Guid.NewGuid().ToString();
            lblR.Dock = DockStyle.Fill;
            lblR.TextAlign = ContentAlignment.MiddleCenter;
            lblR.Text = Data.Trim();
            lblR.ForeColor = Color.White;
            lblR.BackColor = Color.Blue;
            lblR.BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used. 
            if (disposing)
                if (components != null)
                    components.Dispose();

            base.Dispose(disposing);
        }

        private void notifyIcon1_MouseUp(object Sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show the form when the user double clicks on the notify icon. 
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    queryDNS();
                }
                else
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                }
            }
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application. 
            this.Close();
        }

        private void menuItem2_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application. 
            queryDNS();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DNSMonitor));
            this.SuspendLayout();
            // 
            // DNSMonitor
            // 
            this.ClientSize = new System.Drawing.Size(284, 112);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DNSMonitor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.ResumeLayout(false);

        }
    }
}