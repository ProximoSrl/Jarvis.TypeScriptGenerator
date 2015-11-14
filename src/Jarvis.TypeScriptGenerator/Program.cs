using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Jarvis.TypeScriptGenerator
{
    class Program
    {
        private const string TsgeneratorJson = "tsgenerator.json";
        private static string _workingFolder;
        private static string _tsGeneratorJson;
        private static bool _optionInit;
        private static bool _optionRun;
        
        static int Main(string[] args)
        {
            Setup();

            if (ParseArgs(args) == 0)
            {
                Banner();
                return 0;
            }

            if (_optionInit)
            {
                return Init();
            }

            if (_optionRun)
            {
                return Run();
            }

            return 0;
        }

        private static int Run()
        {
            if (!ExistsTsGeneratorJson())
            {
                Console.WriteLine("File {0} is missing", TsgeneratorJson);
                return 0;
            }

            try
            {
                // preload
                var tmpDomain = AppDomain.CreateDomain("temp");

                tmpDomain.DoCallBack(() =>
                {
                    var runner = new Runner();
                    runner.Run();
                });

                AppDomain.Unload(tmpDomain);
            }
            catch (Exception ex)
            {
                DumpError(ex);
                return -2;
            }

            return 1;
        }


        private static int ParseArgs(string[] args)
        {
            int parsed = 0;
            foreach (var arg in args)
            {
                switch (Normalize(arg))
                {
                    case "init":
                        _optionInit = true;
                        parsed++;
                        break;

                    case "run":
                        _optionRun = true;
                        parsed++;
                        break;
                }
            }

            return parsed;
        }

        private static void Setup()
        {
            _workingFolder = AppDomain.CurrentDomain.BaseDirectory;
            _tsGeneratorJson = Path.Combine(_workingFolder, TsgeneratorJson);
            _optionInit = false;
        }

        private static int Init()
        {
            if (ExistsTsGeneratorJson())
            {
                Console.WriteLine("File {0} already exists", _tsGeneratorJson);
                return 0;
            }

            Console.WriteLine("Writing {0}", _tsGeneratorJson);

            var options = new TypeScriptGeneratorOptions();
            try
            {
                options.Init(_workingFolder);
                options.Save(_tsGeneratorJson);
            }
            catch (Exception ex)
            {
                DumpError(ex);
                return -1;
            }
            return 1;
        }

        private static void DumpError(Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        private static bool ExistsTsGeneratorJson()
        {
            return File.Exists(_tsGeneratorJson);
        }

        private static string Normalize(string s)
        {
            return s.ToLowerInvariant();
        }

        private static void Banner()
        {
            if (!Environment.UserInteractive)
                return;

            Console.WriteLine("================================================");
            Console.WriteLine(" TypeScriptGenerator " + Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("  init         create a tsgenerator.json if not exists");
            Console.WriteLine("  run          run conversion with tsgenerator.json settings");
            Console.WriteLine();
        }
    }
}
