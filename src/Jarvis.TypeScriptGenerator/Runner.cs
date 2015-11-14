using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http.Controllers;
using Jarvis.TypeScriptGenerator.Builders;
using Newtonsoft.Json;

namespace Jarvis.TypeScriptGenerator
{
    internal class Runner
    {
        private const string TsgeneratorJson = "tsgenerator.json";
        private static IDictionary<string, Assembly> _assembliesCache = new Dictionary<string, Assembly>();
        private TypeScriptGeneratorOptions _options;

        public Runner()
        {
            var workingFolder = AppDomain.CurrentDomain.BaseDirectory;
            var tsGeneratorJson = Path.Combine(workingFolder, TsgeneratorJson);

            var text = File.ReadAllText(tsGeneratorJson);
            _options = JsonConvert.DeserializeObject<TypeScriptGeneratorOptions>(text);
        }

        private Type[] LoadControllers(string pathToAssembly)
        {
            var assembly = LoadAssemblyFromPath(pathToAssembly);
            var allClasses = assembly.GetTypes().Where(x => !x.IsAbstract && x.IsClass).ToArray();

            // check fully qualified interface as a string to avoid webapi version mismatch
            var controllers = allClasses.Where(x =>
                x.GetInterfaces().Any(y => y.FullName == typeof(IHttpController).FullName)
                ).ToArray();

            return controllers;
        }

        private void DumpError(Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        public void Run()
        {
            var folder = Path.GetDirectoryName(_options.Assemblies.First());
            var dlls = Directory.GetFiles(folder, "*.dll");

            foreach (var dll in dlls)
            {
                LoadAssemblyFromPath(dll);
            }

            AppDomain currentDomain = AppDomain.CurrentDomain;
            ResolveEventHandler handler = (sender, args) => LoadAssembly(
                new AssemblyName(args.Name), _options.Assemblies.First()
                );

            currentDomain.ReflectionOnlyAssemblyResolve += handler;
            foreach (var pathToAssembly in _options.Assemblies)
            {
                ProcessAssembly(pathToAssembly, _options);
            }
            currentDomain.ReflectionOnlyAssemblyResolve -= handler;        
        }

        private Assembly LoadAssemblyFromPath(string pathToDll)
        {
            if (_assembliesCache.ContainsKey(pathToDll))
                return _assembliesCache[pathToDll];


            if (!File.Exists(pathToDll))
            {
                Console.WriteLine("...{0} not found", pathToDll);

                _assembliesCache.Add(pathToDll, null);
                return null;
            }

            try
            {
                Console.Write("...loading {0}", Path.GetFileName(pathToDll));
                var raw = File.ReadAllBytes(pathToDll);
                var loaded = Assembly.ReflectionOnlyLoad(raw);
                _assembliesCache.Add(pathToDll, loaded);
                Console.WriteLine("=> {0}", loaded.GetName());
                return loaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: {0}\n", ex.Message);
            }
            return null;
        }
        private bool ProcessAssembly(string pathToAssembly, TypeScriptGeneratorOptions options)
        {
            if (!File.Exists(pathToAssembly))
            {
                Console.WriteLine("File {0} not found", pathToAssembly);
                return false;
            }

            Console.WriteLine("Processing {0}", pathToAssembly);

            try
            {
                var controllers = LoadControllers(pathToAssembly);

                if (!controllers.Any())
                {
                    Console.WriteLine("No controllers found");
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

        private Assembly LoadAssembly(AssemblyName assemblyName, string pathToAssembly)
        {
            var baseFolder = Path.GetDirectoryName(pathToAssembly);
            var fname = Path.Combine(baseFolder, new AssemblyName(assemblyName.Name).Name + ".dll");

            return LoadAssemblyFromPath(fname);
        }
    }
}