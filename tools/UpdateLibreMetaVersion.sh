#! /bin/bash
# Script to go through all the .csproj files and update
#   the version number of a package.

find src -name \*.csproj | xargs sed -i -e 's/Include="LibreMetaverse" Version=".*"/Include="LibreMetaverse" Version="2.5.8.102"/'

