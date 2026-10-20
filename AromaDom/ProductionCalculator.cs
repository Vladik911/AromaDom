using System.Globalization;
namespace AromaDom;
public static class ProductionCalculator
{
    public static int Calculate(int productTypeId, int materialTypeId, int rawQuantity, double firstParameter, double secondParameter)
    {
        if (rawQuantity < 0 || !double.IsFinite(firstParameter) || !double.IsFinite(secondParameter) || firstParameter <= 0 || secondParameter <= 0)
            return -1;
        var products = Database.Query($"SELECT Coefficient FROM ProductTypes WHERE Id={productTypeId}");
        var materials = Database.Query($"SELECT Loss FROM MaterialTypes WHERE Id={materialTypeId}");
        if (products.Rows.Count == 0 || materials.Rows.Count == 0)
            return -1;
        var coefficient = double.Parse(products.Rows[0][0].ToString()!, CultureInfo.InvariantCulture);
        var loss = double.Parse(materials.Rows[0][0].ToString()!, CultureInfo.InvariantCulture);
        var perItem = firstParameter * secondParameter * coefficient;
        if (!double.IsFinite(perItem) || perItem <= 0)
            return -1;
        var result = Math.Floor(rawQuantity * (1 - loss) / perItem);
        return double.IsFinite(result) && result <= int.MaxValue ? (int)result : -1;
    }
}
