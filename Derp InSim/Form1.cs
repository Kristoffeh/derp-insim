using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InSimDotNet;
using InSimDotNet.Packets;
using System.Globalization;
using System.Threading;
using InSimDotNet.Helpers;
using System.Timers;
using System.IO;

namespace Derp_InSim
{
    public partial class Form1 : Form
    {
        InSim insim = new InSim();
        
        // Global Vars
        public const string Tag = "^5EC^0™";
        const string DataFolder = "files";
        public const string InSimVersion = "0.003a";
        public string TrackName = "None";
        public string HostName = "host";
        public string LayoutName = "None";

        // MySQL Variables
        public SQLInfo SqlInfo = new SQLInfo();
        public bool ConnectedToSQL = false;
        public int SQLRetries = 0;

        // MySQL Connect
        string SQLIPAddress = "127.0.0.1";
        string SQLDatabase = "lfs";
        string SQLUsername = "root";
        string SQLPassword = "Fiskebolle2015!";


        class Connections
        {
            // NCN fields
            public byte UCID;
            public string UName;
            public string PName;
            public bool IsAdmin;

            // Custom Fields
            public bool IsSuperAdmin;
            
            public bool OnTrack;

            public byte Interface;

            public int cash;
            public int bankbalance;
            public string regdate;
            public string lastseen;
            public int totaljobsdone;
            public int totalearnedfromjobs;
            public string cars;

            public long TotalDistance;
        }
        class Players
        {
            public byte UCID;
            public byte PLID;
            public string PName;
            public string CName;
            public string NoColPlayername;

            public int kmh;
            public int mph;
            public string Plate;
        }

        private Dictionary<byte, Connections> _connections = new Dictionary<byte, Connections>();
        private Dictionary<byte, Players> _players = new Dictionary<byte, Players>();

        public Form1()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            InitializeComponent();
            RunInSim();
        }

        void RunInSim()
        {

            // Bind packet events.
            insim.Bind<IS_NCN>(NewConnection);
            insim.Bind<IS_NPL>(NewPlayer);
            insim.Bind<IS_MSO>(MessageReceived);
            insim.Bind<IS_MCI>(MultiCarInfo);
            insim.Bind<IS_CNL>(ConnectionLeave);
            insim.Bind<IS_CPR>(ClientRenames);
            insim.Bind<IS_PLL>(PlayerLeave);
            insim.Bind<IS_STA>(OnStateChange);
            insim.Bind<IS_BTC>(ButtonClicked);
            insim.Bind<IS_BFN>(ClearButtons);
            insim.Bind<IS_VTN>(VoteNotify);
            insim.Bind<IS_AXI>(AutocrossInformation);

            // Initialize InSim
            insim.Initialize(new InSimSettings
            {
                Host = "127.0.0.1", // 93.190.143.115
                Port = 29999,
                Admin = "2910",
                Prefix = '!',
                Flags = InSimFlags.ISF_MCI | InSimFlags.ISF_MSO_COLS,

                Interval = 1000
            });

            insim.Send(new[]
            {
                new IS_TINY { SubT = TinyType.TINY_NCN, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_NPL, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_ISM, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_SST, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_MCI, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_NCI, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_AXI, ReqI = 255 },
                new IS_TINY { SubT = TinyType.TINY_SST, ReqI = 255 },
            });

            insim.Send(255, 0, "^3NOTE: ^8InSim connected with version ^2" + InSimVersion);

        }

        #region ' Misc '

        bool TryParseCommand(IS_MSO mso, out string[] args)
        {
            if (mso.UserType == UserType.MSO_PREFIX)
            {
                var message = mso.Msg.Substring(mso.TextStart);
                args = message.Split();
                return args.Length > 0;
            }

            args = null;
            return false;
        }

        /// <summary>Returns true if method needs invoking due to threading</summary>
        private bool DoInvoke()
        {
            foreach (Control c in this.Controls)
            {
                if (c.InvokeRequired) return true;
                break;	// 1 control is enough
            }
            return false;
        }
        #endregion

