using Bypass.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_IPHONE || UNITY_ANDROID )
    using UnityEngine;
#endif

namespace Bypass
{
	public class BypassClient : TCPClientManager
	{
		private string id;
		private string tag;
		private bool autoRegister = false;
		public BypassClient(string ip, int port, string separadorDeComandos = ";", string id = "", string tag = "")
			: base()
		{
			this.id = id;
			this.tag = tag;
			autoRegister = true;
			ConnectEvent += AutoRegister;
			Initialize(ip, port, separadorDeComandos);
		}
		public BypassClient() : base() { }
		private void AutoRegister(object sender, ConnectEventArgs args)
		{
			Register(id, tag);
		}
		public void Register(string id, string tag)
		{
			SendCommand("{\"type\":\"register\", \"data\":\""+id+"\", \"tag\":\""+tag+"\"}");
		}
		public void SendData(string data, string tag, params string[] ids)
		{
			
			JSONClass n = new JSONClass ();
			n["type"] = "send";
			n["data"] = data;
			n["tag"] = tag;
			n["ids"] = ConcatIds(ids);
			SendCommand (n.ToString());
			//SendCommand("{\"type\":\"send\", \"data\":\"" + data + "\", \"tag\":\""+tag+"\", \"ids\":["+ConcatIds(ids)+"]}");
			
		}
		public void SendData(string data, string tag = "")
		{
			JSONClass n = new JSONClass ();
			n["type"] = "send";
			n["data"] = data;
			n["tag"] = tag;
			n["ids"] = new JSONArray();
			SendCommand (n.ToString());
			//SendCommand("{\"type\":\"send\", \"data\":\"" + data + "\", \"tag\":\""+tag+"\", \"ids\":[]}");
		}
		public void Broadcast(string data)
		{
			JSONClass n = new JSONClass ();
			n["type"] = "broadcast";
			n["data"] = data;
			n["tag"] = "";
			n["ids"] = new JSONArray();
			SendCommand (n.ToString());
			//SendCommand("{\"type\":\"broadcast\", \"data\":\"" + data + "\", \"tag\":\"\", \"ids\":[]}");
		}
		public void BroadcastAll(string data)
		{
			JSONClass n = new JSONClass ();
			n["type"] = "broadcastAll";
			n["data"] = data;
			n["tag"] = "";
			n["ids"] = new JSONArray();
			SendCommand (n.ToString());
			//SendCommand("{\"type\":\"broadcastAll\", \"data\":\"" + data + "\", \"tag\":\"\", \"ids\":[]}");
		}
		private JSONArray ConcatIds(string[] ids)
		{
			JSONArray s = new JSONArray();
			for (int i = 0; i < ids.Length; i++)
			{
				s.Add(ids[i]);
			}
			return s;
		}
	}
	public class TCPClientManager
	{
		/*private ManualResetEvent connectDone = new ManualResetEvent(false);
	    private static ManualResetEvent sendDone = new ManualResetEvent(false);
	    private static ManualResetEvent receiveDone = new ManualResetEvent(false);*/
		
		public event EventHandler<CommandEventArgs> CommandReceivedEvent;
		public event EventHandler<ConnectEventArgs> ConnectEvent;
		public event EventHandler<DisconnectEventArgs> DisconnectEvent;
		
		
		private TcpClient socket;
		private Thread runThread;
		private string buffer;
		
		private bool _conectado = false;
		public bool connected
		{
			get
			{
				return _conectado;
			}
		}
		
		private string ipServidor;
		private int portServidor;
		
		private string separadorDeComandos;
		
		/*public static TCPClientManager instancia;
	    public void Awake() {
	        instancia = this;
	        Application.runInBackground = true;
	    }*/
		
		private float tRetry = 4;
		//private float tAcumuladoRetry = 45;
		private StreamWriter streamWriter;
		bool run = true;
		
		public void Initialize(string ip, int port, string separadorDeComandos = ";")
		{
			LoguearAConsola("Conectando con el servidor");
			run = true;
			if (runThread == null)
			{
				runThread = new Thread(new ThreadStart(Update));
				runThread.Start();
			}
			disconnectTest = Encoding.UTF8.GetBytes("0" + separadorDeComandos);
			ipServidor = ip;
			portServidor = port;
			
			this.separadorDeComandos = separadorDeComandos;
			
			//Debug.Log("Iniciando cliente TCP en " + ip + ":" + port);
			
			_conectado = false;
			
			try
			{
				socket = new TcpClient(ip, port);
				netStream = socket.GetStream();
                #if UNITY_5
                    netStream.ReadTimeout = 33;
                #endif
				//Debug.Log("Conexion con el servidor establecida");
				_conectado = true;
				LoguearAConsola("Conectado");
				onConnect();
				
			}
			catch (Exception e)
			{
				//Debug.Log("Fallo el intento de conexion al server. Reintentando en un rato. Mensaje de error: "+e.Message);
				timer = new Timer(ReintentarConexionAhora, null, 3000, Timeout.Infinite);
				//ReintentarConexionEnUnRato();
			}
		}

