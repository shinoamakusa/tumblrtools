﻿/* 01010011 01101000 01101001 01101110 01101111  01000001 01101101 01100001 01101011 01110101 01110011 01100001
 *
 *  Project: Tumblr Tools - Image parser and downloader from Tumblr blog system
 *
 *  Author: Shino Amakusa
 *
 *  Created: 2013
 *
 *  Last Updated: December, 2014
 *
 * 01010011 01101000 01101001 01101110 01101111  01000001 01101101 01100001 01101011 01110101 01110011 01100001 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tumblr_Tool.Common_Helpers;
using Tumblr_Tool.Enums;
using Tumblr_Tool.Image_Ripper;
using Tumblr_Tool.Managers;
using Tumblr_Tool.Properties;
using Tumblr_Tool.Tumblr_Objects;
using Tumblr_Tool.Tumblr_Stats;

namespace Tumblr_Tool
{
    public partial class mainForm : Form
    {
        public bool isCancelled = false;
        private static TumblrBlog tumblrBlog;

        private AboutForm aboutForm;
        private int currentSelectedTab;
        private bool disableOtherTabs = false;
        private bool downloadDone = false;
        private List<string> downloadedList = new List<string>();
        private List<int> downloadedSizesList = new List<int>();
        private bool fileDownloadDone = false;
        private FileManager fileManager;
        private List<string> notDownloadedList = new List<string>();
        private ToolOptions options = new ToolOptions();
        private OptionsForm optionsForm;
        private bool readyForDownload = true;
        private ImageRipper ripper = new ImageRipper();
        private SaveFile tumblrSaveFile, tumblrLogFile;
        private TumblrStats tumblrStats = new TumblrStats();
        private string version = "1.0.15";
        public string saveLocation;

        public mainForm()
        {
            // Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru");

            InitializeComponent();

            tumblrStats.blog = null;

            this.select_Mode.SelectedIndex = 1;
            AdvancedMenuRenderer renderer = new AdvancedMenuRenderer();
            renderer.HighlightForeColor = Color.Maroon;
            renderer.HighlightBackColor = Color.White;
            renderer.ForeColor = Color.Black;
            renderer.BackColor = Color.White;

            menu_TopMenu.Renderer = renderer;
            txt_WorkStatus.Visible = false;
            txt_Stats_BlogDescription.Visible = false;
            lbl_Stats_BlogTitle.Text = "";

            bar_Progress.Visible = false;
            fileManager = new FileManager();
            this.Text += " (" + version + ")";
            optionsForm = new OptionsForm();
            optionsForm.mainForm = this;
            aboutForm = new AboutForm();
            aboutForm.mainForm = this;
            aboutForm.version = "Version: " + version;

            optionsForm.apiMode = apiModeEnum.JSON.ToString();

            loadOptions();
            lbl_Size.Text = "";
            lbl_PostCount.Text = "";
            lbl_Status.Text = "Ready";

            SetDoubleBuffering(bar_Progress, true);
        }

        public mainForm(string file)
        {
            InitializeComponent();
            tumblrStats.blog = null;
            AdvancedMenuRenderer renderer = new AdvancedMenuRenderer();
            renderer.HighlightForeColor = Color.Maroon;
            renderer.HighlightBackColor = Color.White;
            renderer.ForeColor = Color.Black;
            renderer.BackColor = Color.White;

            menu_TopMenu.Renderer = renderer;

            lbl_Stats_BlogTitle.Text = "";

            txt_WorkStatus.Visible = false;
            txt_Stats_BlogDescription.Visible = false;
            lbl_Size.Text = "";
            lbl_PostCount.Text = "";
            bar_Progress.Visible = false;
            fileManager = new FileManager();
            txt_SaveLocation.Text = Path.GetDirectoryName(file);

            updateStatusText("Opening save file ...");

            openTumblrFile(file);

            //saveFile = fileManager.readTumblrFile(file);

            //if (File.Exists(Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".log"))
            //{
            //    logFile = fileManager.readTumblrFile(Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".log");
            //}

            //txt_TumblrURL.Text = saveFile != null ? saveFile.getBlogURL() : "";
            //tumblrBlog = saveFile != null ? saveFile.blog : null;

            this.Text += " (" + version + ")";
            optionsForm = new OptionsForm();
            optionsForm.mainForm = this;
            aboutForm = new AboutForm();
            aboutForm.mainForm = this;
            aboutForm.version = "Version: " + version;
            this.select_Mode.SelectedIndex = 1;
            optionsForm.apiMode = apiModeEnum.JSON.ToString();
            loadOptions();
            lbl_Status.Text = "Ready";
            SetDoubleBuffering(bar_Progress, true);
        }

        public static void SetDoubleBuffering(System.Windows.Forms.Control control, bool value)
        {
            System.Reflection.PropertyInfo controlProperty = typeof(System.Windows.Forms.Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controlProperty.SetValue(control, value, null);
        }

        public void button_MouseEnter(object sender, EventArgs e)
        {
            Button button = sender as Button;

            button.UseVisualStyleBackColor = false;
            button.ForeColor = Color.Maroon;
            button.FlatAppearance.BorderColor = Color.Maroon;
            button.FlatAppearance.MouseOverBackColor = Color.White;
            button.FlatAppearance.BorderSize = 1;
        }

        public void button_MouseLeave(object sender, EventArgs e)
        {
            Button button = sender as Button;
            button.UseVisualStyleBackColor = true;
            button.ForeColor = Color.Black;
            button.FlatAppearance.BorderSize = 0;
        }

        public bool isValidURL(string urlString)
        {
            try
            {
                return urlString.isValidUrl();
            }
            catch (Exception e)
            {
                string error = e.Message;
                return false;
            }
        }

        public void loadOptions()
        {
            optionsForm.setOptions();
            loadOptions(optionsForm.options);
        }

        public void loadOptions(ToolOptions _options)
        {
            this.options = _options;
        }

        public void openTumblrFile(string file)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    tumblrSaveFile = !string.IsNullOrEmpty(file) ? fileManager.readTumblrFile(file) : null;

                    tumblrBlog = tumblrSaveFile != null ? tumblrSaveFile.blog : null;

                    txt_SaveLocation.Text = !string.IsNullOrEmpty(file) ? Path.GetDirectoryName(file) : "";

                    txt_TumblrURL.Text = "File:" + file;

                    if (tumblrSaveFile != null && tumblrSaveFile.blog != null && !string.IsNullOrEmpty(tumblrSaveFile.blog.url))
                    {
                        txt_TumblrURL.Text = tumblrSaveFile.blog.url;
                    }
                    else if (tumblrSaveFile != null && tumblrSaveFile.blog != null && string.IsNullOrEmpty(tumblrSaveFile.blog.url) && !string.IsNullOrEmpty(tumblrSaveFile.blog.cname))
                    {
                        txt_TumblrURL.Text = tumblrSaveFile.blog.cname;
                    }
                    else
                    {
                        txt_TumblrURL.Text = "Error parsing save file...";
                    }

                    updateStatusText("Ready");
                    btn_Start.Enabled = true;
                }
            }
            catch
            {
            }
        }

        public void updateStatusText(string text)
        {
            if (!lbl_Status.Text.Contains(text))
            {
                lbl_Status.Text = text;
                lbl_Status.Invalidate();
                status_Strip.Update();
                status_Strip.Refresh();
            }
        }

        public void updateWorkStatusText(string strToReplace, string strToAdd = "")
        {
            if (txt_WorkStatus.Text.Contains(strToReplace) && !txt_WorkStatus.Text.Contains(string.Concat(strToReplace, strToAdd)))
            {
                txt_WorkStatus.Text = txt_WorkStatus.Text.Replace(strToReplace, string.Concat(strToReplace, strToAdd));

                txt_WorkStatus.Update();
                txt_WorkStatus.Refresh();
            }
        }

        public void updateWorkStatusText(string str)
        {
            if (txt_WorkStatus.Text.EndsWith(str))
            {
                txt_WorkStatus.Text += str;

                txt_WorkStatus.Update();
                txt_WorkStatus.Refresh();
            }
        }

        public void updateWorkStatusTextNewLine(string text)
        {
            if (!txt_WorkStatus.Text.Contains(text))
            {
                txt_WorkStatus.Text += txt_WorkStatus.Text != "" ? "\r\n" : "";
                txt_WorkStatus.Text += text;
                txt_WorkStatus.Update();
                txt_WorkStatus.Refresh();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.aboutForm.ShowDialog();
        }

        private void btn_Browse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog ofd = new FolderBrowserDialog())
            {
                DialogResult result = ofd.ShowDialog();
                if (result == DialogResult.OK)

                    txt_SaveLocation.Text = ofd.SelectedPath;
            }
        }

        private void btn_Crawl_Click(object sender, EventArgs e)
        {
            enableUI_Crawl(false);
            this.saveLocation = txt_SaveLocation.Text;

            if (tumblrSaveFile != null && options.generateLog)
            {
                string file = txt_SaveLocation.Text + @"\" + Path.GetFileNameWithoutExtension(tumblrSaveFile.fileName);

                if (File.Exists(Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".log"))
                {
                    updateStatusText("Reading log file ...");

                    tumblrLogFile = fileManager.readTumblrFile(Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".log");
                }
            }

            lbl_PostCount.Visible = false;
            lbl_PostCount.ForeColor = Color.Black;
            bar_Progress.Visible = false;
            txt_WorkStatus.Visible = true;
            txt_WorkStatus.Clear();
            lbl_Size.Text = "";
            lbl_Size.Visible = false;
            fileManager = new FileManager();
            ripper = new ImageRipper();


            if (checkFields())
            {
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        updateStatusText("Initializing ...");
                        updateWorkStatusTextNewLine("Initializing ... ");
                    });
                }

                crawl_Worker.RunWorkerAsync(ripper);

                crawl_UpdateUI_Worker.RunWorkerAsync(ripper);
            }
            else
            {
                btn_Start.Enabled = true;
                lbl_PostCount.Visible = false;
                lbl_Size.Visible = false;
                bar_Progress.Visible = false;
                img_DisplayImage.Visible = true;
                tab_TumblrStats.Enabled = true;
            }
        }

        private void btn_GetStats_Click(object sender, EventArgs e)
        {
            lbl_PostCount.Visible = false;
            updateStatusText("Initializing...");
            if (isValidURL(txt_Stats_TumblrURL.Text))
            {
                enableUI_Stats(false);

                getStats_Worker.RunWorkerAsync();
                getStatsUI_Worker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Please enter valid url!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                updateStatusText("Ready");
            }
        }

        private bool checkFields()
        {
            bool saveLocationEmpty = string.IsNullOrEmpty(this.saveLocation);
            bool urlValid = true;

            if (saveLocationEmpty)
            {
                MessageBox.Show("Save Location cannot be left empty! \r\nSelect a valid location on disk", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btn_Browse.Focus();
            }
            else
            {
                if (!isValidURL(txt_TumblrURL.Text))
                {
                    MessageBox.Show("Please enter valid url!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txt_TumblrURL.Focus();
                    urlValid = false;
                }
            }

            return (!saveLocationEmpty && urlValid);
        }

        private void colorizeProgressBar(int value)
        {
            switch (value / 10)
            {
                case 0:
                    bar_Progress.ForeColor = Color.Black;
                    break;

                case 1:
                    bar_Progress.ForeColor = Color.DarkGray;
                    break;

                case 2:
                    bar_Progress.ForeColor = Color.DarkRed;
                    break;

                case 3:
                    bar_Progress.ForeColor = Color.Firebrick;
                    break;

                case 4:
                    bar_Progress.ForeColor = Color.OrangeRed;
                    break;

                case 5:
                    bar_Progress.ForeColor = Color.Navy;
                    break;

                case 6:
                    bar_Progress.ForeColor = Color.DarkBlue;
                    break;

                case 7:
                    bar_Progress.ForeColor = Color.Blue;
                    break;

                case 8:
                    bar_Progress.ForeColor = Color.LightBlue;
                    break;

                case 9:
                    bar_Progress.ForeColor = Color.LimeGreen;
                    break;

                default:
                    bar_Progress.ForeColor = Color.YellowGreen;
                    break;
            }
        }

        private void crawlUIWorker_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ripper != null)
            {
                //while (ripper.statusCode != processingCodes.Done)
                //{
                //    // wait for crawler to catch up
                //}

                if (ripper.statusCode != processingCodes.Crawling)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                            {
                                updateWorkStatusText("Parsing posts ...", " done");

                                updateWorkStatusTextNewLine("Found " + (ripper.imageList.Count() == 0 ? "no" : ripper.imageList.Count().ToString()) + " new image(s) to download");
                                bar_Progress.Visible = false;
                                bar_Progress.Value = 0;
                                bar_Progress.Update();

                                bar_Progress.Refresh();
                            });
                    }
                }
                else
                {
                    if (ripper.statusCode == processingCodes.UnableDownload)
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                updateWorkStatusTextNewLine("Error downloading the blog post XML");
                                updateStatusText("Error");
                            });
                        }
                    }
                    else if (ripper.statusCode == processingCodes.invalidURL)
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                updateWorkStatusTextNewLine("Invalid Tumblr URL");
                                updateStatusText("Error");
                            });
                        }
                    }
                }
            }

            if (!this.IsDisposed)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (optionsForm.parseOnly)
                    {
                        enableUI_Crawl(true);
                    }
                    else if (ripper.statusCode != processingCodes.Done)
                    {
                        enableUI_Crawl(true);
                    }
                    else
                    {
                        updateStatusText("Done");
                        bar_Progress.Visible = false;
                        lbl_PostCount.Visible = false;
                    }
                });
            }
        }

        private void crawlUIWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (ripper != null)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                                {
                                    bar_Progress.Minimum = 0;
                                    bar_Progress.Value = 0;
                                    bar_Progress.Step = 1;
                                    bar_Progress.Maximum = 100;

                                    // lbl_Timer.Visible = true;
                                    lbl_PostCount.Text = "";
                                    img_DisplayImage.Image = Resources.crawling;
                                });
                    }

                    int percent = 0;

                    while (percent < 100 && (ripper.statusCode != processingCodes.invalidURL && ripper.statusCode != processingCodes.Done && ripper.statusCode != processingCodes.connectionError))
                    {

                        if (ripper.statusCode == processingCodes.checkingConnection)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateWorkStatusTextNewLine("Checking for Internet connection ...");
                                    });
                                }
                            }
                        }

                        if (ripper.statusCode == processingCodes.connectionOK)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateWorkStatusText("Checking for Internet connection ...", " ok");
                                        updateWorkStatusTextNewLine("Starting ...");
                                    });
                                }
                            }
                        }
                        if (ripper.statusCode == processingCodes.connectionError)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateStatusText("Error");
                                        updateWorkStatusText("Checking for Internet connection ...", " not found");
                                        btn_Start.Enabled = true;
                                        lbl_PostCount.Visible = false;
                                        bar_Progress.Visible = false;
                                        img_DisplayImage.Visible = true;
                                        img_DisplayImage.Image = Resources.tumblrlogo;
                                        tab_TumblrStats.Enabled = true;
                                    });
                                }
                            }
                        }

                        if (ripper.statusCode == processingCodes.gettingBlogInfo)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateWorkStatusTextNewLine("Getting Blog info ...");
                                    });
                                }
                            }
                        }

                        if (ripper.statusCode == processingCodes.blogInfoOK)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateWorkStatusText("Getting Blog info ...", " done");
                                        lbl_PostCount.Text = "0 / 0";
                                        lbl_PostCount.Visible = false;
                                        txt_WorkStatus.Visible = true;

                                        txt_WorkStatus.SelectionStart = txt_WorkStatus.Text.Length;
                                    });
                                }
                            }
                        }

                        if (ripper.statusCode == processingCodes.Starting)
                        {
                            lock (ripper)
                            {
                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        updateWorkStatusTextNewLine("Indexing " + "\"" + ripper.blog.title + "\" ... ");
                                        updateStatusText("Starting ...");
                                    });
                                }

                                if (ripper.totalPosts != 0)
                                {
                                    if (!this.IsDisposed)
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            updateWorkStatusTextNewLine(ripper.totalPosts + " photo posts found.");
                                        });
                                    }
                                }

                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                        {
                                            updateWorkStatusTextNewLine("Parsing posts ...");
                                        });
                                }
                            }
                        }
                        if (ripper.statusCode == processingCodes.Crawling)
                        {
                            lock (ripper)
                            {
                                percent = ripper.percentComplete;

                                if (percent > 100)
                                    percent = 100;

                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                       {
                                           // colorizeProgressBar(percent);
                                       });

                                    this.Invoke((MethodInvoker)delegate
                                       {
                                           updateStatusText("Indexing...");
                                           bar_Progress.Visible = true;
                                           // lbl_PostCount.Visible = true;
                                           lbl_PercentBar.Visible = true;
                                           lbl_PostCount.Visible = true;
                                           lbl_PercentBar.Text = percent.ToString() + "%";
                                           lbl_PostCount.Text = ripper.parsedPosts.ToString() + "/" + ripper.totalPosts.ToString();
                                           bar_Progress.Value = percent;
                                       });
                                }
                            }
                        }
                    }

                    if (ripper.statusCode == processingCodes.invalidURL)
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                updateStatusText("Error");
                                updateWorkStatusTextNewLine("Invalid Tumblr URL: " + txt_TumblrURL.Text);
                                btn_Start.Enabled = true;
                                lbl_PostCount.Visible = false;
                                bar_Progress.Visible = false;
                                img_DisplayImage.Visible = true;
                                img_DisplayImage.Image = Resources.tumblrlogo;
                                tab_TumblrStats.Enabled = true;
                            });
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }

        private void crawlWorker_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (ripper != null)
                {

                    lock (ripper)
                    {
                        ripper.statusCode = processingCodes.Done;
                    }


                    if (ripper.statusCode == processingCodes.Done)
                    {
                        tumblrSaveFile.blog = ripper.blog;
                        tumblrLogFile = null;
                        ripper.tumblrPostLog = null;

                        if (!optionsForm.parseOnly)
                        {
                            Thread.Sleep(100);
                            fileManager.totalToDownload = ripper.totalImagesCount;
                            download_Worker.RunWorkerAsync(ripper.imageList);
                            download_UIUpdate_Worker.RunWorkerAsync(fileManager);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void crawlWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Thread.Sleep(100);


                lock (ripper)
                {
                    Thread.Sleep(100);
                    tumblrBlog = new TumblrBlog();
                    tumblrBlog.url = txt_TumblrURL.Text;

                    this.ripper = new ImageRipper(tumblrBlog, this.saveLocation, optionsForm.generateLog, optionsForm.parsePhotoSets, optionsForm.parseJPEG, optionsForm.parsePNG, optionsForm.parseGIF, 0);
                    ripper.statusCode = processingCodes.Initializing;

                }

                lock (ripper)
                {
                    ripper.statusCode = processingCodes.checkingConnection;
                }

                if (WebHelper.checkForInternetConnection())
                {
                    lock (ripper)
                    {
                        ripper.statusCode = processingCodes.connectionOK;
                    }



                    if (this.ripper != null)
                    {
                        this.ripper.setAPIMode(options.apiMode);
                        this.ripper.setLogFile(ref tumblrLogFile);

                        if (ripper.isValidTumblr())
                        {
                            lock (ripper)
                            {
                                ripper.statusCode = processingCodes.gettingBlogInfo;
                            }

                            if (this.ripper.setBlogInfo())
                            {
                                lock (ripper)
                                {
                                    ripper.statusCode = processingCodes.blogInfoOK;
                                }

                                if (!saveTumblrFile(this.ripper.blog.name))
                                {
                                    lock (ripper)
                                    {
                                        ripper.statusCode = processingCodes.saveFileError;
                                    }
                                }
                                else
                                {
                                    lock (ripper)
                                    {
                                        ripper.statusCode = processingCodes.saveFileOK;
                                    }

                                    if (this.ripper != null)
                                    {
                                        int mode = 0;
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            mode = select_Mode.SelectedIndex + 1;
                                        });

                                        lock (ripper)
                                        {
                                            ripper.statusCode = processingCodes.Starting;
                                        }

                                        tumblrBlog = this.ripper.parseBlogPosts(mode);

                                        lock (ripper)
                                        {
                                            if (ripper.logUpdated)
                                            {
                                                ripper.statusCode = processingCodes.SavingLogFile;

                                                if (!this.IsDisposed)
                                                {
                                                    this.Invoke((MethodInvoker)delegate
                                                    {
                                                        updateStatusText("Saving Log File");
                                                        updateWorkStatusTextNewLine("Saving Log File ...");
                                                    });
                                                }

                                                fileManager.saveTumblrFile(ripper.saveLocation + @"\" + ripper.tumblrPostLog.getFileName(), ripper.tumblrPostLog);

                                                if (!this.IsDisposed)
                                                {
                                                    this.Invoke((MethodInvoker)delegate
                                                    {
                                                        updateStatusText("Log Saved");
                                                        updateWorkStatusText("Saving Log File ...", " done");
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }

                                lock (ripper)
                                {
                                    ripper.statusCode = processingCodes.Done;
                                }
                            }
                            else
                            {
                                lock (ripper)
                                {
                                    ripper.statusCode = processingCodes.blogInfoError;
                                }
                            }
                        }
                        else
                        {
                            lock (ripper)
                            {
                                ripper.statusCode = processingCodes.invalidURL;
                            }
                        }
                    }
                }
                else
                {
                    lock (ripper)
                    {
                        ripper.statusCode = processingCodes.connectionError;
                    }
                }
            }
            catch
            {
            }
        }

        private void downloadUIUpdate_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    try
                    {
                        if (fileManager.totalToDownload == 0)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                img_DisplayImage.Image = Resources.tumblrlogo;
                            });
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                img_DisplayImage.ImageLocation = downloadedList[downloadedList.Count - 1];
                            });
                        }
                    }
                    catch
                    {
                        //
                    }
                    // img_DisplayImage.Image = Image.FromFile(fileManager.downloadedList[fileManager.downloadedList.Count - 1]);
                    this.Invoke((MethodInvoker)delegate
                    {
                        img_DisplayImage.Update();
                        img_DisplayImage.Refresh();
                    });

                    if (fileManager.statusCode == downloadStatusCodes.Done && downloadedList.Count > 0)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            updateWorkStatusText("Downloading images ...", " done");
                            updateWorkStatusTextNewLine("Downloaded " + downloadedList.Count.ToString() + " image(s).");
                        });
                    }
                    this.Invoke((MethodInvoker)delegate
                    {
                        updateStatusText("Done");

                        lbl_PercentBar.Visible = false;

                        bar_Progress.Visible = false;
                        enableUI_Crawl(true);
                    });
                }
            }
            catch
            {
            }
        }

        private void downloadUIUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        updateStatusText("Preparing...");

                        bar_Progress.Step = 1;
                        bar_Progress.Minimum = 0;
                        bar_Progress.Maximum = 100;
                        bar_Progress.Value = 0;
                        lbl_PercentBar.Text = "0%";

                        lbl_PostCount.Text = "";
                    });
                }

                FileManager fileManager = (FileManager)e.Argument;

                if (fileManager.totalToDownload == 0)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            bar_Progress.Visible = false;

                            // updateWorkStatusTextNewLine("No new images to download");
                            lbl_PostCount.Visible = false;
                            updateStatusText("Done"); ;
                        });
                    }
                }
                else
                {
                    while (fileManager.statusCode != downloadStatusCodes.Downloading && !isCancelled)
                    {
                        int percent = (int)fileManager.percentDownloaded;
                        if (fileManager.statusCode == downloadStatusCodes.Preparing)
                        {
                            if (!this.IsDisposed)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    updateStatusText("Preparing...");

                                    bar_Progress.Step = 1;
                                    bar_Progress.Minimum = 0;
                                    bar_Progress.Maximum = 100;
                                    bar_Progress.Value = 0;
                                    lbl_PercentBar.Text = "0%";

                                    lbl_PostCount.Text = "";
                                });
                            }
                        }
                    }

                    if (fileManager.statusCode == downloadStatusCodes.Downloading)
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                updateWorkStatusTextNewLine("Downloading images ...");
                                updateStatusText("Downloading..."); ;
                            });
                        }

                        decimal totalLength = 0;
                        while (!downloadDone && !isCancelled)
                        {


                            int c = 0;
                            int f = 0;

                            if (notDownloadedList.Count != 0 && f != notDownloadedList.Count)
                            {
                                f = notDownloadedList.Count;

                                if (!this.IsDisposed)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        lbl_PostCount.ForeColor = Color.Maroon;
                                        bar_Progress.ForeColor = Color.Maroon;
                                        lbl_PercentBar.ForeColor = Color.Maroon;
                                        updateWorkStatusTextNewLine("Error: Unable to download " + notDownloadedList[notDownloadedList.Count - 1]);
                                    });
                                }
                            }

                            if (downloadedList.Count != 0 && c != downloadedList.Count)
                            {
                                c = downloadedList.Count;

                                if (!this.IsDisposed)
                                {
                                    try
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            if (img_DisplayImage.ImageLocation != downloadedList[c - 1])
                                            {
                                                img_DisplayImage.ImageLocation = downloadedList[c - 1];
                                                img_DisplayImage.Load();
                                                //img_DisplayImage.Update();
                                                img_DisplayImage.Refresh();
                                            }
                                        });
                                    }
                                    catch (Exception)
                                    {
                                        readyForDownload = true;
                                    }

                                    int downloaded = fileManager.downloadedList.Count + 1;
                                    int total = fileManager.totalToDownload;

                                    if (downloaded > total)
                                        downloaded = total;

                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        if (!bar_Progress.Visible)
                                        {
                                            bar_Progress.Visible = true;
                                        }

                                        if (!lbl_PercentBar.Visible)
                                            lbl_PercentBar.Visible = true;

                                        if (!lbl_PostCount.Visible)
                                            lbl_PostCount.Visible = true;

                                        if (!lbl_Size.Visible)
                                            lbl_Size.Visible = true;

                                        lbl_PostCount.Text = downloaded.ToString() + " / " + total.ToString();
                                    });

                                    int percent = total > 0 ? (int)(((double)downloaded / (double)total) * 100.00) : 0;
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        if (bar_Progress.Value != percent)
                                        {
                                            bar_Progress.Value = percent;
                                            lbl_PercentBar.Text = percent.ToString() + "%";

                                            bar_Progress.Update();
                                            bar_Progress.Refresh();
                                        }
                                    });

                                    try
                                    {
                                        totalLength = (downloadedSizesList.Sum(x => Convert.ToInt32(x)) / (decimal)1024 / (decimal)1024);
                                        decimal totalLengthNum = totalLength > 1024 ? totalLength / 1024 : totalLength;
                                        string suffix = totalLength > 1024 ? "GB" : "MB";

                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            lbl_Size.Text = (totalLengthNum).ToString("0.00") + " " + suffix;
                                        });
                                    }
                                    catch (Exception)
                                    {
                                        //
                                    }

                                    //if (bar_Progress.Value != (int)fileManager.percentDownloaded)
                                    //{
                                    // bar_Progress.Value = (int)fileManager.percentDownloaded;

                                    if ((int)fileManager.percentDownloaded <= 0)
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            updateStatusText("Downloading: Connecting");
                                        });
                                    }
                                    else if (percent != (int)fileManager.percentDownloaded)
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            updateStatusText("Downloading: " + fileManager.percentDownloaded.ToString() + "%");
                                        });
                                    }

                                    // lbl_PercentBar.Text = fileManager.percentDownloaded.ToString() + "%";

                                    //}
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void downloadWorker_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (fileManager)
            {
                fileManager.statusCode = downloadStatusCodes.Done;
            }

            try
            {
                tumblrSaveFile.blog.posts = null;
                saveTumblrFile(ripper.blog.name);
                downloadDone = true;
            }
            catch
            {
            }
        }

        private void downloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                downloadedList.Clear();
                downloadedSizesList.Clear();
                downloadDone = false;
                fileDownloadDone = false;
                HashSet<PhotoPostImage> imagesList = (HashSet<PhotoPostImage>)e.Argument;

                Thread.Sleep(100);


                lock (fileManager)
                {
                    fileManager.statusCode = downloadStatusCodes.Preparing;
                }

                if (imagesList != null && imagesList.Count != 0)
                {
                    int j = 0;

                    lock (fileManager)
                    {
                        fileManager.statusCode = downloadStatusCodes.Downloading;
                    }

                    readyForDownload = true;

                    foreach (PhotoPostImage photoImage in imagesList)
                    {
                        if (isCancelled || downloadDone)
                            break;

                        while (!readyForDownload)
                        {
                            //wait till ready for download
                        }

                        fileDownloadDone = false;

                        lock (fileManager)
                        {
                            fileManager.statusCode = downloadStatusCodes.Downloading;
                        }

                        bool downloaded = false;
                        string fullPath = "";

                        while (!fileDownloadDone && !isCancelled)
                        {
                            fullPath = FileHelper.getFullFilePath(photoImage.filename, this.saveLocation);

                            downloaded = fileManager.downloadFile(photoImage.url, this.saveLocation, string.Empty, 1);

                            if (downloaded)
                            {
                                //if (ripper.commentsList.ContainsKey(Path.GetFileName(photoURL)))
                                //{
                                //    ImageHelper.addImageDescription(fullPath, ripper.commentsList[Path.GetFileName(photoURL)]);
                                //}

                                j++;
                                fileDownloadDone = true;
                                photoImage.downloaded = true;
                                fullPath = FileHelper.fixFileName(fullPath);
                                downloadedList.Add(fullPath);

                                downloadedSizesList.Add((int)new FileInfo(fullPath).Length);
                            }
                            else if (fileManager.statusCode == downloadStatusCodes.UnableDownload)
                            {
                                notDownloadedList.Add(photoImage.url);
                                photoImage.downloaded = false;
                                (new FileInfo(fullPath)).Delete();
                                fileDownloadDone = true;
                            }
                        }

                        if (isCancelled)
                        {
                            (new FileInfo(fullPath)).Delete();
                        }
                    }

                    downloadDone = true;
                }
            }
            catch
            {
            }
        }

        private void enableUI_Crawl(bool state)
        {
            btn_Browse.Enabled = state;
            btn_Start.Enabled = state;
            select_Mode.Enabled = state;
            fileToolStripMenuItem.Enabled = state;
            optionsToolStripMenuItem.Enabled = state;
            //lbl_PostCount.Visible = state;
            txt_TumblrURL.Enabled = state;
            txt_SaveLocation.Enabled = state;
            disableOtherTabs = !state;
        }

        private void enableUI_Stats(bool state)
        {
            btn_GetStats.Enabled = state;
            fileToolStripMenuItem.Enabled = state;
            optionsToolStripMenuItem.Enabled = state;
            txt_Stats_TumblrURL.Enabled = state;
            disableOtherTabs = !state;
        }

        private void fileBW_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private void fileBW_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    openTumblrFile((string)e.Argument);
                });
            }
            catch
            {
            }
        }

        private void form_Closing(object sender, FormClosingEventArgs e)
        {
            //e.Cancel = true;

            this.ripper.isCancelled = true;
            this.isCancelled = true;
            this.downloadDone = true;

            if (crawl_Worker.IsBusy)
            {
                crawl_Worker.CancelAsync();
            }

            if (crawl_UpdateUI_Worker.IsBusy)
            {
                crawl_UpdateUI_Worker.CancelAsync();
            }

            if (download_Worker.IsBusy)
            {
                download_Worker.CancelAsync();
            }

            if (download_UIUpdate_Worker.IsBusy)
            {
                download_UIUpdate_Worker.CancelAsync();
            }

            if (getStats_Worker.IsBusy)
            {
                getStats_Worker.CancelAsync();
            }

            if (getStatsUI_Worker.IsBusy)
            {
                getStatsUI_Worker.CancelAsync();
            }

            Application.Exit();
        }

        private void getStatsUIWorker_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        enableUI_Stats(true);
                        updateStatusText("Done");
                        lbl_PostCount.Visible = false;
                        bar_Progress.Visible = false;
                        lbl_PercentBar.Visible = false;
                    });
                }
            }
            catch
            {
            }
        }

        private void getStatsUIWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                        {
                            bar_Progress.Minimum = 0;
                            bar_Progress.Value = 0;
                            bar_Progress.Maximum = 100;
                            bar_Progress.Step = 1;
                            bar_Progress.Visible = true;
                            lbl_Size.Visible = false;
                            lbl_PercentBar.Visible = true;
                        });
                }

                while (this.tumblrStats.statusCode != processingCodes.Done && this.tumblrStats.statusCode != processingCodes.connectionError && this.tumblrStats.statusCode != processingCodes.invalidURL)
                {
                    if (tumblrStats.blog == null)
                    {
                        // wait till other worker created and populated blog info
                        this.Invoke((MethodInvoker)delegate
                        {
                            lbl_PercentBar.Text = "Getting initial blog info ... ";
                        });
                    }
                    else if (string.IsNullOrEmpty(tumblrStats.blog.title) && string.IsNullOrEmpty(tumblrStats.blog.description) && tumblrStats.totalPosts <= 0)
                    {
                        // wait till we got the blog title and desc and posts number
                    }
                    else
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                bar_Progress.Minimum = 0;
                                bar_Progress.Value = 0;
                                bar_Progress.Maximum = 100;
                                bar_Progress.Step = 1;
                                bar_Progress.Visible = true;

                                box_PostStats.Visible = true;
                                lbl_Stats_TotalCount.Visible = true;
                                lbl_Stats_BlogTitle.Text = tumblrStats.blog.title;
                                lbl_Stats_TotalCount.Text = tumblrStats.totalPosts.ToString();

                                lbl_PostCount.Text = "";
                                lbl_PostCount.Visible = true;
                                img_Stats_Avatar.LoadAsync(JSONHelper.getAvatarQueryString(tumblrStats.blog.url));
                            });
                        }

                        int percent = 0;
                        while (percent < 100)
                        {
                            if (!this.IsDisposed)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    txt_Stats_BlogDescription.Visible = true;
                                    if (txt_Stats_BlogDescription.Text == "")
                                        txt_Stats_BlogDescription.Text = WebHelper.stripHTMLTags(tumblrStats.blog.description);
                                });
                            }

                            percent = (int)(((double)tumblrStats.parsed / (double)tumblrStats.totalPosts) * 100.00);
                            if (percent < 0)
                                percent = 0;

                            if (percent >= 100)
                                percent = 100;

                            if (!this.IsDisposed)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    updateStatusText("Getting stats ...");
                                    lbl_PercentBar.Visible = true;
                                    lbl_Stats_TotalCount.Text = tumblrStats.totalPosts.ToString();

                                    lbl_Stats_PhotoCount.Text = tumblrStats.photoPosts.ToString();
                                    lbl_Stats_TextCount.Text = tumblrStats.textPosts.ToString();
                                    lbl_Stats_QuoteStats.Text = tumblrStats.quotePosts.ToString();
                                    lbl_Stats_LinkCount.Text = tumblrStats.linkPosts.ToString();
                                    lbl_Stats_AudioCount.Text = tumblrStats.audioPosts.ToString();
                                    lbl_Stats_VideoCount.Text = tumblrStats.videoPosts.ToString();
                                    lbl_Stats_ChatCount.Text = tumblrStats.chatPosts.ToString();
                                    lbl_Stats_AnswerCount.Text = tumblrStats.answerPosts.ToString();
                                    lbl_PercentBar.Text = percent.ToString() + "%";
                                    lbl_PostCount.Visible = true;
                                    lbl_PostCount.Text = tumblrStats.parsed.ToString() + "/" + tumblrStats.totalPosts.ToString();
                                    bar_Progress.Value = percent;
                                });
                            }
                        }
                    }
                }

                if (tumblrStats.statusCode == processingCodes.invalidURL)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            updateStatusText("Error");
                            lbl_PostCount.Visible = false;
                            bar_Progress.Visible = false;
                            lbl_Size.Visible = false;

                            MessageBox.Show("Invalid Tumblr URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                }
                else if (this.tumblrStats.statusCode == processingCodes.connectionError)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("No Internet connection detected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        updateStatusText("Error");
                    });
                }
            }
            catch (Exception)
            {
                // throw ex;
            }
        }

        private void getStatsWorker_AfterDone(object sender, RunWorkerCompletedEventArgs e)
        {
            this.tumblrStats.statusCode = processingCodes.Done;
        }

        private void getStatsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.tumblrStats.statusCode = processingCodes.Initializing;
            try
            {
                this.tumblrStats = new TumblrStats();
                // Thread.Sleep(100);

                if (WebHelper.checkForInternetConnection())
                {
                    this.Invoke((MethodInvoker)delegate
                            {
                                this.tumblrStats = new TumblrStats(tumblrBlog, txt_Stats_TumblrURL.Text, options.apiMode);
                                this.tumblrStats.statusCode = processingCodes.Initializing;
                            });

                    this.tumblrStats.parsePosts();

                    // tumblrStats.setAPIMode(options.apiMode);
                }
                else
                {
                    tumblrStats.statusCode = processingCodes.connectionError;
                }
            }
            catch
            {
            }
        }

        private void imageLoaded(object sender, AsyncCompletedEventArgs e)
        {
            readyForDownload = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btn_Start.Enabled = false;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.AddExtension = true;
                ofd.DefaultExt = ".tumblr";
                ofd.RestoreDirectory = true;
                ofd.Filter = "Tumblr Tools Files (.tumblr)|*.tumblr|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    updateStatusText("Opening save file ...");

                    fileBackgroundWorker.RunWorkerAsync(ofd.FileName);
                }
                else
                {
                    btn_Start.Enabled = true;
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.optionsForm.ShowDialog();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile saveFile = new SaveFile(ripper.blog.name + ".tumblr", ripper.blog);
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AddExtension = true;
                sfd.DefaultExt = ".tumblr";
                sfd.Filter = "Tumblr Tools Files (.tumblr)|*.tumblr";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (fileManager.saveTumblrFile(sfd.FileName, saveFile))
                    {
                        MessageBox.Show("Saved", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private bool saveTumblrFile(string name)
        {
            //if (saveFile == null || saveFile.blog.name != name)
            //{
            //    saveFile = new SaveFile(name + ".tumblr", ripper.blog);
            //}

            tumblrSaveFile = new SaveFile(name + ".tumblr", ripper.blog);

            return fileManager.saveTumblrFile(txt_SaveLocation.Text + @"\" + tumblrSaveFile.getFileName(), tumblrSaveFile);
        }

        private void statsTumblrURLUpdate(object sender, EventArgs e)
        {
            txt_Stats_TumblrURL.Text = txt_TumblrURL.Text;
        }

        private void tabControl_Main_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (disableOtherTabs)
            {
                Dotnetrix.Controls.TabControl tabWizardControl = sender as Dotnetrix.Controls.TabControl;

                int selectedTab = tabWizardControl.SelectedIndex;

                //Disable the tab selection
                if (currentSelectedTab != selectedTab)
                {
                    //If selected tab is different than the current one, re-select the current tab.
                    //This disables the navigation using the tab selection.
                    tabWizardControl.SelectTab(currentSelectedTab);
                }
            }
        }

        private void tabMainTabSelect_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (!e.TabPage.Enabled)
            {
                e.Cancel = true;
            }
        }

        private void tabPage_Enter(object sender, EventArgs e)
        {
            currentSelectedTab = tabControl_Main.SelectedIndex;
        }

        private void toolStripMenuItem_Paint(object sender, PaintEventArgs e)
        {
            ToolStripMenuItem TSMI = sender as ToolStripMenuItem;

            AdvancedMenuRenderer renderer = TSMI.GetCurrentParent().Renderer as AdvancedMenuRenderer;

            renderer.changeTextForeColor(TSMI, e);
        }

        private void txt_StatsTumblrURL_TextChanged(object sender, EventArgs e)
        {
            txt_TumblrURL.Text = txt_Stats_TumblrURL.Text;
        }

        private void workStatusAutoScroll(object sender, EventArgs e)
        {
            txt_WorkStatus.SelectionStart = txt_WorkStatus.TextLength;
            txt_WorkStatus.ScrollToCaret();
        }
    }
}