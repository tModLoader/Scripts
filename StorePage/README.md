# StorePage/
Executable script for generating consistent mod store page images, with a logo and/or subtitle placed consistently in the corner.

### Requirements
- .NET or Visual Studio

### Usage
- Place PNG screenshots into the `./inputs/` directory, named in manner of `ModId1.png`.
- Place PNG logos into the `./logos/` directory, named in manner of `ModId.png`.
- Customize options in the `./Screenshots.yaml` file.
- Run `dotnet run`, or run the project in Visual Studio.
- Check `./outputs/`.
