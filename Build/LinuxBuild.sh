#!/bin/bash

outputPath="linux"

if [ -d "$outputPath" ]; then
    rm -rf "$outputPath"
fi

mkdir -p "$outputPath"

dotnet publish ../EmmyLua.LanguageServer -c Release --output "$outputPath" -r Linux-x64
