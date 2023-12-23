using IVSoftware.Portable.Csv;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace Testbench
{
    public partial class MainForm : Form
    {
        static string Folder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "IVSoftware",
            Assembly.GetExecutingAssembly().GetName().Name,
            "Data");
        string Output { get; } = Path.Combine(Folder, "Output.csv");
        public MainForm()
        {
            InitializeComponent();
            Directory.CreateDirectory(Folder);
            var buttonLocation = new Point(dataGridView.Width - 70, dataGridView.Height - 70);
            Label buttonRefresh = new Label
            {
                Size = new Size(60, 60),
                Text = "↻",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 12),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = buttonLocation,
                ForeColor = Color.White,
                BackColor = Color.CornflowerBlue,
            };
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, buttonRefresh.Width, buttonRefresh.Height);
            buttonRefresh.Region = new Region(path);
            dataGridView.Controls.Add(buttonRefresh);
            buttonRefresh.Click += (sender, e) =>
            {
                BeginInvoke(() =>
                {
                    File.WriteAllLines(Output, Recordset.GetAllLines());
                    Process.Start("notepad.exe", Output);
                    string[] lines = File.ReadAllLines(Output);

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
                });
            };
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridView.DataSource = Recordset;

            var masterType = typeof(MasterRecord);
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if(masterType.GetProperty(column.Name) is PropertyInfo pi)
                {
                    if(pi.GetCustomAttribute<HeaderTextAttribute>() is HeaderTextAttribute headerText)
                    {
                        column.HeaderText = headerText.Value;
                    }
                    if (pi.GetCustomAttribute<StringFormatAttribute>() is StringFormatAttribute stringFormat)
                    {
                        column.DefaultCellStyle.Format = stringFormat.Value;
                    }
                }
            }
            dataGridView.Columns[nameof(MasterRecord.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView.Columns[nameof(MasterRecord.ID)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Recordset.Add(new MasterRecord
            {
                Int32 = 100,
                Name = "IV, Tommy"
            });
            Recordset.Add(new MasterRecord
            {
                Name = "Red, Green, Yellow, Blue",
            });
        }
        BindingList<MasterRecord> Recordset { get; } = new BindingList<MasterRecord>();
    }

    class MasterRecord
    {
        public MasterRecord() => _id++;

        public int ID { get; } = _id;
        private static int _id = 1;

        [StringFormat(@"yyyy.MM.dd")]

        [HeaderText("Formatted")]
        public DateTime DateTime { get; set; } = DateTime.Now;

        public DateOnly DateOnly { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public TimeOnly TimeOnly { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
        public int? Int32 { get; set; }

        [HeaderText("Test String")]
        public string Name { get; set; } = $"{nameof(MasterRecord)} {_id}";

        [CsvIgnore]
        public string IgnoreMe { get; set; } = $"{nameof(IgnoreMe)} {_id}";

        public decimal? Decimal { get; set; }
        public double? Double { get; set; }
        public float? Float { get; set; }
    }
}
