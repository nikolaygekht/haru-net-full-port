# Building and Running Tests

## Prerequisites

Install the .NET 10 SDK. The library targets .NET 8 (net8.0), tests, demos and tools target .NET 10 (net10.0).
- Windows: Download from https://dotnet.microsoft.com/download
- Linux: `sudo apt-get install dotnet-sdk-10.0` or similar
- macOS: `brew install dotnet`

## Build the Solution

From the project root directory:

```bash
# Restore dependencies
dotnet restore Haru.sln

# Build the entire solution
dotnet build Haru.sln

# Or build in Release mode
dotnet build Haru.sln -c Release
```

## Build Individual Projects

```bash
# Build Haru library
dotnet build cs-src/Haru/Haru.csproj

# Build Haru.Test project
dotnet build cs-src/Haru.Test/Haru.Test.csproj
```

## Run Tests

The tests use xUnit v3 on Microsoft Testing Platform (MTP). `global.json` in the repository root switches
`dotnet test` to MTP mode, which requires the **.NET 10 SDK**.
Run these commands from the repository root:

```bash
# Run all tests
dotnet test --solution cs-src/Haru.sln

# Run tests in a specific project
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj

# Show every test result
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --output Detailed

# Run tests with coverage (dotnet tool install -g dotnet-coverage)
dotnet-coverage collect "dotnet test --solution cs-src/Haru.sln" -f xml -o coverage.xml
```

The test project is also an executable, so it can be run directly (xUnit native switches such as `-class`):

```bash
dotnet run --project cs-src/Haru.Test/Haru.Test.csproj
dotnet run --project cs-src/Haru.Test/Haru.Test.csproj -- -class "Haru.Test.Png.GrayscalePngTests"
```

## Run Specific Test Classes

```bash
# Run only PNG reader basic tests (wildcards allowed)
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.PngReaderBasicTests"

# Run only grayscale tests
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.GrayscalePngTests"

# Run only palette tests
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.PalettePngTests"

# Run only RGB tests
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.RgbPngTests"

# Run only transparency tests
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter-class "*.TransparencyPngTests"

# The VSTest-style filter is still accepted as well:
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj --filter "FullyQualifiedName~TransparencyPngTests"
```

## Project Structure

```
claude3/
├── Haru.sln                          # Solution file
├── cs-src/
│   ├── Haru/                         # Main library project
│   │   ├── Haru.csproj
│   │   └── Png/                      # PNG facade
│   │       ├── IPngReader.cs
│   │       ├── ImageSharpPngReader.cs
│   │       ├── PngColorType.cs
│   │       ├── PngImageInfo.cs
│   │       ├── PngPalette.cs
│   │       ├── PngReaderFactory.cs
│   │       └── PngTransparency.cs
│   └── Haru.Test/                    # Test project
│       ├── Haru.Test.csproj
│       ├── TestHelper.cs             # Embedded resource helper
│       ├── Resources/                # Test PNG files (embedded)
│       │   ├── test_grayscale_2x2.png
│       │   ├── test_grayscale_alpha_2x2.png
│       │   ├── test_palette_2x2.png
│       │   ├── test_palette_trans_2x2.png
│       │   ├── test_rgb_2x2.png
│       │   └── test_rgba_2x2.png
│       └── Png/                      # PNG tests
│           ├── PngReaderBasicTests.cs
│           ├── GrayscalePngTests.cs
│           ├── PalettePngTests.cs
│           ├── RgbPngTests.cs
│           └── TransparencyPngTests.cs
└── c-src/                            # Original C source code

```

## Expected Test Results

All tests should pass once the .NET SDK is installed and the projects are built. The test suite includes:

- **Basic validation tests**: Signature validation, null checks, argument validation
- **Grayscale tests**: 8 tests for grayscale PNG functionality
- **Palette tests**: 7 tests for 256-color indexed PNG
- **RGB tests**: 8 tests for truecolor RGB PNG
- **Transparency tests**: 10+ tests for RGBA, grayscale+alpha, and palette+tRNS

## CI/CD Notes

The test resources are embedded in the assembly as `EmbeddedResource`, so the tests will work in CI/CD pipelines where only compiled binaries are present (no need to copy PNG files).

## Troubleshooting

If tests fail due to missing resources, check:
1. Resources are embedded: `<EmbeddedResource Include="Resources\**\*" />` in .csproj
2. Resource names match: `Haru.Test.Resources.<filename>`
3. Build the test project before running tests