        // Player joins server
        void NewConnection(InSim insim, IS_NCN packet)
        {
            try
            {
                _connections.Add(packet.UCID, new Connections
                {
                    UCID = packet.UCID,
                    UName = packet.UName,
                    PName = packet.PName,
                    IsAdmin = packet.Admin,

                    IsSuperAdmin = GetUserAdmin(packet.UName),
                    OnTrack = false,
                    TotalDistance = 0,
                    cash = 1,
                    bankbalance = 0,
                    cars = "UF1, XFG, XRG",
                    regdate = "0.0.0"
                });


                if (ConnectedToSQL)
                {
                    try
                    {
                        if (SqlInfo.UserExist(packet.UName))
                        {
                            SqlInfo.UpdateUser(packet.UName, true);//Updates the last joined time to the current one

                            string[] LoadedOptions = SqlInfo.LoadUserOptions(packet.UName);
                            _connections[packet.UCID].cash = Convert.ToInt32(LoadedOptions[0]);
                            _connections[packet.UCID].bankbalance = Convert.ToInt32(LoadedOptions[1]);
                            _connections[packet.UCID].TotalDistance = Convert.ToInt32(LoadedOptions[2]);
                            _connections[packet.UCID].cars = LoadedOptions[3];
                            _connections[packet.UCID].regdate = LoadedOptions[4];
                            _connections[packet.UCID].lastseen = LoadedOptions[5];
                            _connections[packet.UCID].totaljobsdone = Convert.ToInt32(LoadedOptions[6]);
                            _connections[packet.UCID].totalearnedfromjobs = Convert.ToInt32(LoadedOptions[7]);
                        }
                        else SqlInfo.AddUser(packet.UName, _connections[packet.UCID].cash, _connections[packet.UCID].bankbalance, _connections[packet.UCID].TotalDistance, _connections[packet.UCID].cars, _connections[packet.UCID].regdate, _connections[packet.UCID].lastseen, _connections[packet.UCID].totaljobsdone, _connections[packet.UCID].totalearnedfromjobs);
                    }
                    catch (Exception EX)
                    {
                        if (!SqlInfo.IsConnectionStillAlive())
                        {
                            ConnectedToSQL = false;
                            SQLReconnectTimer.Start();
                        }
                        else Console.WriteLine("NCN(Add/Load)User - " + EX.Message);
                    }
                }

                #region ' Retrieve HostName '
                if (packet.UCID == 0 && packet.UName == "")
                {
                    HostName = packet.PName;
                }
                #endregion

                if (packet.ReqI == 0)
                {
                    insim.Send(packet.UCID, "^8Current track: ^3" + TrackHelper.GetFullTrackName(TrackName));
                }




            }
            catch (Exception e)
            {
                var conn = _players[packet.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[packet.UCID].UName + ") NCN - Exception: " + e, false);
            }

        }


        // Player joins race or enter track
        void NewPlayer(InSim insim, IS_NPL packet)
        {
            try
            {
                if (_players.ContainsKey(packet.PLID))
                {
                    // Leaving pits, just update NPL object.
                    _players[packet.PLID].UCID = packet.UCID;
                    _players[packet.PLID].PLID = packet.PLID;
                    _players[packet.PLID].PName = packet.PName;
                    _players[packet.PLID].CName = packet.CName;
                    _players[packet.PLID].Plate = packet.Plate;
                }
                else
                {
                    // Add new player.
                    _players.Add(packet.PLID, new Players
                    {
                        UCID = packet.UCID,
                        PLID = packet.PLID,
                        PName = packet.PName,
                        CName = packet.CName,
                        Plate = packet.Plate
                    });
                }
            }
            catch (Exception e)
            {
                var conn = _players[packet.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[packet.UCID].UName + ") NPL - Exception: " + e, false);
            }
        }

        // Player left the server
        void ConnectionLeave(InSim insim, IS_CNL CNL)
        {
            try
            {
                string nocolplyname = StringHelper.StripColors(_connections[CNL.UCID].PName);

                LogTextToFile("connections", _connections[CNL.UCID].PName + " (" + _connections[CNL.UCID].UName + ") Disconnected", false);

                // Save values of user - CNL (on disconnect)

                if (ConnectedToSQL)
                {
                    try
                    {
                        SqlInfo.UpdateUser(_connections[CNL.UCID].UName, true);

                        SqlInfo.UpdateUser(_connections[CNL.UCID].UName, false, _connections[CNL.UCID].cash, _connections[CNL.UCID].bankbalance, _connections[CNL.UCID].TotalDistance, _connections[CNL.UCID].cars, _connections[CNL.UCID].totaljobsdone, _connections[CNL.UCID].totalearnedfromjobs);
                    }
                    catch (Exception EX)
                    {
                        if (!SqlInfo.IsConnectionStillAlive())
                        {
                            ConnectedToSQL = false;
                            SQLReconnectTimer.Start();
                        }
                        else
                        {
                            var conn = _connections[CNL.UCID];
                            LogTextToFile("error", "[" + conn.UCID + "] " + StringHelper.StripColors(conn.PName) + "(" + _connections[CNL.UCID].UName + ") NPL - Exception: " + EX.Message, false);
                        }
                    }
                }

                _connections.Remove(CNL.UCID);
            }
            catch (Exception e)
            {
                var conn = _players[CNL.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[CNL.UCID].UName + ") CNL - Exception: " + e, false);
            }
        }