        private void LoguearAConsola(string textoALoguear)
        {
            //throw new NotImplementedException();
            #if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_IPHONE || UNITY_ANDROID )
                Debug.Log(textoALoguear);
            #else
                Console.WriteLine(textoALoguear);
            #endif
        }
		Timer timer;
		/*private void ReintentarConexionEnUnRato()
	    {
	        ReintentarConexionAhora();
	        //Invoke("ReintentarConexionAhora", 3);
	    }*/
		
		private void ReintentarConexionAhora(object state)
		{
			timer.Dispose();
			//Debug.Log("Reintentando conexion...");
			if (!_conectado) Initialize(ipServidor, portServidor);
		}
		private void ReintentarConexionAhora()
		{
			//Debug.Log("Reintentando conexion...");
			if (!_conectado) Initialize(ipServidor, portServidor);
		}
		
		NetworkStream netStream;
		DateTime t;
		public void Update()
		{
			t = DateTime.Now;
			/*if (Input.GetKeyDown(KeyCode.Space))
	        {
	            OnData(s);
	        }/*
	        if (Input.GetKeyDown(KeyCode.A))
	        {
	            OnData(s2);
	        }*/

#if UNITY_5
{
    if (run)
    {
        clientLoop();
    }
}
#else
{
    while (run) {
        clientLoop();
    }
}
#endif



		}


	    private void clientLoop()
	    {
            {
                if (_conectado && netStream.CanRead)
                {
                    byte[] myReadBuffer = new byte[1024];
                    StringBuilder myCompleteMessage = new StringBuilder();
                    int numberOfBytesRead = 0;

                    // Incoming message may be larger than the buffer size. 
                    //Debug.Log(netStream.DataAvailable);
                    while (netStream != null && netStream.DataAvailable)
                    {
                        numberOfBytesRead = netStream.Read(myReadBuffer, 0, myReadBuffer.Length);

                        myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));

                    }

                    string mensajeCompleto = myCompleteMessage.ToString();
                    if (mensajeCompleto != "")
                    {
                        OnData(mensajeCompleto);
                    }
                }

