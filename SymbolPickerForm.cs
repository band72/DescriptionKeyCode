using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PointDescriptionUpdater;

public class SymbolPickerForm : Form
{
    private DataGridView dgv;
    private TextBox txtNewSymbol;
    private Button btnAdd;
    private Button btnRemove;
    
    public event Action<string>? SymbolSelected;
    public event Action<string>? SymbolAdded;
    public event Action<string>? SymbolRemoved;

    public SymbolPickerForm(Form owner)
    {
        this.Owner = owner;
        this.Text = "Master Symbols";
        this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
        this.ShowInTaskbar = false;
        
        // Height matches main form
        int h = owner.Height;
        this.Size = new Size(250, h);
        
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(owner.Right + 10, owner.Top);

        // Grid for symbols
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            MultiSelect = false
        };
        dgv.Columns.Add("Symbol", "Required Symbols");
        
        var toolTip = new ToolTip();
        toolTip.SetToolTip(dgv, "Double click to change descriptions");
        
        dgv.CellDoubleClick += (s, e) => {
            if (e.RowIndex >= 0)
            {
                var val = dgv.Rows[e.RowIndex].Cells[0].Value?.ToString();
                if (!string.IsNullOrEmpty(val))
                    SymbolSelected?.Invoke(val);
            }
        };

        // Bottom panel for editing
        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
        
        // Center mathematically across 250 width
        // txt(150) + pad(5) + btn(25) + pad(5) + btn(25) = 210
        // (250 - 210) / 2 = 20 left margin
        txtNewSymbol = new TextBox { Location = new Point(20, 11), Width = 150 };
        int th = txtNewSymbol.Height;
        btnAdd = new Button { Text = "+", Location = new Point(175, 11), Width = 25, Height = th, TextAlign = ContentAlignment.MiddleCenter };
        btnRemove = new Button { Text = "-", Location = new Point(205, 11), Width = 25, Height = th, TextAlign = ContentAlignment.MiddleCenter };

        btnAdd.Click += (s, e) => {
            string text = txtNewSymbol.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                bool exists = false;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == text)
                        exists = true;
                }
                
                if (!exists)
                {
                    dgv.Rows.Add(text);
                    SymbolAdded?.Invoke(text);
                }
                txtNewSymbol.Clear();
            }
        };

        btnRemove.Click += (s, e) => {
            if (dgv.SelectedRows.Count > 0)
            {
                string? text = dgv.SelectedRows[0].Cells[0].Value?.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    dgv.Rows.RemoveAt(dgv.SelectedRows[0].Index);
                    SymbolRemoved?.Invoke(text);
                }
            }
        };

        panelBottom.Controls.Add(txtNewSymbol);
        panelBottom.Controls.Add(btnAdd);
        panelBottom.Controls.Add(btnRemove);

        this.Controls.Add(dgv);
        this.Controls.Add(panelBottom);
    }

    public void LoadSymbols(IEnumerable<string> symbols)
    {
        dgv.Rows.Clear();
        foreach (var sym in symbols.OrderBy(x => x))
        {
            dgv.Rows.Add(sym);
        }
    }
}
