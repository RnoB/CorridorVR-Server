using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class SwarmVRServer
    {



        static void Main(string[] args)
        {
            SwarmServer swarmserver = new SwarmServer();
            swarmserver.startServer();

        }



    }

    public class SwarmServer
    {
        private List<handleClient> listClient = new List<handleClient>();
        private int count;
        public int port;
        public int kickOutTime;
        private TcpListener serverSocket;
        private TcpClient clientSocket;
        public UdpClient udpReceive;
        public UdpClient udpSend;
        public List<int> listId = new List<int>();
        public List<string> listName = new List<string>();
        public List<double> xPlayers = new List<double>();
        public List<double> yPlayers = new List<double>();
        public List<double> zPlayers = new List<double>();
        public List<double> rxPlayers = new List<double>();
        public List<double> ryPlayers = new List<double>();
        public List<double> rzPlayers = new List<double>();
        public List<bool> playerLap = new List<bool>();

        public List<long> updateTime = new List<long>();
        public List<int> listScore = new List<int>();
        public List<int> removePlayer = new List<int>();
        public double Speed;
        public int Level;
        public int nNeighbors;

        public int nLeft=0;
        public int nRight=0;
        public Stopwatch clock;
        IPEndPoint ipRemoteEndPointReceive;
        IPEndPoint broadcastEndPoint;

        FileStream fs;

        public double Aco;
        public double Len;
        public double Lex;
        public double Lco;
        public double Wco;
        public double Wbi;
        public double Hco;
        public void startServer()
        {

            Aco = 3.14 / 4;
            Len = 10;
            Lex = 10;
            Lco = 20;
            Wco = 4;
            Wbi = 3 * Wco;
            Hco = 3;
            clock = Stopwatch.StartNew();
            kickOutTime = 10000000;
            Console.WriteLine("Experiments Parameter");
            Console.WriteLine("\t--------");
            Console.WriteLine("Level :");
            Console.WriteLine("\tImitation Game");
            Console.WriteLine("\t\t11 - Two Gates");
            Console.WriteLine("\tCollective Movements");
            Console.WriteLine("\t\t21 - Colors");
            Console.WriteLine("\t\t22 - Center of Mass");
            Console.WriteLine("\t\t23 - Visibility");
            Level = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Closest Neighbors Numbers(Collective Movements only)");
            nNeighbors = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Speed (default = 5)");
            Speed = Convert.ToDouble(Console.ReadLine());


            string paramPath = pathDefine("param");
            if (!File.Exists(paramPath))
            {
                fs = File.Create(paramPath);
                fs.Close();
            }
            string appendText = Speed.ToString() + "\t" +
        Level.ToString() + "\t" +
        Environment.NewLine;
            File.AppendAllText(paramPath, appendText);
            fs.Dispose();
            string positionPath = pathDefine("position");
            if (!File.Exists(positionPath))
            {
                fs = File.Create(positionPath);
                fs.Close();
            }


            port = 4954;
            //IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[2];
            //IPAddress ipAddress = IPAddress.Parse("192.168.0.9");
            IPAddress ipAddress = IPAddress.Parse("169.254.217.95");
            IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, port);
            //IPAddress broadcast = IPAddress.Parse("192.168.0.255");
            IPAddress broadcast = IPAddress.Parse("255.255.0.0");
            broadcastEndPoint = new IPEndPoint(broadcast, port + 2);
            serverSocket = new TcpListener(ipLocalEndPoint);
            clientSocket = default(TcpClient);
            udpReceive = new UdpClient(port + 1);
            udpSend = new UdpClient();
            udpSend.EnableBroadcast = true;
            Console.WriteLine("Enable UDP broadcast is {0}",udpSend.EnableBroadcast);
            ipRemoteEndPointReceive = new IPEndPoint(IPAddress.Any, port + 1);
            udpReceive.Client.ReceiveTimeout = 200;
            udpSend.Client.SendTimeout = 200;

            serverSocket.Start();
            Console.WriteLine(ipLocalEndPoint + " >> " + "Server Started");

            count = 0;
            Thread newPlayerThread = new Thread(newPlayer);
            newPlayerThread.Start();
            Thread reception = new Thread(receive);
            reception.Start();

            while (true)
            {

                send();
                for (int k = 0; k < listId.Count; k++)
                {
                    appendText = clock.ElapsedMilliseconds.ToString() + "\t" +
                                listId[k].ToString() + "\t" +
                                    xPlayers[k].ToString() + "\t" +
                                    yPlayers[k].ToString() + "\t" +
                                    zPlayers[k].ToString() + "\t" +
                                    rxPlayers[k].ToString() + "\t" +
                                    ryPlayers[k].ToString() + "\t" +
                                    rzPlayers[k].ToString() + "\t" +
                                    Environment.NewLine;
                    File.AppendAllText(positionPath, appendText);

                    if (listId[k] > 1000 && listId[k] < 20000)
                    {
                        for (int j = 0; j < listId.Count; j++)
                        {
                            if (listId[j] < 1000)
                            {
                                double Rkj = Math.Sqrt(
                                    Math.Pow(xPlayers[k] - xPlayers[k], 2) +
                                    Math.Pow(yPlayers[k] - yPlayers[k], 2) +
                                    Math.Pow(zPlayers[k] - zPlayers[k], 2));
                                if (Rkj < 5)
                                {
                                    listScore[k] += 1000;
                                }
                            }
                        }
                    }
                    nLeft++;
                    nRight+=2;
                    if (listId[k] < 1000 && listId[k] < 20000)
                    {
                        if (playerLap[k] == false)
                        {
                            if (zPlayers[k] > -Lex)
                                playerLap[k] = !playerLap[k];
                        }
                        else if (zPlayers[k] < -(Lex + Wbi / Math.Tan(Aco))
     && zPlayers[k] > -(Lex + Lco + Wbi / Math.Tan(Aco)))
                        {
                            if (xPlayers[k] > Wco)
                            {
                                nRight++;
                            }
                            else if (xPlayers[k] < -Wco)
                            {
                                nLeft++;
                            }
                            playerLap[k] = !playerLap[k];

                        }
                    }

                }
            }


            serverSocket.Stop();
            Console.WriteLine(" >> " + "exit");
            Console.ReadLine();

        }
        private void newPlayer()
        {
            while (true)
            {
                count += 1;
                Console.WriteLine("Wait For Player");

                clientSocket = serverSocket.AcceptTcpClient();
                clientSocket.NoDelay = true;
                Console.WriteLine(" >> " + "Player " + Convert.ToString(count) + " started!");
                listClient.Add(new handleClient());
                listClient.Last().startClient(clientSocket, count, this);
                listClient.ForEach(delegate (handleClient client)
                {
                    if (client.clientId != listClient.Last().clientId)
                    {
                        if (client.clientId < 20000)
                        {
                            client.codeSendUdp.Add((byte)33);
                        }
                        else
                        {
                            client.codeSendUdp.Add((byte)33);
                            client.codeSendUdp.Add((byte)35);
                        }

                    }
                });
            }
        }
        private void receive()
        {
            Random rand = new Random();
            while (true)
            {
                byte code = 0;

                while (code != 255)
                {
                    try
                    {
                        byte[] ReadBuffer = ReceivebufferUdp();
                        code = ReadBuffer[0];
                        switch (code)
                        {
                            case (byte)20:
                                int clientIdD = BitConverter.ToInt32(ReadBuffer, 1);
                                int clientIdx = listId.IndexOf(clientIdD);
                                //Console.WriteLine("20 client Id : " + clientIdD);
                                xPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + 1);
                                yPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + sizeof(double) + 1);
                                zPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + 2 * sizeof(double) + 1);
                                rxPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + 3 * sizeof(double) + 1);
                                ryPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + 4 * sizeof(double) + 1);
                                rzPlayers[clientIdx] = BitConverter.ToDouble(ReadBuffer, sizeof(Int32) + 5 * sizeof(double) + 1);

                                updateTime[clientIdx] = clock.ElapsedMilliseconds;
                                if (listId[clientIdx] < 1000)
                                {
                                    listScore[clientIdx] = BitConverter.ToInt32(ReadBuffer, sizeof(Int32) + 3 * sizeof(double) + 1);
                                }
                                break;
                            case (byte)24:
                                int predIdx;
                                predIdx = rand.Next(1, listId.Count);
                                while (listClient[predIdx].clientId > 20000)
                                {
                                    predIdx = rand.Next(1, listId.Count);
                                }
                                listClient[predIdx].codeSendUdp.Add((byte)25);
                                break;
                            case (byte)30:
                                int removeId = BitConverter.ToInt32(ReadBuffer, 1);
                                Console.WriteLine("remove ID : " + removeId);
                                removePlayer.Add(removeId);


                                break;
                            case (byte)255:

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex);
                        code = 255;
                    }
                }
                int nPlayers = xPlayers.Count;
                if (nPlayers > 0)
                {

                    for (int l = 0; l < nPlayers; l++)
                    {

                        if (clock.ElapsedMilliseconds - updateTime[l] > kickOutTime)
                        {

                            removePlayer.Add(listId[l]);
                        }

                    }
                }
            }
        }
        private void send()
        {
            if (removePlayer.Any())
            {
                for (int l = 0; l < removePlayer.Count(); l++)
                {
                    int removeIdx = listId.IndexOf(removePlayer[l]);


                    byte[] message37 = new byte[sizeof(Int32)];
                    message37 = BitConverter.GetBytes(removePlayer[l]);
                    SendBufferBroadcastUdp((byte)37, message37);
                    //SendBufferUdp((byte)39, listClient[removeIdx].ipRemoteEndPointSend);

                    xPlayers.RemoveAt(removeIdx);
                    yPlayers.RemoveAt(removeIdx);
                    zPlayers.RemoveAt(removeIdx);
                    rxPlayers.RemoveAt(removeIdx);
                    ryPlayers.RemoveAt(removeIdx);
                    rzPlayers.RemoveAt(removeIdx);
                    playerLap.RemoveAt(removeIdx);
                    listId.RemoveAt(removeIdx);
                    listName.RemoveAt(removeIdx);
                    listClient.RemoveAt(removeIdx);
                    updateTime.RemoveAt(removeIdx);




                }
                removePlayer.Clear();

            }
            int nPlayers = listId.Count();
            if (nPlayers > 0)
            {
                Thread.Sleep(10);
                byte[][] message21 = new byte[nPlayers * 3][];
                byte[][] message23 = new byte[nPlayers][];

                // MR : left and right
                byte[][] message41 = new byte[2][];

                //list of positions
                for (int l = 0; l < nPlayers; l++)
                {

                    message21[l * 3 + 0] = BitConverter.GetBytes(xPlayers[l]);
                    message21[l * 3 + 1] = BitConverter.GetBytes(yPlayers[l]);
                    message21[l * 3 + 2] = BitConverter.GetBytes(zPlayers[l]);
                    message23[l] = BitConverter.GetBytes(listScore[l]);

                }

                byte[] messageUdp = Combine(message21);
                byte[] messageScore = Combine(message23);
                SendBufferBroadcastUdp((byte)21, messageUdp);

                // left and right
                message41[0] = BitConverter.GetBytes(nLeft);
                message41[1] = BitConverter.GetBytes(nRight);
                byte[] messageLR = Combine(message41);
                SendBufferBroadcastUdp((byte)41, messageLR);
                //Console.WriteLine("nLeft : " + nLeft + " nRight : " + nRight + "messqge : " );
                //Console.WriteLine(messageLR);
                //Console.WriteLine(message41);
                //Console.WriteLine(messageUdp);

                for (int k = 0; k < listId.Count; k++)
                {



                    int j = -1;
                    if (listId[k] > 20000)
                    {
                        listClient[k].codeSendUdp.Add((byte)23);
                    }
                    listClient[k].codeSendUdp.ForEach(delegate (byte code)
                    {

                        j++;

                        //Console.WriteLine("code : " + code);
                        switch (code)
                        {
                            case 31:

                                SendBufferUdp((byte)31, listClient[k].ipRemoteEndPointSend);
                                break;
                            case 33:
                                byte[][] message33 = new byte[4][];
                                message33[0] = BitConverter.GetBytes(listId.Last());
                                message33[1] = BitConverter.GetBytes(xPlayers.Last());
                                message33[2] = BitConverter.GetBytes(yPlayers.Last());
                                message33[3] = BitConverter.GetBytes(zPlayers.Last());
                                byte[] message3 = Combine(message33);

                                SendBufferUdp((byte)33, message3, listClient[k].ipRemoteEndPointSend);
                                break;
                            case 35:
                                SendBufferUdp((byte)35, Encoding.UTF8.GetBytes(listName.Last()), listClient[k].ipRemoteEndPointSend);
                                break;
                            case 23:
                                SendBufferUdp((byte)23, messageScore, listClient[k].ipRemoteEndPointSend);
                                break;

                            case 25:
                                SendBufferUdp((byte)25, listClient[k].ipRemoteEndPointSend);


                                break;
                            case 27:
                                SendBufferUdp((byte)27, listClient[k].ipRemoteEndPointSend);
                                break;



                        }




                    });

                    listClient[k].codeSendUdp.Clear();
                    try
                    {
                        //SendBufferUdp((byte)255, listClient[k].ipRemoteEndPointSend);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("this error");
                    }
                }
            }

        }

        string pathDefine(string namePath)
        {
            string path1 = "../Levels";
            string path2 = path1 + @"/" + Level.ToString();
            string path3 = path2 + @"/" + DateTime.Today.ToString("s").Substring(2, 8);
            string path4 = path3 + @"/" + namePath;
            string fullpath = path4 + @"/" + DateTime.Now.ToString("s").Substring(11, 2) + "-" +
                DateTime.Now.ToString("s").Substring(14, 2) + "-" +
                    DateTime.Now.ToString("s").Substring(17, 2) + ".txt";

            if (!Directory.Exists(path1))
            {
                Directory.CreateDirectory(path1);
            }
            if (!Directory.Exists(path2))
            {
                Directory.CreateDirectory(path2);
            }
            if (!Directory.Exists(path3))
            {
                Directory.CreateDirectory(path3);
            }
            if (!Directory.Exists(path4))
            {
                Directory.CreateDirectory(path4);
            }
            return fullpath;
        }
        private byte[] ReceivebufferUdp()
        {

            try
            {
                byte[] array;
                array = udpReceive.Receive(ref ipRemoteEndPointReceive);
                return array;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }
        private void SendBufferUdp(byte[] buf, IPEndPoint ipRemoteEndPointSend)
        {
            try
            {
                byte[] array = new byte[1 + buf.Length];
                Buffer.BlockCopy(buf, 0, array, 0, buf.Length);
                array[buf.Length] = 255;
                udpSend.Send(array, array.Length, ipRemoteEndPointSend);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SendBufferBroadcastUdp(byte code, byte[] buf)
        {
            try
            {
                byte[] array = new byte[1 + buf.Length];
                array[0] = code;
                Buffer.BlockCopy(buf, 0, array, 1, buf.Length);
                udpSend.Send(array, array.Length, broadcastEndPoint);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SendBufferUdp(byte code, IPEndPoint ipRemoteEndPointSend)
        {
            try
            {
                byte[] codeA = new byte[1];
                codeA[0] = code;
                udpSend.Send(codeA, 1, ipRemoteEndPointSend);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SendBufferUdp(byte code, byte[] buf, IPEndPoint ipRemoteEndPointSend)
        {
            try
            {
                byte[] array = new byte[1 + buf.Length];
                Buffer.BlockCopy(buf, 0, array, 1, buf.Length);
                array[0] = code;
                udpSend.Send(array, array.Length, ipRemoteEndPointSend);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private byte[] Combine(params byte[][] arrays)
        {
            byte[] array = new byte[arrays.Sum((byte[] x) => x.Length)];
            int num = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                byte[] array2 = arrays[i];
                Buffer.BlockCopy(array2, 0, array, num, array2.Length);
                num += array2.Length;
            }
            return array;
        }
    }
    //Class to handle each client request separatly
    public class handleClient
    {
        private SwarmServer swarmServer;
        private TcpClient clientSocket;
        public int clientId;
        public string clientName;
        public double X;
        public double Y;
        public double Z;
        private Random rand = new Random();
        public List<byte> codeSend = new List<byte>();
        public List<byte> codeSendUdp = new List<byte>();

        private IPEndPoint ipRemoteEndPointReceive;
        public IPEndPoint ipRemoteEndPointSend;

        public void startClient(TcpClient inClientSocket, int clientId, SwarmServer swarmServer)
        {

            this.clientSocket = inClientSocket;
            this.clientId = clientId;
            this.swarmServer = swarmServer;
            IPAddress ipAddress = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address;
            ipRemoteEndPointReceive = new IPEndPoint(ipAddress, 0);
            ipRemoteEndPointSend = new IPEndPoint(ipAddress, swarmServer.port + 2);


            X = rand.NextDouble();
            Y = 1.5;
            Z = rand.NextDouble() - 1;
            swarmServer.xPlayers.Add(X);
            swarmServer.yPlayers.Add(Y);
            swarmServer.zPlayers.Add(Z);
            swarmServer.rxPlayers.Add(0);
            swarmServer.ryPlayers.Add(0);
            swarmServer.rzPlayers.Add(0);
            swarmServer.playerLap.Add(true);
            swarmServer.updateTime.Add(0);
            swarmServer.listScore.Add(0);
            register();


        }

        private void register()
        {
            try
            {

                byte[] bytesFrom = Receivebuffer(1 + 2 * sizeof(int));
                int code = bytesFrom[0];

                if (code == 10)
                {
                    int playerType = BitConverter.ToInt32(bytesFrom, 1);
                    if (BitConverter.ToInt32(bytesFrom, 1) == 1)
                    {
                        clientId += 20000;

                    }
                    else if (BitConverter.ToInt32(bytesFrom, 1) == 2)
                    {
                        clientId += 1000;
                    }
                    swarmServer.listId.Add(clientId);
                    byte[] nameStream = Receivebuffer(BitConverter.ToInt32(bytesFrom, 1 + sizeof(int)));
                    clientName = Encoding.UTF8.GetString(nameStream);
                    swarmServer.listName.Add(clientName);
                }
                Receivebuffer(1);
                int nPlayers = swarmServer.listId.Count();

                byte[] code2 = new byte[1];
                code2[0] = 17;

                byte[][] message = new byte[4][];
                message[0] = code2;
                message[1] = BitConverter.GetBytes(swarmServer.Speed);
                message[2] = BitConverter.GetBytes(swarmServer.Level);
                message[3] = BitConverter.GetBytes(swarmServer.nNeighbors);

                byte[] sendBuffer = Combine(message);
                SendBuffer(sendBuffer);

                code2[0] = 19;

                message = new byte[8][];
                message[0] = code2;

                message[1] = BitConverter.GetBytes(swarmServer.Aco);
                message[2] = BitConverter.GetBytes(swarmServer.Len);
                message[3] = BitConverter.GetBytes(swarmServer.Lex);
                message[4] = BitConverter.GetBytes(swarmServer.Lco);
                message[5] = BitConverter.GetBytes(swarmServer.Wco);
                message[6] = BitConverter.GetBytes(swarmServer.Wbi);
                message[7] = BitConverter.GetBytes(swarmServer.Hco);



                sendBuffer = Combine(message);
                SendBuffer(sendBuffer);

                code2 = new byte[1];
                code2[0] = 11;

                message = new byte[nPlayers + 3][];
                message[0] = code2;
                message[1] = BitConverter.GetBytes(nPlayers);
                message[2] = BitConverter.GetBytes(clientId);
                for (int k = 0; k < nPlayers; k++)
                {
                    message[3 + k] = BitConverter.GetBytes(swarmServer.listId[k]);
                }
                sendBuffer = Combine(message);
                SendBuffer(sendBuffer);
                if (clientId > 20000)
                {
                    code2[0] = 13;

                    message = new byte[2 * nPlayers + 1][];
                    message[0] = code2;
                    for (int k = 0; k < nPlayers; k++)
                    {
                        message[1 + 2 * k] = BitConverter.GetBytes(Encoding.UTF8.GetByteCount(swarmServer.listName[k]));
                        message[2 + 2 * k] = Encoding.UTF8.GetBytes(swarmServer.listName[k]);
                    }


                    sendBuffer = Combine(message);
                    SendBuffer(sendBuffer);
                }



                code2[0] = 21;

                message = new byte[1 + nPlayers * 3][];
                message[0] = code2;
                for (int k = 0; k < nPlayers; k++)
                {
                    message[1 + k * 3] = BitConverter.GetBytes(swarmServer.xPlayers[k]);
                    message[2 + k * 3] = BitConverter.GetBytes(swarmServer.yPlayers[k]);
                    message[3 + k * 3] = BitConverter.GetBytes(swarmServer.zPlayers[k]);
                }

                sendBuffer = Combine(message);
                SendBuffer(sendBuffer);


            }
            catch (Exception ex)
            {
                Console.WriteLine(" >> " + ex.ToString());
            }

        }

        private void SendBuffer(byte[] buf)
        {
            try
            {


                byte[] array = new byte[1 + buf.Length];
                Buffer.BlockCopy(buf, 0, array, 0, buf.Length);
                array[buf.Length] = 255;
                NetworkStream stream = clientSocket.GetStream();
                stream.Write(array, 0, array.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private byte[] Receivebuffer(int size)
        {
            try
            {
                NetworkStream stream = clientSocket.GetStream();
                byte[] array = new byte[size];
                stream.Read(array, 0, size);
                return array;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        private byte[] Combine(params byte[][] arrays)
        {
            byte[] array = new byte[arrays.Sum((byte[] x) => x.Length)];
            int num = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                byte[] array2 = arrays[i];
                Buffer.BlockCopy(array2, 0, array, num, array2.Length);
                num += array2.Length;
            }
            return array;
        }
    }
}