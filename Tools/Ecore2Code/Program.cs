﻿using CommandLine;
using CommandLine.Text;
using NMF.Interop.Ecore;
using NMF.Interop.Ecore.Transformations;
using NMF.Transformations;
using NMF.Utilities;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NMF.Transformations.Core;
using NMF.Models.Meta;
using System.Diagnostics;
using NMF.Transformations.Parallel;
using NMF.Models.Repository;
using NMF.Models.Repository.Serialization;
using NMF.Models;

using PythonCodeGenerator.CodeDom;


namespace Ecore2Code
{
    class Options
    {
        public Options()
        {
            OverallNamespace = "GeneratedCode";
        }

        [Option('n', "namespace", HelpText="The root namespace")]
        public string OverallNamespace { get; set; }

        [Option('l', "language", DefaultValue = SupportedLanguage.CS, HelpText = "The language in which the code should be generated")]
        public SupportedLanguage Language { get; set; }

        [Option('f', "folder", HelpText = "Determines whether the code for classes should be separated to multiple files")]
        public bool UseFolders { get; set; }

        [Option('p', "parallel", HelpText = "If specified, runs the code generator in parallel mode (in incubation)")]
        public bool Parallel { get; set; }

        [Option('x', "force", HelpText = "If specified, the code is generated regardless of existing code")]
        public bool Force { get; set; }

        [Option('g', "operations", HelpText = "If specified, the code generator generates stubs for operations")]
        public bool Operations { get; set; }

        [Option('u', "model-uri", HelpText ="If specified, overrides the uri of the base package.")]
        public string Uri { get; set; }

        [ValueList(typeof(List<string>))]
        public IList<string> InputFiles { get; set; }

        [Option('o', "output", Required=true, HelpText="The output file/folder in which the code should be generated")]
        public string OutputFile { get; set; }

        [Option('m', "metamodel", Required=false, HelpText="Specify this argument if you want to serialize the NMeta metamodel possibly generated from Ecore")]
        public string NMeta { get; set; }

        [OptionList('r', "resolve", Required=false, HelpText="A list of namespace remappings in the syntax URI=file", Separator=';')]
        public List<string> NamespaceMappings { get; set; }

        public string GetHelp()
        {
            return HelpText.AutoBuild(this);
        }
    }

    public enum SupportedLanguage
    {
        CS,
        VB,
        CPP,
        JS,
        PY
    }

