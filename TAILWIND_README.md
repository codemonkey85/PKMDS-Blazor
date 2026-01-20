# Tailwind CSS Integration

This project uses Tailwind CSS for utility-first styling. The integration is designed to be seamless and requires no manual intervention.

## How it works

1. **Automatic CLI Download**: The Tailwind CSS CLI binary is automatically downloaded during the first build if not present. The binary is platform-specific (Windows, Linux, or macOS) and is excluded from source control via `.gitignore`.

2. **Automatic Build**: Tailwind CSS is built automatically during the normal `dotnet build` or `dotnet publish` process via MSBuild targets.

3. **Configuration**: 
   - `Pkmds.Rcl/tailwind.config.js` - Tailwind configuration file
   - `Pkmds.Rcl/wwwroot/css/tailwind.input.css` - Input CSS with Tailwind directives
   - `Pkmds.Rcl/wwwroot/css/tailwind.css` - Generated output CSS (gitignored)

## Development Workflow

### Building the project
```bash
dotnet build
```
This will automatically:
- Download the Tailwind CLI if needed
- Generate the Tailwind CSS
- Build the project

### Publishing the project
```bash
dotnet publish -c Release
```
This will generate a minified version of the Tailwind CSS for production.

### Clean build
```bash
dotnet clean
```
This will remove the generated Tailwind CSS file.

## CI/CD

The existing GitHub Actions workflows will work without any changes:
- The Tailwind CLI will be downloaded automatically during the build
- The generated CSS will be included in the published output
- No additional configuration or setup steps are needed

## Adding Tailwind Classes

Simply use Tailwind utility classes in your Razor components:

```razor
<div class="flex justify-center items-center p-4">
    <button class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded">
        Click me
    </button>
</div>
```

## Customizing Tailwind

Edit `Pkmds.Rcl/tailwind.config.js` to customize your Tailwind configuration:

```javascript
module.exports = {
  content: [
    './**/*.{razor,html,cshtml}',
    '../Pkmds.Web/**/*.{razor,html,cshtml}'
  ],
  theme: {
    extend: {
      // Add your customizations here
      colors: {
        'custom': '#your-color',
      },
    },
  },
  plugins: [],
}
```

## Troubleshooting

### CSS not updating
Run `dotnet clean` followed by `dotnet build` to force a rebuild of the Tailwind CSS.

### Tailwind CLI download fails
If the automatic download fails, check your internet connection and firewall settings. The binary is downloaded from GitHub releases.
