using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkMIDI {
	class Program {

		static TcpClient tcpClient;
		static NetworkStream stream;

		//static TcpListener tcpListener;
		//static NetworkStream inputStream;
		//static TcpClient consoleClient;

		static bool running = false;

		static int Main(string[] args) {
			int result = StartTCP();
			if (result != 0) { Console.WriteLine(result); return 30; }
			running = true;
			Task listenerTask = Task.Run(() => TCPListener());
			Console.WriteLine("Started network MIDI!");

			if (tcpClient.Connected)
				Console.WriteLine("Connected!");
			else
				Console.WriteLine("Disconnected!");

			Task heartbeat = Task.Run(() => Heartbeat());

			Console.ReadKey();
			running = false;
			Console.ResetColor();
			StopTCP();
			Console.ResetColor();
			CloseTCP();
			Console.ResetColor();
			Console.WriteLine("Stopped network MIDI");
			return 0;
		}

		static int StartTCP() {
            Console.Out.WriteLine("Connecting to 192.168.1.128...");
			try {
				tcpClient = new TcpClient("192.168.1.128", 50000);
			} catch (SocketException) {
				return 1;
			} catch (TimeoutException) {
				return 1;
			}
			if (!tcpClient.Connected) return 1;

			tcpClient.NoDelay = true;
			stream = tcpClient.GetStream();
			stream.WriteTimeout = 3000;
			stream.ReadTimeout = 3000;
            byte[] initData1 = [0x00, 0x00, 0x00, 0x10, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x03];
			byte[] initData2 = [0x00, 0x00, 0x00, 0x10, 0x23, 0x00, 0x00, 0x00, 0x19, 0xe7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

			stream.Write(initData1, 0, initData1.Length);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("Sent: {0}", BitConverter.ToString(initData1));

			byte[] receiveBuffer1 = new byte[tcpClient.ReceiveBufferSize];
			try {
				int received1 = stream.Read(receiveBuffer1, 0, receiveBuffer1.Length);
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Got:  {0}\n", BitConverter.ToString(receiveBuffer1, 0, received1));
			} catch (IOException e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				return 1;
			}

			stream.Write(initData2, 0, initData2.Length);
			Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Sent: {0}", BitConverter.ToString(initData2), ConsoleColor.Cyan);

			byte[] receiveBuffer2 = new byte[tcpClient.ReceiveBufferSize];
			try {
				int received2 = stream.Read(receiveBuffer2, 0, receiveBuffer2.Length);
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Got:  {0}\n", BitConverter.ToString(receiveBuffer2, 0, received2));
			} catch (IOException e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Out.WriteLine(e.Message);
				return 1;
			}
			Console.ResetColor();
			return 0;
		}

		static void TCPListener() {
            //IPAddress anAddress = IPAddress.Any;
            //tcpListener = new TcpListener(anAddress, 50000);
            //tcpListener.Start();

            while (running) {
				try {
					if (tcpClient == null || !tcpClient.Connected) {
						Console.WriteLine("Disconnected!");
						break;
						//consoleClient = tcpListener.AcceptTcpClient();
						//consoleClient = tcpClient;
						//consoleClient.NoDelay = true;
						//inputStream = consoleClient.GetStream();
					}

					byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
					int bytesRead = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
					string dataReceived = BitConverter.ToString(buffer, 0, bytesRead);

					byte[] ackMessage = { 0x00, 0x00, 0x00, 0x10, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
					stream.Write(ackMessage, 0, ackMessage.Length);

					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"Data received: {dataReceived}");
					Console.WriteLine($"MIDI data:     {GetStringFromMIDIBytes(buffer)}");
					Console.ResetColor();


					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine($"Sending ACK:   {BitConverter.ToString(ackMessage)}");
				} catch (ObjectDisposedException) {
					return;
				} catch (IOException e) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Out.WriteLine(e.Message);
				}
			}
		}

		async static void Heartbeat() {
            // Heartbeat every 1s
            // F0 43 10 3E 12 7F F7
            byte[] beat = [0x00, 0x00, 0x00, 0x1b, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x07, 0xf0, 0x43, 0x10, 0x3e, 0x12, 0x7f, 0xf7];
            for (;;) {
				if (!running) break;
				await Task.Delay(1000);
                stream.Write(beat, 0, beat.Length);
			}

        }

        static void CloseTCP() {
			stream.Close();
			tcpClient.Close();
		}

		static void StopTCP() {
			if (tcpClient == null || !tcpClient.Connected || stream == null || !stream.CanWrite) {
				return;
			}

			//byte[] closeData0 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb0, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData1 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb1, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData2 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb2, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData3 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb3, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData4 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb4, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData5 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb5, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData6 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb6, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData7 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb7, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData8 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb8, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeData9 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xb9, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataA = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xba, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataB = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xbb, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataC = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xbc, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataD = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xbd, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataE = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xbe, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			//byte[] closeDataF = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x13, 0x00, 0x00, 0x00, 0xbf, 0x78, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
			byte[] closeDataZ = [0x00, 0x00, 0x00, 0x10, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff];

			try {
				//outputStream.Write(closeData0);
				//outputStream.Write(closeData1);
				//outputStream.Write(closeData2);
				//outputStream.Write(closeData3);
				//outputStream.Write(closeData4);
				//outputStream.Write(closeData5);
				//outputStream.Write(closeData6);
				//outputStream.Write(closeData7);
				//outputStream.Write(closeData8);
				//outputStream.Write(closeData9);
				//outputStream.Write(closeDataA);
				//outputStream.Write(closeDataB);
				//outputStream.Write(closeDataC);
				//outputStream.Write(closeDataD);
				//outputStream.Write(closeDataE);
				//outputStream.Write(closeDataF);
				stream.Write(closeDataZ);
			} catch (IOException) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Out.WriteLine("Couldn't close properly...");
			}
			stream.Close();
			tcpClient.Close();
		}

		static List<int> GetIntsFromBytes(byte[] bytes) {
			List<int> output = [];
			for (int i = 0; i < bytes.Length - 4; i += 4) {
				if (bytes[i] == 0xFF) { break; }
				byte[] number = new byte[4];
				Array.Copy(bytes, i, number, 0, 4);
				Array.Reverse(number);
				output.Add(BitConverter.ToInt32(number));
			}
			return output;
		}

		static string GetStringFromMIDIBytes(byte[] bytes) {
			List<int> controlData = GetIntsFromBytes(bytes);
			int length = controlData[2];
			for (int i = 0; i < bytes.Length; i += 4) {
				if (bytes[i] == 0xFF) {
					return BitConverter.ToString(bytes, i + 8, length);
				}
			}
			return "";
		}
	}
}
