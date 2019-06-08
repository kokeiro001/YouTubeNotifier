using System;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.ConsoleApp
{
    class ConsoleLogger : IMyLogger
    {
        public void Infomation(string message) => Console.WriteLine($"Infomation {message}");

        public void Warning(string message) => Console.WriteLine($"Warning {message}");

        public void Error(string message) => Console.WriteLine($"Error {message}");
    }
}
