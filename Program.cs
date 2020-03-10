using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
namespace Demo1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            ConcurrentDictionary<string, int> setting = new ConcurrentDictionary<string, int>();
            setting.TryAdd("ReadSpeed", 10);
            setting.TryAdd("ProcessSpeed", 40);
            Console.ForegroundColor = ConsoleColor.Red;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            StreamReader reader = new StreamReader("data.txt");
            Console.WriteLine("Before OuterFunction Call!");
            var readingTask = ReadStream(setting, reader, queue, tokenSource.Token);
            var processTask = ProcessQueue(setting, queue, tokenSource.Token);
            
            Console.WriteLine("OuterFunction Call Over!");
            Console.WriteLine("Came to wait!" + readingTask.Status.ToString());
            while (readingTask.Status == TaskStatus.WaitingForActivation || 
                    processTask.Status == TaskStatus.WaitingForActivation) 
            {
                
                if (Console.KeyAvailable) 
                {
                    var key = Console.ReadKey();
                    if (key.KeyChar.Equals(ConsoleKey.LeftArrow)) 
                    {
                        setting.TryGetValue("ReadSpeed", out int val);
                        setting.TryUpdate("ReadSpeed", val + 10, val);
                    }
                    else
                    if (key.KeyChar.Equals(ConsoleKey.RightArrow))
                    {
                        setting.TryGetValue("ProcessSpeed", out int val);
                        setting.TryUpdate("ProcessSpeed", val + 10, val);
                    }
                }
                await Task.Delay(50);
            }
            sw.Stop();
            Console.WriteLine($"Total time: { sw.ElapsedMilliseconds.ToString() }");
            Console.ResetColor();
        }

        static async Task ReadStream(ConcurrentDictionary<string, int> setting, StreamReader stream, ConcurrentQueue<string> queue, CancellationToken cancellationToken)
        {
            do 
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Reading line!...");
                var line =await stream.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) break;
                queue.Enqueue(line);
                setting.TryGetValue("ReadSpeed", out int val);
                await Task.Delay(val);
            } while(true);
        }

        static async Task ProcessQueue(ConcurrentDictionary<string, int> setting, ConcurrentQueue<string> queue, CancellationToken cancellationToken)
        {
            while(queue.TryDequeue(out string line))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("                                          Process Queue!");
                setting.TryGetValue("ProcessSpeed", out int val);
                await Task.Delay(val);
            }
        }


        static async Task OuterFunction(CancellationToken cancellationToken)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File Loading Started!");
            var result = ReadLines("data.txt");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File Loading Complete!");
            Console.WriteLine("Starting to print lines in the screen!");
            await result.ContinueWith(async t  =>
            {
                foreach(var line in t.Result)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    await PrintLine(line);
                }
            });
            Console.WriteLine("Print lines in the screen is over!");
        }
        static async Task<string[]> ReadLines(string fileName)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"File Reading begins!");
            StreamReader sr = File.OpenText(fileName);
            List<string> list = new List<string>(); 
            do
            {
                await Task.Yield();
                var line =await sr.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) break;
                list.Add(line);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(line);
            }while(true);
            Console.WriteLine($"File Reading is over!");
            return list.ToArray();
        }

        static async Task PrintLine(string line)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(line);
            await Task.Delay(100);
        }
    }
}
