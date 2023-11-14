using DoggoShopClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace WebRazor.Pages.Admin.Order
{
    using System.Text.Json;
    using Order = DoggoShopClient.Models.Order;
    using Product = DoggoShopClient.Models.Product;

    [Authorize(Roles = "Employee")]
    public class DetailModel : PageModel
    {
        public DoggoShopClient.Models.Order Order { get; set;}

        private readonly PRN221DBContext dbContext;
        private          HttpClient      client;

        public int ID;

        public DetailModel(PRN221DBContext dbContext)
        {
            this.dbContext = dbContext;
            this.client    = new();
        }


        public async Task<IActionResult> OnGet(int? id)
        {

            this.ID = (int)id;

            var response = await this.client.GetAsync("https://localhost:5000/api/Order/GetAll/");
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            this.Order = JsonSerializer.Deserialize<List<Order>>(data, options).FirstOrDefault(x=>x.OrderId == id);

            response = await this.client.GetAsync("https://localhost:5000/api/Product/GetAll");
            data     = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Product>>(data, options);

            foreach (var item in this.Order.OrderDetails)
            {
                item.Product = products.Where(p => p.DeletedAt == null)
                    .FirstOrDefault(p => p.ProductId == item.ProductId);
            }

            return this.Page();
        }
    }
}
