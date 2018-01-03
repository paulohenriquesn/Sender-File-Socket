using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SFILESocket
{
    enum scenes
    {
        MENU = 0,
        CLIENT_METHOD = 1,
        SERVER_METHOD = 2
    }

    class Program
    {
        const byte ok = 1;
        static string ipAddress;
        static string FileArchive;
        static int Port;
        static string[] readCfg = File.ReadAllLines("config.cfg");
        static scenes selectedOption = scenes.MENU;
        static int SelectedOptionRead;
        static Encoding UTF8 = new UTF8Encoding(false);
        public static Image byteArrayToImage(byte[] byteArrayIn)
        {

            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
            Image img = (Image)converter.ConvertFrom(byteArrayIn);

            return img;
        }
        public static Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destImage = new Bitmap(width, height);
            using (var g = Graphics.FromImage(destImage))
            {
                g.DrawImage(image, 0, 0, width, height);
            }
            return destImage;
        }
        static int ReadInt32(Stream stream)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }
        static long ReadInt64(Stream stream)
        {
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt64(buffer, 0);
        }
        static MemoryStream ReadStream(Stream stream, long size, int bufferSize)
        {
            long readed = 0;
            if (bufferSize > size)
                bufferSize = (int)size;
            byte[] buffer = new byte[bufferSize];
            MemoryStream memory = new MemoryStream();
           
            int read = stream.Read(buffer, 0, buffer.Length);
            while (read > 0 && readed < size)
            {
                readed += read;
                memory.Write(buffer, 0, read);
                if (readed + buffer.Length > size)
                {
                    long remaining = size - readed;
                    if (remaining <= 0)
                        break;
                    read = stream.Read(buffer, 0, (int)remaining);
                }

                else
                    read = stream.Read(buffer, 0, buffer.Length);
            }
            memory.Seek(0, SeekOrigin.Begin);
            return memory;

        }
        static void WriteFile(Stream stream, string filePath, int bufferSize)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create))
            {
                byte[] buffer = new byte[bufferSize];
                int read = stream.Read(buffer, 0, buffer.Length);
                while (read > 0)
                {
                    file.Write(buffer, 0, read);
                    file.Flush();
                    read = stream.Read(buffer, 0, buffer.Length);
                }
            }

        }
        static void Main(string[] args)
        {
            ipAddress = readCfg[0];
            Port = int.Parse(readCfg[1]);
            FileArchive = readCfg[2];

            if (selectedOption == scenes.MENU)
            {
                Console.WriteLine("1 - Client\n2 - Server\n3 - Exit");
                SelectedOptionRead = int.Parse(Console.ReadLine());
            }
            if (SelectedOptionRead <= 0 || SelectedOptionRead > 2)
            {
                Environment.Exit(0);
            }
            else if (SelectedOptionRead == 1)
            {
                Console.Clear();
                using (var client = new TcpClient())
                {
                    using (FileStream file = new FileStream(FileArchive, FileMode.Open))
                    {
                        var fileName = UTF8.GetBytes(Path.GetFileName(FileArchive));
                        var size = BitConverter.GetBytes(file.Length);
                        var fileNameLen = BitConverter.GetBytes(fileName.Length);
                        client.Connect(ipAddress, Port);
                        using (var stream = client.GetStream())
                        {



                            stream.Write(fileNameLen, 0, 4);
                            stream.Write(fileName, 0, fileName.Length);
                            stream.Write(size, 0, 8);
                            file.CopyTo(stream);
                            stream.Flush();
                            if ((byte)stream.ReadByte() == ok)
                                Console.WriteLine("Sent.");
                            else
                                Console.WriteLine("ERROR.");
                        }
                    }

                }
            }
            else if (SelectedOptionRead == 2)
            {
                Console.Clear();
                while (true)
                {
                    var listener = new TcpListener(System.Net.IPAddress.Parse(ipAddress), Port);

                    listener.Start();
                    Console.WriteLine("Server Initialized");
                    for (; ; )
                    {
                        using (var remote = listener.AcceptTcpClient())
                        {
                            try
                            {
                                using (var stream = remote.GetStream())
                                {


                                    long size = (long)ReadInt32(stream);
                                    string name = UTF8.GetString(ReadStream(stream, (int)size, 251).ToArray());
                                    Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"File '{name}' Accepted"); Console.ResetColor();
                                    Console.Beep();

                                    size = ReadInt64(stream);
                                    using (MemoryStream memory = ReadStream(stream, size, 251))
                                    {
                                        WriteFile(memory, $"uploads\\{Path.GetFileNameWithoutExtension(name)}.upload{Path.GetExtension(name)}", 4096);

                                    }
                                }
                            }

                            catch (Exception ex) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"ERROR: {ex.ToString()}"); Console.ResetColor(); }

                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"File Saved"); Console.ResetColor();
                            Console.Beep();
                        }
                    }
                }
            }
        }
    }
}




