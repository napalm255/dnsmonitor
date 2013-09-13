using System;
using System.Configuration;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;


namespace DNSMonitor
{
    public class DNSMonitor : System.Windows.Forms.Form
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenu contextMenu;
        private System.Windows.Forms.MenuItem menuItemExit;
        private System.Windows.Forms.MenuItem menuItemRefresh;
        private System.Windows.Forms.MenuItem menuItemSettings;
        private System.Windows.Forms.MenuItem menuItemAutoRefresh;
        private System.ComponentModel.IContainer components;
        private Timer refreshTimer = new Timer();

        private Dictionary<string, List<string>> dnsMatches = new Dictionary<string, List<string>>();
        private List<string> dnsServers = new List<string>();
        private List<string> dnsDomains = new List<string>();

        [STAThread]
        static void Main()
        {
            Application.Run(new DNSMonitor());
        }

        public DNSMonitor()
        {
            // Initialize Notification Form
            InitializeComponent();
            
            // Initialize Notification Form Layout
            initLayout();
            //initLocation();
            settingsLoad();
            
            // Initialize Components
            this.Resize += new EventHandler(DNSMonitor_Resize);
            this.components = new System.ComponentModel.Container();
            this.contextMenu = new System.Windows.Forms.ContextMenu();
            this.menuItemExit = new System.Windows.Forms.MenuItem();
            this.menuItemRefresh = new System.Windows.Forms.MenuItem();
            this.menuItemSettings = new System.Windows.Forms.MenuItem();
            this.menuItemAutoRefresh = new System.Windows.Forms.MenuItem();

            // Create refreshTimer Tick Event
            refreshTimer.Tick += new EventHandler(refreshTimer_Tick);
            
            // Initialize contextMenu1 
            this.contextMenu.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { this.menuItemExit, this.menuItemRefresh, this.menuItemSettings, this.menuItemAutoRefresh });

            // Initialize menuItem1 
            this.menuItemExit.Index = 0;
            this.menuItemExit.Text = "E&xit";
            this.menuItemExit.Click += new System.EventHandler(this.exitDisplay_Click);

            // Initialize menuItem2 
            this.menuItemRefresh.Index = 0;
            this.menuItemRefresh.Text = "&Refresh";
            this.menuItemRefresh.Click += new System.EventHandler(this.refreshDisplay_Click);

            // Initialize menuItem3
            this.menuItemSettings.Index = 0;
            this.menuItemSettings.Text = "&Settings";
            this.menuItemSettings.Click += new System.EventHandler(this.settingsDisplay_Click);

            // Initialize menuItem3
            this.menuItemAutoRefresh.Index = 0;
            this.menuItemAutoRefresh.Text = "&Auto-Refresh";
            this.menuItemAutoRefresh.Click += new System.EventHandler(this.autoRefresh_Click);

