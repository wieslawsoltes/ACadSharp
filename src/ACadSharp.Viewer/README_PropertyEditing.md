# Property Editing Feature

This document describes the property editing functionality added to the ACadSharp Viewer.

## Overview

The property editing feature allows users to modify primitive and complex property values directly in the properties grid. This enhances the viewer's capability from read-only to interactive editing.

## Features Added

### 1. Enhanced ObjectProperty Model

The `ObjectProperty` class in `Interfaces/ICadObjectTreeService.cs` has been enhanced with:

- **IsEditable**: Determines if a property can be edited
- **PropertyInfo**: Reflection information for property access
- **SourceObject**: The object containing the property
- **UnderlyingType**: Helper property for handling nullable types
- **IsPrimitiveEditable**: Indicates if property is a directly editable primitive
- **TrySetValue()**: Method to safely set property values with type conversion
- **ConvertValue()**: Private method for type conversion

### 2. Property Editors

#### Primitive Type Editors (`Controls/PropertyEditors.cs`)

- **EditablePropertyTextBox**: For string properties
- **EditablePropertyCheckBox**: For boolean properties  
- **EditablePropertyComboBox**: For enum properties
- **EditablePropertyNumericUpDown**: For numeric properties (int, long, short, byte, double, float, decimal)

#### Complex Type Editors (`Controls/ComplexPropertyEditors.cs`)

- **CoordinatePropertyEditor**: For XYZ and XY coordinate editing
- **ColorPropertyEditor**: For CAD color property editing

### 3. Value Converters (`Converters/PropertyValueConverters.cs`)

- **BooleanValueConverter**: Converts between boolean values and strings
- **EnumValueConverter**: Handles enum value conversion
- **NumericValueConverter**: Converts between numeric types and strings
- **PropertyValueUpdateConverter**: Handles property value updates with validation

### 4. Type Converters (`Converters/TypeConverters.cs`)

Static converters for type checking:
- **IsNumericType**: Identifies numeric types
- **IsStringType**: Identifies string types
- **IsBooleanType**: Identifies boolean types
- **IsEnumType**: Identifies enum types

### 5. Property Editing Commands (`ViewModels/PropertyEditingCommands.cs`)

- **EditPropertyCommand**: Command for property editing operations
- **ValidatePropertyCommand**: Command for property value validation
- Comprehensive validation logic for different data types
- Error handling and logging

### 6. UI Updates

#### MainWindow.axaml Changes

- Updated DataGrid to support editing (`IsReadOnly="False"`)
- Added conditional property editors using MultiBinding and type converters
- Visual indicators for editable properties (`[E]` marker)
- Support for both primitive and complex property editing

#### Standard DataGrid Editing

The UI uses standard DataGrid editing capabilities:
- **Display Template**: Shows property values for navigation and read-only display
- **Edit Template**: Text editor for editing property values in-place
- **Navigable properties**: Button for navigating to related objects
- **Read-only properties**: TextBlock display only
- **Edit Validation**: Values are validated before being applied

### 7. ViewModel Integration

#### MainWindowViewModel Updates

- **PropertyEditingCommands**: Exposed property editing functionality
- **EditPropertyCommand**: Command for property editing
- **ValidatePropertyValueCommand**: Command for validation
- **RefreshPropertyGrids()**: Method to refresh UI after edits

#### CadDocumentModel Updates

- **RefreshSelectedObjectProperties()**: Method to refresh property display after editing
- Enhanced property generation with editing metadata

## Usage

### Editing Properties

1. Select an object in the tree view
2. Locate the property in the properties grid
3. Editable properties are marked with a green `[E]` indicator
4. **Double-click** the property value or **press F2** to enter edit mode
5. **Type the new value** in the text editor
6. **Press Enter** to apply changes or **Escape** to cancel
7. **Invalid values** are rejected and the editor remains open

### Validation

- Property values are validated before being set
- Invalid values are rejected and the original value is restored
- Type conversion is handled automatically
- Error handling prevents application crashes

### Error Handling

- Comprehensive try-catch blocks prevent crashes
- Invalid edits are logged but don't interrupt workflow
- Properties revert to original values on failed edits

## Technical Implementation

### Property Detection

The system determines editability based on:
- Property has public setter (`CanWrite`)
- Setter method is public (`SetMethod?.IsPublic`)
- Property is not marked as read-only
- Property type is supported (primitives, enums, or has custom editors)

### Type Support

#### Fully Supported Types
- **Primitives**: int, long, short, byte, double, float, decimal, bool
- **String**: Full text editing support
- **Enums**: Dropdown selection with all enum values
- **Nullable versions** of all above types

#### Complex Type Support
- **Coordinates**: XY and XYZ types with component editing
- **Colors**: CAD color properties with predefined and custom options

#### Extensibility

The system is designed to be easily extensible:
- Add new editors by implementing custom controls
- Add new type converters for additional data types
- Register new template selectors for complex types

## Configuration

### Adding New Property Editors

1. Create a new control inheriting from appropriate Avalonia control
2. Implement `ObjectProperty` binding
3. Add type detection logic to `TypeConverters`
4. Update UI template selection in `MainWindow.axaml`

### Customizing Validation

Extend the validation logic in `PropertyEditingCommands.ValidateProperty()` to add custom validation rules for specific property types or values.

## Performance Considerations

- Property reflection is cached in ObjectProperty instances
- Type conversion is optimized for common scenarios
- UI updates are throttled to prevent excessive refreshing
- Validation is lightweight and doesn't block UI

## Future Enhancements

Potential areas for expansion:
- **Collection editing**: Support for editing array/list properties
- **Object reference editing**: Support for changing object references
- **Constraint validation**: Property-specific validation rules
- **Undo/Redo**: Transaction support for property changes
- **Batch editing**: Multi-object property editing
- **Custom editors**: Plugin system for application-specific property types