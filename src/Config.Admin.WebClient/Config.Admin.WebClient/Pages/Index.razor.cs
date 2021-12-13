using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Index
{
    [CascadingParameter] public PageError PageError { get; set; } = null!;
}