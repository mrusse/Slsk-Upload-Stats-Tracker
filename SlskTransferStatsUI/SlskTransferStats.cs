using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

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
            
            this.Icon = new Icon("icon.ico");

            //Only load the tree if settings exist TODO: tell the user to set their settings
            if (File.Exists("settings.ini"))
            {
                //Read settings
                textBox1.Text = "";
                FileStream fs = new FileStream("settings.ini", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);

                string str = sr.ReadLine();

                Globals.UserDataFile = str;

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

                //Load tree (this does not call the parser for folder information, that only happens on the database button click)
                loadTree();

            }
            else
            {
                textBox4.Text = "Please initialize your settings";
                textBox1.Text = "Please initialize your settings";
                textBox7.Text = "\r\nPlease initialize your settings";

            }

                    

        }
        
        //Outside the class because I access them further down
        List<Person> users = new List<Person>();
        List<Folder> folders = new List<Folder>();

        public List<Person> loadTree()
        {

            string folder;
            string input;
            int folderCount;

            //Clear nodes so their are no dupes
            treeView1.Nodes.Clear();

            //Load data files (dont parse if they dont exist)
            if (File.Exists(Globals.UserDataFile + "\\fileData.txt"))
            {
                input = System.IO.File.ReadAllText(@Globals.UserDataFile + "\\fileData.txt");
                folders = JsonConvert.DeserializeObject<List<Folder>>(input);

                //folder stats
                folders.Sort((x, y) => y.DownloadNum.CompareTo(x.DownloadNum));

                richTextBox1.Text = "";
                textBox9.Text = "";

                List<string> inFolderList = new List<string>();
                
                inFolderList.Add(folders[0].Path);
                
                int index = folders[0].Path.IndexOf("\\", folders[0].Path.IndexOf("\\") + 1);
                string newPath = folders[0].Path.Substring(0, index + 1);
                richTextBox1.AppendText(newPath + "...\\" +folders[0].Foldername + ":\r\n");
                textBox9.AppendText(folders[0].DownloadNum.ToString() + "\r\n");

                bool inFolder = false;

                int max;
                if(folders.Count > 35)
                {
                    max = 35;
                }
                else
                {
                    max = folders.Count;
                }

                for (int i = 1; i < max; i++)
                {
                    //Console.WriteLine(folders[i].Path);
                    inFolder = false;
                    for (int j = 0; j < inFolderList.Count; j ++)
                    {
                        //Console.WriteLine("| " + j + " | " + inFolderList[j]);
                        if (folders[i].Path == inFolderList[j] || folders[i].Path.ToLower() == inFolderList[j].ToLower())
                        {
                            //Console.WriteLine(folders[i].Path + " " + inFolderList[j]);
                            inFolder = true;
                            i++;
                            break;
                        }

                    }

                    if (inFolder == false)
                    {
                        inFolderList.Add(folders[i].Path);

                        index = folders[i].Path.IndexOf("\\", folders[i].Path.IndexOf("\\") + 1);
                        newPath = folders[i].Path.Substring(0,index + 1);

                        richTextBox1.AppendText(newPath + "...\\" + folders[i].Foldername + "\r\n");
                        textBox9.AppendText(folders[i].DownloadNum.ToString() + "\r\n");
                    }

                }

                folders = JsonConvert.DeserializeObject<List<Folder>>(input);

            }

            if (File.Exists(Globals.UserDataFile + "\\userData.txt"))
            {
                
                input = System.IO.File.ReadAllText(Globals.UserDataFile + "\\userData.txt");
                users = JsonConvert.DeserializeObject<List<Person>>(input);

                //Top 10 users stat
                users.Sort((x, y) => y.TotalDownloadSize.CompareTo(x.TotalDownloadSize));
                richTextBox2.Text = "";
                richTextBox4.Text = "";
                richTextBox5.Text = "";

                int max;

                if (users.Count > 28)
                {
                    max = 28;
                }
                else
                {
                    max = users.Count;
                }

                for (int i = 0; i < max; i++)
                {
                    decimal userDownload = Convert.ToDecimal(users[i].TotalDownloadSize);
                    userDownload = Decimal.Divide(userDownload, 1000000);
                    richTextBox2.AppendText(users[i].Username + ":\r\n");
                    richTextBox5.AppendText((userDownload).ToString("#.###") + " GB\r\n");

                }

                //Right justify user download size list
                richTextBox5.SelectAll();
                richTextBox5.SelectionAlignment = HorizontalAlignment.Right;
                richTextBox5.DeselectAll();

                //last user to download stat
                users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
                string lastuser = "Last user to download: " + users[users.Count - 1].Username;

                //last user to download stat
                //this is fucked
                string lastDate = users[users.Count - 1].DownloadList[users[users.Count - 1].DownloadList.Count - 1].Date
                                                        .Substring(1, users[users.Count - 1].DownloadList[users[users.Count - 1].DownloadList.Count - 1].Date.Length - 2);

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

                treeView1.BeginUpdate();
                long totalDownloadSize = 0;
                int downloadCount = 0;
                

                //Actual tree building
                for (int i = 0; i < users.Count; i++)
                {
                    //parent node
                    treeView1.Nodes.Add(users[i].Username);                   
                    folderCount = -1;

                    for (int j = 0; j < users[i].DownloadList.Count; j++)
                    {
                        //get folder
                        folder = users[i].DownloadList[j].Path.Substring(0, users[i].DownloadList[j].Path.LastIndexOf("\\"));

                        if (j == 0 || (users[i].DownloadList[j - 1].Path.Substring(0, users[i].DownloadList[j - 1].Path.LastIndexOf("\\")) != folder))
                        {
                            //first child (folders)
                            treeView1.Nodes[i].Nodes.Add(folder);
                            folderCount++;

                        }
                        //second child (songs)
                        treeView1.Nodes[i].Nodes[folderCount].Nodes.Add(users[i].DownloadList[j].Filename);
                        totalDownloadSize += users[i].DownloadList[j].Size;                        
                        downloadCount++;

                    }


                }

                //General stats textbox
                richTextBox4.AppendText("Number of users: " + users.Count + "\r\n\r\n\r\nFiles uploaded: " + downloadCount + "\r\n\r\n\r\n" + lastuser + "\r\n\r\n\r\n" + lastDate);

                //total upload stat
                decimal totalDlDecimal = Convert.ToDecimal(totalDownloadSize);
                totalDlDecimal = Decimal.Divide(totalDlDecimal, 1000000);
                textBox7.Text = "\r\n" + (totalDlDecimal).ToString("#.##") + " GB";

                //search text
                label15.Text = "";

                treeView1.SelectedNode = treeView1.Nodes[0];
                treeView1.Nodes[0].EnsureVisible();
                treeView1.EndUpdate();
            }

            //I used this at one point not sure if its used anymore lol
            return users;

        }

        //"Submit to database" button click event (writes text to file and parses it, then adds to database)
        private void button1_Click(object sender, EventArgs e)
        {
            Program.FileRead wr = new Program.FileRead();
            File.WriteAllText("parsingData.txt", textBox1.Text);
            textBox1.Text = "";
            richTextBox3.Text = "";
            if (File.Exists("settings.ini"))
            {
                textBox1.Text = "";
                richTextBox3.Rtf = wr.ParseData().Rtf;
                loadTree();
            }
            else
            {
                textBox4.Text = "Please initialize your settings";
                textBox1.Text = "Please initialize your settings";
                textBox7.Text = "\r\nPlease initialize your settings";
            }

        }

        //"Clear output" button
        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox3.Rtf = "";
        }

        //These variables are for searching  source: "https://stackoverflow.com/questions/11530643/treeview-search" 
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
                if (StartNode.Text.ToLower().Contains(SearchText.ToLower()))
                {
                    CurrentNodeMatches.Add(StartNode);
                }
                if (StartNode.Nodes.Count != 0)
                {
                    SearchNodes(SearchText, StartNode.Nodes[0]);//Recursive Search 
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
            for (int i = 0; i < users.Count; i++)
            {
                //Find what user is selected
                if (users[i].Username == selectedNodeText)
                {

                    //Format data (yes i do this a lot when i actually store the dates like this it breaks)
                    string lastDate = users[i].LastDate.Substring(1, users[i].LastDate.Length - 2);
                    string[] dateSplit = lastDate.Split();
                    lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                    //Update text
                    label5.Text = "User Information";
                    textBox4.AppendText("Username: " + users[i].Username + "\r\n\r\nNumber of downloads by user: " + users[i].DownloadNum +
                                        "\r\n\r\nLast download: " + lastDate + "\r\n\r\nTotal download size: " + users[i].TotalDownloadSize + " kb");
                    userFolder = true;
                    break;

                }

            }

            //Same as user for loop but for folders
            for (int i = 0; i < folders.Count; i++)
            {

                if (folders[i].Path == selectedNodeText)
                {

                    label5.Text = "Folder Information";
                    textBox4.AppendText("Folder name: " + folders[i].Foldername + "\r\n\r\nTimes downloaded from: " + folders[i].DownloadNum +
                                        "\r\n\r\nLast user to download: " + folders[i].LatestUser + "\r\n\r\nLast date downloaded: " + 
                                        folders[i].LastTimeDownloaded +  "\r\n\r\nFull path: " + folders[i].Path);
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

                int index = users.FindIndex(person => person.Username == user);

                for (int i = 0; i < users[index].DownloadList.Count; i++)
                {

                    if (users[index].DownloadList[i].Filename == selectedNodeText)
                    {
                        //the classic date re arange that could be a function 
                        string lastDate = users[index].DownloadList[i].Date.Substring(1, users[i].LastDate.Length - 2);
                        string[] dateSplit = lastDate.Split();
                        lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                        int downloadCount = getSongDownloadNum(users[index].DownloadList[i].Filename);

                        label5.Text = "File Information";
                        textBox4.AppendText("Filename: " + users[index].DownloadList[i].Filename + 
                                            "\r\n\r\nFile size: " + users[index].DownloadList[i].Size +
                                            " kb\r\n\r\nNumber of times downloaded: " + downloadCount +
                                            "\r\n\r\nDate downloaded: " + lastDate +
                                            "\r\n\r\nFile path: " + users[index].DownloadList[i].Path);
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
            System.IO.File.WriteAllText(@"settings.ini", Globals.UserDataFile + Environment.NewLine);
            if (Globals.SlskFolders.Count != 0)
            {
                for (int i = 0; i < Globals.SlskFolders.Count; i++)
                {
                    if (i != Globals.SlskFolders.Count - 1)
                    {
                        System.IO.File.AppendAllText(@"settings.ini", Globals.SlskFolders[i] + Environment.NewLine);
                    }
                    else
                    {
                        System.IO.File.AppendAllText(@"settings.ini", Globals.SlskFolders[i]);
                    }
                    textBox1.Text = "";
                    textBox4.Text = "";
                    textBox7.Text = "";
                }
            }     

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

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
        }
       

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true )
            {
                checkBox3.Checked = false;
                checkBox5.Checked = false;
                checkBox4.Checked = false;

                loadTree();

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

                loadTree();

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

                loadTree();

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

                loadTree();

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
                Program.FileRead wr = new Program.FileRead();

                TreeNode node = treeView1.SelectedNode;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                int index = users.FindIndex(person => person.Username == node.Text);

                //if the selection is a folder
                for (int i = 0; i < folders.Count; i++)
                {

                    if (folders[i].Path == selectedNodeText)
                    {                                               

                        List<int> toRemove = new List<int>();

                        for(int j = 0; j < users[index].DownloadList.Count; j++)
                        {

                            string newPath = users[index].DownloadList[j].Path.Substring(0, users[index].DownloadList[j].Path.LastIndexOf("\\"));

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

                                if (users[index].DownloadList.Count > 1)
                                {
                                    users[index].DownloadList.RemoveAt(removeIndex);
                                    users[index].DownloadNum -= 1;
                                }

                                else if(users[index].DownloadList.Count == 1){

                                    //Changing selected node text to fully remove user when they only have one more download to remove
                                    selectedNodeText = node.Text;
                                    break;

                                }

                            }

                        }

                        if (File.Exists("settings.ini"))
                        {
                            textBox1.Text = "";
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            string output = JsonConvert.SerializeObject(users);
                            System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);
                            wr.ParseData();
                            loadTree();
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

                for (int j = 0; j < users[index].DownloadList.Count; j++)
                {

                    string newPath = users[index].DownloadList[j].Path.Substring(0, users[index].DownloadList[j].Path.LastIndexOf("\\"));

                    if (users[index].DownloadList[j].Filename == selectedNodeText && users[index].DownloadList.Count > 1)
                    {
                        users[index].DownloadList.RemoveAt(j);
                        users[index].DownloadNum -= 1;

                        string output = JsonConvert.SerializeObject(users);
                        System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                        wr = new Program.FileRead();
                        if (File.Exists("settings.ini"))
                        {
                            textBox1.Text = "";
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            wr.ParseData();
                            loadTree();
                        }
                        else
                        {
                            textBox4.Text = "Please initialize your settings";
                            textBox1.Text = "Please initialize your settings";
                            textBox7.Text = "\r\nPlease initialize your settings";
                        }

                        break;
                    }
                    else if (users[index].DownloadList.Count == 1)
                    {

                        //Changing selected node text to fully remove user when they only have one more download to remove
                        selectedNodeText = node.Text;
                        break;

                    }

                }

                //if section is a user completly remove the user
                for (int i = 0; i < users.Count; i++)
                {
                    if (selectedNodeText == users[i].Username)
                    {
                        users.RemoveAt(i);
                        string output = JsonConvert.SerializeObject(users);
                        System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                        wr = new Program.FileRead();
                        if (File.Exists("settings.ini"))
                        {
                            textBox1.Text = "";
                            if (File.Exists("parsingData.txt"))
                            {
                                File.Delete("parsingData.txt");
                            }
                            wr.ParseData();
                            loadTree();
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


        private int getSongDownloadNum(string songName)
        {

            List<string> allDownloads = new List<string>();
            int count = 0;

            for (int i = 0; i < users.Count; i++)
            {
                for (int j = 0; j < users[i].DownloadList.Count; j++)
                {
                    allDownloads.Add(users[i].DownloadList[j].Filename);
                }
            }

            for(int i = 0; i < allDownloads.Count; i++)
            {
                if(songName.ToLower() == allDownloads[i].ToLower())
                {
                    count++;
                }
            }


            return count;

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
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Username = new string((users[i].Username).ToCharArray().OrderBy(x => Guid.NewGuid()).ToArray());
            }
            
            string output = JsonConvert.SerializeObject(users);
            System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

        }

        //Refresh Tree button
        private void button8_Click(object sender, EventArgs e)
        {
            if (File.Exists("settings.ini"))
            {
                textBox1.Text = "";
                if (File.Exists("parsingData.txt"))
                {
                    File.Delete("parsingData.txt");
                }
                Program.FileRead wr = new Program.FileRead();
                wr.ParseData();
                loadTree();
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
    }

    //Global variables
    public static class Globals
    {
        public static String UserDataFile;
        public static List<string> SlskFolders = new List<string>();
        public static bool initSettings;

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
        public string Path { get; set; }
        public string Foldername { get; set; }
        public int DownloadNum { get; set; }
        public string LatestUser { get; set; }
        public string LastTimeDownloaded { get; set; }

        public Folder(string path, string foldername, int downloadNum, string latestUser, string lastTimeDownloaded)
        {
            Path = path;
            Foldername = foldername;
            DownloadNum = downloadNum;
            LatestUser = latestUser;
            LastTimeDownloaded = lastTimeDownloaded;
        }
    }

    public class Download
    {
        public string Filename { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public string Date { get; set; }

        public Download(string filename, string path, long size, string date)
        {
            Filename = filename;
            Path = path;
            Size = size;
            Date = date;
        }
    }

    public class Person
    {
        public string Username { get; set; }
        public int DownloadNum { get; set; }
        public long TotalDownloadSize { get; set; }
        public string LastDate { get; set; }
        public List<Download> DownloadList { get; set; }

        public Person(string username, int downloadNum, long totalDownloadSize, string lastDate, List<Download> downloadlist)
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
        public class FileRead
        {

            public RichTextBox ParseData()
            {
                //Open files TODO: change test2 and get rid of parser file or change
                FileStream fs = new FileStream("parsingData.txt", FileMode.OpenOrCreate, FileAccess.Read);
                FileStream fs2 = new FileStream("parsedData.txt", FileMode.Create, FileAccess.Write);
                StreamReader sr = new StreamReader(fs);
                StreamWriter sw = new StreamWriter(fs2);
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
                long size;
                string folder;
                RichTextBox info = new RichTextBox();             
                int dateLength;
                string[] dateSplit;              

                //loop through file made by input data
                while (str != null)
                {
                    //get length of the date section of the file (this changes depending on day of month)
                    dateLength = (str.Substring(0, str.IndexOf("]") + 1)).Length -1;
                    //Look for the term "Queue" and then on the next line the word "Queued" then parse that line
                    if (str.IndexOf("Queue", dateLength + 2, 5) == dateLength + 2)
                    {
                        queued = str;
                        str = sr.ReadLine();
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
                                for(int i = 0; i < Globals.SlskFolders.Count; i++)
                                {
                                    string localPath = Globals.SlskFolders[i].Substring(Globals.SlskFolders[i].LastIndexOf("\\") + 1);
                                    if (path.Contains(localPath) || path.Contains(localPath.ToLower()))
                                    {
                                        drive = Globals.SlskFolders[i].Substring(0, Globals.SlskFolders[i].LastIndexOf("\\"));
                                        //Console.WriteLine(drive);
                                        break;
                                     }
                                }
                                //add drive to path
                                path = drive + path;

                                filename = queued.Substring(queued.LastIndexOf("\\") + 1);

                                if (File.Exists(path)){
                                    size = new FileInfo(path).Length / 1000;
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

                                downloads.Add(new Download(filename, path, size, date));
                                users.Add(new Person(username, 1, size, date, downloads));
                                index = users.FindIndex(person => person.Username == username);

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
                                        //Console.WriteLine(drive);
                                        break;
                                    }
                                }
                                path = drive + path;

                                

                                filename = queued.Substring(queued.LastIndexOf("\\") + 1);

                                if (File.Exists(path)){
                                    size = new FileInfo(path).Length / 1000;
                                }
                                else
                                {
                                    size = 0;
                                    //Console.WriteLine(path);
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

                                    

                                    users[index].DownloadList.Add(new Download(filename, path, size, date));
                                    users[index] = new Person(username,
                                                              users[index].DownloadNum + 1,
                                                              users[index].TotalDownloadSize += size,
                                                              users[index].DownloadList[users[index].DownloadList.Count - 1].Date,
                                                              users[index].DownloadList);

                                    //info for the output textbox
                                    info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                                    info.SelectionColor = Color.White;
                                    info.AppendText("\tNew download for \"");
                                    info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
                                    info.SelectionColor = Color.White;
                                    info.AppendText(users[index].Username);
                                    info.SelectionFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
                                    info.SelectionColor = Color.White;
                                    info.AppendText("\": " + users[index].DownloadList[users[index].DownloadList.Count - 1].Filename +"\r\n");


                                }

                            }

                        }

                    }

                    str = sr.ReadLine();
                }

                
                string foldername;
                bool folderDL;
                int folderIndex;
                string output;
                string lastDate;


                //Get folder information from the database TODO: remove the parser file output
                for (int i = 0; i < users.Count; i++)
                {


                    sw.WriteLine("User: " + users[i].Username + ", Number of downloads: " + users[i].DownloadNum + ", " +
                                 "Last Download: " + users[i].LastDate + ", Total download size: " + users[i].TotalDownloadSize +
                                 " kb" + "\n\n\tUsers Downloads:\n");



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

                                folders.Add(new Folder(folder, foldername, 1, users[i].Username, lastDate));
                            }
                            else
                            {
                                folderDL = false;

                                for (int k = 0; k < folders.Count; k++)
                                {
                                  
                                    if (folders[k].Path == folder && (folders[k].LatestUser != users[i].Username))
                                    {                        
                                        folders[k] = new Folder(folder, foldername, folders[k].DownloadNum += 1, users[i].Username, lastDate);                                       
                                        folderDL = true;
                                        break;
                                    }

                                }

                                if (folderDL == false)
                                {
 
                                    folders.Add(new Folder(folder, foldername, 1, users[i].Username, lastDate));


                                }


                            }

                            folderIndex = folders.FindIndex(Folder => Folder.Path == folder);
                            sw.WriteLine("\t" + folder + "\tTotal folder download count: " + folders[folderIndex].DownloadNum);


                        }


                        sw.WriteLine("\t\t" + users[i].DownloadList[j].Filename + ", " + users[i].DownloadList[j].Size + " kb ");

                    }
                    sw.WriteLine("\n");

                }

                //removes duplicated in the folder list caused by slsk sometimes giving fully lowercased filepaths in the log file
                for (int i = 0; i < folders.Count; i++)
                {
                    for (int j = 0; j < folders.Count; j++)
                    {
                        if((j != i) && (folders[i].Path.ToLower() == folders[j].Path.ToLower())){
                            if(i < j)
                            {
 
                                //TODO: fix this bug that somehow adds a square bracket in the middle of the date
                                if (folders[i].LastTimeDownloaded.Contains("]"))
                                {
                                    Console.WriteLine("BUGGED SQUARE BRACKET " + folders[i].LastTimeDownloaded + "\n");
                                    folders[i].LastTimeDownloaded = folders[i].LastTimeDownloaded.Replace(@"]", "");

                                }

                                DateTime date1 = DateTime.Parse(folders[i].LastTimeDownloaded);
                                DateTime date2 = DateTime.Parse(folders[j].LastTimeDownloaded);
                                

                                if(date1 >= date2)
                                {
                                    folders[j].LatestUser = folders[i].LatestUser;
                                    folders[j].LastTimeDownloaded = folders[i].LastTimeDownloaded;
                                }
                                else
                                {
                                    folders[i].LatestUser = folders[j].LatestUser;
                                    folders[i].LastTimeDownloaded = folders[j].LastTimeDownloaded;
                                }

                                folders[i].DownloadNum += folders[j].DownloadNum;

                            }
                            else
                            {                              
                                folders[i].DownloadNum = folders[j].DownloadNum;
                                folders[i].LatestUser = folders[j].LatestUser;
                                folders[i].LastTimeDownloaded = folders[j].LastTimeDownloaded;
                            }
                           
                        }
                    }
                }

                //sort and save database
                users.Sort((x, y) => DateTime.Compare(x.convertDate(x.LastDate), y.convertDate(y.LastDate)));
                output = JsonConvert.SerializeObject(users);
                System.IO.File.WriteAllText(@Globals.UserDataFile + "\\userData.txt", output);

                output = JsonConvert.SerializeObject(folders);
                System.IO.File.WriteAllText(@Globals.UserDataFile + "\\fileData.txt", output);

                sr.Close();
                fs.Close();
                sw.Flush();
                sw.Close();
                fs2.Close();

                //retuirn info for output box
                return info;
            }
        }


        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SlskTransferStats());

        }

    }

}