        // Button click (is_btn click ID's)
        void ButtonClicked(InSim insim, IS_BTC BTC)
        {
            try
            {
                BTC_ClientClickedButton(BTC);
            }
            catch (Exception e)
            {
                var conn = _players[BTC.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[BTC.UCID].UName + ") BTC - Exception: " + e, false);
            }
        }

        // BuTton FunctioN (IS_BFN, SHIFT + I)
        void ClearButtons(InSim insim, IS_BFN BFN)
        {
            try
            {
                insim.Send(BFN.UCID, "^8InSim buttons cleared ^7(SHIFT + I)");
            }
            catch (Exception e)
            {
                var conn = _players[BFN.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[BFN.UCID].UName + ") BFN - Exception: " + e, false);
            }
        }

        // Autocross information
        void AutocrossInformation(InSim insim, IS_AXI AXI)
        {
            try
            {
                if (AXI.NumO != 0)
                {
                    LayoutName = AXI.LName;
                    if (AXI.ReqI == 0)
                    {
                        insim.Send(255, "^8Layout ^2" + LayoutName + " ^8loaded");
                    }
                }
            }
            catch (Exception e)
            {

            }
        }

        // Vote notify (cancel votes)
        private void VoteNotify(InSim insim, IS_VTN VTN)
        {
            try
            {
                var test = VTN.UCID;

                foreach (var conn in _connections.Values)
                {
                    if (conn.UCID == VTN.UCID)
                    {
                        if (VTN.Action == VoteAction.VOTE_END)
                        {
                            if (_connections[VTN.UCID].IsAdmin != true)
                            {
                                insim.Send("/cv");
                            }
                        }

                        if (VTN.Action == VoteAction.VOTE_RESTART)
                        {
                            if (_connections[VTN.UCID].IsAdmin != true)
                            {
                                insim.Send("/cv");
                            }
                        }


                    }
                }

            }
            catch (Exception e)
            {
                var conn = _players[VTN.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[VTN.UCID].UName + ") - VTN - Exception: " + e, false);
            }
        }

        // MCI - Multi Car Info
        private void MultiCarInfo(InSim insim, IS_MCI mci)
        {
            try
            {
                {
                    foreach (CompCar car in mci.Info)
                    {
                        Connections conn = GetConnection(car.PLID);

                        int Sped = Convert.ToInt32(MathHelper.SpeedToKph(car.Speed));

                        decimal SpeedMS = (decimal)(((car.Speed / 32768f) * 100f) / 2);
                        decimal Speed = (decimal)((car.Speed * (100f / 32768f)) * 3.6f);

                        int kmh = car.Speed / 91;
                        int mph = car.Speed / 146;
                        var X = car.X;
                        var Y = car.Y;
                        var Z = car.Z;
                        var angle = car.AngVel / 30;
                        string anglenew = "";

                        foreach (var cne in _players.Values)
                        {
                            if (cne.UCID == conn.UCID)
                            {
                                cne.kmh = kmh;
                                cne.mph = mph;

                                _connections[cne.UCID].TotalDistance += Convert.ToInt32(SpeedMS);
                            }
                        }



                        anglenew = angle.ToString().Replace("-", "");

                        

                        {




                        }

                    }


                }
            }
            catch (Exception e)
            {

            }
        }

