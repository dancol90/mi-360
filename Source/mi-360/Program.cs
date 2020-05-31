using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Serilog;

namespace mi360
{
    class Program
    {
        static void Main(string[] args)
        {
            const string LoggerTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";

            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm", CultureInfo.InvariantCulture);
            var fileName = Path.Combine(Path.GetTempPath(), $"mi-360-{timeStamp}.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: LoggerTemplate)
                .WriteTo.File(path: fileName, outputTemplate: LoggerTemplate)
                .CreateLogger();

            Application.Run(new Mi360Application());
            Log.CloseAndFlush();
        }

    }
}
