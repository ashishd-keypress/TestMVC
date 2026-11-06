using System;

namespace TestMVC.Models.ReqRes
{
    public class GetBrandReq
    {
        public string? SearchText { get; set; }
        public bool? IsActive { get; set; }
        public int? UserId { get; set; }
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetBrandRes
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class GetBrandByIdReq
    {
        public int BrandId { get; set; }
        public int? UserId { get; set; }
    }

    public class SaveBrandReq
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
    }
}
