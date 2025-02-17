﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkMIDI_test {
	class Program {

		static TcpClient tcpClient;
		static NetworkStream outputStream;

		static TcpListener tcpListener;
		static NetworkStream inputStream;
		static TcpClient consoleClient;

		static bool running = false;

		static int Main(string[] args) {
			Console.Out.WriteLine("Starting...");
			int result = StartTCP();
			if (result != 0) { Console.WriteLine(result); return 30; }
			running = true;
			Task listenerTask = Task.Run(() => TCPListener());
			Console.WriteLine("Started network MIDI!");


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
			try {
				tcpClient = new TcpClient("192.168.1.128", 12300);
			} catch (SocketException) {
				return 1;
			}
			tcpClient.NoDelay = true;
			outputStream = tcpClient.GetStream();
			outputStream.WriteTimeout = 1000;
			outputStream.ReadTimeout = 1000;
			byte[] initData1 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x03 };
			byte[] initData2 = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x23, 0x00, 0x00, 0x00, 0x19, 0xe7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			outputStream.Write(initData1, 0, initData1.Length);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("Sent: {0}", BitConverter.ToString(initData1));

			byte[] receiveBuffer1 = new byte[tcpClient.ReceiveBufferSize];
			try {
				int received1 = outputStream.Read(receiveBuffer1, 0, receiveBuffer1.Length);
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Got:  {0}\n", BitConverter.ToString(receiveBuffer1, 0, received1));
			} catch (IOException e) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.Message);
				return 1;
			}

			outputStream.Write(initData2, 0, initData1.Length);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("Sent: {0}", BitConverter.ToString(initData2), ConsoleColor.Cyan);

			byte[] receiveBuffer2 = new byte[tcpClient.ReceiveBufferSize];
			try {
				int received2 = outputStream.Read(receiveBuffer2, 0, receiveBuffer2.Length);
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
			IPAddress anAddress = IPAddress.Any;
			tcpListener = new TcpListener(anAddress, 12300);
			tcpListener.Start();

			while (running) {
				try {
					if (consoleClient == null || !consoleClient.Connected) {
						consoleClient = tcpListener.AcceptTcpClient();
						consoleClient.NoDelay = true;
						inputStream = consoleClient.GetStream();
					}

					byte[] buffer = new byte[consoleClient.ReceiveBufferSize];
					int bytesRead = inputStream.Read(buffer, 0, consoleClient.ReceiveBufferSize);
					string dataReceived = BitConverter.ToString(buffer, 0, bytesRead);

					byte[] ackMessage = { 0x00, 0x00, 0x00, 0x10, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };
					inputStream.Write(ackMessage, 0, ackMessage.Length);

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

		static void CloseTCP() {
			inputStream.Close();
			consoleClient.Close();
		}

		static void StopTCP() {
			if (tcpClient == null || !tcpClient.Connected || outputStream == null || !outputStream.CanWrite) {
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
			byte[] closeDataZ = new byte[] { 0x00, 0x00, 0x00, 0x10, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff };

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
				outputStream.Write(closeDataZ);
			} catch (IOException) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Out.WriteLine("Couldn't close properly...");
			}
			outputStream.Close();
			tcpClient.Close();
		}

		static List<int> GetIntsFromBytes(byte[] bytes) {
			List<int> output = new List<int>();
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
