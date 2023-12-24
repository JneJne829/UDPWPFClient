using System;
using System.IO;
using System.IO.Ports;
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
using RankMemberNamespace;


namespace UDPWPFClient
{
    public partial class MainWindow : Window
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.66.51"), 11000);
        private ClientData clientData = new ClientData(0, "",0, new PlayerPoint(0, 0), 0);
        SerialPort mySerialPort = new SerialPort("COM4", 115200);
        private Random random = new Random();
        private ConcurrentDictionary<int, Ellipse> playerList = new ConcurrentDictionary<int, Ellipse>();
        private ConcurrentDictionary<int, Ellipse> ellipseMap = new ConcurrentDictionary<int, Ellipse>();
        private ConcurrentDictionary<int, int> ballsEatenPerSecond = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<int, int> playerMassPerSecond = new ConcurrentDictionary<int, int>();
        private PlayerPoint mousePosition = new PlayerPoint(0, 0);
        private PlayerPoint playerPosition = new PlayerPoint(0, 0);
        private List<Food> foods = new List<Food>();
        private Color playerColor = Colors.Green;
        private int isUsingJoystick = 0;
        private bool initMouse = false;
        private bool isRun = true; 
        private int status = 0;
        private int playerID = -1;
        private int colorPtr = 0;
        private int eatenFood = 0;
        private int eatenPlayer = 0;
        private const double JoystickCenter = 512.0;
        private const double MovementFactor = 0.05;
        private double playerDiameter;
        private DateTime setTime;
        private DateTime DeletePlayersetTime;
        private DateTime aliveTime;
        private String playerName = "UnknownCell";
        private List<Color> colorList = new List<Color>
            {
                Colors.Olive,Colors.Red,Colors.Blue,Colors.Yellow,Colors.Orange,
                Colors.Purple,Colors.Magenta,Colors.Cyan,Colors.Lime,Colors.Pink,
                Colors.Green,Colors.Olive,Colors.Brown,Colors.Gold,Colors.Silver,
                Colors.Violet,Colors.Indigo,Colors.Maroon,Colors.Navy,Colors.Turquoise
            };

        private Thread? tcpListenerThread;
        private TcpListener? tcpListener;
        private TcpClient? tcpClient;
        private StreamReader? reader;
        private volatile bool isListening = true;

        public MainWindow()
        {
            InitializeComponent();
            aliveTime = DateTime.Now;
            udpClient = new UdpClient(0); // 不指定端口号，系统会自动选择一个可用端口
            InterfaceSelector(0);
            StartTcpListener();
            Task.Run(() => StartUdpListening()); // 在后台开始监听
            Task.Run(() => MouseUpdate());
            Task.Run(() => SendWithTimeout());
        }

