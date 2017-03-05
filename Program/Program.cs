using AssettoCorsaSharedMemory;
using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace ACtoSerial
{
    class Program
    {
        static SerialPort _port;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Specify COM port!");
                Console.WriteLine("List of available ports:");
                foreach (var availablePort in SerialPort.GetPortNames())
                {
                    Console.WriteLine("- {0}", availablePort);
                }
                return;
            }

            Console.WriteLine("Trying to open COM port: {0}", args[0]);
            _port = new SerialPort(args[0]);
            _port.BaudRate = 115200;
            _port.DataBits = 8;
            _port.StopBits = StopBits.One;
            _port.Parity = Parity.None;
            _port.Open();

            ManualResetEvent stopme = new ManualResetEvent(false);

            Thread portReader = new Thread((obj) =>
            {
                while (!stopme.WaitOne(1000))
                {
                    if (_port.BytesToRead > 0)
                    {
                        _port.ReadExisting(); // clear rcv buffer
                    }
                }
                Console.WriteLine("Exiting reader");
            });
            portReader.Start();
            try
            {
                RunLoop();
            }
            finally
            {
                stopme.Set();
                _port.Close();
                Thread.Sleep(500);
            }
        }

        private static void WriteToSerial(string format, params object[] args)
        {
            _port.WriteLine(String.Format(format, args));
        }

        private static void RunLoop()
        {
            AssettoCorsa ac = new AssettoCorsa();
            ac.StaticInfoInterval = 1000; // Get StaticInfo updates ever 5 seconds
            ac.StaticInfoUpdated += StaticInfoUpdated; // Add event listener for StaticInfo
            ac.PhysicsInterval = 100;
            ac.PhysicsUpdated += PhysicsUpdated;
            ac.GraphicsInterval = 300;
            ac.GraphicsUpdated += GraphicsUpdated;
            ac.GameStatusChanged += GameStatusChanged;
            ac.Start(); // Connect to shared memory and start interval timers

            Console.WriteLine("Press Q to quit!");
            Console.WriteLine("Serial buffer stats:");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
                Thread.Sleep(100);

                // buffer stats
                Console.Write("{0,5} / {1,5}\r", _port.BytesToWrite, _port.WriteBufferSize);
            }
            ac.Stop();
            Console.WriteLine();
        }

        private static void GameStatusChanged(object sender, GameStatusEventArgs e)
        {
            if (e.GameStatus == AC_STATUS.AC_LIVE)
            {
                // perhaps do something with it?
            }
            else
            {
                // perhaps do something with it?
            }
        }

        private static void GraphicsUpdated(object sender, GraphicsEventArgs e)
        {
            WriteToSerial("Best time: {0}", e.Graphics.BestTime);
        }

        private static void PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            WriteToSerial("Fuel: {0}", e.Physics.Fuel.ToString(CultureInfo.InvariantCulture));
            WriteToSerial("RPM:  {0}", e.Physics.Rpms);
            WriteToSerial("Tyre wear: {0}", string.Join(", ", e.Physics.TyreWear.Select(x => x.ToString(CultureInfo.InvariantCulture))));
        }

        static void StaticInfoUpdated(object sender, StaticInfoEventArgs e)
        {
            WriteToSerial("Max RPM: {0}", e.StaticInfo.MaxRpm);
        }
    }
}