using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using DoggoShopClient.Models;

namespace WebRazor.Pages.Admin.Product
{
    using Product = DoggoShopClient.Models.Product;

    [Authorize(Roles = "Employee")]
    public class EditModel : PageModel
    {
        [BindProperty] public Product                               Product { get; set; }
        public                List<Category> Categories;
        private readonly      IHubContext<HubServer>                hubContext;
        private               HttpClient                            client = new();

        public EditModel(IHubContext<HubServer> hubContext) { this.hubContext = hubContext; }

        public async Task LoadProduct(int? id)
        {
            var response = await this.client.GetAsync("https://localhost:5000/api/Category");
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Categories = JsonSerializer.Deserialize<List<Category>>(data, options);
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var response = await this.client.GetAsync($"https://localhost:5000/api/Product/getActiveProductById/{id}");
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Product = JsonSerializer.Deserialize<Product>(data, options);

            await LoadProduct(id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            await LoadProduct(id);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await this.client.GetAsync($"https://localhost:5000/api/Product/getActiveProductById/{id}");
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var product = JsonSerializer.Deserialize<Product>(data, options);
            product.ProductName     = Product.ProductName;
            product.CategoryId      = Product.CategoryId;
            product.UnitPrice       = Product.UnitPrice;
            product.QuantityPerUnit = Product.QuantityPerUnit;
            product.UnitsInStock    = Product.UnitsInStock;

            ViewData["success"] = "Update successfully";
            var         json    = Newtonsoft.Json.JsonConvert.SerializeObject(product);
            HttpContent content = new StringContent(json);
            await this.client.PostAsync("https://localhost:5000/api/Product/", content);
            await hubContext.Clients.All.SendAsync("Reload");

            return Page();
        }

        public async Task<IActionResult> OnGetDelete(int? id)
        {
            // Product = await dbContext.Products.Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.ProductId == (int)id);
            //
            // Product.DeletedAt = DateTime.Now;
            //
            // await dbContext.SaveChangesAsync();
            // await hubContext.Clients.All.SendAsync("Reload");
        
            return Redirect("/Admin/Product/Index");
        }
    }
}