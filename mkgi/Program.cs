using GitignoreIoLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Terminal.Gui;

namespace mkgi
{
    class Program
    {
        public static Action _isRunning = MainApp;

        static void Main(string[] args)
        {
            while (_isRunning != null)
                _isRunning.Invoke();
            Application.Shutdown();
        }

        private static Window _mainWindow;
        private static ListView _listView;
        private static Label _fileNameLabel;
        private static StatusBar _statusBar;
        private static List<string> _templates = new List<string>();

        public static void MainApp()
        {
            Application.Init();
            var top = Application.Top;

            _mainWindow = CreateWindow();
            _listView = CreateListView();
            _fileNameLabel = CreateLabel();
            _statusBar = CreateStatusBar();

            _mainWindow.Add(_listView);
            top.Add(_fileNameLabel);
            top.Add(_mainWindow);
            top.Add(_statusBar);

            top.LayoutComplete += (e) =>
            {
                _fileNameLabel.X = Pos.Left(_mainWindow) + 1;
                _fileNameLabel.Y = Pos.Bottom(_mainWindow);
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
                var task = GitignoreIoRepository.GetTemplateNames();
                if (task.Wait(5000))
                {
                    _templates = task.Result.ToList();
                    listView.SetSource(_templates);
                }
                return listView;
            }

            static Label CreateLabel()
            {
                return new Label
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = 1,
                    ColorScheme = Colors.TopLevel,
                    Text = Path.Combine(Directory.GetCurrentDirectory(), ".gitignore"),
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
                return new StatusBar(new StatusItem[] {
                    new StatusItem(Key.ControlS, "~^S~ Save", Save),
                    new StatusItem(Key.ControlQ, "~^Q~ Quit", Quit),
                });
            }
        }

        static async void Save()
        {
            var selectedTemplates = GetSelectedTemplates();
            if (selectedTemplates.Count() == 0)
            {
                MessageBox.ErrorQuery(50, 7, "Nothing Selected", "\nSelect one or more templates and try again.", "OK");
            }
            else
            {
                var file = _fileNameLabel.Text.ToString();
                if (File.Exists(file))
                {
                    switch (MessageBox.ErrorQuery(50, 9, "Overwrite", $"\n{file} exists.\n\nDo you want to overwrite it or append to it?", "Overwrite", "Append", "Cancel"))
                    {
                        case 0:
                            await SaveGitignore(file, selectedTemplates);
                            Quit();
                            break;
                        case 1:
                            await AppendGitignore(file, selectedTemplates);
                            Quit();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    await SaveGitignore(file, selectedTemplates);
                    Quit();
                }
            }

            static async Task AppendGitignore(string filePath, IEnumerable<string> selectedTemplates)
            {
                try
                {
                    using var writer = new StreamWriter(filePath, true);
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

            static IEnumerable<string> GetSelectedTemplates()
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

        static void Quit()
        {
            _isRunning = null; 
            Application.Top.Running = false;
        }

        static void NotImplemented(string what)
        {
            MessageBox.Query(50, 7, "Not implemented", $"\n{what} is not implemented.", "OK");
        }
    }
}
