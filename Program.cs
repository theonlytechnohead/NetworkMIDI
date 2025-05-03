using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkMIDI {
	class Program {

		static TcpClient tcpClient;
		static NetworkStream stream;
		
		static bool running = false;

		static int Main(string[] args) {
			int result = StartTCP();
			if (result != 0) { Console.WriteLine(result); return 30; }
			running = true;
			Task listenerTask = Task.Run(() => TCPListener());

			if (tcpClient.Connected)
				Console.WriteLine("Connected!");
			else
				Console.WriteLine("Disconnected!");

			Task heartbeat = Task.Run(() => Heartbeat());

			Console.ReadKey();
			running = false;
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
			return 0;
            
		}

		static void TCPListener() {
            while (running) {
				try {
					if (tcpClient == null || !tcpClient.Connected) {
						Console.WriteLine("Disconnected!");
						break;
					}

					byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
					int bytesRead = stream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
					string dataReceived = BitConverter.ToString(buffer, 0, bytesRead);

					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"Data received: {dataReceived}");
					Console.WriteLine($"MIDI data:     {GetStringFromMIDIBytes(buffer)}");
					Console.ResetColor();
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
            // F0 43 10 3E 12 7F F7 -> CL?
			// F0 43 10 3E 19 7F F7 -> QL (5?)
            byte[] beat = [0xf0, 0x43, 0x10, 0x3e, 0x19, 0x7f, 0xf7];
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
