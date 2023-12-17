using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PlayerPointNamespace;

namespace PlayerDataNamespace
{
    public class PlayerData
    {
        public PlayerPoint MousePosition { get; set; }
        public PlayerPoint PlayerPosition { get; set; }
        public double PlayerMass { get; set; }
        public double PlayerDiameter { get; set; }
        public PlayerData()
        {

        }
        public PlayerData(PlayerPoint mousePosition, PlayerPoint playerPosition, double playerMass, double playerDiameter)
        {
            MousePosition = mousePosition;
            PlayerPosition = playerPosition;
            PlayerMass = playerMass;
            PlayerDiameter = playerDiameter;
        }
    }

}
