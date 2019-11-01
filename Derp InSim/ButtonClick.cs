using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InSimDotNet;
using InSimDotNet.Packets;
using InSimDotNet.Helpers;
using System.Windows.Forms;


namespace Derp_InSim
{
    public partial class Form1
    {
        private void BTC_ClientClickedButton(IS_BTC BTC)
        {
            try
            {
                
                {

                    switch (BTC.ClickID)
                    {
                        case 1:

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                
                {
                    MessageBox.Show("" + e, "AN ERROR OCCURED");
                }
            }
        }
    }
}
