using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PlayerDataNamespace;
using FoodNamespace;
using RankMemberNamespace;

namespace HostDataNamespace
{
    public class HostData
    {
        public int Mode { get; set; }
        public Content Content { get; set; }

        public HostData()
        {

        }
        public HostData(int mode, Content content)
        {
            Mode = mode;
            Content = content;
        }
    }

    public class Content
    {
        public string Message { get; set; }
        public List<RankMember> Rank { get; set; }
        public int PlayerID { get; set; }
        public PlayerData PlayerData { get; set; }
        public List<Food> AddEllipse { get; set; }
        public List<int> eatenFood { get; set; }
        public Content()
        {

        }
        public Content(string message, int playerID)
        {
            Message = message;
            PlayerID = playerID;
        }
    }

}
