using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NetWork_000
{
    class Program
    {
        static List<Socket> ListClientSockets = new List<Socket>();
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        static void Main(string[] args)
        {
            Console.WriteLine("测试Socket通信：");
            Console.WriteLine("Test socket communication:");
            Console.WriteLine("-------------------------------------------------------------");
            Console.WriteLine("电脑_主机(PC_Host)←→服务器(Server)←→电脑_加入(PC_Join)");
            Console.WriteLine("                           ↑");
            Console.WriteLine("                           ↓");
            Console.WriteLine("                   电脑_加入(PC_Join)");
            Console.WriteLine("-------------------------------------------------------------");
            Console.WriteLine("1.服务端(Server)");
            Console.WriteLine("2.客户端_主机(Client_Host)");
            Console.WriteLine("3.客户端_加入(Client_Join)");
            char select = Console.ReadKey().KeyChar;
            Console.WriteLine();
            switch(select)
            {
                case '1':
                    {
                        Console.WriteLine("请输入端口：");
                        Console.WriteLine("Please input port:");
                        int port = Convert.ToInt32(Console.ReadLine());
                        Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint ServerEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), port);
                        ServerSocket.Bind(ServerEndPoint);
                        ServerSocket.Listen(10);
                        Console.WriteLine("正在监听......");
                        Console.WriteLine("Listening......");

                        Thread thread = new Thread(new ParameterizedThreadStart(Server_Listen));
                        thread.IsBackground = true;
                        thread.Name = "ServerThread";
                        thread.Start(ServerSocket);

                        Console.WriteLine("按任意键结束程序......");
                        Console.WriteLine("Press any key to end the exe......");
                        Console.ReadKey();
                        ServerSocket.Close();
                        break;
                    }
                case '2':
                    {
                        Console.WriteLine("请输入服务器ip：");
                        Console.WriteLine("Please input server's ip:");
                        String ip = Console.ReadLine();
                        Console.WriteLine("请输入服务器端口：");
                        Console.WriteLine("Please input server's port:");
                        int port = Convert.ToInt32(Console.ReadLine());

                        Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                        ServerSocket.Connect(ServerEndPoint);

                        Console.WriteLine("请输入游戏中监听的端口：");
                        Console.WriteLine("Please input the port which the game is listening on:");
                        int gamePort = Convert.ToInt32(Console.ReadLine());

                        Client_HostOrJoin_LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), gamePort);
                        Client_HostOrJoin_LocalSocket.Connect(LocalEndPoint);
                        Console.WriteLine("连接成功！");
                        Console.WriteLine("Connect successfully!");

                        ListClientSockets.Add(Client_HostOrJoin_LocalSocket);

                        Thread t1 = new Thread(new ParameterizedThreadStart(TransferForHost_ServerToLocal));
                        Thread t2 = new Thread(new ParameterizedThreadStart(TransferForHost_LocalToServer));
                        t1.IsBackground = true;
                        t2.IsBackground = true;
                        t1.Name = "Client_Host_ServerSocket-LocalSocket";
                        t2.Name = "Client_Host_LocalSocket-ServerSocket";
                        Data aData ;
                        aData.socket_1 = ServerSocket;
                        aData.socket_2 = null;
                        aData.port = gamePort;
                        object obj2 = (object)aData;
                        t1.Start(ServerSocket);
                        t2.Start(obj2);


                        Console.ReadKey();
                        ServerSocket.Close();
                        t1.Abort();
                        t2.Abort();
                        break;
                    }
                case'3':
                    {
                        Console.WriteLine("请输入服务器ip：");
                        Console.WriteLine("Please input server's ip:");
                        String ip = Console.ReadLine();
                        Console.WriteLine("请输入服务器端口：");
                        Console.WriteLine("Please input server's port:");
                        int port = Convert.ToInt32(Console.ReadLine());

                        Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                        ServerSocket.Connect(ServerEndPoint);

                        Console.WriteLine("请输入需转发的端口：");
                        Console.WriteLine("Please input the port which is need to be mapped:");
                        int mappedPort = Convert.ToInt32(Console.ReadLine());

                        Socket LocalServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), mappedPort);
                        LocalServerSocket.Bind(LocalEndPoint);
                        LocalServerSocket.Listen(10);
                        Client_HostOrJoin_LocalSocket = LocalServerSocket.Accept();
                        Console.WriteLine("成功接受！");
                        Console.WriteLine("Accept successfully!");

                        ListClientSockets.Add(LocalServerSocket);
                        ListClientSockets.Add(Client_HostOrJoin_LocalSocket);

                        Thread t1 = new Thread(new ParameterizedThreadStart(TransferForJoin_ServerToLocal));
                        Thread t2 = new Thread(new ParameterizedThreadStart(TransferForJoin_LocalToServer));
                        t1.IsBackground = true;
                        t2.IsBackground = true;
                        t1.Name = "Client_Join_LocalSocket-ServerSocket";
                        t2.Name = "Client_Join_ServerSocket-LocalSocket";
                        Data aData;
                        aData.socket_1 = ServerSocket;
                        aData.socket_2 = LocalServerSocket;
                        aData.port = mappedPort;
                        object obj2 = (object)aData;
                        t1.Start(ServerSocket);
                        t2.Start(obj2);

                        Console.ReadKey();
                        ServerSocket.Close();
                        LocalServerSocket.Close();
                        t1.Abort();
                        t2.Abort();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("错误，请重新打开程序......");
                        Console.WriteLine("Error.Please open the exe again......");
                        break;
                    }
            }
            foreach (Socket s in ListClientSockets)
            {
                s.Close();
            }
            ListClientSockets.Clear();
            //Console.ReadKey();
        }
        /////////////Server://////////////////
        public static void Server_Listen(object obj)
        {
            Socket ServerSocket = obj as Socket;
            while(true)
            {
                Socket Client = null;
                try
                {
                    Client = ServerSocket.Accept();

                    Console.WriteLine(Client.RemoteEndPoint.ToString() + " 已连接！");
                    Console.WriteLine(Client.RemoteEndPoint.ToString() + " Connected!");
                    ListClientSockets.Add(Client);
                    Thread Thread_RecvAndSend = new Thread(new ParameterizedThreadStart(Server_RecvAndSend));
                    Thread_RecvAndSend.IsBackground = true;
                    Thread_RecvAndSend.Name = "ServerThread_RecvAndSend_"+Convert.ToString(ListClientSockets.Count);
                    Thread_RecvAndSend.Start(Client);
                    Client = null;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        public static void Server_RecvAndSend(object obj)
        {
            Socket Client = obj as Socket;

            while (true)
            {

                //
                Thread.Sleep(1);
                byte[] data = new byte[1024*1024*2];
                int size = 0;
                try
                {
                    size = Client.Receive(data);

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + "已接收自"+Client.RemoteEndPoint.ToString()+":" + size + "B");
                    if(size == 0)
                    {
                        Client.Close();
                        ListClientSockets.Remove(Client);
                        break;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + e.Message);
                    Client.Close();
                    ListClientSockets.Remove(Client);
                    break;
                }

                //这2条语句不能放在Receive前，否则会少发送一些数据！！！！！！
                List<Socket> LSocket = new List<Socket>(ListClientSockets);
                LSocket.Remove(Client);
                foreach (Socket s in LSocket)
                {
                    try
                    {
                        s.Send(data.Take(size).ToArray());

                        //
                        Console.WriteLine(Thread.CurrentThread.Name + "已发送至" + s.RemoteEndPoint.ToString() + ":" + size + "B");
                    }
                    catch(Exception e)
                    {

                        //
                        Console.WriteLine(Thread.CurrentThread.Name + ":" + e.Message);
                        continue;
                    }
                    
                }
                size = 0;
            }
        }
        //////////////////////Client://////////////////////////
        private static Socket Client_HostOrJoin_LocalSocket = null;
        struct Data
        {
            public Socket socket_1;
            public Socket socket_2;
            public int port;
        }
        //Client_Host:
        public static void TransferForHost_ServerToLocal(object obj)
        {
            Socket ServerSocket = obj as Socket;

            //try
            //{
                while (true)
                {

                    //
                    Thread.Sleep(1);
                    byte[] data = new byte[1024 * 1024 * 2];
                    int size = ServerSocket.Receive(data);

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已接收自" + ServerSocket.RemoteEndPoint.ToString() + ":" + size + "B");

                    try
                    {
                    Client_HostOrJoin_LocalSocket.Send(data.Take(size).ToArray());

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已发送至" + Client_HostOrJoin_LocalSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error:"+e.Message);
                    }

                    //if (size == 0)
                    //{
                    //    ServerSocket.Close();
                    //    Client_HostOrJoin_LocalSocket.Close();
                    //    break;
                    //}
                }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error:" + e.Message);
            //}
        }
        public static void TransferForHost_LocalToServer(object obj)
        {
            Socket ServerSocket = ((Data)obj).socket_1;//服务器Socket/Server socket
            int GamePort = ((Data)obj).port;

            //try
            //{
                while (true)
                {

                    //
                    Thread.Sleep(1);
                    byte[] data = new byte[1024 * 1024 * 2];
                    int size = Client_HostOrJoin_LocalSocket.Receive(data);

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已接收自" + Client_HostOrJoin_LocalSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    ServerSocket.Send(data.Take(size).ToArray());

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已发送至" + ServerSocket.RemoteEndPoint.ToString() + ":" + size + "B");

                    if (size == 0)
                    {
                        Client_HostOrJoin_LocalSocket.Close();
                        ListClientSockets.Remove(Client_HostOrJoin_LocalSocket);
                        Client_HostOrJoin_LocalSocket = null;
                        Console.WriteLine("已断开，正在重新连接......");
                        Console.WriteLine("Disconnected.Reconnecting......");
                        Client_HostOrJoin_LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), GamePort);
                        Client_HostOrJoin_LocalSocket.Connect(LocalEndPoint);
                        Console.WriteLine("连接成功！");
                        Console.WriteLine("Connected successfully!");
                        ListClientSockets.Add(Client_HostOrJoin_LocalSocket);
                    }
                }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error:" + e.Message);
            //}
        }
        //Client_Join
        //public static void Transfer(object obj)
        //{
        //    Socket Socket_1 = ((Socket[])obj)[0];
        //    Socket Socket_2 = ((Socket[])obj)[1];

        //    try
        //    {
        //        while (true)
        //        {

        //            //
        //            Thread.Sleep(1);
        //            byte[] Data = new byte[1024*1024*10];
        //            int size = Socket_1.Receive(Data);

        //            //
        //            Console.WriteLine(Thread.CurrentThread.Name + ":" + "已接收自"+Socket_1.RemoteEndPoint.ToString()+":" + size + "B");
        //            Socket_2.Send(Data.Take(size).ToArray());

        //            //
        //            Console.WriteLine(Thread.CurrentThread.Name + ":" + "已发送至"+Socket_2.RemoteEndPoint.ToString()+":" + size + "B");

        //            if(size == 0)
        //            {
        //                Socket_1.Close();
        //                Socket_2.Close();
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error:" + e.Message);
        //    }
        //}

        public static void TransferForJoin_ServerToLocal(object obj)
        {
            Socket ServerSocket = obj as Socket;

            //try
            //{
                while (true)
                {

                    //
                    Thread.Sleep(1);
                    byte[] data = new byte[1024 * 1024 * 2];
                    int size = ServerSocket.Receive(data);

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已接收自" + ServerSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    try
                    {

                        Client_HostOrJoin_LocalSocket.Send(data.Take(size).ToArray());

                        //
                        Console.WriteLine(Thread.CurrentThread.Name + ":" + "已发送至" + Client_HostOrJoin_LocalSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error:"+e.Message);
                    }

                    //if (size == 0)
                    //{
                    //    ServerSocket.Close();
                    //    Client_HostOrJoin_LocalSocket.Close();
                    //    break;
                    //}
                }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error:" + e.Message);
        //    }
        }

        public static void TransferForJoin_LocalToServer(object obj)
        {
            Socket ServerSocket = ((Data)obj).socket_1;
            Socket LocalServerSocket = ((Data)obj).socket_2;
            int mappedPort = ((Data)obj).port;
            //try
            //{
                while (true)
                {
                    //
                    Thread.Sleep(1);
                    byte[] data = new byte[1024*1024*2];
                    int size = Client_HostOrJoin_LocalSocket.Receive(data);

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已接收自" + Client_HostOrJoin_LocalSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    ServerSocket.Send(data.Take(size).ToArray());

                    //
                    Console.WriteLine(Thread.CurrentThread.Name + ":" + "已发送至" + ServerSocket.RemoteEndPoint.ToString() + ":" + size + "B");
                    if (size == 0)
                    {
                        Client_HostOrJoin_LocalSocket.Close();
                        ListClientSockets.Remove(Client_HostOrJoin_LocalSocket);
                        Client_HostOrJoin_LocalSocket = null;
                        Console.WriteLine("已断开，正在重新接受......");
                        Console.WriteLine("Disconnected.Reaccepting......");
                        Client_HostOrJoin_LocalSocket = LocalServerSocket.Accept();
                        Console.WriteLine("接受成功！");
                        Console.WriteLine("Accepted successfully!");
                        ListClientSockets.Add(Client_HostOrJoin_LocalSocket);

                    }
                }
            //}
            //catch(Exception e)
            //{
            //    Console.WriteLine("Error:" + e.Message);
            //}
        }
    }
}
