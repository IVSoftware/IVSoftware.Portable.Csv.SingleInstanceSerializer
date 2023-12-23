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


```
public MainForm()
{
    InitializeComponent();
    listBox.DataSource = Persons;
    listBox.DisplayMember = "Name";
    buttonToCsv.Click += (sender, e) =>
    {
        if (Persons.Any() || warnEmpty())
        {
            List<string> builder = new List<string>
            {
                typeof(Person).GetCsvHeader(),
            };
            foreach (var person in Persons)
            {
                builder.Add(person.ToCsvLine());
            }
            Directory.CreateDirectory(Path.GetDirectoryName(_filePathCsv));
            File.WriteAllLines(_filePathCsv, builder.ToArray());
            Process.Start("notepad.exe", _filePathCsv);
        }
    };
    buttonFromCsv.Click += async (sender, e) =>
    {
        UseWaitCursor = true;
        await localClearAndWait();
        if (File.Exists(_filePathCsv))
        {
            string[] lines = File.ReadAllLines(_filePathCsv);
            if (lines.FirstOrDefault() is string header)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (!string.IsNullOrEmpty(line))
                    {
                        Persons.Add(typeof(Person).FromCsvLine<Person>(line));
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Expecting header");
            }
        }
        UseWaitCursor = false;
    };
    async Task localClearAndWait()
    {
        if(Persons.Any())
        {
            Persons.Clear();
            await Task.Delay(1000); // Observe the cleared list
        }
    }
}
bool warnEmpty()
{
    return !DialogResult.Cancel.Equals(
        MessageBox.Show(
            "Person list is empty. Save anyway?",
            "Confirm",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question
        ));
}
BindingList<Person> Persons { get; } = new BindingList<Person>();
```

#### StackOverflow

[Populate a ListBox with CSV values from a custom class](https://stackoverflow.com/q/77514872/5438626) [[README](https://github.com/IVSoftware/serialization-binding-intro)]
_Use Startup Project = 'serialization-intro'_

[Combine CSVs with different columns](https://stackoverflow.com/q/77696298/5438626)
_Use Startup Project = 'combine-csvs-with-column-mapping'_