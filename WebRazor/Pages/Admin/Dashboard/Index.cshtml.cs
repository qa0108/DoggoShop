namespace WebRazor.Pages.Admin.Dashboard
{
    using System.Text.Json;
    using DoggoShopClient.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = "Employee")]
    public class IndexModel : PageModel
    {
        private readonly PRN221DBContext dBContext;
        private          HttpClient      client;
        public IndexModel(PRN221DBContext dBContext)
        {
            this.dBContext = dBContext;
            this.client    = new HttpClient();
        }

        [FromQuery(Name = "txtDate")] public DateTime? Date { get; set; }

        public decimal TotalSales;
        public decimal WeeklySales;
        public decimal?[] StatisticOrders = {null, null, null, null, null, null, null, null, null, null, null, null};

        public int TotalCus;
        public int Guest;
        public float TotalCusMonth;
        public float GuestMonth;

        public async Task<IActionResult> OnGetAsync()
        {
            var api      = "https://localhost:5000/api/Order/GetAll";
            var response = await this.client.GetAsync(api);
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var Orders = JsonSerializer.Deserialize<List<Order>>(data, options);
            Orders     = Orders.Where(o => o.RequiredDate != null).ToList();
            TotalSales = (decimal)Orders.Sum(x => x.Freight);

            DateTime now = Date != null ? (DateTime) Date : DateTime.Now;
            ViewData["Date"] = now.Date.ToString("yyyy-MM-dd");
            DateTime sub = now.AddDays(-7);
            List<Order> OrdersWeek = Orders.Where(x => x.OrderDate >= sub
                                                       && x.OrderDate <= now).ToList();

            WeeklySales = (decimal)OrdersWeek.Sum(x => x.Freight);

            List<Order> OrderYear = Orders.Where(o => o.OrderDate.Value.Year == now.Year).ToList();

            for(int i = 0; i < now.Month; i++)
            {
                List<Order> OrdersMonth = OrderYear.Where(o => o.OrderDate.Value.Month == i + 1).ToList();
                StatisticOrders[i] = (decimal) OrdersMonth.Sum(x => x.Freight);
            }
            
            var cusApi      = "https://localhost:5000/api/Customer";
            response = await this.client.GetAsync(cusApi);
            data     = await response.Content.ReadAsStringAsync();
            var            Customer   = JsonSerializer.Deserialize<List<Customer>>(data, options);
            TotalCus = Customer.Count();
            TotalCusMonth = Customer.Where(x => x.CreatedAt != null && x.CreatedAt.Value.Year == now.Year).ToList().Count() / now.Month;

            Guest = Customer.Where(x => x.Accounts.Count == 0).Count();
            GuestMonth = Customer.Where(x => x.Accounts.Count == 0).Where(x => x.CreatedAt != null && x.CreatedAt.Value.Year == now.Year)
                .Count() / now.Month;

            return Page();
        }
    }
}
