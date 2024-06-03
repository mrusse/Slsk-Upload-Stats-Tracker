using Dapper;
using System;
using System.Collections.Generic;
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
                var output = cnn.Query<Download>("select * from Download where Username = " + username, new DynamicParameters());
                return output.ToList();
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

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
