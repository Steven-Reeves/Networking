using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDServer
{
    class ServerProgram
    {
        static string serviceName = "SD Server";
        static string prsIP = "127.0.0.1";
        static ushort prsPort = 30000;
        static int CLIENT_BACKLOG = 42;

        static void Main(string[] args)
        {

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    // Input was an option
                    if (args[i][0] == '-')
                    {
                        // Input was -prs
                        // -prs <PRS IP address>:<PRS port>
                        if (args[i] == "-prs")
                        {
                            if (++i < args.Length)
                            {
                                string[] parts = args[i].Split(':');
                                if (parts.Length != 2)
                                    throw new Exception("Unexpected value for -prs argument.");
                                prsIP = parts[0];
                                prsPort = System.Convert.ToUInt16(parts[1]);
                            }
                            else
                                throw new Exception("No value for -prs argument!");
                        }
                        else
                            throw new Exception("Unknown argument: " + args[i]);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processiong command arguments: " + ex.Message);
                return;
            }

            // Show variables
            Console.WriteLine("PRS Address: " + prsIP.ToString());
            Console.WriteLine("PRS Port: " + prsPort.ToString());
            Console.WriteLine("///////////////");

            // create the session table
            SessionTable sessionTable = new SessionTable();
            

            PRSServiceClient.prsAddress = IPAddress.Parse(prsIP);
            PRSServiceClient.prsPort = prsPort;
            PRSServiceClient prs = new PRSServiceClient(serviceName);
            ushort listeningPort = prs.RequestPort(); 

            // create the TCP listening socket
            Socket listeningSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            listeningSocket.Listen(CLIENT_BACKLOG); 
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
                ClientThread clientThread = new ClientThread(clientSocket, sessionTable);
                clientThread.Start();
            }

            // close down the listening socket
            Console.WriteLine("Closing listening socket");
            listeningSocket.Close();

            // close the listening port that I received from the PRS
            prs.ClosePort();
        }

        class ClientThread
        {
            private Thread theThread;

            private Socket clientSocket;
            private NetworkStream socketNetworkStream;
            private StreamReader socketReader;
            private StreamWriter socketWriter;

            private SessionTable sessionTable;
            private SDSession session;

            private State currentState;

            abstract class State
            {
                protected ClientThread client;

                public State(ClientThread client)
                {
                    this.client = client;
                }

                public abstract SDSession HandleOpenCmd();
                public abstract SDSession HandleResumeCmd();

                public abstract void HandleGetCmd(SDSession session);
                public abstract void HandlePostCmd(SDSession session);

                public void HandleCloseCmd(SDSession session)
                {
                    // Get session Id
                    string closeString = client.socketReader.ReadLine();
                    ulong closeSessionID = System.Convert.ToUInt64(closeString);

                    // Close the session
                    client.sessionTable.CloseSession(closeSessionID);

                    if ((client.session != null) && (client.session.ID == closeSessionID))
                    {
                        client.session = null;
                    }

                    //Send respoonse to client
                    client.socketWriter.WriteLine("closed");
                    client.socketWriter.WriteLine(closeSessionID.ToString());
                    client.socketWriter.Flush();
                }

                protected void SendError(string errorMsg)
                {
                    client.socketWriter.WriteLine("error");
                    client.socketWriter.WriteLine(errorMsg);
                    client.socketWriter.Flush();
                }
            }

            class ReadyForSessionCmd : State
            {
                public ReadyForSessionCmd(ClientThread client) : base (client)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    // create a new session for the client
                    SDSession session = client.sessionTable.NewSession();
                    Console.WriteLine("Opened new session with id " + session.ID.ToString());

                    // send Accepted(sessionId)
                    client.socketWriter.WriteLine("accepted");
                    client.socketWriter.WriteLine(session.ID.ToString());
                    client.socketWriter.Flush();
                    Console.WriteLine("Sent accepted id = " + session.ID.ToString() + " to client");

                    return session;
                }

                public override SDSession HandleResumeCmd()
                {
                    // Get requested session id to resume
                    string requestString = client.socketReader.ReadLine();
                    ulong requestedSessionID = System.Convert.ToUInt64(requestString);

                    // find the requested session for the client
                    SDSession session = client.sessionTable.Lookup(requestedSessionID);
                    if (session != null)
                    {
                        Console.WriteLine("Resuming new session with id " + session.ID.ToString());

                        // send Accepted(sessionId)
                        client.socketWriter.WriteLine("accepted");
                        client.socketWriter.WriteLine(session.ID.ToString());
                        client.socketWriter.Flush();
                        Console.WriteLine("Sent accepted id = " + session.ID.ToString() + " to client");
                    }
                    else
                    {
                        Console.WriteLine("Cannot find this session: " + requestedSessionID.ToString());
                        // Send rejected (reason)
                        client.socketWriter.WriteLine("rejected");
                        client.socketWriter.WriteLine("Session ID: " + requestedSessionID.ToString() + " Not found!");
                        client.socketWriter.Flush();
                        Console.WriteLine("Sent rejected id = " + requestedSessionID.ToString() + " to client");

                    }
                    return session;
                }

                public override void HandleGetCmd(SDSession session)
                {
                    client.socketReader.ReadLine();

                    SendError("No session open");
                }

                public override void HandlePostCmd(SDSession session)
                {
                    client.socketReader.ReadLine();
                    client.socketReader.ReadLine();
                    client.socketReader.ReadLine();

                    SendError("No session open");
                }

            }

            class ReadyForDocumentCmd : State
            {
                public ReadyForDocumentCmd(ClientThread client) : base (client)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    SendError("Session already open");
                    return null;
                }

                public override SDSession HandleResumeCmd()
                {
                    client.socketReader.ReadLine();

                    SendError("Session already open");
                    return null;
                }

                public override void HandleGetCmd(SDSession session)
                {
                    // read the document name from the client
                    string documentName = client.socketReader.ReadLine();
                    Console.WriteLine("Getting document " + documentName);

                    // lookup the document in the session
                    try
                    {
                        string documentContents = session.GetValue(documentName);
                        Console.WriteLine("Found value " + documentName);

                        // send the document name and length to the client
                        client.socketWriter.WriteLine("success");
                        client.socketWriter.WriteLine(documentName);
                        client.socketWriter.WriteLine(documentContents.Length.ToString());
                        client.socketWriter.Flush();

                        // send the document contents to the client
                        client.socketWriter.Write(documentContents);
                        client.socketWriter.Flush();

                        Console.WriteLine("Sent contents of " + documentName);
                    }
                    catch (Exception ex)
                    {
                        SendError(ex.Message);
                    }
                }

                public override void HandlePostCmd(SDSession session)
                {
                    // read the document name from the client
                    string documentName = client.socketReader.ReadLine();
                    Console.WriteLine("Posting document " + documentName);

                    // read document length
                    int documentLength = System.Convert.ToInt32(client.socketReader.ReadLine());
                    Console.WriteLine("Document length " + documentLength.ToString());
                    // read document contents
                    char[] buffer = new char[documentLength];
                    int result = client.socketReader.Read(buffer, 0, documentLength);
                    Console.WriteLine("Received " + result.ToString() + " bytes of content");
                    if (result == documentLength)
                    {
                        // store document
                        string documentContents = new string(buffer);
                        client.session.PutValue(documentName, documentContents);
                        Console.WriteLine("Put documents into the session");

                        // send success to client
                        client.socketWriter.WriteLine("success");
                        client.socketWriter.Flush();
                        Console.WriteLine("Sent Success to client!");
                    }
                    else
                    {
                        // send error to client
                        SendError("Unexpected document length");
                    }

                }

            }

            public ClientThread(Socket clientSocket, SessionTable sessionTable)
            {
                this.clientSocket = clientSocket;
                socketNetworkStream = new NetworkStream(clientSocket);
                socketReader = new StreamReader(socketNetworkStream);
                socketWriter = new StreamWriter(socketNetworkStream);

                this.sessionTable = sessionTable;
                session = null;
                currentState = null;
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

                currentState = new ReadyForSessionCmd(this);

                bool done = false;
                while (!done && clientSocket.Connected)
                {
                    // read the next command from the client
                    string cmd = socketReader.ReadLine();
                    if (cmd == null)
                    {
                        // client disconnected
                        done = true;
                        break;
                    }
                    Console.WriteLine("Received cmd " + cmd);

                    switch (cmd)
                    {
                        case "open":
                            session = currentState.HandleOpenCmd();
                            currentState = new ReadyForDocumentCmd(this);
                            break;

                        case "resume":
                            {
                                session = currentState.HandleResumeCmd();
                                if (session != null)
                                {
                                    // successfully resumed session
                                    // change state
                                    currentState = new ReadyForDocumentCmd(this);
                                }
                            }
                            break;

                        case "get":
                            {
                                Console.WriteLine("Received GET cmd from client");
                                currentState.HandleGetCmd(session);
                            }
                            break;

                        case "post":
                            {
                                Console.WriteLine("Received POST cmd from client");
                                currentState.HandlePostCmd(session);
                                break;
                            }
                        case "close":
                            {
                                Console.WriteLine("Received CLOSE cmd from client");
                                currentState.HandleCloseCmd(session);
                                if(session == null)
                                {
                                    currentState = new ReadyForSessionCmd(this);
                                }
                            }
                            break;

                        case "exit":
                            Console.WriteLine("Received EXIT cmd from client");
                            done = true;
                            break;

                        default:
                            Console.WriteLine("Received unknown cmd from client: " + cmd);
                            break;
                    }
                }

                // disconnect from client and close the socket, it's stream and reader/writer
                Console.WriteLine("Disconnecting from client");
                clientSocket.Disconnect(false);
                socketNetworkStream.Close();
                socketReader.Close();
                socketWriter.Close();
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

    class SDSession
    {
        private ulong sessionId;
        private Dictionary<string, string> sessionValues;

        public SDSession(ulong sessionId)
        {
            this.sessionId = sessionId;
            sessionValues = new Dictionary<string, string>();
        }

        public ulong ID { get { return sessionId; } }

        public void PutValue(string name, string value)
        {
            sessionValues[name] = value;
        }

        public string GetValue(string name)
        {
            if (!sessionValues.ContainsKey(name))
                throw new Exception("Unknown value " + name);

            return sessionValues[name];
        }
    }

    class SessionTable
    {
        private Dictionary<ulong, SDSession> sessionTable;
        private ulong nextSessionId;

        public SessionTable()
        {
            sessionTable = new Dictionary<ulong, SDSession>();
            nextSessionId = 1;
        }

        public SDSession NewSession()
        {
            // allocate a new session, with a unique ID and save it for later in the session table
            ulong sessionId = nextSessionId++;
            SDSession session = new SDSession(sessionId);
            sessionTable[sessionId] = session;
            return session;
        }

        public SDSession Lookup(ulong sessionId)
        {
            if (sessionTable.ContainsKey(sessionId))
            {
                SDSession session = sessionTable[sessionId];
                return session;
            }

            return null;
        }

        public void CloseSession(ulong sessionId)
        {
            if (sessionTable.ContainsKey(sessionId))
            {
                sessionTable.Remove(sessionId);
            }
        }
    }


    // Stubbed out is okay for Assignment 3
    class PRSServiceClient
    {
        public static IPAddress prsAddress;
        public static ushort prsPort;

        public PRSServiceClient(string serviceName)
        {
        }

        public ushort LookupPort()
        {
            // called by the FTClient
            return 40002;   // NOTE: different address for SD Server
        }

        public ushort RequestPort()
        {
            // called by the FTServer
            // after successfully requesting a port
            // this class will keep the port alive on a separate thread
            // until the port closed
            return 40001;
        }

        public void ClosePort()
        {
            // called by the FTServer
        }

        private void KeepAlive()
        {
            // called by the FTServer
        }
    }
}
