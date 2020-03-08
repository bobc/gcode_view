# Gcode View
This an application to view the thumbnails embedded in GCode files by PrusaSlicer.

It does not generate a preview from the Gcode itself. Requires Windows.

# Installing

1. Checkout repo or download zip of master
2. Either :
- a) build the solution using Visual Studio and run the .exe
- b) install by running publish/setup.exe

# Usage

1. Run the application
2. Select a folder using "Choose..." button
3. Generate separate PNG file using "Create Thumbs" button

Only the menu Help|About is functional, the others are placeholders.


# Data usage

- The application creates a settings file in USERPROFILE:\AppData\Roaming\RMC
- The application writes PNG files to local disc if requested
- Does not use internet


