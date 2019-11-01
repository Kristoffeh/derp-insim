using System;
using System.Threading;
using InSimDotNet.Packets;
using System.Globalization;
using System.Windows.Forms;

namespace Derp_InSim
{
    public partial class Form1
    {
        System.Timers.Timer SQLReconnectTimer = new System.Timers.Timer();
        System.Timers.Timer SuperFast = new System.Timers.Timer();

        public void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

                #region ' Timer '


                SuperFast.Elapsed += new System.Timers.ElapsedEventHandler(SuperFast_Elapsed);
                SuperFast.Interval = 1;
                SuperFast.Enabled = true;

                System.Timers.Timer Payout = new System.Timers.Timer();
                Payout.Elapsed += new System.Timers.ElapsedEventHandler(Payout_Timer);
                Payout.Interval = 3000;
                Payout.Enabled = true;

                // SQL timer
                SQLReconnectTimer.Interval = 10000;
                SQLReconnectTimer.Elapsed += new System.Timers.ElapsedEventHandler(SQLReconnectTimer_Elapsed);
                
                ConnectedToSQL = SqlInfo.StartUp(SQLIPAddress, SQLDatabase, SQLUsername, SQLPassword);
                if (!ConnectedToSQL)
                {
                    insim.Send(255, "SQL connect attempt failed! Attempting to reconnect in 10 seconds!");
                    SQLReconnectTimer.Start();
                }
                else
                {
                    insim.Send(255, "SQL Connected!");
                }

                #endregion
            }
            catch (Exception error)
            {

                {
                    MessageBox.Show("" + error.Message, "AN ERROR OCCURED");
                }
            }
        }

        private void SuperFast_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                {
                    foreach (var Conn in _players.Values)
                    {
                        {
                            var cn = _connections[Conn.UCID];
                            {
                                // DARK
                                insim.Send(new IS_BTN
                                {
                                    UCID = cn.UCID,
                                    ReqI = 1,
                                    ClickID = 1,
                                    BStyle = ButtonStyles.ISB_DARK,
                                    H = 8,
                                    W = 62,
                                    T = 0,
                                    L = 69,
                                });

                                // Cash label
                                insim.Send(new IS_BTN
                                {
                                    Text = "Cash:",
                                    UCID = cn.UCID,
                                    ReqI = 2,
                                    ClickID = 2,
                                    BStyle = ButtonStyles.ISB_LEFT,
                                    H = 4,
                                    W = 6,
                                    T = 1,
                                    L = 70,
                                });

                                // Cash box
                                insim.Send(new IS_BTN
                                {
                                    Text = "^2€" + _connections[cn.UCID].cash,
                                    UCID = cn.UCID,
                                    ReqI = 3,
                                    ClickID = 3,
                                    BStyle = ButtonStyles.ISB_LIGHT | ButtonStyles.ISB_LEFT,
                                    H = 4,
                                    W = 10,
                                    T = 1,
                                    L = 76,
                                });

                                // km
                                insim.Send(new IS_BTN
                                {
                                    Text = "^0" + string.Format("{0:n0}", _connections[cn.UCID].TotalDistance / 1000) + " km",
                                    UCID = cn.UCID,
                                    ReqI = 4,
                                    ClickID = 4,
                                    BStyle = ButtonStyles.ISB_LIGHT | ButtonStyles.ISB_LEFT,
                                    H = 4,
                                    W = 10,
                                    T = 1,
                                    L = 88,
                                });
                            }
                        }


                    }
                }
            }
            catch (Exception error)
            {
                
                {
                    MessageBox.Show("" + error.Message, "AN ERROR OCCURED");
                }
            }
        }

        private void Payout_Timer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                {
                    foreach (var Conn in _players.Values)
                    {
                        var con = _connections[Conn.UCID];

                        if ((Conn.PName == HostName && Conn.UCID == 0) == false)
                        {
                            if (Conn.CName == "UF1" || Conn.CName == "XFG" || Conn.CName == "XRG" || Conn.CName == "MRT")
                            {
                                if (Conn.kmh > 50)
                                {
                                    con.cash += 1;
                                }
                            }
                            else if (Conn.CName == "LX4" || Conn.CName == "LX6" || Conn.CName == "RB4" || Conn.CName == "FXO" || Conn.CName == "XRT" || Conn.CName == "RAC" || Conn.CName == "FZ5")
                            {
                                if (Conn.kmh > 30)
                                {
                                    con.cash += 1;
                                }
                            }
                            else if (Conn.CName == "UFR" || Conn.CName == "XFR" || Conn.CName == "FXR" || Conn.CName == "XRR" || Conn.CName == "FZR" || Conn.CName == "MRT" || Conn.CName == "FBM" || Conn.CName == "FOX")
                            {
                                if (Conn.kmh > 30)
                                {
                                    con.cash += 1;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                
                {
                    MessageBox.Show("" + error.Message, "AN ERROR OCCURED");
                }
            }
        }

        private void SQLReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            {
                SQLRetries++;
                ConnectedToSQL = SqlInfo.StartUp(SQLIPAddress, SQLDatabase, SQLUsername, SQLPassword);
                if (!ConnectedToSQL)
                {
                    insim.Send(255, "SQL connect attempt failed! Attempting to reconnect in 10 seconds!", false);
                }
                else
                {
                    insim.Send(255, "SQL connected after ^2" + SQLRetries + " ^8times!");
                    SQLRetries = 0;
                    SQLReconnectTimer.Stop();
                }
            }
        }
    }
}
