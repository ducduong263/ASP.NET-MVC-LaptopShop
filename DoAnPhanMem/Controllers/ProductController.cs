using DoAnPhanMem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Linq.Expressions;

namespace DoAnPhanMem.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        WebshopEntities db = new WebshopEntities();

        public ActionResult Index(int? page, string sortOrder)
        {
            return View(GetProduct(m => m.status_ == "1", page, sortOrder));
        }
        //Tìm kiếm sản phẩm
        public ActionResult SearchResult(int? page, string sortOrder, string s)
        {
            ViewBag.SortBy = "search?s=" + s + "&";
            ViewBag.Type = "Kết quả tìm kiếm - " + s;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.ResetSort = String.IsNullOrEmpty(sortOrder) ? "" : "";
            //ViewBag.DateAscSort = sortOrder == "date_asc" ? "date_asc" : "date_asc";
            //ViewBag.DateDescSort = sortOrder == "date_desc" ? "date_desc" : "date_desc";
            //ViewBag.PopularSort = sortOrder == "popular" ? "popular" : "popular";
            //ViewBag.PriceDescSort = sortOrder == "price_desc" ? "price_desc" : "price_desc";
            //ViewBag.PriceAscSort = sortOrder == "price_asc" ? "price_asc" : "price_asc";
            //ViewBag.NameAscSort = sortOrder == "name_asc" ? "name_asc" : "name_asc";
            //ViewBag.NameDescSort = sortOrder == "name_desc" ? "name_desc" : "name_desc";
            var list = db.Products.OrderByDescending(m => m.pro_id);
            switch (sortOrder)
            {
                case "price_asc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderBy(m => (m.price - m.Discount.discount_price)).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                case "price_desc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderByDescending(m => m.price - m.Discount.discount_price).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                case "date_asc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderBy(m => m.update_at).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                case "date_desc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderByDescending(m => m.update_at).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                case "name_asc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderBy(m => m.pro_name).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                case "name_desc":
                    list = (IOrderedQueryable<Product>)db.Products.OrderByDescending(m => m.pro_name).Where(m => m.status_ == "1" && m.pro_name.Contains(s));
                    break;
                default:
                    list = (IOrderedQueryable<Product>)db.Products.OrderByDescending(m => m.update_at);
                    break;
            }
            ViewBag.Countproduct = db.Products.Where(m => m.status_ == "1" && m.pro_name.Contains(s)).Count();
            return View("Index", GetProduct(m => m.status_ == "1" && (m.pro_name.Contains(s) || m.pro_id.ToString().Contains(s)), page, sortOrder));
        }
        private IPagedList GetProduct(Expression<Func<Product, bool>> expr, int? page, string sortOrder)
        {
            int pageSize = 9; //1 trang hiện thỉ tối đa 9 sản phẩm
            int pageNumber = (page ?? 1); //đánh số trang
            ViewBag.AvgFeedback = db.Feedbacks.ToList();
            ViewBag.OrderDetail = db.Oder_Detail.ToList();
            ViewBag.CurrentSort = sortOrder;
            ViewBag.ResetSort = String.IsNullOrEmpty(sortOrder) ? "" : "";
            ViewBag.DateAscSort = sortOrder == "date_asc" ? "date_asc" : "date_asc";
            ViewBag.DateDescSort = sortOrder == "date_desc" ? "date_desc" : "date_desc";
            ViewBag.PopularSort = sortOrder == "popular" ? "popular" : "popular";
            ViewBag.PriceDescSort = sortOrder == "price_desc" ? "price_desc" : "price_desc";
            ViewBag.PriceAscSort = sortOrder == "price_asc" ? "price_asc" : "price_asc";
            ViewBag.NameAscSort = sortOrder == "name_asc" ? "name_asc" : "name_asc";
            ViewBag.NameDescSort = sortOrder == "name_desc" ? "name_desc" : "name_desc";
            var list = db.Products.Where(expr).OrderByDescending(m => m.pro_id).ToPagedList(pageNumber, pageSize);
            switch (sortOrder)
            {
                case "price_asc":
                    list = db.Products.Where(expr).OrderBy(m => (m.price - m.Discount.discount_price)).ToPagedList(pageNumber, pageSize);
                    break;
                case "price_desc":
                    list = db.Products.Where(expr).OrderByDescending(m => (m.price - m.Discount.discount_price)).ToPagedList(pageNumber, pageSize);
                    break;
                case "date_asc":
                    list = db.Products.Where(expr).OrderBy(m => m.update_at).ToPagedList(pageNumber, pageSize);
                    break;
                case "date_desc":
                    list = db.Products.Where(expr).OrderByDescending(m => m.update_at).ToPagedList(pageNumber, pageSize);
                    break;
                case "name_asc":
                    list = db.Products.Where(expr).OrderBy(m => m.pro_name).ToPagedList(pageNumber, pageSize);
                    break;
                case "name_desc":
                    list = db.Products.Where(expr).OrderByDescending(m => m.pro_name).ToPagedList(pageNumber, pageSize);
                    break;
                default:
                    list = db.Products.Where(expr).OrderByDescending(m => m.update_at).ToPagedList(pageNumber, pageSize);
                    break;
            }
            ViewBag.Showing = list.Count();
            return list;
        }

        public ActionResult ProductDetail(int id, int? page)
        {
            int pagesize = 1;
            int cpage = page ?? 1;
            var product = db.Products.SingleOrDefault(m => m.status_ == "1" && m.pro_id == id);
            if (product == null)
            {
                return Redirect("/");
            }
            //sản phẩm liên quan
            ViewBag.relatedproduct = db.Products.Where(item => item.status_ == "1" && item.pro_id != product.pro_id).Take(8).ToList();
            ViewBag.ProductImage = db.ProductImgs.Where(item => item.product_id == id).ToList();
            ViewBag.ListFeedback = db.Feedbacks.Where(m => m.status == "2").ToList();
            ViewBag.ListReplyFeedback = db.ReplyFeedbacks.Where(m => m.status == "2").ToList();
            ViewBag.CountFeedback = db.Feedbacks.Where(m => m.status == "2" && m.product_id == product.pro_id).Count();
            ViewBag.OrderFeedback = db.Oder_Detail.ToList();
            var comment = db.Feedbacks.Where(m => m.product_id == product.pro_id && m.status == "2").OrderByDescending(m => m.create_at).ToList();
            ViewBag.PagerFeedback = comment.ToPagedList(cpage, pagesize);
            //product.view++;
            db.SaveChanges();
            return View(product);
        }
    }
}