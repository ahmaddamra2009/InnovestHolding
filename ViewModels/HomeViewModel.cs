using InnovestHolding.Models;

namespace InnovestHolding.ViewModels;

public class HomeViewModel
{
    public Dictionary<string, string> Content { get; set; } = new();
    public List<NavItem> NavItems { get; set; } = new();
    public List<Event> Events { get; set; } = new();
    public List<News> News { get; set; } = new();
    public List<Platform> Platforms { get; set; } = new();
    public List<Partner> Partners { get; set; } = new();
    public List<Faq> Faqs { get; set; } = new();
    public List<Stat> Stats { get; set; } = new();

    public string Get(string key, string fallback = "") =>
        Content.TryGetValue(key, out var val) ? val : fallback;
}
