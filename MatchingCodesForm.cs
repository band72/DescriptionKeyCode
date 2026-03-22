using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PointDescriptionUpdater;

public class MatchingCodesForm : Form
{
    private DataGridView dgv;
    private Button btnApply;
    private Panel panelBottom;
    private bool isLoading = false;
    
    public event Action<Dictionary<string, string>>? ApplyMatchesRequested;

    private string GetDbPath()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PointDescriptionUpdater");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, "MatchingCodes.txt");
    }

    public MatchingCodesForm(Form owner)
    {
        this.Owner = owner;
        this.Text = "Matching Codes";
        this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
        this.ShowInTaskbar = false;
        
        // Height 20% larger than the old 33% calculation
        int h = Math.Max(250, (int)(owner.Height * 0.4));
        this.Size = new Size(300, h);
        
        this.StartPosition = FormStartPosition.Manual;

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,
            MultiSelect = true
        };
        
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        
        dgv.Columns.Add("Import", "Import");
        dgv.Columns.Add("Export", "Export");
        
        // We trigger saving when cells change, or dirty state commits, or user deletes a row
        dgv.CellValueChanged += (s, e) => { if (!isLoading) SaveData(); };
        dgv.CurrentCellDirtyStateChanged += (s, e) => { if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        dgv.UserDeletedRow += (s, e) => { if (!isLoading) SaveData(); };

        panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };
        
        btnApply = new Button 
        { 
            Text = "Apply Matches", 
            Width = 200, 
            Height = 40,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter 
        };
        btnApply.Location = new Point((this.ClientSize.Width - btnApply.Width) / 2, (panelBottom.Height - btnApply.Height) / 2);
        
        btnApply.Click += (s, e) => {
            ForceSaveData();
            
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                var imp = row.Cells[0].Value?.ToString();
                var exp = row.Cells[1].Value?.ToString() ?? ""; 
                
                if (!string.IsNullOrWhiteSpace(imp) && !map.ContainsKey(imp))
                {
                    map[imp] = exp;
                }
            }
            ApplyMatchesRequested?.Invoke(map);
        };

        panelBottom.Controls.Add(btnApply);

        this.Controls.Add(dgv);
        this.Controls.Add(panelBottom);
    }
    
    // Prevent disposal if the user closes the floater - keep it in memory
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        ForceSaveData();
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
        base.OnFormClosing(e);
    }

    public void ForceSaveData()
    {
        if (dgv.IsCurrentCellInEditMode) dgv.EndEdit();
        SaveData();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadData();
    }
    
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (btnApply != null && panelBottom != null)
        {
            btnApply.Left = (this.ClientSize.Width - btnApply.Width) / 2;
            btnApply.Top = (panelBottom.Height - btnApply.Height) / 2;
        }
    }

    private void LoadData()
    {
        isLoading = true;
        string path = GetDbPath();
        if (File.Exists(path))
        {
            try
            {
                var lines = File.ReadAllLines(path);
                foreach(var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 1)
                    {
                        string imp = parts[0];
                        string exp = parts.Length > 1 ? parts[1] : "";
                        if (!string.IsNullOrWhiteSpace(imp))
                        {
                            dgv.Rows.Add(imp, exp);
                        }
                    }
                }
            }
            catch { }
        }
        isLoading = false;
    }

    private void SaveData()
    {
        if (isLoading) return;
        try
        {
            var lines = new List<string>();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                var imp = row.Cells[0].Value?.ToString();
                var exp = row.Cells[1].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(imp))
                {
                    lines.Add($"{imp}\t{exp}");
                }
            }
            File.WriteAllLines(GetDbPath(), lines);
        }
        catch { }
    }
}
