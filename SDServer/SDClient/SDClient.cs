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
using PRSProtocolLibrary;

namespace SDClient
{
    class ClientProgram
    {
        // TODO: add these for arg parsing
        static bool OPEN_SESSION = false;
        static bool RESUME_SESSION = false;
        static bool CLOSE_SESSION = false;
        static bool GET = false;
        static bool POST = false;
        static string PRSIP = "127.0.0.1";
        static ushort PRSPort = 30000;
        static string serviceName = "SD Server";
        static string serverIP = "127.0.0.1";
        static string documentName = null;

        static void Main(string[] args)
        {

            // Current cmd args in 'Properties'
            // TODO: Check for cmd args


            // TODO: Added these to static
            //string serviceName = "FT Client";
            // Defualt serverPort
            //ushort PRSPort = 30000;
            //string PRSIP = "127.0.0.1";
            // Default directory name
            //string directoryName = "foo";
            //string serverIP = null;
            //ushort serverPort = 40001;
            //string serverName = "FT Server";

            // TODO: Add PRS protocol library, like in assignement 2

            // TODO: check all of these
            try
            {
                /* Parse these arguments
                -prs <PRS IP address>:<PRS port>
                -s <SD server IP address>
		        -o | -r <session id> | -c <session id>
                [-get <document> | -post <document>]
                 */

                for (int i = 0; i < args.Length; i++)
                {
                    // Input was an option
                    if (args[i][0] == '-')
                    {
                        // Input was -prs
                        if (args[i] == "-prs")
                        {
                            if (++i < args.Length)
                            {
                                string[] parts = args[i].Split(':');
                                if(parts.Length !=2)
                                    throw new Exception ("Unexpected value for -prs argument.");
                                PRSIP = parts[0];
                                PRSPort = System.Convert.ToUInt16(parts[1]);
                            }
                            else
                                throw new Exception("No value for -prs argument!");
                        }
                        // Input was -s
                       else if (args[i] == "-s")
                        {
                            if (++i < args.Length)
                            {
                                serverIP = args[i];
                            }
                            else
                            {
                                throw new Exception("No value for -s argument!");
                            }
                        }
                        // Input was -o
                        else if(args[i] == "-o")
                        {
                            OPEN_SESSION = true;
                        }
                        // Input was -r <session id>
                        else if(args[i] == "-r")
                        {
                            // TODO: resume existing session
                        }
                        // Input was -c <session id>
                        else if (args[i] == "-c")
                        {
                            CLOSE_SESSION = true;
                            // TODO: get session id
                        }
                        // Input was -get 
                        else if(args[i] == "-get")
                        {
                            GET = true;
                            if (++i < args.Length)
                            {
                                documentName = args[i];
                            }
                            else
                            {
                                throw new Exception("No value for -get argument!");
                            }
                        }
                        // Input was -POST 
                        else if(args[i] == "-post")
                        {
                            POST = true;
                            if (++i < args.Length)
                            {
                                documentName = args[i];
                            }
                            else
                            {
                                throw new Exception("No value for -post argument!");
                            }
                        }
                        else
                            throw new Exception("Unknown argument: " + args[i]);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error processiong command arguments: " + ex.Message);
                return;
            }

            // Lookup serverPort with PRS stub
            PRSCServiceClient prs = new PRSCServiceClient(serviceName, IPAddress.Parse(PRSIP), PRSPort);
            ushort serverPort = prs.LookupPort(serviceName);

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // make reader writers
            NetworkStream socketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(socketNetworkStream);
            StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

            // Open or resume session
            ulong sessionID = 0;
            string responseString;
            if (OPEN_SESSION)
            {
                // Open a new sesion with the server
                Console.WriteLine("Sending OPEN to server");
                socketwriter.WriteLine("open");
                socketwriter.Flush();

                // recieve accept from server

                responseString = socketReader.ReadLine();
                if (responseString == "accepted")
                {
                    Console.WriteLine("Server Accepted new request");
                    responseString = socketReader.ReadLine();
                    sessionID = System.Convert.ToUInt64(responseString);
                    Console.WriteLine("Received sessionID = " + sessionID.ToString());
                }
                else
                {
                    Console.WriteLine("Received invalid response" + responseString);
                }
            }

            if(RESUME_SESSION)
            {
                // TODO: Resume session
            }

            // Perform Get/Post
            if (GET)
            {

                // TODO: remove this line after testing
                //documentName = "foo";

                Console.WriteLine("Sending GET to server for document " + documentName);
                socketwriter.WriteLine("get");
                socketwriter.WriteLine(documentName);
                socketwriter.Flush();

                // Recieve response for GET
                responseString = socketReader.ReadLine();
                if (responseString == "success")
                {
                    Console.WriteLine("Success!");
                    responseString = socketReader.ReadLine();
                    if (responseString == documentName)
                    {
                        Console.WriteLine("Recieved expected docment name " + documentName);
                        responseString = socketReader.ReadLine();
                        int length = System.Convert.ToInt32(responseString);
                        Console.WriteLine("Recieved length " + length);

                        char[] buffer = new char[length];
                        int result = socketReader.Read(buffer, 0, length);
                        if (result == length)
                        {
                            string documentContents = new string(buffer);
                            Console.WriteLine("Received " + result.ToString() + " bytes of content, as follows...");
                            Console.WriteLine(documentContents);
                        }
                        else
                            Console.WriteLine("Error, received wrong number of bytes");

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
            }

            if (POST)
            {
                // TODO: POST
            }

            if (CLOSE_SESSION)
            {
                // TODO: CLOSE
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

    // Okay to turn in with stubs TODO: check if this is okay for Assignment 3
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