            // Create the NotifyIcon. 
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);

            // The Icon property sets the icon that will appear 
            // in the systray for this application.
            notifyIcon.Icon = new Icon(GetType(), "activity_monitor_homer.ico");

            // The ContextMenu property sets the menu that will 
            // appear when the systray icon is right clicked.
            notifyIcon.ContextMenu = this.contextMenu;

            // The Text property sets the text that will be displayed, 
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon.Text = "DNS Monitor v" + this.ProductVersion.ToString();
            notifyIcon.Visible = true;

            // Handle the DoubleClick event to activate the form.
            notifyIcon.MouseUp += new MouseEventHandler(this.notifyIcon_MouseUp);

            queryDNS();
            this.Hide();
        }

        void DNSMonitor_Resize(object sender, EventArgs e)
        {
            initLocation();
        }

        public void queryDNS()
        {
            // Refresh Config
            loadDNSConfig();
            
            // Clear Panels
            this.Controls["Main"].Controls.Clear();
            try
            {
                foreach (string dom in dnsDomains)
                {
                    if (dom == "") break;
                    List<string> ipResults = new List<string>();
                    foreach (string srv in dnsServers)
                    {
                        var Options = new JHSoftware.DnsClient.RequestOptions();
                        Options.RetryCount = 1;
                        Options.TimeOut = new TimeSpan(0, 0, 1);
                        Options.DnsServers = new System.Net.IPAddress[] { System.Net.IPAddress.Parse(srv.Trim()) };
                        try
                        {
                            var IPs = JHSoftware.DnsClient.LookupHost(dom.Trim(), JHSoftware.DnsClient.IPVersion.IPv4, Options);
                            foreach (var IP in IPs)
                            {
                                ipResults.Add(IP.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            //ipResults.Add(e.ToString());
                        }
                    }

                    string matchedLocation = "";
                    string matchedIP = "";
                    var uniqueIPs = new HashSet<string>(ipResults);
                    foreach (string s in uniqueIPs)
                    {
                        matchedIP = s;
                        foreach (KeyValuePair<string, List<string>> match in dnsMatches)
                        {
                            if (match.Value.Contains(s))
                            {
                                matchedLocation = match.Key;
                            }
                        }
                    }

                    Match matchKeyVal = Regex.Match(matchedLocation, @"^(.*)\:(.*)$");
                    string matchLocation = matchKeyVal.Groups[1].Value;
                    string matchColor = matchKeyVal.Groups[2].Value;

                    addPanel(matchLocation, dom, matchedIP, ipResults, matchColor);
                }
            }
            catch
            {
            }
            
        }

        public bool loadDNSConfig()
        {
            // Load Domain Names
            dnsDomains.Clear();
            foreach (string domain in Settings.Default.domainNames.Split(','))
            {
                dnsDomains.Add(domain.Trim());
            }

            // Load DNS Servers
            dnsServers.Clear();
            foreach (string server in Settings.Default.dnsServers.Split(','))
            {
                dnsServers.Add(server.Trim());
            }

            // Load DNS Matches
            dnsMatches.Clear();
            foreach (string match in Settings.Default.dnsMatches.Split(','))
            {
                Match KeyVal = Regex.Match(match, @"^(.*)\:([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})(.*)$");
                string Key = KeyVal.Groups[1].Value.Trim();
                string Val = KeyVal.Groups[2].Value.Trim();
                string mColor = KeyVal.Groups[3].Value.Trim();
                mColor = mColor.Replace(":", "");
                string KC = Key + ":" + mColor;

                if (!dnsMatches.ContainsKey(KC))
                    dnsMatches.Add(KC, new List<string>());

                dnsMatches[KC].Add(Val);
            }

            return true;
        }

        private void initLocation()
        {
            this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width - 15;
            this.Top = Screen.PrimaryScreen.WorkingArea.Height - this.Height - 10;
        }

        private void initLayout()
        {
            // Create Main Panel
            Panel m = new Panel();
            m.Parent = this;
            m.Name = "Main";
            m.BackColor = Color.White;
            m.Dock = DockStyle.Fill;
            m.Size = new Size(0, 50);
            m.AutoSize = true;
            m.Resize += new EventHandler(m_Resize);

            // Create Settings Panel
            Panel s = new Panel();
            s.Parent = this;
            s.Name = "Settings";
            s.BackColor = Color.White;
            s.Dock = DockStyle.Fill;
            s.Size = new Size(0, 40);
            s.AutoSize = true;
            s.Visible = false;
            // Create Auto-Refresh Interval Setting
            Panel set1 = new Panel();
            set1.Parent = s;
            set1.Name = "setting_panel_AutoRefreshInterval";
            set1.Dock = DockStyle.Top;
            set1.Padding = new Padding(10);
            set1.BorderStyle = BorderStyle.FixedSingle;
            set1.AutoSize = true;
            Label lbl1 = new Label();
            lbl1.Parent = set1;
            lbl1.Name = "setting_lbl_AutoRefreshInterval";
            lbl1.AutoSize = true;
            lbl1.TextAlign = ContentAlignment.MiddleLeft;
            lbl1.Dock = DockStyle.Fill;
            lbl1.Text = "Auto Refresh Interval (min)";
            TextBox txt1 = new TextBox();
            txt1.Parent = set1;
            txt1.Name = "setting_txt_AutoRefreshInterval";
            txt1.Dock = DockStyle.Right;
            txt1.Width = 75;
            txt1.TabIndex = 3;

            // Create DNS Server Setting
            Panel set2 = new Panel();
            set2.Parent = s;
            set2.Name = "setting_panel_dnsServer";
            set2.Dock = DockStyle.Top;
            set2.Padding = new Padding(10);
            set2.BorderStyle = BorderStyle.FixedSingle;
            set2.AutoSize = true;
            Label lbl2 = new Label();
            lbl2.Parent = set2;
            lbl2.Name = "setting_lbl_dnsServer";
            lbl2.AutoSize = true;
            lbl2.TextAlign = ContentAlignment.MiddleLeft;
            lbl2.Dock = DockStyle.Top;
            lbl2.Text = "DNS Servers";
            TextBox txt2 = new TextBox();
            txt2.Parent = set2;
            txt2.Name = "setting_txt_dnsServer";
            txt2.Dock = DockStyle.Bottom;
            txt2.TabIndex = 2;

            // Create Domain Names Setting
            Panel set3 = new Panel();
            set3.Parent = s;
            set3.Name = "setting_panel_DomainNames";
            set3.Dock = DockStyle.Top;
            set3.Padding = new Padding(10);
            set3.BorderStyle = BorderStyle.FixedSingle;
            set3.AutoSize = true;
            Label lbl3 = new Label();
            lbl3.Parent = set3;
            lbl3.Name = "setting_lbl_DomainNames";
            lbl3.AutoSize = true;
            lbl3.TextAlign = ContentAlignment.MiddleLeft;
            lbl3.Dock = DockStyle.Top;
            lbl3.Text = "Domain Names";
            TextBox txt3 = new TextBox();
            txt3.Parent = set3;
            txt3.Name = "setting_txt_DomainNames";
            txt3.Dock = DockStyle.Bottom;
            txt3.TabIndex = 1;

            // Create DNS Matches Setting
            Panel set4 = new Panel();
            set4.Parent = s;
            set4.Name = "setting_panel_dnsMatches";
            set4.Dock = DockStyle.Top;
            set4.Padding = new Padding(10);
            set4.BorderStyle = BorderStyle.FixedSingle;
            set4.AutoSize = true;
            Label lbl4 = new Label();
            lbl4.Parent = set4;
            lbl4.Name = "setting_lbl_dnsMatches";
            lbl4.AutoSize = true;
            lbl4.TextAlign = ContentAlignment.MiddleLeft;
            lbl4.Dock = DockStyle.Top;
            lbl4.Text = "DNS Matches";
            TextBox txt4 = new TextBox();
            txt4.Parent = set4;
            txt4.Name = "setting_txt_dnsMatches";
            txt4.Dock = DockStyle.Bottom;
            txt4.TabIndex = 0;


            /* ListBox Edit Method -- Work-in-Progress
            ListBox lst2 = new ListBox();
            ContextMenu lst2Context = new ContextMenu();
            lst2.Parent = set2;
            lst2.Name = "setting_lst_DNSServer";
            lst2.Size = new Size(0, 50);
            lst2.ScrollAlwaysVisible = true;
            lst2.Dock = DockStyle.Bottom;
            lst2.ContextMenu = lst2Context;
            MenuItem lst2Add = new MenuItem();
            lst2Add.Index = 0;
            lst2Add.Text = "Add";
            lst2Add.Click += new EventHandler(lst2Add_Click);
            MenuItem lst2Delete = new MenuItem();
            lst2Delete.Index = 0;
            lst2Delete.Text = "Delete";
            lst2Delete.Click += new EventHandler(lst2Delete_Click);
            lst2Context.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { lst2Add, lst2Delete });
            */

            // Create Links Panel
            Panel n = new Panel();
            n.Parent = this;
            n.Name = "Links";
            n.BackColor = Color.DarkGray;
            n.BorderStyle = BorderStyle.FixedSingle;
            n.Dock = DockStyle.Bottom;
            n.Size = new Size(0, 50);
            PictureBox picSettings = new PictureBox();
            picSettings.Parent = n;
            picSettings.Name = "SettingsButton";
            picSettings.Cursor = Cursors.Hand;
            picSettings.Dock = DockStyle.Left;
            picSettings.SizeMode = PictureBoxSizeMode.CenterImage;
            picSettings.Image = new Bitmap(GetType(), "img_settings.png");
            picSettings.Click += new System.EventHandler(this.settingsDisplay_Click);
            PictureBox picRefresh = new PictureBox();
            picRefresh.Parent = n;
            picRefresh.Name = "RefreshButton";
            picRefresh.Cursor = Cursors.Hand;
            picRefresh.Dock = DockStyle.Right;
            picRefresh.SizeMode = PictureBoxSizeMode.CenterImage;
            picRefresh.Image = new Bitmap(GetType(), "img_refresh.png");
            picRefresh.Click += new System.EventHandler(this.refreshDisplay_Click);
        }

        void lst2Delete_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void lst2Add_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void m_Resize(object sender, EventArgs e)
        {
            //initLocation();
        }

        private void addPanel(string matchLocation, string Domain, string MatchedIP, List<string> AllIPs, string matchColor)
        {
            
            // Color Breakdown
            string foreColor = "";
            string backColor = "";
            Color fColor = new Color();
            Color bColor = new Color();
            Match matchKeyVal = Regex.Match(matchColor, @"^\((.*)\|(.*)\)$");
            if (matchKeyVal.Groups.Count >= 2)
            {
                foreColor = matchKeyVal.Groups[1].Value;
                fColor = (Color)TypeDescriptor.GetConverter(typeof(Color)).ConvertFromString("#" + foreColor);
                backColor = matchKeyVal.Groups[2].Value;
                bColor = (Color)TypeDescriptor.GetConverter(typeof(Color)).ConvertFromString("#" + backColor);
            }

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
            lblL.Text = matchLocation.Trim();
            lblL.ForeColor = Color.Black;
            if (foreColor.Length > 0 ) { lblL.ForeColor = fColor; }
            lblL.BackColor = Color.White;
            if (backColor.Length > 0 ) { lblL.BackColor = bColor; }
            lblL.BorderStyle = BorderStyle.FixedSingle;
            Label lblR = new Label();
            lblR.Parent = r;
            lblR.Size = new Size(50, 50);
            lblR.Name = Guid.NewGuid().ToString();
            lblR.Dock = DockStyle.Fill;
            lblR.TextAlign = ContentAlignment.MiddleCenter;
            lblR.Text = Domain.Trim().ToLower() + "\n" + MatchedIP.Trim();
            lblR.ForeColor = Color.Black;
            if (foreColor.Length > 0) { lblR.ForeColor = fColor; }
            lblR.BackColor = Color.White;
            if (backColor.Length > 0) { lblR.BackColor = bColor; }
            lblR.BorderStyle = BorderStyle.FixedSingle;

            // Create ToolTip
            ToolTip lbl_Tip = new ToolTip();
            lbl_Tip.IsBalloon = true;
            lbl_Tip.ToolTipIcon = ToolTipIcon.Info;
            lbl_Tip.ToolTipTitle = Domain.Trim().ToLower();
            string ToolTipText = "Last Query Time: \n\t" + DateTime.Now.ToString() + "\n";
            ToolTipText += "Location: \n\t" + matchLocation.ToUpper().Trim() + "\n";
            ToolTipText += "Query Results: \n\t" + string.Join<string>("\n\t", AllIPs.ToArray()); 
            lbl_Tip.SetToolTip(lblR, ToolTipText);
            lbl_Tip.SetToolTip(lblL, ToolTipText);

        }

        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used. 
            if (disposing)
                if (components != null)
                    components.Dispose();

            base.Dispose(disposing);
        }

        private void notifyIcon_MouseUp(object Sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show the form when the user double clicks on the notify icon. 
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    initLocation();
                    queryDNS();
                }
                else
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                }
            }
        }

        private void exitDisplay_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application. 
            this.Close();
        }

        private void settingsLoad()
        {
            Settings.Default.Reload();
            this.Controls["Settings"].Controls["setting_panel_AutoRefreshInterval"].Controls["setting_txt_AutoRefreshInterval"].Text = Settings.Default.autoRefreshInterval.ToString();

            this.Controls["Settings"].Controls["setting_panel_dnsServer"].Controls["setting_txt_dnsServer"].Text = Settings.Default.dnsServers;

            this.Controls["Settings"].Controls["setting_panel_DomainNames"].Controls["setting_txt_DomainNames"].Text = Settings.Default.domainNames;

            this.Controls["Settings"].Controls["setting_panel_dnsMatches"].Controls["setting_txt_dnsMatches"].Text = Settings.Default.dnsMatches;  
            
        }

        private void settingsUpdate()
        {
            // Auto-Refresh Interval
            int settingAutoRefreshInterval = Convert.ToInt32(this.Controls["Settings"].Controls["setting_panel_AutoRefreshInterval"].Controls["setting_txt_AutoRefreshInterval"].Text.Trim());
            Settings.Default.autoRefreshInterval = settingAutoRefreshInterval;
            refreshTimer.Interval = Settings.Default.autoRefreshInterval * 60000;

            // DNS Servers
            string dnsServers = this.Controls["Settings"].Controls["setting_panel_dnsServer"].Controls["setting_txt_dnsServer"].Text;
            Settings.Default.dnsServers = dnsServers.Trim();

            // Domain Names
            string domainNames = this.Controls["Settings"].Controls["setting_panel_DomainNames"].Controls["setting_txt_DomainNames"].Text;
            Settings.Default.domainNames = domainNames.Trim();

            // Match Locations
            string matchLocations = this.Controls["Settings"].Controls["setting_panel_dnsMatches"].Controls["setting_txt_dnsMatches"].Text;
            Settings.Default.dnsMatches = matchLocations.Trim();

            // Save Settings
            Settings.Default.Save();
            
        }

        private void settingsDisplay_Click(object Sender, EventArgs e)
        {
            // Flip Between Main and Settings
            if (this.Controls["Main"].Visible)
            {
                // Open Settings Panel
                this.Controls["Main"].Visible = false;
                this.Controls["Settings"].Visible = true;
                this.Controls["Links"].Controls["RefreshButton"].Visible = false;
                settingsLoad();
            }
            else if (this.Controls["Settings"].Visible)
            {
                // Open Main Panel
                this.Controls["Main"].Visible = true;
                this.Controls["Settings"].Visible = false;
                this.Controls["Links"].Controls["RefreshButton"].Visible = true;
                settingsUpdate();
                queryDNS();
            }
        }

        private void refreshDisplay_Click(object Sender, EventArgs e)
        {
            // Refresh the display
            queryDNS();
        }

        private void autoRefresh_Click(object Sender, EventArgs e)
        {
            // Auto-Refresh
            if (menuItemAutoRefresh.Checked)
            {
                menuItemAutoRefresh.Checked = false;
                refreshTimer.Enabled = false;
                refreshTimer.Stop();
            }
            else if (!menuItemAutoRefresh.Checked)
            {
                menuItemAutoRefresh.Checked = true;
                refreshTimer.Interval = Settings.Default.autoRefreshInterval * 1000;
                refreshTimer.Enabled = true;
                refreshTimer.Start();
            }            
        }

        void refreshTimer_Tick(object sender, EventArgs e)
        {
            queryDNS();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DNSMonitor));
            this.SuspendLayout();
            // 
            // DNSMonitor
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(274, 84);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(290, 650);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(290, 100);
            this.Name = "DNSMonitor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.ResumeLayout(false);

        }
    }
}