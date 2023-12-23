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
using LiveCharts;
using LiveCharts.Wpf;

namespace UDPWPFClient
{
    /// <summary>
    /// MatchResults.xaml 的互動邏輯
    /// </summary>
    public partial class MatchResults : Window
    {
        public SeriesCollection BallsEatenSeries { get; set; }
        public SeriesCollection PlayerMassSeries { get; set; }

        public MatchResults(int aliveTime, int eatenPlayer, int eatenFood,
                            ConcurrentDictionary<int, int> ballsEatenPerSecond,
                            ConcurrentDictionary<int, int> playerMassPerSecond)
        {
            InitializeComponent();


            // 设置基本信息
            aliveTimeText.Text = aliveTime.ToString();
            eatenPlayerText.Text = eatenPlayer.ToString();
            eatenFoodText.Text = eatenFood.ToString();

            // 初始化圖表數據
            BallsEatenSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<int>(ballsEatenPerSecond.Values)
                }
            };
            PlayerMassSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<int>(playerMassPerSecond.Values)
                }
            };

            DataContext = this;
        }
    }
}
