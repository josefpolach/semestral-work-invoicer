#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
release_dir=artifacts/release
app_dir="$release_dir/Invoicing-win-x64"
archive="$app_dir.zip"

rm -rf -- "$app_dir"
rm -f -- "$archive"
dotnet publish src/Invoicing.App/Invoicing.App.csproj -c Release -r win-x64 --self-contained true -o "$app_dir"

mkdir -p "$app_dir/data"
export UID
export GID="$(id -g)"
DATA_DIR="./$app_dir/data" docker compose --env-file .env.example run --rm flyway
cp .env.example "$app_dir/.env"
if ! awk -F= '
    $1 == "LETTR_API_KEY" { found = 1; if (length($0) != length($1) + 1) exit 1 }
    END { if (!found) exit 1 }
' "$app_dir/.env"; then
    echo "Expected an empty LETTR_API_KEY in the release configuration." >&2
    exit 1
fi

if command -v zip >/dev/null; then
    (cd "$release_dir" && zip -qr Invoicing-win-x64.zip Invoicing-win-x64)
else
    (cd "$release_dir" && python3 -m zipfile -c Invoicing-win-x64.zip Invoicing-win-x64)
fi
printf 'Built %s\n' "$archive"
