namespace ContainerDelivery.Core.Enums;

public enum DeliveryMethod
{
    BarcodeScan = 0,
    Manual = 1
}

public static class DeliveryMethodExtensions
{
    public static string ToDisplayString(this DeliveryMethod method, string language = "en")
    {
        return (method, language.ToLower()) switch
        {
            (DeliveryMethod.BarcodeScan, "ar") => "مسح الباركود",
            (DeliveryMethod.BarcodeScan, _) => "Barcode Scan",
            (DeliveryMethod.Manual, "ar") => "يدوي",
            (DeliveryMethod.Manual, _) => "Manual",
            _ => method.ToString()
        };
    }
}