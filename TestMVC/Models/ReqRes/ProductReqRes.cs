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
        public string? Image { get; set; }
        public string? ImageUrl
        {
            get => Image;
            set => Image = value;
        }
        public string? ImagePath
        {
            get => Image;
            set => Image = value;
        }
        public decimal Price { get; set; }
        public decimal? MRP { get; set; }
        public decimal? SellingPrice { get; set; }
        public string? Size { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Color { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public int WarrantyMonths { get; set; } = 12;
        public bool TrackIMEI { get; set; } = false;
        public long? ProductModelId { get; set; }
        public string? ModelName { get; set; }
        public int StockQuantity { get; set; } = 0;
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
        public string? Image { get; set; }
        public string? ImageUrl
        {
            get => Image;
            set => Image = value;
        }
        public string? ImagePath
        {
            get => Image;
            set => Image = value;
        }
        public decimal Price { get; set; }
        public decimal? MRP { get; set; }
        public decimal? SellingPrice { get; set; }
        public string? Size { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Color { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public int WarrantyMonths { get; set; } = 12;
        public bool TrackIMEI { get; set; } = false;
        public long? ProductModelId { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
    }
}