        private void StartTcpListener()
        {      
            
            tcpListenerThread = new Thread(new ThreadStart(() =>
            {
                while (isUsingJoystick != 2)
                    ;
                tcpListener = new TcpListener(IPAddress.Any, 5000);
                tcpListener.Start();
                tcpClient = tcpListener.AcceptTcpClient();
                reader = new StreamReader(tcpClient.GetStream());

                try
                {
                    while (isListening)
                    { 
                        while (isUsingJoystick == 2)
                        {
                            string? data = reader.ReadLine();
                            if (data != null)
                            {
                                string[] parts = data.Split(',');
                                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                                {
                                    JoystickDataProcessing(x, y);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }));
            tcpListenerThread.Start();
        }

        private void SendWithTimeout()
        {            
            while (isRun)
            {              
                if (status == 1)
                {
                    if((DateTime.Now - setTime).TotalSeconds > 3)
                    {
                        MessageBox.Show("Server connection failed.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        status = 0;
                    }
                }
                else if(status == 3)
                {
                    if ((DateTime.Now - setTime).TotalSeconds > 3)
                    {
                        status = 0;
                        Dispatcher.Invoke(() =>
                        {
                            Init();
                            InterfaceSelector(0);
                        });
                                            
                    }
                }
            }
        }

        private void MouseUpdate()
        {
            while (isRun)
            {                
                if (status == 2)
                {
                    SendData(status, playerName, playerID, mousePosition);

                    Dispatcher.InvokeAsync(() =>
                    {
                        Canvas.SetLeft(Mouse, mousePosition.X - (Mouse.Height / 2));
                        Canvas.SetTop(Mouse, mousePosition.Y - (Mouse.Width / 2));
                        Mouse.Height = playerDiameter / 2;
                        Mouse.Width = playerDiameter / 2;
                    });
                }
                Thread.Sleep(2); // 按照您的要求，每4毫秒发送一次
            }
        }
        private void StartUdpListening()
        {
            try
            {
                while (isRun)
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
                        case 3:
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

                                if (hostData.Content.PlayerID == playerID)
                                {
                                    if (!initMouse)
                                    {
                                        initMouse = true;
                                        mousePosition = hostData.Content.PlayerData.PlayerPosition;
                                    }
                                    playerPosition = hostData.Content.PlayerData.PlayerPosition;
                                    playerDiameter = hostData.Content.PlayerData.PlayerDiameter;
                                    eatenFood += hostData.Content.PlayerData.EatenFood;
                                    ballsEatenPerSecond.AddOrUpdate(((int)(DateTime.Now - aliveTime).TotalSeconds), 0, (k, oldValue) => oldValue + hostData.Content.PlayerData.EatenFood);
                                    playerMassPerSecond[(int)(DateTime.Now - aliveTime).TotalSeconds] = (int)hostData.Content.PlayerData.PlayerMass;
                                    eatenPlayer = hostData.Content.PlayerData.EatenPlayer;
                                }

                            }
                            else if (hostData.Mode == 4)
                            {
                                UpdateRanking(hostData.Content.Rank);
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

        private void UpdateRanking(List<RankMember> members)
        {
            // Sort the ranking by the Mass property in descending order and take the top five
            var topMembers = members.OrderByDescending(m => m.Mass).Take(5).ToList();

            // Update UI
            Dispatcher.Invoke(() => {
                RankingListView.ItemsSource = topMembers; // Ensure this matches the name of your ListView in XAML
            });
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
                SendData(status, playerName, playerID = random.Next(0, int.MaxValue), new PlayerPoint());
        }
        private void Init()
        {
            initMouse = false;
            eatenPlayer = 0;
            eatenFood = 0;
            aliveTime = DateTime.Now;
            mousePosition = new PlayerPoint(50, 50);
            clientData = new ClientData(0, playerName, 0, new PlayerPoint(0, 0), 0);
            ballsEatenPerSecond = new ConcurrentDictionary<int, int>();
            playerMassPerSecond = new ConcurrentDictionary<int, int>();
            playerList = new ConcurrentDictionary<int, Ellipse>();
            ellipseMap = new ConcurrentDictionary<int, Ellipse>();
            foods = new List<Food>();
            List<UIElement> ellipsesToRemove = new List<UIElement>();

            // 遍历 GameCanvas 中的所有子元素
            foreach (UIElement child in GameCanvas.Children)
            {
                // 检查是否为 Ellipse 类型
                if (child is Ellipse)
                {
                    ellipsesToRemove.Add(child);
                }
            }

            // 移除所有已识别的 Ellipse 元素
            foreach (Ellipse ellipse in ellipsesToRemove)
            {
                GameCanvas.Children.Remove(ellipse);
            }
            playerID = -1;
    }
        private void DeletePlayer(HostData hostData)
        {
            DeletePlayersetTime = DateTime.Now;
            if (playerList.ContainsKey(hostData.Content.PlayerID))
            {
                Ellipse player = playerList[hostData.Content.PlayerID];
                GameCanvas.Children.Remove(player);
                playerList.TryRemove(hostData.Content.PlayerID, out _);                
            }
            if(hostData.Content.PlayerID == playerID)
            {
                setTime = DateTime.Now;
                status = 3;
                MatchResults matchResults = new MatchResults((int)(DateTime.Now - aliveTime).TotalSeconds, eatenPlayer, eatenFood, ballsEatenPerSecond, playerMassPerSecond);
                matchResults.Show();
                //MessageBox.Show($"You Died | Eaten {eatenPlayer} Players | Eaten {eatenFood} Foods | Time Alive {(((int)((DateTime.Now - aliveTime).TotalSeconds)) / 60).ToString("D2")}:{(((int)((DateTime.Now - aliveTime).TotalSeconds)) % 60).ToString("D2")}");
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

                    //StatusText.Text = $"{((int)hostData.Content.PlayerData.PlayerMass).ToString()}";
                }
                playerList[hostData.Content.PlayerID] = cell;               
            }
            else
            {
                Ellipse cell = new Ellipse();
                cell.Fill = new SolidColorBrush(colorList[(hostData.Content.PlayerData.PlayerColor % colorList.Count)]);
                cell.Width = hostData.Content.PlayerData.PlayerDiameter;
                cell.Height = hostData.Content.PlayerData.PlayerDiameter;

                playerList[hostData.Content.PlayerID] = cell;
                GameCanvas.Children.Add(cell);
                Canvas.SetLeft(cell, hostData.Content.PlayerData.PlayerPosition.X);
                Canvas.SetTop(cell, hostData.Content.PlayerData.PlayerPosition.Y);
            }
            AddFood(hostData.Content.AddEllipse);
            RemoveFood(hostData.Content.eatenFood);
            UpdateEllipseZIndices(GameCanvas);
        }
        private void UpdateEllipseZIndices(Canvas myCanvas)
        {
            foreach (var child in myCanvas.Children)
            {
                if (child is Ellipse ellipse)
                {
                    // 這裡我們以 Ellipse 的面積作為 ZIndex 的依據
                    double area = ellipse.Width * ellipse.Height;
                    Canvas.SetZIndex(ellipse, (int)area);
                }
            }
        }
        private double CalculateScale(double Diameter)
        {
            return (1 / SelectAppropriateRatio(Diameter)) / (Diameter / this.ActualHeight);
        }
        private double SelectAppropriateRatio(double diameter)
        {
            if (diameter <= 10)
                return 8;
            else if (diameter <= 30)
            {
                // 10 到 30 之間，從 8 平滑過渡到 6.5
                double minDiameter1 = 10;
                double maxDiameter1 = 30;
                double minRatio1 = 8;
                double maxRatio1 = 6.5;

                double progress1 = (diameter - minDiameter1) / (maxDiameter1 - minDiameter1);
                return minRatio1 + (maxRatio1 - minRatio1) * progress1;
            }
            else if (diameter <= 60)
            {
                // 30 到 60 之間，從 6.5 平滑過渡到 4.5
                double minDiameter2 = 30;
                double maxDiameter2 = 60;
                double minRatio2 = 6.5;
                double maxRatio2 = 4.5;

                double progress2 = (diameter - minDiameter2) / (maxDiameter2 - minDiameter2);
                return minRatio2 + (maxRatio2 - minRatio2) * progress2;
            }
            else if (diameter >= 200)
                return 3;
            else
            {
                // 60 到 200 之間，從 4.5 平滑過渡到 3
                double minDiameter3 = 60;
                double maxDiameter3 = 200;
                double minRatio3 = 4.5;
                double maxRatio3 = 3;

                double progress3 = (diameter - minDiameter3) / (maxDiameter3 - minDiameter3);
                return minRatio3 + (maxRatio3 - minRatio3) * progress3;
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
            LeaderboardView.Visibility = Visibility.Collapsed;
            RankingListView.Visibility = Visibility.Collapsed;
            GameLogo.Visibility = Visibility.Collapsed;
            NameLabel.Visibility = Visibility.Collapsed;
            IpLabel.Visibility = Visibility.Collapsed;
            ChangeColorButton.Visibility = Visibility.Collapsed;
            UsingJoystick.Visibility = Visibility.Collapsed;
            StartGameButton.Visibility = Visibility.Collapsed;
            //StartGameButton.Visibility = Visibility.Visible;
            Player.Visibility = Visibility.Collapsed;
            Mouse.Visibility = Visibility.Collapsed;
        }
        private void Main_Page()
        {
            GameLogo.Visibility = Visibility.Visible;
            NameLabel.Visibility = Visibility.Visible;
            StartGameButton.Visibility = Visibility.Visible;
            ChangeColorButton.Visibility = Visibility.Visible;
            UsingJoystick.Visibility = Visibility.Visible;
            IpLabel.Visibility = Visibility.Visible;
        }
        private void Game_Page()
        {
            LeaderboardView.Visibility = Visibility.Visible;
            RankingListView.Visibility = Visibility.Visible;
            Player.Visibility = Visibility.Visible;
            //Mouse.Visibility = Visibility.Visible;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (status == 0)
            {
                setTime = DateTime.Now;
                aliveTime = DateTime.Now;
                status = 1;
                clientData.Color = colorPtr % colorList.Count;
                await Task.Run(() => SendData(status, playerName, playerID = random.Next(0, int.MaxValue), new PlayerPoint()));
            }
                
        }
        private void RankingListView_MouseMove(object sender, MouseEventArgs e)
        {
            // 轉換點到 Canvas 的坐標系
            Point position = e.GetPosition(GameCanvas);

            // 呼叫 Canvas 的 MouseMove 事件處理器
            GameCanvas_MouseMove(GameCanvas, e);
        }
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (status == 2 && isUsingJoystick == 0)
            {
                Point position = e.GetPosition(GameCanvas);
                mousePosition.X = position.X;
                mousePosition.Y = position.Y;
            }
        }

        private void SendData(int mode, String name, int number, PlayerPoint position)
        {
            try
            {
                clientData.Mode = mode;
                clientData.Name = name;
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

        private void ChangeColor_Click(object sender, MouseButtonEventArgs e)
        {
            // 示例：更改顏色
            colorPtr++;
            ChangeColorButton.Foreground = new SolidColorBrush(colorList[colorPtr % colorList.Count]);          

            // 其他邏輯...
        }

        private void UsingJoystick_Click(object sender, MouseButtonEventArgs e)
        {
            SolidColorBrush brush = UsingJoystick.Foreground as SolidColorBrush;
            Color graphiteGray = Color.FromRgb(50, 50, 50);
            Color customViolet = Color.FromRgb(148, 0, 211);
            switch (isUsingJoystick)
            {
                case 0:
                    if (OpenMySerialPort(false))
                    {
                        UsingJoystick.Foreground = new SolidColorBrush(customViolet);
                        UsingJoystick.Content = "Joystick : Port";
                        isUsingJoystick = 1;
                    }
                    else
                    {
                        UsingJoystick.Foreground = new SolidColorBrush(customViolet);
                        UsingJoystick.Content = "Joystick : TCP/IP";
                        isUsingJoystick = 2;
                    }
                    break;
                case 1:
                    //OpenMySerialPort(false);
                    UsingJoystick.Foreground = new SolidColorBrush(customViolet);
                    UsingJoystick.Content = "Joystick : TCP/IP";
                    isUsingJoystick = 2;
                    break;
                case 2:
                    UsingJoystick.Foreground = new SolidColorBrush(graphiteGray);
                    UsingJoystick.Content = "Joystick : Disable";
                    isUsingJoystick = 0;
                    break;  
            }           
            
        }
        private bool OpenMySerialPort(bool response)
        {
            string[] ports = SerialPort.GetPortNames();
            if (!ports.Contains("COM4"))
            {
                MessageBox.Show("COM4 端口未找到，请检查设备连接。");
                return false;
            }

            if (response) // 打开端口
            {
                if (!mySerialPort.IsOpen)
                {
                    try
                    {
                        mySerialPort.DataReceived -= MySerialPort_DataReceived;
                        mySerialPort.DataReceived += MySerialPort_DataReceived;
                        mySerialPort.Open();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开端口: {ex.Message}");
                        return false;
                    }
                }
            }
            else // 关闭端口
            {
                if (mySerialPort.IsOpen)
                {
                    mySerialPort.DataReceived -= MySerialPort_DataReceived;
                    mySerialPort.Close();
                    return true;
                }
            }

            return false;
        }

        private void MySerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = mySerialPort.ReadLine();
            string[] parts = data.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                //MessageBox.Show($"{x}, {y}");
                JoystickDataProcessing(x, y);
            }
        }

        private void JoystickDataProcessing(double x, double y)
        {
            if (isUsingJoystick != 0)
            {
                if (Math.Abs(x - JoystickCenter) < 3)
                    mousePosition.X = playerPosition.X + (playerDiameter / 2);
                else
                {
                    if (Math.Abs(playerPosition.X - mousePosition.X) < playerDiameter * 2)
                        mousePosition.X += (x - JoystickCenter) * MovementFactor * 3;
                    else if ((mousePosition.X - playerPosition.X) > 0)
                        mousePosition.X = playerPosition.X + (playerDiameter / 2) + (playerDiameter * 2);
                    else if ((mousePosition.X - playerPosition.X) <= 0)
                        mousePosition.X = playerPosition.X + (playerDiameter / 2) - (playerDiameter * 2);
                }

                if (Math.Abs(y - JoystickCenter) < 5)
                    mousePosition.Y = playerPosition.Y + playerDiameter / 2;
                else
                {
                    if (Math.Abs(playerPosition.Y - mousePosition.Y) < playerDiameter * 2)
                        mousePosition.Y += (y - JoystickCenter) * MovementFactor * 3;
                    else if ((mousePosition.Y - playerPosition.Y) > 0)
                        mousePosition.Y = playerPosition.Y + (playerDiameter / 2) + (playerDiameter * 2);
                    else if ((mousePosition.Y - playerPosition.Y) <= 0)
                        mousePosition.Y = playerPosition.Y + (playerDiameter / 2) - (playerDiameter * 2);
                }

            }
        }
        private double CalculateDistance(PlayerPoint x, PlayerPoint y)
        {
            double xDistance = x.X - y.X;
            double yDistance = x.Y - y.Y;

            return Math.Sqrt(xDistance * xDistance + yDistance * yDistance);
        }
        private void Name_Click(object sender, MouseButtonEventArgs e)
        {
            EnterName enterName = new EnterName();
            if (enterName.ShowDialog() == true) // 检查对话框返回值
            {
                string input = enterName.ResponseText;
                if (String.IsNullOrWhiteSpace(input))
                {
                    NameLabel.Content = "UnknownCell";
                    playerName = "UnknownCell";
                }
                else
                {
                    NameLabel.Content = input; // 更新标签内容
                    playerName = input;
                }
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
            SendData(-1, playerName, playerID, new PlayerPoint());
            isListening = false; // 通知所有线程停止运行
            isRun = false;

            // 关闭 TCP 监听器
            tcpListener?.Stop();

            // 关闭串行端口
            if (mySerialPort.IsOpen)
            {
                mySerialPort.Close();
            }

            // 关闭 UDP 客户端
            udpClient?.Close();

            // 终止 TCP 监听线程
            if (tcpListenerThread != null && tcpListenerThread.IsAlive)
            {
                tcpListenerThread.Abort(); // 或者使用其他合适的方式来终止线程
            }

            // 确保关闭所有其他资源和线程

            Application.Current.Shutdown(); // 退出应用程序
        }



    }
}