                if (_conectado && netStream.CanWrite)
                {
                    if (DateTime.Now.Subtract(t).TotalSeconds > tRetry)
                    {
                        t = DateTime.Now;

                        if (!IsConnected)
                        {
                            _conectado = false;
                            onDisconnect();
                            ReintentarConexionAhora();
                        }
                        /* 
                            //tAcumuladoRetry = tRetry;
                            try
                            {
                                netStream.Write(disconnectTest, 0, disconnectTest.Length);
                                netStream.Flush();
                            }
                            catch
                            {
                                _conectado = false;
                                onDisconnect();
                                //Debug.Log("Perdida conexion con el servidor. Reintentando en un rato");
                                ReintentarConexionEnUnRato();
                            }
                            */
                    }
                }



            }
	    }

		
		private bool IsConnected
		{
			get
			{
				try
				{
					if (socket != null && socket.Client != null && socket.Client.Connected)
					{
						// Detect if client disconnected
						if (socket.Client.Poll(0, SelectMode.SelectRead))
						{
							byte[] buff = new byte[1];
							if (socket.Client.Receive(buff, SocketFlags.Peek) == 0)
							{
								// Client disconnected
								return false;
							}
							else
							{
								return true;
							}
						}
						
						return true;
					}
					else
					{
						return false;
					}
				}
				catch
				{
					return false;
				}
			}
		}
		
		byte[] disconnectTest;
		static byte[] GetBytes(string str)
		{
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}
		public void SendCommand(string data)
		{
			if (IsConnected)
			{
				Stream s = socket.GetStream();
				byte[] d = Encoding.UTF8.GetBytes(data + separadorDeComandos);
				try
				{
					s.Write(d, 0, d.Length);
					s.Flush();
				}
				catch (Exception e)
				{
					//Debug.Log(e.Message);
				}
			}
		}
		void onConnect()
		{
			EventHandler<ConnectEventArgs> newEvent = ConnectEvent;
			ConnectEventArgs _arg = new ConnectEventArgs();
			if (newEvent != null) newEvent(this, _arg);
		}
		void onDisconnect()
		{
			EventHandler<DisconnectEventArgs> newEvent = DisconnectEvent;
			DisconnectEventArgs arg = new DisconnectEventArgs();
			if (newEvent != null) newEvent(this, arg);
		}
		private void enviarEventoComando(string comando)
		{
			EventHandler<CommandEventArgs> newEvent = CommandReceivedEvent;
			CommandEventArgs _arg = new CommandEventArgs(comando);
			if (newEvent != null) newEvent(this, _arg);
		}
		
		void OnData(string data)
		{
			data = buffer + data;
			//data += buffer;
			//Debug.Log("Recibida data en el buffer: "+data);
			string[] commands = data.Split(new string[] { separadorDeComandos }, StringSplitOptions.None);
			buffer = commands[commands.Length - 1];
			
			//Debug.Log("Se lograron extraer "+(commands.Length - 1)+" comandos finalizados con '"+separadorDeComandos+"'");
			
			for (int j = 0; j < commands.Length - 1; j++)
			{
				if (commands[j] == "")
				{
					continue;
				}
				
				//Debug.Log("Enviando evento de comando: "+commands[j]);
				enviarEventoComando(commands[j]);
				
			}
			
			//Debug.Log("Quedo en el buffer: '" + buffer + "'");
			
		}
		void OnConnect()
		{
			//Debug.Log ("Connected");
			streamWriter = new StreamWriter(socket.GetStream());
		}
		public void Close()
		{
			if (socket != null)
			{
                run = false;
				socket.Close();
			}
			
		}
		
	}

	public class CommandEventArgs : EventArgs
	{
		public string command = "";
		
		public CommandEventArgs(string comando)
		{
			this.command = comando;
		}
	}
	public class ConnectEventArgs : EventArgs
	{
		//public string comando = "";
		
		public ConnectEventArgs(/*string comando*/)
		{
			//this.comando = comando;
		}
	}
	public class DisconnectEventArgs : EventArgs
	{
		//public string comando = "";
		
		public DisconnectEventArgs(/*string comando*/)
		{
			//this.comando = comando;
		}
	}
	//#define USE_SharpZipLib
	/* * * * *
	 * A simple JSON Parser / builder
	 * ------------------------------
	 * 
	 * It mainly has been written as a simple JSON parser. It can build a JSON string
	 * from the node-tree, or generate a node tree from any valid JSON string.
	 * 
	 * If you want to use compression when saving to file / stream / B64 you have to include
	 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
	 * define "USE_SharpZipLib" at the top of the file
	 * 
	 * Written by Bunny83 
	 * 2012-06-09
	 * 
	 * Modified by oPless, 2014-09-21 to round-trip properly
	 *
	 * Features / attributes:
	 * - provides strongly typed node classes and lists / dictionaries
	 * - provides easy access to class members / array items / data values
	 * - the parser ignores data types. Each value is a string.
	 * - only double quotes (") are used for quoting strings.
	 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
	 * - There are only 3 types: arrays(JSONArray), objects(JSONClass) and values(JSONData)
	 * - provides "casting" properties to easily convert to / from those types:
	 *   int / float / double / bool
	 * - provides a common interface for each node so no explicit casting is required.
	 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
	 * 
	 * 
	 * 2012-12-17 Update:
	 * - Added internal JSONLazyCreator class which simplifies the construction of a JSON tree
	 *   Now you can simple reference any item that doesn't exist yet and it will return a JSONLazyCreator
	 *   The class determines the required type by it's further use, creates the type and removes itself.
	 * - Added binary serialization / deserialization.
	 * - Added support for BZip2 zipped binary format. Requires the SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ )
	 *   The usage of the SharpZipLib library can be disabled by removing or commenting out the USE_SharpZipLib define at the top
	 * - The serializer uses different types when it comes to store the values. Since my data values
	 *   are all of type string, the serializer will "try" which format fits best. The order is: int, float, double, bool, string.
	 *   It's not the most efficient way but for a moderate amount of data it should work on all platforms.
	 * 
	 * * * * */


	namespace SimpleJSON
	{
		public enum JSONBinaryTag
		{
			Array = 1,
			Class = 2,
			Value = 3,
			IntValue = 4,
			DoubleValue = 5,
			BoolValue = 6,
			FloatValue = 7,
		}
		
		public abstract class JSONNode
		{
			#region common interface
			
			public virtual void Add (string aKey, JSONNode aItem)
			{
			}
			
			public virtual JSONNode this [int aIndex]   { get { return null; } set { } }
			
			public virtual JSONNode this [string aKey]  { get { return null; } set { } }
			
			public virtual string Value                { get { return ""; } set { } }
			
			public virtual int Count                   { get { return 0; } }
			
			public virtual void Add (JSONNode aItem)
			{
				Add ("", aItem);
			}
			
			public virtual JSONNode Remove (string aKey)
			{
				return null;
			}
			
			public virtual JSONNode Remove (int aIndex)
			{
				return null;
			}
			
			public virtual JSONNode Remove (JSONNode aNode)
			{
				return aNode;
			}
			
			public virtual IEnumerable<JSONNode> Children
			{
				get {
					yield break;
				}
			}
			
			public IEnumerable<JSONNode> DeepChildren
			{
				get {
					foreach (var C in Children)
						foreach (var D in C.DeepChildren)
							yield return D;
				}
			}
			
			public override string ToString ()
			{
				return "JSONNode";
			}
			
			public virtual string ToString (string aPrefix)
			{
				return "JSONNode";
			}
			
			public abstract string ToJSON (int prefix);
			
			#endregion common interface
			
			#region typecasting properties
			
			public virtual JSONBinaryTag Tag { get; set; }
			
			public virtual int AsInt
			{
				get {
					int v = 0;
					if (int.TryParse (Value, out v))
						return v;
					return 0;
				}
				set {
					Value = value.ToString ();
					Tag = JSONBinaryTag.IntValue;
				}
			}
			
			public virtual float AsFloat
			{
				get {
					float v = 0.0f;
					if (float.TryParse (Value, out v))
						return v;
					return 0.0f;
				}
				set {
					Value = value.ToString ();
					Tag = JSONBinaryTag.FloatValue;
				}
			}
			
			public virtual double AsDouble
			{
				get {
					double v = 0.0;
					if (double.TryParse (Value, out v))
						return v;
					return 0.0;
				}
				set {
					Value = value.ToString ();
					Tag = JSONBinaryTag.DoubleValue;
					
				}
			}
			
			public virtual bool AsBool
			{
				get {
					bool v = false;
					if (bool.TryParse (Value, out v))
						return v;
					return !string.IsNullOrEmpty (Value);
				}
				set {
					Value = (value) ? "true" : "false";
					Tag = JSONBinaryTag.BoolValue;
					
				}
			}
			
			public virtual JSONArray AsArray
			{
				get {
					return this as JSONArray;
				}
			}
			
			public virtual JSONClass AsObject
			{
				get {
					return this as JSONClass;
				}
			}
			
			
			#endregion typecasting properties
			
			#region operators
			
			public static implicit operator JSONNode (string s)
			{
				return new JSONData (s);
			}
			
			public static implicit operator string (JSONNode d)
			{
				return (d == null) ? null : d.Value;
			}
			
			public static bool operator == (JSONNode a, object b)
			{
				if (b == null && a is JSONLazyCreator)
					return true;
				return System.Object.ReferenceEquals (a, b);
			}
			
			public static bool operator != (JSONNode a, object b)
			{
				return !(a == b);
			}
			
			public override bool Equals (object obj)
			{
				return System.Object.ReferenceEquals (this, obj);
			}
			
			public override int GetHashCode ()
			{
				return base.GetHashCode ();
			}
			
			
			#endregion operators
			
			internal static string Escape (string aText)
			{
				string result = "";
				foreach (char c in aText) {
					switch (c) {
					case '\\':
						result += "\\\\";
						break;
					case '\"':
						result += "\\\"";
						break;
					case '\n':
						result += "\\n";
						break;
					case '\r':
						result += "\\r";
						break;
					case '\t':
						result += "\\t";
						break;
					case '\b':
						result += "\\b";
						break;
					case '\f':
						result += "\\f";
						break;
					default   :
						result += c;
						break;
					}
				}
				return result;
			}
			
			static JSONData Numberize (string token)
			{
				bool flag = false;
				int integer = 0;
				double real = 0;
				
				if (int.TryParse (token, out integer)) {
					return new JSONData (integer);
				}
				
				if (double.TryParse (token, out real)) {
					return new JSONData (real);
				}
				
				if (bool.TryParse (token, out flag)) {
					return new JSONData (flag);
				}
				
				throw new NotImplementedException (token);
			}
			
			static void AddElement (JSONNode ctx, string token, string tokenName, bool tokenIsString)
			{
				if (tokenIsString) {
					if (ctx is JSONArray)
						ctx.Add (token);
					else
						ctx.Add (tokenName, token); // assume dictionary/object
				} else {
					JSONData number = Numberize (token);
					if (ctx is JSONArray)
						ctx.Add (number);
					else
						ctx.Add (tokenName, number);
					
				}
			}
			
			public static JSONNode Parse (string aJSON)
			{
				Stack<JSONNode> stack = new Stack<JSONNode> ();
				JSONNode ctx = null;
				int i = 0;
				string Token = "";
				string TokenName = "";
				bool QuoteMode = false;
				bool TokenIsString = false;
				while (i < aJSON.Length) {
					switch (aJSON [i]) {
					case '{':
						if (QuoteMode) {
							Token += aJSON [i];
							break;
						}
						stack.Push (new JSONClass ());
						if (ctx != null) {
							TokenName = TokenName.Trim ();
							if (ctx is JSONArray)
								ctx.Add (stack.Peek ());
							else if (TokenName != "")
								ctx.Add (TokenName, stack.Peek ());
						}
						TokenName = "";
						Token = "";
						ctx = stack.Peek ();
						break;
						
					case '[':
						if (QuoteMode) {
							Token += aJSON [i];
							break;
						}
						
						stack.Push (new JSONArray ());
						if (ctx != null) {
							TokenName = TokenName.Trim ();
							
							if (ctx is JSONArray)
								ctx.Add (stack.Peek ());
							else if (TokenName != "")
								ctx.Add (TokenName, stack.Peek ());
						}
						TokenName = "";
						Token = "";
						ctx = stack.Peek ();
						break;
						
					case '}':
					case ']':
						if (QuoteMode) {
							Token += aJSON [i];
							break;
						}
						if (stack.Count == 0)
							throw new Exception ("JSON Parse: Too many closing brackets");
						
						stack.Pop ();
						if (Token != "") {
							TokenName = TokenName.Trim ();
							/*
								if (ctx is JSONArray)
									ctx.Add (Token);
								else if (TokenName != "")
									ctx.Add (TokenName, Token);
									*/
							AddElement (ctx, Token, TokenName, TokenIsString);
							TokenIsString = false;
						}
						TokenName = "";
						Token = "";
						if (stack.Count > 0)
							ctx = stack.Peek ();
						break;
						
					case ':':
						if (QuoteMode) {
							Token += aJSON [i];
							break;
						}
						TokenName = Token;
						Token = "";
						TokenIsString = false;
						break;
						
					case '"':
						QuoteMode ^= true;
						TokenIsString = QuoteMode == true ? true : TokenIsString;
						break;
						
					case ',':
						if (QuoteMode) {
							Token += aJSON [i];
							break;
						}
						if (Token != "") {
							/*
								if (ctx is JSONArray) {
									ctx.Add (Token);
								} else if (TokenName != "") {
									ctx.Add (TokenName, Token);
								}
								*/
							AddElement (ctx, Token, TokenName, TokenIsString);
							TokenIsString = false;
							
						}
						TokenName = "";
						Token = "";
						TokenIsString = false;
						break;
						
					case '\r':
					case '\n':
						break;
						
					case ' ':
					case '\t':
						if (QuoteMode)
							Token += aJSON [i];
						break;
						
					case '\\':
						++i;
						if (QuoteMode) {
							char C = aJSON [i];
							switch (C) {
							case 't':
								Token += '\t';
								break;
							case 'r':
								Token += '\r';
								break;
							case 'n':
								Token += '\n';
								break;
							case 'b':
								Token += '\b';
								break;
							case 'f':
								Token += '\f';
								break;
							case 'u':
							{
								string s = aJSON.Substring (i + 1, 4);
								Token += (char)int.Parse (
									s,
									System.Globalization.NumberStyles.AllowHexSpecifier);
								i += 4;
								break;
							}
							default  :
								Token += C;
								break;
							}
						}
						break;
						
					default:
						Token += aJSON [i];
						break;
					}
					++i;
				}
				if (QuoteMode) {
					throw new Exception ("JSON Parse: Quotation marks seems to be messed up.");
				}
				return ctx;
			}
			
			public virtual void Serialize (System.IO.BinaryWriter aWriter)
			{
			}
			
			public void SaveToStream (System.IO.Stream aData)
			{
				var W = new System.IO.BinaryWriter (aData);
				Serialize (W);
			}
			
			#if USE_SharpZipLib
			public void SaveToCompressedStream(System.IO.Stream aData)
			{
				using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
				{
					gzipOut.IsStreamOwner = false;
					SaveToStream(gzipOut);
					gzipOut.Close();
				}
			}
			
			public void SaveToCompressedFile(string aFileName)
			{
				
				#if USE_FileIO
				System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
				using(var F = System.IO.File.OpenWrite(aFileName))
				{
					SaveToCompressedStream(F);
				}
				
				#else
				throw new Exception("Can't use File IO stuff in webplayer");
				#endif
			}
			public string SaveToCompressedBase64()
			{
				using (var stream = new System.IO.MemoryStream())
				{
					SaveToCompressedStream(stream);
					stream.Position = 0;
					return System.Convert.ToBase64String(stream.ToArray());
				}
			}
			
			#else
			public void SaveToCompressedStream (System.IO.Stream aData)
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			
			public void SaveToCompressedFile (string aFileName)
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			
			public string SaveToCompressedBase64 ()
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			#endif
			
			public void SaveToFile (string aFileName)
			{
				#if USE_FileIO
				System.IO.Directory.CreateDirectory ((new System.IO.FileInfo (aFileName)).Directory.FullName);
				using (var F = System.IO.File.OpenWrite (aFileName)) {
					SaveToStream (F);
				}
				#else
				throw new Exception ("Can't use File IO stuff in webplayer");
				#endif
			}
			
			public string SaveToBase64 ()
			{
				using (var stream = new System.IO.MemoryStream ()) {
					SaveToStream (stream);
					stream.Position = 0;
					return System.Convert.ToBase64String (stream.ToArray ());
				}
			}
			
			public static JSONNode Deserialize (System.IO.BinaryReader aReader)
			{
				JSONBinaryTag type = (JSONBinaryTag)aReader.ReadByte ();
				switch (type) {
				case JSONBinaryTag.Array:
				{
					int count = aReader.ReadInt32 ();
					JSONArray tmp = new JSONArray ();
					for (int i = 0; i < count; i++)
						tmp.Add (Deserialize (aReader));
					return tmp;
				}
				case JSONBinaryTag.Class:
				{
					int count = aReader.ReadInt32 ();                
					JSONClass tmp = new JSONClass ();
					for (int i = 0; i < count; i++) {
						string key = aReader.ReadString ();
						var val = Deserialize (aReader);
						tmp.Add (key, val);
					}
					return tmp;
				}
				case JSONBinaryTag.Value:
				{
					return new JSONData (aReader.ReadString ());
				}
				case JSONBinaryTag.IntValue:
				{
					return new JSONData (aReader.ReadInt32 ());
				}
				case JSONBinaryTag.DoubleValue:
				{
					return new JSONData (aReader.ReadDouble ());
				}
				case JSONBinaryTag.BoolValue:
				{
					return new JSONData (aReader.ReadBoolean ());
				}
				case JSONBinaryTag.FloatValue:
				{
					return new JSONData (aReader.ReadSingle ());
				}
					
				default:
				{
					throw new Exception ("Error deserializing JSON. Unknown tag: " + type);
				}
				}
			}
			
			#if USE_SharpZipLib
			public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
			{
				var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
				return LoadFromStream(zin);
			}
			public static JSONNode LoadFromCompressedFile(string aFileName)
			{
				#if USE_FileIO
				using(var F = System.IO.File.OpenRead(aFileName))
				{
					return LoadFromCompressedStream(F);
				}
				#else
				throw new Exception("Can't use File IO stuff in webplayer");
				#endif
			}
			public static JSONNode LoadFromCompressedBase64(string aBase64)
			{
				var tmp = System.Convert.FromBase64String(aBase64);
				var stream = new System.IO.MemoryStream(tmp);
				stream.Position = 0;
				return LoadFromCompressedStream(stream);
			}
			#else
			public static JSONNode LoadFromCompressedFile (string aFileName)
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			
			public static JSONNode LoadFromCompressedStream (System.IO.Stream aData)
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			
			public static JSONNode LoadFromCompressedBase64 (string aBase64)
			{
				throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
			}
			#endif
			
			public static JSONNode LoadFromStream (System.IO.Stream aData)
			{
				using (var R = new System.IO.BinaryReader (aData)) {
					return Deserialize (R);
				}
			}
			
			public static JSONNode LoadFromFile (string aFileName)
			{
				#if USE_FileIO
				using (var F = System.IO.File.OpenRead (aFileName)) {
					return LoadFromStream (F);
				}
				#else
				throw new Exception ("Can't use File IO stuff in webplayer");
				#endif
			}
			
			public static JSONNode LoadFromBase64 (string aBase64)
			{
				var tmp = System.Convert.FromBase64String (aBase64);
				var stream = new System.IO.MemoryStream (tmp);
				stream.Position = 0;
				return LoadFromStream (stream);
			}
		}
		// End of JSONNode
		
		public class JSONArray : JSONNode, IEnumerable
		{
			private List<JSONNode> m_List = new List<JSONNode> ();
			
			public override JSONNode this [int aIndex]
			{
				get {
					if (aIndex < 0 || aIndex >= m_List.Count)
						return new JSONLazyCreator (this);
					return m_List [aIndex];
				}
				set {
					if (aIndex < 0 || aIndex >= m_List.Count)
						m_List.Add (value);
					else
						m_List [aIndex] = value;
				}
			}
			
			public override JSONNode this [string aKey]
			{
				get{ return new JSONLazyCreator (this); }
				set{ m_List.Add (value); }
			}
			
			public override int Count
			{
				get { return m_List.Count; }
			}
			
			public override void Add (string aKey, JSONNode aItem)
			{
				m_List.Add (aItem);
			}
			
			public override JSONNode Remove (int aIndex)
			{
				if (aIndex < 0 || aIndex >= m_List.Count)
					return null;
				JSONNode tmp = m_List [aIndex];
				m_List.RemoveAt (aIndex);
				return tmp;
			}
			
			public override JSONNode Remove (JSONNode aNode)
			{
				m_List.Remove (aNode);
				return aNode;
			}
			
			public override IEnumerable<JSONNode> Children
			{
				get {
					foreach (JSONNode N in m_List)
						yield return N;
				}
			}
			
			public IEnumerator GetEnumerator ()
			{
				foreach (JSONNode N in m_List)
					yield return N;
			}
			
			public override string ToString ()
			{
				string result = "[ ";
				foreach (JSONNode N in m_List) {
					if (result.Length > 2)
						result += ", ";
					result += N.ToString ();
				}
				result += " ]";
				return result;
			}
			
			public override string ToString (string aPrefix)
			{
				string result = "[ ";
				foreach (JSONNode N in m_List) {
					if (result.Length > 3)
						result += ", ";
					result += "\n" + aPrefix + "   ";                
					result += N.ToString (aPrefix + "   ");
				}
				result += "\n" + aPrefix + "]";
				return result;
			}
			
			public override string ToJSON (int prefix)
			{
				string s = new string (' ', (prefix + 1) * 2);
				string ret = "[ ";
				foreach (JSONNode n in m_List) {
					if (ret.Length > 3)
						ret += ", ";
					ret += "\n" + s;
					ret += n.ToJSON (prefix + 1);
					
				}
				ret += "\n" + s + "]";
				return ret;
			}
			
			public override void Serialize (System.IO.BinaryWriter aWriter)
			{
				aWriter.Write ((byte)JSONBinaryTag.Array);
				aWriter.Write (m_List.Count);
				for (int i = 0; i < m_List.Count; i++) {
					m_List [i].Serialize (aWriter);
				}
			}
		}
		// End of JSONArray
		
		public class JSONClass : JSONNode, IEnumerable
		{
			private Dictionary<string,JSONNode> m_Dict = new Dictionary<string,JSONNode> ();
			
			public override JSONNode this [string aKey]
			{
				get {
					if (m_Dict.ContainsKey (aKey))
						return m_Dict [aKey];
					else
						return new JSONLazyCreator (this, aKey);
				}
				set {
					if (m_Dict.ContainsKey (aKey))
						m_Dict [aKey] = value;
					else
						m_Dict.Add (aKey, value);
				}
			}
			
			public override JSONNode this [int aIndex]
			{
				get {
					if (aIndex < 0 || aIndex >= m_Dict.Count)
						return null;
					return m_Dict.ElementAt (aIndex).Value;
				}
				set {
					if (aIndex < 0 || aIndex >= m_Dict.Count)
						return;
					string key = m_Dict.ElementAt (aIndex).Key;
					m_Dict [key] = value;
				}
			}
			
			public override int Count
			{
				get { return m_Dict.Count; }
			}
			
			
			public override void Add (string aKey, JSONNode aItem)
			{
				if (!string.IsNullOrEmpty (aKey)) {
					if (m_Dict.ContainsKey (aKey))
						m_Dict [aKey] = aItem;
					else
						m_Dict.Add (aKey, aItem);
				} else
					m_Dict.Add (Guid.NewGuid ().ToString (), aItem);
			}
			
			public override JSONNode Remove (string aKey)
			{
				if (!m_Dict.ContainsKey (aKey))
					return null;
				JSONNode tmp = m_Dict [aKey];
				m_Dict.Remove (aKey);
				return tmp;        
			}
			
			public override JSONNode Remove (int aIndex)
			{
				if (aIndex < 0 || aIndex >= m_Dict.Count)
					return null;
				var item = m_Dict.ElementAt (aIndex);
				m_Dict.Remove (item.Key);
				return item.Value;
			}
			
			public override JSONNode Remove (JSONNode aNode)
			{
				try {
					var item = m_Dict.Where (k => k.Value == aNode).First ();
					m_Dict.Remove (item.Key);
					return aNode;
				} catch {
					return null;
				}
			}
			
			public override IEnumerable<JSONNode> Children
			{
				get {
					foreach (KeyValuePair<string,JSONNode> N in m_Dict)
						yield return N.Value;
				}
			}
			
			public IEnumerator GetEnumerator ()
			{
				foreach (KeyValuePair<string, JSONNode> N in m_Dict)
					yield return N;
			}
			
			public override string ToString ()
			{
				string result = "{";
				foreach (KeyValuePair<string, JSONNode> N in m_Dict) {
					if (result.Length > 2)
						result += ", ";
					result += "\"" + Escape (N.Key) + "\":" + N.Value.ToString ();
				}
				result += "}";
				return result;
			}
			
			public override string ToString (string aPrefix)
			{
				string result = "{ ";
				foreach (KeyValuePair<string, JSONNode> N in m_Dict) {
					if (result.Length > 3)
						result += ", ";
					result += "\n" + aPrefix + "   ";
					result += "\"" + Escape (N.Key) + "\" : " + N.Value.ToString (aPrefix + "   ");
				}
				result += "\n" + aPrefix + "}";
				return result;
			}
			
			public override string ToJSON (int prefix)
			{
				string s = new string (' ', (prefix + 1) * 2);
				string ret = "{ ";
				foreach (KeyValuePair<string,JSONNode> n in m_Dict) {
					if (ret.Length > 3)
						ret += ", ";
					ret += "\n" + s;
					ret += string.Format ("\"{0}\": {1}", n.Key, n.Value.ToJSON (prefix + 1));
				}
				ret += "\n" + s + "}";
				return ret;
			}
			
			public override void Serialize (System.IO.BinaryWriter aWriter)
			{
				aWriter.Write ((byte)JSONBinaryTag.Class);
				aWriter.Write (m_Dict.Count);
				foreach (string K in m_Dict.Keys) {
					aWriter.Write (K);
					m_Dict [K].Serialize (aWriter);
				}
			}
		}
		// End of JSONClass
		
		public class JSONData : JSONNode
		{
			private string m_Data;
			
			
			public override string Value
			{
				get { return m_Data; }
				set {
					m_Data = value;
					Tag = JSONBinaryTag.Value;
				}
			}
			
			public JSONData (string aData)
			{
				m_Data = aData;
				Tag = JSONBinaryTag.Value;
			}
			
			public JSONData (float aData)
			{
				AsFloat = aData;
			}
			
			public JSONData (double aData)
			{
				AsDouble = aData;
			}
			
			public JSONData (bool aData)
			{
				AsBool = aData;
			}
			
			public JSONData (int aData)
			{
				AsInt = aData;
			}
			
			public override string ToString ()
			{
				return "\"" + Escape (m_Data) + "\"";
			}
			
			public override string ToString (string aPrefix)
			{
				return "\"" + Escape (m_Data) + "\"";
			}
			
			public override string ToJSON (int prefix)
			{
				switch (Tag) {
				case JSONBinaryTag.DoubleValue:
				case JSONBinaryTag.FloatValue:
				case JSONBinaryTag.IntValue:
					return m_Data;
				case JSONBinaryTag.Value:
					return string.Format ("\"{0}\"", Escape (m_Data));
				default:
					throw new NotSupportedException ("This shouldn't be here: " + Tag.ToString ());
				}
			}
			
			public override void Serialize (System.IO.BinaryWriter aWriter)
			{
				var tmp = new JSONData ("");
				
				tmp.AsInt = AsInt;
				if (tmp.m_Data == this.m_Data) {
					aWriter.Write ((byte)JSONBinaryTag.IntValue);
					aWriter.Write (AsInt);
					return;
				}
				tmp.AsFloat = AsFloat;
				if (tmp.m_Data == this.m_Data) {
					aWriter.Write ((byte)JSONBinaryTag.FloatValue);
					aWriter.Write (AsFloat);
					return;
				}
				tmp.AsDouble = AsDouble;
				if (tmp.m_Data == this.m_Data) {
					aWriter.Write ((byte)JSONBinaryTag.DoubleValue);
					aWriter.Write (AsDouble);
					return;
				}
				
				tmp.AsBool = AsBool;
				if (tmp.m_Data == this.m_Data) {
					aWriter.Write ((byte)JSONBinaryTag.BoolValue);
					aWriter.Write (AsBool);
					return;
				}
				aWriter.Write ((byte)JSONBinaryTag.Value);
				aWriter.Write (m_Data);
			}
		}
		// End of JSONData
		
		internal class JSONLazyCreator : JSONNode
		{
			private JSONNode m_Node = null;
			private string m_Key = null;
			
			public JSONLazyCreator (JSONNode aNode)
			{
				m_Node = aNode;
				m_Key = null;
			}
			
			public JSONLazyCreator (JSONNode aNode, string aKey)
			{
				m_Node = aNode;
				m_Key = aKey;
			}
			
			private void Set (JSONNode aVal)
			{
				if (m_Key == null) {
					m_Node.Add (aVal);
				} else {
					m_Node.Add (m_Key, aVal);
				}
				m_Node = null; // Be GC friendly.
			}
			
			public override JSONNode this [int aIndex]
			{
				get {
					return new JSONLazyCreator (this);
				}
				set {
					var tmp = new JSONArray ();
					tmp.Add (value);
					Set (tmp);
				}
			}
			
			public override JSONNode this [string aKey]
			{
				get {
					return new JSONLazyCreator (this, aKey);
				}
				set {
					var tmp = new JSONClass ();
					tmp.Add (aKey, value);
					Set (tmp);
				}
			}
			
			public override void Add (JSONNode aItem)
			{
				var tmp = new JSONArray ();
				tmp.Add (aItem);
				Set (tmp);
			}
			
			public override void Add (string aKey, JSONNode aItem)
			{
				var tmp = new JSONClass ();
				tmp.Add (aKey, aItem);
				Set (tmp);
			}
			
			public static bool operator == (JSONLazyCreator a, object b)
			{
				if (b == null)
					return true;
				return System.Object.ReferenceEquals (a, b);
			}
			
			public static bool operator != (JSONLazyCreator a, object b)
			{
				return !(a == b);
			}
			
			public override bool Equals (object obj)
			{
				if (obj == null)
					return true;
				return System.Object.ReferenceEquals (this, obj);
			}
			
			public override int GetHashCode ()
			{
				return base.GetHashCode ();
			}
			
			public override string ToString ()
			{
				return "";
			}
			
			public override string ToString (string aPrefix)
			{
				return "";
			}
			
			public override string ToJSON (int prefix)
			{
				return "";
			}
			
			public override int AsInt
			{
				get {
					JSONData tmp = new JSONData (0);
					Set (tmp);
					return 0;
				}
				set {
					JSONData tmp = new JSONData (value);
					Set (tmp);
				}
			}
			
			public override float AsFloat
			{
				get {
					JSONData tmp = new JSONData (0.0f);
					Set (tmp);
					return 0.0f;
				}
				set {
					JSONData tmp = new JSONData (value);
					Set (tmp);
				}
			}
			
			public override double AsDouble
			{
				get {
					JSONData tmp = new JSONData (0.0);
					Set (tmp);
					return 0.0;
				}
				set {
					JSONData tmp = new JSONData (value);
					Set (tmp);
				}
			}
			
			public override bool AsBool
			{
				get {
					JSONData tmp = new JSONData (false);
					Set (tmp);
					return false;
				}
				set {
					JSONData tmp = new JSONData (value);
					Set (tmp);
				}
			}
			
			public override JSONArray AsArray
			{
				get {
					JSONArray tmp = new JSONArray ();
					Set (tmp);
					return tmp;
				}
			}
			
			public override JSONClass AsObject
			{
				get {
					JSONClass tmp = new JSONClass ();
					Set (tmp);
					return tmp;
				}
			}
		}
		// End of JSONLazyCreator
		
		public static class JSON
		{
			public static JSONNode Parse (string aJSON)
			{
				return JSONNode.Parse (aJSON);
			}
		}
	}
}