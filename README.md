## IVSoftware.Portable.Csv.SingleInstanceSerializer


#### Methods
```
/// <summary>
/// Enumerates the public R/W property names of an instance of @type.
/// </summary>
/// <returns>Comma delimited list of names.</returns>
public static string GetCsvHeader(this Type @type);

/// <summary>
/// Enumerates the public R/W property names of an instance of @type.
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
```

#### Attribute

```
/// <summary>
/// Opt-out this property from Header and Instance serialization.
/// </summary>
[CsvIgnore]
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

[Populate a ListBox with CSV values from a custom class](https://stackoverflow.com/a/77524987/5438626)