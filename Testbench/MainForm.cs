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
        string OutputAlt { get; } = Path.Combine(Folder, "OutputAlt.csv");
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
                    var titleBuilder = new List<string>();

                    File.WriteAllLines(Output, Recordset.GetAllLines());
                    if (ModifierKeys.HasFlag(Keys.Alt))
                    {
                        titleBuilder.Add("[Open With System Default App]");
                        // Open with system Default App for .csv e.g. MS Excel.
                        Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = Output, });
                    }
                    else
                    {
                        titleBuilder.Add("[Open With Notepad]");
                        Process.Start("notepad.exe", Output);
                    }

                    string[] lines;

                    if (Control.IsKeyLocked(Keys.CapsLock))
                    {
                        makeFuzzyRecordFile();
                        lines = File.ReadAllLines(OutputAlt);
                    }
                    else
                    {
                        lines = File.ReadAllLines(Output);
                    }

                    if (ModifierKeys.HasFlag(Keys.Control) || IsKeyLocked(Keys.CapsLock))
                    {
                        titleBuilder.Add("[Fuzzy Extract header]");
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
                    }
                    else
                    {
                        titleBuilder.Add("[Strict header]");
                        if (lines.FirstOrDefault() is string header)
                        {
                            Debug.Assert(
                                string.Equals(header, typeof(MasterRecord).GetCsvHeader()),
                                "Expecting a strict match when FromCsvLine is invoked."
                            );
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var line = lines[i];
                                if (!string.IsNullOrEmpty(line))
                                {
                                    MasterRecord record = typeof(MasterRecord).FromCsvLine<MasterRecord>(line);
                                    Recordset.Add(record);
                                }
                            }
                        }
#if false
                        // A test case.
                        // Basically, to prove that the Extract method 'does' work on an
                        // irregular file, it's necessary to show that FromCsvLine() 'doesn't'.

                        if(File.Exists(OutputAlt))
                        {
                            var failLines = File.ReadAllLines(OutputAlt);
                            if (failLines.FirstOrDefault() is string irregularHeader)
                            {
                                Debug.Assert(
                                    !string.Equals(irregularHeader, typeof(MasterRecord).GetCsvHeader()),
                                    "Expecting a discrepancy in headers."
                                );
                                try
                                {
                                    for (int i = 1; i < failLines.Length; i++)
                                    {
                                        var failLine = failLines[i];
                                        if (!string.IsNullOrEmpty(failLine))
                                        {
                                            MasterRecord record = typeof(MasterRecord).FromCsvLine<MasterRecord>(failLine);
                                            Recordset.Add(record);
                                        }
                                }
                                }
                                catch (Exception ex)
                                {
                                    // We ARE expecting this exception in this case. This is why
                                    // we use Extract to read files where the header is not a match.
                                    Debug.Fail(ex.Message);
                                }
                            }
                        }
#endif
                    }
                    Text = string.Join(string.Empty, titleBuilder.ToArray().Reverse());
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
                Name = "IV, Tommy",
                IgnoreMe = $"{nameof(MasterRecord.IgnoreMe)} 1",
            });
            Recordset.Add(new MasterRecord
            {
                Name = "Red, Green, Yellow, Blue",
                IgnoreMe = $"{nameof(MasterRecord.IgnoreMe)} 2",
            });
            Recordset.Add(new MasterRecord
            {
                Name = nameof(Math.PI),
                Double = Math.PI,
            });
            Recordset.Add(new MasterRecord
            {
                Object = "Unknown",
                Double = 1.2345678,
                Float = 2.34567890f,
                Decimal = 3.45678901m,
            });
        }
        BindingList<MasterRecord> Recordset { get; } = new BindingList<MasterRecord>();

        /// <summary>
        /// Makes a file that is irregular with respect to Master Record for testing purposes.
        /// </summary>
        private void makeFuzzyRecordFile()
        {
            var fuzzyRecordset = new List<FuzzyRecord>();
            fuzzyRecordset.AddRange(Enumerable.Range(0, 3).Select(_=> new FuzzyRecord()));

            File.WriteAllLines(OutputAlt, fuzzyRecordset.GetAllLines());
            Process.Start("notepad.exe", OutputAlt);
        }
    }

    class MasterRecord
    {
        public MasterRecord() => _id++;

        public int ID { get; } = _id;
        protected static int _id = 1;

        [StringFormat(@"yyyy.MM.dd")]

        [HeaderText("Formatted")]
        public DateTime DateTime { get; set; } = DateTime.Now;

        public DateOnly DateOnly { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public TimeOnly TimeOnly { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
        public int? Int32 { get; set; }

        [StringFormat("F4")]
        public double? Double { get; set; }
        public float? Float { get; set; }

        [StringFormat("F2")]
        public decimal? Decimal { get; set; }

        [CsvIgnore]
        public string IgnoreMe { get; set; }
        public object Object { get; set; }

        [HeaderText("Test String")]
        public string Name { get; set; } = $"{nameof(MasterRecord)} {_id}";
    }

    class FuzzyRecord
    {
        public FuzzyRecord() => _id++;

        public int ID { get; } = _id;
        protected static int _id = 1;

        [HeaderText("Test String")]
        public string TestString { get; set; } = $"{nameof(TestString)} {_id}";
        public string ExtraString { get; set; } = $"{nameof(ExtraString)} {_id}";
        public int ExtraInt { get; set; } = _id;

        public int Int32 { get; set; } = _rando.Next(10, 1000);

        private static Random _rando = new Random();
    }
}
