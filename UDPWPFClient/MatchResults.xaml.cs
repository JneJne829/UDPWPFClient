using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Concurrent;

namespace UDPWPFClient
{
    /// <summary>
    /// MatchResults.xaml 的互動邏輯
    /// </summary>
    public partial class MatchResults : Window
    {
        public MatchResults(int aliveTime, int eatenPlayer, int eatenFood, ConcurrentDictionary<int, int> ballsEatenPerSecond, ConcurrentDictionary<int, int> playerMassPerSecond)
        {
            InitializeComponent();

            // 设置基本信息
            aliveTimeText.Text = aliveTime.ToString();
            eatenPlayerText.Text = eatenPlayer.ToString();
            eatenFoodText.Text = eatenFood.ToString();

            // 设置字典数据到 ListView
            ballsEatenListView.ItemsSource = ballsEatenPerSecond;
            playerMassListView.ItemsSource = playerMassPerSecond;
        }
    }
}
