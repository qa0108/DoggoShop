using DoggoShopClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WebRazor.Pages.Admin.Product
{
    using System.Text;
    using Newtonsoft.Json;
    using JsonSerializer = System.Text.Json.JsonSerializer;

    [Authorize(Roles = "Employee")]
    public class CreateModel : PageModel
    {
        [BindProperty] public DoggoShopClient.Models.Product        Product { get; set; }
        public                List<Category> Categories;
        private readonly      PRN221DBContext                       dbContext;
        private readonly      IHubContext<HubServer>                hubContext;
        private               HttpClient                            client;

        public CreateModel(PRN221DBContext dbContext, IHubContext<HubServer> hubContext)
        {
            this.dbContext  = dbContext;
            this.hubContext = hubContext;
            this.client     = new HttpClient();
        }

        public async Task LoadProduct()
        {
            var api       = "https://localhost:5000/api/Category";
            var response = await this.client.GetAsync(api);
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            this.Categories = JsonSerializer.Deserialize<List<Category>>(data, options);
        }

        public async Task<IActionResult> OnGetAsync()
        {

            await LoadProduct();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            await LoadProduct();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var api       = "https://localhost:5000/api/Product";
            var productJson = JsonSerializer.Serialize(this.Product);
            var data      = new StringContent(productJson, Encoding.UTF8, "application/json");
            await client.PostAsync(api, data);
            
            await hubContext.Clients.All.SendAsync("Reload");

            return Redirect("/Admin/Product/Index");
        }
    }
}
