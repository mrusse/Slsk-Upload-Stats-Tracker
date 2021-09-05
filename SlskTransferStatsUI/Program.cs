using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace SlskTransferStatsUI
{

    

    static class Program
    {
        class FileRead
        {

            public void ParseData()
            {
                FileStream fs = new FileStream("E:\\SlskTransferStatsUI\\test.txt", FileMode.Open, FileAccess.Read);
                FileStream fs2 = new FileStream("E:\\SlskTransferStatsUI\\parsed.txt", FileMode.Create, FileAccess.Write);
                StreamReader sr = new StreamReader(fs);
                StreamWriter sw = new StreamWriter(fs2);
                sr.BaseStream.Seek(0, SeekOrigin.Begin);

                List<Person> users = new List<Person>();

                if (File.Exists("E:\\SlskTransferStatsUI\\userData.txt"))
                {
                    string input = System.IO.File.ReadAllText(@"E:\\SlskTransferStats\\userData.txt");
                    users = JsonConvert.DeserializeObject<List<Person>>(input);
                }

                string drive = "E:";
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

                while (str != null)
                {
                    if (str.IndexOf("Queue", 27, 5) == 27)
                    {
                        queued = str;
                        str = sr.ReadLine();
                        if (str.IndexOf("Queued", 30, 6) == 30)
                        {

                            //string[] split = queued.Split();
                            username = queued.Substring(53, queued.Length - 53);
                            username = username.Substring(0, username.LastIndexOf(" for file @"));

                            index = users.FindIndex(person => person.Username == username);

                            if (index < 0)
                            {

                                path = queued.Substring(queued.IndexOf("\\"));
                                path = drive + path;

                                filename = queued.Substring(queued.LastIndexOf("\\") + 1);
                                size = new FileInfo(path).Length / 1000;

                                date = queued.Substring(0, queued.IndexOf("]") + 1);

                                List<Download> downloads = new List<Download>();

                                System.Console.WriteLine("New user found: " + username);
                                System.Console.WriteLine("New download for \"" + username + "\": " + filename);

                                downloads.Add(new Download(filename, path, size, date));
                                users.Add(new Person(username, 1, size, date, downloads));
                                index = users.FindIndex(person => person.Username == username);

                            }
                            else
                            {
                                path = queued.Substring(queued.IndexOf("\\"));
                                path = drive + path;

                                filename = queued.Substring(queued.LastIndexOf("\\") + 1);
                                size = new FileInfo(path).Length / 1000;

                                date = queued.Substring(0, queued.IndexOf("]") + 1);
                                added = false;

                                for (int i = 0; i < users[index].DownloadList.Count; i++)
                                {

                                    if (users[index].DownloadList[i].Filename == filename && users[index].DownloadList[i].Date == date)
                                    {
                                        added = true;
                                        //Console.WriteLine (users[index].DownloadList[i].Filename);
                                        break;
                                    }

                                }

                                if (added == false)
                                {



                                    users[index].DownloadList.Add(new Download(filename, path, size, date));
                                    users[index] = new Person(username,
                                                              users[index].DownloadNum + 1,
                                                              users[index].TotalDownloadSize += size,
                                                              users[index].DownloadList[users[index].DownloadList.Count - 1].Date,
                                                              users[index].DownloadList);

                                    System.Console.WriteLine("New download for \"" + users[index].Username + "\": " + users[index].DownloadList[users[index].DownloadList.Count - 1].Filename);


                                }

                            }

                        }

                    }

                    str = sr.ReadLine();
                }

                List<Folder> folders = new List<Folder>();
                string foldername;
                bool folderDL;
                int folderIndex;



                for (int i = 0; i < users.Count; i++)
                {

                    sw.WriteLine("User: " + users[i].Username + ", Number of downloads: " + users[i].DownloadNum + ", " +
                                 "Last Download: " + users[i].LastDate + ", Total download size: " + users[i].TotalDownloadSize +
                                 " kb" + "\n\n\tUsers Downloads:\n");



                    for (int j = 0; j < users[i].DownloadList.Count; j++)
                    {

                        folder = users[i].DownloadList[j].Path.Substring(0, users[i].DownloadList[j].Path.LastIndexOf("\\"));
                        foldername = folder.Substring(folder.LastIndexOf("\\") + 1);

                        if (j == 0 || (users[i].DownloadList[j - 1].Path.Substring(0, users[i].DownloadList[j - 1].Path.LastIndexOf("\\")) != folder))
                        {

                            if (folders.Count == 0)
                            {
                                folders.Add(new Folder(folder, foldername, 1, users[i].Username));
                            }
                            else
                            {
                                folderDL = false;

                                for (int k = 0; k < folders.Count; k++)
                                {

                                    if (folders[k].Path == folder && folders[k].LatestUser != users[i].Username)
                                    {
                                        System.Console.WriteLine(folder + "\t" + folders[k].Path);
                                        folders[k] = new Folder(folder, foldername, folders[k].DownloadNum += 1, users[i].Username);
                                        folderDL = true;
                                        break;
                                    }

                                }

                                if (folderDL == false)
                                {
                                    folders.Add(new Folder(folder, foldername, 1, users[i].Username));
                                }


                            }

                            folderIndex = folders.FindIndex(Folder => Folder.Path == folder);
                            sw.WriteLine("\t" + folder + "\tTotal folder download count: " + folders[folderIndex].DownloadNum);


                        }


                        sw.WriteLine("\t\t" + users[i].DownloadList[j].Filename + ", " + users[i].DownloadList[j].Size + " kb ");

                    }
                    sw.WriteLine("\n");

                }

                string output = JsonConvert.SerializeObject(users);
                System.IO.File.WriteAllText(@"E:\\SlskTransferStatsUI\\userData.txt", output);

                sr.Close();
                fs.Close();
                sw.Flush();
                sw.Close();
                fs2.Close();

                return;
            }
        }

        [STAThread]
        static void Main()
        {

            FileRead wr = new FileRead();
            

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
