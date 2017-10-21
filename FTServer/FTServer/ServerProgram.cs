using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            // TODO: Process CMD line stuff
            // -prs <PRS IP Aaddress>:<PRS port>


            // TODO: get the listening port from the PRS for the "FT Server" service
            string serviceName = "FT Server";
            string prsIP = "127.0.0.1";     // TODO: get this from cmd line
            ushort prsPort = 30000;         // TODO: get this from cmd line
            PRSCServiceClient prs = new PRSCServiceClient(serviceName, IPAddress.Parse(prsIP), prsPort);
            // PRSCServiceClient
            ushort listeningPort = prs.RequestPort();

            // create the TCP listening socket
            Socket listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            listeningSocket.Listen(42);     // 42 is the number of clients that can be waiting for us to accept their connection
            Console.WriteLine("Listening for clients on port " + listeningPort.ToString());

            bool done = false;
            while (!done)
            {
                // wait for a client to connect
                Console.WriteLine("Ready to accept new client");
                Socket clientSocket = listeningSocket.Accept();
                Console.WriteLine("Accepted connection from client");
                // TODO: worry about keep alives to the PRS for our FT Server port number

                // create a thread for this client, and then return to listening for more clients
                Console.WriteLine("Launch new thread for connected client");
                ClientThread clientThread = new ClientThread(clientSocket);
                clientThread.Start();
            }

            // close down the listening socket
            Console.WriteLine("Closing listening socket");
            listeningSocket.Close();

            // Close the listening port from the PRS
            prs.ClosePort();
        }

        class ClientThread
        {
            private Thread theThread;
            private Socket clientSocket;

            public ClientThread(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                theThread = new Thread(new ParameterizedThreadStart(ClientThreadFunc));
            }

            public void Start()
            {
                // Start the encapsulated thread
                // pass the instance of this class "ClientThread" to the thread so it can operate upon it
                theThread.Start(this);
            }

            private void Run()
            {
                NetworkStream ns = new NetworkStream(clientSocket);
                StreamReader reader = new StreamReader(ns);
                StreamWriter writer = new StreamWriter(ns);

                bool done = false;
                while (!done && clientSocket.Connected)
                {
                    string cmd = reader.ReadLine();
                    Console.WriteLine("Recieved cmd: " + cmd);
                    if(cmd == null)
                    {
                        done = true;
                        break;
                    }
                    switch (cmd)
                    {
                        case "get":
                            {
                                Console.WriteLine("Recieved get from the client");

                                // Read directory
                                string directoryName = reader.ReadLine();
                                Console.WriteLine("getting files from: " + directoryName);

                                //Open the directory
                                DirectoryInfo di = new DirectoryInfo(directoryName);

                                // Send each file to the client
                                foreach (FileInfo fi in di.EnumerateFiles())
                                {
                                    Console.WriteLine("Found file: " + fi.Name + " in directory");
                                    if (fi.Extension == ".txt")
                                    {
                                        Console.WriteLine("Found TXT file: " + fi.Name);

                                        // Send the file name and file length to the client
                                        writer.WriteLine(fi.Name);
                                        writer.WriteLine(fi.Length.ToString());
                                        writer.Flush();

                                        // Send the file contents to the client
                                        FileStream fs = fi.OpenRead();

                                        StreamReader fileReader = new StreamReader(ns);
                                        string fileContents = fileReader.ReadToEnd();
                                        writer.Write(fileContents);
                                        writer.Flush();
                                        fileReader.Close();
                                        fs.Close();

                                    }


                                }
                            }
                            break;
                        case "exit":
                            Console.WriteLine("Received EXIT from client");
                            done = true;
                            break;
                    }
                }
                // disconnect from client and close the socket
                Console.WriteLine("Disconnecting from client");
                clientSocket.Disconnect(false);
                ns.Close();
                writer.Close();
                reader.Close();

                clientSocket.Close();
                Console.WriteLine("Disconnected from client");
            }

            private static void ClientThreadFunc(object data)
            {
                Console.WriteLine("Client thread started");
                ClientThread ct = data as ClientThread;
                ct.Run();
            }
        }
    }

    // TODO: Okay to turn in with STUB!
    // Put this in the PRSProtocol Library
    class PRSCServiceClient
    {
        public PRSCServiceClient(string serviceName, IPAddress prsAdress, ushort port)
        {
            // TODO: PRSServiceClient.PRSServiceClient()
        }

        public ushort RequestPort()
        {
            // TODO: PRSServiceClient.RequestPort()
            // After getting a port
            // this class will keep port alive on a separate thread until closed
        
            return 40001;
        }

        public ushort LookupPort()
        {
            // Called by client
            // TODO: PRSServiceClient.LookupPort()
            return 40001;
        }

        public void ClosePort()
        {
            // TODO: PRSServiceClient.ClosePort()
        }

        public void KeepAlive()
        {
            // TODO: PRSServiceClient.KeepAlive()
        }


    }
}
