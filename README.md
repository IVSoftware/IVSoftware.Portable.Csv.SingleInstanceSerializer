## IVSoftware.Portable.Csv.SingleInstanceSerializer

These extensions reflect the properties of a given class T. The basic capabilities are:

- Generate a comma-delimited header string, based on the property names of class T.
- Given an instance of class T, generate a comma-delimited string containing values corresponding to the order and count of the generated header.
- Given a comma-delimited string containing values corresponding to the order and count of the generated header, generate a class instance.

The `[CsvIgnore]` attribute makes a property invisible to header and values generators.

___

Version 1.0.3 adds advanced capability:

- Extract an instance from a CSV file where the header and values do not strictly follow the format of the generated header. In this case, a class can be designed with properties that correspond to the values of interest in the file. Other values in other columns will be safely ignored. In general, this would be used to compose a Master Record that cherry picks values from multiple csv files.

The `[HeaderText]` attribute makes a property mappable to header names in the file that are different from the property names in the class.

The type conversion has been greatly expanded, and now supports almost any System type that has a static `Parse` method.


#### Methods
```
/// <summary>
/// Enumerates the public R/W property names of an instance of @type as comma-delimited string.
/// </summary>
/// <returns>Comma delimited list of names.</returns>
public static string GetCsvHeader(this Type @type);

/// <summary>
/// Enumerates the public R/W property names of an instance of @type as array.
/// </summary>
/// <returns>String array of names.</returns>
public static string[] GetCsvHeaderArray(this Type @type);

/// <summary>
/// Deserializes an instance of @type from comma delimited string 
/// based on names obtained from GetCsvHeader().
/// </summary>
/// <returns>Instance of type T</returns>
public static T FromCsvLine<T>(this Type @type, string csvLine);

/// <summary>
/// Deserializes an instance of @type from comma delimited string 
/// based on names obtained from GetCsvHeader().
/// </summary>
/// <returns>Comma delimited enumuration of public R/W property values.</returns>
public static string ToCsvLine<T>(this T instance);

/// <summary>
/// Fuzzy deserialization of an instance of @type from comma delimited string, meaning
/// that it's tolerant of names in header that aren't directly mapped to the object.
/// </summary>
/// <returns>Instance of type T</returns>
public static T Extract<T>(this Type @type, string unqualifiedHeader, string csvLine, bool ignoreCase = false);
```

#### Attributes

```
/// <summary>
/// Opt-out this property from Header and Instance serialization.
/// </summary>
[CsvIgnore]

/// <summary>
/// Header text mapping, if different from Property name or if spaces are present.
/// </summary>
[HeaderText]

/// <summary>
/// Format string for ToString() and ParseExact methods
/// </summary>
public class StringFormatAttribute : Attribute
{
    public StringFormatAttribute(string value) => Value = value;
    public string Value { get; }
}
```

#### Usage

##### Modifier Keys:
- [Control] Reads file using Extract instead of FromCsvLine (which requires a strict header)
- [ALT] Opens loopback in System Default App (e.g. MS Excel) instead of Notepad.

```
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

                string[] lines = File.ReadAllLines(Output);

                if (ModifierKeys.HasFlag(Keys.Control))
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
}
```
___
**Test Class v 1.0.3**

```
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
    public string IgnoreMe { get; set; }

    [StringFormat("F4")]
    public double? Double { get; set; }
    public float? Float { get; set; }

    [StringFormat("F2")]
    public decimal? Decimal { get; set; }
    public object Object { get; set; }
}
```

#### StackOverflow

[Populate a ListBox with CSV values from a custom class](https://stackoverflow.com/q/77514872/5438626) [[README](https://github.com/IVSoftware/serialization-binding-intro)]
_Use Startup Project = 'serialization-intro'_

[Combine CSVs with different columns](https://stackoverflow.com/q/77696298/5438626)
_Use Startup Project = 'combine-csvs-with-column-mapping'_