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

        public MainWindow()
        {
            InitializeComponent();

            cbServer.ItemsSource = _sqlServers.Select(x => x.Key);
        }

        private void btnRunQueries_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = _sqlServers[cbServer.SelectedValue.ToString()];

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                sqlConnection.InfoMessage += SqlConnection_InfoMessage;

                _files.Keys.ToList().ForEach(fileName => ExecuteQuery(fileName, sqlConnection));
            }
        }

        string _currentCommandOutput;

        private void ExecuteQuery(string fileName, SqlConnection sqlConnection)
        {
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
                }
                catch (Exception ex)
                {
                    _currentCommandOutput += $"{ex.Message}{Environment.NewLine}";
                }
            }

            var destinationFile = _files[fileName].Replace(fileName, $"{cbServer.SelectedValue}\\{fileName.Replace(".sql", ".txt")}");

            var destinationDirectory = Path.GetDirectoryName(destinationFile);
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.WriteAllText(destinationFile, _currentCommandOutput);
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
    }
}