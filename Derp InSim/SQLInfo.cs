using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derp_InSim
{
    public class SQLInfo
    {
        const string TimeFormat = "dd/MM/yyyy";//ex: 23/03/2003
        const string TimeFormatTwo = "dd/MM/yyyy HH:mm";//ex: 23/03/2003 23:00
        MySqlConnection SQL = new MySqlConnection();
        public SQLInfo() { }

        public bool IsConnectionStillAlive()
        {
            try
            {
                if (SQL.State == System.Data.ConnectionState.Open) return true;
                else return false;
            }
            catch { return false; }
        }
        
        // Load the database
        public bool StartUp(string server, string database, string username, string password)
        {
            try
            {
                if (IsConnectionStillAlive()) return true;

                SQL.ConnectionString = "Server=" + server +
                    ";Database=" + database +
                    ";Uid=" + username +
                    ";Pwd=" + password +
                    ";Connect Timeout=10;";
                SQL.Open();

                Query("CREATE TABLE IF NOT EXISTS users(PRIMARY KEY(username),username CHAR(25) NOT NULL,cash int(10),bankbalance int(10),totaldistance int(11),cars CHAR(90),regdate CHAR(16) NOT NULL,lastseen CHAR(16),totaljobsdone int(10), totalearnedfromjobs int(10));");
            }
            catch { return false; }
            return true;
        }

        // Load query
        public int Query(string str)
        {
            MySqlCommand query = new MySqlCommand();
            query.Connection = SQL;
            query.CommandText = str;
            query.Prepare();
            return query.ExecuteNonQuery();
        }

        #region Player Saving Stuff
        // Exist in database
        public bool UserExist(string username, string table = "users")
        {
            MySqlCommand query = new MySqlCommand();
            query.Connection = SQL;
            query.CommandText = "SELECT username FROM " + table + " WHERE username='" + username + "' LIMIT 1;";
            query.Prepare();
            MySqlDataReader dr = query.ExecuteReader();

            bool found = false;

            if (dr.Read()) if (dr.GetString(0) != "") found = true;
            dr.Close();

            return found;
        }

        // Add user to database
        public void AddUser(string username, long cash, long bankbalance, long totaldistance, string cars, string regdate, string lastseen, long totaljobsdone, long totalearnedfromjobs)
        {
            if (username == "") return;
            Query("INSERT INTO users VALUES ('" + username + "', " + cash + ", " + bankbalance + ", " + totaldistance + ", '" + cars + "', '" + DateTime.UtcNow.ToString(TimeFormat) + "', '" + DateTime.UtcNow.ToString(TimeFormat) + "', " + totaljobsdone + ", " + totalearnedfromjobs + ");");
        }

        public void UpdateUser(string username, bool updatejointime, int cash = 0, int bankbalance = 0, long totaldistance = 0, string cars = "UF1, XFG, XRG", int totaljobsdone = 0, int totalearnedfromjobs = 0)
        {
            if (updatejointime) Query("UPDATE users SET lastseen='" + DateTime.UtcNow.ToString(TimeFormatTwo) + "' WHERE username='" + username + "';");
            else Query("UPDATE users SET cash=" + cash + ", bankbalance=" + bankbalance + ", totaldistance=" + totaldistance + ", cars=" + cars + ", totaljobsdone=" + totaljobsdone + 
                ", totalearnedfromjobs=" + totalearnedfromjobs + " WHERE username='" + username + "';");
        }

        // Load their options
        public string[] LoadUserOptions(string username)
        {
            string[] options = new string[8];

            MySqlCommand query = new MySqlCommand();
            query.Connection = SQL;
            query.CommandText = "SELECT cash, bankbalance, totaldistance, cars, regdate, lastseen, totaljobsdone, totalearnedfromjobs FROM users WHERE username='" + username + "' LIMIT 1;";
            query.Prepare();
            MySqlDataReader dr = query.ExecuteReader();

            if (dr.Read())
                if (dr.GetString(0) != "")
                {
                    options[0] = dr.GetString(0);
                    options[1] = dr.GetString(1);
                    options[2] = dr.GetString(2);
                    options[3] = dr.GetString(3);
                    options[4] = dr.GetString(4);
                    options[5] = dr.GetString(5);
                    options[6] = dr.GetString(6);
                    options[7] = dr.GetString(7);
                }
            dr.Close();

            return options;
        }
        #endregion
    }
}
