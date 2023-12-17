using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PlayerPointNamespace;

namespace ClientDataNamespace
{
    public class ClientData
    {
        public int Mode { get; set; }

        public int Number { get; set; }

        public PlayerPoint Position { get; set; }

        public ClientData()
        {
 
        }
        public ClientData(int mode, int number, PlayerPoint position)
        {
            Mode = mode;
            Number = number;
            Position = position;
        }
    }
}
