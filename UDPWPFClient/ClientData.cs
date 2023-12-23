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
        public String Name { get; set; }
        public int Number { get; set; }
        public PlayerPoint Position { get; set; }
        public int Color { get; set; }

        public ClientData()
        {
 
        }
        public ClientData(int mode, String name, int number, PlayerPoint position, int color)
        {
            Mode = mode;
            Name = name;
            Number = number;
            Position = position;
            Color = color;
        }
    }
}
