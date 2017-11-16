/*
 * Steven Reeves 
 * 11/12/2017
 * CST 415
 * Assignment #4
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


        static bool OPEN_SESSION = false;
        static bool RESUME_SESSION = false;
        static ulong RESUME_SESSION_ID = 0;
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
                            RESUME_SESSION = true;

                            if (++i < args.Length)
                            {
                                RESUME_SESSION_ID = System.Convert.ToUInt64(args[i]);
                            }
                            else
                            {
                                throw new Exception("No value for -r argument!");
                            }
                        }
                        // Input was -c <session id>
                        else if (args[i] == "-c")
                        {
                            CLOSE_SESSION = true;
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

            // Show variables
            Console.WriteLine("PRS Address: " + PRSIP.ToString());
            Console.WriteLine("PRS Port: " + PRSPort.ToString());
            Console.WriteLine("Server Address: " + serverIP.ToString());
            Console.WriteLine("Open Session: " + OPEN_SESSION.ToString());
            Console.WriteLine("Close Session: " + CLOSE_SESSION.ToString());
            Console.WriteLine("Resume Session: " + RESUME_SESSION.ToString());
            Console.WriteLine("Resume Session Address: " + RESUME_SESSION_ID.ToString());
            Console.WriteLine("Get: " + GET.ToString());
            Console.WriteLine("Post: " + POST.ToString());
            Console.WriteLine("Document: " + documentName);
            Console.WriteLine("///////////////" );


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
            try
            {
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

                else if (RESUME_SESSION)
                {
                    // Resume session with cmd line arg
                    Console.WriteLine("Sending RESUME to server");
                    socketwriter.WriteLine("resume");
                    socketwriter.WriteLine(RESUME_SESSION_ID.ToString());
                    socketwriter.Flush();

                    // receive accept from server
                    responseString = socketReader.ReadLine();
                    if (responseString == "accepted")
                    {
                        Console.WriteLine("Server Accepted the resume request");
                        responseString = socketReader.ReadLine();
                        sessionID = System.Convert.ToUInt64(responseString);
                        Console.WriteLine("Received sessionID = " + sessionID.ToString());
                        if (sessionID != RESUME_SESSION_ID)
                            throw new Exception("Resumed wrong session ID!");
                    }
                    // receive reject from server
                    else if (responseString == "rejected")
                    {
                        Console.WriteLine("Server rejected the resume request");
                        responseString = socketReader.ReadLine();
                        Console.WriteLine("Rejection reason: " + responseString);
                    }
                    else
                    {
                        Console.WriteLine("Received invalid response" + responseString);
                    }
                }


                // Perform Get/Post
                if (GET)
                {

                    Console.WriteLine("Sending GET to server for document " + documentName);
                    socketwriter.WriteLine("get");
                    socketwriter.WriteLine(documentName);
                    socketwriter.Flush();

                    // Receive response for GET
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
                            Console.WriteLine("Recieved length " + length.ToString());

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

                else if (POST)
                {
                    // read document contents
                    string documentContents = "";
                    string line;
                    while((line = Console.ReadLine()) != null)
                    {
                        documentContents += line + "\n";
                    }

                    // Send Post
                    Console.WriteLine("Sending POST to server for document " + documentName);
                    socketwriter.WriteLine("post");
                    socketwriter.WriteLine(documentName);
                    socketwriter.WriteLine(documentContents.Length.ToString());
                    socketwriter.Write(documentContents);
                    socketwriter.Flush();

                    // recieve success from server
                    responseString = socketReader.ReadLine();
                    if (responseString == "success")
                    {
                        Console.WriteLine("Success!");
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

                if (CLOSE_SESSION)
                {
                    // Close session with cmd line arg
                    Console.WriteLine("Sending Close to server");
                    socketwriter.WriteLine("close");
                    socketwriter.WriteLine(sessionID.ToString());
                    socketwriter.Flush();

                    // receive close from server
                    responseString = socketReader.ReadLine();
                    if (responseString == "closed")
                    {
                        responseString = socketReader.ReadLine();
                        ulong closedSessionId = System.Convert.ToUInt64(responseString);
                        Console.WriteLine("Server closed session " + closedSessionId.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Received invalid response" + responseString);
                    }

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
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

    // This is okay for Assignment 4
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

