using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Admin;

public class AuditLogModel : PageModel
{
    private readonly IAuditService _auditService;

    public AuditLogModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public IReadOnlyList<AuditLog> Logs { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public string? ActionFilter { get; set; }
    public string? UserFilter { get; set; }

    private const int PageSize = 50;

    public async Task OnGetAsync(int page = 1, string? action = null, string? userId = null)
    {
        CurrentPage = Math.Max(1, page);
        ActionFilter = action;
        UserFilter = userId;

        var total = await _auditService.GetCountAsync(action, userId);
        TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

        Logs = await _auditService.GetLogsAsync(
            skip: (CurrentPage - 1) * PageSize,
            take: PageSize,
            actionFilter: action,
            userIdFilter: userId);
    }
}
