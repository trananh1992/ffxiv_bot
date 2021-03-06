﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FFXIV_netBot.Module;

using System.IO;
using System.Text.RegularExpressions;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace FFXIV_netBot.Forms
{
    public partial class SaveConfigForm : Form
    {
        private Controls.ImageButton closeButton;
        private SaveConfigDelegate saveFinishDelegate;
        private string defFileName;

        private String currentDirectory;

        private SaveConfigHolder configHolder;

        public SaveConfigForm(SaveConfigHolder configHolder, SaveConfigDelegate saveFinishDelegate)
        {
            this.currentDirectory = Path.GetDirectoryName(Application.ExecutablePath);

            this.configHolder = configHolder;
            this.saveFinishDelegate = saveFinishDelegate;
            this.defFileName = configHolder.fileName;

            this.TopMost = true;
            InitializeComponent();

            this.closeButton = new Controls.ImageButton();
            this.closeButton.Parent = this;
            this.closeButton.ForeColor = Color.Transparent;
            this.closeButton.BackgroundImage = global::FFXIV_netBot.Properties.Resources.closeBtn;
            this.closeButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.closeButton.Location = new System.Drawing.Point(220, 11);
            this.closeButton.Name = "buttonClose";
            this.closeButton.Size = new System.Drawing.Size(19, 18);
            this.closeButton.TabStop = false;
            this.closeButton.Click += new System.EventHandler(this.CancelButton_Click);
            this.Controls.Add(this.closeButton);

            this.scanDirectoryForConfig();
            String item = (string)this.existFileSelector.SelectedItem;
            this.saveButton.Enabled = (configHolder.type != ConfigType.None);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                                         Color.FromArgb(1, 86, 77, 52), 2, ButtonBorderStyle.Outset,
                                         Color.FromArgb(1, 86, 77, 52), 2, ButtonBorderStyle.Outset,
                                         Color.FromArgb(1, 86, 77, 52), 2, ButtonBorderStyle.Inset,
                                         Color.FromArgb(1, 86, 77, 52), 2, ButtonBorderStyle.Inset);
        }



        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x84:
                    base.WndProc(ref m);
                    if ((int)m.Result == 0x1)
                        m.Result = (IntPtr)0x2;
                    return;
            }

            base.WndProc(ref m);
        }

        private const int CS_DROPSHADOW = 0x00020000;
        protected override CreateParams CreateParams
        {
            get
            {
                // add the drop shadow flag for automatically drawing
                // a drop shadow around the form
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void SaveConfigForm_Load(object sender, EventArgs e)
        {
        }

        private void scanDirectoryForConfig()
        {
            string[] wprs = Directory.GetFiles(this.currentDirectory, "*.cfg");
            if (wprs.Length > 0)
            {
                string[] fileName = new string[wprs.Length + 1];
                fileName[0] = "";
                for (int i = 0; i < wprs.Length; ++i)
                {
                    fileName[i + 1] = Path.GetFileNameWithoutExtension(wprs[i]);
                }
                this.existFileSelector.DataSource = fileName;
                if (this.defFileName != null && fileName.Contains(this.defFileName))
                {
                    this.existFileSelector.SelectedItem = this.defFileName;
                }
                this.existFileSelector.Enabled = true;
            }
            else
            {
                this.existFileSelector.DataSource = wprs;
                this.existFileSelector.Enabled = false;
            }
        }

        private void existFileSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            String selectedFilename = (string)this.existFileSelector.SelectedItem;
            if (selectedFilename.Length > 0)
            {
                this.FileName.Text = "";
            }
        }

        private void FileName_KeyPress(object sender, EventArgs e)
        {
            this.existFileSelector.SelectedItem = "";
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            bool successSave = false;
            String fileName = this.FileName.Text;
            if (fileName.Length == 0)
            {
                fileName = (string)this.existFileSelector.SelectedItem;
            }

            if (fileName != null && fileName.Length > 0)
            {
                try
                {
                    string regexSearch = new string(Path.GetInvalidPathChars());
                    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                    fileName = r.Replace(fileName, "");

                    String fullPath = Path.Combine(this.currentDirectory, fileName);
                    fullPath = Path.ChangeExtension(fullPath, "cfg");

                    using (FileStream fs = new FileStream(fullPath, FileMode.Create))
                    {
                        using (BinaryWriter binWriter = new BinaryWriter(fs))
                        {
                            byte[] itemByte = this.configHolder.currentConfigToByte();
                            if(itemByte.Length > 0)
                            {
                                binWriter.Write(itemByte);
                                successSave = true;
                            }
                            binWriter.Close();
                        }
                        fs.Close();
                    }
                }
                catch
                {
                    successSave = false;
                    MessageBox.Show("Unknown Error\n\nabort loading", "Loading Error");
                }
            }

            if (successSave)
            {
                this.saveFinishDelegate(fileName);
                this.Close();
            }
            else
            {
                this.scanDirectoryForConfig();
            } 

        }
    }
}
