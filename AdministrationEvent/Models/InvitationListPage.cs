using System.Collections.Generic;
namespace AdministrationEvent.Models
{
    public class InvitationListPage
    {
        public List<InvitationConfiguration> Items { get; set; } = new();
        public string Search { get; set; } = "";
        public string Status { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageCount { get; set; } = 1;
        public int Total { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
