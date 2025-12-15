# PowerShell script to hash password using the same method as the server
# This requires the BCrypt.Net-Next package

Write-Host "Hashing password '12345' using BCrypt (work factor 12)..." -ForegroundColor Cyan

# Note: This script requires .NET to be available
# You can also use the API endpoint: GET /api/Utility/hash-password?password=12345

Write-Host ""
Write-Host "To hash the password, you can:" -ForegroundColor Yellow
Write-Host "1. Use the API endpoint: http://localhost:5000/api/Utility/hash-password?password=12345" -ForegroundColor Green
Write-Host "2. Or run the application and call the endpoint" -ForegroundColor Green
Write-Host ""
Write-Host "Example hashed password format: `$2a`$12`$..." -ForegroundColor Cyan

