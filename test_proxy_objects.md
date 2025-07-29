# Testing Proxy Object Support in ACadSharp Viewer

## Summary of Changes Made

I have successfully implemented support for displaying proxy objects and unknown objects in the ACadSharp viewer. Here are the key changes:

### 1. **File Loading Configuration**
- Modified `CadFileService.cs` to enable `KeepUnknownEntities = true` and `KeepUnknownNonGraphicalObjects = true` for both DXF and DWG readers
- This ensures that proxy objects are loaded and included in the document

### 2. **Tree Display Improvements**
- Updated `CadObjectTreeService.cs` to better display unknown objects:
  - **Unknown Entities**: Now show as `"ACAD_PROXY_ENTITY (Proxy Entity) (handle)"` instead of just `"UnknownEntity"`
  - **Unknown Non-Graphical Objects**: Now show as `"ACAD_PROXY_OBJECT (Proxy Object) (handle)"` instead of just `"UnknownNonGraphicalObject"`
  - **Object Type Display**: Shows the actual DXF class name in parentheses
  - **Special Section**: Added a dedicated "Unknown Objects (Proxy/Unsupported)" section in the tree

### 3. **Properties Panel**
- The properties panel already shows all public properties of unknown objects
- For proxy objects, this includes:
  - `DxfClass` information (DxfName, CppClassName, ProxyFlags, etc.)
  - `WasZombie` flag indicating if the class was not loaded when the file was created
  - Handle and other standard properties

## How to Test

### 1. **Load a File with Proxy Objects**
- Open the ACadSharp Viewer
- Load a DXF or DWG file that contains proxy objects
- Files created with third-party applications or newer AutoCAD versions often contain proxy objects

### 2. **Check the Object Tree**
- Look for the "Unknown Objects (Proxy/Unsupported)" section
- Proxy entities should appear as `"ACAD_PROXY_ENTITY (Proxy Entity) (handle)"`
- Proxy objects should appear as `"ACAD_PROXY_OBJECT (Proxy Object) (handle)"`
- The object type in parentheses should show the actual DXF class name

### 3. **Check Properties Panel**
- Select any proxy object in the tree
- The properties panel should show:
  - DxfClass information
  - ProxyFlags indicating what operations are allowed
  - WasZombie flag
  - Handle and other properties

### 4. **Search Functionality**
- Use the search feature to find proxy objects
- Search by object type, handle, or other properties
- Proxy objects should be included in search results

## Expected Behavior

### Before Changes:
- Proxy objects were not loaded (filtered out by default configuration)
- If loaded, they appeared as generic "UnknownEntity" or "UnknownNonGraphicalObject"
- Limited information about the actual proxy object type

### After Changes:
- Proxy objects are loaded and displayed in the tree
- Clear identification of proxy objects with descriptive names
- Full access to proxy object properties and metadata
- Dedicated section for unknown/proxy objects
- Search functionality includes proxy objects

## Technical Details

### Proxy Object Types Supported:
- `ACAD_PROXY_ENTITY` (0x1f2) - Proxy entities
- `ACAD_PROXY_OBJECT` (0x1f3) - Proxy non-graphical objects

### Configuration Settings:
- `KeepUnknownEntities = true` - Loads unknown entities including proxy entities
- `KeepUnknownNonGraphicalObjects = true` - Loads unknown non-graphical objects including proxy objects

### Display Improvements:
- Uses `ObjectName` property to show actual DXF class names
- Special handling for `UnknownEntity` and `UnknownNonGraphicalObject` types
- Enhanced display names with "(Proxy Entity)" and "(Proxy Object)" labels
- Dedicated tree section for unknown objects

## Files Modified:
1. `src/ACadSharp.Viewer/Services/CadFileService.cs` - Enabled proxy object loading
2. `src/ACadSharp.Viewer/Services/CadObjectTreeService.cs` - Improved tree display and added unknown objects section

The implementation ensures that proxy objects are now fully visible and accessible in the ACadSharp viewer, providing users with complete information about these objects that were previously hidden or poorly displayed. 