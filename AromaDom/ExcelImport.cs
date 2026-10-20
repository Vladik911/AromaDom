using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
namespace AromaDom;
public static class ExcelImport
{
    static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static List<string[]> Read(string folder, string suffix, int columns)
    {
        var files = Directory.GetFiles(folder, "*.xlsx").Where(f => Path.GetFileName(f).EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (files.Length != 1)
            throw new Exception("В папке должен быть один файл " + suffix);
        using var zip = ZipFile.OpenRead(files[0]);
        var strings = new List<string>();
        var shared = zip.GetEntry("xl/sharedStrings.xml");
        if (shared != null)
        {
            using var stream = shared.Open();
            strings = XDocument.Load(stream).Descendants(Ns + "si").Select(x => string.Concat(x.Descendants(Ns + "t").Select(t => t.Value))).ToList();
        }
        var entry = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Не найден первый лист: " + suffix);
        using var sheet = entry.Open();
        var result = new List<string[]>();
        foreach (var row in XDocument.Load(sheet).Descendants(Ns + "row").Skip(1))
        {
            var values = new string[columns];
            Array.Fill(values, "");
            foreach (var cell in row.Elements(Ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? "";
                var index = 0;
                foreach (var letter in reference.TakeWhile(char.IsLetter))
                    index = index * 26 + letter - 'A' + 1;
                index--;
                if (index < 0 || index >= columns)
                    continue;
                var value = cell.Element(Ns + "v")?.Value ?? "";
                values[index] = (string?)cell.Attribute("t") switch
                {
                    "s" => strings[int.Parse(value)],
                    "inlineStr" => string.Concat(cell.Descendants(Ns + "t").Select(t => t.Value)),
                    _ => value
                };
            }
            if (values.All(string.IsNullOrWhiteSpace))
                continue;
            if (values.Any(string.IsNullOrWhiteSpace))
                throw new Exception("Обнаружены пустые поля в " + suffix + ", строка " + (string?)row.Attribute("r"));
            result.Add(values);
        }
        return result;
    }
    static string Number(string value)
    {
        if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number < 0)
            throw new Exception("Некорректное число в Excel: " + value);
        return number.ToString(CultureInfo.InvariantCulture);
    }
    public static void Import(string folder)
    {
        var types = Read(folder, "АД_Material_type_import.xlsx", 2);
        var productsTypes = Read(folder, "АД_Product_type_import.xlsx", 2);
        var materials = Read(folder, "АД_Materials_import.xlsx", 7);
        var products = Read(folder, "АД_Products_import.xlsx", 4);
        var recipes = Read(folder, "АД_Material_products_import.xlsx", 3);
        var original = Database.PathName;
        var backup = original + "." + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bak";
        File.Copy(original, backup);
        var statements = new List<string> { "DELETE FROM Recipes", "DELETE FROM Products", "DELETE FROM Materials", "DELETE FROM Units", "DELETE FROM MaterialTypes", "DELETE FROM ProductTypes" };
        string Q(string value) => Database.Quote(value.Trim());
        foreach (var r in types)
        {
            var loss = decimal.Parse(Number(r[1].Replace("%", "")), CultureInfo.InvariantCulture) / 100;
            statements.Add($"INSERT INTO MaterialTypes(Name,Loss) VALUES({Q(r[0])},{loss.ToString(CultureInfo.InvariantCulture)})");
        }
        foreach (var r in productsTypes)
            statements.Add($"INSERT INTO ProductTypes(Name,Coefficient) VALUES({Q(r[0])},{Number(r[1])})");
        foreach (var r in materials)
        {
            statements.Add($"INSERT OR IGNORE INTO Units(Name) VALUES({Q(r[6])})");
            statements.Add($"INSERT INTO Materials(Name,TypeId,Price,Stock,Minimum,Pack,UnitId) VALUES({Q(r[0])},(SELECT Id FROM MaterialTypes WHERE Name={Q(r[1])}),{Number(r[2])},{Number(r[3])},{Number(r[4])},{Number(r[5])},(SELECT Id FROM Units WHERE Name={Q(r[6])}))");
        }
        foreach (var r in products)
            statements.Add($"INSERT INTO Products(Article,TypeId,Name,Price) VALUES({Q(r[0])},(SELECT Id FROM ProductTypes WHERE Name={Q(r[1])}),{Q(r[2])},{Number(r[3])})");
        foreach (var r in recipes)
            statements.Add($"INSERT INTO Recipes VALUES((SELECT Id FROM Materials WHERE Name={Q(r[0])}),(SELECT Id FROM Products WHERE Name={Q(r[1])}),{Number(r[2])})");
        Database.ExecuteTransaction(statements);
    }
}
