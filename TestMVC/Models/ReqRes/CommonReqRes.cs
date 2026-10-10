namespace TestMVC.Models.ReqRes
{
    public class TranStatus
    {
        public int Code { get; set; } = 0; // 0 = Success, 2 = DB/System Error
        public string ReturnMessage { get; set; } = "Success.";
    }

    public class ToggleStatusReq
    {
        public int Id { get; set; }
        public bool? IsActive { get; set; }
        public int? UserId { get; set; }
    }

    public class ToggleStatusRes
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteReq
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
    }

    public class DeleteRes
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
