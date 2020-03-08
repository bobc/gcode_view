using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Drawing2D;
using System.IO;
using RMC;



namespace gcode_view
{
    public partial class Form1 : Form
    {
        string Company = "RMC";
        string AppTitle = "Gcode View";
        string AppDescription = "Gcode Thumbnail Viewer";
        string AppVersion = "0.1.0";

        const string Default_filepath = @"C:\";

        AppSettings AppSettings;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/" + Company;
            string Filename = Path.Combine(AppDataFolder, AppTitle + ".config.xml");

            LoadAppSettings(Filename);

            if (string.IsNullOrEmpty(AppSettings.Folder))
            {
                AppSettings.Folder = Default_filepath;
            }

            textBoxFilename.Text = AppSettings.Folder;

            toolStripStatusLabel1.Text = "Nothing selected";

            buttonScan_Click(sender, e);
        }

        public void LoadAppSettings(string filename)
        {
            AppSettingsBase.Filename = filename;

            AppSettings = (AppSettings)AppSettings.LoadFromXmlFile(filename);

            if (AppSettings != null)
            {
                AppSettings.MainForm = this;
                AppSettings.OnLoad();
            }
            else
            {
                AppSettings = new AppSettings(this);
            }
            //
        }

        private void SaveAppSettings()
        {
            AppSettings.OnSaving();
            AppSettings.SaveToXmlFile(AppSettingsBase.Filename);
        }


        private void buttonChooseFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBoxFilename.Text;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                AppSettings.Folder = folderBrowserDialog1.SelectedPath;
                textBoxFilename.Text = AppSettings.Folder;

                SaveAppSettings();

                buttonScan_Click(sender, e);
            }
        }
        private void buttonScan_Click(object sender, EventArgs e)
        {
            try
            {
                scan_folder();
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "Error: " + ex.Message;
            }
        }

        private void buttonGenFiles_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(AppSettings.Folder, "*.gcode");

            foreach (string f in files)
                process_file(f, false, true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                AppDescription + Environment.NewLine +
                "" + Environment.NewLine +
                "Version " + AppVersion + Environment.NewLine +
                "" + Environment.NewLine +
                "Bob Cousins 2020" + Environment.NewLine,
                AppTitle, 
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void textBoxFilename_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                AppSettings.Folder = textBoxFilename.Text;
                SaveAppSettings();
                buttonScan_Click(sender, e);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = listView1.SelectedIndices;

            if (selected.Count > 0)
            {
                string filename = Path.Combine(AppSettings.Folder, listView1.Items[selected[0]].Text + ".gcode");
                try
                {
                    toolStripStatusLabel1.Text = "Loading " + filename;
                    UseWaitCursor = true;
                    Application.DoEvents();
                    process_file(filename, true, false);
                }
                catch (Exception ex)
                {
                    toolStripStatusLabel1.Text = "Error: " + ex.Message;
                }
                finally
                {
                    UseWaitCursor = false;
                }
            }
        }

        void scan_folder()
        {
            string[] files = Directory.GetFiles(AppSettings.Folder, "*.gcode");

            listView1.Clear();

            foreach (string f in files)
                listView1.Items.Add(Path.GetFileNameWithoutExtension(f));

            listView1.View = View.List;

            toolStripStatusLabel1.Text = string.Format("{0} files found", files.Length);
        }


        void process_file(string filename, bool display, bool create_thumb_file)
        {
            string[] lines = File.ReadAllLines(filename);
            string thumb_data = "";
            int index = 0;
            string file_base = Path.GetFileNameWithoutExtension(filename);

            // ; thumbnail begin 220x124 19588

            // ; Jggg ==
            // ; thumbnail end

            bool getting_data = false;
            string temp;
            double max_z = 0;
            string z_line = "Z0 ";
            string support = "Unknown";
            string layer_height = "Unknown";
            string estimated_print_time = "Unknown";

            // ; estimated printing time (normal mode) = 4h 21m 30s
            // ; support_material = 0

            while (index < lines.Length)
            {
                if (lines[index].StartsWith(";"))
                {
                    if (lines[index].Contains("thumbnail begin"))
                    {
                        temp = StringUtils.After(lines[index], "begin ");
                        int dim_x = StringUtils.StringToInteger(StringUtils.Before(temp, "x"));

                        if (dim_x > 16)
                            getting_data = true;
                    }
                    else if (getting_data && lines[index].Contains("thumbnail end"))
                    {
                        getting_data = false;
                    }
                    else if (getting_data)
                    {
                        thumb_data += StringUtils.After(lines[index], "; ");
                    }
                    else if (lines[index].Contains("estimated printing time"))
                    {
                        estimated_print_time = StringUtils.After(lines[index], "= ");
                    }
                    else if (lines[index].Contains(" support_material "))
                    {
                        if (StringUtils.After(lines[index], "= ") == "1")
                            support = "Yes";
                        else
                            support = "No";
                    }
                    else if (lines[index].Contains(" layer_height "))
                    {
                        layer_height = StringUtils.After(lines[index], "= ");
                    }
                }
                else
                {
                    if (lines[index].StartsWith("G1"))
                    {
                        if (lines[index].Contains("Z"))
                        {
                            z_line = lines[index];
                            temp = StringUtils.After(z_line, "Z");
                            temp = StringUtils.Before(temp, " ");
                            double val;
                            if (double.TryParse(temp, out val))
                            {
                                max_z = Math.Max(max_z, val);
                            }
                        }
                    }
                }

                index++;
            }

            byte[] data = System.Convert.FromBase64String(thumb_data);

            if (display)
            {
                if (data.Length > 0)
                {
                    MemoryStream stream = new MemoryStream(data);
                    Image img = Bitmap.FromStream(stream);

                    Bitmap bmp = new Bitmap(220*2, 124*2);
                    Graphics g = Graphics.FromImage(bmp);

                    g.DrawImage(
                        img,
                        // destination rectangle 
                        new Rectangle(0, 0, bmp.Width, bmp.Height),
                        // source
                        0,
                        0,           
                        img.Width,       
                        img.Height,      
                        GraphicsUnit.Pixel);

                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox1.Height = bmp.Height;
                    pictureBox1.Width = bmp.Width;
                    pictureBox1.Image = bmp;
                }
                else
                {
                    Bitmap bmp = new Bitmap(220, 124);
                    Graphics g = Graphics.FromImage(bmp);
                    g.FillRectangle(Brushes.White, 0,0, bmp.Width, bmp.Height);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    Rectangle rect = new Rectangle(0,0,bmp.Width, bmp.Height);
                    g.DrawString("No thumbnail available", new Font("Arial", 10), Brushes.Red, rect, stringFormat);

                    pictureBox1.Height = bmp.Height;
                    pictureBox1.Width = bmp.Width;
                    pictureBox1.Image = bmp;
                }

                toolStripStatusLabel1.Text = filename;

                textBoxInfo.Clear();
                textBoxInfo.AppendText(string.Format("Height  = {0}", max_z) + Environment.NewLine);
                textBoxInfo.AppendText(string.Format("Support = {0}", support) + Environment.NewLine);
                textBoxInfo.AppendText(string.Format("Layer height = {0}", layer_height) + Environment.NewLine);
                textBoxInfo.AppendText(string.Format("Estimated print time = {0}", estimated_print_time) + Environment.NewLine);
            }

            if (create_thumb_file)
            {
                filename = Path.ChangeExtension(filename, ".png");
                File.WriteAllBytes(filename, data);
            }
        }
    }
}
