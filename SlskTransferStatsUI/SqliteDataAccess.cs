using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlskTransferStatsUI
{
    public class SqliteDataAccess
    {
        public static List<Person> LoadUsers()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<Person>("select * from User", new DynamicParameters());
                return output.ToList();
            }
        }

        public static void SaveUser(Person user)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into User (Username, DownloadNum, TotalDownloadSize, LastDate) values (@Username, @DownloadNum, @TotalDownloadSize, @LastDate)", user);
            }
        }

        public static List<Download> LoadDownloads()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<Download>("select * from Download", new DynamicParameters());
                return output.ToList();
            }
        }

        public static List<Download> LoadUserDownloads(String username)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<Download>("select * from Download where Username = '" + username.Replace("'", "''") + "'", new DynamicParameters());
                return output.ToList();
            }
        }

        public static int CountDownload(String path)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                int count = cnn.ExecuteScalar<int>("select count(*) from Download where lower(Path)='" + path.Replace("'", "''").ToLower() + "'");
                return count;
            }
        }

        public static void SaveDownload(Download download)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into Download (Username, Filename, Path, Size, Date) values (@Username, @Filename, @Path, @Size, @Date)", download);
            }
        }

        public static List<Folder> LoadFolders()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<Folder>("select * from Folder", new DynamicParameters());
                return output.ToList();
            }
        }

        public static Folder LoadFolder(String path)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<Folder>("select * from Folder where PathToLower='" + path.Replace("'", "''") + "'", new DynamicParameters());
                return output.ToList()[0];
            }
        }

        public static int CheckFolder(String path)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                int count = cnn.ExecuteScalar<int>("select count(*) from Folder where PathToLower='" + path.Replace("'", "''") + "'");
                return count;
            }
        }

        public static void SaveFolder(Folder folder)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into Folder (Path, PathToLower, Foldername, DownloadNum, LatestUser, LastTimeDownloaded) values (@Path, @PathToLower, @Foldername, @DownloadNum, @LatestUser, @LastTimeDownloaded)", folder);
            }
        }

        public static void UpdateFolder(Folder folder)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("update Folder set DownloadNum = @DownloadNum, LatestUser = @LatestUser, LastTimeDownloaded = @LastTimeDownloaded where Path = @Path", folder);
            }
        }

        public static void ConvertLegacyDatabase(List<Person> users)
        {
            string connectionString = LoadConnectionString();
            SQLiteConnection dbcon = new SQLiteConnection(connectionString);
            dbcon.Open();

            SQLiteCommand sqlComm;
            sqlComm = new SQLiteCommand("begin", dbcon);
            sqlComm.ExecuteNonQuery();

            for (int i = 0; i < users.Count; i++)
            {
                dbcon.Execute("insert into User (Username, DownloadNum, TotalDownloadSize, LastDate) values (@Username, @DownloadNum, @TotalDownloadSize, @LastDate)", users[i]);

                for (int j = 0; j < users[i].DownloadList.Count; j++)
                {
                    users[i].DownloadList[j].Username = users[i].Username;
                    dbcon.Execute("insert into Download (Username, Filename, Path, Size, Date) values (@Username, @Filename, @Path, @Size, @Date)", users[i].DownloadList[j]);

                    string path = users[i].DownloadList[j].Path.Substring(0, users[i].DownloadList[j].Path.LastIndexOf("\\"));
                    string foldername = path.Substring(path.LastIndexOf("\\") + 1);

                    //Change date format
                    string lastDate = users[i].DownloadList[j].Date.Substring(1, users[i].LastDate.Length - 2);
                    string[] dateSplit = lastDate.Split();
                    lastDate = dateSplit[0] + ", " + dateSplit[2] + " " + dateSplit[1] + " " + dateSplit[4] + " " + dateSplit[3];

                    int count = dbcon.ExecuteScalar<int>("select count(*) from Folder where PathToLower='" + path.ToLower().Replace("'", "''") + "'");
                    if (count == 0)
                    {
                        Folder folder = new Folder(path, path.ToLower(), foldername, 1, users[i].Username, lastDate);
                        dbcon.Execute("insert into Folder (Path, PathToLower, Foldername, DownloadNum, LatestUser, LastTimeDownloaded) values (@Path, @PathToLower, @Foldername, @DownloadNum, @LatestUser, @LastTimeDownloaded)", folder);
                    }
                    else
                    {
                        Folder folder = dbcon.Query<Folder>("select * from Folder where PathToLower='" + path.ToLower().Replace("'", "''") + "'", new DynamicParameters()).ToList()[0];
                        if (folder.LatestUser != users[i].Username)
                        {
                            folder.LatestUser = users[i].Username;
                            folder.DownloadNum = folder.DownloadNum += 1;
                            folder.LastTimeDownloaded = lastDate;

                            dbcon.Execute("update Folder set DownloadNum = @DownloadNum, LatestUser = @LatestUser, LastTimeDownloaded = @LastTimeDownloaded where Path = @Path", folder);
                        }
                    }
                }
            }

            sqlComm = new SQLiteCommand("end", dbcon);
            sqlComm.ExecuteNonQuery();
            dbcon.Close();
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