        void ClientRenames(InSim insim, IS_CPR CPR)
        {
            try
            {
                {
                _connections[CPR.UCID].PName = CPR.PName;

                foreach (var CurrentPlayer in _players.Values) if (CurrentPlayer.UCID == CPR.UCID) CurrentPlayer.PName = CPR.PName;//make sure your code is AFTER this one
                }
            }
            catch (Exception e)
            {
                var conn = _players[CPR.UCID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[CPR.UCID].UName + ") - CPR - Exception: " + e, false);
            }
        }

        void OnStateChange(InSim insim, IS_STA STA)
        {
            try
            {
                if (TrackName != STA.Track)
                {
                    TrackName = STA.Track;
                    insim.Send(new IS_TINY { SubT = TinyType.TINY_AXI, ReqI = 255 });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e, "AN ERROR OCCURED");
                insim.Send(255, "^8An error occured: ^1{0}", e);
            }
        }

        // Join spectators (SHIFT + S)
        void PlayerLeave(InSim insim, IS_PLL PLL)
        {
            try
            {

                {
                    // Conn.OnTrack = false;
                    ConnectedToSQL = false;
                }

                _players.Remove(PLL.PLID);//make sure your code is BEFORE this one
            }
            catch (Exception e)
            {
                var conn = _players[PLL.PLID];
                conn.NoColPlayername = StringHelper.StripColors(conn.PName);

                LogTextToFile("error", "[" + conn.UCID + "] " + conn.NoColPlayername + "(" + _connections[PLL.PLID].UName + ") - PLL - Exception: " + e, false);
            }
        }



        #region ' Functions '
        void ClearPen(string Username)
        {
            try
            {
                
                {
                    insim.Send("/p_clear " + Username);
                }
            }
            catch (Exception error)
            {
                
                {
                    MessageBox.Show("" + error, "AN ERROR OCCURED");
                    insim.Send(255, "^8An error occured: ^1{0}", error);
                }
            }
        }

        void KickID(string Username)
        {
            try
            {
                
                {
                    insim.Send("/kick " + Username);
                }
            }
            catch (Exception error)
            {
                
                {
                    MessageBox.Show("" + error, "AN ERROR OCCURED");
                }
            }
        }

        private void btn(string text, byte height, byte width, byte top, byte length, ButtonStyles bstyle, byte clickid, byte ucid)
        {
            try
            {
                
                {
                    insim.Send(new IS_BTN
                    {
                        Text = text,
                        UCID = ucid,
                        ReqI = clickid,
                        ClickID = clickid,
                        BStyle = bstyle,
                        H = height,
                        W = width,
                        T = top,
                        L = length
                    });
                }
            }
            catch
            {

            }
        }

        private Connections GetConnection(byte PLID)
        {//Get Connection from PLID
            Players NPL;
            if (_players.TryGetValue(PLID, out NPL)) return _connections[NPL.UCID];
            return null;
        }

        private bool IsConnAdmin(Connections Conn)
        {//general admin check, both Server and InSim
            if (Conn.IsAdmin == true || Conn.IsSuperAdmin == true) return true;
            return false;
        }

        private bool GetUserAdmin(string Username)
        {//reading admins.ini when connecting to server for InSim admin
            StreamReader CurrentFile = new StreamReader(DataFolder + "/admins.ini");

            string line = null;
            while ((line = CurrentFile.ReadLine()) != null)
            {
                if (line == Username)
                {
                    CurrentFile.Close();
                    return true;
                }
            }
            CurrentFile.Close();
            return false;
        }

        private void LogTextToFile(string file, string text, bool AdminMessage = true)
        {

            if (System.IO.File.Exists(DataFolder + "/" + file + ".log") == false) { FileStream CurrentFile = System.IO.File.Create(DataFolder + "/" + file + ".log"); CurrentFile.Close(); }

            StreamReader TextTempData = new StreamReader(DataFolder + "/" + file + ".log");
            string TempText = TextTempData.ReadToEnd();
            TextTempData.Close();

            StreamWriter TextData = new StreamWriter(DataFolder + "/" + file + ".log");
            TextData.WriteLine(TempText + DateTime.Now + ": " + text);
            TextData.Flush();
            TextData.Close();
        }

        private void MessageToAdmins(string Message)
        {
            {
                foreach (var CurrentConnection in _connections.Values)
                {
                    if (IsConnAdmin(CurrentConnection) && CurrentConnection.UName != "")
                    {
                        
                        {
                            insim.Send(CurrentConnection.UCID, 0, "^3AC: ^8" + Message);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
