﻿/*
 * Steven Reeves 
 * 10/22/2017
 * CST 415
 * Assignment #2
 */

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
            string serviceName = "FT Server";
            string prsIP = "127.0.0.1";
            ushort prsPort = 30000;
            string cmdPRSPort = null;

            //Current cmd args in 'Properties'
            //-prs 127.0.0.1:30000
            for (int i = 0; i < args.Length; i++)
            {
                // Input was an option
                if (args[i][0] == '-')
                {
                    // Input was -prs
                    if (args[i] == "-prs")
                    {
                        i++;
                        if (i >= args.Length)
                        {
                            Console.WriteLine("Invalid input for -prs argument. Defaults used.");
                            prsIP = "127.0.0.2";
                            cmdPRSPort = "30001";
                        }
                        string[] parts = args[i].Split(':');
                        prsIP = parts[0];
                        cmdPRSPort = parts[1];
                    }
                    else
                    {
                        Console.WriteLine("Invalid argument used.");
                    }
                }

            }

            prsPort = ushort.Parse(cmdPRSPort);

            PRSCServiceClient prs = new PRSCServiceClient(serviceName, IPAddress.Parse(prsIP), prsPort);
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

                                        StreamReader fileReader = new StreamReader(fs);
                                        string fileContents = fileReader.ReadToEnd();
                                        writer.Write(fileContents);
                                        writer.Flush();
                                        fileReader.Close();
                                        fs.Close();
                                    }
                                }
                                // Tell the client we're done!
                                writer.WriteLine("done");
                                writer.Flush();
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

    // Okay to turn in with stubs
    class PRSCServiceClient
    {
        public PRSCServiceClient(string serviceName, IPAddress prsAdress, ushort port)
        {
            // PRSServiceClient.PRSServiceClient()
        }

        public ushort RequestPort()
        {
            // PRSServiceClient.RequestPort()
            // After getting a port
            // this class will keep port alive on a separate thread until closed
        
            return 40001;
        }

        public ushort LookupPort()
        {
            // Called by client
            // PRSServiceClient.LookupPort()
            return 40001;
        }

        public void ClosePort()
        {
            // PRSServiceClient.ClosePort()
        }

        public void KeepAlive()
        {
            // PRSServiceClient.KeepAlive()
        }


    }
}
