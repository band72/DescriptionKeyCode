using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PointDescriptionUpdater;

public class MainForm : Form
{
    private Button btnLoadMaster;
    private Button btnImportPoints;
    private Button btnSavePoints;
    private DataGridView grid;
    private Label lblStatus;
    
    private SymbolPickerForm symbolPicker;
    private MatchingCodesForm matchingPicker;
    private List<string> masterDescriptions = new List<string>();
    
    private string GetDbPath()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PointDescriptionUpdater");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, "MasterList.txt");
    }

    private void LoadPersistedMasterList()
    {
        string path = GetDbPath();
        if (File.Exists(path))
        {
            try 
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var trimmed = line.Trim();
                        if (!masterDescriptions.Contains(trimmed))
                            masterDescriptions.Add(trimmed);
                    }
                }
            }
            catch {}
        }
    }

    private void SavePersistedMasterList()
    {
        try 
        {
            File.WriteAllLines(GetDbPath(), masterDescriptions);
        }
        catch {}
    }
    
    public MainForm()
    {
        InitializeComponent();
        
        LoadPersistedMasterList();
        
        symbolPicker = new SymbolPickerForm(this);
        symbolPicker.SymbolSelected += OnSymbolSelectedFromPicker;
        
        symbolPicker.SymbolAdded += (sym) => {
            if (!masterDescriptions.Contains(sym))
            {
                masterDescriptions.Add(sym);
                SavePersistedMasterList();
                lblStatus.Text = $"Added '{sym}' to Master List. Total: {masterDescriptions.Count}";
            }
        };
        
        symbolPicker.SymbolRemoved += (sym) => {
            if (masterDescriptions.Contains(sym))
            {
                masterDescriptions.Remove(sym);
                SavePersistedMasterList();
                lblStatus.Text = $"Removed '{sym}'. Total: {masterDescriptions.Count}";
            }
        };

        matchingPicker = new MatchingCodesForm(this);
        matchingPicker.ApplyMatchesRequested += (map) => {
            int updated = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                var importedDesc = row.Cells["ImportedDesc"].Value?.ToString() ?? "";
                var cleanedDesc = row.Cells["CleanedDesc"].Value?.ToString() ?? "";
                
                // Allow matching against both variations
                if (map.TryGetValue(cleanedDesc, out string? expVal))
                {
                    row.Cells["BestMatch"].Value = expVal;
                    updated++;
                }
                else if (map.TryGetValue(importedDesc, out expVal))
                {
                    row.Cells["BestMatch"].Value = expVal;
                    updated++;
                }
            }
            lblStatus.Text = $"Applied matching codes to {updated} records.";
        };
    }
    
    private void RepositionFloatingForms()
    {
        if (symbolPicker != null && symbolPicker.Visible)
            symbolPicker.Location = new Point(this.Right + 5, this.Top);
            
        if (matchingPicker != null && matchingPicker.Visible)
            matchingPicker.Location = new Point(this.Right + 5 + (symbolPicker != null ? symbolPicker.Width : 0) + 5, this.Top);
    }
    
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (masterDescriptions.Count > 0)
        {
            lblStatus.Text = $"Loaded {masterDescriptions.Count} master descriptions from memory.";
            symbolPicker.LoadSymbols(masterDescriptions);
        }
        else
        {
            lblStatus.Text = "No persisted descriptions. Add a new one via '+' on the palette.";
        }
        
        symbolPicker.Show(this);
        matchingPicker.Show(this);
        
        RepositionFloatingForms();
    }
    
    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        RepositionFloatingForms();
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        matchingPicker?.ForceSaveData();
        base.OnFormClosing(e);
    }
    
    private void OnSymbolSelectedFromPicker(string symbol)
    {
        if (grid.SelectedRows.Count > 0)
        {
            foreach (DataGridViewRow row in grid.SelectedRows)
                row.Cells["BestMatch"].Value = symbol;
        }
        else if (grid.CurrentCell != null)
        {
            grid.Rows[grid.CurrentCell.RowIndex].Cells["BestMatch"].Value = symbol;
        }
    }
    
    private void InitializeComponent()
    {
        this.Text = "Point Description Updater";
        this.Size = new Size(1100, 600);
        this.StartPosition = FormStartPosition.CenterScreen;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
        
        btnLoadMaster = new Button { Text = "Add'l Codes", Location = new Point(20, 20), Width = 180, Height = 40, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleCenter };
        btnLoadMaster.Click += BtnLoadMaster_Click;

        btnImportPoints = new Button { Text = "Import Points", Location = new Point(220, 20), Width = 160, Height = 40, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleCenter };
        btnImportPoints.Click += BtnImportPoints_Click;
        
        btnSavePoints = new Button { Text = "Export Updated Points", Location = new Point(400, 20), Width = 200, Height = 40, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleCenter };
        btnSavePoints.Click += BtnSavePoints_Click;

        lblStatus = new Label { Location = new Point(620, 30), AutoSize = true, Text = "Ready." };

        topPanel.Controls.Add(btnLoadMaster);
        topPanel.Controls.Add(btnImportPoints);
        topPanel.Controls.Add(btnSavePoints);
        topPanel.Controls.Add(lblStatus);
        
        var btnHelp = new Button { Text = "?", Width = 30, Height = 40, Font = new Font("Segoe UI", 12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        btnHelp.Top = 20;
        topPanel.Resize += (s, e) => {
            btnHelp.Left = topPanel.Width - btnHelp.Width - 20;
        };
        btnHelp.Click += (s, e) => {
            string helpText = @"POINT DESCRIPTION UPDATER - QUICK START HELP MANUAL

1. Add'l Codes:
Click ""Add'l Codes"" to selectively import a text/CSV file. The app will scrape its unique descriptions and add them to your Master Symbols palette. 

2. Import Points:
Click ""Import Points"" to load your working coordinate file. The format expects: Point, Northing, Easting, Elevation, Description. The application instantly generates a ""Cleaned"" version of your descriptions (stripping hyphens and quotes).

3. Master Symbols Palette:
This floating window contains your validated base codes. Double-click any symbol to force-override the ""Best Match"" column for your currently selected rows in the main window. You can manually type and use the '+' and '-' buttons to manage these!

4. Matching Codes Palette:
This is your logic rules engine. Type a raw description in the ""Import"" column and its target definition in the ""Export"" column. Click ""Apply Matches"" to automatically translate your main grid. (Note: Leaving an Export cell blank will intentionally wipe out the description for that code).

5. Export Updated Points:
Once your ""Best Match"" column is perfectly mapped, click Export to save a new valid coordinate file!";
            MessageBox.Show(this, helpText, "Help Manual", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        topPanel.Controls.Add(btnHelp);

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        // Center all the text vertically in DateGridView
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Point", HeaderText = "Point", ReadOnly = true, Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Northing", HeaderText = "Northing", ReadOnly = true, Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Easting", HeaderText = "Easting", ReadOnly = true, Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Elevation", HeaderText = "Elevation", ReadOnly = true, Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ImportedDesc", HeaderText = "Imported Description", ReadOnly = true, Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CleanedDesc", HeaderText = "Cleaned", ReadOnly = true, Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BestMatch", HeaderText = "Best Match (Master)", Width = 200 });

        this.Controls.Add(grid);
        this.Controls.Add(topPanel);
    }
    
    private void BtnLoadMaster_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Title = "Select Master List File", Filter = "Text Files|*.txt;*.csv|All Files|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            var lines = File.ReadAllLines(ofd.FileName);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(',');
                
                if (parts.Length >= 5)
                {
                    string desc = parts[4].Trim();
                    if (!string.IsNullOrEmpty(desc) && !masterDescriptions.Contains(desc))
                        masterDescriptions.Add(desc);
                }
                else
                {
                    string desc = parts[0].Trim();
                    if (!string.IsNullOrEmpty(desc) && !masterDescriptions.Contains(desc))
                        masterDescriptions.Add(desc);
                }
            }
            SavePersistedMasterList();
            lblStatus.Text = $"Loaded and saved {masterDescriptions.Count} unique descriptions.";
            
            symbolPicker.LoadSymbols(masterDescriptions);
            if (!symbolPicker.Visible) symbolPicker.Show(this);
            
            RepositionFloatingForms();
        }
    }

    private void BtnImportPoints_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Title = "Select Points to Import", Filter = "Point Files|*.txt;*.csv|All Files|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            grid.Rows.Clear();
            var lines = File.ReadAllLines(ofd.FileName);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    string pt = parts[0].Trim();
                    string n = parts[1].Trim();
                    string eStr = parts[2].Trim();
                    string z = parts[3].Trim();
                    string importedDesc = parts[4].Trim();

                    string cleanedDesc = importedDesc.Replace("-", " ").Replace("\"", " ").Trim();
                    while (cleanedDesc.Contains("  "))
                        cleanedDesc = cleanedDesc.Replace("  ", " ");

                    string bestMatch = GetBestMatch(cleanedDesc);

                    grid.Rows.Add(pt, n, eStr, z, importedDesc, cleanedDesc, bestMatch);
                }
            }
            lblStatus.Text = $"Imported {grid.Rows.Count} points.";
            
            if (!symbolPicker.Visible) symbolPicker.Show(this);
            if (!matchingPicker.Visible) matchingPicker.Show(this);
            RepositionFloatingForms();
        }
    }
    
    private void BtnSavePoints_Click(object? sender, EventArgs e)
    {
        if (grid.Rows.Count == 0) return;

        using var sfd = new SaveFileDialog { Title = "Save Updated Points", Filter = "CSV Files|*.csv;*.txt|All Files|*.*", DefaultExt = "csv" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            using var writer = new StreamWriter(sfd.FileName);
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var pt = row.Cells["Point"].Value?.ToString() ?? "";
                var n = row.Cells["Northing"].Value?.ToString() ?? "";
                var eStr = row.Cells["Easting"].Value?.ToString() ?? "";
                var z = row.Cells["Elevation"].Value?.ToString() ?? "";
                var match = row.Cells["BestMatch"].Value?.ToString() ?? "";
                
                writer.WriteLine($"{pt},{n},{eStr},{z},{match}");
            }
            lblStatus.Text = $"Saved to {Path.GetFileName(sfd.FileName)}";
        }
    }
    
    private string GetBestMatch(string input)
    {
        if (masterDescriptions.Count == 0) return input;
        
        var exact = masterDescriptions.FirstOrDefault(m => string.Equals(m, input, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        
        var prefix = masterDescriptions.FirstOrDefault(m => input.StartsWith(m + " ", StringComparison.OrdinalIgnoreCase) || input.StartsWith(m + "-", StringComparison.OrdinalIgnoreCase));
        if (prefix != null) return prefix;

        int minDistance = int.MaxValue;
        string bestMatch = input;

        foreach (var master in masterDescriptions)
        {
            int distance = LevenshteinDistance(input.ToUpperInvariant(), master.ToUpperInvariant());
            if (distance < minDistance)
            {
                minDistance = distance;
                bestMatch = master;
            }
        }
        
        return bestMatch; 
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int[] v0 = new int[t.Length + 1];
        int[] v1 = new int[t.Length + 1];

        for (int i = 0; i < v0.Length; i++) v0[i] = i;

        for (int i = 0; i < s.Length; i++)
        {
            v1[0] = i + 1;

            for (int j = 0; j < t.Length; j++)
            {
                int cost = (s[i] == t[j]) ? 0 : 1;
                v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
            }

            for (int j = 0; j < v0.Length; j++)
                v0[j] = v1[j];
        }

        return v1[t.Length];
    }
}
