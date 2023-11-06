using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DoAnPhanMem.Models;

namespace DoAnPhanMem.Controllers
{
    public class CartController : Controller
    {
        WebshopEntities db = new WebshopEntities();

        public ActionResult ViewCart()
        {
            var cart = this.GetCart();
            double discount = 0d;
            var listProduct = cart.Item1.ToList();
            ViewBag.Quans = cart.Item2;

            if (Session["Discount"] != null && Session["Discountcode"] != null)
            {
                var code = Session["Discountcode"].ToString();
                //UseDiscountCode(code);
                var discountupdatequan = db.Discounts.Where(d => d.discount_code == code).FirstOrDefault();
                if (discountupdatequan.quantity == 0 || discountupdatequan.discount_start >= DateTime.Now || discountupdatequan.discount_end <= DateTime.Now)
                {
                    Notification.setNotification3s("Mã giảm giá không thể sử dụng", "error");
                    return View(listProduct);
                }
                discount = Convert.ToDouble(Session["Discount"].ToString());
                ViewBag.Total = PriceSum();
                ViewBag.Discount = discount;

                return View(listProduct);
            }
            ViewBag.Total = PriceSum();
            ViewBag.Discount = discount;
            return View(listProduct);
        }
        public ActionResult AddToCart(int productId, int quantity)
        {
            var cart = Session["Cart"] as List<CartModel>;
            if (cart == null)
            {
                cart = new List<CartModel>();
            }
            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingItem = cart.FirstOrDefault(item => item.pro_id == productId);
            if (existingItem != null)
            {
                // Nếu sản phẩm đã tồn tại, cập nhật số lượng
                existingItem.quantity += quantity;
            }
            else
            {
                // Nếu sản phẩm chưa tồn tại, thêm mới vào giỏ hàng
                var product = db.Products.SingleOrDefault(p => p.status_ == "1" && p.pro_id == productId);
                if (product != null)
                {
                    var newItem = new CartModel
                    {
                        pro_id = product.pro_id,
                        pro_name = product.pro_name,
                        quantity = quantity,
                    };

                    cart.Add(newItem);
                }
            }
            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }
        private Tuple<List<Product>, List<int>> GetCart()
        {
            //check null 
            var cart = Session["Cart"] as List<CartModel>;
            if (cart == null)
            {
                cart = new List<CartModel>();
                Session["Cart"] = cart;
            }
            var productIds = new List<int>();
            var quantities = new List<int>();
            // Lấy mã sản phẩm & số lượng trong giỏ hàng
            foreach (var item in cart)
            {
                productIds.Add(item.pro_id);
                quantities.Add(item.quantity);
            }
            // Select sản phẩm để hiển thị
            var listProduct = new List<Product>();
            foreach (var id in productIds)
            {
                var product = db.Products.SingleOrDefault(p => p.status_ == "1" && p.pro_id == id);
                listProduct.Add(product);
            }
            return new Tuple<List<Product>, List<int>>(listProduct, quantities);
        }
        [HttpPost]
        public ActionResult UpdateCart(int productId, int quantity)
        {
            var cart = Session["Cart"] as List<CartModel>;
            if (cart == null)
            {
                cart = new List<CartModel>();
            }

            var existingItem = cart.FirstOrDefault(item => item.pro_id == productId);
            if (existingItem != null)
            {
                existingItem.quantity = quantity;
            }

            Session["Cart"] = cart;

            // Trả về dữ liệu JSON để xử lý trên phía client
            return Json(new { success = true });
        }
        [HttpPost]
        public ActionResult RemoveFromCart(int productId)
        {
            var cart = Session["Cart"] as List<CartModel>;
            if (cart != null)
            {
                var itemToRemove = cart.FirstOrDefault(item => item.pro_id == productId);
                if (itemToRemove != null)
                {
                    cart.Remove(itemToRemove);
                }
                Session["Cart"] = cart;
            }
            return Json(new { success = true });
        }
        //public ActionResult RemoveFromCart(int productId)
        //{
        //    var cart = Session["Cart"] as List<CartModel>;
        //    if (cart != null)
        //    {
        //        var itemToRemove = cart.FirstOrDefault(item => item.pro_id == productId);
        //        if (itemToRemove != null)
        //        {
        //            cart.Remove(itemToRemove);
        //        }
        //        Session["Cart"] = cart;
        //    }

