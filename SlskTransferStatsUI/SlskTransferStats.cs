using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace SlskTransferStatsUI
{

    public partial class SlskTransferStats : Form
    {
        public SlskTransferStats()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(AppDomain.CurrentDomain.FriendlyName);
            string version = Convert.ToString(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            Text = "Soulseek Upload Stats v" + version.Substring(0, version.Length - 2);

            //Only load the tree if settings exist TODO: tell the user to set their settings
            if (File.Exists("settings.ini"))
            {
                Globals.stopwatch = Stopwatch.StartNew();
                Globals.loading = true;

                //Read settings
                textBox1.Text = "";
                FileStream fs = new FileStream("settings.ini", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);

                string str = sr.ReadLine();
                Globals.UserDataFile = str;

                str = sr.ReadLine();
                if(str == "True")
                {
                    checkBox6.Checked = true;
                }
                else
                {
                    checkBox6.Checked = false;
                }

                while (str != null)
                {
                    str = sr.ReadLine();
                    Globals.SlskFolders.Add(str);
                }

                Globals.SlskFolders.RemoveAt(Globals.SlskFolders.Count - 1);
                label4.Text = ("Database save location: " + Globals.UserDataFile);

                for (int i = 0; i < Globals.SlskFolders.Count; i++)
                {
                    textBox5.AppendText(Globals.SlskFolders[i] + "\r\n");
                }

                sr.Close();
                fs.Close();

                if (File.Exists("userData.txt"))
                {
                    tabControl1.Visible = false;
                    treeView1.Visible = false;

                    DialogResult dialogResult = MessageBox.Show("Legacy text based database files found. Click OK to convert to a SQLite database.", "Please Convert Database", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                    if (dialogResult == DialogResult.OK)
                    {
                        ConvertLegacyDatabase();
                    }
                    else
                    {
                        Application.Exit();
                    }
                }

                Globals.users = SqliteDataAccess.LoadUsers();
                Globals.folders = SqliteDataAccess.LoadFolders();

                //Load tree (this does not call the parser for folder information, that only happens on the database button click)
                LoadTree();
            }
            else
            {
                textBox4.Text = "Please initialize your settings";
                textBox1.Text = "Please initialize your settings";
                textBox7.Text = "\r\nPlease initialize your settings";
            }
        }

        private void ConvertLegacyDatabase()
        {
            if (File.Exists(Globals.UserDataFile + "\\userData.txt"))
            {
                string input = File.ReadAllText(Globals.UserDataFile + "\\userData.txt");
                List<Person> users = JsonConvert.DeserializeObject<List<Person>>(input);

                SqliteDataAccess.ConvertLegacyDatabase(users);
            }

            tabControl1.Visible = true;
            treeView1.Visible = true;

            File.Move(Globals.UserDataFile + "\\userData.txt", Globals.UserDataFile + "\\legacyDatabaseBackup.txt");

            if (File.Exists(Globals.UserDataFile + "\\fileData.txt"))
            {
                File.Delete(Globals.UserDataFile + "\\fileData.txt");
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if(e.Node.Parent == null)
            {
                e.Node.Nodes.Clear();

                int folderCount = -1;
                double totalDownloadSize = 0;
                int downloadCount = 0;
                Person user = Globals.users.Find(i => i.Username == e.Node.Text);
                List<Download> userDownloads = SqliteDataAccess.LoadUserDownloads(user.Username);

                for (int j = 0; j < userDownloads.Count; j++)
                {
                    //get folder
                    string folder = userDownloads[j].Path.Substring(0, userDownloads[j].Path.LastIndexOf("\\"));

                    if (j == 0 || (userDownloads[j - 1].Path.Substring(0, userDownloads[j - 1].Path.LastIndexOf("\\")) != folder))
                    {
                        //first child (folders)
                        e.Node.Nodes.Add(folder);
                        folderCount++;

                    }
                    //second child (songs)
                    e.Node.Nodes[folderCount].Nodes.Add(userDownloads[j].Filename);
                    totalDownloadSize += userDownloads[j].Size;
                    downloadCount++;
                }
            }
        }

        //"Submit to database" button click event (writes text to file and parses it, then adds to database)
        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Contains("Queued") && textBox1.Text.Contains("Queue upload"))
            {
                File.WriteAllText("parsingData.txt", textBox1.Text);
                richTextBox6.Rtf = "";
                richTextBox3.Text = "";

                int extra = (int)Math.Round(textBox1.Lines.Count() * 0.05);
                progressBar1.Minimum = 1;
                progressBar1.Maximum = textBox1.Lines.Count() + extra;
                progressBar1.Value = 1;
                progressBar1.Step = 1;

                textBox1.Text = "";
                if (File.Exists("settings.ini"))
                {
                    textBox1.Text = "";
                    ParseData();
                }
                else
                {
                    textBox4.Text = "Please initialize your settings";
                    textBox1.Text = "Please initialize your settings";
                    textBox7.Text = "\r\nPlease initialize your settings";
                }
            }
        }

        public void LoadTree()
        {
            if (backgroundWorker2.IsBusy != true)
            {
                
                backgroundWorker2.RunWorkerAsync();
            }
        }

        public void ParseData()
        {
            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync();
            }
        }

        //ParseData
        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            FileStream fs = new FileStream("parsingData.txt", FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            sr.BaseStream.Seek(0, SeekOrigin.Begin);

            List<Person> users = new List<Person>();
            List<Folder> folders = new List<Folder>();
            string input;

            //Deserializes the database and sorts it
            if (File.Exists(Globals.UserDataFile + "\\userData.txt"))
            {
                input = System.IO.File.ReadAllText(@Globals.UserDataFile + "\\userData.txt");
                users = JsonConvert.DeserializeObject<List<Person>>(input);
                users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
            }

            string drive = "";
            string str = sr.ReadLine();
            string queued;
            string filename;
            string username;
            string path;
            string date;
            int index;
            bool added;
            double size;
            double totalSize = 0;
            int newUserCount = 0;
            int oldUserCount = 0;
            int filesAdded = 0;
            string folder;
            RichTextBox info = new RichTextBox();
            RichTextBox stats = new RichTextBox();
            int dateLength;
            string[] dateSplit;
            List<string> oldUserList = new List<string>();

            //loop through file made by input data
            while (str != null)
            {
                //get length of the date section of the file (this changes depending on day of month)
                dateLength = (str.Substring(0, str.IndexOf("]") + 1)).Length - 1;
                //Look for the term "Queue" and then on the next line the word "Queued" then parse that line
                if (str.IndexOf("Queue", dateLength + 2, 5) == dateLength + 2)
                {
                    queued = str;
                    str = sr.ReadLine();

                    if(str == null)
                    {
                        break;
                    }

                    backgroundWorker1.ReportProgress(1);
                    if (str.IndexOf("Queued", dateLength + 5, 6) == dateLength + 5)
                    {
                        username = queued.Substring(dateLength + 28, queued.Length - (dateLength + 28));
                        username = username.Substring(0, username.LastIndexOf(" for file @"));

                        index = users.FindIndex(person => person.Username == username);

                        //if user is not in the database
                        if (index < 0)
                        {
                            //get path
                            path = queued.Substring(queued.IndexOf("\\"));

                            //math path with no drive to path from settings with drive (then extract the drive from this string)
                            for (int i = 0; i < Globals.SlskFolders.Count; i++)
                            {
                                string localPath = Globals.SlskFolders[i].Substring(Globals.SlskFolders[i].LastIndexOf("\\") + 1);
                                if (path.Contains(localPath) || path.Contains(localPath.ToLower()))
                                {
                                    drive = Globals.SlskFolders[i].Substring(0, Globals.SlskFolders[i].LastIndexOf("\\"));
                                    break;
                                }
                            }
                            //add drive to path
                            path = drive + path;
                            filename = queued.Substring(queued.LastIndexOf("\\") + 1);

                            if (File.Exists(path))
                            {
                                size = new FileInfo(path).Length / 1000;
                                totalSize += size;
                            }
                            else
                            {
                                size = 0;
                            }

                            date = queued.Substring(0, queued.IndexOf("]") + 1);

                            //create downloads list and add a download
                            List<Download> downloads = new List<Download>();

                            info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                            info.SelectionColor = Color.White;
                            info.AppendText("New user found: " + username + "\r\n");
                            info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                            info.SelectionColor = Color.White;
                            info.AppendText("\tNew download for \"");
                            info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                            info.SelectionColor = Color.White;
                            info.AppendText(username);
                            info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                            info.SelectionColor = Color.White;
                            info.AppendText("\": " + filename + "\r\n");

                            downloads.Add(new Download(username, filename, path, size, date));
                            users.Add(new Person(username, 1, size, date, downloads));
                            oldUserList.Add(username);
                            index = users.FindIndex(person => person.Username == username);
                            newUserCount += 1;
                            filesAdded += 1;

                        }
                        //user already in the database
                        else
                        {
                            path = queued.Substring(queued.IndexOf("\\"));

                            //math path with no drive to path from settings with drive (then extract the drive from this string)
                            for (int i = 0; i < Globals.SlskFolders.Count; i++)
                            {
                                string localPath = Globals.SlskFolders[i].Substring(Globals.SlskFolders[i].LastIndexOf("\\") + 1);
                                if (path.Contains(localPath) || path.Contains(localPath.ToLower()))
                                {
                                    drive = Globals.SlskFolders[i].Substring(0, Globals.SlskFolders[i].LastIndexOf("\\"));
                                    break;
                                }
                            }
                            path = drive + path;
                            filename = queued.Substring(queued.LastIndexOf("\\") + 1);

                            if (File.Exists(path))
                            {
                                size = new FileInfo(path).Length / 1000;
                            }
                            else
                            {
                                size = 0;
                            }

                            date = queued.Substring(0, queued.IndexOf("]") + 1);
                            added = false;

                            //check if file is already in their downloads
                            for (int i = 0; i < users[index].DownloadList.Count; i++)
                            {
                                if (users[index].DownloadList[i].Filename == filename && users[index].DownloadList[i].Date == date)
                                {
                                    added = true;
                                    break;
                                }
                            }

                            //if not added, add it to their downloads list
                            if (added == false)
                            {
                                users[index].DownloadList.Add(new Download(username, filename, path, size, date));
                                users[index] = new Person(username,
                                                          (int)users[index].DownloadNum + 1,
                                                          users[index].TotalDownloadSize += size,
                                                          users[index].DownloadList[users[index].DownloadList.Count - 1].Date,
                                                          users[index].DownloadList);

                                totalSize += size;

                                //Check if user has been added to the text box already
                                if (oldUserList.IndexOf(username) == -1)
                                {
                                    oldUserList.Add(username);
                                    oldUserCount += 1;
                                    info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                                    info.SelectionColor = Color.White;
                                    info.AppendText("Returning user found: " + username + "\r\n");
                                }

                                //info for the output textbox
                                info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                                info.SelectionColor = Color.White;
                                info.AppendText("\tNew download for \"");
                                info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                                info.SelectionColor = Color.White;
                                info.AppendText(users[index].Username);
                                info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                                info.SelectionColor = Color.White;
                                info.AppendText("\": " + users[index].DownloadList[users[index].DownloadList.Count - 1].Filename + "\r\n");

                                filesAdded += 1;
                            }
                        }
                    }
                }

                str = sr.ReadLine();
                backgroundWorker1.ReportProgress(1);
            }

            if (oldUserCount != 0 || newUserCount != 0)
            {
                string totalSizeString = "";
                decimal totalSizeDecimal = Convert.ToDecimal(totalSize);

                if (totalSize < 1000)
                {
                    totalSizeString = totalSizeDecimal.ToString("#.###") + " KB";
                }
                if (totalSize > 1000 && totalSize < 1000000)
                {
                    totalSizeDecimal = Decimal.Divide(totalSizeDecimal, 1000);
                    totalSizeString = totalSizeDecimal.ToString("#.###") + " MB";
                }
                if (totalSize > 1000000)
                {
                    totalSizeDecimal = Decimal.Divide(totalSizeDecimal, 1000000);
                    totalSizeString = totalSizeDecimal.ToString("#.###") + " GB";
                }

                stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                stats.SelectionColor = Color.White;
                stats.AppendText("Total size of added uploads: ");
                stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                stats.SelectionColor = Color.White;
                stats.AppendText(totalSizeString + "\r\n");

                stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                stats.SelectionColor = Color.White;
                stats.AppendText("Number of files uploaded: ");
                stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                stats.SelectionColor = Color.White;
                stats.AppendText(filesAdded + "\r\n");

                if (newUserCount > 0)
                {
                    stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                    stats.SelectionColor = Color.White;
                    stats.AppendText("New user count: ");
                    stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                    stats.SelectionColor = Color.White;
                    stats.AppendText(newUserCount + "\r\n");
                }

                if (oldUserCount > 0)
                {
                    stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                    stats.SelectionColor = Color.White;
                    stats.AppendText("Returning user count: ");
                    stats.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                    stats.SelectionColor = Color.White;
                    stats.AppendText(oldUserCount + "\r\n");
                }
            }

            string foldername;
            bool folderDL;
            int folderIndex;
            string output;
            string lastDate;

            //Get folder information from the database TODO: remove the parser file output
            for (int i = 0; i < users.Count; i++)
            {
                for (int j = 0; j < users[i].DownloadList.Count; j++)
                {
                    folder = users[i].DownloadList[j].Path.Substring(0, users[i].DownloadList[j].Path.LastIndexOf("\\"));
                    foldername = folder.Substring(folder.LastIndexOf("\\") + 1);

                    //Change date format
                    lastDate = users[i].DownloadList[j].Date.Substring(1, users[i].LastDate.Length - 2);
                    dateSplit = lastDate.Split();
                    lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                    if (j == 0 || (users[i].DownloadList[j - 1].Path.Substring(0, users[i].DownloadList[j - 1].Path.LastIndexOf("\\")) != folder))
                    {
                        if (folders.Count == 0)
                        {

                            folders.Add(new Folder(folder, folder.ToLower(), foldername, 1, users[i].Username, lastDate));
                        }
                        else
                        {
                            folderDL = false;

                            for (int k = 0; k < folders.Count; k++)
                            {

                                if (folders[k].Path == folder && (folders[k].LatestUser != users[i].Username))
                                {
                                    folders[k] = new Folder(folder, folder.ToLower(), foldername, folders[k].DownloadNum += 1, users[i].Username, lastDate);
                                    folderDL = true;
                                    break;
                                }
                            }

                            if (folderDL == false)
                            {
                                folders.Add(new Folder(folder, folder.ToLower(), foldername, 1, users[i].Username, lastDate));
                            }
                        }
                        folderIndex = folders.FindIndex(Folder => Folder.Path == folder);
                    }
                }
            }

            //removes duplicated in the folder list caused by slsk sometimes giving fully lowercased filepaths in the log file
            for (int i = 0; i < folders.Count; i++)
            {
                for (int j = 0; j < folders.Count; j++)
                {
                    if ((j != i) && (folders[i].Path.ToLower() == folders[j].Path.ToLower()))
                    {

                        //TODO: fix this bug that somehow adds a square bracket in the middle of the date
                        if (folders[i].LastTimeDownloaded.Contains("]"))
                        {
                            //Console.WriteLine("BUGGED SQUARE BRACKET in folders[i]" + folders[i].LastTimeDownloaded + "\n");
                            folders[i].LastTimeDownloaded = folders[i].LastTimeDownloaded.Replace(@"]", "");
                        }

                        //TODO: fix this bug that somehow adds a square bracket in the middle of the date
                        if (folders[j].LastTimeDownloaded.Contains("]"))
                        {
                            //Console.WriteLine("BUGGED SQUARE BRACKET in folders[j]" + folders[j].LastTimeDownloaded + "\n");
                            folders[j].LastTimeDownloaded = folders[j].LastTimeDownloaded.Replace(@"]", "");

                        }

                        DateTime date1 = default(DateTime);
                        DateTime date2 = default(DateTime);

                        try
                        {
                            date1 = DateTime.Parse(folders[i].LastTimeDownloaded);
                            date2 = DateTime.Parse(folders[j].LastTimeDownloaded);
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine(x.Message);
                            Console.WriteLine(folders[i].LastTimeDownloaded);
                            Console.WriteLine(folders[j].LastTimeDownloaded);
                        }

                        if (date1 >= date2)
                        {
                            folders[j].LatestUser = folders[i].LatestUser;
                            folders[j].LastTimeDownloaded = folders[i].LastTimeDownloaded;
                            folders[j].DownloadNum += folders[i].DownloadNum;
                            folders.Remove(folders[i]);
                        }
                        else
                        {
                            folders[i].LatestUser = folders[j].LatestUser;
                            folders[i].LastTimeDownloaded = folders[j].LastTimeDownloaded;
                            folders[i].DownloadNum += folders[j].DownloadNum;
                            folders.Remove(folders[j]);
                        }
                    }
                }
            }

            //sort and save database
            users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
            output = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

            output = JsonConvert.SerializeObject(folders, Formatting.Indented);
            File.WriteAllText(@Globals.UserDataFile + "\\fileData.txt", output);

            sr.Close();
            fs.Close();

            //retuirn info for output box
            String[] textBoxReturn = { info.Rtf, stats.Rtf };
            e.Result = textBoxReturn;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.PerformStep();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            LoadTree();

            String[] result = (String[])e.Result;
            richTextBox3.Rtf = result[0];
            richTextBox6.Rtf = result[1];
            progressBar1.Value = progressBar1.Maximum;
            Application.DoEvents();
        }

        //LoadTree
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            string input;
            List<Person> users = new List<Person>();
            List<Folder> folders = new List<Folder>();

            //Clear nodes so their are no dupes
            treeView1.Invoke(new Action(() => {
                treeView1.Nodes.Clear();
            }));

            //Load data files (dont parse if they dont exist)
            if (Globals.folders.Count > 0)
            {
                folders = Globals.folders;
                Globals.topFolders = new List<string>();

                //folder stats
                folders.Sort((x, y) => y.DownloadNum.CompareTo(x.DownloadNum));
                folders = folders.GroupBy(x => (x.Path + "\\" + x.Foldername).ToLower()).Select(x => x.First()).ToList();

                listView1.Invoke(new Action(() =>
                {
                    listView1.Items.Clear();
                }));

                int max;
                int index = folders[0].Path.IndexOf("\\", folders[0].Path.IndexOf("\\") + 1);
                string newPath = folders[0].Path.Substring(0, index + 1);

                if (folders.Count > 100)
                {
                    max = 100;
                }
                else
                {
                    max = folders.Count;
                }

                for (int i = 0; i < max; i++)
                {
                    newPath = folders[i].Path.Split('\\').Last();

                    if (checkBox6.Checked)
                    {
                        listView1.Invoke(new Action(() => {
                            string[] row = { folders[i].Path, folders[i].DownloadNum.ToString() };
                            ListViewItem item = new ListViewItem(row);
                            listView1.Items.Add(item);

                            Globals.topFolders.Add(folders[i].Path);
                        }));
                    }
                    else
                    {
                        listView1.Invoke(new Action(() =>
                        {
                            string[] row = { newPath, folders[i].DownloadNum.ToString() };
                            ListViewItem item = new ListViewItem(row);
                            listView1.Items.Add(item);

                            Globals.topFolders.Add(folders[i].Path);
                        }));
                    }
                }

                listView1.Invoke(new Action(() =>
                {
                    listView1.Columns[0].Width = listView1.Width - listView1.Columns[1].Width - SystemInformation.VerticalScrollBarWidth - 4;
                }));
            }

            if (Globals.users.Count > 0)
            {
                users = Globals.users;
                users.Sort((x, y) => y.TotalDownloadSize.CompareTo(x.TotalDownloadSize));

                richTextBox4.Invoke(new Action(() => {
                    richTextBox4.Text = "";
                }));

                int max;

                if (users.Count > 100)
                {
                    max = 100;
                }
                else
                {
                    max = users.Count;
                }

                listView2.Invoke(new Action(() => {
                    listView2.Items.Clear();
                    listView2.Visible = false;
                }));

                for (int i = 0; i < max; i++)
                {
                    string[] row = new string[3];
                    decimal userDownload = Convert.ToDecimal(users[i].TotalDownloadSize);

                    if (users[i].TotalDownloadSize < 1000)
                    {
                        row = new string[]{ users[i].Username, (userDownload).ToString("#.###") + " KB" };
                    }
                    if (users[i].TotalDownloadSize > 1000 && users[i].TotalDownloadSize < 1000000)
                    {
                        userDownload = Decimal.Divide(userDownload, 1000);
                        row = new string[] { users[i].Username, (userDownload).ToString("#.###") + " MB"};
                    }
                    if (users[i].TotalDownloadSize > 1000000)
                    {
                        userDownload = Decimal.Divide(userDownload, 1000000);
                        row = new string[] { users[i].Username, (userDownload).ToString("#.###") + " GB"};
                    }

                    ListViewItem item = new ListViewItem(row);
                    listView2.Invoke(new Action(() => {
                        listView2.Items.Add(item);
                    }));
                }

                listView2.Invoke(new Action(() => {
                    listView2.Visible = true;
                    listView2.Columns[0].Width = listView2.Width - listView2.Columns[1].Width - SystemInformation.VerticalScrollBarWidth - 4;
                }));

                //last user to download stat
                users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
                string lastuser = "Last user to download: " + users[users.Count - 1].Username;

                List<Download> userDownloads = SqliteDataAccess.LoadUserDownloads(users[users.Count - 1].Username);
                string lastDate = userDownloads[userDownloads.Count - 1].Date.Substring(1, userDownloads[userDownloads.Count - 1].Date.Length - 2);

                string[] dateSplit = lastDate.Split();
                lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];
                lastDate = "Last upload date: " + lastDate;

                //Number of users stat
                if (checkBox2.Checked == true)
                {
                    users.Sort((x, y) => y.TotalDownloadSize.CompareTo(x.TotalDownloadSize));
                }

                if (checkBox3.Checked == true)
                {
                    users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
                }

                if (checkBox4.Checked == true)
                {
                    users.Sort((x, y) => x.Username.CompareTo(y.Username));
                }

                if (checkBox5.Checked == true)
                {
                    users.Sort((x, y) => DateTime.Compare(y.convertDate(y.LastDate), x.convertDate(x.LastDate)));
                }

                double totalDownloadSize = 0;
                int downloadCount = 0;

                for (int i = 0; i < users.Count; i++)
                {
                    totalDownloadSize += users[i].TotalDownloadSize;
                    downloadCount += (int) users[i].DownloadNum;
                }

                //General stats textbox
                richTextBox4.Invoke(new Action(() => {
                    richTextBox4.AppendText("Number of users: " + users.Count + "\r\n\r\n\r\nFiles uploaded: " + downloadCount + "\r\n\r\n\r\n" + lastuser + "\r\n\r\n\r\n" + lastDate);
                }));

                //total upload stat
                decimal totalDlDecimal = Convert.ToDecimal(totalDownloadSize);
                totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000000);

                textBox7.Invoke(new Action(() => {
                    textBox7.Text = "\r\n" + (totalDlDecimal).ToString("#.##") + " GB";
                }));

                //Actual tree building
                treeView1.Invoke(new Action(() => {
                    treeView1.BeginUpdate();
                    //Actual tree building
                    for (int i = 0; i < users.Count; i++)
                    {
                        //parent node
                        treeView1.Nodes.Add(users[i].Username);
                        treeView1.Nodes[i].Nodes.Add("");
                    }
                    treeView1.EndUpdate();
                }));
            }
            
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Globals.loading)
            {
                Globals.loading = false;
                Globals.stopwatch.Stop();
                Console.WriteLine("Loading took: " + Globals.stopwatch.Elapsed.ToString(@"mm\:ss\.fff"));

                if(tabControl1.SelectedIndex == 1)
                {
                    treeView1.BringToFront();
                }
            } 
        }

        //"Clear output" button
        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox3.Rtf = "";
            richTextBox6.Rtf = "";
            if(progressBar1.Value >= progressBar1.Maximum)
            {
                progressBar1.Minimum = 0;
                progressBar1.Value = 0;
            }
        }

        private List<TreeNode> CurrentNodeMatches = new List<TreeNode>();

        private int LastNodeIndex = 0;
        private string LastSearchText;

        //Search button
        private void button3_Click(object sender, EventArgs e)
        {
            string searchText = this.textBox3.Text;
            Search(searchText);
        }

        private void Search(string searchText)
        {
            if (String.IsNullOrEmpty(searchText))
            {
                return;
            };

            if (LastSearchText != searchText)
            {
                //It's a new Search
                CurrentNodeMatches.Clear();
                LastSearchText = searchText;
                LastNodeIndex = 0;
                SearchNodes(searchText, treeView1.Nodes[0]);
                label15.Text = (LastNodeIndex+1) + "/" +CurrentNodeMatches.Count + " results";
                if(CurrentNodeMatches.Count > 1)
                {
                    button3.Text = "Next";
                    CurrentNodeMatches[0].EnsureVisible();
                }
            }

            if (LastNodeIndex >= 0 && CurrentNodeMatches.Count > 0 && LastNodeIndex < CurrentNodeMatches.Count)
            {
                TreeNode selectedNode = CurrentNodeMatches[LastNodeIndex];
                label15.Text = (LastNodeIndex + 1) + "/" + CurrentNodeMatches.Count + " results";
                LastNodeIndex++;
                this.treeView1.SelectedNode = selectedNode;
                this.treeView1.SelectedNode.Expand();
                this.treeView1.Select();

                selectedNode.EnsureVisible();

            }

            if(LastNodeIndex == CurrentNodeMatches.Count)
            {
                textBox3.Text = "";
                label15.Text = "";
                LastSearchText = "";
                button3.Text = "Search";
            }
        }

        private void SearchNodes(string SearchText, TreeNode StartNode)
        {
            while (StartNode != null)
            {
                Person user = Globals.users.Find(i => i.Username == StartNode.Text);

                if (StartNode.Text.ToLower().Contains(SearchText.ToLower()))
                {
                    CurrentNodeMatches.Add(StartNode);
                }

                List<Download> userDownloads = SqliteDataAccess.LoadUserDownloads(user.Username);

                foreach (Download download in userDownloads)
                {
                    if (download.Filename.ToLower().Contains(SearchText.ToLower()))
                    {
                        StartNode.ExpandAll();
                        foreach(TreeNode node in StartNode.Nodes)
                        {
                            if (download.Path.Remove(download.Path.LastIndexOf('\\')) == node.Text)
                            {
                                foreach(TreeNode node2 in node.Nodes)
                                {
                                    if (node2.Text.ToLower().Contains(SearchText.ToLower()) && !CurrentNodeMatches.Contains(node2))
                                    {
                                        CurrentNodeMatches.Add(node2);
                                    }
                                }
                            }
                        }
                    }
                }

                StartNode = StartNode.NextNode;
            }
        }

        //This controls what the info box shows (User stats, Folder stats etc)
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string selectedNodeText = e.Node.Text;
            bool userFolder = false;
            textBox4.Text = "";

            //Check if a user is selected
            for (int i = 0; i < Globals.users.Count; i++)
            {
                //Find what user is selected
                if (Globals.users[i].Username == selectedNodeText)
                {
                    //Format data (yes i do this a lot when i actually store the dates like this it breaks)
                    string lastDate = Globals.users[i].LastDate.Substring(1, Globals.users[i].LastDate.Length - 2);
                    string[] dateSplit = lastDate.Split();
                    lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                    //Determine if download size is in KB, mb or GB

                    string downloadString = "";
                    decimal totalDlDecimal = Convert.ToDecimal(Globals.users[i].TotalDownloadSize);

                    if (Globals.users[i].TotalDownloadSize < 1000)
                    {
                        downloadString = totalDlDecimal.ToString("#.###") + " KB";
                    }
                    if(Globals.users[i].TotalDownloadSize > 1000 && Globals.users[i].TotalDownloadSize < 1000000)
                    {
                        totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000);
                        downloadString = totalDlDecimal.ToString("#.###") + " MB";
                    }
                    if (Globals.users[i].TotalDownloadSize > 1000000)
                    {
                        totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000000);
                        downloadString = totalDlDecimal.ToString("#.###") + " GB";
                    }

                    //Update text
                    label5.Text = "User Information";
                    textBox4.AppendText("Username: " + Globals.users[i].Username + "\r\n\r\nNumber of downloads by user: " + Globals.users[i].DownloadNum +
                                        "\r\n\r\nLast download: " + lastDate + "\r\n\r\nTotal download size: " + downloadString);
                   
                    userFolder = true;
                    break;
                }
            }

            //Same as user for loop but for folders
            for (int i = 0; i < Globals.folders.Count; i++)
            {
                if (Globals.folders[i].Path == selectedNodeText)
                {

                    label5.Text = "Folder Information";
                    textBox4.AppendText("Folder name: " + Globals.folders[i].Foldername + "\r\n\r\nTimes downloaded from: " + Globals.folders[i].DownloadNum +
                                        "\r\n\r\nLast user to download: " + Globals.folders[i].LatestUser + "\r\n\r\nLast date downloaded: " +
                                        Globals.folders[i].LastTimeDownloaded +  "\r\n\r\nFull path: " + Globals.folders[i].Path);
                    
                    userFolder = true;
                    break;
                    
                }
            }

            //if the seleceted not isnt a person or folder it is a file
            if (userFolder == false)
            {
                TreeNode node = treeView1.SelectedNode;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                string user = node.Text;
                int index = Globals.users.FindIndex(person => person.Username == user);
                List<Download> userDownloads = SqliteDataAccess.LoadUserDownloads(Globals.users[index].Username);

                for (int i = 0; i < userDownloads.Count; i++)
                {
                    if (userDownloads[i].Filename == selectedNodeText)
                    {
                        //Determine if download size is in KB, mb or GB

                        string downloadString = "";
                        decimal totalDlDecimal = Convert.ToDecimal(userDownloads[i].Size);

                        if (userDownloads[i].Size < 1000)
                        {
                            downloadString = totalDlDecimal.ToString("#.###") + " KB";
                        }
                        if (userDownloads[i].Size > 1000 && userDownloads[i].Size < 1000000)
                        {
                            totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000);
                            downloadString = totalDlDecimal.ToString("#.###") + " MB";
                        }
                        if (userDownloads[i].Size > 1000000)
                        {
                            totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000000);
                            downloadString = totalDlDecimal.ToString("#.###") + " GB";
                        }

                        //the classic date re arange that could be a function 
                        string lastDate = userDownloads[i].Date.Substring(1, Globals.users[i].LastDate.Length - 2);
                        string[] dateSplit = lastDate.Split();
                        lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                        int downloadCount = SqliteDataAccess.CountDownload(userDownloads[i].Path);

                        label5.Text = "File Information";
                        textBox4.AppendText("Filename: " + userDownloads[i].Filename + 
                                            "\r\n\r\nFile size: " + downloadString +
                                            "\r\n\r\nNumber of times downloaded: " + downloadCount +
                                            "\r\n\r\nDate downloaded: " + lastDate.Replace("]", "") +
                                            "\r\n\r\nFile path: " + userDownloads[i].Path);
                        break;
                    }
                }
            }

        }

        //"Database save location: " button in settings (opens a folder pick dialog)
        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                Globals.UserDataFile = dialog.SelectedPath;
                label4.Text = ("Database save location: " + dialog.SelectedPath);
            }
        }

        //Similar to last
        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                Globals.SlskFolders.Add(dialog.SelectedPath);
                textBox5.AppendText(dialog.SelectedPath + "\r\n");
            }

        }

        //Detects enter key up for search
        //TODO: make it so the cursor goes back into the textbox after you search once
        //this will allow for further searches
        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string searchText = this.textBox3.Text;
                searchText = searchText.Replace("\n", "").Replace("\r", "");
                textBox3.Text = searchText;
                
                Search(searchText);

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        //Save settings button (writes info to file)
        private void button6_Click(object sender, EventArgs e)
        {
            if (File.Exists("settings.ini"))
            {
                File.Delete("settings.ini");
            }

            File.WriteAllText(@"settings.ini", Globals.UserDataFile + Environment.NewLine + checkBox6.Checked.ToString() + Environment.NewLine);
            if (Globals.SlskFolders.Count != 0)
            {
                for (int i = 0; i < Globals.SlskFolders.Count; i++)
                {
                    if (i != Globals.SlskFolders.Count - 1)
                    {
                        File.AppendAllText(@"settings.ini", Globals.SlskFolders[i] + Environment.NewLine);
                    }
                    else
                    {
                        File.AppendAllText(@"settings.ini", Globals.SlskFolders[i]);
                    }
                    textBox1.Text = "";
                    textBox4.Text = "";
                    textBox7.Text = "";
                }
            }

            Application.Restart();
            Environment.Exit(0);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            treeView1.BeginUpdate();
            if (checkBox1.Checked == true)
            {
                treeView1.ExpandAll();
                treeView1.Nodes[0].EnsureVisible();
            }
            else
            {
                treeView1.CollapseAll();
                treeView1.Nodes[0].EnsureVisible();
            }
            treeView1.EndUpdate();
        }
       

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true )
            {
                checkBox3.Checked = false;
                checkBox5.Checked = false;
                checkBox4.Checked = false;

                LoadTree();

                if (checkBox1.Checked == true)
                {
                    treeView1.ExpandAll();
                    treeView1.SelectedNode = treeView1.Nodes[0];
                    treeView1.Nodes[0].EnsureVisible();
                }
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {
                checkBox2.Checked = false;
                checkBox5.Checked = false;
                checkBox4.Checked = false;

                LoadTree();

                if (checkBox1.Checked == true)
                {
                    treeView1.ExpandAll();
                    treeView1.SelectedNode = treeView1.Nodes[0];
                    treeView1.Nodes[0].EnsureVisible();
                }
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked == true)
            {
                checkBox2.Checked = false;
                checkBox4.Checked = false;
                checkBox3.Checked = false;

                LoadTree();

                if (checkBox1.Checked == true)
                {
                    treeView1.ExpandAll();
                    treeView1.SelectedNode = treeView1.Nodes[0];
                    treeView1.Nodes[0].EnsureVisible();
                }
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {
                checkBox3.Checked = false;
                checkBox5.Checked = false;
                checkBox2.Checked = false;

                LoadTree();

                if (checkBox1.Checked == true)
                {
                    treeView1.ExpandAll();
                    treeView1.SelectedNode = treeView1.Nodes[0];
                    treeView1.Nodes[0].EnsureVisible();
                }
            }
        }

        //remove selected button
        private void button7_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure to delete this item?","Confirm Remove",MessageBoxButtons.YesNo);

            if (confirmResult == DialogResult.Yes)
            {
                string selectedNodeText = treeView1.SelectedNode.Text;

                TreeNode node = treeView1.SelectedNode;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                int index = Globals.users.FindIndex(person => person.Username == node.Text);

                //if the selection is a folder
                for (int i = 0; i < Globals.folders.Count; i++)
                {
                    if (Globals.folders[i].Path == selectedNodeText)
                    {                                               
                        List<int> toRemove = new List<int>();

                        for(int j = 0; j < Globals.users[index].DownloadList.Count; j++)
                        {
                            string newPath = Globals.users[index].DownloadList[j].Path.Substring(0, Globals.users[index].DownloadList[j].Path.LastIndexOf("\\"));

                            if (newPath == selectedNodeText)
                            {                               
                                toRemove.Add(j);
                            }
                        }

                        if(toRemove.Count > 0)
                        {
                            for(int k = 0; k < toRemove.Count; k++)
                            {
                                int removeIndex = toRemove[k] - k;

                                if (Globals.users[index].DownloadList.Count > 1)
                                {
                                    Globals.users[index].DownloadList.RemoveAt(removeIndex);
                                    Globals.users[index].DownloadNum -= 1;
                                }

                                else if(Globals.users[index].DownloadList.Count == 1){
                                    //Changing selected node text to fully remove user when they only have one more download to remove
                                    selectedNodeText = node.Text;
                                    break;
                                }
                            }
                        }

                        if (File.Exists("settings.ini"))
                        {
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            string output = JsonConvert.SerializeObject(Globals.users, Formatting.Indented);
                            System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                            ParseData();
                        }
                        else
                        {
                            textBox4.Text = "Please initialize your settings";
                            textBox1.Text = "Please initialize your settings";
                            textBox7.Text = "\r\nPlease initialize your settings";
                        }
                        break;
                    }
                }

                for (int j = 0; j < Globals.users[index].DownloadList.Count; j++)
                {
                    string newPath = Globals.users[index].DownloadList[j].Path.Substring(0, Globals.users[index].DownloadList[j].Path.LastIndexOf("\\"));

                    if (Globals.users[index].DownloadList[j].Filename == selectedNodeText && Globals.users[index].DownloadList.Count > 1)
                    {
                        Globals.users[index].DownloadList.RemoveAt(j);
                        Globals.users[index].DownloadNum -= 1;

                        string output = JsonConvert.SerializeObject(Globals.users, Formatting.Indented);
                        System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                        if (File.Exists("settings.ini"))
                        {
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            ParseData();
                        }
                        else
                        {
                            textBox4.Text = "Please initialize your settings";
                            textBox1.Text = "Please initialize your settings";
                            textBox7.Text = "\r\nPlease initialize your settings";
                        }
                        break;
                    }
                    else if (Globals.users[index].DownloadList.Count == 1)
                    {
                        //Changing selected node text to fully remove user when they only have one more download to remove
                        selectedNodeText = node.Text;
                        break;

                    }
                }

                //if section is a user completly remove the user
                for (int i = 0; i < Globals.users.Count; i++)
                {
                    if (selectedNodeText == Globals.users[i].Username)
                    {
                        Globals.users.RemoveAt(i);
                        string output = JsonConvert.SerializeObject(Globals.users, Formatting.Indented);
                        System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                        if (File.Exists("settings.ini"))
                        {
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            ParseData();
                        }
                        else
                        {
                            textBox4.Text = "Please initialize your settings";
                            textBox1.Text = "Please initialize your settings";
                            textBox7.Text = "\r\nPlease initialize your settings";
                        }
                        break;
                    }
                }    
            }
        }

        //Detect enter key for searching
        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox3.Text != "")
            {
                string searchText = this.textBox3.Text;
                searchText = searchText.Replace("\n", "").Replace("\r", "");
                textBox3.Text = searchText;

                Search(searchText);

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        //Function to scrable names in the tree. useful for taking screenshots
        public void ScrambleNames()
        {
            for (int i = 0; i < Globals.users.Count; i++)
            {
                Globals.users[i].Username = new string((Globals.users[i].Username).ToCharArray().OrderBy(x => Guid.NewGuid()).ToArray());
            }
            
            string output = JsonConvert.SerializeObject(Globals.users, Formatting.Indented);
            File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

        }

        //Refresh Tree button
        private void button8_Click(object sender, EventArgs e)
        {
            if (File.Exists("settings.ini"))
            {
                if (File.Exists("parsingData.txt"))
                {
                    File.Delete("parsingData.txt");
                }

                textBox3.Text = "";
                label15.Text = "";
                LastSearchText = "";
                button3.Text = "Search";
                CurrentNodeMatches.Clear();
                LastNodeIndex = 0;

                ParseData();
                checkBox1.Checked = false;
            }
            else
            {
                textBox4.Text = "Please initialize your settings";
                textBox1.Text = "Please initialize your settings";
                textBox7.Text = "\r\nPlease initialize your settings";
            }
        }

        //remove last folder option in settings
        private void button9_Click(object sender, EventArgs e)
        {
            //If no folder was added, don't continue
            if(Globals.SlskFolders.Count > 0){
                textBox5.Text = "";

                Globals.SlskFolders.RemoveAt(Globals.SlskFolders.Count - 1);

                for (int i = 0; i < Globals.SlskFolders.Count; i++)
                {
                    textBox5.AppendText(Globals.SlskFolders[i] + "\r\n");
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (progressBar1.Value >= progressBar1.Maximum)
            {
                progressBar1.Minimum = 0;
                progressBar1.Value = 0;
            }

            if(tabControl1.SelectedIndex == 1 && !Globals.loading)
            {
                treeView1.BringToFront();
            }
            else
            {
                treeView1.SendToBack();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            int index = listView1.SelectedItems[0].Index;
            try
            {
                Process.Start(Globals.topFolders[index]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void openSelectedNodeFileFolder(TreeNode selected)
        {
            
            try
            {
                if (selected.Parent != null)
                {
                    if (selected.Nodes.Count > 0)
                    {

                        Process.Start(selected.Text);

                    }
                    else
                    {
                        string path = selected.Parent.Text + "\\" + selected.Text;
                        Process.Start(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            TreeNode selected = treeView1.SelectedNode;
            openSelectedNodeFileFolder(selected);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                string selected = listView2.SelectedItems[0].Text;
                tabControl1.SelectedIndex = 1;
                Application.DoEvents();
                Search(selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            openSelectedNodeFileFolder(e.Node);
        }
    }

    //Global variables
    public static class Globals
    {
        public static List<Person> users = new List<Person>();
        public static List<Folder> folders = new List<Folder>();

        public static bool initSettings;
        public static bool loading;
        public static string UserDataFile;
        public static List<string> SlskFolders = new List<string>();
        public static List<string> topFolders = new List<string>();
        public static Stopwatch stopwatch;
    }

    public class FileNum
    {
        public string Filename { get; set; }
        public int DownloadNum { get; set; }
        
        public FileNum(string filename, int downloadNum)
        {
            Filename = filename;
            DownloadNum = downloadNum;
        }
    }

    public class Folder
    {
        public Int64 Id { get; set; }
        public string Path { get; set; }
        public string PathToLower { get; set; }
        public string Foldername { get; set; }
        public Int64 DownloadNum { get; set; }
        public string LatestUser { get; set; }
        public string LastTimeDownloaded { get; set; }

        public Folder(Int64 id, string latestUser, string foldername, string path, string pathToLower, Int64 downloadNum, string lastTimeDownloaded)
        {
            Id = id;
            Path = path;
            PathToLower = pathToLower;
            Foldername = foldername;
            DownloadNum = downloadNum;
            LatestUser = latestUser;
            LastTimeDownloaded = lastTimeDownloaded;
        }

        [JsonConstructor]
        public Folder(string path, string pathToLower, string foldername, Int64 downloadNum, string latestUser, string lastTimeDownloaded)
        {
            Path = path;
            PathToLower = pathToLower;
            Foldername = foldername;
            DownloadNum = downloadNum;
            LatestUser = latestUser;
            LastTimeDownloaded = lastTimeDownloaded;
        }
    }

    public class Download
    {
        public Int64 Id { get; set; }
        public string Username { get; set; }
        public string Filename { get; set; }
        public string Path { get; set; }
        public double Size { get; set; }
        public string Date { get; set; }

        public Download(Int64 id, string username, string filename, string path, double size, string date)
        {
            Id = id;
            Username = username;
            Filename = filename;
            Path = path;
            Size = size;
            Date = date;
        }

        public Download(string username, string filename, string path, double size, string date)
        {
            Username = username;
            Filename = filename;
            Path = path;
            Size = size;
            Date = date;
        }
    }
    public class Person
    {
        public Int64 Id { get; }
        public string Username { get; set; }
        public Int64 DownloadNum { get; set; }
        public double TotalDownloadSize { get; set; }
        public string LastDate { get; set; }
        public List<Download> DownloadList { get; set; }

        public Person(Int64 id, string username, Int64 downloadNum, double totalDownloadSize, string lastDate)
        {
            Id = id;
            Username = username;
            DownloadNum = downloadNum;
            TotalDownloadSize = totalDownloadSize;
            LastDate = lastDate;
        }

        [JsonConstructor]
        public Person(string username, Int64 downloadNum, double totalDownloadSize, string lastDate, List<Download> downloadlist)
        {
            Username = username;
            DownloadNum = downloadNum;
            TotalDownloadSize = totalDownloadSize;
            DownloadList = downloadlist;
            LastDate = lastDate;
        }
        //Converts the LastDate to a time format which can be parsed
        public DateTime convertDate(string date)
        {
            string[] dateSplit;
            DateTime ParsedDate;

            date = LastDate.Substring(1, this.LastDate.Length - 2);
            dateSplit = date.Split();
            date = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

            //Try to parse date for known cultural formats
            if (DateTime.TryParse(date, out ParsedDate))
            {
                return ParsedDate;
            }
            //Error if datestring not found
            MessageBox.Show("Given datestring is in a format that is not supported. Please report it to the github page with your transfer queue.");
            System.Diagnostics.Process.Start("https://github.com/mrusse/Slsk-Upload-Stats-Tracker/issues");
            throw new NotSupportedException("Given datestring is in a format that is not supported. Please report it to the github page with your transfer queue.");
        }
    }

    //Main parsing class (if called it will add new data to the database and rebuild the folder stats from that database)
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SlskTransferStats());
        }
    }
}