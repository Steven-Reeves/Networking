/*
 * Steven Reeves 
 * 10/30/2017
 * CST 415
 * Assignment #3
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDClient
{
    class ClientProgram
    {
        // TODO: add these for arg parsing
        static bool OPEN_SESSION = false;

        static void Main(string[] args)
        {

            //Current cmd args in 'Properties'
            //-prs 127.0.0.1:40002 -s 127.0.0.1 -d foo

            string serviceName = "FT Client";
            string serverName = "FT Server";
            // Defualt serverPort
            ushort serverPort = 40001;
            ushort PRSPort = 0;
            string cmdPRSPort = null;
            string PRSIP = "127.0.0.1";
            string serverIP = null;
            // Default directory name
            string directoryName = "foo";

            // TODO: Add PRS protocol library, like in assignement 2

            // TODO: check all of these "else if"?
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
                            cmdPRSPort = "40001";
                        }
                        string[] parts = args[i].Split(':');
                        PRSIP = parts[0];
                        cmdPRSPort = parts[1];
                    }
                    // Input was -s
                    if (args[i] == "-s")
                    {
                        i++;
                        serverIP = args[i];
                    }
                    // Input was -o
                    if (args[i] == "-o")
                    {
                        i++;
                        directoryName = args[i];
                    }
                    // Input was -r <session id>
                    if (args[i] == "-d")
                    {
                        i++;
                        directoryName = args[i];
                    }
                    // Input was -c <session id>
                    if (args[i] == "-d")
                    {
                        i++;
                        directoryName = args[i];
                    }
                    // Input was -get 
                    if (args[i] == "-d")
                    {
                        i++;
                        directoryName = args[i];
                    }
                    // Input was -POST 
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

            // Lookup serverPort with PRS stub
            PRSCServiceClient prs = new PRSCServiceClient(serviceName, IPAddress.Parse(PRSIP), PRSPort);
            serverPort = prs.LookupPort(serverName);

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // make reader writers
            NetworkStream socketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(socketNetworkStream);
            StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

            // SD Client to do all the cool things

            // Open a new sesion with the server
            Console.WriteLine("Sending OPEN to server");
            socketwriter.WriteLine("open");
            socketwriter.Flush();

            // recieve accept from server
            ulong sessionID = 0;
            string responseString = socketReader.ReadLine();
            if(responseString == "accepted")
            {
                Console.WriteLine("Server Accepted new request");
                responseString = socketReader.ReadLine();
                sessionID = System.Convert.ToUInt64(responseString);
                Console.WriteLine("Recieved sessionID = " + sessionID.ToString());


            }
            else
            {
                Console.WriteLine("Recieved invalid response" + responseString);
            }

            // Send a get "foo"
            // TODO: actually get this name
            string documentName = "foo";
            Console.WriteLine("Sending GET to server for document " + documentName);
            socketwriter.WriteLine("get");
            socketwriter.WriteLine(documentName);
            socketwriter.Flush();

            if (responseString == "success")
            {
                Console.WriteLine("Success!");
                responseString = socketReader.ReadLine();
                if(responseString == documentName)
                {
                    Console.WriteLine("Recieved expected docment name " + documentName);
                    responseString = socketReader.ReadLine();
                    int length = System.Convert.ToInt32(responseString);
                    Console.WriteLine("Recieved length " + length);

                    // TODO recieve contents

                }
                else
                {
                    Console.WriteLine("Recieved unexpected docment name!");
                }
                sessionID = System.Convert.ToUInt64(responseString);
                Console.WriteLine("Recieved sessionID = " + sessionID.ToString());


            }
            else if (responseString == "error")
            {
                responseString = socketReader.ReadLine();
                Console.WriteLine("Recieved error from server: " + responseString);
            }
            else
            {
                Console.WriteLine("Recieved invalid response" + responseString);
            }

            // TODO: POST

            // TODO: CLOSE


            /*
            socketwriter.WriteLine("get");
            socketwriter.WriteLine(directoryName);
            socketwriter.Flush();
            Console.WriteLine("Sent get: " + directoryName);

            // Download the files that the server says are in the directory
            bool done = false;
            while (!done)
            {
                string cmdString = socketReader.ReadLine();

                if (cmdString == "done")
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
            */
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

        public ushort LookupPort(string serviceName)
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

