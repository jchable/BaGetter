using BaGetter.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BaGetter.Web.Admin;

public class ImportModel : PageModel
{
    private readonly FeedImportProgressTracker _tracker;

    public ImportModel(FeedImportProgressTracker tracker)
    {
        _tracker = tracker;
    }

    public bool IsRunning => _tracker.IsRunning;

    public void OnGet()
    {
    }
}
