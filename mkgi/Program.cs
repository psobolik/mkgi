using GitignoreIoLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;

namespace mkgi
{
    class Program
    {
        private static Action _isRunning = MainApp;

        private static void Main()
        {
            while (_isRunning != null)
                _isRunning.Invoke();
            Application.Shutdown();
        }

        private static ListView _listView;
        private static List<string> _templates;
        private static string _fileName;

        private static void MainApp()
        {
            Application.Init();
            _fileName = Path.Combine(Directory.GetCurrentDirectory(), ".gitignore");
            var top = Application.Top;

            var mainWindow = CreateWindow();
            _listView = CreateListView();
            var fileNameLabel = CreateLabel(_fileName);

            mainWindow.Add(_listView);
            top.Add(fileNameLabel);
            top.Add(mainWindow);
            top.Add(CreateStatusBar());

            top.LayoutComplete += (e) =>
            {
                fileNameLabel.X = Pos.Left(mainWindow) + 1;
                fileNameLabel.Y = Pos.Bottom(mainWindow);
            };
            
            Application.Run();

            static ListView CreateListView()
            {
                var listView = new ListView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    AllowsMarking = true,
                    AllowsMultipleSelection = true,
                };

                async void OnListViewOnInitialized(object sender, EventArgs args)
                {
                    _templates = (await GitignoreIoRepository.GetTemplateNames()).ToList();
                    await _listView.SetSourceAsync(_templates);
                }

                listView.Initialized += OnListViewOnInitialized;
                return listView;
            }

            static Label CreateLabel(string text)
            {
                return new Label
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = 1,
                    ColorScheme = Colors.TopLevel,
                    Text = text,
                };
            }

            static Window CreateWindow()
            {
                var window = new Window("Templates")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 2,
                };
                window.KeyPress += (e) =>
                {
                    var keyValue = (char)e.KeyEvent.KeyValue;
                    if (keyValue > 32 && keyValue <= 127)
                    {
                        e.Handled = true;
                        var skip = keyValue == _templates[_listView.SelectedItem].First() ? _listView.SelectedItem + 1 : 0;
                        var index = skip + _templates.Skip(skip).ToList().FindIndex(t => t.StartsWith(keyValue));
                        if (index >= 0 && index < _templates.Count())
                        {
                            _listView.TopItem = index;
                            _listView.SelectedItem = index;
                        }
                    }
                };
                return window;
            }

            static StatusBar CreateStatusBar()
            {
                return new StatusBar(new[] {
                    new StatusItem(Key.ControlS, "~^S~ Save", Save),
                    new StatusItem(Key.ControlQ, "~^Q~ Quit", Quit),
                });
            }
        }

        private static async void Save()
        {
            var selectedTemplates = GetSelectedTemplates();
            if (!selectedTemplates.Any())
            {
                MessageBox.ErrorQuery(50, 7, "Nothing Selected", "\nSelect one or more templates and try again.", "OK");
            }
            else
            {
                if (File.Exists(_fileName))
                {
                    switch (MessageBox.ErrorQuery(50, 9, "Overwrite", $"\n{_fileName} exists.\n\nDo you want to overwrite it or append to it?", "Overwrite", "Append", "Cancel"))
                    {
                        case 0:
                            await SaveGitignore(_fileName, selectedTemplates);
                            Quit();
                            break;
                        case 1:
                            await AppendGitignore(_fileName, selectedTemplates);
                            Quit();
                            break;
                    }
                }
                else
                {
                    await SaveGitignore(_fileName, selectedTemplates);
                    Quit();
                }
            }

            static async Task AppendGitignore(string filePath, IEnumerable<string> selectedTemplates)
            {
                try
                {
                    await using var writer = new StreamWriter(filePath, true);
                    var contents = await GitignoreIoRepository.GetTemplate(selectedTemplates.ToArray());
                    await writer.WriteAsync(contents);
                }
                catch (IOException ex)
                {
                    MessageBox.ErrorQuery("Error", ex.Message);
                }
            }

            static async Task SaveGitignore(string filePath, IEnumerable<string> selectedTemplates)
            {
                try
                {
                    var contents = await GitignoreIoRepository.GetTemplate(selectedTemplates.ToArray());
                    await File.WriteAllTextAsync(filePath, contents);
                }
                catch(IOException ex)
                {
                    MessageBox.ErrorQuery("Error", ex.Message);
                }
            }

            static List<string> GetSelectedTemplates()
            {
                var selectedItems = new List<string>();
                for (int i = 0, l = _listView.Source.Count; i < l; ++i)
                {
                    if (_listView.Source.IsMarked(i))
                        selectedItems.Add(_templates[i]);
                }
                return selectedItems;
            }
        }

        private static void Quit()
        {
            _isRunning = null; 
            Application.Top.Running = false;
        }
    }
}
