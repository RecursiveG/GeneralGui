using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;

namespace prj_GeneralGui {
  public class ScriptItem : ComboBoxItem {
    public ScriptItem(FileInfo fi) : this(fi.Name, File.ReadAllText(fi.FullName)) {
    }
    public ScriptItem(string script_name, string script_template) {
      ScriptName = script_name;
      ScriptTemplate = script_template;
      Content = script_name;
    }

    public string ScriptName;
    public string ScriptTemplate;
  }

  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      DirectoryInfo workdir = new DirectoryInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
      Debug.WriteLine($"Workdir: {workdir.FullName}");
      FileInfo[] scripts = workdir.GetFiles("*.txt");
      foreach (var fi in scripts) {
        Debug.WriteLine($"Found script: {fi.FullName}");
        ComboBoxScriptList.Items.Add(new ScriptItem(fi));
      }
      Debug.WriteLine($"Found {scripts.Length} scripts.");

      ComboBoxScriptList.Items.Add(new ScriptItem("list_folder.txt", "dir\n// Right click -> Insert folder at next line.\n"));
      ComboBoxScriptList.Items.Add(new ScriptItem("read_file.txt", "type\n// Right click -> Insert file at next line.\n"));
      if (!ComboBoxScriptList.Items.IsEmpty) {
        ComboBoxScriptList.SelectedItem = ComboBoxScriptList.Items[0];
      }
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      List<string> cmd = new List<string>();
      foreach (string line in TextBoxScriptEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None)) {
        if (line.Trim().Length == 0 || line.Trim().StartsWith("// ")) continue;
        cmd.Add(line);
      }
      for (int i = 0; i < cmd.Count; i++) {
        Debug.WriteLine("argv[{0}]={1}", i, cmd[i]);
      }
      Debug.WriteLine("Executing...");
      if (cmd.Count < 1) {
        MessageBox.Show("Invalid program");
        return;
      }
      
      Process p = new Process();
      p.StartInfo.WorkingDirectory = new DirectoryInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)).FullName;
      p.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
      p.StartInfo.ArgumentList.Add("/K");
      for (int i = 0; i < cmd.Count; i++) {
        p.StartInfo.ArgumentList.Add(cmd[i]);
      }
      p.Start();
    }

    private void InsertFilePathAtCursor(object sender, RoutedEventArgs e) {
      var fd = new System.Windows.Forms.OpenFileDialog {
        CheckFileExists = true,
        CheckPathExists = true,
        Multiselect = false
      };
      var result = fd.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK) {
        string to_insert = fd.FileName;
        int old_location = TextBoxScriptEditor.SelectionStart;
        TextBoxScriptEditor.Text = TextBoxScriptEditor.Text.Insert(old_location, to_insert);
        TextBoxScriptEditor.SelectionStart = old_location + to_insert.Length;
      }
    }

    private void InsertFolderPathAtCursor(object sender, RoutedEventArgs e) {
      var fbd = new System.Windows.Forms.FolderBrowserDialog();
      var result = fbd.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK) {
        string to_insert = fbd.SelectedPath;
        var old_location = TextBoxScriptEditor.SelectionStart;
        TextBoxScriptEditor.Text = TextBoxScriptEditor.Text.Insert(old_location, to_insert);
        TextBoxScriptEditor.SelectionStart = old_location + to_insert.Length;
      }
    }

    private bool comboBoxSelectionChangeRollbackInProgress = false;
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (comboBoxSelectionChangeRollbackInProgress) return;

      string old_template = "";
      ScriptItem old_selection = null;
      foreach (ScriptItem si in e.RemovedItems) {
        old_template = si.ScriptTemplate;
        old_selection = si;
      }

      if (TextBoxScriptEditor.Text != old_template) {
        MessageBoxResult result = MessageBox.Show("Your changes won't be saved, continue?", "Content changed", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes) {
          comboBoxSelectionChangeRollbackInProgress = true;
          ComboBoxScriptList.SelectedItem = old_selection;
          comboBoxSelectionChangeRollbackInProgress = false;
          return;
        }
      }

      foreach (ScriptItem si in e.AddedItems) {
        TextBoxScriptEditor.Text = si.ScriptTemplate;
      }
    }
  }
}
