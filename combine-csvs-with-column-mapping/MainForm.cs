using IVSoftware.Portable.Csv;
using System.ComponentModel;
using System.Diagnostics;

namespace combine_csvs_with_column_mapping
{
    public partial class MainForm : Form
    {
        string Folder { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public MainForm() => InitializeComponent();
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridView.DataSource = Recordset;
            dataGridView.Columns[nameof(MasterRecord.RollNo)].HeaderText = "Roll no";

            foreach (var file in new[] { Path.Combine(Folder, "1.csv"), Path.Combine(Folder, "2.csv") })
            {
                string[] lines = File.ReadAllLines(file);

                if (lines.FirstOrDefault() is string header)
                {
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (!string.IsNullOrEmpty(line))
                        {
                            MasterRecord record = typeof(MasterRecord).Extract<MasterRecord>(header, line);
                            Recordset.Add(record);
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "Expecting header");
                }
            }
            bool save = true;
            if(save)
            {
                var savePath = Path.Combine(Folder, "master.csv");
                var builder = new List<string>();
                builder.Add(typeof(MasterRecord).GetCsvHeader());
                builder.AddRange(Recordset.Select(_ => _.ToCsvLine()));
                var lines = string.Join(Environment.NewLine, builder.ToArray());
                File.WriteAllText(savePath, lines);
            }
        }
        BindingList<MasterRecord> Recordset { get; } = new BindingList<MasterRecord>();
    }

    class MasterRecord
    {
        [HeaderText("Roll no")]
        public int? RollNo { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Because specified format e.g. "21-12-2023" does not format to DateTime we use string here.
        /// </summary>
        public string Date { get; set; } = string.Empty;
        public int? Score { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
