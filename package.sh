#!/bin/bash

# Clean up any existing package
rm -f invoice-system.zip

# Clean build artifacts
find . -type d -name "bin" -exec rm -rf {} +
find . -type d -name "obj" -exec rm -rf {} +

# Create temporary directory for packaging
mkdir -p temp_package

# Copy necessary files
cp -r src temp_package/
cp -r "database design" temp_package/
cp README.md temp_package/
cp .gitignore temp_package/

# Remove any existing appsettings.json and copy example
rm -f temp_package/src/InvoiceSystem.Web/appsettings.json
cp src/InvoiceSystem.Web/appsettings.json.example temp_package/src/InvoiceSystem.Web/

# Remove any development or temporary files
find temp_package -name "*.user" -type f -delete
find temp_package -name "*.suo" -type f -delete
find temp_package -name ".DS_Store" -type f -delete
find temp_package -name "*.db" -type f -delete
find temp_package -name "*.log" -type f -delete

# Create zip file
cd temp_package
zip -r ../invoice-system.zip .
cd ..

# Clean up
rm -rf temp_package

echo "Package created: invoice-system.zip" 