using System;	
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
			try
			{
				// Listens
				webListener = new TcpListener(port); // This is an older method so you will
													 // get a warning from the compiler.
				webListener.Start();
				Console.WriteLine("gWebD {0} is Running on port {1}... Press ^C to Stop...", VERSION, port);
				//start the thread which calls the method 'StartListen'
				Thread mainThread = new Thread(new ThreadStart(StartListen));
				mainThread.Start() ;

			}
			catch(Exception e)
			{
				Console.WriteLine("Unable to start, is a firewall blocking connections?");
			}
		}

		public string GetTheDefaultFileName(string sLocalDirectory)
		{
			StreamReader reader;
			String sLine = "";

			try
			{
				//Open the default.dat to find out the list
				// of default file
				reader = new StreamReader("Data/Default.dat");

				while ((sLine = reader.ReadLine()) != null)
				{
					//Look for the default file in the web server root folder
					if (File.Exists( sLocalDirectory + sLine) == true)
						break;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occured when getting default filename, check directory.");
			}
			if (File.Exists( sLocalDirectory + sLine) == true) {
				return sLine;
			}
			else {
				return "";
			}
		}

		public string GetMimeType(string sRequestedFile)
		{
			StreamReader reader;
			String sLine = "";
			String sMimeType = "";
			String sFileExt = "";
			String sMimeExt = "";
			
			// Convert to lowercase
			sRequestedFile = sRequestedFile.ToLower();
			
			int iStartPos = sRequestedFile.IndexOf(".");

			sFileExt = sRequestedFile.Substring(iStartPos);
			
			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				reader = new StreamReader("Data/Mime.Dat");

				while ((sLine = reader.ReadLine()) != null)
				{

					sLine.Trim();

					if (sLine.Length > 0)
					{
						//find the separator
						iStartPos = sLine.IndexOf(";");
						
						// Convert to lower case
						sLine = sLine.ToLower();
						
						sMimeExt = sLine.Substring(0,iStartPos);
						sMimeType = sLine.Substring(iStartPos + 1);
						
						if (sMimeExt == sFileExt) {
							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occured whilst checking MIME type!");
			}

			if (sMimeExt == sFileExt) {
				return sMimeType; 
			}
			else {
				return "";
			}
		}

		public string GetLocalPath(string sMyWebServerRoot, string sDirName)
		{
			StreamReader reader;
			String sLine = "";
			String sVirtualDir = ""; 
			String sRealDir = "";
			int iStartPos = 0;
			
			//Remove extra spaces
			sDirName.Trim();
			
			// Convert to lowercase
			sMyWebServerRoot = sMyWebServerRoot.ToLower();
			
			// Convert to lowercase
			sDirName = sDirName.ToLower();

			//Remove the slash
			//sDirName = sDirName.Substring(1, sDirName.Length - 2);


			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				reader = new StreamReader("Data/VDirs.Dat");

				while ((sLine = reader.ReadLine()) != null)
				{
					//Remove extra Spaces
					sLine.Trim();

					if (sLine.Length > 0)
					{
						//find the separator
						iStartPos = sLine.IndexOf(";");
						
						// Convert to lowercase
						sLine = sLine.ToLower();
						
						sVirtualDir = sLine.Substring(0,iStartPos);
						sRealDir = sLine.Substring(iStartPos + 1);
						
						if (sVirtualDir == sDirName)
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


			Console.WriteLine("Virtual Dir : " + sVirtualDir);
			Console.WriteLine("Directory   : " + sDirName);
			Console.WriteLine("Physical Dir: " + sRealDir);
			if (sVirtualDir == sDirName) {
				return sRealDir;
			}
			else {
				return "";
			}
		}
		
		public void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, ref Socket webSocket)
		{

			String sBuffer = "";
			
			// if Mime type is not provided set default to text/html
			if (sMIMEHeader.Length == 0 )
			{
				sMIMEHeader = "text/html";  // Default Mime Type is text/html
			}

			sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
			sBuffer = sBuffer + "Server: cx1193719-b\r\n";
			sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
			sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
			sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
			
			Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer); 

			SendToBrowser( bSendData, ref webSocket);

			Console.WriteLine("Total Bytes : " + iTotBytes.ToString());

		}

		public void SendToBrowser(String sData, ref Socket webSocket)
		{
			SendToBrowser (Encoding.ASCII.GetBytes(sData), ref webSocket);
		}

		public void SendToBrowser(Byte[] bSendData, ref Socket webSocket)
		{
			int numBytes = 0;
			
			try
			{
				if (webSocket.Connected)
				{
					if (( numBytes = webSocket.Send(bSendData, bSendData.Length,0)) == -1)
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

		public void StartListen()
		{

			int iStartPos = 0;
			String sRequest;
			String sDirName;
			String sRequestedFile;
			String sErrorMessage;
			String sLocalDir;
			String sMyWebServerRoot = "../Sites/";
			String sPhysicalFilePath = "";
			String sFormattedMessage = "";
			String sResponse = "";
			
			
			
			while(true)
			{
				//Accept a new connection
				Socket webSocket = webListener.AcceptSocket() ;

				Console.WriteLine ("Socket Type " + webSocket.SocketType ); 
				if(webSocket.Connected)
				{
					Console.WriteLine("\nClient Connected!!\n==================\nClient IP {0}\n", webSocket.RemoteEndPoint) ;

					//make a byte array and receive data from the client 
					Byte[] bReceive = new Byte[1024] ;
					int i = webSocket.Receive(bReceive,bReceive.Length,0) ;
					//Convert Byte to String
					string sBuffer = Encoding.ASCII.GetString(bReceive);
					
					//At present we will only deal with GET type
					if (sBuffer.Substring(0,3) != "GET" )
					{
						Console.WriteLine("Only Get Method is supported..");
						webSocket.Close();
						return;
					}

					
					// Look for HTTP request
					iStartPos = sBuffer.IndexOf("HTTP",1);


					// Get the HTTP text and version e.g. it will return "HTTP/1.1"
					string sHttpVersion = sBuffer.Substring(iStartPos,8);
        
					        					
					// Extract the Requested Type and Requested file/directory
					sRequest = sBuffer.Substring(0,iStartPos - 1);
        
										
					//Replace backslash with Forward Slash, if Any
					sRequest.Replace("\\","/");


					//If file name is not supplied add forward slash to indicate 
					//that it is a directory and then we will look for the 
					//default file name..
					if ((sRequest.IndexOf(".") <1) && (!sRequest.EndsWith("/")))
					{
						sRequest = sRequest + "/"; 
					}


					//Extract the requested file name
					iStartPos = sRequest.LastIndexOf("/") + 1;
					sRequestedFile = sRequest.Substring(iStartPos);

					
					//Extract The directory Name
					sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/")-3);
					
						
					
					/////////////////////////////////////////////////////////////////////
					// Identify the Physical Directory
					/////////////////////////////////////////////////////////////////////
					if ( sDirName == "/") {
						sLocalDir = sMyWebServerRoot;
					}
					else
					{
						//Get the Virtual Directory
						sLocalDir = GetLocalPath(sMyWebServerRoot, sDirName);
					}


					Console.WriteLine("Directory Requested : " +  sLocalDir);

					//If the physical directory does not exists then
					// dispaly the error message
					if (sLocalDir.Length == 0 )
					{
						sErrorMessage = "<H2>Error!! Requested Directory does not exists</H2><Br>";
						//sErrorMessage = sErrorMessage + "Please check data\\Vdirs.Dat";

						//Format The Message
						SendHeader(sHttpVersion,  "", sErrorMessage.Length, " 404 Not Found", ref webSocket);

						//Send to the browser
						SendToBrowser(sErrorMessage, ref webSocket);

						webSocket.Close();

						continue;
					}

					
					/////////////////////////////////////////////////////////////////////
					// Identify the File Name
					/////////////////////////////////////////////////////////////////////

					//If The file name is not supplied then look in the default file list
					if (sRequestedFile.Length == 0 )
					{
						// Get the default filename
						sRequestedFile = GetTheDefaultFileName(sLocalDir);

						if (sRequestedFile == "")
						{
							sErrorMessage = "<H2>Error!! No Default File Name Specified</H2>";
							SendHeader(sHttpVersion,  "", sErrorMessage.Length, " 404 Not Found", ref webSocket);
							SendToBrowser ( sErrorMessage, ref webSocket);

							webSocket.Close();

							return;

						}
					}

					


					/////////////////////////////////////////////////////////////////////
					// Get TheMime Type
					/////////////////////////////////////////////////////////////////////
					
					String sMimeType = GetMimeType(sRequestedFile);

					//Build the physical path
					sPhysicalFilePath = sLocalDir + sRequestedFile;
					Console.WriteLine("File Requested : " +  sPhysicalFilePath);					
					
					if (File.Exists(sPhysicalFilePath) == false)
					{
						sErrorMessage = "<H2>404 Error! File Does Not Exists...</H2>";
						SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref webSocket);
						SendToBrowser( sErrorMessage, ref webSocket);

						Console.WriteLine(sFormattedMessage);
					}
				
					else
					{
						int iTotBytes=0;

						sResponse ="";

						FileStream fileStream = new FileStream(sPhysicalFilePath, FileMode.Open, 	FileAccess.Read, FileShare.Read);
						// Create a reader that can read bytes from the FileStream.

						
						BinaryReader reader = new BinaryReader(fileStream);
						byte[] bytes = new byte[fileStream.Length];
						int read;
						while((read = reader.Read(bytes, 0, bytes.Length)) != 0) 
						{
							// Read from the file and write the data to the network
							sResponse = sResponse + Encoding.ASCII.GetString(bytes,0,read);

							iTotBytes = iTotBytes + read;
						}
						reader.Close(); 
						fileStream.Close();

						SendHeader(sHttpVersion,  sMimeType, iTotBytes, " 200 OK", ref webSocket);

						SendToBrowser(bytes, ref webSocket);
						
						//webSocket.Send(bytes, bytes.Length,0);

					}
					webSocket.Close();						
				}
			}
		}
	}
}
