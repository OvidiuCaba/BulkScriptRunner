using System;
using System.Collections.Generic;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace RunQueries
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, string> _sqlServers = new Dictionary<string, string>
        {
            ["TEST"] = "Server=(local);Database=TEST;User Id=sa;Password=sa;",
            ["TESTAfter"] = "Server=(local);Database=TESTAfter;User Id=sa;Password=sa;",
        };

        private Dictionary<string, string> _files;
        private Dictionary<string, bool> _filesStatus;
        private Dictionary<string, string> _outputFiles;

        private string _currentCommandOutput;

        private bool _hasErrors;

        public MainWindow()
        {
            InitializeComponent();

            cbServer.ItemsSource = _sqlServers.Select(x => x.Key);
        }

        private void btnRunQueries_Click(object sender, RoutedEventArgs e)
        {
            if (_files == null || !_files.Any())
            {
                System.Windows.MessageBox.Show("Select a folder with SQL scripts before proceeding.");
                return;
            }

            if (cbServer.SelectedValue == null)
            {
                System.Windows.MessageBox.Show("Select a server before proceeding.");
                return;
            }

            _hasErrors = false;
            lblStatus.Content = "Running queries";

            string connectionString = _sqlServers[cbServer.SelectedValue.ToString()];

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                sqlConnection.InfoMessage += SqlConnection_InfoMessage;

                _files.Keys.ToList().ForEach(fileName => ExecuteQuery(fileName, sqlConnection));
            }

            if(_filesStatus.Any(x => x.Value))
            {
                lblStatus.Content = "Finished with errors";
                lblStatus.Foreground = new SolidColorBrush(Colors.Red);
                var filesWithErrors = _filesStatus.Where(x => x.Value).Select(x => x.Key);
                foreach (var listBoxItem in lbFiles.Items)
                {
                    if (filesWithErrors.Contains(listBoxItem.ToString()))
                    {
                        var item = lbFiles.ItemContainerGenerator.ContainerFromItem(listBoxItem) as ListBoxItem;
                        item.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
            }
            else
            {
                lblStatus.Content = "Finished successfully";
                lblStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void ExecuteQuery(string fileName, SqlConnection sqlConnection)
        {
            _hasErrors = false;

            var queryString = File.ReadAllText(_files[fileName]);

            _currentCommandOutput = string.Empty;

            using (var command = new SqlCommand(queryString, sqlConnection))
            {
                command.StatementCompleted += Command_StatementCompleted;
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    foreach (SqlError err in ex.Errors)
                    {
                        _currentCommandOutput += $"Msg {err.Number}, Level {err.Class}, State {err.State}, Line {err.LineNumber}{Environment.NewLine}";
                        _currentCommandOutput += $"{err.Message}{Environment.NewLine}";
                    }

                    _hasErrors = true;
                }
                catch (Exception ex)
                {
                    _currentCommandOutput += $"{ex.Message}{Environment.NewLine}";

                    _hasErrors = true;
                }
            }

            var outputFile = _files[fileName].Replace(fileName, $"{cbServer.SelectedValue}\\{fileName.Replace(".sql", ".txt")}");

            if (_outputFiles == null)
                _outputFiles = new Dictionary<string, string>();

            if (!_outputFiles.ContainsKey(fileName))
                _outputFiles.Add(fileName, outputFile);

            var destinationDirectory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.WriteAllText(outputFile, _currentCommandOutput);

            if (_hasErrors)
                _filesStatus[fileName] = true;
        }

        private void SqlConnection_InfoMessage(object sender, SqlInfoMessageEventArgs args)
        {
            foreach (SqlError err in args.Errors)
            {
                Console.WriteLine("Msg {0}, Level {1}, State {2}, Line {3}",
                    err.Number, err.Class, err.State, err.LineNumber);
                Console.WriteLine("{0}", err.Message);
            }
        }

        private void Command_StatementCompleted(object sender, System.Data.StatementCompletedEventArgs e)
        {
            _currentCommandOutput += $"({e.RecordCount} row(s) affected){Environment.NewLine}";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                lblFolder.Content = dialog.SelectedPath;

                _files = Directory.EnumerateFiles(dialog.SelectedPath)
                                    .Where(x => x.ToLower().EndsWith(".sql"))
                                    .Select(x => new { FileName = x.Substring(x.LastIndexOf("\\") + 1), Path = x })
                                    .OrderBy(x => x.FileName)
                                    .ToDictionary(x => x.FileName, x => x.Path);
                _filesStatus = _files.ToDictionary(x => x.Key, x => false);

                lbFiles.ItemsSource = _files.Keys;
            }
        }

        private void cbServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lblConnectionString.Content = _sqlServers[cbServer.SelectedValue.ToString()];
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void lbFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_outputFiles == null || !_outputFiles.Any())
                return;

            txtOutput.Text = File.ReadAllText(_outputFiles[lbFiles.SelectedItem.ToString()]);
        }

        private void lbFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(_files[lbFiles.SelectedItem.ToString()]);
        }

        private void lbFiles_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (lbFiles.Items.Count == 0)
                return;

            lbFiles.ToolTip = "Double click to open script in MSSM";
        }

        private void lbFiles_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lbFiles.ToolTip = null;
        }
    }
}