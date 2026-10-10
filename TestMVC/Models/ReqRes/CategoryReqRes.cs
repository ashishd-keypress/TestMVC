using System;

namespace TestMVC.Models.ReqRes
{
    public class GetCategoryReq
    {
        public string? SearchText { get; set; }
        public bool? IsActive { get; set; }
        public int? UserId { get; set; }
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetCategoryRes
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class GetCategoryByIdReq
    {
        public int CategoryId { get; set; }
        public int? UserId { get; set; }
    }

    public class SaveCategoryReq
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
    }
}
