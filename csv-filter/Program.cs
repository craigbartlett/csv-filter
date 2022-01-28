using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

namespace csvFilter
{
    class Program
    {
        const int BufferSize = 4096;

        public static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.Name = "csv-filter";
            app.Command("parse",
                (listen2) =>
                {
                    var queryOpt = listen2.Option("--query <column=value>", "key/value pairs to filter with", CommandOptionType.MultipleValue);
                    var fileOpt = listen2.Option("--file <value>", "Absolute path to csv file", CommandOptionType.SingleValue);

                    listen2.HelpOption("-? | -h | --help");
                    listen2.OnExecute(() =>
                    {
                        Parse(fileOpt.Value(), queryOpt.Value());
                        return 0;
                    });
                });
            app.HelpOption("-? | -h | --help");
            app.Execute(args);
        }

        private static void Parse(string filePath, string query)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("[{0}] was not found.", filePath);
                return;
            }

            Console.WriteLine("Query: {0}", query);
            Console.WriteLine("Output:\n");

            List<Filter> filters = BuildFilters(ReadHeader(filePath), query);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, Encoding.UTF8, true, BufferSize))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    bool flag = true;
                    foreach (Filter filter in filters)
                    {
                        if (lineParts[filter.Column] != filter.Value)
                            flag = false;
                    }

                    if (flag)
                        Console.WriteLine(line);
                }
            }
        }

        private static List<Filter> BuildFilters(Dictionary<string, int> headers, string query)
        {
            List<Filter> filters = new List<Filter>();
            
            string[] pairs = query.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            foreach(string pair in pairs)
            {
                string[] pairLocal = pair.Split("=", StringSplitOptions.RemoveEmptyEntries);
                filters.Add(new Filter(pairLocal[0], headers[pairLocal[0]], pairLocal[1]));
            }

            return filters;
        }

        /// <summary>
        /// provide header to column ID mapping -> allows user to supply any headers to any csv
        /// </summary>
        private static Dictionary<string, int> ReadHeader(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sr = new StreamReader(fs, Encoding.UTF8, true, BufferSize))
            {
                string line = sr.ReadLine();
                string[] lineParts = line.Split(",", StringSplitOptions.RemoveEmptyEntries);

                Dictionary<string, int> header = new Dictionary<string, int>();

                for (int i = 0; i < lineParts.Length; i++)
                {
                    header.Add(lineParts[i], i);
                }

                return header;
            }
        }
    }

    public class Filter
    {
        public int Column { get; set; } = -1;
        public string ColumnHeader { get; set; }
        public string Value { get; set; }

        public Filter(string columnHeader, int column, string value)
        {
            ColumnHeader = columnHeader;
            Column = column;
            Value = value;
        }
    }
}
