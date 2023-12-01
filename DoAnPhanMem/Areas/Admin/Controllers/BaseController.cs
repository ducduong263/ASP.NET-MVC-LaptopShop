using DoAnPhanMem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using DoAnPhanMem;


namespace DoAnPhanMem.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {
        // GET: Admin/Base
        public BaseController()
        {
                var user = Session["TaiKhoan"] as Account;
                if (user.Role.role_name != "Admin" || user.Role.role_name != "Nhân viên")
                {
                    System.Web.HttpContext.Current.Response.Redirect("~/Home/Index");
                }
        }
        //đăng xuất admin quay về trang chủ
        public ActionResult Logout()
        {
            Session.Clear();
            return Redirect("~/Home/Index");
        }
        //chuyển từ trang admin sang trang thông tin cá nhân
        public ActionResult ViewProfile()
        {
           
            return Redirect("~/Account/Editprofile");
        }
        //chuyển từ trang admin sang trang chủ
        public ActionResult BackToHome()
        {

            return Redirect("~/Home/Index");

        }
    }
}