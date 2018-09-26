﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Media;
using System.Drawing.Imaging;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Text;
using System.Threading;

namespace launcher_imperivm_iii
{
    public partial class Launcher : MaterialForm
    {

        IniParser parserSettings = new IniParser(@"Settings.ini");
        IniParser parserLauncher = new IniParser(@"Launcher.ini");
        IniParser parserConst = new IniParser(@"DATA/CONST.INI");
        String pathTextResolutions = "Resolutions.txt";
        String pathTemplate = @"DATA/INTERFACE/MENU/TEMPLATE.INI";
        String pathBackground = @"CURRENTLANG/MENUBACKGROUND.BMP";
        String pathImage16_9 = @"CURRENTLANG/menu_16_9.BMP";
        String pathImage4_3 = @"CURRENTLANG/menu_4_3.BMP";
        String lastUpdate = "update__1_52";

        private WaveOut waveOut;
        bool isSoundPlay = true;

        private WaveOutEvent outputDevice;
        private AudioFileReader audioButton;

        public Launcher()
        {
            InitializeComponent();
            MaterialSkinManager materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Grey700, Primary.Grey900, Primary.Grey500, Accent.Amber200, TextShade.WHITE);
        }

        private void changeLanguageResolution()
        {

            parserSettings.AddSetting("Language", "Default", language.Text);
            parserLauncher.AddSetting("Default", "Resolution", resolution.SelectedIndex.ToString());

            int x = int.Parse(resolution.Text.Split('x')[0]);
            int y = int.Parse(resolution.Text.Split('x')[1]);

            Decimal is_16_9 = Math.Abs(Decimal.Divide(x, y) - Decimal.Divide(16, 9));
            Decimal is_4_3 = Math.Abs(Decimal.Divide(x, y) - Decimal.Divide(4, 3));

            if ((is_4_3 - is_16_9) < 0)
            {
                Console.WriteLine(is_4_3 + " 4:3 [" + x + "," + y + "]");
                ResizeImage(pathImage4_3, pathBackground, x, y);
            }
            else
            {
                Console.WriteLine(is_16_9 + " 16:9 [" + x + "," + y + "]");
                ResizeImage(pathImage16_9, pathBackground, x, y);
            }


            parserConst.AddSetting("Resolutions", "Res1_x", x.ToString());
            parserConst.AddSetting("Resolutions", "Res1_y", y.ToString());
            lineChanger("Larghezza = " + x, pathTemplate, 2);
            lineChanger("Altezza = " + y, pathTemplate, 3);
            

            parserConst.SaveSettings();
            parserSettings.SaveSettings();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            WaveFileReader reader = new WaveFileReader(@"Music\launcher.wav");
            LoopStream loop = new LoopStream(reader);
            waveOut = new WaveOut();
            waveOut.Init(loop);
            waveOut.Play();
           

            var pakLanguages = new DirectoryInfo("local").GetFiles("*.pak");
            for (int i = 0; i < pakLanguages.Length; i++)
            {
                String l = pakLanguages[i].Name.Split('.')[0];
                language.Items.Add(FirstCharToUpper(l));

            }

            IniParser parser = new IniParser(@"Settings.ini");
            String languageDefault = (parser.GetSetting("Language", "Default")).ToUpper();

            language.SelectedIndex = language.FindStringExact(languageDefault);
            language.DropDownStyle = ComboBoxStyle.DropDownList;
            String resolutionDefault = (parser.GetSetting("Options", "Resolution"));

            listResolution();

            loadLanguageLauncher();

            loadFolderMods();
            resolution.Refresh();
            this.Refresh();
        }

