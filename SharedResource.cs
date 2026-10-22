namespace InnovestHolding;

/// <summary>
/// Marker type of the single shared string resource: Resources/SharedResource.resx (neutral = Arabic, also the
/// fallback for a missing English key) and Resources/SharedResource.en.resx. It must stay in the ROOT namespace:
/// with ResourcesPath "Resources" the runtime looks for InnovestHolding.Resources.SharedResource.
/// </summary>
public sealed class SharedResource
{
}