        //    return RedirectToAction("ViewCart"); // Chuyển hướng lại trang giỏ hàng sau khi xóa sản phẩm
        //}
        public ActionResult Checkout()
        {
            var TK = Session["TaiKhoan"] as Account;
            if (TK == null)
            {
                Notification.setNotification3s("Chức năng này yêu cầu đăng nhập", "error");
                return RedirectToAction("Login", "Account");

            }
            int userId = TK.acc_id;
            var user = db.Accounts.SingleOrDefault(u => u.acc_id == userId);
            var cart = this.GetCart();
            ViewBag.Quans = cart.Item2;
            ViewBag.ListProduct = cart.Item1.ToList();
            if (cart.Item2.Count < 1)
            {
                Notification.setNotification3s("Không có sản phẩm nào để thanh toán", "error");
                return RedirectToAction(nameof(ViewCart));
            }
            double discount = 0d;

            if (Session["Discount"] != null)
            {
                discount = Convert.ToDouble(Session["Discount"].ToString());
            }
            ViewBag.Total = PriceSum();
            ViewBag.Discount = discount;
            return View(user);
        }
        public ActionResult UseDiscountCode(string code)
        {
            var discount = db.Discounts.SingleOrDefault(d => d.discount_code == code);
            if (discount != null)
            {
                if (discount.discount_start < DateTime.Now && discount.discount_end > DateTime.Now && discount.quantity != 0)
                {
                    Session["Discountcode"] = discount.discount_code;
                    Session["Discount"] = discount.discount_price;
                    return Json(new { success = true, discountPrice = discount.discount_price }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { success = false, discountPrice = 0 }, JsonRequestBehavior.AllowGet);
        }
        private double PriceSum()
        {
            var cart = GetCart();
            var products = cart.Item1;
            double total = 0d;
            double discount = 0d;
            double productPrice = 0d;
            for (int i = 0; i < products.Count; i++)
            {
                var item = products[i];
                productPrice = item.price;
                if (item.Discount != null)
                {
                    if (item.Discount.discount_start < DateTime.Now && item.Discount.discount_end > DateTime.Now)
                    {
                        productPrice = item.price - item.Discount.discount_price;
                    }
                }
                total += productPrice * cart.Item2[i];
            }
            if (Session["Discount"] != null)
            {
                discount = Convert.ToDouble(Session["Discount"].ToString());
                total -= discount;
            }
            return total;
        }
        public ActionResult CartPartial()
        {
            var cart = this.GetCart();
            ViewBag.Count = cart.Item1.Count();
            return PartialView();
        }
        public async ActionResult SaveOrder(string note, string emailID, string orderID, string orderItem, string orderDiscount, string orderPrice, string orderTotal)
        {
            var TK = Session["TaiKhoan"] as Account;
            try
            {
                var culture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
                double priceSum = 0;

                string productquancheck = "0";
                if (Session["Discount"] != null && Session["Discountcode"] != null)
                {
                    string check_discount = Session["Discountcode"].ToString();
                    var discountupdatequan = db.Discounts.Where(d => d.discount_code == check_discount).SingleOrDefault();
                    int newquantity = (discountupdatequan.quantity - 1);
                    discountupdatequan.quantity = newquantity;
                }
                var cart = this.GetCart();
                var listProduct = new List<Product>();
                var order = new Order()
                {
                    acc_id = TK.acc_id,
                    oder_date = DateTime.Now,
                    status = "1",
                    order_note = Request.Form["OrderNote"].ToString(),
                    delivery_id = 1,
                    orderAddressId = orderAdress.orderAddressId,
                    oder_date = DateTime.Now,
                    update_at = DateTime.Now,
                    payment_id = 1,
                    update_by = User.Identity.GetUserId().ToString(),
                    total = Convert.ToDouble(TempData["Total"])
                };
                for (int i = 0; i < cart.Item1.Count; i++)
                {
                    var item = cart.Item1[i];
                    var _price = item.price;
                    if (item.Discount != null)
                    {
                        if (item.Discount.discount_star < DateTime.Now && item.Discount.discount_end > DateTime.Now)
                        {
                            _price = item.price - item.Discount.discount_price;
                        }
                    }
                    order.Oder_Detail.Add(new Oder_Detail
                    {
                        create_at = DateTime.Now,
                        create_by = User.Identity.GetUserId().ToString(),
                        disscount_id = item.disscount_id,
                        genre_id = item.genre_id,
                        price = _price,
                        product_id = item.product_id,
                        quantity = cart.Item2[i],
                        status = "1",
                        update_at = DateTime.Now,
                        update_by = User.Identity.GetUserId().ToString(),
                        transection = "transection"
                    });
                    // Xóa cart
                    Response.Cookies["product_" + item.product_id].Expires = DateTime.Now.AddDays(-10);
                    // Thay đổi số lượng và số lượt mua của product 
                    var product = db.Products.SingleOrDefault(p => p.product_id == item.product_id);
                    productquancheck = product.quantity;
                    product.buyturn += cart.Item2[i];
                    product.quantity = (Convert.ToInt32(product.quantity ?? "0") - cart.Item2[i]).ToString();
                    listProduct.Add(product);
                    priceSum += (_price * cart.Item2[i]);
                    orderItem += "<tr style='margin'> <td align='left' width='75%' style=' padding: 6px 12px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;' >" +
                                product.product_name + "</td><td align='left' width='25%' style=' padding: 6px 12px; font-family: 'Source Sans Pro', Helvetica, Arial, sans-serif; font-size: 16px; line-height: 24px; ' >" + product.price.ToString("#,0₫", culture.NumberFormat) + "</td> </tr>";
                }
                //thêm dữ liệu vào table
                if (productquancheck.Trim() != "0")
                {
                    db.Orders.Add(order);
                }
                else
                {
                    Notification.setNotification3s("Sản phẩm đã hết hàng", "error");
                    return RedirectToAction("ViewCart", "Cart");
                }
                db.Configuration.ValidateOnSaveEnabled = false;

                await db.SaveChangesAsync();
                Notification.setNotification3s("Đặt hàng thành công", "success");
                Session.Remove("Discount");
                Session.Remove("Discountcode");
                emailID = TK.email;
                orderID = order.order_id.ToString();
                orderDiscount = (priceSum + 30000 - order.total).ToString("#,0₫", culture.NumberFormat);
                orderPrice = priceSum.ToString("#,0₫", culture.NumberFormat);
                orderTotal = order.total.ToString("#,0₫", culture.NumberFormat);
                Notification.setNotification3s("Đặt hàng thành công", "success");
                return RedirectToAction("TrackingOrder", "Account");
            }
            catch
            {
                Notification.setNotification3s("Lỗi! đặt hàng không thành công", "error");
                return RedirectToAction("Checkout", "Cart");
            }
        }
    }
}