    class Program
    {
        static void Main(string[] args)
        {            
            Options options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
#if DEBUG
                GenerateCode(options);
#else
                try
                {
                    GenerateCode(options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#endif
            }
            else
            {
                Console.WriteLine("You are using me wrongly!");
                Console.WriteLine("Usage: Ecore2Code [Options] -o [Output File or directory] [Inputfiles]");
                Console.WriteLine("Input files may either be in NMeta or Ecore format.");
                Console.WriteLine("Example: Ecore2Code -f -n NMF.Models -o Meta NMeta.nmf");
                Console.WriteLine(options.GetHelp());
            }
            Console.Read();
        }

        private static void GenerateCode(Options options)
        {
            var packageTransform = new NMF.Models.Meta.Meta2ClassesTransformation();
            var stopWatch = new Stopwatch();

            packageTransform.ForceGeneration = options.Force;
            packageTransform.CreateOperations = options.Operations;
            packageTransform.DefaultNamespace = options.OverallNamespace;

            Dictionary<Uri, string> mappings = null;
            if (options.NamespaceMappings != null && options.NamespaceMappings.Count > 0)
            {
                mappings = new Dictionary<Uri, string>();
                foreach (var mapping in options.NamespaceMappings)
                {
                    if (string.IsNullOrEmpty(mapping)) continue;
                    var lastIdx = mapping.LastIndexOf('=');
                    if (lastIdx == -1)
                    {
                        Console.WriteLine("Namespace mapping {0} is missing required separator =", mapping);
                        continue;
                    }
                    Uri uri;
                    if (!Uri.TryCreate(mapping.Substring(0, lastIdx), UriKind.Absolute, out uri))
                    {
                        uri = new Uri(mapping.Substring(0, lastIdx), UriKind.Relative);
                    }
                    mappings.Add(uri, mapping.Substring(lastIdx + 1));
                }
            }

            var metaPackage = LoadPackageFromFiles(options.InputFiles, options.OverallNamespace, mappings);
            if (options.Uri != null)
            {
                Uri uri;
                if (Uri.TryCreate(options.Uri, UriKind.Absolute, out uri))
                {
                    metaPackage.Uri = uri;
                }
                else
                {
                    Console.Error.WriteLine("The provided string {0} could not be parsed as an absolute URI.", options.Uri);
                }
            }
            if (metaPackage.Uri == null)
            {
                Console.Error.WriteLine("Warning: There is no base Uri for the provided metamodels. Some features of the generated code will be disabled.");
            }

            var model = metaPackage.Model;
            if (model == null)
            {
                model = new Model();
                model.RootElements.Add(metaPackage);
            }
            model.ModelUri = metaPackage.Uri;
            if (options.NMeta != null)
            {
                using (var fs = File.Create(options.NMeta))
                {
                    MetaRepository.Instance.Serializer.Serialize(model, fs);
                }
            }

            stopWatch.Start();
            var compileUnit = TransformationEngine.Transform<INamespace, CodeCompileUnit>(metaPackage, 
                options.Parallel
                   ? (ITransformationEngineContext)new ParallelTransformationContext(packageTransform)
                   : new TransformationContext(packageTransform));
            stopWatch.Stop();

            Console.WriteLine("Operation took {0}ms", stopWatch.Elapsed.TotalMilliseconds);

            CodeDomProvider generator = null;

            switch (options.Language)
            {
                case SupportedLanguage.CS:
                    generator = new Microsoft.CSharp.CSharpCodeProvider();
                    break;
                case SupportedLanguage.VB:
                    generator = new Microsoft.VisualBasic.VBCodeProvider();
                    break;
                case SupportedLanguage.CPP:
                    generator = new Microsoft.VisualC.CppCodeProvider();
                    break;
                case SupportedLanguage.JS:
                    generator = new Microsoft.JScript.JScriptCodeProvider();
                    break;
                case SupportedLanguage.PY:
                    Console.WriteLine("Python woohoo!");
                    generator = new PythonProvider();                                        
                    break;
                default:
                    Console.WriteLine("Unknown language detected. Falling back to default C#");
                    generator = new Microsoft.CSharp.CSharpCodeProvider();
                    break;
            }

            var genOptions = new System.CodeDom.Compiler.CodeGeneratorOptions()
                {
                    BlankLinesBetweenMembers = true,
                    VerbatimOrder = false,
                    ElseOnClosing = false,
                    BracingStyle = "C",
                    IndentString = "    "
                };
            if (options.UseFolders)
            {
                foreach (var file in MetaFacade.SplitCompileUnit(compileUnit))
                {
                    var fileInfo = new FileInfo(Path.Combine(options.OutputFile, file.Key) + "." + generator.FileExtension);
                    CheckDirectoryExists(fileInfo.Directory);
                    using (var fs = fileInfo.Create())
                    {
                        using (var sw = new StreamWriter(fs))
                        {
                            generator.GenerateCodeFromCompileUnit(file.Value, sw, genOptions);
                        }
                    }
                }
            }
            else
            {
                using (var sw = new StreamWriter(options.OutputFile))
                {
                    generator.GenerateCodeFromCompileUnit(compileUnit, sw, genOptions);
                }
            }

            Console.WriteLine("Code generated successfully!");
        }

        private static void CheckDirectoryExists(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Exists)
            {
                CheckDirectoryExists(directoryInfo.Parent);
                directoryInfo.Create();
            }
        }

        public static INamespace LoadPackageFromFiles(IList<string> files, string overallName, IDictionary<Uri, string> resolveMappings)
        {
            if (files == null || files.Count == 0) return null;

            var packages = new List<INamespace>();
            var repository = new ModelRepository(EcoreInterop.Repository);
            
            if (resolveMappings != null)
            {
                repository.Locators.Add(new FileMapLocator(resolveMappings));
            }

            foreach (var ecoreFile in files)
            {
                if (Path.GetExtension(ecoreFile) == ".ecore")
                {
#if DEBUG
                    var model = repository.Resolve(ecoreFile);
                    var ePackages = model.RootElements.OfType<EPackage>();
                    foreach (var ePackage in ePackages)
                    {
                        packages.Add(EcoreInterop.Transform2Meta(ePackage));
                    }
#else
                    try
                    {
                        var ePackages = repository.Resolve(ecoreFile).RootElements.OfType<EPackage>();
                        foreach (var ePackage in ePackages)
                        {
                            packages.Add(EcoreInterop.Transform2Meta(ePackage));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred reading the Ecore file. The error message was: " + ex.Message);
                        Environment.ExitCode = 1;
                    }
#endif
                }
                else if (Path.GetExtension(ecoreFile) == ".nmf" || Path.GetExtension(ecoreFile) == ".nmeta")
                {
#if DEBUG
                    packages.AddRange(repository.Resolve(ecoreFile).RootElements.OfType<INamespace>());
#else
                    try
                    {
                        packages.AddRange(repository.Resolve(ecoreFile).RootElements.OfType<INamespace>());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred reading the NMeta file. The error message was: " + ex.Message);
                        Environment.ExitCode = 1;
                    }
#endif
                }
            }

            if (packages.Count == 0)
            {
                throw new InvalidOperationException("No package could be found.");
            }
            else if (packages.Count == 1)
            {
                return packages.First();
            }
            else
            {
                var package = new Namespace() { Name = overallName };
                package.ChildNamespaces.AddRange(packages);
                return package;
            }
        }
    }
}
