﻿using client.Classes;
using client.Forms;
using client.User_controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace client
{

    public partial class frmMain : Form
    {
        // P/Invoke declarations for Windows 11 taskbar preview fix
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_DISALLOW_PEEK = 11;
        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;

        // Allow doubleBuffering drawing each frame to memory and then onto screen
        // Solves flickering issues mostly as the entire rendering of the screen is done in 1 operation after being first loaded to memory
        protected override CreateParams CreateParams

        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        public Category ThisCategory;
        public List<ucShortcut> ControlList;
        public Color HoverColor;
        public static uint eDpi { get; set; } // Effective DPI
        public static decimal xDpi { get; set; } // eDpi ratio for HDPi conversions

        private string passedDirec;
        public Point mouseClick;

        public int ucShortcutHeight;
        public int ucShortcutWidth;
        public int iconSize = 64; // Base icon size in pixels (configurable in future)

        //------------------------------------------------------------------------------------
        // CTOR AND LOAD
        //
        public frmMain(string passedDirectory, int cursorPosX, int cursorPosY)
        {
            InitializeComponent();
            eDpi = Display(DpiType.Effective); // Get Dpi from clicked screen
            xDpi = eDpi / 96; // Get eDpi conversion ratio
            System.Runtime.ProfileOptimization.StartProfile("frmMain.Profile");
            mouseClick = new Point(Cursor.Position.X, Cursor.Position.Y); // Construct point p based on passed x y mouse values
            passedDirec = passedDirectory;
            FormBorderStyle = FormBorderStyle.None;
            
            // Windows 11 taskbar preview fix
            this.ShowInTaskbar = false;
            this.TopMost = true;

            using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(MainPath.path + "\\config\\" + passedDirec + "\\GroupIcon.ico")))
                this.Icon = new Icon(ms);

            if (Directory.Exists(@MainPath.path + @"\config\" + passedDirec))
            {
                ControlList = new List<ucShortcut>();

                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                ThisCategory = new Category($"config\\{passedDirec}");
                this.BackColor = ImageFunctions.FromString(ThisCategory.ColorString);
                Opacity = (1 - (ThisCategory.Opacity / 100));
                
                // Use icon size from category if available
                if (ThisCategory.IconSize > 0)
                    iconSize = ThisCategory.IconSize;

                if (BackColor.R * 0.2126 + BackColor.G * 0.7152 + BackColor.B * 0.0722 > 255 / 2)
                    //if backcolor is light, set hover color as darker
                    HoverColor = Color.FromArgb(BackColor.A, (BackColor.R - 50), (BackColor.G - 50), (BackColor.B - 50));
                else
                    //light backcolor is light, set hover color as darker
                    HoverColor = Color.FromArgb(BackColor.A, (BackColor.R + 50), (BackColor.G + 50), (BackColor.B + 50));
            }
            else
            {
                Application.Exit();
            }
        }

        // eDpi Calculations Below -----------------
        public uint Display(DpiType type)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.Bounds.Contains(mouseClick)) // Get what screen to target eDpi
                {
                    screen.GetDpi(DpiType.Effective, out uint x, out _);
                eDpi = x;
                return (x);
                }
            }
            return (eDpi);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadCategory();
            SetLocation();
            
            // Disable Windows 11 taskbar preview
            int trueValue = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_DISALLOW_PEEK, ref trueValue, sizeof(int));
            DwmSetWindowAttribute(this.Handle, DWMWA_EXCLUDED_FROM_PEEK, ref trueValue, sizeof(int));
        }

        // Sets location of form
        private void SetLocation()
        {
            List<Rectangle> taskbarList = FindDockedTaskBars();
            Rectangle taskbar = new Rectangle();
            Rectangle screen = new Rectangle();

            int i = 0;
            int locationy;
            int locationx;
            if (taskbarList.Count != 0)
            {
                foreach (var scr in Screen.AllScreens) // Get what screen user clicked on
                {
                    if (scr.Bounds.Contains(mouseClick))
                    {
                        screen.X = scr.Bounds.X;
                        screen.Y = scr.Bounds.Y;
                        screen.Width = scr.Bounds.Width;
                        screen.Height = scr.Bounds.Height;
                        taskbar = taskbarList[i];
                    }
                    i++;
                }

                if (taskbar.Contains(mouseClick)) // Click on taskbar
                {
                    if (taskbar.Top == screen.Top && taskbar.Width == screen.Width)
                    {
                        // TOP
                        locationy = screen.Y + taskbar.Height + 10;
                        locationx = mouseClick.X - (this.Width / 2);
                    }
                    else if (taskbar.Bottom == screen.Bottom && taskbar.Width == screen.Width)
                    {
                        // BOTTOM
                        locationy = screen.Y + screen.Height - this.Height - taskbar.Height - 10;
                        locationx = mouseClick.X - (this.Width / 2);
                    }
                    else if (taskbar.Left == screen.Left)
                    {
                        // LEFT
                        locationy = mouseClick.Y - (this.Height / 2);
                        locationx = screen.X + taskbar.Width + 10;

                    }
                    else
                    {
                        // RIGHT
                        locationy = mouseClick.Y - (this.Height / 2);
                        locationx = screen.X + screen.Width - this.Width - taskbar.Width - 10;
                    }

                }
                else // not click on taskbar
                {
                    locationy = mouseClick.Y - this.Height - 20;
                    locationx = mouseClick.X - (this.Width / 2);

                }

                this.Location = new Point(locationx, locationy);

                // If form goes over screen edge
                if (this.Left < screen.Left)
                    this.Left = screen.Left + 10;
                if (this.Top < screen.Top)
                    this.Top = screen.Top + 10;
                if (this.Right > screen.Right)
                    this.Left = screen.Right - this.Width - 10;

                // If form goes over taskbar
                if (taskbar.Contains(this.Left, this.Top) && taskbar.Contains(this.Right, this.Top)) // Top taskbar
                    this.Top = screen.Top + 10 + taskbar.Height;
                if (taskbar.Contains(this.Left, this.Top)) // Left taskbar
                    this.Left = screen.Left + 10 + taskbar.Width;
                if (taskbar.Contains(this.Right, this.Top))  // Right taskbar
                    this.Left = screen.Right - this.Width - 10 - taskbar.Width;

            }
            else // Hidded taskbar
            {
                foreach (var scr in Screen.AllScreens) // get what screen user clicked on
                {
                    if (scr.Bounds.Contains(mouseClick))
                    {
                        screen.X = scr.Bounds.X;
                        screen.Y = scr.Bounds.Y;
                        screen.Width = scr.Bounds.Width;
                        screen.Height = scr.Bounds.Height;
                    }
                    i++;
                }

                if (mouseClick.Y > Screen.PrimaryScreen.Bounds.Height - 35)
                    locationy = Screen.PrimaryScreen.Bounds.Height - this.Height - 45;
                else
                    locationy = mouseClick.Y - this.Height - 20;
                locationx = mouseClick.X - (this.Width / 2);

                this.Location = new Point(locationx, locationy);

                // If form goes over screen edge
                if (this.Left < screen.Left)
                    this.Left = screen.Left + 10;
                if (this.Top < screen.Top)
                    this.Top = screen.Top + 10;
                if (this.Right > screen.Right)
                    this.Left = screen.Right - this.Width - 10;

                // If form goes over taskbar
                if (taskbar.Contains(this.Left, this.Top) && taskbar.Contains(this.Right, this.Top)) // Top taskbar
                    this.Top = screen.Top + 10 + taskbar.Height;
                if (taskbar.Contains(this.Left, this.Top)) // Left taskbar
                    this.Left = screen.Left + 10 + taskbar.Width;
                if (taskbar.Contains(this.Right, this.Top))  // Right taskbar
                    this.Left = screen.Right - this.Width - 10 - taskbar.Width;
            }
        }
        // Search for active taskbars on screen
        public static List<Rectangle> FindDockedTaskBars()
        {
            List<Rectangle> dockedRects = new List<Rectangle>();
            foreach (var tmpScrn in Screen.AllScreens)
            {
                if (!tmpScrn.Bounds.Equals(tmpScrn.WorkingArea))
                {
                    Rectangle rect = new Rectangle();

                    var leftDockedWidth = Math.Abs((Math.Abs(tmpScrn.Bounds.Left) - Math.Abs(tmpScrn.WorkingArea.Left)));
                    var topDockedHeight = Math.Abs((Math.Abs(tmpScrn.Bounds.Top) - Math.Abs(tmpScrn.WorkingArea.Top)));
                    var rightDockedWidth = ((tmpScrn.Bounds.Width - leftDockedWidth) - tmpScrn.WorkingArea.Width);
                    var bottomDockedHeight = ((tmpScrn.Bounds.Height - topDockedHeight) - tmpScrn.WorkingArea.Height);
                    if ((leftDockedWidth > 0))
                    {
                        rect.X = tmpScrn.Bounds.Left;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = leftDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if ((rightDockedWidth > 0))
                    {
                        rect.X = tmpScrn.WorkingArea.Right;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = rightDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if ((topDockedHeight > 0))
                    {
                        rect.X = tmpScrn.WorkingArea.Left;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = topDockedHeight;
                    }
                    else if ((bottomDockedHeight > 0))
                    {
                        rect.X = tmpScrn.WorkingArea.Left;
                        rect.Y = tmpScrn.WorkingArea.Bottom;
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = bottomDockedHeight;
                    }
                    else
                    {
                        // Nothing found!
                    }

                    dockedRects.Add(rect);
                }
            }

            if (dockedRects.Count == 0)
            {
                // Taskbar is set to "Auto-Hide".
            }

            return dockedRects;
        }
        //
        //------------------------------------------------------------------------------------
        //

        // Loading category and building shortcuts
        private void LoadCategory()
        {
            //System.Diagnostics.Debugger.Launch();

            // Calculate control size based on icon size with padding
            // Add padding around the icon (about 20% on each side)
            int scaledIconSize = (int)(iconSize * xDpi);
            int padding = (int)(scaledIconSize * 0.2); // 20% padding
            
            // Set the size of each shortcut control based on icon + padding
            ucShortcutHeight = scaledIconSize + (padding * 2);
            ucShortcutWidth = scaledIconSize + (padding * 2);
            
            // Start with zero width and height
            this.Width = 0;
            this.Height = ucShortcutHeight;

            int x = 0;
            int y = 0;
            int width = ThisCategory.Width;  
            int columns = 1;

            // Check if icon caches exist for the category being loaded
            // If not then rebuild the icon cache
            if (!Directory.Exists(@MainPath.path + @"\config\" + ThisCategory.Name + @"\Icons\"))
            {
                ThisCategory.cacheIcons();
            }

            foreach (ProgramShortcut psc in ThisCategory.ShortcutList)
            {

                if (columns > width)  // creating new row if there are more psc than max width
                {
                    x = 0;
                    y += ucShortcutHeight;
                    this.Height += ucShortcutHeight;
                    columns = 1;
                }

                // Expand form width only as needed for current row
                if (x + ucShortcutWidth > this.Width)
                    this.Width = x + ucShortcutWidth;

                // OLD
                //BuildShortcutPanel(x, y, psc);

                // Building shortcut controls
                ucShortcut pscPanel = new ucShortcut() 
                {
                    Psc = psc, 
                    MotherForm = this, 
                    ThisCategory = ThisCategory 
                };
                pscPanel.Location = new System.Drawing.Point(x, y);
                this.Controls.Add(pscPanel);
                this.ControlList.Add(pscPanel);
                pscPanel.Show();
                pscPanel.BringToFront();

                // Reset values
                x += ucShortcutWidth;
                columns++;
            }
        }

        // OLD (Having some issues with the uc build, so keeping the old code below)
        private void BuildShortcutPanel(int x, int y, ProgramShortcut psc)
        {
            this.shortcutPic = new System.Windows.Forms.PictureBox();
            this.shortcutPic.BackColor = System.Drawing.Color.Transparent;
            this.shortcutPic.Location = new System.Drawing.Point(25, 15);
            this.shortcutPic.Size = new System.Drawing.Size(25, 25);
            this.shortcutPic.BackgroundImage = ThisCategory.loadImageCache(psc); // Use the local icon cache for the file specified as the icon image
            this.shortcutPic.BackgroundImageLayout = ImageLayout.Stretch;
            this.shortcutPic.TabStop = false;
            this.shortcutPic.Click += new System.EventHandler((sender, e) => OpenFile(psc.Arguments, psc.FilePath, psc.WorkingDirectory));
            this.shortcutPic.Cursor = System.Windows.Forms.Cursors.Hand;
            this.shortcutPanel.Controls.Add(this.shortcutPic);
            this.shortcutPic.Show();
            this.shortcutPic.BringToFront();
            this.shortcutPic.MouseEnter += new System.EventHandler((sender, e) => this.shortcutPanel.BackColor = Color.Black);
            this.shortcutPic.MouseLeave += new System.EventHandler((sender, e) => this.shortcutPanel.BackColor = System.Drawing.Color.Transparent);
        }

        // Click handler for shortcuts
        public void OpenFile(string arguments, string path, string workingDirec)
        {
            // starting program from psc panel click
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.Arguments = arguments;
            proc.FileName = path;
            proc.WorkingDirectory = workingDirec;

            /*
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = path;
            */

            try
            {
                Process.Start(proc);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        // Closes application upon deactivation
        private void frmMain_Deactivate(object sender, EventArgs e)
        {
            // closes program if user clicks outside form
            // Add a small delay to prevent closing when Windows 11 shows taskbar preview
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!this.ContainsFocus && !this.Bounds.Contains(Cursor.Position))
                {
                    this.Close();
                }
            };
            timer.Start();
        }

        // Keyboard shortcut handlers
        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {

            try
            {
                switch (e.KeyCode)
                {

                    case Keys.D1:
                        ControlList[0].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D2:
                        ControlList[1].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D3:
                        ControlList[2].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D4:
                        ControlList[3].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D5:
                        ControlList[4].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D6:
                        ControlList[5].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D7:
                        ControlList[6].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D8:
                        ControlList[7].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D9:
                        ControlList[8].ucShortcut_MouseEnter(sender, e);
                        break;
                    case Keys.D0:
                        ControlList[9].ucShortcut_MouseEnter(sender, e);
                        break;
                }
            }
            catch
            {

            }
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch();
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter && ThisCategory.allowOpenAll)
            {
                foreach (ucShortcut usc in this.ControlList)
                    usc.ucShortcut_Click(sender, e);
            }

            try
            {
                switch (e.KeyCode)
                {
                    case Keys.D1:
                        ControlList[0].ucShortcut_MouseLeave(sender, e);
                        ControlList[0].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D2:
                        ControlList[1].ucShortcut_MouseLeave(sender, e);
                        ControlList[1].ucShortcut_Click(sender, e);

                        break;
                    case Keys.D3:
                        ControlList[2].ucShortcut_MouseLeave(sender, e);
                        ControlList[2].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D4:
                        ControlList[3].ucShortcut_MouseLeave(sender, e);
                        ControlList[3].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D5:
                        ControlList[4].ucShortcut_MouseLeave(sender, e);
                        ControlList[4].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D6:
                        ControlList[5].ucShortcut_MouseLeave(sender, e);
                        ControlList[5].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D7:
                        ControlList[6].ucShortcut_MouseLeave(sender, e);
                        ControlList[6].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D8:
                        ControlList[7].ucShortcut_MouseLeave(sender, e);
                        ControlList[7].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D9:
                        ControlList[8].ucShortcut_MouseLeave(sender, e);
                        ControlList[8].ucShortcut_Click(sender, e);
                        break;
                    case Keys.D0:
                        ControlList[9].ucShortcut_MouseLeave(sender, e);
                        ControlList[9].ucShortcut_Click(sender, e);
                        break;
                }
            }
            catch
            {

            }
        }

        //
        // endregion
        //
        public System.Windows.Forms.PictureBox shortcutPic;
        public System.Windows.Forms.Panel shortcutPanel;
        //
        // END OF CLASS
        //
    }
}
