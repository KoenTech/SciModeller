using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using IronPython.Hosting;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;


namespace NatkModelleren
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<double> vidData = new List<double>();
        public MainWindow()
        {
            InitializeComponent();
            
            // Set Scottplot style
            var plt = wpfPlot1.plt;
            plt.Style(ScottPlot.Style.Blue2);
            plt.Style(dataBg: System.Drawing.Color.FromArgb(20, 20, 20), figBg: System.Drawing.Color.FromArgb(28, 28, 28));
            
            // Set Plot title
            plt.Title("Graph");
            plt.YLabel("y");
            plt.XLabel("x");
            plt.Legend();
        }


        // Not implemented yet

        //List<double> afgeleide(List<double> data, double deltaTime)
        //{
        //    //afgeleide
        //    List<double> dataAT = new List<double>();
        //    for (int i = 0; i < data.Count - 1; i++)
        //    {
                
        //        dataAT.Add((data[i + 1] - data[i]) / deltaTime);
        //    }
        //    return dataAT;
        //}


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Check if advanced mode is enabled
            if (AdvancedToggle.IsChecked.Value)
            {
                // If advanced mode is enabled just run the user script
                pythonParse(formulas1.Text, Rvar.Text);
            }
            else
            {
                // Otherwise format the input from the different textboxes and put it in a loop
                string[] Lines = formulas1.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                StringBuilder str = new StringBuilder();
                str.Append("while i < max:\n");
                str.Append("  i += 1\n");
                foreach (var text in Lines)
                {
                    str.Append("  " + text + "\n");
                }

                string math = variables1.Text + "\nr = []\ni = 0\nimport math\n" + str.ToString();
                Debug.Write(math);
                pythonParse(math);
            }
        }

        async void pythonParse(string script, string Rvar = "r")
        {
            var engine = Python.CreateEngine(); // Extract Python language engine from their grasp
            var scope = engine.CreateScope(); // Introduce Python namespace (scope)
            var source = engine.CreateScriptSourceFromString(script);

            var plt = wpfPlot1.plt;
            plt.Clear();
            List<double> data = new List<double>();
            try
            {
                await Task.Run(() => source.Execute(scope));
                var result = scope.GetVariable(Rvar); // To get the finally set variable 'r' from the python script

                // Convert all data to <double>
                foreach (var item in result)
                {
                    data.Add(Convert.ToDouble(item));
                }

                // Plot the data
                plt.PlotSignal(data.ToArray(), SampleRateBox.Value, label: "Model");
                wpfPlot1.Render();
            }
            catch (Exception ex)
            {
                // Show error message
                MessageBox.Show(ex.Message, "Python Error!");
            }
        }

        private void AdvancedToggle_Click(object sender, RoutedEventArgs e)
        {
            if (AdvancedToggle.IsChecked.Value)
            {
                variables1.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(formulas1, 2);
                Rvar.IsEnabled = true;
            }
            else
            {
                variables1.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(formulas1, 1);
                Rvar.IsEnabled = false;
            }
        }

        void SaveFile()
        {
            SciModel model = new SciModel() {variables = variables1.Text, formulas = formulas1.Text, advanced = AdvancedToggle.IsChecked.Value};
            Serialize(model);
        }

        void OpenFile()
        {
            SciModel model = Deserialize();
            if (model != null)
            {
                variables1.Text = model.variables;
                formulas1.Text = model.formulas;
                AdvancedToggle.IsChecked = model.advanced;
                if (AdvancedToggle.IsChecked.Value)
                {
                    variables1.Visibility = Visibility.Collapsed;
                    Grid.SetColumnSpan(formulas1, 2);
                    Rvar.IsEnabled = true;
                }
                else
                {
                    variables1.Visibility = Visibility.Visible;
                    Grid.SetColumnSpan(formulas1, 1);
                    Rvar.IsEnabled = false;
                }
            }
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void Open_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        static void Serialize(SciModel model)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "My Model"; // Default file name
            dlg.DefaultExt = ".natk"; // Default file extension
            dlg.Filter = "Natk models (.natk)|*.natk"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                FileStream fs = new FileStream(dlg.FileName, FileMode.Create);

                // Construct a BinaryFormatter and use it to serialize the data to the stream.
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(fs, model);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                    throw;
                }
                finally
                {
                    fs.Close();
                }
            }
        }
        static SciModel Deserialize()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Natk models (.natk)|*.natk";
            openFileDialog.DefaultExt = ".natk";
            if (openFileDialog.ShowDialog() == true)
            {
                SciModel model = null;

                // Open the file containing the data that you want to deserialize.
                FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open);
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    // Deserialize the Scimodel from the file and
                    // assign the reference to the local variable.
                    model = (SciModel)formatter.Deserialize(fs);
                    return model;
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    MessageBox.Show("Error opening file: " + openFileDialog.SafeFileName);
                }
                finally
                {
                    fs.Close();
                }
            }
            return null;
        }
    }
}
