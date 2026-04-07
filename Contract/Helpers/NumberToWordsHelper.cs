namespace Contract;

public static class NumberToWordsHelper
{
    private static readonly string[] Ones = ["", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười", "mười một", "mười hai", "mười ba", "mười bốn", "mười lăm", "mười sáu", "mười bảy", "mười tám", "mười chín"];
    private static readonly string[] Tens = ["", "", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi"];

    /// <summary>
    /// Chuyển số thành chữ tiếng Việt.
    /// Ví dụ: 1500000 → "một triệu năm trăm nghìn đồng"
    /// </summary>
    public static string ConvertToWords(decimal number)
    {
        if (number == 0) return "không đồng";

        long wholePart = (long)number;
        int decimalPart = (int)((number - wholePart) * 100);

        string result = ConvertWholeNumber(wholePart);

        if (decimalPart > 0)
            result += " phẩy " + ConvertWholeNumber(decimalPart);

        return result + " đồng";
    }

    private static string ConvertWholeNumber(long number)
    {
        if (number == 0) return "không";
        if (number < 20) return Ones[number];

        if (number < 100)
        {
            int tens = (int)(number / 10);
            int ones = (int)(number % 10);
            if (ones == 0) return Tens[tens];
            if (ones == 5) return Tens[tens] + " lăm";
            if (ones == 1 && tens > 1) return Tens[tens] + " mốt";
            return Tens[tens] + " " + Ones[ones];
        }

        if (number < 1000)
        {
            int hundreds = (int)(number / 100);
            long remainder = number % 100;
            string result = Ones[hundreds] + " trăm";
            if (remainder == 0) return result;
            if (remainder < 10) return result + " linh " + Ones[remainder];
            return result + " " + ConvertWholeNumber(remainder);
        }

        if (number < 1_000_000)
        {
            int thousands = (int)(number / 1000);
            long remainder = number % 1000;
            string result = ConvertWholeNumber(thousands) + " nghìn";
            if (remainder == 0) return result;
            if (remainder < 100) return result + " không trăm " + ConvertWholeNumber(remainder);
            return result + " " + ConvertWholeNumber(remainder);
        }

        if (number < 1_000_000_000)
        {
            long millions = number / 1_000_000;
            long remainder = number % 1_000_000;
            string result = ConvertWholeNumber(millions) + " triệu";
            if (remainder == 0) return result;
            return result + " " + ConvertWholeNumber(remainder);
        }

        long billions = number / 1_000_000_000;
        long remainderBillions = number % 1_000_000_000;
        string resultBillions = ConvertWholeNumber(billions) + " tỷ";
        if (remainderBillions == 0) return resultBillions;
        return resultBillions + " " + ConvertWholeNumber(remainderBillions);
    }
}
