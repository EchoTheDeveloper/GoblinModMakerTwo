using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using GMMLauncher.Views;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace GMMLauncher.ViewModels
{
    public class DecompilerViewModel : ViewModelBase
    {
        private AssemblyItem _selectedItem;
        public AssemblyItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }
    
        public ObservableCollection<AssemblyItem> AssemblyTree { get; set; } = new();
        public async Task LoadAssembly(Decompiler decompilerWindow, string dllPath)
        {
            if (App.DecompiledTree != null)
            {
                AssemblyTree = App.DecompiledTree;
                var tree = decompilerWindow.FindControl<TreeView>("TreeView");
                tree.ItemsSource = AssemblyTree;
                return;
            }
            var progressBar = new ProgressWindow();
            progressBar.Show();
                
            AssemblyTree.Clear();

            var module = new PEFile(dllPath);
            var metadata = module.Metadata;

            var sortedTypeDefinitions = await Task.Run(() =>
            {
                return metadata.TypeDefinitions
                    .Select(handle => new
                    {
                        Handle = handle,
                        Name = metadata.GetString(metadata.GetTypeDefinition(handle).Name)
                    })
                    .OrderBy(x => x.Name)
                    .Select(x => x.Handle)
                    .ToList();
            });

            int count = sortedTypeDefinitions.Count;
            int current = 0;

            progressBar.Show();

            var progress = new Progress<int>(percent =>
            {
                progressBar.SetProgress(percent);
                if (percent >= 100)
                {
                    progressBar.Close();
                }
            });

            var decompiler = new CSharpDecompiler(dllPath, new DecompilerSettings());

            const int CHUNK_SIZE = 20;

            await Task.Run(async () =>
            {
                for (int i = 0; i < count; i += CHUNK_SIZE)
                {
                    var chunk = sortedTypeDefinitions.Skip(i).Take(CHUNK_SIZE).ToList();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var handle in chunk)
                        {
                            var typeDef = metadata.GetTypeDefinition(handle);
                            string name = metadata.GetString(typeDef.Name);
                            var ns = metadata.GetString(typeDef.Namespace);
                            var fullTypeName = new FullTypeName(string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}");

                            try
                            {
                                var decompiledCode = decompiler.DecompileTypeAsString(fullTypeName);
                                AssemblyTree.Add(new AssemblyItem
                                {
                                    Name = name,
                                    DecompiledCode = new AvaloniaEdit.Document.TextDocument(decompiledCode)
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error decompiling {fullTypeName}: {ex.Message}");
                            }

                            current++;
                            int percentComplete = (int)((current / (double)count) * 100);
                            ((IProgress<int>)progress).Report(percentComplete);
                        }
                    });
                }
            });
            App.DecompiledTree = AssemblyTree;
            Console.WriteLine(App.DecompiledTree.Count);
        }


    }

    public class AssemblyItem
    {
        public string Name { get; set; }
        public TextDocument DecompiledCode { get; set; }
    }
}
