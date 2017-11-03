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
        static void Main(string[] args)
        {
            // get the listening port from the PRS for the "SD Server" service
            string serviceName = "SD Server";
            string prsIP = "127.0.0.1";
            ushort prsPort = 30000;

            // TODO: Test argument parsing
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

            // create the session table
            SessionTable sessionTable = new SessionTable();
            

            PRSServiceClient.prsAddress = IPAddress.Parse(prsIP);
            PRSServiceClient.prsPort = prsPort;
            PRSServiceClient prs = new PRSServiceClient(serviceName);
            ushort listeningPort = 40001; // TODO: reconnect this prs.RequestPort();

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
            private SessionTable sessionTable;
            private SDSession session;
            private State currentState;

            abstract class State
            {
                protected SessionTable sessionTable;
                protected NetworkStream socketNetworkStream;
                protected StreamReader socketReader;
                protected StreamWriter socketWriter;

                public State(SessionTable sessionTable, NetworkStream socketNetworkStream, StreamReader socketReader, StreamWriter socketWriter)
                {
                    this.sessionTable = sessionTable;
                    this.socketNetworkStream = socketNetworkStream;
                    this.socketReader = socketReader;
                    this.socketWriter = socketWriter;
                }

                public abstract SDSession HandleOpenCmd();
                public abstract SDSession HandleResumeCmd(ulong sessionId);
                public abstract void HandleCloseCmd(ulong sessionId);
                public abstract void HandleGetCmd(SDSession session);
                public abstract void HandlePostCmd();

                protected void SendError(string errorMsg)
                {
                    socketWriter.WriteLine("error");
                    socketWriter.WriteLine(errorMsg);
                    socketWriter.Flush();
                }
            }

            class ReadyForSessionCmd : State
            {
                public ReadyForSessionCmd(SessionTable sessionTable, NetworkStream socketNetworkStream, StreamReader socketReader, StreamWriter socketWriter) :
                    base (sessionTable, socketNetworkStream, socketReader, socketWriter)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    // create a new session for the client
                    SDSession session = sessionTable.NewSession();
                    Console.WriteLine("Opened new session with id " + session.ID.ToString());

                    // send Accepted(sessionId)
                    socketWriter.WriteLine("accepted");
                    socketWriter.WriteLine(session.ID.ToString());
                    socketWriter.Flush();
                    Console.WriteLine("Sent accepted id = " + session.ID.ToString() + " to client");

                    return session;
                }

                public override SDSession HandleResumeCmd(ulong sessionId)
                {
                    return null;
                }

                public override void HandleCloseCmd(ulong sessionId)
                {

                }

                public override void HandleGetCmd(SDSession session)
                {
                    SendError("No session open");
                }

                public override void HandlePostCmd()
                {
                    SendError("No session open");
                }

            }

            class ReadyForDocumentCmd : State
            {
                public ReadyForDocumentCmd(SessionTable sessionTable, NetworkStream socketNetworkStream, StreamReader socketReader, StreamWriter socketWriter) :
                    base (sessionTable, socketNetworkStream, socketReader, socketWriter)
                {
                }

                override public SDSession HandleOpenCmd()
                {
                    SendError("Session already open");
                    return null;
                }

                public override SDSession HandleResumeCmd(ulong sessionId)
                {
                    SendError("Session already open");
                    return null;
                }

                public override void HandleCloseCmd(ulong sessionId)
                {

                }

                public override void HandleGetCmd(SDSession session)
                {
                    // read the document name from the client
                    string documentName = socketReader.ReadLine();
                    Console.WriteLine("Getting document " + documentName);

                    // lookup the document in the session
                    try
                    {
                        string documentContents = session.GetValue(documentName);
                        Console.WriteLine("Found value " + documentName);

                        // send the document name and length to the client
                        socketWriter.WriteLine("success");
                        socketWriter.WriteLine(documentName);
                        socketWriter.WriteLine(documentContents.Length.ToString());
                        socketWriter.Flush();

                        // send the document contents to the client
                        socketWriter.Write(documentContents);
                        socketWriter.Flush();

                        Console.WriteLine("Sent contents of " + documentName);
                    }
                    catch (Exception ex)
                    {
                        SendError(ex.Message);
                    }
                }

                public override void HandlePostCmd()
                {

                }

            }

            public ClientThread(Socket clientSocket, SessionTable sessionTable)
            {
                this.clientSocket = clientSocket;
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
                NetworkStream socketNetworkStream = new NetworkStream(clientSocket);
                StreamReader socketReader = new StreamReader(socketNetworkStream);
                StreamWriter socketWriter = new StreamWriter(socketNetworkStream);

                currentState = new ReadyForSessionCmd(sessionTable, socketNetworkStream, socketReader, socketWriter);

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
                            currentState = new ReadyForDocumentCmd(sessionTable, socketNetworkStream, socketReader, socketWriter);
                            break;

                        case "resume":
                            {
                                // parse out the sessionId
                                ulong sessionId = 0;
                                session = currentState.HandleResumeCmd(sessionId);
                                if (session != null)
                                {
                                    // successfully resumed session
                                    // change state
                                    currentState = new ReadyForDocumentCmd(sessionTable, socketNetworkStream, socketReader, socketWriter);
                                }
                                else
                                {
                                    //???
                                }
                            }
                            break;

                        case "get":
                            {
                                Console.WriteLine("Received GET cmd from client");
                                currentState.HandleGetCmd(session);

                            }
                            break;

                        case "exit":
                            Console.WriteLine("Received EXIT cmd from client");
                            done = true;
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
    }

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
