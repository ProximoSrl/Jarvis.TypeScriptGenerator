using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http.Controllers;
using Jarvis.TypeScriptGenerator.Builders;
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
                var text = File.ReadAllText(_tsGeneratorJson);
                var options = JsonConvert.DeserializeObject<TypeScriptGeneratorOptions>(text);

                foreach (var pathToAssembly in options.Assemblies)
                {
                    ProcessAssembly(pathToAssembly, options);
                }
            }
            catch (Exception ex)
            {
                DumpError(ex);
                return -2;
            }

            return 1;
        }

        private static bool ProcessAssembly(string pathToAssembly, TypeScriptGeneratorOptions options)
        {
            if (!File.Exists(pathToAssembly))
            {
                Console.WriteLine("File {0} not found", pathToAssembly);
                return false;
            }

            Console.WriteLine("Processing {0}", pathToAssembly);

            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                var baseFolder = Path.GetDirectoryName(pathToAssembly);
                ResolveEventHandler handler = (sender, args) =>
                {
                    Console.WriteLine("...loading {0}", args.Name);
                    var fname = Path.Combine(baseFolder, new AssemblyName(args.Name).Name + ".dll");
                    if (!File.Exists(fname))
                        return null;

                    return Assembly.LoadFile(fname);
                };

                currentDomain.AssemblyResolve += handler;

                
                var controllers = LoadControllers(pathToAssembly);

                if (!controllers.Any())
                {
                    Console.WriteLine("No controllers found");
                    currentDomain.AssemblyResolve -= handler;

                    return false;
                }

                foreach (var controller in controllers)
                {
                    Console.WriteLine("...analyzing {0}", controller.FullName);
                    try
                    {
                        var builder = new TypeScriptBuilder(options.DestFolder, options.NgModule);
                        foreach (var reference in options.References)
                        {
                            builder.AddReference(reference);
                        }

                        var pathToTs = builder.GenerateClientApi(controller, options.Namespace, options.ApiRoot);
                        Console.WriteLine("...written {0}", pathToTs);
                    }
                    catch (Exception ex)
                    {
                        DumpError(ex);
                    }
                }

                currentDomain.AssemblyResolve -= handler;
            }
            catch (Exception ex)
            {
                if (ex is ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                    foreach (var le in loaderExceptions)
                    {
                        DumpError(le);
                    }
                }
                else
                {
                    DumpError(ex);
                }
                return false;
            }

            return true;
        }

        private static Type[] LoadControllers(string pathToAssembly)
        {

            var assembly = Assembly.LoadFile(pathToAssembly);
            var allClasses = assembly.GetTypes().Where(x =>!x.IsAbstract &&x.IsClass).ToArray();
            
            // check fully qualified interface as a string to avoid webapi version mismatch
            var controllers =  allClasses.Where(x=>
                x.GetInterfaces().Any(y=>y.FullName == typeof(IHttpController).FullName)
            ).ToArray();

            return controllers;
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
            Console.WriteLine();
        }
    }
}
