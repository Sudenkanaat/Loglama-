using System;
using System.Data.SqlClient;
using System.IO;

namespace PaymentSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessPayment();
            Console.ReadLine();
        }
        private static void ProcessPayment()
        {
            try
            {
                // Kullanıcıdan kullanıcı ID'sini alma
                Console.WriteLine("Lütfen kullanıcı ID'sini giriniz:");
                string userId = Console.ReadLine();

                // Kullanıcı bilgilerini veritabanından çek
                var user = GetUserDetailsFromDatabase(userId);

                if (user == null)
                {
                    throw new Exception("Kullanıcı bulunamadı");
                }

                // Kullanıcı bilgilerini konsoldan aldıktan sonra diğer bilgileri al
                Console.WriteLine("Lütfen kart numaranızı giriniz (16 haneli):");
                user.CardNumber = Console.ReadLine();

                Console.WriteLine("Lütfen CVV kodunu giriniz (3 haneli):");
                user.Cvv = Console.ReadLine();

                Console.WriteLine("Lütfen son kullanma tarihini giriniz (MM/YY):");
                user.ExpiryDate = Console.ReadLine();

                Console.WriteLine("Lütfen ödeme tutarını giriniz:");
                user.Amount = Convert.ToDecimal(Console.ReadLine());

                // Kart bilgilerini doğrulama
                ValidateCardDetails(user.CardNumber, user.Cvv, user.ExpiryDate, user.Amount);

                // Tüm doğrulamalar başarılıysa ödeme başarılıdır
                Console.WriteLine("Ödeme başarılı");
            }
            catch (Exception hata)
            {
                LogError(hata);
            }
        }


        private static User GetUserDetailsFromDatabase(string userId)
        {
            string connectionString = "Server=LAPTOP-IN8EDKHP;Database=PaymentSystemDB;Integrated Security=True;";
            string query = "SELECT CardNumber, Cvv, ExpiryDate, Amount FROM Users WHERE UserId = @UserId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        return new User
                        {
                            CardNumber = reader["CardNumber"].ToString(),
                            Cvv = reader["Cvv"].ToString(),
                            ExpiryDate = reader["ExpiryDate"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"])
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    return null;
                }
            }
        }

        private static void ValidateCardDetails(string cardNumber, string cvv, string expiryDate, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length != 16 || !long.TryParse(cardNumber, out _))
            {
                throw new Exception("Geçersiz kart numarası");
            }

            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length != 3 || !int.TryParse(cvv, out _))
            {
                throw new Exception("Geçersiz CVV kodu");
            }

            if (string.IsNullOrWhiteSpace(expiryDate) || !DateTime.TryParseExact(expiryDate, "MM/yy", null, System.Globalization.DateTimeStyles.None, out DateTime expiry))
            {
                throw new Exception("Geçersiz son kullanma tarihi");
            }

            if (expiry < DateTime.Now)
            {
                throw new Exception("Kartın son kullanma tarihi geçmiş");
            }

            if (amount <= 0)
            {
                throw new Exception("Geçersiz ödeme tutarı");
            }
        }

        private static void LogError(Exception hata)
        {
            string logFilePath = "payment_log.txt";
            const long maxLogFileSize = 1024 * 1024; // 1 MB
            string logEntry = $"{Environment.UserName}\n" +
                              $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                              $"{hata.Message}\n" +
                              $"{hata.StackTrace}\n" +
                              "********************\n";

            try
            {
                // Log dosyasının boyutunu kontrol et ve gerekirse yeni dosya oluştur
                FileInfo logFileInfo = new FileInfo(logFilePath);
                if (logFileInfo.Exists && logFileInfo.Length > maxLogFileSize)
                {
                    string newLogFilePath = $"payment_log_{DateTime.Now:yyyyMMddHHmmss}.txt";
                    File.Move(logFilePath, newLogFilePath);
                }

                // Dosya kilitleme mekanizması kullanarak loglama
                using (FileStream fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.WriteLine(logEntry);
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"Log yazılırken bir hata oluştu: {ioEx.Message}");
            }
        }
    }

    class User
    {
        public string CardNumber { get; set; }
        public string Cvv { get; set; }
        public string ExpiryDate { get; set; }
        public decimal Amount { get; set; }
    }
}
