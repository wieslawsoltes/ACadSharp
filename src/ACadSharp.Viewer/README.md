# ACadSharp Viewer

A modern Avalonia-based desktop application for viewing and comparing DWG and DXF files using the ACadSharp library.

## Features

- **Dual-Pane Interface**: Side-by-side comparison of DWG and DXF files
- **Complete Object Tree**: Hierarchical view of all CAD objects in both files
- **Properties Panel**: Detailed property inspection for selected objects
- **Advanced Search**: Search by tag code, object handles, or object data
- **Modern UI**: Built with Avalonia UI framework for cross-platform compatibility
- **MVVM Architecture**: Clean separation of concerns following SOLID principles

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern and SOLID principles:

### Models
- `CadDocumentModel`: Represents a CAD document with metadata and collections
- `CadObjectTreeNode`: Tree node structure for object hierarchy
- `ObjectProperty`: Property representation for CAD objects
- `SearchCriteria`: Search parameters for finding objects

### ViewModels
- `ViewModelBase`: Base class with ReactiveUI support
- `MainWindowViewModel`: Main application logic and state management

### Views
- `MainWindow`: Main application window with split-pane layout
- Modern XAML-based UI with responsive design

### Services
- `ICadFileService`: Interface for file operations
- `CadFileService`: Implementation for loading DWG/DXF files
- `ICadObjectTreeService`: Interface for tree operations
- `CadObjectTreeService`: Implementation for building object trees and searching

### Interfaces
- Clean interfaces following dependency inversion principle
- Easy to test and extend

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Building the Application

1. Clone the repository
2. Navigate to the `src` directory
3. Build the solution:
   ```bash
   dotnet build ACadSharp.sln
   ```

### Running the Application

1. Navigate to the viewer project:
   ```bash
   cd ACadSharp.Viewer
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

## Usage

### Loading Files

1. Click "Load DWG" to select a DWG file for the left panel
2. Click "Load DXF" to select a DXF file for the right panel
3. The application will automatically parse and display the file structure

### Navigating the Object Tree

- Expand/collapse tree nodes to explore the document structure
- Click on any object to view its properties in the properties panel
- The tree shows:
  - Document header
  - Tables (Layers, Block Records, Text Styles, etc.)
  - Objects (Root Dictionary)
  - Entities (Model Space objects)

### Searching Objects

1. Use the search box in the toolbar
2. Search by:
   - Object handle (hexadecimal)
   - Object type name
   - Object data/properties
3. Search results are highlighted in both trees
4. Clear search to remove highlighting

### Comparing Files

- Load both DWG and DXF files to compare their structures
- Navigate through corresponding sections in both panels
- Use the search feature to find specific objects in both files
- Compare properties of similar objects

## Development

### Project Structure

```
ACadSharp.Viewer/
├── Models/              # Data models
├── ViewModels/          # ViewModels with business logic
├── Views/               # XAML views
├── Services/            # Business services
├── Interfaces/          # Service interfaces
├── Assets/              # Application resources
└── App.axaml            # Application definition
```

### Adding New Features

1. **New Services**: Implement interfaces in the `Interfaces` folder
2. **New Models**: Add to the `Models` folder
3. **New ViewModels**: Extend `ViewModelBase`
4. **New Views**: Create XAML files in the `Views` folder

### Testing

The application is designed for easy testing:
- Services implement interfaces for dependency injection
- ViewModels use ReactiveUI for reactive programming
- Models implement `INotifyPropertyChanged` for UI updates

## Dependencies

- **ACadSharp**: Core CAD file processing library
- **Avalonia**: Cross-platform UI framework
- **ReactiveUI**: Reactive programming for MVVM
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Logging**: Logging framework

## Contributing

1. Follow the existing code style and architecture
2. Implement interfaces for new services
3. Use ReactiveUI patterns for ViewModels
4. Add appropriate error handling
5. Update documentation for new features

## License

This project is part of the ACadSharp library and follows the same MIT license.

## Support

For issues and questions:
- Check the ACadSharp documentation
- Review the existing code examples
- Create an issue in the main ACadSharp repository 