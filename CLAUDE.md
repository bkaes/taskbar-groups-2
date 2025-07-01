# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

**Build the solution:**
```bash
msbuild TaskbarGroups.sln /p:Configuration=Release
```

**Build for debug:**
```bash
msbuild TaskbarGroups.sln /p:Configuration=Debug
```

**Restore NuGet packages:**
```bash
nuget restore TaskbarGroups.sln
```

**Clean build:**
```bash
msbuild TaskbarGroups.sln /t:Clean
```

## Architecture Overview

This is a Windows Forms application (.NET Framework 4.7.2) that creates pinnable taskbar groups for Windows. The architecture consists of:

### Core Components

1. **Entry Point** (`client.cs`): 
   - Handles command-line arguments to determine if launching the configuration app or a group
   - Sets up required directories (config, JITComp, Shortcuts)
   - Uses AppUserModelID to prevent Windows from stacking the app with itself

2. **Forms** (UI layer):
   - `frmClient`: Main configuration window for managing groups
   - `frmGroup`: Window for creating/editing groups and their shortcuts
   - `frmMain`: Window displayed when a taskbar group is activated

3. **Data Models**:
   - `Category`: Represents a taskbar group with its configuration, shortcuts, and visual settings
   - `ProgramShortcut`: Individual shortcuts within a group

4. **Key Services**:
   - `ShellLink`: Creates Windows shortcuts with custom AppUserModelID for taskbar integration
   - `IconFactory`: Handles icon extraction and caching from executables and shortcuts
   - `handleWindowsApp`: Special handling for Windows Store app shortcuts
   - `handleFolder`: Special handling for folder shortcuts

### Data Flow

1. Groups are created/edited in `frmGroup` and saved as `Category` objects
2. Category data is serialized to XML in `/config/[GroupName]/ObjectData.xml`
3. Icons are cached in `/config/[GroupName]/Icons/` for performance
4. Windows shortcuts are created in `/Shortcuts/` folder with special properties
5. When clicked, shortcuts launch the app with the group name as argument
6. `frmMain` reads the group data and displays the shortcuts for launching

### Key Design Patterns

- XML serialization for configuration persistence
- Icon caching to avoid repeated extraction
- Windows Shell integration via COM interop
- AppUserModelID manipulation for taskbar behavior

## Important Implementation Details

- The app requires different handling for regular executables, Windows shortcuts (.lnk), Windows Store apps, and folders
- Icons are extracted differently based on file type and cached locally
- Working directories and arguments can be configured per shortcut
- Keyboard shortcuts (1-0 keys) launch shortcuts by position
- Ctrl+Enter launches all shortcuts in a group (if enabled)