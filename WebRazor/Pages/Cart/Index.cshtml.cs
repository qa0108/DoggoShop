using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Transactions;
using System.Security.Claims;
using System.Text;
using DoggoShopClient.Models;

namespace WebRazor.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private HttpClient client;
        private string AccountApiUrl;
        private string ProductApiUrl;
        private string CustomerApiUrl;
        private string OrdersApiUrl;
        private string OrderDetailApiUrl;
        public Dictionary<DoggoShopClient.Models.Product, int> Cart { get; set; } = new Dictionary<DoggoShopClient.Models.Product, int>();

        [BindProperty]
        public Customer? Customer { get; set; }

        public decimal Sum { get; set; } = 0;


        public bool Disable = false;

        private DoggoShopClient.Models.Order Order;

        public IndexModel()
        {
            this.client = new HttpClient();
        }

        #region Load Info
        public async Task<bool> checkLogin()
        {
            try
            {
                var accId = Int32.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                AccountApiUrl = "https://localhost:5000/api/Account/id/" + accId;
                var response = await client.GetAsync(AccountApiUrl);
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var auth = JsonSerializer.Deserialize<DoggoShopClient.Models.Account>(data, options);

                CustomerApiUrl = "https://localhost:5000/api/customer/getCustomerById/" + auth.CustomerId;
                var responseCus = await client.GetAsync(CustomerApiUrl);
                var dataCus = await responseCus.Content.ReadAsStringAsync();
                Customer = JsonSerializer.Deserialize<DoggoShopClient.Models.Customer>(dataCus, options);
                Customer.Accounts.Add(auth);

                Disable = true;
                return true;
            } catch (Exception e)
            {
                return false;
            }

        }

        private Dictionary<int, int> getCart()
        {
            String cart = HttpContext.Session.GetString("cart");


            Dictionary<int, int> list;

            if (cart != null)
            {
                list = JsonSerializer.Deserialize<Dictionary<int, int>>(cart);
            }
            else
            {
                list = new Dictionary<int, int>();
            }

            return list;
        }

        public async Task LoadCartAsync(Dictionary<int, int> list)
        {
            foreach (var item in list)
            {
                DoggoShopClient.Models.Product product = await getProductAsync(item.Key);

                Cart.Add(product, item.Value);

                Sum += (decimal)product.UnitPrice * item.Value;
            }
        }

        public async Task LoadInfo()
        {
            await checkLogin();

            var listIdCart = getCart();

            await LoadCartAsync(listIdCart);
        }
        #endregion

        public async Task<IActionResult> OnGet()
        {
            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            await LoadInfo();

            return Page();
        }

        #region Order with cart
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<Customer> AddCustomer()
        {
            Customer customer = new Customer();
            customer.CustomerId = RandomString(5);

            CustomerApiUrl = "https://localhost:5000/api/Customer/getCustomerById/" + customer.CustomerId;
            var responseCus = await client.GetAsync(CustomerApiUrl);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var dataCus = await responseCus.Content.ReadAsStringAsync();
            var isCustomerIdExist = JsonSerializer.Deserialize<bool>(dataCus, options);
            // Check unique customerId
            while (!isCustomerIdExist)
            {
                customer.CustomerId = RandomString(5);
            }
            
            customer.ContactName = Customer.ContactName;
            customer.Address = Customer.Address;
            customer.CompanyName = Customer.CompanyName;
            customer.ContactTitle = Customer.ContactTitle;
            customer.CreatedAt = DateTime.Now;
            CustomerApiUrl = "https://localhost:5000/api/customer";
            var cusJson = JsonSerializer.Serialize(customer, options);
            var content = new StringContent(cusJson, Encoding.UTF8, "application/json");
            await client.PostAsync(AccountApiUrl, content);
            CustomerApiUrl = "https://localhost:5000/api/customer/getLastCustomer";
            var responseLastCus = await client.GetAsync(CustomerApiUrl);
            var dataLastCus = await responseLastCus.Content.ReadAsStringAsync();
            var lastCus = JsonSerializer.Deserialize<Customer>(dataLastCus, options);
            return lastCus;
        }

        public async Task<DoggoShopClient.Models.Order> AddOrder()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            if (Customer.CustomerId == null)
            {
                Customer = await AddCustomer();
            }

            DoggoShopClient.Models.Order order = new DoggoShopClient.Models.Order();
            order.CustomerId = Customer.CustomerId;
            order.OrderDate = DateTime.Now;
            order.RequiredDate = DateTime.Now.AddDays(7);

            OrdersApiUrl = "https://localhost:5000/api/Order";
            var orderJson = JsonSerializer.Serialize(order);
            var content = new StringContent(orderJson, Encoding.UTF8, "application/json");
            await client.PostAsync(OrdersApiUrl, content);
            OrdersApiUrl = "https://localhost:5000/api/Order/getLastOrder";
            var response = await client.GetAsync(OrdersApiUrl);
            var data = await response.Content.ReadAsStringAsync();
            var lastOrder = JsonSerializer.Deserialize<DoggoShopClient.Models.Order>(data, options);
            return lastOrder;
        }

        public async Task<decimal> AddOrderDetail(int key, int value, DoggoShopClient.Models.Order order)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            DoggoShopClient.Models.Product product = await getProductAsync(key);

            if (product.UnitsInStock - (short)value < 0)
            {
                throw new Exception(product.ProductName + " not enough quantity. Units in stock:" + product.UnitsInStock);
            }
            product.UnitsInStock -= (short)value;
            product.UnitsOnOrder += (short)value;

            OrderDetailApiUrl = "https://localhost:5000/api/OrderDetail/getOrderCount/" + Customer.CustomerId + "/" + product.ProductId;
            var response = await client.GetAsync(OrderDetailApiUrl);
            var data = await response.Content.ReadAsStringAsync();
            var orderCount = JsonSerializer.Deserialize<int>(data, options);
            if (orderCount > 0)
            {
                product.ReorderLevel += 1;
            }

            OrderDetail od = new OrderDetail();
            od.OrderId = order.OrderId;
            od.ProductId = product.ProductId;
            od.Quantity = (short)value;
            od.UnitPrice = (decimal)product.UnitPrice;
            od.Discount = 0;
            OrderDetailApiUrl = "https://localhost:5000/api/OrderDetail";
            var odJson = JsonSerializer.Serialize(od);
            var content = new StringContent(odJson, Encoding.UTF8, "application/json");
            var responseOd = await client.PostAsync(OrderDetailApiUrl, content);
            if (!responseOd.IsSuccessStatusCode)
            {
                ViewData["fail"] = responseOd.ReasonPhrase;
            }
            return od.UnitPrice * od.Quantity;
        }
        #endregion

        public async Task<IActionResult> OnPost()
        {

            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadInfo();
                return Page();
            }
            var listIdCart = getCart();

            if (listIdCart.Count == 0)
            {
                await LoadInfo();
                return Page();
            }

            await checkLogin();

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                decimal Freight = 0;
                try
                {
                    DoggoShopClient.Models.Order order = await AddOrder();

                    foreach (var item in listIdCart)
                    {
                        Freight += await AddOrderDetail(item.Key, item.Value, order);
                    }

                    order.Freight = Freight;
                    OrdersApiUrl = "https://localhost:5000/api/Order/updateFreight/" + order.OrderId;
                    var orderJson = JsonSerializer.Serialize(order);
                    var data = new StringContent(orderJson, Encoding.UTF8, "application/json");
                    var response = await client.PutAsync(OrdersApiUrl, data);
                    if (!response.IsSuccessStatusCode)
                    {
                        ViewData["fail"] = "Something were wrong " + response.StatusCode;
                    }
                    ViewData["success"] = "Order successfull";
                    listIdCart.Clear();
                    HttpContext.Session.Remove("cart");
                    HttpContext.Session.SetInt32("cartSize", listIdCart.Count);
                    transaction.Complete();
                    Order = order;

                }
                catch (Exception e)
                {
                    transaction.Dispose();

                    ViewData["fail"] = e.Message;
                }
            }

            if (Customer.Accounts.Count > 0)
            {
            }

            await LoadInfo();

            return Page();
        }

        public async Task<DoggoShopClient.Models.Product?> getProductAsync(int? id)
        {
            if (id == null)
            {
                return null;
            }

            ProductApiUrl = "https://localhost:5000/api/Product/getActiveProductById/" + id;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = await client.GetAsync(ProductApiUrl);
            var data = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<DoggoShopClient.Models.Product>(data, options);

            return product;
        }

        #region Action handler
        public async Task<IActionResult> OnGetAdd(int? id)
        {
            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            DoggoShopClient.Models.Product product = await getProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            if (product == null || product.UnitsInStock == 0)
            {
                TempData["fail"] = "Quantity = 0";
            }
            else

                try
                {
                    var listIdCart = getCart();

                    if ((listIdCart.Where(p => p.Key == id)).Count() == 0)
                    {
                        listIdCart.Add((int)id, 1);
                    }


                    HttpContext.Session.SetString("cart", JsonSerializer.Serialize(listIdCart));
                    TempData["success"] = "Add to cart successfull";

                    int size = listIdCart.Count;
                    HttpContext.Session.SetInt32("cartSize", size);
                }
                catch (Exception e)
                {
                    TempData["fail"] = e.Message;
                }

            return Redirect("/Product/Detail/" + id);
        }

        public async Task<IActionResult> OnGetDown(int? id)
        {
            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            DoggoShopClient.Models.Product product = await getProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            try
            {
                var listIdCart = getCart();

                if (listIdCart.ContainsKey((int)id))
                {
                    if (listIdCart[(int)id] == 1)
                    {
                        TempData["fail"] = "Quantity must > 1";
                        return Redirect("/Cart");
                    }
                    listIdCart[(int)id] -= 1;
                }


                HttpContext.Session.SetString("cart", JsonSerializer.Serialize(listIdCart));
                TempData["success"] = "Quantity down successfull";
            }
            catch (Exception e)
            {
                TempData["fail"] = e.Message;
            }

            return Redirect("/Cart");
        }

        public async Task<IActionResult> OnGetUp(int? id)
        {
            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            DoggoShopClient.Models.Product product = await getProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }


            try
            {
                var listIdCart = getCart();

                if (listIdCart.ContainsKey((int)id))
                {
                    listIdCart[(int)id] += 1;
                }


                HttpContext.Session.SetString("cart", JsonSerializer.Serialize(listIdCart));
                TempData["success"] = "Quanity up successfull";
            }
            catch (Exception e)
            {
                TempData["fail"] = e.Message;
            }

            return Redirect("/Cart");
        }

        public async Task<IActionResult> OnGetRemove(int? id)
        {
            var check = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (check != null && check.Value.Equals("Employee"))
            {
                return NotFound();
            }

            DoggoShopClient.Models.Product product = await getProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }


            try
            {
                var listIdCart = getCart();

                if ((listIdCart.Where(p => p.Key == id)).Count() != 0)
                {
                    listIdCart.Remove((int)id);
                }


                HttpContext.Session.SetString("cart", JsonSerializer.Serialize(listIdCart));
                TempData["success"] = "Remove from cart successfull";

                int size = listIdCart.Count;
                HttpContext.Session.SetInt32("cartSize", size);
            }
            catch (Exception e)
            {
                TempData["fail"] = e.Message;
            }

            return Redirect("/Cart");
        }
        #endregion
    }
}
