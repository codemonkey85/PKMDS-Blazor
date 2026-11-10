namespace Pkmds.Rcl.Components.Pages;

// Keep the review playground out of the published app's routes.
#if DEBUG
[Route("/dropdown-demo")]
#endif
public partial class DropdownDemo;
