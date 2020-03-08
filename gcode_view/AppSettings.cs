using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using RMC;


namespace gcode_view
{

    public class AppSettings
    {
        public Point MainPos;
        public Size MainSize;

        public int Split1Distance;
        public int Split2Distance;

        public Size LeftPanel;

        public string Folder;

        public MRUList RecentlyOpenedFiles;

        [XmlIgnore]
        public Form1 MainForm;

        public AppSettings()
        {
            RecentlyOpenedFiles = new MRUList();
        }

        public AppSettings(Form1 MainForm)
        {
            this.MainForm = MainForm;

            RecentlyOpenedFiles = new MRUList();

            OnSaving();
        }

        public void OnLoad()
        {
            MainForm.Location = MainPos;
            MainForm.Width = MainSize.Width;
            MainForm.Height = MainSize.Height;

            MainForm.splitContainer1.SplitterDistance = LeftPanel.Width;
        }

        public void OnSaving()
        {
            if (MainForm.WindowState != System.Windows.Forms.FormWindowState.Minimized)
            {
                MainPos = MainForm.Location;
                MainSize = new Size(MainForm.Width, MainForm.Height);
                LeftPanel.Width = MainForm.splitContainer1.Panel1.Width;
            }
        }


        public static AppSettings LoadFromXmlFile(string FileName)
        {
            AppSettings result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));

            if (!File.Exists(FileName))
                return result;

            FileStream fs = new FileStream(FileName, FileMode.Open);

            try
            {
                result = (AppSettings)serializer.Deserialize(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return result;
        }

        public bool SaveToXmlFile(string FileName)
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
            TextWriter Writer = null;

            AppSettingsBase.CreateDirectory(FileName);
            try
            {
                Writer = new StreamWriter(FileName, false, Encoding.UTF8);

                serializer.Serialize(Writer, this);
                result = true;

            }
            finally
            {
                if (Writer != null)
                {
                    Writer.Close();
                }
            }
            return result;
        }
    }



}
