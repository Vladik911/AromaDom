using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace AromaDom;
public class MaterialWindow : Window
{
    readonly int materialId;
    readonly ComboBox type = new() { DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    readonly ComboBox unit = new() { DisplayMemberPath = "Name", SelectedValuePath = "Id" };
    readonly TextBox name = new();
    readonly TextBox price = new();
    readonly TextBox pack = new();
    readonly TextBox stock = new();
    readonly TextBox minimum = new();
    public MaterialWindow(int id)
    {
        materialId = id;
        Title = id == 0 ? "Добавление материала" : "Редактирование материала";
        Width = 540;
        Height = 700;
        MinHeight = 500;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        type.ItemsSource = Database.Query("SELECT Id,Name FROM MaterialTypes ORDER BY Name").DefaultView;
        unit.ItemsSource = Database.Query("SELECT Id,Name FROM Units ORDER BY Name").DefaultView;
        void Field(string label, Control control)
        {
            panel.Children.Add(new TextBlock { Text = label });
            panel.Children.Add(control);
        }
        Field("Тип материала", type);
        Field("Наименование", name);
        Field("Цена единицы, руб.", price);
        Field("Единица измерения", unit);
        Field("Количество в упаковке", pack);
        Field("Количество на складе", stock);
        Field("Минимальное количество", minimum);
        var save = new Button { Content = "Сохранить", IsDefault = true };
        save.Click += SaveClick;
        panel.Children.Add(save);
        var back = new Button { Content = "Назад", IsCancel = true };
        back.Click += (_, _) => Close();
        panel.Children.Add(back);
        if (id == 0)
        {
            price.Text = "0";
            stock.Text = "0";
            minimum.Text = "0";
            pack.Text = "1";
            type.SelectedIndex = 0;
            unit.SelectedIndex = 0;
            return;
        }
        var row = Database.Query($"SELECT * FROM Materials WHERE Id={id}").Rows[0];
        name.Text = row["Name"].ToString();
        type.SelectedValue = row["TypeId"].ToString();
        unit.SelectedValue = row["UnitId"].ToString();
        price.Text = row["Price"].ToString();
        pack.Text = row["Pack"].ToString();
        stock.Text = row["Stock"].ToString();
        minimum.Text = row["Minimum"].ToString();
    }
    void SaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            decimal Number(TextBox box, string label, bool positive = false)
            {
                if (!decimal.TryParse(box.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value < 0 || (positive && value == 0))
                    throw new Exception(label + (positive ? ": введите число больше нуля." : ": введите число не меньше нуля."));
                return value;
            }
            if (string.IsNullOrWhiteSpace(name.Text) || type.SelectedValue == null || unit.SelectedValue == null)
                throw new Exception("Заполните название, выберите тип материала и единицу измерения.");
            var p = Number(price, "Цена");
            if (decimal.Round(p, 2) != p)
                throw new Exception("Цена должна содержать не более двух знаков после запятой.");
            var values = new[] { p, Number(stock, "Остаток"), Number(minimum, "Минимум"), Number(pack, "Упаковка", true) }.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray();
            var duplicate = Database.Query($"SELECT Id FROM Materials WHERE Name={Database.Quote(name.Text.Trim())} AND Id<>{materialId}");
            if (duplicate.Rows.Count > 0)
                throw new Exception("Материал с таким названием уже существует. Укажите другое название.");
            var fields = $"Name={Database.Quote(name.Text.Trim())},TypeId={int.Parse(type.SelectedValue.ToString()!)},UnitId={int.Parse(unit.SelectedValue.ToString()!)},Price={values[0]},Stock={values[1]},Minimum={values[2]},Pack={values[3]}";
            if (materialId == 0)
                Database.Query($"INSERT INTO Materials(Name,TypeId,UnitId,Price,Stock,Minimum,Pack) VALUES({Database.Quote(name.Text.Trim())},{int.Parse(type.SelectedValue.ToString()!)},{int.Parse(unit.SelectedValue.ToString()!)},{string.Join(',', values)})");
            else
                Database.Query($"UPDATE Materials SET {fields} WHERE Id={materialId}");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MainWindow.Error(ex);
        }
    }
}
