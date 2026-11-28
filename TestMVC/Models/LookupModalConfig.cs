namespace TestMVC.Models
{
    public class LookupModalConfig
    {
        public string ModalId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-tags";
        public string InsertLabel { get; set; } = "Add New Item";
        public string InsertInputId { get; set; } = string.Empty;
        public string InsertFormId { get; set; } = string.Empty;
        public string InsertButtonId { get; set; } = string.Empty;
        public string InsertPlaceholder { get; set; } = string.Empty;
        public string AlertBoxId { get; set; } = string.Empty;
        public string CountLabelId { get; set; } = string.Empty;
        public string TableBodyId { get; set; } = string.Empty;
        public string ColumnHeading { get; set; } = "Name";
        public string ListHeading { get; set; } = "Existing Items";
        public string OpenTriggerLabel { get; set; } = string.Empty;
    }
}
