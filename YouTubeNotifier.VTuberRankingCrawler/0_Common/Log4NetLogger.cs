using log4net;
using log4net.Config;
using log4net.Layout;
using log4net.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.VTuberRankingCrawler.Common
{
    public class Log4NetLogger
    {
        private readonly ILog log;

        public Log4NetLogger(ILog log)
        {
            this.log = log;
        }

        public void Infomation(
            string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            log.Info($"{file} {line} {member} {message}");
        }

        public void Warning(
            string message,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            log.Warn($"{file} {line} {member} {message}");
        }

        public void Error(string message)
        {
            log.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            log.Error(message, exception);
        }


        public static Log4NetLogger Create()
        {
            var log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead($"log4net.config.xml"));

            var assembly = Assembly.GetEntryAssembly();
            var repo = LogManager.CreateRepository(
                assembly,
                typeof(log4net.Repository.Hierarchy.Hierarchy));

            XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            var log = LogManager.GetLogger(assembly, assembly.GetName().Name);
            return new Log4NetLogger(log);
        }
    }

    public class FullStackTraceConverter : PatternConverter
    {
        protected override void Convert(TextWriter writer, object state)
        {
            try
            {
                var stackTrace = new StackTrace(true);

                var value = stackTrace.GetFrames()
                    .Where(x => x.GetMethod().DeclaringType.Assembly != typeof(LogManager).Assembly)
                    .Select(x => $"{x.GetMethod().DeclaringType.FullName}.{x.GetMethod().Name} line:{x.GetFileLineNumber()}\n\t")
                    .JoinString(string.Empty);

                writer.Write(value);
            }
            catch
            {
            }
        }
    }

    public class FullStackTracePatternLayout : PatternLayout
    {
        public FullStackTracePatternLayout()
        {
            AddConverter(new ConverterInfo
            {
                Name = "full_stack_trace",
                Type = typeof(FullStackTraceConverter)
            });
        }
    }
}
