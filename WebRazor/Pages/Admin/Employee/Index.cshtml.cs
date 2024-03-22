namespace WebRazor.Pages.Admin.Employee
{
    using System.Text;
    using System.Text.Json;
    using DoggoShopClient.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using WebRazor.Materials;
    using Employee = DoggoShopClient.Models.Employee;

    [Authorize(Roles = "Employee")]
    public class IndexModel : PageModel
    {
        private readonly PRN221DBContext dbContext;
        private          HttpClient      client;

        public IndexModel(PRN221DBContext dbContext)
        {
            this.dbContext = dbContext;
            this.client    = new HttpClient();
        }

        [FromQuery(Name = "page")] public int Page { get; set; } = 1;
        private int perPage = 10;
        public List<Employee> Employees  { get; set; }
        [FromQuery(Name = "txtSearch")] public string Search { get; set; } = "";

        public List<String> PagesLink { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {

            await Load();

            return Page();
        }

        public async Task Load()
        {
            if (Search == null) Search = "";

            var result = await this.client.GetAsync("https://localhost:5000/api/Employee/GetAll");
            var data   = await result.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var employees = JsonSerializer.Deserialize<List<Employee>>(data, options);


            Employees = employees
                .OrderByDescending(p => p.EmployeeId)
                .Skip((Page - 1) * perPage).Take(perPage)
                .ToList();

            PageLink page = new PageLink(perPage);
            PagesLink = page.getLink(Page, employees.Count, "/Admin/Employee/Index?");
        }

        public async Task<IActionResult> OnGetActive(int? id)
        {
            
            var response = await this.client.GetAsync("https://localhost:5000/api/Employee/Get/" + id);
            var data     = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var employee = JsonSerializer.Deserialize<Employee>(data, options);
            if (employee != null)
            {
                employee.Active = !employee.Active;
                var content = new StringContent(JsonSerializer.Serialize(employee), Encoding.UTF8, "application/json");
                await this.client.PutAsync("https://localhost:5000/api/Employee/Add/", content);
            }

            return this.Redirect("/Admin/Employee/Index");
        }
    }
}
