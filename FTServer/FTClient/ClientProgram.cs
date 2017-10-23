/*
 * Steven Reeves 
 * 10/22/2017
 * CST 415
 * Assignment #2
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTClient
{
    class ClientProgram
    {
        static void Main(string[] args)
        {

           //string serverIP = "127.0.0.1";
           //string directoryName = "foo";
           //if (args.Length > 0)
           //directoryName = args[0];

            // serverPort hard coded in due to stubbing out PRS functionality
            ushort serverPort = 40001;
            ushort PRSPort = 0;
            string cmdPRSPort = null;
            string cmdPRSIP = null;
            string serverIP = null;
            string directoryName = null;


            for (int i = 0; i < args.Length; i++)
            {
                // Input was an option
                if (args[i][0] == '-')
                {
                    // Input was -prs
                    if (args[i] == "-prs")
                    {
                        i++;
                        if(i >= args.Length)
                        {
                            Console.WriteLine("Invalid input for -prs argument. Defaults used.");
                            cmdPRSIP = "127.0.0.1";
                            cmdPRSPort = "40001";
                        }
                        string[] parts = args[i].Split(':');
                        cmdPRSIP = parts[0];
                        cmdPRSPort = parts[1];
                    }
                    // Input was -s
                    if (args[i] == "-s")
                    {
                        i++;
                        serverIP = args[i];
                    }
                    // Input was -d
                    if (args[i] == "-d")
                    {
                        i++;
                        directoryName = args[i];
                    }
                    else
                    {
                        Console.WriteLine("Invalid argument used.");
                    }
                }

            }

            PRSPort = ushort.Parse(cmdPRSPort);

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // make reader writers
            NetworkStream socketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(socketNetworkStream);
            StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

            // create the local directory
            Directory.CreateDirectory(directoryName);

            socketwriter.WriteLine("get");
            socketwriter.WriteLine(directoryName);
            socketwriter.Flush();
            Console.WriteLine("Sent get: " + directoryName);

            // Download the files that the server says are in the directory
            bool done = false;
            while(!done)
            {
                string cmdString = socketReader.ReadLine();

                if(cmdString == "done")
                {
                    // Server is done!
                    done = true;
                }
                else
                {
                    string filename = cmdString;
                    string lengthstring = socketReader.ReadLine();
                    int filelength = System.Convert.ToInt32(lengthstring);

                    char[] buffer = new char[filelength];
                    socketReader.Read(buffer, 0, filelength);
                    string fileContents = new string(buffer);
                    File.WriteAllText(Path.Combine(directoryName, filename), fileContents);
                }
            }

            // disconnect from the server and close socket
            Console.WriteLine("Disconnecting from server");
            sock.Disconnect(false);
            socketReader.Close();
            socketwriter.Close();
            socketNetworkStream.Close();
            sock.Close();
            Console.WriteLine("Disconnected from server");
        }
    }
}
