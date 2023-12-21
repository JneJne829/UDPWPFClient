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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using PlayerDataNamespace;
using HostDataNamespace;
using ClientDataNamespace;
using PlayerPointNamespace;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;
using FoodNamespace;


namespace UDPWPFClient
{
    public partial class MainWindow : Window
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.66.51"), 11000);
        private ClientData clientData = new ClientData(0, 0, new PlayerPoint(0, 0));
        private Random random = new Random();
        private ConcurrentDictionary<int, Ellipse> playerList = new ConcurrentDictionary<int, Ellipse>();
        private ConcurrentDictionary<int, Ellipse> ellipseMap = new ConcurrentDictionary<int, Ellipse>();
        private PlayerPoint playerPosition = new PlayerPoint(0, 0);
        private List<Food> foods = new List<Food>();
        private int status = 0;
        private int playerID = -1;
        private static DateTime setTime;

        public MainWindow()
        {
            InitializeComponent();
            udpClient = new UdpClient(0); // 不指定端口号，系统会自动选择一个可用端口
            InterfaceSelector(0);
            Task.Run(() => StartListening()); // 在后台开始监听
            Task.Run(() => MouseUpdate());
            Task.Run(() => SendWithTimeout());
        }
        private void SendWithTimeout()
        {
            while (true)
            {
                setTime = DateTime.Now;
                if (status == 1)
                {
                    if((DateTime.Now - setTime).TotalSeconds > 3)
                    {
                        MessageBox.Show("Server connection failed.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        status = 0;
                    }
                }
            }
        }

        private void MouseUpdate()
        {
            while (true)
            {                
                if (status == 2)
                {
                    SendData(status, playerID, playerPosition);
                }
                Thread.Sleep(2); // 按照您的要求，每4毫秒发送一次
            }
        }
        private void StartListening()
        {
            try
            {
                while (true)
                {
                    // 接收數據
                    var serverResponse = udpClient.Receive(ref serverEndPoint);

                    // 反序列化接收到的數據
                    var hostData = JsonSerializer.Deserialize<HostData>(serverResponse);

                    switch (status)
                    {
                        case 1:
                            if (hostData.Mode == 0 && hostData.Content.PlayerID == playerID) //如果是本Client的創建訊息
                            {
                                ReceiveCreationMessage(hostData);
                            }
                            break;
                        case 2:
                            if (hostData.Mode == 1)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    switch (hostData.Content.Message)
                                    {
                                        case "PlayerMove" :
                                            UpdatedPlayerUI(hostData);
                                            break;
                                        case "Delete" :
                                            DeletePlayer(hostData);
                                            break;
                                    }
                                });
                                
                            }
                            break;
                    }                    
                }
            }
            catch (ObjectDisposedException)
            {
                // UdpClient 被關閉時會拋出此異常
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error: {ex.Message} 請重新啟動");
                });
            }
        }
        private async void ReceiveCreationMessage(HostData hostData)
        {
            if (hostData.Content.Message == "Generating")
            {
                if (hostData.Content.AddEllipse != null)
                {
                    foods.AddRange(hostData.Content.AddEllipse);
                }
            }                    
            else if (hostData.Content.Message == "Success")
            {
                Dispatcher.Invoke(() =>
                {
                    InterfaceSelector(1);
                    if(foods.Count != 0)
                    AddFood(foods);
                });
                status = 2;            
            }
            while (hostData.Content.Message == "Failure")
                SendData(status, playerID = random.Next(0, int.MaxValue), new PlayerPoint());
        }
        private void DeletePlayer(HostData hostData)
        {
            if (playerList.ContainsKey(hostData.Content.PlayerID))
            {
                Ellipse player = playerList[hostData.Content.PlayerID];
                GameCanvas.Children.Remove(player);
                playerList.TryRemove(hostData.Content.PlayerID, out _);
            }
                
        }
        private void UpdatedPlayerUI(HostData hostData)
        {
            if (playerList.ContainsKey(hostData.Content.PlayerID))
            {
                Ellipse cell = playerList[hostData.Content.PlayerID];
                cell.Width = hostData.Content.PlayerData.PlayerDiameter;
                cell.Height = hostData.Content.PlayerData.PlayerDiameter;
                
                Canvas.SetLeft(cell, hostData.Content.PlayerData.PlayerPosition.X);
                Canvas.SetTop(cell, hostData.Content.PlayerData.PlayerPosition.Y);
                
                if (hostData.Content.PlayerID == playerID)
                {
                    double scale = CalculateScale(hostData.Content.PlayerData.PlayerDiameter);
                    double ellipseCenterX = hostData.Content.PlayerData.PlayerPosition.X + (hostData.Content.PlayerData.PlayerDiameter / 2);
                    double ellipseCenterY = hostData.Content.PlayerData.PlayerPosition.Y + (hostData.Content.PlayerData.PlayerDiameter / 2);

                    // 計算 ScrollViewer 中心點的偏移量，並考慮縮放比例
                    double offsetX = (ellipseCenterX * scale) - (scrollViewer.ViewportWidth / 2);
                    double offsetY = (ellipseCenterY * scale) - (scrollViewer.ViewportHeight / 2);


                    scrollViewer.ScrollToHorizontalOffset(offsetX);
                    scrollViewer.ScrollToVerticalOffset(offsetY);
                    
                    GameCanvasScaleTransform.ScaleX = scale;
                    GameCanvasScaleTransform.ScaleY = scale;
                    StatusText.Text = $"{((int)cell.ActualHeight).ToString()}";
                }
                playerList[hostData.Content.PlayerID] = cell;               
            }
            else
            {
                Ellipse cell = new Ellipse();
                cell.Fill = new SolidColorBrush(Colors.Green);
                cell.Width = hostData.Content.PlayerData.PlayerDiameter;
                cell.Height = hostData.Content.PlayerData.PlayerDiameter;

                playerList[hostData.Content.PlayerID] = cell;
                GameCanvas.Children.Add(cell);
                Canvas.SetLeft(cell, hostData.Content.PlayerData.PlayerPosition.X);
                Canvas.SetTop(cell, hostData.Content.PlayerData.PlayerPosition.Y);
            }
            AddFood(hostData.Content.AddEllipse);
            RemoveFood(hostData.Content.eatenFood);
        }
        private double CalculateScale(double Diameter)
        {
            return (1 / SelectAppropriateRatio(Diameter)) / (Diameter / this.ActualHeight);
        }
        private double SelectAppropriateRatio(double diameter)
        {
            if (diameter <= 30)
                return 8;
            else if (diameter >= 200)
                return 3;
            else
            {
                // 當直徑介於30到50之間時，使用線性插值計算比例
                // 這裡我們將30對應到8，將50對應到6
                double minDiameter = 30;
                double maxDiameter = 200;
                double minRatio = 8;
                double maxRatio = 3;

                // 計算直徑在這個範圍內的相對位置
                double progress = (diameter - minDiameter) / (maxDiameter - minDiameter);

                // 根據進度計算比例
                return minRatio + (maxRatio - minRatio) * progress;
            }
        }



        private void RemoveFood(List<int> eatenFood)
        {
            Ellipse foundEllipse;
            foreach (var FoodKey in eatenFood)
            {
                if (ellipseMap.ContainsKey(FoodKey))
                {
                    foundEllipse = ellipseMap[FoodKey];
                    if (foundEllipse != null)
                    {
                        GameCanvas.Children.Remove(foundEllipse);
                        ellipseMap.TryRemove(FoodKey, out var removedValue);
                    }
                }
                               
            }
        }
        private void AddFood(List<Food> AddEllipse)
        {
            List<Color> colors = new List<Color>
                {
                    Colors.Red,
                    Colors.Orange,
                    Colors.Yellow,
                    Colors.Green,
                    Colors.Blue,
                    Colors.Indigo,
                    Colors.Violet
                };
            foreach (Food food in AddEllipse)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = food.Diameter,
                    Height = food.Diameter,
                    Fill = new SolidColorBrush(colors[food.Color])
                };
                Canvas.SetLeft(ellipse, food.X);
                Canvas.SetTop(ellipse, food.Y);
                GameCanvas.Children.Add(ellipse);

                ellipseMap[food.Key] = ellipse;
            }
        }
        private void InterfaceSelector(int mode)
        {
            CloaseAllElement();
            switch (mode)
            {
                case 0:
                    Main_Page();
                    break;
                case 1:
                    Game_Page();
                    break;
            }
        }
        private void CloaseAllElement()
        {
            GameLogo.Visibility = Visibility.Collapsed;
            IpLabel.Visibility = Visibility.Collapsed;
            StartGameButton.Visibility = Visibility.Collapsed;
            //StartGameButton.Visibility = Visibility.Visible;
            Player.Visibility = Visibility.Collapsed;
        }
        private void Main_Page()
        {
            StartGameButton.Visibility = Visibility.Visible;
            GameLogo.Visibility = Visibility.Visible;
            IpLabel.Visibility = Visibility.Visible;
        }
        private void Game_Page()
        {
            Player.Visibility = Visibility.Visible;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (status == 0)
            {
                status = 1;
                await Task.Run(() => SendData(status, playerID = random.Next(0, int.MaxValue), new PlayerPoint()));
            }
                
        }

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (status == 2)
            {
                Point position = e.GetPosition(GameCanvas);
                playerPosition.X = position.X;
                playerPosition.Y = position.Y;
            }
        }

        private void SendData(int mode, int number, PlayerPoint position)
        {
            try
            {
                clientData.Mode = mode;
                clientData.Number = number;
                clientData.Position = position;

                // 序列化要傳送的數據
                byte[] bytesToSend = JsonSerializer.SerializeToUtf8Bytes(clientData);

                // 發送數據
                udpClient.Send(bytesToSend, bytesToSend.Length, serverEndPoint);
            }
            catch (SocketException socketEx)
            {
                // 服务器可能关闭或网络问题
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Unable to connect to the server: {socketEx.Message}");
                });
            }
            catch (ObjectDisposedException)
            {
                // UdpClient已被关闭
                /*Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("The connection is closed, please restart the client.");
                });*/
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error: {ex.Message}");
                });
            }
        }

        private void IpLabel_Click(object sender, MouseButtonEventArgs e)
        {
            InputDialog inputDialog = new InputDialog();
            if (inputDialog.ShowDialog() == true) // 检查对话框返回值
            {
                string inputIp = inputDialog.ResponseText;
                if (String.IsNullOrWhiteSpace(inputIp))
                    IpLabel.Content = "Enter IP";
                else
                IpLabel.Content = inputIp; // 更新标签内容

                // 验证并更新 IP 地址
                if (IPAddress.TryParse(inputIp, out IPAddress ip))
                {
                    // 如果输入的是有效的 IP 地址，则更新 serverEndPoint
                    serverEndPoint = new IPEndPoint(ip, serverEndPoint.Port);
                    // 或者，如果您想保持原来的 serverEndPoint 实例，可以这样做：
                    // serverEndPoint.Address = ip;
                }
                else
                {
                    // 如果输入的 IP 地址无效，可以在这里处理错误
                    MessageBox.Show("輸入的 IP 地址無效，請重新輸入。");
                }
            }
        }


        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 只有當用戶點擊並拖曳Grid時，才移動窗口
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SendData(-1, playerID, new PlayerPoint());
            udpClient?.Close();
            base.OnClosed(e);
        }
    }
}

