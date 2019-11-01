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

namespace Derp_InSim
{
    public partial class Form1
    {
        private void MessageReceived(InSim insim, IS_MSO mso)
        {
            try
            {
                {

                    if (mso.UserType == UserType.MSO_PREFIX)
                    {
                        string Text = mso.Msg.Substring(mso.TextStart, (mso.Msg.Length - mso.TextStart));
                        string[] command = Text.Split(' ');
                        command[0] = command[0].ToLower();

                        switch (command[0])
                        {
                            case "!ac":
                                {//Admin chat
                                    if (mso.UCID == _connections[mso.UCID].UCID)
                                    {
                                        if (!IsConnAdmin(_connections[mso.UCID]))
                                        {
                                            insim.Send(mso.UCID, 0, "You are not an admin");
                                            break;
                                        }
                                        if (command.Length == 1)
                                        {
                                            insim.Send(mso.UCID, 0, "^1Invalid command format. ^2Usage: ^7!ac <text>");
                                            break;
                                        }

                                        string atext = Text.Remove(0, command[0].Length + 1);

                                        foreach (var Conn in _connections.Values)
                                        {
                                            {
                                                if (IsConnAdmin(Conn) && Conn.UName != "")
                                                {
                                                    insim.Send(Conn.UCID, 0, "^3Admin chat: ^7" + _connections[mso.UCID].PName + " ^8(" + _connections[mso.UCID].UName + "):");
                                                    insim.Send(Conn.UCID, 0, "^3: ^7" + atext);
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }

                            case "!help":
                                insim.Send(mso.UCID, 0, "^3Help commands (temporary list):");
                                insim.Send(mso.UCID, 0, "^7!help ^8- See a list of available commands");
                                insim.Send(mso.UCID, 0, "^7!info ^8- See a few lines of server info");


                                // Admin commands
                                foreach (var CurrentConnection in _connections.Values)
                                {
                                    if (CurrentConnection.UCID == mso.UCID)
                                    {
                                        if (IsConnAdmin(CurrentConnection) && CurrentConnection.UName != "")
                                        {
                                            insim.Send(CurrentConnection.UCID, 0, "^3Administrator commands:");
                                            insim.Send(CurrentConnection.UCID, 0, "^7!ac ^8- Talk with the other admins that are online");
                                        }
                                    }
                                }

                                insim.Send(mso.UCID, "^8Playername: ^7" + StringHelper.StripColors(_connections[mso.UCID].PName));

                                break;

                            case "!info":

                                int count = _connections.Count - 1;

                                insim.Send(mso.UCID, 0, "^3Server Info (temporary list):");
                                insim.Send(mso.UCID, 0, "^8Players connected: ^2" + count);
                                insim.Send(mso.UCID, 0, "^8Players on the track: ^2" + _players.Count);
                                break;

                            case "!s":
                                foreach (var CurrentConnection in _players.Values)
                                {
                                    if (CurrentConnection.UCID == mso.UCID)
                                    {
                                        insim.Send(mso.UCID, "^8Current speed: ^3{0} ^8kmh, ^3{1} ^8mph", CurrentConnection.kmh, CurrentConnection.mph);

                                    }

                                }
                                break;

                            case "!show":
                            case "!showoff":
                            case "!stats":
                                foreach (var conn in _connections.Values)
                                {
                                    if (conn.UCID == mso.UCID)
                                    {
                                        insim.Send(255, "^8Showoff for ^7" + _connections[mso.UCID].PName + " ^8(" + _connections[mso.UCID].UName + ")");
                                        insim.Send(255, "^8Cash: ^2€" + conn.cash);
                                    }
                                }
                                break;

                            default:
                                insim.Send(mso.UCID, 0, "^8Invalid command, check out {0} for commands", "^2!help^8");
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e, "AN ERROR OCCURED");
                insim.Send(255, "^8An error occured: ^1{0}", e);
            }
        }
    }

}
