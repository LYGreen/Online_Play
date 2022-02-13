using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetWork_006
{
    class Program
    {
        static char Select = '\0';//1:服务器，2:客户端_主机，3:客户端_加入
        static String ip = "\0";//【服务器&客户端】分开使用
        static int port = 0;//【服务器&客户端】分开使用
        //Host1
        static Socket Host_ServerCommunicationSocket = null;
        static List<Socket> Host_ListServerSockets = new List<Socket>();
        static List<Socket> Host_ListLocalSockets = new List<Socket>();
        static int Host_ListenPort = 0;
        //Server
        static Socket Server_ListenSocket = null;
        static Socket Server_HostCommunicationSocket = null;
        static List<Socket> Server_ListHostSockets = new List<Socket>();
        static List<Socket> Server_ListClientSockets = new List<Socket>();
        //Join
        static Socket Join_ServerSocket = null;
        static Socket Join_LocalListenSocket = null;
        static Socket Join_LocalClientSocket = null;
        static int Join_MappedPort = 0;
        //Function//////////////////////////////////////////////////////////
        static byte[] byte_Combine(byte[] data1,byte[] data2)
        {
            byte[] Data = new byte[data1.Length + data2.Length];
            Array.Copy(data1, 0, Data, 0, data1.Length);
            Array.Copy(data2, 0, Data, data1.Length, data2.Length);
            return Data;
        }
        ////////////////////////////////////////////////////////////////////
        struct Server_Sockets //【服务器】使用
        {
            public Socket HostSocket;
            public Socket ClientSocket;
        }
        struct Host_Sockets //【主机】使用
        {
            public Socket LocalSocket;
            public Socket ServerSocket;
        }
        struct Join_Sockets //【加入】使用
        {
            public Socket ServerSocket;
            public Socket LocalSocket;
        }
        //Main//////////////////////////////////////////////////////////////
        static void Main(string[] args)
        {
            Console.WriteLine("联机工具");
            Console.WriteLine("1.服务器，2.客户端_主机，3.客户端_加入");
            Select = Console.ReadKey().KeyChar;
            Console.WriteLine();
            switch (Select)
            {
                case'1':
                    {
                        //Server
                        Console.WriteLine("请输入端口:");
                        port = Convert.ToInt32(Console.ReadLine());
                        Server_ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint Server_LocalEndPoint = new IPEndPoint (IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()),port);
                        Server_ListenSocket.Bind(Server_LocalEndPoint);
                        Server_ListenSocket.Listen(10);
                        Console.WriteLine("正在监听......");

                        Thread Thread_Listen = new Thread(new ThreadStart(Server_Listen));
                        Thread_Listen.Name = "Server_Listen";
                        Thread_Listen.IsBackground = true;
                        Thread_Listen.Start();

                        Console.ReadKey();
                        break;
                    }
                case'2':
                    {
                        //Host
                        Console.WriteLine("请输入服务器ip:");
                        ip = Console.ReadLine();
                        Console.WriteLine("请输入服务器端口:");
                        port = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("请输入监听端口:");
                        Host_ListenPort = Convert.ToInt32(Console.ReadLine());

                        Host_ServerCommunicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint Host_ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                        Console.WriteLine("正在连接服务器......");
                        Host_ServerCommunicationSocket.Connect(Host_ServerEndPoint);
                        Host_ServerCommunicationSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_HOST_COMMUNICATION_LYGREEN"));
                        Console.WriteLine("连接成功！");

                        Thread Thread_ReceiveMessage = new Thread(new ThreadStart(Host_ReceiveMessage));
                        Thread_ReceiveMessage.Name = "Host_ReceiveMessage";
                        Thread_ReceiveMessage.IsBackground = true;
                        Thread_ReceiveMessage.Start();

                        Console.ReadKey();
                        break;
                    }
                case'3':
                    {
                        //Join
                        Console.WriteLine("请输入服务器ip");
                        ip = Console.ReadLine();
                        Console.WriteLine("请输入服务器端口:");
                        port = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("请输入转发端口:");
                        Join_MappedPort = Convert.ToInt32(Console.ReadLine());

                        Thread Thread_LocalListen = new Thread(new ThreadStart(Join_LocalListen));
                        Thread_LocalListen.Name = "Join_LocalListen";
                        Thread_LocalListen.IsBackground = true;
                        Thread_LocalListen.Start();

                        Console.ReadKey();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("啊？你输入的是什么？");
                        break;
                    }
            }
        }
        /////////////////////////////////////////////////////////////////////////////////////
        //Server
        static void Server_Listen()
        {
            try
            {
                while(true)
                {
                    Socket ClientSocket = Server_ListenSocket.Accept();
                    byte[] FirstData = {};
                    bool BreakOut = false;
                    for(int i = 0;(i < 4) && (BreakOut == false);i++)
                    {
                        byte[] DataBuffer = new byte[2048];
                        int size = ClientSocket.Receive(DataBuffer);
                        FirstData = byte_Combine(FirstData, DataBuffer.Take(size).ToArray());
                        String FirstStr = Encoding.Unicode.GetString(FirstData);
                        switch(FirstStr)
                        {
                            case"MESSAGE_HOST_COMMUNICATION_LYGREEN":
                                {
                                    Server_HostCommunicationSocket = ClientSocket;
                                    Console.WriteLine("Message:已接受主机端消息Socket " + ClientSocket.RemoteEndPoint + " 的连接！");

                                    FirstData = new byte[] { };
                                    BreakOut = true;
                                    break;
                                }
                            case"MESSAGE_HOST_LYGREEN":
                                {
                                    Server_ListHostSockets.Add(ClientSocket);
                                    Console.WriteLine("Message:已接受主机端 " + ClientSocket.RemoteEndPoint + " 的连接！");

                                    Server_Sockets ThreadSocket = new Server_Sockets();
                                    if (Server_ListClientSockets.Count > 0)
                                    {
                                        ThreadSocket.HostSocket = ClientSocket;
                                        ThreadSocket.ClientSocket = Server_ListClientSockets[0];
                                        Server_ListClientSockets.Remove(ThreadSocket.ClientSocket);
                                    }

                                    Thread Thread_ReceiveFromServerAndSendToClient = new Thread(new ParameterizedThreadStart(Server_ReceiveFromHostAndSendToClient));
                                    Thread_ReceiveFromServerAndSendToClient.Name = "Server_ReceiveFromHostAndSendToClient";
                                    Thread_ReceiveFromServerAndSendToClient.IsBackground = true;

                                    Thread Thread_ReceiveFromClientAndSendToHost = new Thread(new ParameterizedThreadStart(Server_ReceiveFromClientAndSendToHost));
                                    Thread_ReceiveFromClientAndSendToHost.Name = "Server_ReceiveFromClientAndSendToHost";
                                    Thread_ReceiveFromClientAndSendToHost.IsBackground = true;

                                    Thread_ReceiveFromServerAndSendToClient.Start(ThreadSocket);
                                    Thread_ReceiveFromClientAndSendToHost.Start(ThreadSocket);

                                    FirstData = new byte[] { };
                                    BreakOut = true;
                                    break;
                                }
                            case"MESSAGE_JOIN_LYGREEN":
                                {
                                    Console.WriteLine("Message:已接受加入端 " + ClientSocket.RemoteEndPoint + " 的连接！");
                                    Server_ListClientSockets.Add(ClientSocket);

                                    //创建一个线程防止Server_Listen()线程被阻塞
                                    Thread Thread_Client_Work = new Thread(new ParameterizedThreadStart(Server_Thread_Client_Work));
                                    Thread_Client_Work.Name = "Server_Thread_Client_Work00";
                                    Thread_Client_Work.IsBackground = true;
                                    Thread_Client_Work.Start(ClientSocket);

                                    FirstData = new byte[] { };
                                    BreakOut = true;
                                    break;
                                }
                        }
                        Thread.Sleep(250);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        static void Server_Thread_Client_Work(object obj)
        {
            Socket ClientSocket = obj as Socket;
            Server_HostCommunicationSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_HOST_CREATESOCKET_LYGREEN"));
            ClientSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_JOIN_SUCCESSFULLY_LYGREEN"));
        }
        static void Server_ReceiveFromClientAndSendToHost(object obj)
        {
            Server_Sockets ThreadSocket = (Server_Sockets)obj;

            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = ThreadSocket.ClientSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + ThreadSocket.ClientSocket.RemoteEndPoint + " " + size + " B数据");
                    ThreadSocket.HostSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送至 " + ThreadSocket.HostSocket.RemoteEndPoint + " " + size + " B数据");
                    if (size == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        static void Server_ReceiveFromHostAndSendToClient(object obj)
        {
            Server_Sockets ThreadSocket = (Server_Sockets)obj;

            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = ThreadSocket.HostSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + ThreadSocket.HostSocket.RemoteEndPoint + " " + size + " B数据");
                    ThreadSocket.ClientSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送至 " + ThreadSocket.ClientSocket.RemoteEndPoint + " " + size + " B数据");
                    if (size == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////
        //Host
        static void Host_ReceiveMessage()
        {
            byte[] FirstData = { };
            try
            {
                while (true)
                {
                    byte[] DataBuffer = new byte[2048];
                    int size = Host_ServerCommunicationSocket.Receive(DataBuffer);
                    FirstData = byte_Combine(FirstData, DataBuffer.Take(size).ToArray());
                    String FirstStr = Encoding.Unicode.GetString(FirstData);
                    switch (FirstStr)
                    {
                        case "MESSAGE_HOST_CREATESOCKET_LYGREEN":
                            {
                                Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                IPEndPoint ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                                ServerSocket.Connect(ServerEndPoint);
                                ServerSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_HOST_LYGREEN"));
                                Host_ListServerSockets.Add(ServerSocket);

                                Socket LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), Host_ListenPort);
                                LocalSocket.Connect(LocalEndPoint);
                                Host_ListLocalSockets.Add(LocalSocket);

                                Host_Sockets ThreadSocket = new Host_Sockets();
                                ThreadSocket.ServerSocket = ServerSocket;
                                ThreadSocket.LocalSocket = LocalSocket;

                                Thread Thread_ReceiveFromServerAndSendToLocal = new Thread(new ParameterizedThreadStart(Host_ReceiveFromServerAndSendToLocal));
                                Thread_ReceiveFromServerAndSendToLocal.Name = "Host_ReceiveFromServerAndSendToLocal";
                                Thread_ReceiveFromServerAndSendToLocal.IsBackground = true;
                               
                                Thread Thread_ReceiveFromLocalAndSendToServer = new Thread(new ParameterizedThreadStart(Host_ReceiveFromLocalAndSendToServer));
                                Thread_ReceiveFromLocalAndSendToServer.Name = "Host_ReceiveFromLocalAndSendToServer";
                                Thread_ReceiveFromLocalAndSendToServer.IsBackground = true;

                                Thread_ReceiveFromServerAndSendToLocal.Start(ThreadSocket);
                                Thread_ReceiveFromLocalAndSendToServer.Start(ThreadSocket);
                                //Thread_Connect.Start((object)to);

                                Host_ServerCommunicationSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_HOST_CREATESOCKET_SUCCESSFULLY_LYGREEN"));
                                break;
                            }

                    }
                    FirstData = new byte[] { };
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        struct ThreadObjects
        {
            public Thread t1;
            public Thread t2;
            public Host_Sockets hs;
        }
        static void Host_ReceiveFromServerAndSendToLocal(object obj)
        {
            Host_Sockets ThreadSocket = (Host_Sockets)obj;
            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = ThreadSocket.ServerSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + ThreadSocket.ServerSocket.RemoteEndPoint + " " + size + " B数据");
                    ThreadSocket.LocalSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送自 " + ThreadSocket.LocalSocket.RemoteEndPoint + " " + size + " B数据");
                    if (size == 0)
                    {
                        Console.WriteLine("已断开！");
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        static void Host_ReceiveFromLocalAndSendToServer(object obj)
        {
            Host_Sockets ThreadSocket = (Host_Sockets)obj;
            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = ThreadSocket.LocalSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + ThreadSocket.LocalSocket.RemoteEndPoint + " " + size + " B数据");
                    ThreadSocket.ServerSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送自 " + ThreadSocket.ServerSocket.RemoteEndPoint + " " + size + " B数据");
                    
                    if (size == 0)
                    {
                        Console.WriteLine("已断开！");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        //////////////////////////////////////////////////////////////////
        //Join
        static void Join_LocalListen()
        {
            Join_LocalListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint Join_LocalEndPoint = new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString()), Join_MappedPort);
            Join_LocalListenSocket.Bind(Join_LocalEndPoint);
            Join_LocalListenSocket.Listen(10);

            while(true)
            {
                Join_LocalClientSocket = Join_LocalListenSocket.Accept();
                Console.WriteLine("连接成功！");

                Join_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint Join_ServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                Console.WriteLine("正在连接服务器......");
                Join_ServerSocket.Connect(Join_ServerEndPoint);
                Join_ServerSocket.Send(Encoding.Unicode.GetBytes("MESSAGE_JOIN_LYGREEN"));
                byte[] FirstData = {};
                String FirstStr = "\0";
                for (int i = 0;i < 4 ;i++ )
                {
                    byte[] DataBuffer = new byte[2048];
                    int size = Join_ServerSocket.Receive(DataBuffer);
                    FirstData = byte_Combine(FirstData, DataBuffer.Take(size).ToArray());
                    FirstStr = Encoding.Unicode.GetString(FirstData);
                    if (FirstStr == "MESSAGE_JOIN_SUCCESSFULLY_LYGREEN")
                    {
                        break;
                    }
                    Thread.Sleep(250);
                }
                if (FirstStr == "MESSAGE_JOIN_SUCCESSFULLY_LYGREEN")
                {
                    Console.WriteLine("连接成功！");

                    Thread Thread_ReceiveFromLocalAndSendToServer = new Thread(new ThreadStart(Join_ReceiveFromLocalAndSendToServer));
                    Thread_ReceiveFromLocalAndSendToServer.Name = "Join_ReceiveFromLocalAndSendToServer";
                    Thread_ReceiveFromLocalAndSendToServer.IsBackground = true;

                    Thread Thread_ReceiveFromServerAndSendToLocal = new Thread(new ThreadStart(Join_ReceiveFromServerAndSendToLocal));
                    Thread_ReceiveFromServerAndSendToLocal.Name = "Join_ReceiveFromServerAndSendToLocal";
                    Thread_ReceiveFromServerAndSendToLocal.IsBackground = true;

                    Thread_ReceiveFromLocalAndSendToServer.Start();
                    Thread_ReceiveFromServerAndSendToLocal.Start();
                }
            }
        }
        static void Join_ReceiveFromLocalAndSendToServer()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = Join_LocalClientSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + Join_LocalClientSocket.RemoteEndPoint + " " + size + " B数据");

                    Join_ServerSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送自 " + Join_ServerSocket.RemoteEndPoint + " " + size + " B数据");

                    if (size == 0)
                    {
                        Console.WriteLine("已断开！");
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
        static void Join_ReceiveFromServerAndSendToLocal()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[2048];
                    int size = Join_ServerSocket.Receive(data);
                    Console.WriteLine("Message:已接收自 " + Join_ServerSocket.RemoteEndPoint + " " + size + " B数据");

                    Join_LocalClientSocket.Send(data.Take(size).ToArray());
                    Console.WriteLine("Message:已发送自 " + Join_LocalClientSocket.RemoteEndPoint + " " + size + " B数据");

                    if (size == 0)
                    {
                        Console.WriteLine("已断开！");
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
    }
}
