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
        public ActionResult SaveOrder(string note, string orderID, string orderItem, string orderDiscount, string orderPrice, string orderTotal, string oder_address)
        {
            var TK = Session["TaiKhoan"] as Account;
            try
            {
                var culture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
                double priceSum = 0;

                int productquancheck = 0;
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
                    oder_address = oder_address,
                    payment_id = 1,
                    total = Convert.ToDouble(TempData["Total"])
                };

                for (int i = 0; i < cart.Item1.Count; i++)
                {
                    var item = cart.Item1[i];
                    var _price = item.price;
                    if (item.Discount != null)
                    {
                        if (item.Discount.discount_start < DateTime.Now && item.Discount.discount_end > DateTime.Now)
                        {
                            _price = item.price - item.Discount.discount_price;
                        }
                    }
                    order.Oder_Detail.Add(new Oder_Detail
                    {
                        pro_id = item.pro_id,
                        discount_id = item.discount_id,
                        cate_id = item.cate_id,
                        price = _price,
                        quantity = cart.Item2[i],
                        status = "1",
                    });
                    //xóa giỏ hàng
                    Session["Cart"] = null;

                    // Thay đổi số lượng và số lượt mua của product 
                    var product = db.Products.SingleOrDefault(p => p.pro_id == item.pro_id);
                    productquancheck = product.quantity;
                    product.buyturn += cart.Item2[i];
                    product.quantity = product.quantity - cart.Item2[i];
                    listProduct.Add(product);
                    priceSum += (_price * cart.Item2[i]);
                }
                //thêm dữ liệu vào table
                if (productquancheck ==  0)
                {
                    db.Orders.Add(order);
                }
                else
                {
                    Notification.setNotification3s("Sản phẩm đã hết hàng", "error");
                    return RedirectToAction("ViewCart", "Cart");
                }
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChangesAsync();
                Session.Remove("Discount");
                Session.Remove("Discountcode");
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