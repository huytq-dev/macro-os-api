<#
  Tải file JSON chính thức của USDA FoodData Central (Foundation, SR Legacy, Survey/FNDDS) cho `import-usda-bulk` rồi giải nén.
  KHÔNG tải Branded (hàng trăm MB, không dùng). Dữ liệu nằm ngoài git: mặc định BE/.usda-data (đã .gitignore).

    pwsh BE/scripts/download-usda.ps1                      # -> BE/.usda-data
    pwsh BE/scripts/download-usda.ps1 -Dir D:\usda-data

  Rồi: dotnet run --project src/API -- import-usda-bulk --dir <thư mục>   (mặc định dry-run, xem Docs/Design/Food_Reseed.md mục 9)
  Tên file có ngày nên script đọc trang https://fdc.nal.usda.gov/download-datasets lấy bản MỚI NHẤT của từng loại (SR Legacy chỉ có bản 2018-04).
#>
param([string]$Dir = (Join-Path $PSScriptRoot '..\.usda-data'))

$ErrorActionPreference = 'Stop'
$base = 'https://fdc.nal.usda.gov/'
$zips = Join-Path $Dir 'zips'
New-Item -ItemType Directory -Force $zips | Out-Null

Write-Host 'Đọc danh sách bản tải...'
$page = (Invoke-WebRequest -UseBasicParsing "${base}download-datasets/").Content   # có dấu / cuối: không có thì FDC chuyển hướng sang http

foreach ($prefix in 'FoodData_Central_foundation_food', 'FoodData_Central_sr_legacy_food', 'FoodData_Central_survey_food') {
    $names = [regex]::Matches($page, "fdc-datasets/(${prefix}_json_[0-9-]+\.zip)") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    if (-not $names) { throw "Không tìm thấy bản JSON của $prefix trên trang tải." }
    $name = $names | Select-Object -Last 1          # ngày dạng ISO nên sắp xếp chữ cái = mới nhất
    $zip = Join-Path $zips $name
    if (-not (Test-Path $zip)) {
        Write-Host "Tải $name ..."
        Invoke-WebRequest -UseBasicParsing "${base}fdc-datasets/$name" -OutFile $zip
    } else { Write-Host "Đã có $name" }
    Expand-Archive -Path $zip -DestinationPath $Dir -Force
}

Write-Host "Xong. File JSON ở: $((Resolve-Path $Dir).Path)"
Get-ChildItem $Dir -Filter *.json | ForEach-Object { '{0,-60} {1,8:N1} MB' -f $_.Name, ($_.Length / 1MB) }
