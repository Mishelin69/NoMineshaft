#!/bin/bash

if [ -z "$LETHAL_PLUGIN_PATH" ]; then
    echo "WARNING: LETHAL_PLUGIN_PATH is not set"
    DEST_PATH="./TestPlugins" 
else
    DEST_PATH="$LETHAL_PLUGIN_PATH"
fi

DLL_NAME="mishelin.NoMineshaft.dll"
BUILD_OUTPUT="./bin/Release/netstandard2.1"
ZIP_NAME="NoMineshaft_Release.zip"

dotnet build --configuration Release

if [ $? -eq 0 ]; then
    echo "--- Build Successful. Moving files... ---"

    # Create destination directory if it doesn't exist
    mkdir -p "$DEST_PATH"

    # uncoment this if you want to copy the dll into somewhere
    cp "$BUILD_OUTPUT/$DLL_NAME" "$DEST_PATH/"
    zip -j "$ZIP_NAME" "$BUILD_OUTPUT/$DLL_NAME" "$BUILD_OUTPUT/manifest.json" "README.md" "icon.png"
else
    echo "!!! Build Failed. Aborting move. !!!"
    exit 1
fi