        public void changeCursor(Image cursor)
        {
            Bitmap img = new Bitmap(cursor, 50, 50);
            img.MakeTransparent(img.GetPixel(0, 0));
            PictureBox pb = new PictureBox() { Image = img };
            Cursor.Current = new Cursor(((Bitmap)pb.Image).GetHicon());
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void changeFolderMovies(string l)
        {
            if (l.Equals("spanish"))
            {
                if (!Directory.Exists(@"Movies_ITA"))
                {
                    Directory.Move(@"Movies", @"Movies_ITA");
                    Directory.Move(@"Movies_SPA", @"Movies");
                }
            }
            else if (l.Equals("italian"))
            {
                if (!Directory.Exists(@"Movies_SPA"))
                {
                    Directory.Move(@"Movies", @"Movies_SPA");
                    Directory.Move(@"Movies_ITA", @"Movies");
                }
            }
        }

        public void listResolution()
        {
            ArrayList list = readLines(pathTextResolutions);
            foreach (string i in list)
            {
                if (!i.Equals("\n") && !i.Equals(""))
                {
                    resolution.Items.Add(i);
                }

            }

            resolution.SelectedIndex = int.Parse(parserLauncher.GetSetting("Default", "Resolution"));
            resolution.DropDownStyle = ComboBoxStyle.DropDownList;
        }


        public static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        private void loadLanguageLauncher()
        {
            saveButton.Image = launcher_imperivm_iii.Properties.Resources.save;
            if (parserLauncher != null)
            {
                parserLauncher.AddSetting("Default", "Language", language.Text);
                parserLauncher.SaveSettings();

                String defaultLanguage = parserLauncher.GetSetting("Default", "Language");
                buttonAdventures.Text = parserLauncher.GetSetting(defaultLanguage, "ButtonAdventures");
                buttonScenarios.Text = parserLauncher.GetSetting(defaultLanguage, "ButtonScenarios");
                buttonProfiles.Text = parserLauncher.GetSetting(defaultLanguage, "ButtonProfiles");
                buttonConquest.Text = parserLauncher.GetSetting(defaultLanguage, "ButtonConquest");

                tabPage1.Text = parserLauncher.GetSetting(defaultLanguage, "Page1");
                tabPage2.Text = parserLauncher.GetSetting(defaultLanguage, "Page2");
                tabPage3.Text = parserLauncher.GetSetting(defaultLanguage, "Page3");
                tabPage4.Text = parserLauncher.GetSetting(defaultLanguage, "Page4");
                
                volumeSlider1.Volume = float.Parse(parserLauncher.GetSetting("Default", "Volume"));
                
                

                if (parserLauncher.GetSetting("Default", "Admin") == "1")
                {
                    checkBox1.Checked = true;
                }
                else
                {
                    checkBox1.Checked = false;
                }

                changeFolderMovies(language.Text.ToLower());
            }
        }

        private void loadFolderMods()
        {
            List<string> noMods = new List<string> { "AdditionalArt", "Buildings", "data", "emptyadv", "emptyconquest", "emptyscn", "Fonts", "MapObjects", "Minimap", "newmap", "Outlines", "randommap", "RandomMap", "RandomMapSettlements", "Sounds", "Terrain", "UI", "Units", "Visuals", lastUpdate };
            var pakMods = new DirectoryInfo("Packs").GetFiles("*.pak");
            for (int i = 0; i < pakMods.Length; i++)
            {
                String name = pakMods[i].Name.Split('.')[0];
                if (!noMods.Contains(name))
                {
                    listMods.Items.Add(name.ToLower());
                    if (name != name.ToUpper())
                    {
                        listMods.SetItemChecked(listMods.FindStringExact(name.ToLower()), true);
                    }
                }
            }
        }

        private void language_SelectedIndexChanged(object sender, EventArgs e)
        {
            saveButton.Image = launcher_imperivm_iii.Properties.Resources.save;
            loadLanguageLauncher();
            this.Refresh();
        }

        private void listMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            String item = listMods.Items[e.Index].ToString();
            if (listMods.GetItemChecked(listMods.FindStringExact(item)))
            {
                System.IO.File.Move(@"Packs/" + item + ".pak", @"Packs/" + item.ToUpper() + ".pak");
            }
            else
            {
                System.IO.File.Move(@"Packs/" + item.ToUpper() + ".pak", @"Packs/" + item + ".pak");
            }

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            changeLanguageResolution();
            parserLauncher.AddSetting("Default", "Volume", volumeSlider1.Volume.ToString());
            parserLauncher.SaveSettings();
            saveButton.Image = launcher_imperivm_iii.Properties.Resources.saveOk;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                parserLauncher.AddSetting("Default", "Admin", "1");
            }
            else
            {
                parserLauncher.AddSetting("Default", "Admin", "0");
            }
            parserLauncher.SaveSettings();
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.fxgamestudio.com/");
        }

        private void buttonScenarios_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Scenarios");
        }

        private void buttonAdventures_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Adventures");
        }

        private void buttonConquest_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Conquests");
        }

        private void buttonProfiles_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Profiles");
        }

        private void buttonPacks_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Packs");
        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            changeLanguageResolution();
            parserLauncher.SaveSettings();
        }

        private void materialFlatButton1_Click_1(object sender, EventArgs e)
        {
            changeLanguageResolution();
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            if (checkBox1.Checked)
            {
                processStartInfo.Verb = "runas";
            }
            processStartInfo.FileName = @"gbr.exe";
            System.Diagnostics.Process.Start(processStartInfo);

            Application.Exit();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            if (isSoundPlay)
            {
                pictureBox5.Image = launcher_imperivm_iii.Properties.Resources.soundOff;
                isSoundPlay = false;
                waveOut.Stop();
            }
            else
            {
                pictureBox5.Image = launcher_imperivm_iii.Properties.Resources.soundOk;
                waveOut.Play();
                isSoundPlay = true;
            }

        }

        private void pictureBox15_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"Packs");
        }

        public static Bitmap AdjustBrightness(Bitmap Image, int Value)
        {
            System.Drawing.Bitmap TempBitmap = Image;
            float FinalValue = (float)Value / 255.0f;
            System.Drawing.Bitmap NewBitmap = new System.Drawing.Bitmap(TempBitmap.Width, TempBitmap.Height);
            System.Drawing.Graphics NewGraphics = System.Drawing.Graphics.FromImage(NewBitmap);
            float[][] FloatColorMatrix ={
                      new float[] {1, 0, 0, 0, 0},
                      new float[] {0, 1, 0, 0, 0},
                      new float[] {0, 0, 1, 0, 0},
                      new float[] {0, 0, 0, 1, 0},
                      new float[] {FinalValue, FinalValue, FinalValue, 1, 1}
                 };

            System.Drawing.Imaging.ColorMatrix NewColorMatrix = new System.Drawing.Imaging.ColorMatrix(FloatColorMatrix);
            System.Drawing.Imaging.ImageAttributes Attributes = new System.Drawing.Imaging.ImageAttributes();
            Attributes.SetColorMatrix(NewColorMatrix);
            NewGraphics.DrawImage(TempBitmap, new System.Drawing.Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height), 0, 0, TempBitmap.Width, TempBitmap.Height, System.Drawing.GraphicsUnit.Pixel, Attributes);
            Attributes.Dispose();
            NewGraphics.Dispose();
            return NewBitmap;
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            Bitmap img = launcher_imperivm_iii.Properties.Resources.logoPlay;
            img.MakeTransparent(img.GetPixel(0, 0));
            pictureBox1.Image = AdjustBrightness(img, 50);
            pictureBox1.BackColor = Color.Transparent;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            Bitmap img = launcher_imperivm_iii.Properties.Resources.logoPlay;
            img.MakeTransparent(img.GetPixel(0, 0));
            pictureBox1.Image = img;
            pictureBox1.BackColor = Color.Transparent;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            changeLanguageResolution();
            parserLauncher.SaveSettings();
            waveOut.Stop();
            if (isSoundPlay)
            {
                playRandomSound();
            }
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
            if (checkBox1.Checked)
            {
                processStartInfo.Verb = "runas";
            }
            processStartInfo.FileName = @"gbr.exe";
            System.Diagnostics.Process.Start(processStartInfo);
            System.Threading.Thread.Sleep(2000);
            Application.Exit();
            
            
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/RErjBq8");
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/Imperivm3/");
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/fabiomarigo7/imperivm-steam");
        }

        public void ResizeImage(string input, string output, int width, int height)
        {
            using (var image = Image.FromFile(input))
            using (var newImage = ScaleImage(image, width, height))
            {
                newImage.Save(output, ImageFormat.Bmp);
            }
        }

        public static Image ScaleImage(Image image, int width, int height)
        {
            Size newSize = new Size(width, height);
            Image newImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics GFX = Graphics.FromImage((Bitmap)newImage))
            {
                GFX.DrawImage(image, new Rectangle(Point.Empty, newSize));
            }
            return newImage;
        }


        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        static ArrayList readLines(string fileName)
        {
            ArrayList list = new ArrayList();
            int counter = 0;
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = file.ReadLine()) != null)
            {
                list.Add(line);
                counter++;
            }
            file.Close();
            return list;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void pictureBox17_Click(object sender, EventArgs e)
        {
            if (!resX.Text.Equals("") && !resY.Text.Equals(""))
            {
                File.AppendAllText(pathTextResolutions, "\n" + resX.Text + "x" + resY.Text);
                resolution.Items.Clear();
                listResolution();
            }

        }

        private void resX_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void resY_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            if (!resX.Text.Equals("") && !resY.Text.Equals(""))
            {
                string searchFor = resX.Text + "x" + resY.Text;
                string[] lines = File.ReadAllLines(pathTextResolutions);
                List<String> newLines = new List<String>();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Equals(searchFor) && !lines[i].Equals("") && !lines[i].Equals("\n"))
                    {
                        newLines.Add(lines[i]);
                    }
                }
                resolution.Items.Clear();

                string[] endLinesArray = new string[newLines.Count];
                for (int i = 0; i < newLines.Count; i++)
                {
                    endLinesArray[i] = newLines[i];
                }

                File.WriteAllLines(pathTextResolutions, endLinesArray);
                listResolution();
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/d4nijerez");
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://download.fxinteractive.com/FX_Classic_Store_Area/Imperivm-GBR/ES_Manual_Imperivm_GBR.pdf");
        }

        private void label11_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/danijerez/launcher-imperivm-III");
        }

        private void pictureBox20_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://store.steampowered.com");
        }

        private void pictureBox21_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"Multiplayer\vpn-client\vpn.exe");
        }

        private void playRandomSound()
        {
            audioFiles = new List<String>();
            String l = language.Text;
            if (l.Equals("English"))
            {
                l = "Italian";
            }
            String directory = "Local\\" + l + "\\CURRENTLANG\\VOICES\\";
            ProcessDirectory(directory);
            outputDevice = new WaveOutEvent();
            Random random = new Random();
            audioButton = new AudioFileReader(audioFiles[random.Next(0, audioFiles.Count)]);
            outputDevice.Init(audioButton);
            outputDevice.Play();
        }
        List<String> audioFiles = null;
        public void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public void ProcessFile(string path)
        {
            Console.WriteLine(path);
            audioFiles.Add(path);
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            
        }

        private void volumeSlider1_VolumeChanged(object sender, EventArgs e)
        {
            waveOut.Volume = volumeSlider1.Volume;
            volumeSlider1.BackColor = Color.White;
        }

        private void pictureBox19_Click(object sender, EventArgs e)
        {
            
        }

        private void label7_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/fabiomarigo7/imperivm-steam/blob/master/update/changelog.md");
        }

        private void pictureBox22_Click(object sender, EventArgs e)
        {
            playRandomSound();
        }

        private void label19_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/RattlesMake");
        }
    }
}
