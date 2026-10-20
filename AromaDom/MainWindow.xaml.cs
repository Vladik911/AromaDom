using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace AromaDom;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            Database.Initialize();
            Refresh();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }
    public static void Error(Exception ex) => MessageBox.Show("Не удалось выполнить действие. Проверьте данные и доступ к базе.\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    public void Refresh()
    {
        var table = Database.Query("SELECT m.*,t.Name TypeName,u.Name UnitName,COALESCE((SELECT SUM(Quantity) FROM Recipes WHERE MaterialId=m.Id),0) Required FROM Materials m JOIN MaterialTypes t ON t.Id=m.TypeId JOIN Units u ON u.Id=m.UnitId ORDER BY m.Name");
        var cards = new List<object>();
        foreach (DataRow row in table.Rows)
        {
            string N(string field) => double.Parse(row[field].ToString()!, CultureInfo.InvariantCulture).ToString("0.00");
            cards.Add(new { Id = row["Id"].ToString(), Heading = row["TypeName"] + " | " + row["Name"], MinimumText = "Минимальное количество: " + N("Minimum") + " " + row["UnitName"], StockText = "Количество на складе: " + N("Stock") + " " + row["UnitName"], PriceText = "Цена: " + N("Price") + " р / Единица измерения: " + row["UnitName"], RequiredText = "Требуемое количество: " + N("Required") + " " + row["UnitName"] });
        }
        MaterialsList.ItemsSource = cards;
        Summary.Text = "Материалов: " + table.Rows.Count;
    }
    void AddClick(object sender, RoutedEventArgs e) => OpenEditor(0);
    void EditClick(object sender, RoutedEventArgs e) => OpenEditor(int.Parse(((Button)sender).Tag.ToString()!));
    void OpenEditor(int id)
    {
        try
        {
            if (new MaterialWindow(id) { Owner = this }.ShowDialog() == true)
                Refresh();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }
    void ProductsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            new ProductsWindow(int.Parse(((Button)sender).Tag.ToString()!)) { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }
    void CalculatorClick(object sender, RoutedEventArgs e)
    {
        try
        {
            new CalculatorWindow { Owner = this }.ShowDialog();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }
    void ImportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выберите папку с пятью таблицами Excel" };
        if (dialog.ShowDialog() != true)
            return;
        if (MessageBox.Show("Импорт заменит справочники, материалы и рецептуры. Текущая база будет сохранена в резервную копию. Продолжить?", "Импорт данных", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            ExcelImport.Import(dialog.FolderName);
            Refresh();
            MessageBox.Show("Все пять таблиц импортированы.", "Импорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }
}
