using System;

namespace TestMVC.Models.ReqRes
{
    public class GetProductReq
    {
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string? SearchText { get; set; }
        public bool? IsActive { get; set; }
        public int? UserId { get; set; }
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetProductRes
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Size { get; set; }
        public int? BrandId { get; set; }
        public string? Brand { get; set; }
        public int? CategoryId { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class GetProductByIdReq
    {
        public int ProductId { get; set; }
        public int? UserId { get; set; }
    }

    public class SaveProductReq
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Size { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
    }
}
