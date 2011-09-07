using System;	
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;

/* gWeb is a hobby Web Server made for personal and not commercial use */

namespace gWebD
{
	class gWebD 
	{
		private const Double VERSION = 0.1; // Version number for gWebD.
		private TcpListener webListener;
		private int port = 5050; // This can be any port that isnt already in use by another
								  // service or program.

		// Starts listening on the appropriate port and starts a thread running for each
		// client that connects.
		public gWebD()
		{
			try {
				if (portAvailable(port))
				{
					// Listens
					webListener = new TcpListener(port); // This is an old method, will be changed later.
					webListener.Start();
					Console.WriteLine("gWebD {0} is Running on port {1}... Press ^C to Stop...", VERSION, port);
					//start the thread which calls the method 'StartListen'
					Thread mainThread = new Thread(new ThreadStart(StartListen));
					mainThread.Start() ;

				}
				else
				{
					Console.WriteLine("Unable to start, is the port being used?");
				}
			} catch {
				Console.WriteLine("You cannot run a duplicate server!");	
			}
		}

		public string GetTheDefaultFileName(string localDirectory)
		{
			StreamReader reader;
			String line = "";

			try
			{
				//Open the default.dat to find out the list
				// of default file
				reader = new StreamReader("Data/Default.dat");

				while ((line = reader.ReadLine()) != null)
				{
					//Look for the default file in the web server root folder
					if (File.Exists( localDirectory + line) == true)
						break;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occured when getting default filename, check directory.");
			}
			if (File.Exists( localDirectory + line) == true) {
				return line;
			}
			else {
				return "";
			}
		}

		public string GetMimeType(string requestedFile)
		{
			StreamReader reader;
			String line = "";
			String mimeType = "";
			String fileExt = "";
			String mimeExt = "";
			
			// Convert to lowercase
			requestedFile = requestedFile.ToLower();
			
			int startPos = requestedFile.IndexOf(".");

			fileExt = requestedFile.Substring(startPos);
			
			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				reader = new StreamReader("Data/Mime.dat");

				while ((line = reader.ReadLine()) != null)
				{

					line.Trim();

					if (line.Length > 0)
					{
						//find the separator
						startPos = line.IndexOf(";");
						
						// Convert to lower case
						line = line.ToLower();
						
						mimeExt = line.Substring(0,startPos);
						mimeType = line.Substring(startPos + 1);
						
						if (mimeExt == fileExt) {
							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occured whilst checking MIME type!");
				Console.WriteLine(e.ToString());
			}

			if (mimeExt == fileExt) {
				return mimeType; 
			}
			else {
				return "";
			}
		}

		public string GetLocalPath(string serverRoot, string dirName)
		{
			StreamReader reader;
			String line = "";
			String virtDir = ""; 
			String realDir = "";
			int startPos = 0;
			
			//Remove extra spaces
			dirName.Trim();
			
			// Convert to lowercase
			serverRoot = serverRoot.ToLower();
			
			// Convert to lowercase
			dirName = dirName.ToLower();

			//Remove the slash
			//dirName = dirName.Substring(1, dirName.Length - 2);


			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				reader = new StreamReader("Data/VDirs.dat");

				while ((line = reader.ReadLine()) != null)
				{
					//Remove extra Spaces
					line.Trim();

					if (line.Length > 0)
					{
						//find the separator
						startPos = line.IndexOf(";");
						
						// Convert to lowercase
						line = line.ToLower();
						
						virtDir = line.Substring(0,startPos);
						realDir = line.Substring(startPos + 1);
						
						if (virtDir == dirName)
						{
							break;
						}
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occured when getting the Local Path, check directories!");
			}


			Console.WriteLine("Virtual Dir : " + virtDir);
			Console.WriteLine("Directory   : " + dirName);
			Console.WriteLine("Physical Dir: " + realDir);
			if (virtDir == dirName) {
				return realDir;
			}
			else {
				return "";
			}
		}
		
		public void SendHeader(string httpVer, string mimeHead, int totBytes, string statusCode, ref Socket webSocket)
		{

			String buffer = "";
			
			// if Mime type is not provided set default to text/html
			if (mimeHead.Length == 0 )
			{
				mimeHead = "text/html";  // Default Mime Type is text/html
			}

			buffer = buffer + httpVer + statusCode + "\r\n";
			buffer = buffer + "Server: cx1193719-b\r\n";
			buffer = buffer + "Content-Type: " + mimeHead + "\r\n";
			buffer = buffer + "Accept-Ranges: bytes\r\n";
			buffer = buffer + "Content-Length: " + totBytes + "\r\n\r\n";
			
			Byte[] sendData = Encoding.ASCII.GetBytes(buffer); 

			SendToBrowser( sendData, ref webSocket);

			Console.WriteLine("Total Bytes : " + totBytes.ToString());

		}

		public void SendToBrowser(String data, ref Socket webSocket)
		{
			SendToBrowser (Encoding.ASCII.GetBytes(data), ref webSocket);
		}

		public void SendToBrowser(Byte[] sendData, ref Socket webSocket)
		{
			int numBytes = 0;
			
			try
			{
				if (webSocket.Connected)
				{
					if (( numBytes = webSocket.Send(sendData, sendData.Length,0)) == -1)
						Console.WriteLine("Socket Error cannot Send Packet");
					else
					{
						Console.WriteLine("No. of bytes send {0}" , numBytes);
					}
				}
				else
					Console.WriteLine("Connection Dropped....");
			}
			catch (Exception  e)
			{
				Console.WriteLine("An error occured when sending data!");
							
			}
		}


		// Application Starts Here..
		public static void Main() 
		{
			gWebD Server = new gWebD();
		}
		
		public bool portAvailable(int port) {
			bool isAvailable = true;
			IPGlobalProperties globalProp = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] confInfo = globalProp.GetActiveTcpConnections();
			
			foreach (TcpConnectionInformation tcpi in confInfo)
			{
				if(tcpi.LocalEndPoint.Port == port)
				{
					isAvailable = false;
					break;
				}
				else
				{
					isAvailable = true;
					break;
				}
			}
			return isAvailable;
		}

		public void StartListen()
		{

			int startPos = 0;
			String Request;
			String dirName;
			String requestedFile;
			String ErrorMessage;
			String LocalDir;
			String serverRoot = "../Sites/";
			String PhysicalFilePath = "";
			String FormattedMessage = "";
			String Response = "";
			
			
			
			while(true)
			{
				//Accept a new connection
				Socket webSocket = webListener.AcceptSocket() ;

				Console.WriteLine ("Socket Type " + webSocket.SocketType ); 
				if(webSocket.Connected)
				{
					Console.WriteLine("\nClient Connected!!\n==================\nClient IP {0}\n", webSocket.RemoteEndPoint) ;

					//make a byte array and receive data from the client 
					Byte[] recieve = new Byte[1024] ;
					int i = webSocket.Receive(recieve,recieve.Length,0) ;
					//Convert Byte to String
					string buffer = Encoding.ASCII.GetString(recieve);
					
					//At present we will only deal with GET type
					if (buffer.Substring(0,3) != "GET" )
					{
						Console.WriteLine("Only Get Method is supported..");
						webSocket.Close();
						return;
					}

					
					// Look for HTTP request
					startPos = buffer.IndexOf("HTTP",1);


					// Get the HTTP text and version e.g. it will return "HTTP/1.1"
					string httpVer = buffer.Substring(startPos,8);
        
					        					
					// Extract the Requested Type and Requested file/directory
					Request = buffer.Substring(0,startPos - 1);
        
										
					//Replace backslash with Forward Slash, if Any
					Request.Replace("\\","/");


					//If file name is not supplied add forward slash to indicate 
					//that it is a directory and then we will look for the 
					//default file name..
					if ((Request.IndexOf(".") <1) && (!Request.EndsWith("/")))
					{
						Request = Request + "/"; 
					}


					//Extract the requested file name
					startPos = Request.LastIndexOf("/") + 1;
					requestedFile = Request.Substring(startPos);

					
					//Extract The directory Name
					dirName = Request.Substring(Request.IndexOf("/"), Request.LastIndexOf("/")-3);
					
						
					
					/////////////////////////////////////////////////////////////////////
					// Identify the Physical Directory
					/////////////////////////////////////////////////////////////////////
					if ( dirName == "/") {
						LocalDir = serverRoot;
					}
					else
					{
						//Get the Virtual Directory
						LocalDir = GetLocalPath(serverRoot, dirName);
					}


					Console.WriteLine("Directory Requested : " +  LocalDir);

					//If the physical directory does not exists then
					// dispaly the error message
					if (LocalDir.Length == 0 )
					{
						ErrorMessage = "<H2>Error!! Requested Directory does not exists</H2><Br>";
						//ErrorMessage = ErrorMessage + "Please check data\\Vdirs.Dat";

						//Format The Message
						SendHeader(httpVer,  "", ErrorMessage.Length, " 404 Not Found", ref webSocket);

						//Send to the browser
						SendToBrowser(ErrorMessage, ref webSocket);

						webSocket.Close();

						continue;
					}

					
					/////////////////////////////////////////////////////////////////////
					// Identify the File Name
					/////////////////////////////////////////////////////////////////////

					//If The file name is not supplied then look in the default file list
					if (requestedFile.Length == 0 )
					{
						// Get the default filename
						requestedFile = GetTheDefaultFileName(LocalDir);

						if (requestedFile == "")
						{
							ErrorMessage = "<H2>Error!! No Default File Name Specified</H2>";
							SendHeader(httpVer,  "", ErrorMessage.Length, " 404 Not Found", ref webSocket);
							SendToBrowser ( ErrorMessage, ref webSocket);

							webSocket.Close();

							return;

						}
					}

					


					/////////////////////////////////////////////////////////////////////
					// Get TheMime Type
					/////////////////////////////////////////////////////////////////////
					
					String mimeType = GetMimeType(requestedFile);

					//Build the physical path
					PhysicalFilePath = LocalDir + requestedFile;
					Console.WriteLine("File Requested : " +  PhysicalFilePath);					
					
					if (File.Exists(PhysicalFilePath) == false)
					{
						ErrorMessage = "<H2>404 Error! File Does Not Exists...</H2>";
						SendHeader(httpVer, "", ErrorMessage.Length, " 404 Not Found", ref webSocket);
						SendToBrowser( ErrorMessage, ref webSocket);

						Console.WriteLine(FormattedMessage);
					}
				
					else
					{
						int totBytes=0;

						Response ="";

						FileStream fileStream = new FileStream(PhysicalFilePath, FileMode.Open, 	FileAccess.Read, FileShare.Read);
						// Create a reader that can read bytes from the FileStream.

						
						BinaryReader reader = new BinaryReader(fileStream);
						byte[] bytes = new byte[fileStream.Length];
						int read;
						while((read = reader.Read(bytes, 0, bytes.Length)) != 0) 
						{
							// Read from the file and write the data to the network
							Response = Response + Encoding.ASCII.GetString(bytes,0,read);

							totBytes = totBytes + read;
						}
						reader.Close(); 
						fileStream.Close();

						SendHeader(httpVer,  mimeType, totBytes, " 200 OK", ref webSocket);

						SendToBrowser(bytes, ref webSocket);
						
						//webSocket.Send(bytes, bytes.Length,0);

					}
					webSocket.Close();						
				}
			}
		}
	}
}
